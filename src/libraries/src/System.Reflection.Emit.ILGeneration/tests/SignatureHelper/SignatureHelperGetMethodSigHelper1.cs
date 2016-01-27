// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class SignatureHelperGetMethodSigHelper1
    {
        [Fact]
        public void PosTest1()
        {
            int expectedValue = 3;
            int actualValue;

            SignatureHelper sHelper = SignatureHelper.GetMethodSigHelper(CallingConventions.Any, typeof(int));
            actualValue = sHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void PosTest2()
        {
            int expectedValue = 3;
            int actualValue;

            SignatureHelper sHelper = SignatureHelper.GetMethodSigHelper(CallingConventions.ExplicitThis, typeof(string));
            actualValue = sHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void PosTest3()
        {
            int expectedValue = 3;
            int actualValue;

            SignatureHelper sHelper = SignatureHelper.GetMethodSigHelper(CallingConventions.HasThis, typeof(string));
            actualValue = sHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void PosTest4()
        {
            int expectedValue = 3;
            int actualValue;

            SignatureHelper sHelper = SignatureHelper.GetMethodSigHelper(CallingConventions.Standard, typeof(string));
            actualValue = sHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void PosTest5()
        {
            int expectedValue = 3;
            int actualValue;

            SignatureHelper sHelper = SignatureHelper.GetMethodSigHelper(CallingConventions.VarArgs, typeof(string));
            actualValue = sHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }
    }
}
