using SqlKata.Compilers;

namespace SqlKata.VisitorCompilers
{
    public class SqlServerVisitorCompiler : VisitorCompiler
    {
        public override string EngineCode => EngineCodes.SqlServer;

        protected override void CompileSelect(Query query, SqlResultBuilder builder)
        {
            CompileCteListSection(query, builder);

            CompileSelectColumnsSection(query, builder);

            CompileFromSection(query, builder);

            CompileJoinSection(query, builder);

            CompileWhereSection(query, builder);

            CompileGroupBySection(query, builder);

            CompileHavingSection(query, builder);

            CompileOrderBySection(query, builder);

            CompileOffsetSection(query, builder);

            CompileLimitSection(query, builder);
        }

        protected override void CompileLimitClause(LimitClause limit, SqlResultBuilder builder)
        {
            if (!limit.HasLimit())
            {
                return;
            }

            builder.Append(" FETCH NEXT ");
            builder.AddValue(limit.Limit);
            builder.Append(" ROWS ONLY");
        }

        protected override void CompileOffsetClause(OffsetClause offset, SqlResultBuilder builder)
        {
            if (!offset.HasOffset())
            {
                return;
            }

            builder.Append(" OFFSET ");
            builder.AddValue(offset.Offset);
            builder.Append(" ROWS");
        }

        protected override string OpeningIdentifier => "[";
        protected override string ClosingIdentifier => "]";

        public bool UseLegacyPagination { get; set; }
    }
}
