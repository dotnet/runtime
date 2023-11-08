// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* Regression Test for Dev11 bug #95728: LINQ/CLR :: Accessing a static generic field <String> causes CLR to crash with FatalExecutionEngineError
* 
* Comments from bug: FatalExecutionEngineError: The runtime has encountered a fatal error. The address of the error was at 0x71ff5dcd, 
* on thread 0x7f4. The error code is 0xc0000005. This error may be a bug in the CLR or in the unsafe or non-verifiable portions of user 
* code. Common sources of this bug include user marshaling errors for COM-interop or PInvoke, which may corrupt the stack.
*/

using System;
using System.Linq.Expressions;
using Xunit;

namespace StaticFieldBug
{
    public class StubClass<T>
    {
        public StubClass(T value)
        {
            StubClass<T>.StaticField = value;
        }
        public static T StaticField = default(T);
        public static T StaticProperty
        {
            get { return StaticField; }
        }
    }
        
    public class Program
    {
        [Fact]
        public static void TestEntryPoint()
        {
            Foo<string>("Run me to crash LINQ...");
            
            Console.WriteLine("PASS (we didn't crash)!");
        }
        public static void Foo<T>(T value)
        {
            Expression<Func<int, T>> lambda;
            StubClass<T> foo = new StubClass<T>((T)value);
            lambda = i => StubClass<T>.StaticField;            
        }
    }
}
