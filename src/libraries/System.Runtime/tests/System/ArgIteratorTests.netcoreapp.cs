// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;
using Xunit;

namespace System.Tests
{
    public static class ArgIteratorTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsArgIteratorSupported))]
        public static void ArgIterator_GetRemainingCount_GetNextArg()
        {
            object[] result = GetAllArgs("a", "r", "g", "s", __arglist(true, "hello", 0.42));
            Assert.Equal(new object[] {"a", "r", "g", "s", true, "hello", 0.42}, result);
        }

        private static object[] GetAllArgs(Object arg0, Object arg1, Object arg2, Object arg3, __arglist) 
        {
            ArgIterator args = new ArgIterator(__arglist);
            int argCount = args.GetRemainingCount() + 4;
            object[] objArgs = new Object[argCount];

            // Handle the hard-coded arguments
            objArgs[0] = arg0;
            objArgs[1] = arg1;
            objArgs[2] = arg2;
            objArgs[3] = arg3;

            // Walk all of the args in the variable part of the argument list.
            for (int i = 4; i < argCount; i++)
            {
                objArgs[i] = TypedReference.ToObject(args.GetNextArg());
            }
            args.End();

            return objArgs;
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsArgIteratorSupported))]
        public static void ArgIterator_GetNextArgType()
        {
            var types = new Type[] 
            {
                typeof(string), 
                typeof(byte), 
                typeof(short),
                typeof(long), 
                typeof(int), 
                typeof(float),
                typeof(double),
                typeof(DummyClass),
                typeof(DummyStruct)
            };

            VerifyTypes(types, __arglist(
                default(string), 
                default(byte), 
                default(short),
                default(long), 
                default(int), 
                default(float),
                default(double),
                default(DummyClass),
                default(DummyStruct)
            ));
        }

        private class DummyClass { }
        private struct DummyStruct { }

        private static void VerifyTypes(Type[] types, __arglist) 
        {
            ArgIterator args = new ArgIterator(__arglist);
            int argCount = args.GetRemainingCount();
            Assert.Equal(types.Length, argCount);

            object[] objArgs = new Object[argCount];
            for (int i = 0; i < argCount; i++)
            {
                RuntimeTypeHandle handle = args.GetNextArgType();
                Type type = Type.GetTypeFromHandle(handle);
                Assert.Equal(types[i], type);
                args.GetNextArg(handle);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsArgIteratorNotSupported))]
        public unsafe static void ArgIterator_Throws_PlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new ArgIterator(new RuntimeArgumentHandle()));
            Assert.Throws<PlatformNotSupportedException>(() => {
                fixed (void* p = "test")
                {
                    new ArgIterator(new RuntimeArgumentHandle(), p);
                }
            });
            Assert.Throws<PlatformNotSupportedException>(() => new ArgIterator().End());
            Assert.Throws<PlatformNotSupportedException>(() => new ArgIterator().Equals(new object()));
            Assert.Throws<PlatformNotSupportedException>(() => new ArgIterator().GetHashCode());
            Assert.Throws<PlatformNotSupportedException>(() => new ArgIterator().GetNextArg());
            Assert.Throws<PlatformNotSupportedException>(() => new ArgIterator().GetNextArg(new RuntimeTypeHandle()));
            Assert.Throws<PlatformNotSupportedException>(() => new ArgIterator().GetNextArgType());
            Assert.Throws<PlatformNotSupportedException>(() => new ArgIterator().GetRemainingCount());
        }
    }
}
