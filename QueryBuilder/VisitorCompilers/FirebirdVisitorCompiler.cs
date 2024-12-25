using SqlKata.Compilers;

namespace SqlKata.VisitorCompilers
{
    public class FirebirdVisitorCompiler : VisitorCompiler
    {
        public override string EngineCode => EngineCodes.Firebird;

        protected override void AppendWrapped(string name, SqlResultBuilder builder)
        {
            base.AppendWrapped(name.ToUpperInvariant(), builder);
        }
    }
}
