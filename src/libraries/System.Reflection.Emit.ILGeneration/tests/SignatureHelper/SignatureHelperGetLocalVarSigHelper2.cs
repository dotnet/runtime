// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class SignatureHelperGetLocalVarSigHelper
    {
        [Fact]
        public void PosTest1()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");

            int expectedValue = 2;
            int actualValue;

            SignatureHelper localVarHelper = SignatureHelper.GetLocalVarSigHelper(myModule);
            actualValue = localVarHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }
    }
}
