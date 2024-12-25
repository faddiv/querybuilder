using SqlKata.Compilers;

namespace SqlKata.VisitorCompilers
{
    public class SqlServerVisitorCompiler : VisitorCompiler
    {
        public override string EngineCode => EngineCodes.SqlServer;
        protected override void CompileSelect(Query query, SqlResultBuilder builder)
        {
            builder.Append("SELECT ");
            var columns = query.GetComponents<AbstractColumn>(SqlComponents.Select, EngineCode);
            CompileColumns(columns, query, builder);

            if (query.HasComponent(SqlComponents.From))
            {
                var from = query.GetOneComponent<AbstractFrom>(SqlComponents.From, EngineCode);
                CompileFrom(from, query, builder);
            }


            if (query.HasComponent(SqlComponents.Where, EngineCode))
            {
                var conditions = query.GetComponents<AbstractCondition>(SqlComponents.Where, EngineCode);
                CompileConditions(conditions, query, builder);
            }

            if (query.HasComponent(SqlComponents.OrderBy, EngineCode))
            {
                var orderBys = query.GetComponents<AbstractOrderBy>(SqlComponents.OrderBy, EngineCode);
                CompileAbstractOrderBys(orderBys, query, builder);
            }

            if (query.HasComponent(SqlComponents.Offset, EngineCode))
            {
                var offset = query.GetOneComponent<OffsetClause>(SqlComponents.Offset, EngineCode);
                CompileOffsetClause(offset, query, builder);
            }

            if (query.HasComponent(SqlComponents.Limit, EngineCode))
            {
                var limit = query.GetOneComponent<LimitClause>(SqlComponents.Limit, EngineCode);
                CompileLimitClause(limit, query, builder);
            }
        }

        protected override void CompileLimitClause(LimitClause limit, Query query, SqlResultBuilder builder)
        {
            if (!limit.HasLimit())
            {
                return;
            }

            builder.Append(" FETCH NEXT ");
            builder.AddValue(limit.Limit);
            builder.Append(" ROWS ONLY");
        }

        protected override void CompileOffsetClause(OffsetClause offset, Query query, SqlResultBuilder builder)
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
