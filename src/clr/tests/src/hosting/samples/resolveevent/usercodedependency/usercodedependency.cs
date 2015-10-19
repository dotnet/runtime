using System;
using System.Collections.Generic;
using System.Text;

namespace UserCodeDependency
{
    public class UserCodeDependencyClass
    {
        static public void InverseClick(int x, int y)
        {
            Console.WriteLine("[Second User Event Handler] Event called with " + x + ":" + y);
        }

    }
}
