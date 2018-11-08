// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using SampleMetadata;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class MethodTests
    {
        [Fact]
        public unsafe static void TestMethods1()
        {
            TestMethods1Worker(typeof(ClassWithMethods1<>).Project());
            TestMethods1Worker(typeof(ClassWithMethods1<int>).Project());
            TestMethods1Worker(typeof(ClassWithMethods1<string>).Project());
        }

        private static void TestMethods1Worker(Type t)
        {
            const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodInfo m = t.GetMethod("Method1", bf);
            Assert.Equal("Method1", m.Name);
            Assert.Equal(t, m.DeclaringType);
            Assert.Equal(t, m.ReflectedType);

            Assert.False(m.IsGenericMethodDefinition);
            Assert.False(m.IsConstructedGenericMethod());
            Assert.False(m.IsGenericMethod);
            Assert.Equal(MethodAttributes.Public | MethodAttributes.HideBySig, m.Attributes);
            Assert.Equal(MethodImplAttributes.IL, m.MethodImplementationFlags);
            Assert.Equal(CallingConventions.Standard | CallingConventions.HasThis, m.CallingConvention);

            Type theT = t.GetGenericArguments()[0];
            Assert.Equal(typeof(bool).Project(), m.ReturnType);
            ParameterInfo rp = m.ReturnParameter;
            Assert.Equal(null, rp.Name);
            Assert.Equal(typeof(bool).Project(), rp.ParameterType);
            Assert.Equal(m, rp.Member);
            Assert.Equal(-1, rp.Position);

            ParameterInfo[] ps = m.GetParameters();
            Assert.Equal(2, ps.Length);

            {
                ParameterInfo p = ps[0];
                Assert.Equal("x", p.Name);
                Assert.Equal(typeof(int).Project(), p.ParameterType);
                Assert.Equal(m, p.Member);
                Assert.Equal(0, p.Position);
            }

            {
                ParameterInfo p = ps[1];
                Assert.Equal("t", p.Name);
                Assert.Equal(theT, p.ParameterType);
                Assert.Equal(m, p.Member);
                Assert.Equal(1, p.Position);
            }

            TestUtils.AssertNewObjectReturnedEachTime(() => m.GetParameters());

            m.TestMethodInfoInvariants();
        }

        [Fact]
        public unsafe static void TestAllCoreTypes()
        {
            const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodInfo m = typeof(ClassWithMethods1<>).Project().GetMethod("TestPrimitives1", bf);
            Assert.Equal(typeof(void).Project(), m.ReturnParameter.ParameterType);
            ParameterInfo[] ps = m.GetParameters();

            {
                ParameterInfo p = ps.Single((p1) => p1.Name == "bo");
                Assert.Equal(typeof(bool).Project(), p.ParameterType);
            }
            {
                ParameterInfo p = ps.Single((p1) => p1.Name == "c");
                Assert.Equal(typeof(char).Project(), p.ParameterType);
            }
            {
                ParameterInfo p = ps.Single((p1) => p1.Name == "b");
                Assert.Equal(typeof(byte).Project(), p.ParameterType);
            }
            {
                ParameterInfo p = ps.Single((p1) => p1.Name == "s");
                Assert.Equal(typeof(short).Project(), p.ParameterType);
            }
            {
                ParameterInfo p = ps.Single((p1) => p1.Name == "i");
                Assert.Equal(typeof(int).Project(), p.ParameterType);
            }
            {
                ParameterInfo p = ps.Single((p1) => p1.Name == "l");
                Assert.Equal(typeof(long).Project(), p.ParameterType);
            }
            {
                ParameterInfo p = ps.Single((p1) => p1.Name == "ip");
                Assert.Equal(typeof(IntPtr).Project(), p.ParameterType);
            }
            {
                ParameterInfo p = ps.Single((p1) => p1.Name == "sb");
                Assert.Equal(typeof(sbyte).Project(), p.ParameterType);
            }
            {
                ParameterInfo p = ps.Single((p1) => p1.Name == "us");
                Assert.Equal(typeof(ushort).Project(), p.ParameterType);
            }
            {
                ParameterInfo p = ps.Single((p1) => p1.Name == "ui");
                Assert.Equal(typeof(uint).Project(), p.ParameterType);
            }
            {
                ParameterInfo p = ps.Single((p1) => p1.Name == "ul");
                Assert.Equal(typeof(ulong).Project(), p.ParameterType);
            }
            {
                ParameterInfo p = ps.Single((p1) => p1.Name == "uip");
                Assert.Equal(typeof(UIntPtr).Project(), p.ParameterType);
            }
            {
                ParameterInfo p = ps.Single((p1) => p1.Name == "fl");
                Assert.Equal(typeof(float).Project(), p.ParameterType);
            }
            {
                ParameterInfo p = ps.Single((p1) => p1.Name == "db");
                Assert.Equal(typeof(double).Project(), p.ParameterType);
            }
            {
                ParameterInfo p = ps.Single((p1) => p1.Name == "o");
                Assert.Equal(typeof(object).Project(), p.ParameterType);
            }
            {
                ParameterInfo p = ps.Single((p1) => p1.Name == "str");
                Assert.Equal(typeof(string).Project(), p.ParameterType);
            }
            {
                ParameterInfo p = ps.Single((p1) => p1.Name == "tr");
                Assert.Equal(typeof(TypedReference).Project(), p.ParameterType);
            }
        }

        [Fact]
        public static void TestGenericMethods1()
        {
            TestGenericMethods1Worker(typeof(GenericClassWithGenericMethods1<,>).Project());
            TestGenericMethods1Worker(typeof(GenericClassWithGenericMethods1<int, string>).Project());
        }

        private static void TestGenericMethods1Worker(Type t)
        {
            const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodInfo m = t.GetMethod("GenericMethod1", bf);

            Assert.Equal(m, m.GetGenericMethodDefinition());

            Assert.Equal("GenericMethod1", m.Name);
            Assert.Equal(t, m.DeclaringType);
            Assert.Equal(t, m.ReflectedType);

            Assert.True(m.IsGenericMethodDefinition);
            Assert.False(m.IsConstructedGenericMethod());
            Assert.True(m.IsGenericMethod);

            Type[] methodGenericParameters = m.GetGenericArguments();
            Assert.Equal(2, methodGenericParameters.Length);

            Type theT = t.GetGenericArguments()[0];
            Type theU = t.GetGenericArguments()[1];
            Type theM = m.GetGenericArguments()[0];
            Type theN = m.GetGenericArguments()[1];

            theM.TestGenericMethodParameterInvariants();
            theN.TestGenericMethodParameterInvariants();

            ParameterInfo[] ps = m.GetParameters();
            Assert.Equal(1, ps.Length);
            ParameterInfo p = ps[0];
            Type actual = p.ParameterType;

            //GenericClass5<N, M[], IEnumerable<U>, T[,], int>
            Type expected = typeof(GenericClass5<,,,,>).Project().MakeGenericType(
                theN,
                theM.MakeArrayType(),
                typeof(IEnumerable<>).Project().MakeGenericType(theU),
                theT.MakeArrayType(2),
                typeof(int).Project());
            Assert.Equal(expected, actual);

            m.TestGenericMethodInfoInvariants();
        }

        [Fact]
        public static void TestConstructedGenericMethods1()
        {
            TestConstructedGenericMethods1Worker(typeof(GenericClassWithGenericMethods1<,>).Project());
            TestConstructedGenericMethods1Worker(typeof(GenericClassWithGenericMethods1<int, string>).Project());
        }

        private static void TestConstructedGenericMethods1Worker(Type t)
        {
            const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodInfo gm = t.GetMethod("GenericMethod1", bf);
            MethodInfo m = gm.MakeGenericMethod(typeof(object).Project(), typeof(string).Project());

            Assert.Equal(gm, m.GetGenericMethodDefinition());

            Assert.Equal("GenericMethod1", m.Name);
            Assert.Equal(t, m.DeclaringType);
            Assert.Equal(t, m.ReflectedType);

            Assert.False(m.IsGenericMethodDefinition);
            Assert.True(m.IsConstructedGenericMethod());
            Assert.True(m.IsGenericMethod);

            Type[] methodGenericParameters = m.GetGenericArguments();
            Assert.Equal(2, methodGenericParameters.Length);

            Type theT = t.GetGenericArguments()[0];
            Type theU = t.GetGenericArguments()[1];
            Type theM = m.GetGenericArguments()[0];
            Type theN = m.GetGenericArguments()[1];
            Assert.Equal(typeof(object).Project(), theM);
            Assert.Equal(typeof(string).Project(), theN);

            ParameterInfo[] ps = m.GetParameters();
            Assert.Equal(1, ps.Length);
            ParameterInfo p = ps[0];
            Type actual = p.ParameterType;

            //GenericClass5<N, M[], IEnumerable<U>, T[,], int>
            Type expected = typeof(GenericClass5<,,,,>).Project().MakeGenericType(
                theN,
                theM.MakeArrayType(),
                typeof(IEnumerable<>).Project().MakeGenericType(theU),
                theT.MakeArrayType(2),
                typeof(int).Project());
            Assert.Equal(expected, actual);

            m.TestConstructedGenericMethodInfoInvariants();
        }

        [Fact]
        public unsafe static void TestCustomModifiers1()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new CoreMetadataAssemblyResolver(), "mscorlib"))
            {

                Assembly a = lc.LoadFromByteArray(TestData.s_CustomModifiersImage);
                Type t = a.GetType("N", throwOnError: true);
                Type reqA = a.GetType("ReqA", throwOnError: true);
                Type reqB = a.GetType("ReqB", throwOnError: true);
                Type reqC = a.GetType("ReqC", throwOnError: true);
                Type optA = a.GetType("OptA", throwOnError: true);
                Type optB = a.GetType("OptB", throwOnError: true);
                Type optC = a.GetType("OptC", throwOnError: true);

                MethodInfo m = t.GetMethod("MyMethod");
                ParameterInfo p = m.GetParameters()[0];
                Type[] req = p.GetRequiredCustomModifiers();
                Type[] opt = p.GetOptionalCustomModifiers();

                Assert.Equal<Type>(new Type[] { reqA, reqB, reqC }, req);
                Assert.Equal<Type>(new Type[] { optA, optB, optC }, opt);

                TestUtils.AssertNewObjectReturnedEachTime(() => p.GetRequiredCustomModifiers());
                TestUtils.AssertNewObjectReturnedEachTime(() => p.GetOptionalCustomModifiers());
            }
        }

        [Fact]
        public static void TestMethodBody1()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new CoreMetadataAssemblyResolver(), "mscorlib"))
            {
                Assembly coreAssembly = lc.LoadFromStream(TestUtils.CreateStreamForCoreAssembly());
                Assembly a = lc.LoadFromByteArray(TestData.s_AssemblyWithMethodBodyImage);

                Type nonsense = a.GetType("Nonsense`1", throwOnError: true);
                Type theT = nonsense.GetTypeInfo().GenericTypeParameters[0];
                MethodInfo m = nonsense.GetMethod("Foo");
                Type theM = m.GetGenericArguments()[0];
                MethodBody mb = m.GetMethodBody();
                byte[] il = mb.GetILAsByteArray();
                Assert.Equal<byte>(TestData.s_AssemblyWithMethodBodyILBytes, il);
                Assert.Equal(4, mb.MaxStackSize);
                Assert.True(mb.InitLocals);
                Assert.Equal(0x11000001, mb.LocalSignatureMetadataToken);

                IList<LocalVariableInfo> lvis = mb.LocalVariables;
                Assert.Equal(10, lvis.Count);

                Assert.Equal(0, lvis[0].LocalIndex);
                Assert.False(lvis[0].IsPinned);
                Assert.Equal(coreAssembly.GetType("System.Single", throwOnError: true), lvis[0].LocalType);

                Assert.Equal(1, lvis[1].LocalIndex);
                Assert.False(lvis[1].IsPinned);
                Assert.Equal(coreAssembly.GetType("System.Double", throwOnError: true), lvis[1].LocalType);

                Assert.Equal(2, lvis[2].LocalIndex);
                Assert.False(lvis[2].IsPinned);
                Assert.Equal(theT, lvis[2].LocalType);

                Assert.Equal(3, lvis[3].LocalIndex);
                Assert.False(lvis[3].IsPinned);
                Assert.Equal(theT.MakeArrayType(), lvis[3].LocalType);

                Assert.Equal(4, lvis[4].LocalIndex);
                Assert.False(lvis[4].IsPinned);
                Assert.Equal(coreAssembly.GetType("System.Collections.Generic.IList`1", throwOnError: true).MakeGenericType(theM), lvis[4].LocalType);

                Assert.Equal(5, lvis[5].LocalIndex);
                Assert.False(lvis[5].IsPinned);
                Assert.Equal(coreAssembly.GetType("System.String", throwOnError: true), lvis[5].LocalType);

                Assert.Equal(6, lvis[6].LocalIndex);
                Assert.False(lvis[6].IsPinned);
                Assert.Equal(coreAssembly.GetType("System.Int32", throwOnError: true).MakeArrayType(), lvis[6].LocalType);

                Assert.Equal(7, lvis[7].LocalIndex);
                Assert.True(lvis[7].IsPinned);
                Assert.Equal(coreAssembly.GetType("System.Int32", throwOnError: true).MakeByRefType(), lvis[7].LocalType);

                Assert.Equal(8, lvis[8].LocalIndex);
                Assert.False(lvis[8].IsPinned);
                Assert.Equal(coreAssembly.GetType("System.Int32", throwOnError: true).MakeArrayType(), lvis[8].LocalType);

                Assert.Equal(9, lvis[9].LocalIndex);
                Assert.False(lvis[9].IsPinned);
                Assert.Equal(coreAssembly.GetType("System.Boolean", throwOnError: true), lvis[9].LocalType);

                IList<ExceptionHandlingClause> ehcs = mb.ExceptionHandlingClauses;
                Assert.Equal(2, ehcs.Count);

                ExceptionHandlingClause ehc = ehcs[0];
                Assert.Equal(ExceptionHandlingClauseOptions.Finally, ehc.Flags);
                Assert.Equal(97, ehc.TryOffset);
                Assert.Equal(41, ehc.TryLength);
                Assert.Equal(138, ehc.HandlerOffset);
                Assert.Equal(5, ehc.HandlerLength);

                ehc = ehcs[1];
                Assert.Equal(ExceptionHandlingClauseOptions.Filter, ehc.Flags);
                Assert.Equal(88, ehc.TryOffset);
                Assert.Equal(58, ehc.TryLength);
                Assert.Equal(172, ehc.HandlerOffset);
                Assert.Equal(16, ehc.HandlerLength);
                Assert.Equal(146, ehc.FilterOffset);
            }
        }

        [Fact]
        public static void TestEHClauses()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new CoreMetadataAssemblyResolver(), "mscorlib"))
            {
                Assembly coreAssembly = lc.LoadFromStream(TestUtils.CreateStreamForCoreAssembly());
                Assembly a = lc.LoadFromByteArray(TestData.s_AssemblyWithEhClausesImage);

                Type gt = a.GetType("G`1", throwOnError: true);
                Type et = a.GetType("MyException`2", throwOnError: true);
                Type gtP0 = gt.GetGenericTypeParameters()[0];
                Type etP0 = et.GetGenericTypeParameters()[0];
                Type etP1 = et.GetGenericTypeParameters()[1];
                {
                    MethodInfo m = gt.GetMethod("Catch");
                    Type theM = m.GetGenericArguments()[0];

                    MethodBody body = m.GetMethodBody();
                    IList<ExceptionHandlingClause> ehs = body.ExceptionHandlingClauses;
                    Assert.Equal(1, ehs.Count);
                    ExceptionHandlingClause eh = ehs[0];
                    Assert.Equal(ExceptionHandlingClauseOptions.Clause, eh.Flags);
                    Assert.Equal(1, eh.TryOffset);
                    Assert.Equal(15, eh.TryLength);
                    Assert.Equal(16, eh.HandlerOffset);
                    Assert.Equal(16, eh.HandlerLength);
                    Assert.Throws<InvalidOperationException>(() => eh.FilterOffset);
                    Assert.Equal(et.MakeGenericType(gtP0, theM), eh.CatchType);
                }

                {
                    Type sysInt32 = coreAssembly.GetType("System.Int32", throwOnError: true);
                    Type sysSingle = coreAssembly.GetType("System.Single", throwOnError: true);

                    MethodInfo m = gt.MakeGenericType(sysInt32).GetMethod("Catch").MakeGenericMethod(sysSingle);

                    MethodBody body = m.GetMethodBody();
                    IList<ExceptionHandlingClause> ehs = body.ExceptionHandlingClauses;
                    Assert.Equal(1, ehs.Count);
                    ExceptionHandlingClause eh = ehs[0];
                    Assert.Equal(ExceptionHandlingClauseOptions.Clause, eh.Flags);
                    Assert.Equal(1, eh.TryOffset);
                    Assert.Equal(15, eh.TryLength);
                    Assert.Equal(16, eh.HandlerOffset);
                    Assert.Equal(16, eh.HandlerLength);
                    Assert.Throws<InvalidOperationException>(() => eh.FilterOffset);
                    Assert.Equal(et.MakeGenericType(sysInt32, sysSingle), eh.CatchType);
                }

                {
                    MethodInfo m = gt.GetMethod("Finally");
                    MethodBody body = m.GetMethodBody();
                    IList<ExceptionHandlingClause> ehs = body.ExceptionHandlingClauses;
                    Assert.Equal(1, ehs.Count);
                    ExceptionHandlingClause eh = ehs[0];
                    Assert.Equal(ExceptionHandlingClauseOptions.Finally, eh.Flags);
                    Assert.Equal(1, eh.TryOffset);
                    Assert.Equal(15, eh.TryLength);
                    Assert.Equal(16, eh.HandlerOffset);
                    Assert.Equal(14, eh.HandlerLength);
                    Assert.Throws<InvalidOperationException>(() => eh.FilterOffset);
                    Assert.Throws<InvalidOperationException>(() => eh.CatchType);
                }

                {
                    MethodInfo m = gt.GetMethod("Fault");
                    MethodBody body = m.GetMethodBody();
                    IList<ExceptionHandlingClause> ehs = body.ExceptionHandlingClauses;
                    Assert.Equal(1, ehs.Count);
                    ExceptionHandlingClause eh = ehs[0];
                    Assert.Equal(ExceptionHandlingClauseOptions.Fault, eh.Flags);
                    Assert.Equal(1, eh.TryOffset);
                    Assert.Equal(15, eh.TryLength);
                    Assert.Equal(16, eh.HandlerOffset);
                    Assert.Equal(14, eh.HandlerLength);
                    Assert.Throws<InvalidOperationException>(() => eh.FilterOffset);
                    Assert.Throws<InvalidOperationException>(() => eh.CatchType);
                }

                {
                    MethodInfo m = gt.GetMethod("Filter");
                    MethodBody body = m.GetMethodBody();
                    IList<ExceptionHandlingClause> ehs = body.ExceptionHandlingClauses;
                    Assert.Equal(1, ehs.Count);
                    ExceptionHandlingClause eh = ehs[0];
                    Assert.Equal(ExceptionHandlingClauseOptions.Filter, eh.Flags);
                    Assert.Equal(1, eh.TryOffset);
                    Assert.Equal(15, eh.TryLength);
                    Assert.Equal(40, eh.HandlerOffset);
                    Assert.Equal(16, eh.HandlerLength);
                    Assert.Equal(16, eh.FilterOffset);
                    Assert.Throws<InvalidOperationException>(() => eh.CatchType);
                }
            }
        }

        [Fact]
        public static void TestCallingConventions()
        {
            const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase[] mbs = (MethodBase[])(typeof(ExerciseCallingConventions).Project().GetMember("*", MemberTypes.Method | MemberTypes.Constructor, bf));
            mbs = mbs.OrderBy(m => m.Name).ToArray();
            Assert.Equal(5, mbs.Length);

            Assert.Equal(".cctor", mbs[0].Name);
            Assert.Equal(CallingConventions.Standard, mbs[0].CallingConvention);

            Assert.Equal(".ctor", mbs[1].Name);
            Assert.Equal(CallingConventions.Standard | CallingConventions.HasThis, mbs[1].CallingConvention);

            Assert.Equal("InstanceMethod", mbs[2].Name);
            Assert.Equal(CallingConventions.Standard | CallingConventions.HasThis, mbs[2].CallingConvention);

            Assert.Equal("StaticMethod", mbs[3].Name);
            Assert.Equal(CallingConventions.Standard, mbs[3].CallingConvention);

            Assert.Equal("VirtualMethod", mbs[4].Name);
            Assert.Equal(CallingConventions.Standard | CallingConventions.HasThis, mbs[4].CallingConvention);
        }
    }
}
