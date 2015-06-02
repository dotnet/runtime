// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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