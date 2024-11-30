namespace SqlKata.VisitorCompilers
{
    public class OracleVisitorCompiler : VisitorCompiler
    {
        public bool UseLegacyPagination { get; set; }

        public void ApplyLegacyLimit(SqlResult ctx)
        {
            throw new System.NotImplementedException();
        }

        public string CompileLimit(SqlResult ctx)
        {
            throw new System.NotImplementedException();
        }
    }
}
