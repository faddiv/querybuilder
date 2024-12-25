using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using SqlKata.VisitorCompilers.Helpers;

namespace SqlKata.VisitorCompilers
{
    public class SqlResultBuilder
    {
        private readonly Query _query;
        private readonly StringBuilder _builderRaw = new StringBuilder();
        private readonly StringBuilder _builderSql = new StringBuilder();
        private readonly List<object> _bindings = new List<object>();
        private readonly Dictionary<string, object> _namedBindings = new Dictionary<string, object>();

        private SeparatorTracker SeparatorTracker { get; } = new SeparatorTracker();

        public IReadOnlyList<object> Bindings => _bindings;


        public SqlResultBuilder(Query query)
        {
            _query = query;
        }

        public void Append(string sql)
        {
            _builderRaw.Append(sql);
            _builderSql.Append(sql);
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

        public void AddValue(object value, string parameterPrefix)
        {
            var parameter = $"{parameterPrefix}{_bindings.Count}";
            _builderRaw.Append('?');
            _builderSql.Append(parameter);
            _bindings.Add(value);
            _namedBindings.Add(parameter, value);
        }

        public void AppendRaw(string rawConditionExpression, object[] rawConditionBindings)
        {
            throw new NotImplementedException();
        }
    }
}
