using SqlKata.Tests.Infrastructure.TestVisitorCompilers;
using SqlKata.VisitorCompilers;

namespace SqlKata.Tests.Infrastructure;

public abstract class TestSupport2
{
    protected SqlResult CompileForGeneric(Query query, Func<VisitorCompiler, VisitorCompiler> configuration = null)
    {
        return CompileFor(EngineCodes.Generic, query, configuration);
    }

    protected SqlResult CompileFor(string engine, Query query, Func<VisitorCompiler, VisitorCompiler> configuration = null)
    {
        var compiler = CreateCompiler(engine);
        if (configuration != null)
        {
            compiler = configuration(compiler);
        }

        return compiler.Compile(query);
    }

    protected SqlResult CompileFor(string engine, Query query, Action<VisitorCompiler> configuration)
    {
        return CompileFor(engine, query, compiler =>
        {
            configuration(compiler);
            return compiler;
        });
    }

    protected VisitorCompiler CreateCompiler(string engine, bool? useLegacyPagination = null)
    {
        return engine switch
        {
            EngineCodes.Firebird => new FirebirdVisitorCompiler(),
            EngineCodes.MySql => new MySqlVisitorCompiler(),
            EngineCodes.Oracle => new OracleVisitorCompiler
            {
                UseLegacyPagination = useLegacyPagination ?? false
            },
            EngineCodes.PostgreSql => new PostgresVisitorCompiler(),
            EngineCodes.Sqlite => new SqliteVisitorCompiler(),
            EngineCodes.SqlServer => new SqlServerVisitorCompiler
            {
                UseLegacyPagination = useLegacyPagination ?? true
            },
            EngineCodes.Generic => new TestVisitorCompiler(),
            _ => throw new ArgumentException($"Unsupported engine type: {engine}", nameof(engine)),
        };
    }
}
