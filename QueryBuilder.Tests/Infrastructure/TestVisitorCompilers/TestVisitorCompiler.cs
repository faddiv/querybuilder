using SqlKata.VisitorCompilers;

namespace SqlKata.Tests.Infrastructure.TestVisitorCompilers;

/// <summary>
/// A test class to expose private methods
/// </summary>
class TestVisitorCompiler : VisitorCompiler
{
    public override string EngineCode { get; } = "generic";

}
