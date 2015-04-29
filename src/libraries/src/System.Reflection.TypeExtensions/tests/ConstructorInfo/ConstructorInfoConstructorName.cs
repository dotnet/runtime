// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Reflection.Compatibility;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests.ConstructorTests
{
    public class ConstructorInfoConstructorName
    {
        //Positive Test 1: Ensure ConstructorInfo.ContructorName is correct
        [Fact]
        public void PosTest1()
        {
            Assert.Equal(".ctor", ConstructorInfo.ConstructorName);
        }
    }
}
