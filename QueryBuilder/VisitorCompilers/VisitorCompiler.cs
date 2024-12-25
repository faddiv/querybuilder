namespace SqlKata.VisitorCompilers
{
    public abstract class VisitorCompiler
    {
        protected virtual string ParameterPlaceholder { get; set; } = "?";
        protected virtual string EscapeCharacter { get; set; } = "\\";

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
    }
}
