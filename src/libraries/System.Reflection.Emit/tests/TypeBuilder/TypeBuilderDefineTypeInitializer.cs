// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public void TestDefineTypeInitializer()
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
        public void TestThrowsExceptionForCreateTypeCalled()
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
