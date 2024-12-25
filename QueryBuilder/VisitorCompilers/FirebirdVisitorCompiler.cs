using SqlKata.Compilers;

namespace SqlKata.VisitorCompilers
{
    public class FirebirdVisitorCompiler : VisitorCompiler
    {
        public override string EngineCode => EngineCodes.Firebird;
    }
}
