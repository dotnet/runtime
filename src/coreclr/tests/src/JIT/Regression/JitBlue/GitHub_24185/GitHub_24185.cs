// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

// The test shows recursive assertion propogation in one statement.

namespace GitHub_24185
{
    public class Program
    {
        static int Main(string[] args)
        {
            try
            {
                throw new AggregateException(new Exception("A random exception1"), new Exception("A random exception2"));
            }
            catch (Exception e)
            {
                // Each expression in this condition checks that `e` is not null and checks its type.
                // This information should be calculated once and propogated by assertion propogation.
                if (!(e is AggregateException) ||                    
                    !((((AggregateException)e).InnerExceptions[0] is ArgumentException) 
                    || ((AggregateException)e).InnerExceptions[0] is AggregateException))
                {
                    return 100;
                }

            }
            return 0;
        }
    }
}
