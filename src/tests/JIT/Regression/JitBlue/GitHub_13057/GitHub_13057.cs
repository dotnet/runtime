// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using Xunit;

public class Program
{
    const int Pass = 100;
    const int Fail = -1;

    struct RetSt2 {
        public Object _key;
        public Object _value;

        public RetSt2(Object value)
        {
            _key = "f";
            _value = value;
        }
    }

    class Test {
        private Object lockObject;

        internal Test(Object reader)
        {
            lockObject = reader;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        Object FirstValue()
        {
            return "FirstValue";
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        Object SecondValue()
        {
            return "SecondValue";
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public RetSt2 foo(int d) {
            Object value = null;
            lock (lockObject)
            {
                lock (lockObject)
                {
                    if (d == -1) {
                        value = FirstValue();
                    } else {
                        value = SecondValue();
                    }
                }
            }
            return new RetSt2(value);
        }
    }



    [Fact]
    public static int TestEntryPoint()
    {
        RetSt2 r = new Test("Lock").foo(-1);
        Console.WriteLine("r._key: " + r._key);
        Console.WriteLine("r._value: " + r._value);

        r = new Test("Lock").foo(-2);
        Console.WriteLine("r._key: " + r._key);
        Console.WriteLine("r._value: " + r._value);

        return Pass;
    }
}
