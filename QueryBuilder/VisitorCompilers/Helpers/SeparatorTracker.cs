using System;
using System.Collections.Generic;
using System.Text;

namespace SqlKata.VisitorCompilers.Helpers
{
    internal class SeparatorTracker
    {
        private readonly Stack<string> stackedSeparators;
        private readonly Stack<bool> stackedFirstSeparators;
        private string separator;
        private bool firstSeparator;

        public SeparatorTracker()
        {
            stackedSeparators = new Stack<string>();
            stackedFirstSeparators = new Stack<bool>();
        }

        public string Current
        {
            get
            {
                if (separator == null)
                {
                    throw new InvalidOperationException("No separator section is defined.");
                }
                return separator;
            }
        }


        public void StartSection(string separatorValue)
        {
            stackedSeparators.Push(separator);
            stackedFirstSeparators.Push(firstSeparator);
            separator = separatorValue;
            firstSeparator = true;
        }

        public void EndSection()
        {
            if (stackedSeparators.Count > 0)
            {
                separator = stackedSeparators.Pop();
                firstSeparator = stackedFirstSeparators.Pop();
            }
            else
            {
                separator = null;
                firstSeparator = false;
            }
        }

        public void AppendSeparator(StringBuilder builder)
        {
            if (firstSeparator)
            {
                firstSeparator = false;
                return;
            }

            builder.Append(separator);
        }
    }
}
