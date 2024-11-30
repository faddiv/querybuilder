using System.Collections.Generic;
using System.Text;

namespace SqlKata.VisitorCompilers
{
    public class SqlResultBuilder
    {
        private readonly Query _query;
        private readonly StringBuilder _builder = new StringBuilder();

        public SqlResultBuilder(Query query)
        {
            _query = query;
        }
        public void Append(string sql)
        {
            _builder.Append(sql);
        }

        public SqlResult PrepareResult(string parameterPlaceholder, string escapeCharacter)
        {
            var prepareResult = new SqlResult(parameterPlaceholder, escapeCharacter)
            {
                Query = _query,
                RawSql = _builder.ToString(),
                Sql = _builder.ToString(), // Prepare
                Bindings = new List<object>(),
                NamedBindings = new Dictionary<string, object>()
            };
            return prepareResult;
        }
    }
}
