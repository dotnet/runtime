// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class JavaScriptTests
    {
        [Fact]
        public static void TestLocalsInit()
        {
            object[] arg = new object[] {"1","2"};
            //Assert.Equal(123456, Runtime.DoSomething("test", arg));
            for(int i = 0; i < 1500; i++)
            {
                Console.WriteLine("I " + i);
                Assert.Equal(0, Runtime.DoNothing());
            }
        }
    }
}
