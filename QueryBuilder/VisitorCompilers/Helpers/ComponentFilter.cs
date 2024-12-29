using System.Collections;
using System.Collections.Generic;

namespace SqlKata.VisitorCompilers.Helpers
{
    public struct ComponentFilter<TComponent>
        : IEnumerable<TComponent>,
            IEnumerator<TComponent>
        where TComponent : AbstractClause
    {
        private readonly string component;
        private readonly string engine;
        private readonly List<AbstractClause> clauses;
        private int index;

        public ComponentFilter(string component, string engine, List<AbstractClause> clauses)
        {
            this.component = component;
            this.engine = engine;
            this.clauses = clauses;
            index = -1;
        }

        public bool SeekFirstElement()
        {
            index = -1;
            if (!MoveNext())
            {
                return false;
            }

            index--;
            return true;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            index++;
            while (index < clauses.Count)
            {
                if (IsCurrentCorrect())
                {
                    return true;
                }

                index++;
            }

            index = -1;
            return false;
        }

        public void Reset()
        {
            index = -1;
            MoveNext();
        }

        public TComponent Current =>
            IsCurrentCorrect()
                ? (TComponent)clauses[index]
                : null;

        object IEnumerator.Current => Current;

        public ComponentFilter<TComponent> GetEnumerator()
        {
            return this;
        }

        IEnumerator<TComponent> IEnumerable<TComponent>.GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private bool IsCurrentCorrect()
        {
            if (index < 0 && index >= clauses.Count)
            {
                return false;
            }

            var abstractClause = clauses[index];
            if (abstractClause.Component != component)
            {
                return false;
            }

            return abstractClause.Engine == null ||
                   abstractClause.Engine == engine &&
                   abstractClause is TComponent;
        }
    }
}
