// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests
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
