// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class SignatureHelperGetLocalVarSigHelper
    {
        [Fact]
        public void GetLocalVarSigHelper_Length_ReturnsTwo()
        {
            SignatureHelper helper = SignatureHelper.GetLocalVarSigHelper();
            Assert.Equal(2, helper.GetSignature().Length);
        }

        [Fact]
        public void GetLocalVarSigHelper_Module_Length_ReturnsTwo()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            SignatureHelper helper = SignatureHelper.GetLocalVarSigHelper(module);
            Assert.Equal(2, helper.GetSignature().Length);
        }
    }
}
