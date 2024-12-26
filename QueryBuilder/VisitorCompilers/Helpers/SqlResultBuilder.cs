using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SqlKata.VisitorCompilers.Helpers;

namespace SqlKata.VisitorCompilers
{
    public class SqlResultBuilder
    {
        private readonly Query _query;
        private readonly string _parameterPrefix;
        private readonly StringBuilder _builderRaw = new StringBuilder();
        private readonly StringBuilder _builderSql = new StringBuilder();
        private readonly List<object> _bindings = new List<object>();
        private readonly Dictionary<string, object> _namedBindings = new Dictionary<string, object>();

        private SeparatorTracker SeparatorTracker { get; } = new SeparatorTracker();

        public IReadOnlyList<object> Bindings => _bindings;


        public SqlResultBuilder(Query query, string parameterPrefix)
        {
            _query = query;
            _parameterPrefix = parameterPrefix;
        }

        public void Append(string sql)
        {
            _builderRaw.Append(sql);
            _builderSql.Append(sql);
        }

        public void Append(string sql, int start, int length)
        {
            _builderRaw.Append(sql, start, length);
            _builderSql.Append(sql, start, length);
        }
        public void AppendSeparator()
        {
            SeparatorTracker.AppendSeparator(_builderRaw);
            SeparatorTracker.AppendSeparator(_builderSql);
        }

        public SqlResult PrepareResult(string parameterPlaceholder, string escapeCharacter)
        {
            var prepareResult = new SqlResult(parameterPlaceholder, escapeCharacter)
            {
                Query = _query,
                RawSql = _builderRaw.ToString(),
                Sql = _builderSql.ToString(), // Prepare
                Bindings = _bindings,
                NamedBindings = _namedBindings
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
            var parameter = $"{_parameterPrefix}{_bindings.Count}";
            _builderRaw.Append('?');
            _builderSql.Append(parameter);
            _bindings.Add(value);
            _namedBindings.Add(parameter, value);
        }

        public void AppendRaw(string rawConditionExpression, object[] rawConditionBindings)
        {
            var bindingCount = rawConditionExpression.Count(c => c == '?');
            if (bindingCount != rawConditionBindings.Length)
            {
                throw new ArgumentException("Raw condition expression contains unexpected number of bindings");
            }
            var regex = new Regex(@"\?");
            var index = 0;
            var sql = regex.Replace(rawConditionExpression, match =>
            {
                var parameter = $"{_parameterPrefix}{_bindings.Count}";
                _bindings.Add(rawConditionBindings[index]);
                index++;
                return parameter;
            });
            _builderRaw.Append(rawConditionExpression);
            _builderSql.Append(sql);
        }
    }
}
