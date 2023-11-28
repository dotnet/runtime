// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

using Xunit;

namespace LeaveAbstractMethodsNulInVTable
{
    interface IDefault
    {
        public string Method1() => "Interface Method1";

        public string Method2() => "Interface Method2";
    }

    abstract class ClassA : IDefault
    {
        virtual public string Method1() => "ClassA Method1";
    }

    class ClassB : ClassA
    {
        virtual public string Method2() => "ClassB Method2";
    }

    public class Program
    {
        [Fact]
        public static void TestEntryPoint()
        {
            IDefault c = new ClassB();

            string s1 = c.Method1();
            Assert.Equal("ClassA Method1", s1);

            string s2 = c.Method2();
            Assert.Equal("ClassB Method2", s2);
        }
    }

}
