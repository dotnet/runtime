// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class GitHub_24657
{
    struct Wrapper<T>
    {
        public T Val { get; set; }

        public static implicit operator Wrapper<T>(T val) => new Wrapper<T> { Val = val };
    }

    struct TestStruct
    {
        public Wrapper<int> Field1;
        public Wrapper<short> Field2;
        public Wrapper<short> Field3;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public TestStruct(int field1)
        {
            this.Field1 = field1;
            this.Field2 = 0;
            this.Field3 = 0;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnVal = 100;
        var array = new [] { new TestStruct(123), new TestStruct(456) };
        if (array[1].Field1.Val != 456)
        {
            Console.WriteLine("Failed to set value correctly.");
            returnVal = -1;
        }

        array[0].Field3 = 100;

        if (array[1].Field1.Val != 456)
        {
            Console.WriteLine("Value is corrupted.");
            returnVal = -1;
        }

        System.Console.WriteLine(returnVal == 100 ? "Pass" : "Fail");
        return returnVal;
    }
}

