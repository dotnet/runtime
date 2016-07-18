// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ConstructorBuilderCallingConvention
    {
        public static IEnumerable<object[]> CallingConventions_TestData()
        {
            yield return new object[] { CallingConventions.Any };
            yield return new object[] { CallingConventions.ExplicitThis };
            yield return new object[] { CallingConventions.HasThis };
            yield return new object[] { CallingConventions.Standard };
            yield return new object[] { CallingConventions.VarArgs };
        }

        [Theory]
        [MemberData(nameof(CallingConventions_TestData))]
        public void CallingConvention_NullRequiredOptionalCustomModifiers(CallingConventions callingConvention)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Type[] parameterTypes = new Type[] { typeof(int), typeof(double) };

            ConstructorBuilder cb = type.DefineConstructor(MethodAttributes.Public, callingConvention, parameterTypes, null, null);
            Assert.Equal(CallingConventions.Standard, cb.CallingConvention);
        }

        [Theory]
        [MemberData(nameof(CallingConventions_TestData))]
        public void CallingConvention_NoRequiredOptionalCustomModifiers(CallingConventions callingConvention)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Type[] parameterTypes = new Type[] { typeof(int), typeof(double) };

            ConstructorBuilder cb = type.DefineConstructor(MethodAttributes.Public, callingConvention, parameterTypes);
            Assert.Equal(CallingConventions.Standard, cb.CallingConvention);
        }
    }
}
