// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class SignatureHelperGetMethodSigHelper4
    {
        [Fact]
        public void PosTest1()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");


            int expectedValue = 5;
            int actualValue;

            SignatureHelper sHelper = SignatureHelper.GetMethodSigHelper(myModule, typeof(string), new Type[] { typeof(char), typeof(int) });
            actualValue = sHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }



        [Fact]
        public void NegTest1()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");

            int expectedValue = 5;
            int actualValue;

            SignatureHelper sHelper = SignatureHelper.GetMethodSigHelper(null, typeof(string), new Type[] { typeof(char), typeof(int) });
            actualValue = sHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }


        [Fact]
        public void NegTest2()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");

            Assert.Throws<ArgumentNullException>(() => { SignatureHelper sHelper = SignatureHelper.GetMethodSigHelper(myModule, typeof(string), new Type[] { typeof(char), null }); });
        }
    }
}
