// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class SignatureHelperGetPropertySigHelper1
    {
        [Fact]
        public void PosTest1()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");

            int expectedValue = 6;
            int actualValue;
            SignatureHelper sHelper = SignatureHelper.GetPropertySigHelper(myModule, typeof(string), new Type[] { typeof(Delegate), typeof(int) });
            actualValue = sHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void PosTest2()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");

            int expectedValue = 6;
            int actualValue;
            SignatureHelper sHelper = SignatureHelper.GetPropertySigHelper(myModule, typeof(Type), new Type[] { typeof(char), typeof(object) });
            actualValue = sHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void PosTest3()
        {
            int expectedValue = 5;
            int actualValue;
            SignatureHelper sHelper = SignatureHelper.GetPropertySigHelper(null, typeof(string), new Type[] { typeof(string), typeof(int) });
            actualValue = sHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void NegTest1()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");

            Assert.Throws<ArgumentNullException>(() => { SignatureHelper sHelper = SignatureHelper.GetPropertySigHelper(myModule, typeof(string), new Type[] { null, typeof(int) }); });
        }
    }
}
