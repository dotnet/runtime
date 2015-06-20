// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using TestLibrary;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderDefineTypeInitializer
    {
        [Fact]
        public void PosTest1()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);

            // Define a private string field named "Greeting" in the type.
            FieldBuilder greetingField = tpbuild.DefineField("Greeting", typeof(string),
                                                                     FieldAttributes.Private | FieldAttributes.Static);

            // Create the constructor.
            ConstructorBuilder constructor = tpbuild.DefineTypeInitializer();

            // Generate IL for the method. The constructor calls its base class
            // constructor. The constructor stores its argument in the private field.

            ILGenerator constructorIL = constructor.GetILGenerator();
            constructorIL.Emit(OpCodes.Ldstr, "hello");
            constructorIL.Emit(OpCodes.Stsfld, greetingField);
            constructorIL.Emit(OpCodes.Ret);

            Type tp = tpbuild.CreateTypeInfo().AsType();

            FieldInfo fi = tp.GetField("Greeting", BindingFlags.NonPublic | BindingFlags.Static);
            string fieldVal = (string)fi.GetValue(Activator.CreateInstance(tp));


            Assert.Equal("hello", fieldVal);
        }

        [Fact]
        public void NegTest1()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);

            tpbuild.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() => { tpbuild.DefineTypeInitializer(); });
        }
    }
}
