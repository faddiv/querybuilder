using System.Collections.Generic;

namespace SqlKata.VisitorCompilers
{
    public class MySqlVisitorCompiler : VisitorCompiler
    {
        public IEnumerable<char> CompileLimit(SqlResult ctx)
        {
            throw new System.NotImplementedException();
        }
    }
}
