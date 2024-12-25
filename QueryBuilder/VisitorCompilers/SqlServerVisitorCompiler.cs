using SqlKata.Compilers;

namespace SqlKata.VisitorCompilers
{
    public class SqlServerVisitorCompiler : VisitorCompiler
    {
        public override string EngineCode => EngineCodes.SqlServer;

        public bool UseLegacyPagination { get; set; }
    }
}
