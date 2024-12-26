using System;
using System.Collections.Generic;
using System.Linq;
using SqlKata.VisitorCompilers.Helpers;

namespace SqlKata.VisitorCompilers
{
    public abstract class VisitorCompiler
    {
        private HashSet<string> userOperators;

        protected virtual string ParameterPlaceholder => "?";

        protected virtual string EscapeCharacter => "\\";

        protected virtual string OpeningIdentifier => "\"";

        protected virtual string ClosingIdentifier => "\"";

        protected virtual string ParameterPrefix => "@p";

        protected virtual string ColumnAsKeyword => "AS";

        protected virtual string TableAsKeyword => "AS";

        /// <summary>
        /// If true the compiler will remove the SELECT clause for the query used inside WHERE EXISTS
        /// </summary>
        public virtual bool OmitSelectInsideExists { get; set; } = true;

        public abstract string EngineCode { get; }

        protected VisitorCompiler()
        {
        }

        public virtual SqlResult Compile(Query query)
        {
            var sqlResultBuilder = new SqlResultBuilder(query, ParameterPrefix);
            CompileAbstractQuery(query, sqlResultBuilder);

            var sqlResult = sqlResultBuilder.PrepareResult(ParameterPlaceholder, EscapeCharacter);

            return sqlResult;
        }

        private SqlResult PrepareResult(SqlResultBuilder sqlResultBuilder)
        {
            throw new System.NotImplementedException();
        }


        protected virtual void CompileAbstractQuery(AbstractQuery abstractQuery, SqlResultBuilder builder)
        {
            switch (abstractQuery)
            {
                case Query query:
                    CompileQuery(query, builder);
                    break;
                case Join join:
                    CompileJoin(join, builder);
                    break;
                default:
                    throw new System.NotImplementedException();
            }
        }

        protected virtual void CompileJoin(Join join, SqlResultBuilder builder)
        {
            builder.Append(" ");
            builder.Append(join.Type);
            builder.Append(" ");
            var abstractFrom = join.GetOneComponent<AbstractFrom>(SqlComponents.From, EngineCode);
            if (abstractFrom == null)
            {
                throw new ApplicationException("Invalid join expression");
            }

            CompileAbstractFrom(abstractFrom, builder);
            if (join.HasComponent(SqlComponents.Where, EngineCode))
            {
                var abstractConditions = join.GetComponents<AbstractCondition>(SqlComponents.Where, EngineCode);
                builder.Append(" ON ");
                CompileConditions(abstractConditions, builder);
            }
        }

        protected virtual void CompileQuery(Query query, SqlResultBuilder builder)
        {
            switch (query.Method)
            {
                case SqlComponents.Select:
                    CompileSelect(query, builder);
                    break;
                default:
                    throw new System.NotImplementedException();
            }
        }

        protected virtual void CompileSelect(Query query, SqlResultBuilder builder)
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

            if (query.HasComponent(SqlComponents.GroupBy, EngineCode))
            {
                var groupBys = query.GetComponents<AbstractColumn>(SqlComponents.GroupBy, EngineCode);
                CompileGroupBys(groupBys, builder);
            }

            if (!query.HasComponent(SqlComponents.Having, EngineCode))
            {
                var havings = query.GetComponents<AbstractCondition>(SqlComponents.Having, EngineCode);
                CompileHavings(havings, builder);
            }

            if (query.HasComponent(SqlComponents.OrderBy, EngineCode))
            {
                var orderBys = query.GetComponents<AbstractOrderBy>(SqlComponents.OrderBy, EngineCode);
                CompileAbstractOrderBys(orderBys, builder);
            }

            if (query.HasComponent(SqlComponents.Limit, EngineCode))
            {
                var limit = query.GetOneComponent<LimitClause>(SqlComponents.Limit, EngineCode);
                CompileLimitClause(limit, builder);
            }

            if (query.HasComponent(SqlComponents.Offset, EngineCode))
            {
                var offset = query.GetOneComponent<OffsetClause>(SqlComponents.Offset, EngineCode);
                CompileOffsetClause(offset, builder);
            }
        }

        protected virtual void CompileJoins(List<BaseJoin> baseJoins, SqlResultBuilder builder)
        {
            builder.StartSection("");
            foreach (var baseJoin in baseJoins)
            {
                CompileJoin(baseJoin.Join, builder);
            }

            builder.EndSection();
        }

        protected virtual void CompileCteList(List<AbstractFrom> cteList, SqlResultBuilder builder)
        {
            foreach (var abstractFrom in cteList)
            {
                CompileCte(abstractFrom, builder);
            }
        }

        protected virtual void CompileCte(AbstractFrom abstractFrom, SqlResultBuilder builder)
        {
            builder.Append("WITH ");
            AppendWrapped(abstractFrom.Alias, builder);
            builder.Append(" AS (");
            switch (abstractFrom)
            {
                case QueryFromClause queryFrom:
                    CompileAbstractQuery(queryFrom.Query, builder);
                    break;
                default:
                    throw new System.NotImplementedException();
            }

            builder.Append(")\n");
        }

        protected virtual void CompileHavings(List<AbstractCondition> havings, SqlResultBuilder builder)
        {
            builder.Append(" HAVING ");
            builder.StartSection(" AND ");
            foreach (var having in havings)
            {
                CompileCondition(having, builder);
            }

            builder.EndSection();
        }

        protected virtual void CompileGroupBys(List<AbstractColumn> groupBys, SqlResultBuilder builder)
        {
            builder.Append(" GROUP BY ");
            CompileColumns(groupBys, builder);
        }

        protected virtual void CompileColumns(List<AbstractColumn> columns, SqlResultBuilder builder)
        {
            builder.StartSection(", ");
            foreach (var column in columns)
            {
                CompileAbstractColumn(column, builder);
            }

            builder.EndSection();
        }

        protected virtual void CompileLimitClause(LimitClause limit, SqlResultBuilder builder)
        {
            if (!limit.HasLimit())
            {
                return;
            }

            builder.Append(" LIMIT ");
            builder.AddValue(limit.Limit);
        }

        protected virtual void CompileOffsetClause(OffsetClause offset, SqlResultBuilder builder)
        {
            if (!offset.HasOffset())
            {
                return;
            }

            builder.Append(" OFFSET ");
            builder.AddValue(offset.Offset);
        }

        protected virtual void CompileAbstractOrderBys(List<AbstractOrderBy> abstractOrderBys, SqlResultBuilder builder)
        {
            builder.Append(" ORDER BY ");
            builder.StartSection(", ");
            foreach (var abstractOrderBy in abstractOrderBys)
            {
                switch (abstractOrderBy)
                {
                    case OrderBy orderBy:
                        CompileOrderBy(orderBy, builder);
                        break;
                    default:
                        throw new System.NotImplementedException();
                }
            }

            builder.EndSection();
        }

        private void CompileOrderBy(OrderBy orderBy, SqlResultBuilder builder)
        {
            builder.AppendSeparator();
            AppendWrapped(orderBy.Column, builder);
            if (!orderBy.Ascending)
            {
                builder.Append(" DESC");
            }
        }

        protected void CompileConditions(
            IEnumerable<AbstractCondition> conditions,
            SqlResultBuilder builder)
        {
            builder.StartSection(" AND ");
            foreach (var condition in conditions)
            {
                CompileCondition(condition, builder);
            }

            builder.EndSection();
        }

        protected virtual void CompileCondition(AbstractCondition condition, SqlResultBuilder builder)
        {
            builder.AppendSeparator();
            switch (condition)
            {
                case BasicCondition basicCondition:
                    CompileBasicCondition(basicCondition, builder);
                    break;
                case RawCondition rawCondition:
                    CompileRawCondition(rawCondition, builder);
                    break;
                case InCondition inCondition:
                    CompileInCondition(inCondition, builder);
                    break;
                case TwoColumnsCondition twoColumnsCondition:
                    CompileTwoColumnsCondition(twoColumnsCondition, builder);
                    break;
                default:
                    throw new System.NotImplementedException();
            }
        }

        private void CompileTwoColumnsCondition(TwoColumnsCondition twoColumnsCondition, SqlResultBuilder builder)
        {
            AppendWrapped(twoColumnsCondition.First, builder);
            builder.Append(" ");
            builder.Append(twoColumnsCondition.Operator);
            builder.Append(" ");
            AppendWrapped(twoColumnsCondition.Second, builder);
        }

        private void CompileInCondition(InCondition inCondition, SqlResultBuilder builder)
        {
            AppendWrapped(inCondition.Column, builder);
            builder.Append(" IN (");
            builder.StartSection(", ");
            foreach (var value in inCondition.Values)
            {
                builder.AppendSeparator();
                builder.AddValue(value);
            }

            builder.Append(")");
            builder.EndSection();
        }

        protected virtual void CompileRawCondition(RawCondition rawCondition, SqlResultBuilder builder)
        {
            builder.AppendRaw(rawCondition.Expression, rawCondition.Bindings);
        }

        protected virtual void CompileBasicCondition(
            BasicCondition basicCondition,
            SqlResultBuilder builder)
        {
            AppendWrapped(basicCondition.Column, builder);
            builder.Append(" ");
            builder.Append(basicCondition.Operator);
            builder.Append(" ");
            builder.AddValue(basicCondition.Value);
        }

        protected virtual void CompileAbstractFrom(AbstractFrom abstractFrom, SqlResultBuilder builder)
        {
            switch (abstractFrom)
            {
                case FromClause fromClause:
                    CompileFromClause(fromClause, builder);
                    break;
                case QueryFromClause queryFromClause:
                    CompileQueryFromClause(queryFromClause, builder);
                    break;
                case RawFromClause rawFromClause:
                    CompileRawFromClause(rawFromClause, builder);
                    break;
                case AdHocTableFromClause adhocTableFromClause:
                    CompileAdHocTableFromClause(adhocTableFromClause, builder);
                    break;
                default:
                    throw new ArgumentException($"Invalid AbstractForm type: {abstractFrom.GetType()}");
            }
        }

        protected virtual void CompileAdHocTableFromClause(
            AdHocTableFromClause adhocTableFromClause,
            SqlResultBuilder builder)
        {
            throw new System.NotImplementedException();
        }

        protected virtual void CompileRawFromClause(RawFromClause rawFromClause, SqlResultBuilder builder)
        {
            throw new System.NotImplementedException();
        }

        protected virtual void CompileQueryFromClause(
            QueryFromClause queryFromClause,
            SqlResultBuilder builder)
        {
            throw new System.NotImplementedException();
        }

        protected virtual void CompileFromClause(FromClause fromClause, SqlResultBuilder builder)
        {
            AppendWrapped(fromClause.Table, builder);
        }

        protected virtual void CompileAbstractColumn(AbstractColumn column, SqlResultBuilder builder)
        {
            switch (column)
            {
                case QueryColumn qc:
                    CompileQueryColumn(qc, builder);
                    break;
                case AggregatedColumn ac:
                    CompileAggregatedColumn(ac, builder);
                    break;
                case RawColumn rc:
                    CompileRawColumn(rc, builder);
                    break;
                case Column col:
                    CompileColumn(col, builder);
                    break;
                default:
                    throw new System.NotImplementedException();
            }
        }

        private void CompileColumn(Column column, SqlResultBuilder builder)
        {
            builder.AppendSeparator();
            AppendWrapped(column.Name, builder);
        }

        private void CompileRawColumn(RawColumn rawColumn, SqlResultBuilder builder)
        {
            builder.AppendSeparator();
            builder.AppendRaw(rawColumn.Expression, rawColumn.Bindings);
        }

        protected virtual void CompileAggregatedColumn(
            AggregatedColumn aggregatedColumn,
            SqlResultBuilder builder)
        {
            builder.AppendSeparator();
            builder.Append(aggregatedColumn.Aggregate.ToUpperInvariant());
            builder.Append("(");
            builder.StartSection("");
            CompileAbstractColumn(aggregatedColumn.Column, builder);
            builder.EndSection();
            builder.Append(")");
        }

        private void CompileQueryColumn(QueryColumn queryColumn, SqlResultBuilder builder)
        {
            throw new System.NotImplementedException();
        }

        public VisitorCompiler Whitelist(params string[] operators)
        {
            foreach (var op in operators)
            {
                userOperators.Add(op);
            }

            return this;
        }

        protected virtual void AppendWrapped(string name, SqlResultBuilder builder)
        {
            if (name.Contains('.'))
            {
                var index = name.IndexOf('.');
                builder.Append(OpeningIdentifier);
                builder.Append(name, 0, index);
                builder.Append(ClosingIdentifier);
                builder.Append(".");
                var startNext = index + 1;
                if (string.CompareOrdinal(name, startNext, "*", 0, 1) != 0)
                {
                    builder.Append(OpeningIdentifier);
                    builder.Append(name, startNext, name.Length - startNext);
                    builder.Append(ClosingIdentifier);
                }
                else
                {
                    builder.Append("*");
                }
            }
            else
            {
                builder.Append(OpeningIdentifier);
                builder.Append(name);
                builder.Append(ClosingIdentifier);
            }
        }
    }
}
