using SqlKata.Tests.Infrastructure.TestCompilers;
using Compiler2 = SqlKata.Compilers.Compiler;

namespace SqlKata.Tests.Infrastructure
{
    public abstract class TestSupport2
    {
        protected SqlResult CompileForGeneric(Query query, Func<Compiler2, Compiler2> configuration = null)
        {
            return CompileFor(EngineCodes.Generic, query, configuration);
        }

        protected SqlResult CompileFor(string engine, Query query, Func<Compiler2, Compiler2> configuration = null)
        {
            var compiler = CreateCompiler(engine);
            if (configuration != null)
            {
                compiler = configuration(compiler);
            }

            return compiler.Compile(query);
        }

        protected SqlResult CompileFor(string engine, Query query, Action<Compiler2> configuration)
        {
            return CompileFor(engine, query, compiler =>
            {
                configuration(compiler);
                return compiler;
            });
        }

        protected Compiler2 CreateCompiler(string engine, bool? useLegacyPagination = null)
        {
            return engine switch
            {
                EngineCodes.Firebird => new FirebirdCompiler(),
                EngineCodes.MySql => new MySqlCompiler(),
                EngineCodes.Oracle => new OracleCompiler
                {
                    UseLegacyPagination = useLegacyPagination ?? false
                },
                EngineCodes.PostgreSql => new PostgresCompiler(),
                EngineCodes.Sqlite => new SqliteCompiler(),
                EngineCodes.SqlServer => new SqlServerCompiler
                {
                    UseLegacyPagination = useLegacyPagination ?? true
                },
                EngineCodes.Generic => new TestCompiler(),
                _ => throw new ArgumentException($"Unsupported engine type: {engine}", nameof(engine)),
            };
        }
    }
}
