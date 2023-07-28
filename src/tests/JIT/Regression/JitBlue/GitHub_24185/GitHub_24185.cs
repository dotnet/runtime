// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// The test shows recursive assertion propagation in one statement.

namespace GitHub_24185
{
    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                throw new AggregateException(new Exception("A random exception1"), new Exception("A random exception2"));
            }
            catch (Exception e)
            {
                // Each expression in this condition checks that `e` is not null and checks its type.
                // This information should be calculated once and propagated by assertion propagation.
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
