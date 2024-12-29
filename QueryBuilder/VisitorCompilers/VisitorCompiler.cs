using System;
using System.Collections.Generic;
using System.Linq;
using SqlKata.VisitorCompilers.Helpers;

namespace SqlKata.VisitorCompilers
{
    public abstract class VisitorCompiler
    {
        private readonly HashSet<string> userOperators = new HashSet<string>();

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

        public virtual SqlResult Compile(Query query)
        {
            var sqlResultBuilder = new SqlResultBuilder(ParameterPrefix);
            CompileAbstractQuery(query, sqlResultBuilder);

            var sqlResult = sqlResultBuilder.PrepareResult(query, ParameterPlaceholder, EscapeCharacter);

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
            CompileCteListSection(query, builder);

            CompileSelectColumnsSection(query, builder);

            CompileFromSection(query, builder);

            CompileJoinSection(query, builder);

            CompileWhereSection(query, builder);

            CompileGroupBySection(query, builder);

            CompileHavingSection(query, builder);

            CompileOrderBySection(query, builder);

            CompileLimitSection(query, builder);

            CompileOffsetSection(query, builder);
        }

        protected virtual void CompileOffsetSection(Query query, SqlResultBuilder builder)
        {
            if (!builder.TryGetComponent<OffsetClause>(query, SqlComponents.Offset, EngineCode, out var offset))
            {
                return;
            }

            CompileOffsetClause(offset, builder);
        }

        protected virtual void CompileLimitSection(Query query, SqlResultBuilder builder)
        {
            if (!builder.TryGetComponent<LimitClause>(query, SqlComponents.Limit, EngineCode, out var limit))
            {
                return;
            }

            CompileLimitClause(limit, builder);
        }

        protected virtual void CompileOrderBySection(Query query, SqlResultBuilder builder)
        {
            builder.ExecuteOnComponents<AbstractOrderBy, VisitorCompiler>(
                query, SqlComponents.OrderBy, EngineCode, this, (orderBys, compiler, b) =>
                {
                    b.Append(" ORDER BY ");
                    compiler.CompileAbstractOrderBys(orderBys, b);
                });
        }

        protected virtual void CompileHavingSection(Query query, SqlResultBuilder builder)
        {
            builder.ExecuteOnComponents<AbstractCondition, VisitorCompiler>(
                query, SqlComponents.Having, EngineCode, this, (havings, compiler, b) =>
                {
                    b.Append(" HAVING ");
                    compiler.CompileHavings(havings, b);
                });
        }

        protected virtual void CompileGroupBySection(Query query, SqlResultBuilder builder)
        {
            builder.ExecuteOnComponents<AbstractColumn, VisitorCompiler>(
                query, SqlComponents.GroupBy, EngineCode, this, (groupBys, compiler, b) =>
                {
                    b.Append(" GROUP BY ");
                    compiler.CompileGroupBys(groupBys, b);
                });
        }

        protected virtual void CompileWhereSection(Query query, SqlResultBuilder builder)
        {
            builder.ExecuteOnComponents<AbstractCondition, VisitorCompiler>(
                query, SqlComponents.Where, EngineCode, this, (conditions, compiler, b) =>
                {
                    b.Append(" WHERE ");
                    compiler.CompileConditions(conditions, b);
                });
        }

        protected virtual void CompileJoinSection(Query query, SqlResultBuilder builder)
        {
            builder.ExecuteOnComponents<BaseJoin, VisitorCompiler>(
                query, SqlComponents.Join, EngineCode,
                this, (columns, compiler, b) => { compiler.CompileJoins(columns, b); });
        }

        protected virtual void CompileFromSection(Query query, SqlResultBuilder builder)
        {
            if (!builder.TryGetComponent<AbstractFrom>(query, SqlComponents.From, EngineCode, out var from))
            {
                return;
            }

            builder.Append(" FROM ");
            CompileAbstractFrom(from, builder);
        }


        protected virtual void CompileSelectColumnsSection(Query query, SqlResultBuilder builder)
        {
            builder.Append("SELECT ");
            builder.ExecuteOnComponents<AbstractColumn, VisitorCompiler>(
                query, SqlComponents.Select, EngineCode,
                this, (columns, compiler, b) => { compiler.CompileColumns(columns, b); });
        }

        protected virtual void CompileCteListSection(Query query, SqlResultBuilder builder)
        {
            builder.ExecuteOnComponents<AbstractFrom, VisitorCompiler>(
                query, SqlComponents.Cte, EngineCode,
                this, (cteList, compiler, b) => { compiler.CompileCteList(cteList, b); });
        }

        protected virtual void CompileJoins(ComponentFilter<BaseJoin> baseJoins, SqlResultBuilder builder)
        {
            builder.StartSection("");
            foreach (var baseJoin in baseJoins)
            {
                CompileJoin(baseJoin.Join, builder);
            }

            builder.EndSection();
        }

        protected virtual void CompileCteList(ComponentFilter<AbstractFrom> cteList, SqlResultBuilder builder)
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

        protected virtual void CompileHavings(ComponentFilter<AbstractCondition> havings, SqlResultBuilder builder)
        {
            builder.StartSection(" AND ");
            foreach (var having in havings)
            {
                CompileCondition(having, builder);
            }

            builder.EndSection();
        }

        protected virtual void CompileGroupBys(ComponentFilter<AbstractColumn> groupBys, SqlResultBuilder builder)
        {
            CompileColumns(groupBys, builder);
        }

        protected virtual void CompileColumns(ComponentFilter<AbstractColumn> columns, SqlResultBuilder builder)
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

        protected virtual void CompileAbstractOrderBys(
            ComponentFilter<AbstractOrderBy> abstractOrderBys,
            SqlResultBuilder builder)
        {
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

        protected void CompileConditions(
            ComponentFilter<AbstractCondition> conditions,
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
