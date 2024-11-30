using System;
using System.Collections.Generic;
using System.Linq;
using SqlKata.Compilers;
using SqlKata.VisitorCompilers;

namespace SqlKata.Tests.Infrastructure;

public class TestVisitorCompilersContainer
{
    private static class Messages
    {
        public const string ERR_INVALID_ENGINECODE = "Engine code '{0}' is not valid";
        public const string ERR_INVALID_ENGINECODES = "Invalid engine codes supplied '{0}'";
    }

    protected readonly IDictionary<string, VisitorCompiler> Compilers = new Dictionary<string, VisitorCompiler>
    {
        [EngineCodes.Firebird] = new FirebirdVisitorCompiler(),
        [EngineCodes.MySql] = new MySqlVisitorCompiler(),
        [EngineCodes.Oracle] = new OracleVisitorCompiler(),
        [EngineCodes.PostgreSql] = new PostgresVisitorCompiler(),
        [EngineCodes.Sqlite] = new SqliteVisitorCompiler(),
        [EngineCodes.SqlServer] = new SqlServerVisitorCompiler()
        {
            UseLegacyPagination = true
        }
    };

    public IEnumerable<string> KnownEngineCodes
    {
        get { return Compilers.Select(s => s.Key); }
    }

    /// <summary>
    /// Returns a <see cref="Compiler"/> instance for the given engine code
    /// </summary>
    /// <param name="engineCode"></param>
    /// <returns></returns>
    public VisitorCompiler Get(string engineCode)
    {
        if (Compilers.TryGetValue(engineCode, out var compiler))
        {
            return compiler;
        }

        throw new InvalidOperationException(string.Format(Messages.ERR_INVALID_ENGINECODE, engineCode));
    }

    /// <summary>
    /// Convenience method <seealso cref="Get"/>
    /// </summary>
    /// <remarks>Does not validate generic type against engine code before cast</remarks>
    /// <typeparam name="TCompiler"></typeparam>
    /// <param name="engineCode"></param>
    /// <returns></returns>
    public TCompiler Get<TCompiler>(string engineCode) where TCompiler : VisitorCompiler
    {
        return (TCompiler)Get(engineCode);
    }

    /// <summary>
    /// Compiles the <see cref="Query"/> against the given engine code
    /// </summary>
    /// <param name="engineCode"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    public SqlResult CompileFor(string engineCode, Query query)
    {
        var compiler = Get(engineCode);
        return compiler.Compile(query);
    }

    /// <summary>
    /// Compiles the <see cref="Query"/> against the given engine codes
    /// </summary>
    /// <param name="engineCodes"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    public TestSqlResultContainer Compile(IEnumerable<string> engineCodes, Query query)
    {
        var codes = engineCodes.ToList();

        var results = Compilers
            .Where(w => codes.Contains(w.Key))
            .ToDictionary(k => k.Key, v => v.Value.Compile(query.Clone()));

        if (results.Count != codes.Count)
        {
            var missingCodes = codes.Where(w => Compilers.All(a => a.Key != w));
            var templateArg = string.Join(", ", missingCodes);
            throw new InvalidOperationException(string.Format(Messages.ERR_INVALID_ENGINECODES, templateArg));
        }

        return new TestSqlResultContainer(results);
    }

    /// <summary>
    /// Compiles the <see cref="Query"/> against all <see cref="Compiler"/>s
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public TestSqlResultContainer Compile(Query query)
    {
        var resultKeyValues = Compilers
            .ToDictionary(k => k.Key, v => v.Value.Compile(query.Clone()));
        return new TestSqlResultContainer(resultKeyValues);
    }
}
