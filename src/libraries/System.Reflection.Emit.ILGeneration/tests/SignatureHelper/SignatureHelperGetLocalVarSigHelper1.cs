// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class SignatureHelperGetLocalVarSigHelper1
    {
        [Fact]
        public void PosTest1()
        {
            int expectedValue = 2;
            int actualValue;

            SignatureHelper localVarHelper = SignatureHelper.GetLocalVarSigHelper();
            actualValue = localVarHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }
    }
}
