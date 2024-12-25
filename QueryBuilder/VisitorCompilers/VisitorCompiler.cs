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
            var sqlResultBuilder = new SqlResultBuilder(query);
            CompileRaw(query, sqlResultBuilder);

            var sqlResult = sqlResultBuilder.PrepareResult(ParameterPlaceholder, EscapeCharacter);

            return sqlResult;
        }

        private SqlResult PrepareResult(SqlResultBuilder sqlResultBuilder)
        {
            throw new System.NotImplementedException();
        }


        private void CompileRaw(AbstractQuery query, SqlResultBuilder builder)
        {
            switch (query)
            {
                case Query q:
                    CompileQuery(q, builder);
                    break;
                case Join j:
                    CompileJoin(j, builder);
                    break;
                default:
                    throw new System.NotImplementedException();
            }
        }

        protected virtual void CompileJoin(Join join, SqlResultBuilder builder)
        {
            throw new System.NotImplementedException();
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
            builder.Append("SELECT ");
            builder.StartSection(", ");
            var columns = query.GetComponents<AbstractColumn>(SqlComponents.Select, EngineCode);
            foreach (var column in columns)
            {
                CompileAbstractColumn(column, query, builder);
            }

            builder.EndSection();

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
        }

        protected void CompileConditions(
            IEnumerable<AbstractCondition> conditions,
            Query query,
            SqlResultBuilder builder)
        {
            builder.Append(" WHERE ");
            builder.StartSection(" AND ");
            foreach (var condition in conditions)
            {
                CompileCondition(condition, query, builder);
            }

            builder.EndSection();
        }

        protected virtual void CompileCondition(AbstractCondition condition, Query query, SqlResultBuilder builder)
        {
            builder.AppendSeparator();
            switch (condition)
            {
                case BasicCondition basicCondition:
                    CompileBasicCondition(basicCondition, query, builder);
                    break;
                case RawCondition rawCondition:
                    CompileRawCondition(rawCondition, query, builder);
                    break;
                default:
                    throw new System.NotImplementedException();
            }
        }

        protected virtual void CompileRawCondition(RawCondition rawCondition, Query query, SqlResultBuilder builder)
        {
            builder.AppendRaw(rawCondition.Expression, rawCondition.Bindings);
        }

        protected virtual void CompileBasicCondition(
            BasicCondition basicCondition,
            Query query,
            SqlResultBuilder builder)
        {
            AppendWrapped(basicCondition.Column, builder);
            builder.Append(" ");
            builder.Append(basicCondition.Operator);
            builder.Append(" ");
            builder.AddValue(basicCondition.Value, ParameterPrefix);
        }

        protected virtual void CompileFrom(AbstractFrom abstractFrom, Query query, SqlResultBuilder builder)
        {
            if (abstractFrom == null)
            {
                return;
            }

            builder.Append(" FROM ");

            switch (abstractFrom)
            {
                case FromClause fromClause:
                    CompileFromClause(fromClause, query, builder);
                    break;
                case QueryFromClause queryFromClause:
                    CompileQueryFromClause(queryFromClause, query, builder);
                    break;
                case RawFromClause rawFromClause:
                    CompileRawFromClause(rawFromClause, query, builder);
                    break;
                case AdHocTableFromClause adhocTableFromClause:
                    CompileAdHocTableFromClause(adhocTableFromClause, query, builder);
                    break;
                default:
                    throw new ArgumentException($"Invalid AbstractForm type: {abstractFrom.GetType()}");
            }
        }

        protected virtual void CompileAdHocTableFromClause(
            AdHocTableFromClause adhocTableFromClause,
            Query query,
            SqlResultBuilder builder)
        {
            throw new System.NotImplementedException();
        }

        protected virtual void CompileRawFromClause(RawFromClause rawFromClause, Query query, SqlResultBuilder builder)
        {
            throw new System.NotImplementedException();
        }

        protected virtual void CompileQueryFromClause(
            QueryFromClause queryFromClause,
            Query query,
            SqlResultBuilder builder)
        {
            throw new System.NotImplementedException();
        }

        protected virtual void CompileFromClause(FromClause fromClause, Query query, SqlResultBuilder builder)
        {
            AppendWrapped(fromClause.Table, builder);
        }

        protected virtual void CompileAbstractColumn(AbstractColumn column, Query query, SqlResultBuilder builder)
        {
            switch (column)
            {
                case QueryColumn qc:
                    CompileQueryColumn(qc, query, builder);
                    break;
                case AggregatedColumn ac:
                    CompileAggregatedColumn(ac, query, builder);
                    break;
                case RawColumn rc:
                    CompileRawColumn(rc, query, builder);
                    break;
                case Column col:
                    CompileColumn(col, query, builder);
                    break;
                default:
                    throw new System.NotImplementedException();
            }
        }

        private void CompileColumn(Column column, Query query, SqlResultBuilder builder)
        {
            builder.AppendSeparator();
            AppendWrapped(column.Name, builder);
        }

        protected virtual void AppendWrapped(string name, SqlResultBuilder builder)
        {
            builder.Append(OpeningIdentifier);
            builder.Append(name);
            builder.Append(ClosingIdentifier);
        }

        private void CompileRawColumn(RawColumn rawColumn, Query query, SqlResultBuilder builder)
        {
            throw new System.NotImplementedException();
        }

        protected virtual void CompileAggregatedColumn(
            AggregatedColumn aggregatedColumn,
            Query query,
            SqlResultBuilder builder)
        {
            throw new System.NotImplementedException();
        }

        private void CompileQueryColumn(QueryColumn queryColumn, Query query, SqlResultBuilder builder)
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
    }
}
