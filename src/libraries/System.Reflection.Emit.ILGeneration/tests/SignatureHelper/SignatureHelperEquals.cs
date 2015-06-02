// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class SignatureHelperEquals
    {
        [Fact]
        public void PosTest1()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper1 = SignatureHelper.GetFieldSigHelper(myModule);
            SignatureHelper sHelper2 = SignatureHelper.GetFieldSigHelper(myModule);

            bool expectedValue = true;
            bool actualValue;

            actualValue = sHelper1.Equals(sHelper2);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void PosTest2()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper1 = SignatureHelper.GetFieldSigHelper(myModule);
            SignatureHelper sHelper2 = SignatureHelper.GetFieldSigHelper(myModule);

            sHelper1.AddArgument(typeof(string));

            bool expectedValue = false;
            bool actualValue;

            actualValue = sHelper1.Equals(sHelper2);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void PosTest3()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper1 = SignatureHelper.GetFieldSigHelper(myModule);
            SignatureHelper sHelper2 = SignatureHelper.GetFieldSigHelper(myModule);

            sHelper1.AddArgument(typeof(string));
            sHelper2.AddArgument(typeof(string));

            bool expectedValue = true;
            bool actualValue;

            actualValue = sHelper1.Equals(sHelper2);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void PosTest4()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper1 = SignatureHelper.GetFieldSigHelper(myModule);

            bool expectedValue = false;
            bool actualValue;

            actualValue = sHelper1.Equals(null);
            Assert.Equal(expectedValue, actualValue);
        }
    }
}