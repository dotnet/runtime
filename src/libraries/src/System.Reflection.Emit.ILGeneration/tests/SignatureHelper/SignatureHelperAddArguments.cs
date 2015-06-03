// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class SignatureHelperAddArguments
    {
        [Fact]
        public void PosTest1()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper = SignatureHelper.GetFieldSigHelper(myModule);

            int expectedValue = 3;
            int actualValue;

            sHelper.AddArguments(new Type[] { typeof(string), typeof(int) }, null, null);
            actualValue = sHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void PosTest2()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper = SignatureHelper.GetFieldSigHelper(myModule);

            int expectedValue = 11;
            int actualValue;

            sHelper.AddArguments(new Type[] { typeof(string), typeof(int) }, null, new Type[][] { new Type[] { typeof(string), typeof(int) }, new Type[] { typeof(char), typeof(Module) } });
            actualValue = sHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void PosTest3()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper = SignatureHelper.GetFieldSigHelper(myModule);

            int expectedValue = 11;
            int actualValue;

            sHelper.AddArguments(new Type[] { typeof(string), typeof(int) }, new Type[][] { new Type[] { typeof(string), typeof(int) }, new Type[] { typeof(char), typeof(Module) } }, null);
            actualValue = sHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void PosTest4()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper = SignatureHelper.GetFieldSigHelper(myModule);

            int expectedValue = 19;
            int actualValue;

            sHelper.AddArguments(new Type[] { typeof(string), typeof(int) }, new Type[][] { new Type[] { typeof(string), typeof(int) }, new Type[] { typeof(char), typeof(Module) } }, new Type[][] { new Type[] { typeof(string), typeof(int) }, new Type[] { typeof(char), typeof(Module) } });
            actualValue = sHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void NegTest1()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper = SignatureHelper.GetFieldSigHelper(myModule);

            Assert.Throws<ArgumentNullException>(() => { sHelper.AddArguments(new Type[] { typeof(char), null }, null, null); });
        }

        [Fact]
        public void NegTest2()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper = SignatureHelper.GetFieldSigHelper(myModule);
            byte[] signature = sHelper.GetSignature();
            //this action will lead the Signature be finished.

            Assert.Throws<ArgumentException>(() => { sHelper.AddArguments(new Type[] { typeof(string) }, null, null); });
        }

        [Fact]
        public void NegTest3()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper = SignatureHelper.GetFieldSigHelper(myModule);

            Assert.Throws<ArgumentNullException>(() => { sHelper.AddArguments(new Type[] { typeof(string) }, new Type[][] { new Type[] { typeof(int), null } }, null); });
        }

        [Fact]
        public void NegTest4()
        {
            int[] nums = new int[] { 1, 1 };

            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper = SignatureHelper.GetFieldSigHelper(myModule);

            Assert.Throws<ArgumentException>(() => { sHelper.AddArguments(new Type[] { typeof(string) }, new Type[][] { new Type[] { typeof(int), nums.GetType() } }, null); });
        }

        [Fact]
        public void NegTest5()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper = SignatureHelper.GetFieldSigHelper(myModule);

            Assert.Throws<ArgumentException>(() => { sHelper.AddArguments(new Type[] { typeof(string) }, new Type[][] { new Type[] { typeof(int) }, new Type[] { typeof(char) } }, null); });
        }

        [Fact]
        public void NegTest6()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper = SignatureHelper.GetFieldSigHelper(myModule);

            Assert.Throws<ArgumentNullException>(() => { sHelper.AddArguments(new Type[] { typeof(string) }, null, new Type[][] { new Type[] { typeof(int), null } }); });
        }

        [Fact]
        public void NegTest7()
        {
            int[] nums = new int[] { 1, 1 };

            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper = SignatureHelper.GetFieldSigHelper(myModule);

            Assert.Throws<ArgumentException>(() => { sHelper.AddArguments(new Type[] { typeof(string) }, null, new Type[][] { new Type[] { typeof(int), nums.GetType() } }); });
        }

        [Fact]
        public void NegTest8()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");
            SignatureHelper sHelper = SignatureHelper.GetFieldSigHelper(myModule);

            Assert.Throws<ArgumentException>(() => { sHelper.AddArguments(new Type[] { typeof(string) }, null, new Type[][] { new Type[] { typeof(int) }, new Type[] { typeof(char) } }); });
        }
    }

    public class Example { }
}
