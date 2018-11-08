// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using SampleMetadata;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class ConstructorTests
    {
        [Fact]
        public unsafe static void TestConstructors1()
        {
            TestConstructors1Worker(typeof(ClassWithConstructor1<>).Project());
            TestConstructors1Worker(typeof(ClassWithConstructor1<int>).Project());
            TestConstructors1Worker(typeof(ClassWithConstructor1<string>).Project());
        }

        private static void TestConstructors1Worker(Type t)
        {
            const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.ExactBinding;
            Type theT = t.GetGenericArguments()[0];
            Type[] argumentTypes = { typeof(int).Project(), theT };
            ConstructorInfo c = t.GetConstructor(bf, null, argumentTypes, null);

            Assert.Equal(ConstructorInfo.ConstructorName, c.Name);
            Assert.Equal(t, c.DeclaringType);
            Assert.Equal(t, c.ReflectedType);

            Assert.False(c.IsGenericMethodDefinition);
            Assert.False(c.IsConstructedGenericMethod());
            Assert.False(c.IsGenericMethod);
            Assert.Equal(MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, c.Attributes);
            Assert.Equal(MethodImplAttributes.IL, c.MethodImplementationFlags);
            Assert.Equal(CallingConventions.Standard | CallingConventions.HasThis, c.CallingConvention);

            ParameterInfo[] ps = c.GetParameters();
            Assert.Equal(2, ps.Length);

            {
                ParameterInfo p = ps[0];
                Assert.Equal("x", p.Name);
                Assert.Equal(typeof(int).Project(), p.ParameterType);
                Assert.Equal(c, p.Member);
                Assert.Equal(0, p.Position);
            }

            {
                ParameterInfo p = ps[1];
                Assert.Equal("t", p.Name);
                Assert.Equal(theT, p.ParameterType);
                Assert.Equal(c, p.Member);
                Assert.Equal(1, p.Position);
            }

            TestUtils.AssertNewObjectReturnedEachTime(() => c.GetParameters());

            c.TestConstructorInfoInvariants();
        }
    }
}
