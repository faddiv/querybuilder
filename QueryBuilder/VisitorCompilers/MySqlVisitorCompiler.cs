using SqlKata.Compilers;

namespace SqlKata.VisitorCompilers
{
    public class MySqlVisitorCompiler : VisitorCompiler
    {
        public override string EngineCode => EngineCodes.MySql;
    }
}
