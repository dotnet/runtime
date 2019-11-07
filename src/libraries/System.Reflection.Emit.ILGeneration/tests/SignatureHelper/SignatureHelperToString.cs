// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class SignatureHelperToString
    {
        [Fact]
        public void ToString_EqualSignatureHelpers_ReturnsEqualStrings()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            SignatureHelper helper1 = SignatureHelper.GetFieldSigHelper(module);
            SignatureHelper helper2 = SignatureHelper.GetFieldSigHelper(module);

            Assert.Equal(helper1.ToString(), helper2.ToString());
        }
    }
}
