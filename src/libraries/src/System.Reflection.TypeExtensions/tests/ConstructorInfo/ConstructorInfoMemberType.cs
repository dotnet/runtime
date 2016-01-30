// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    // MemberType
    public class ConstructorInfoMemberType
    {
        public class TestClass
        {
            public TestClass()
            {
            }

            public void GetMyMemberType()
            {
            }
        }

        // Positive Test 1: Ensure a constructor's MemberType is Constructor.
        [Fact]
        public void PosTest1()
        {
            ConstructorInfo ci = typeof(TestClass).GetConstructor(new Type[] { });
            Assert.True(ci.IsConstructor);
        }
    }
}
