// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class SignatureHelperGetPropertySigHelper
    {
        [Fact]
        public void PosTest1()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");

            Type[] a = new Type[] { typeof(int), typeof(char) };
            Type[][] b = new Type[][] { a, a };

            int expectedValue = 29;
            int actualValue;

            SignatureHelper sHelper = SignatureHelper.GetPropertySigHelper(myModule, typeof(string), a, a, a, b, b);
            actualValue = sHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void PosTest2()
        {
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");

            Type[] a = new Type[] { typeof(Int16), typeof(bool) };
            Type[][] b = new Type[][] { a, a };

            int expectedValue = 29;
            int actualValue;

            SignatureHelper sHelper = SignatureHelper.GetPropertySigHelper(myModule, typeof(string), a, a, a, b, b);
            actualValue = sHelper.GetSignature().Length;
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void NegTest1()
        {
            Type[] a = new Type[] { typeof(Int16), typeof(bool) };
            Type[][] b = new Type[][] { a, a };

            Assert.Throws<NullReferenceException>(() => { SignatureHelper sHelper = SignatureHelper.GetPropertySigHelper(null, typeof(string), a, a, a, b, b); });
        }


        [Fact]
        public void NegTest2()
        {
            Type[] a = new Type[] { typeof(Int16), null };
            Type[][] b = new Type[][] { a, a };

            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly_SignatureHelperAddArgument"), AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module_SignatureHelperAddArgument");

            Assert.Throws<ArgumentNullException>(() => { SignatureHelper sHelper = SignatureHelper.GetPropertySigHelper(myModule, typeof(string), a, a, a, b, b); });
        }
    }
}