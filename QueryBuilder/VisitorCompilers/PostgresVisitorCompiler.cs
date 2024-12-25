using SqlKata.Compilers;

namespace SqlKata.VisitorCompilers
{
    public class PostgresVisitorCompiler : VisitorCompiler
    {
        public override string EngineCode => EngineCodes.PostgreSql;
    }
}
