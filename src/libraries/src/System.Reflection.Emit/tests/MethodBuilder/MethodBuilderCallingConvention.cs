// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderCallingConvention
    {
        public static IEnumerable<object[]> CallingConventions_TestData()
        {
            yield return new object[] { CallingConventions.Any };
            yield return new object[] { CallingConventions.ExplicitThis };
            yield return new object[] { CallingConventions.HasThis };
            yield return new object[] { CallingConventions.Standard };
            yield return new object[] { CallingConventions.VarArgs };
            yield return new object[] { CallingConventions.Any | CallingConventions.Standard };
            yield return new object[] { CallingConventions.Any | CallingConventions.VarArgs };
            yield return new object[] { CallingConventions.HasThis | CallingConventions.Standard };
            yield return new object[] { CallingConventions.HasThis | CallingConventions.ExplicitThis };
            yield return new object[] { (CallingConventions)(-1) };
        }

        [Theory]
        [MemberData(nameof(CallingConventions_TestData))]
        public void CallingConvention_StaticMethods(CallingConventions callingConvention)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod(callingConvention.ToString(), MethodAttributes.Static, callingConvention);
            Assert.Equal(callingConvention, method.CallingConvention);
        }
        
        [Theory]
        [MemberData(nameof(CallingConventions_TestData))]
        public void CallingConvention_InstanceMethods(CallingConventions callingConvention)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod(callingConvention.ToString(), MethodAttributes.Public, callingConvention);
            Assert.Equal(callingConvention | CallingConventions.HasThis, method.CallingConvention);
        }
    }
}
