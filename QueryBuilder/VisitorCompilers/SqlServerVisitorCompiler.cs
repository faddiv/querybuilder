namespace SqlKata.VisitorCompilers
{
    public class SqlServerVisitorCompiler : VisitorCompiler
    {
        public bool UseLegacyPagination { get; set; }

        public string CompileLimit(SqlResult ctx)
        {
            throw new System.NotImplementedException();
        }
    }
}
