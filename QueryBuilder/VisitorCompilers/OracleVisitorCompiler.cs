using SqlKata.Compilers;

namespace SqlKata.VisitorCompilers
{
    public class OracleVisitorCompiler : VisitorCompiler
    {
        public override string EngineCode => EngineCodes.Oracle;

        protected override string ParameterPrefix => ":p";
        public bool UseLegacyPagination { get; set; }
    }
}
