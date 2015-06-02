// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class SignatureHelperAddArgument
    {
        [Fact]
        public void PosTest1()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper = SignatureHelper.GetFieldSigHelper(myModule);

            int expectedValue = 2;
            int actualValue;

            sHelper.AddArgument(typeof(string), null, null);
            actualValue = sHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void PosTest2()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper = SignatureHelper.GetFieldSigHelper(myModule);

            int expectedValue = 4;
            int actualValue;

            sHelper.AddArgument(typeof(string), new Type[] { typeof(int) }, null);
            actualValue = sHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void PosTest3()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper = SignatureHelper.GetFieldSigHelper(myModule);

            int expectedValue = 4;
            int actualValue;

            sHelper.AddArgument(typeof(string), null, new Type[] { typeof(Type) });
            actualValue = sHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void PosTest4()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper = SignatureHelper.GetFieldSigHelper(myModule);

            int expectedValue = 6;
            int actualValue;

            sHelper.AddArgument(typeof(string), new Type[] { typeof(int) }, new Type[] { typeof(Type) });
            actualValue = sHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void NegTest1()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper = SignatureHelper.GetFieldSigHelper(myModule);

            Assert.Throws<ArgumentNullException>(() => { sHelper.AddArgument(null, null, null); });
        }

        [Fact]
        public void NegTest2()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper = SignatureHelper.GetFieldSigHelper(myModule);
            Byte[] signature = sHelper.GetSignature();
            //this action will lead the Signature be finished.

            Assert.Throws<ArgumentException>(() => { sHelper.AddArgument(typeof(string), null, null); });
        }

        [Fact]
        public void NegTest3()
        {
            int[] nums = { 1, 0 };

            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper = SignatureHelper.GetFieldSigHelper(myModule);

            Assert.Throws<ArgumentException>(() => { sHelper.AddArgument(typeof(string), new Type[] { nums.GetType() }, null); });
        }

        [Fact]
        public void NegTest4()
        {
            int[] nums = { 1, 0 };

            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper = SignatureHelper.GetFieldSigHelper(myModule);

            Assert.Throws<ArgumentException>(() => { sHelper.AddArgument(typeof(string), null, new Type[] { nums.GetType() }); });
        }
    }
}