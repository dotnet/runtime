// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class PrelinkAllTests
    {

        public  static IEnumerable<object[]> PrelinkAll_TestData()
        {
            yield return new object[] { typeof(int) };
            yield return new object[] { typeof(Math) };
            yield return new object[] { typeof(int).MakeByRefType() };
            yield return new object[] { typeof(int).MakePointerType() };
            yield return new object[] { typeof(int[]) };

            yield return new object[] { typeof(NonGenericClass) };
            yield return new object[] { typeof(GenericClass<string>) };
            yield return new object[] { typeof(AbstractClass) };
            yield return new object[] { typeof(NonGenericStruct) };
            yield return new object[] { typeof(GenericStruct<string>) };
            yield return new object[] { typeof(NonGenericInterface) };
            yield return new object[] { typeof(GenericInterface<string>) };

            yield return new object[] { typeof(GenericClass<>) };
            yield return new object[] { typeof(GenericClass<>).GetTypeInfo().GenericTypeParameters[0] };
        }

        [Theory]
        [MemberData(nameof(PrelinkAll_TestData))]
        public void PrelinkAll_ValidType_Success(Type type)
        {
            Marshal.PrelinkAll(type);
            Marshal.PrelinkAll(type);
        }

        [Fact]
        public void PrelinkAll_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("c", () => Marshal.PrelinkAll(null));
        }
    }
}
