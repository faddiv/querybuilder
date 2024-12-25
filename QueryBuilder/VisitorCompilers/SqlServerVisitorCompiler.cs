using SqlKata.Compilers;

namespace SqlKata.VisitorCompilers
{
    public class SqlServerVisitorCompiler : VisitorCompiler
    {
        public override string EngineCode => EngineCodes.SqlServer;
        protected override string OpeningIdentifier => "[";
        protected override string ClosingIdentifier => "]";

        public bool UseLegacyPagination { get; set; }
    }
}
