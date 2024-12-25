using SqlKata.Compilers;

namespace SqlKata.VisitorCompilers
{
    public class OracleVisitorCompiler : VisitorCompiler
    {
        public override string EngineCode => EngineCodes.Oracle;

        public bool UseLegacyPagination { get; set; }
    }
}
