// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class SignatureHelperToString
    {
        [Fact]
        public void PosTest1()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper1 = SignatureHelper.GetFieldSigHelper(myModule);
            SignatureHelper sHelper2 = SignatureHelper.GetFieldSigHelper(myModule);

            string expectedValue;
            string actualValue;

            expectedValue = sHelper1.ToString();
            actualValue = sHelper2.ToString();

            Assert.True(expectedValue.Equals(actualValue));
        }
    }
}
