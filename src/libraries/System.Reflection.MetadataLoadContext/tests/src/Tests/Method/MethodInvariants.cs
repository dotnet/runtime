// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class MethodInvariants
    {
        public static void TestMethodBaseInvariants(this MethodBase mb)
        {
            if (mb is MethodInfo m)
                m.TestMethodInfoInvariants();
            else if (mb is ConstructorInfo c)
                c.TestConstructorInfoInvariants();
            else
                Assert.Fail("What kind of MethodBase is this? " + mb);
        }

        public static void TestMethodInfoInvariants(this MethodInfo m)
        {
            if (m.IsGenericMethodDefinition)
                m.TestGenericMethodInfoCommonInvariants();
            else if (m.IsConstructedGenericMethod())
                m.TestConstructedGenericMethodInfoCommonInvariants();
            else
                m.TestNonGenericMethodInfoCommonInvariants();
        }

        public static void TestNonGenericMethodInfoInvariants(this MethodInfo m) => m.TestNonGenericMethodInfoCommonInvariants();
        public static void TestGenericMethodInfoInvariants(this MethodInfo m) => m.TestGenericMethodInfoCommonInvariants();
        public static void TestConstructedGenericMethodInfoInvariants(this MethodInfo m) => m.TestConstructedGenericMethodInfoCommonInvariants();

        public static void TestConstructorInfoInvariants(this ConstructorInfo c)
        {
            c.TestConstructorInfoCommonInvariants();
        }

        private static void TestMethodBaseCommonInvariants(this MethodBase mb)
        {
            string s = mb.ToString(); // We don't test the contents but make sure it doesn't crash.
            Assert.NotNull(s);

            Assert.Equal(mb.IsGenericMethod, mb.IsGenericMethodDefinition || mb.IsConstructedGenericMethod());
            Assert.False(mb.IsGenericMethodDefinition && mb.IsConstructedGenericMethod());
            Assert.Equal(mb.MethodImplementationFlags, mb.GetMethodImplementationFlags());

            int token = mb.MetadataToken;
            Assert.Equal(0x06000000, token & 0xff000000);

            TestUtils.AssertNewObjectReturnedEachTime(() => mb.GetParameters());
            if (!(mb is ConstructorInfo))
            {
                TestUtils.AssertNewObjectReturnedEachTime(() => mb.GetGenericArguments());
            }

            ParameterInfo[] ps = mb.GetParameters();
            for (int i = 0; i < ps.Length; i++)
            {
                ParameterInfo p = ps[i];
                string parameterString = p.ToString();
                Assert.Equal(i, p.Position);
                Assert.Equal(mb, p.Member);
            }

            MethodBody body = mb.GetMethodBody();

            if (mb.IsAbstract || (mb.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) != 0)
            {
                Assert.Null(body);
            }

            if (body != null)
            {
                IList<LocalVariableInfo> lvis1 = body.LocalVariables;
                IList<LocalVariableInfo> lvis2 = body.LocalVariables;
                if (lvis1.Count != 0)
                {
                    Assert.NotSame(lvis1, lvis2);
                }

                IList<ExceptionHandlingClause> eh1 = body.ExceptionHandlingClauses;
                IList<ExceptionHandlingClause> eh2 = body.ExceptionHandlingClauses;
                if (eh1.Count != 0)
                {
                    Assert.NotSame(eh1, eh2);
                }

                byte[] il1 = body.GetILAsByteArray();
                byte[] il2 = body.GetILAsByteArray();
                if (il1.Length != 0)
                {
                    Assert.Same(il1, il2);
                }
            }
        }

        private static void TestConstructorInfoCommonInvariants(this ConstructorInfo c)
        {
            c.TestMethodBaseCommonInvariants();

            Assert.Equal(MemberTypes.Constructor, c.MemberType);

            Assert.False(c.IsGenericMethodDefinition);
            Assert.False(c.IsConstructedGenericMethod());
            Assert.False(c.IsGenericMethod);

            Assert.Throws<NotSupportedException>(() => c.GetGenericArguments());
        }

        private static void TestMethodInfoCommonInvariants(this MethodInfo m)
        {
            m.TestMethodBaseCommonInvariants();

            Assert.Equal(MemberTypes.Method, m.MemberType);
        }

        private static void TestNonGenericMethodInfoCommonInvariants(this MethodInfo m)
        {
            m.TestMethodInfoCommonInvariants();

            Assert.Equal(0, m.GetGenericArguments().Length);
            Assert.Throws<InvalidOperationException>(() => m.GetGenericMethodDefinition());
        }

        private static void TestGenericMethodInfoCommonInvariants(this MethodInfo m)
        {
            m.TestMethodInfoCommonInvariants();

            if (!m.IsImplementedByRuntime())
                Assert.Equal(m, m.GetGenericMethodDefinition());

            Assert.NotEqual(0, m.GetGenericArguments().Length);

            Type[] genericParameters = m.GetGenericArguments();
            for (int i = 0; i < genericParameters.Length; i++)
            {
                Type gp = genericParameters[i];
                Assert.True(gp.IsGenericMethodParameter());

                MethodInfo declaringMethod = (MethodInfo)gp.DeclaringMethod;
                // The DeclaringMethod isn't necessarily the same as there is only one Type object for a generic parameter and it's shared between
                // all MethodInfo's with different ReflectedTypes and different generic instantiations of the declaring type.
                Assert.Equal(m.MetadataToken, declaringMethod.MetadataToken);
                Assert.Equal(declaringMethod.DeclaringType, gp.DeclaringType);
                Assert.False(declaringMethod.DeclaringType.IsConstructedGenericType);
                Assert.Equal(declaringMethod.DeclaringType, declaringMethod.ReflectedType);

                Assert.Equal(i, gp.GenericParameterPosition);
            }
        }

        private static void TestConstructedGenericMethodInfoCommonInvariants(this MethodInfo m)
        {
            m.TestMethodInfoCommonInvariants();

            Assert.NotEqual(0, m.GetGenericArguments().Length);
            MethodInfo gm = m.GetGenericMethodDefinition();
            Assert.Equal(gm.Module, m.Module);
            Assert.Equal(gm.DeclaringType, m.DeclaringType);
            Assert.Equal(gm.ReflectedType, m.ReflectedType);
            Assert.Equal(gm.MetadataToken, m.MetadataToken);
            Assert.Equal(gm.GetGenericArguments().Length, m.GetGenericArguments().Length);
        }
    }
}
