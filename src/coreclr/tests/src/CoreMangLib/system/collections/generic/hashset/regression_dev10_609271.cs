using System;
using System.Collections.Generic;

namespace Sample
{
    class Program
    {
        static int Main(string[] args)
        {
            int ret = 100;
            HashSet<object> hashset = new HashSet<object>();
            
            ISet<object> iset = (hashset as ISet<object>);

            if (iset == null) ret = 0;

            return ret;
        }
    }
}

 



