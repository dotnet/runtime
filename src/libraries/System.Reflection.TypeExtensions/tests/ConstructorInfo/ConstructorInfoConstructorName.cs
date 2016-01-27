// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
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
