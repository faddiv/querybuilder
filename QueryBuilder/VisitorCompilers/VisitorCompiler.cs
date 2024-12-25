using System.Collections.Generic;

namespace SqlKata.VisitorCompilers
{
    public abstract class VisitorCompiler
    {
        private HashSet<string> userOperators;
        protected virtual string ParameterPlaceholder { get; set; } = "?";
        protected virtual string EscapeCharacter { get; set; } = "\\";

        /// <summary>
        /// If true the compiler will remove the SELECT clause for the query used inside WHERE EXISTS
        /// </summary>
        public virtual bool OmitSelectInsideExists { get; set; } = true;

        public abstract string EngineCode { get; }

        protected VisitorCompiler()
        {
        }

        public virtual SqlResult Compile(Query query)
        {
            var sqlResultBuilder = new SqlResultBuilder(query);
            CompileRaw(query, sqlResultBuilder);

            var sqlResult = sqlResultBuilder.PrepareResult(ParameterPlaceholder, EscapeCharacter);

            return sqlResult;
        }

        private SqlResult PrepareResult(SqlResultBuilder sqlResultBuilder)
        {
            throw new System.NotImplementedException();
        }


        private void CompileRaw(AbstractQuery query, SqlResultBuilder builder)
        {
        }

        public VisitorCompiler Whitelist(params string[] operators)
        {
            foreach (var op in operators)
            {
                userOperators.Add(op);
            }

            return this;
        }
    }
}
