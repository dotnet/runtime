// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Test removal of a dead struct assignment when the assignment is "internal"
// i.e., is not a direct child of a statement node.
// In this example, the statement is STMT(COMMA(CALL HELPER.CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE, ASG)).

namespace GitHub_24253
{
    struct TestStruct
    {
        public static readonly TestStruct ZeroStruct = new TestStruct(0);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public TestStruct(int i)
        {
            this.i = i;
            this.j = 5;
            this.k = 5;
            this.l = 5;
        }

        int i;
        int j;
        short k;
        short l;
    }

    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            GetStruct(1);
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static TestStruct GetStruct(int i)
        {
            // This is the dead assignment that is causing the bug assert.
            TestStruct result = TestStruct.ZeroStruct;
            try
            {
                result = new TestStruct(i);
            }
            catch
            {
                throw new ArgumentException();
            }
            return result;
        }
    }
}
