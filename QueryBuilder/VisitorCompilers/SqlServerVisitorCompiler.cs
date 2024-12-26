using SqlKata.Compilers;

namespace SqlKata.VisitorCompilers
{
    public class SqlServerVisitorCompiler : VisitorCompiler
    {
        public override string EngineCode => EngineCodes.SqlServer;

        protected override void CompileSelect(Query query, SqlResultBuilder builder)
        {
            if (query.HasComponent(SqlComponents.Cte, EngineCode))
            {
                var cteList = query.GetComponents<AbstractFrom>(SqlComponents.Cte, EngineCode);
                CompileCteList(cteList, builder);
            }

            builder.Append("SELECT ");
            var columns = query.GetComponents<AbstractColumn>(SqlComponents.Select, EngineCode);
            CompileColumns(columns, builder);

            if (query.HasComponent(SqlComponents.From))
            {
                var from = query.GetOneComponent<AbstractFrom>(SqlComponents.From, EngineCode);
                builder.Append(" FROM ");
                CompileAbstractFrom(from, builder);
            }

            if (query.HasComponent(SqlComponents.Join, EngineCode))
            {
                var baseJoins = query.GetComponents<BaseJoin>(SqlComponents.Join, EngineCode);
                CompileJoins(baseJoins, builder);
            }

            if (query.HasComponent(SqlComponents.Where, EngineCode))
            {
                var conditions = query.GetComponents<AbstractCondition>(SqlComponents.Where, EngineCode);
                builder.Append(" WHERE ");
                CompileConditions(conditions, builder);
            }

            if (query.HasComponent(SqlComponents.GroupBy))
            {
                var groupBys = query.GetComponents<AbstractColumn>(SqlComponents.GroupBy, EngineCode);
                CompileGroupBys(groupBys, builder);
            }

            if (query.HasComponent(SqlComponents.Having, EngineCode))
            {
                var havings = query.GetComponents<AbstractCondition>(SqlComponents.Having, EngineCode);
                CompileHavings(havings, builder);
            }

            if (query.HasComponent(SqlComponents.OrderBy, EngineCode))
            {
                var orderBys = query.GetComponents<AbstractOrderBy>(SqlComponents.OrderBy, EngineCode);
                CompileAbstractOrderBys(orderBys, builder);
            }

            if (query.HasComponent(SqlComponents.Offset, EngineCode))
            {
                var offset = query.GetOneComponent<OffsetClause>(SqlComponents.Offset, EngineCode);
                CompileOffsetClause(offset, builder);
            }

            if (query.HasComponent(SqlComponents.Limit, EngineCode))
            {
                var limit = query.GetOneComponent<LimitClause>(SqlComponents.Limit, EngineCode);
                CompileLimitClause(limit, builder);
            }
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
