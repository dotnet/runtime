// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class AssemblyGetModuleTests
    {
        [Fact]
        public void PosTest1()
        {
            AssemblyName myAsmName = new AssemblyName("TestAssembly1");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder mb = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module1");
        }

        [Fact]
        public void PosTest2()
        {
            AssemblyName myAsmName = new AssemblyName("TestAssembly2");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module1");
            TypeBuilder myTypeBuilder = myModuleBuilder.DefineType("HelloWorld", TypeAttributes.Public);
            ConstructorBuilder myConstructor = myTypeBuilder.DefineConstructor(
                     MethodAttributes.Public, CallingConventions.Standard, new Type[] { });
            ILGenerator myILGenerator = myConstructor.GetILGenerator();
            myILGenerator.Emit(OpCodes.Ldarg_1);
        }

        [Fact]
        public void PosTest3()
        {
            AssemblyName myAsmName = new AssemblyName("TestAssembly3");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder mb = myAssembly.DefineDynamicModule(new string('a', 259));
        }

        [Fact]
        public void NegTest1()
        {
            AssemblyName myAsmName = new AssemblyName("TestAssembly4");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.Run);
            Assert.Throws<ArgumentException>(() => { ModuleBuilder mb = myAssembly.DefineDynamicModule(""); });
        }

        [Fact]
        public void NegTest2()
        {
            AssemblyName myAsmName = new AssemblyName("TestAssembly5");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.Run);
            Assert.Throws<ArgumentNullException>(() => { ModuleBuilder mb = myAssembly.DefineDynamicModule(null); });
        }

        [Fact]
        public void NegTest3()
        {
            AssemblyName myAsmName = new AssemblyName("TestAssembly6");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.Run);
            char[] chararray = new char[] { '\0', 't', 'e', 's', 't' };
            Assert.Throws<ArgumentException>(() => { ModuleBuilder mb = myAssembly.DefineDynamicModule(new string(chararray)); });
        }


        [Fact]
        public void NegTest4()
        {
            AssemblyName myAsmName = new AssemblyName("TestAssembly7");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder mb = myAssembly.DefineDynamicModule("module1");
            Assert.Throws<InvalidOperationException>(() => { ModuleBuilder mb2 = myAssembly.DefineDynamicModule("module2"); });
        }
    }
}
