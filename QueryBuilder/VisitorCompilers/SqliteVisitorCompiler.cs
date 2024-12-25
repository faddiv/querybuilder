using SqlKata.Compilers;

namespace SqlKata.VisitorCompilers
{
    public class SqliteVisitorCompiler : VisitorCompiler
    {
        public override string EngineCode => EngineCodes.Sqlite;
    }
}
