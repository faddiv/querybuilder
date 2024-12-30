using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SqlKata.VisitorCompilers.Helpers;

namespace SqlKata.VisitorCompilers
{
    public class SqlResultBuilder
    {
        private readonly string _parameterPrefix;
        private readonly StringBuilder _builderRaw = new StringBuilder();
        private readonly List<object> _bindings = new List<object>();

        private SeparatorTracker SeparatorTracker { get; } = new SeparatorTracker();

        public IReadOnlyList<object> Bindings => _bindings;

        public SqlResultBuilder(string parameterPrefix)
        {
            _parameterPrefix = parameterPrefix;
        }

        public void ExecuteOnComponents<TComponent, TState>(
            Query query,
            string component,
            string engineCode,
            TState state,
            Action<ComponentFilter<TComponent>, TState, SqlResultBuilder> action)
            where TComponent : AbstractClause
        {
            var useEngine = engineCode ?? query.EngineScope;

            var filter = new ComponentFilter<TComponent>(component, useEngine, query.Clauses);

            if (!filter.SeekFirstElement())
            {
                return;
            }

            action(filter, state, this);
        }

        public bool TryGetComponent<TComponent>(
            Query query,
            string component,
            string engineCode,
            out TComponent componentValue)
            where TComponent : AbstractClause
        {
            var useEngine = engineCode ?? query.EngineScope;

            var filter = new ComponentFilter<TComponent>(component, useEngine, query.Clauses);

            if (!filter.MoveNext())
            {
                componentValue = null;
                return false;
            }

            componentValue = filter.Current;
            return true;
        }

        public void Append(string sql)
        {
            _builderRaw.Append(sql);
        }

        public void Append(string sql, int start, int length)
        {
            _builderRaw.Append(sql, start, length);
        }

        public void AppendSeparator()
        {
            SeparatorTracker.AppendSeparator(_builderRaw);
        }

        public SqlResult PrepareResult(Query query, string parameterPlaceholder, string escapeCharacter)
        {
            var rawSql = _builderRaw.ToString();
            var namedBindings = new Dictionary<string, object>();
            var sql = BuildSqlWithNamedBindings(rawSql, parameterPlaceholder, namedBindings);

            var prepareResult = new SqlResult(parameterPlaceholder, escapeCharacter)
            {
                Query = query,
                RawSql = rawSql,
                Sql = sql.ToString(), // Prepare
                Bindings = _bindings,
                NamedBindings = namedBindings
            };
            return prepareResult;
        }

        public void StartSection(string separator)
        {
            SeparatorTracker.StartSection(separator);
        }

        public void EndSection()
        {
            SeparatorTracker.EndSection();
        }

        public void AddValue(object value)
        {
            _builderRaw.Append('?');
            _bindings.Add(value);
        }

        public void AppendRaw(string rawConditionExpression, object[] rawConditionBindings)
        {
            var bindingCount = rawConditionExpression.Count(c => c == '?');
            if (bindingCount != rawConditionBindings.Length)
            {
                throw new ArgumentException("Raw condition expression contains unexpected number of bindings");
            }

            _bindings.AddRange(rawConditionBindings);
            _builderRaw.Append(rawConditionExpression);
        }

        private StringBuilder BuildSqlWithNamedBindings(
            string rawSql,
            string parameterPlaceholder,
            Dictionary<string, object> namedBindings)
        {
            var index = 0;
            var sql = new StringBuilder(PredictLengthNeeded(rawSql));
            var startSlice = 0;
            var endSlice = 0;
            while (endSlice < rawSql.Length)
            {
                endSlice = parameterPlaceholder.Length == 1
                    ? rawSql.IndexOf(parameterPlaceholder[0], startSlice)
                    : rawSql.IndexOf(parameterPlaceholder, startSlice, StringComparison.Ordinal);
                if (endSlice == -1)
                {
                    sql.Append(rawSql, startSlice, rawSql.Length - startSlice);
                    break;
                }

                sql.Append(rawSql, startSlice, endSlice - startSlice);
                var parameter = $"{_parameterPrefix}{index}";
                namedBindings.Add(parameter, _bindings[index]);
                sql.Append(parameter);
                index++;
                startSlice = endSlice + parameterPlaceholder.Length;
            }

            return sql;
        }

        private int PredictLengthNeeded(string rawSql)
        {
            return rawSql.Length + (_bindings.Count < 10 ? _bindings.Count * 2 : _bindings.Count * 3);
        }
    }
}
