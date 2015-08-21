// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using TestLibrary;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class ILGeneratorEmitWriteLine
    {
        [Fact]
        public void EmitWriteLineTests()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName(name);

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tb = modbuild.DefineType("C1", TypeAttributes.Public);
            MethodBuilder mbuild = tb.DefineMethod("meth1", MethodAttributes.Public, typeof(int), new Type[] { });
            FieldBuilder fbuil = tb.DefineField("field1", typeof(int), FieldAttributes.Public | FieldAttributes.Static);

            int expectedRet = 1;

            // generate code for the method that we are going to use as MethodInfo in ILGenerator.Emit()
            ILGenerator ilgen = mbuild.GetILGenerator();
            ilgen.Emit(OpCodes.Ldc_I4, expectedRet);
            ilgen.Emit(OpCodes.Ret);

            // create the type where this method is in
            Type tp = tb.CreateTypeInfo().AsType();
            FieldInfo fi = tp.GetField("field1");

            TypeBuilder tb2 = modbuild.DefineType("C2", TypeAttributes.Public);
            MethodBuilder mbuild2 = tb2.DefineMethod("meth2", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[] { });

            // generate code for the method which will be invoking the first method
            ILGenerator ilgen2 = mbuild2.GetILGenerator();
            LocalBuilder lbuild = ilgen2.DeclareLocal(typeof(bool));
            ilgen2.EmitWriteLine(fi);
            ilgen2.EmitWriteLine("emitWriteLine");
            ilgen2.EmitWriteLine(lbuild);
            ilgen2.Emit(OpCodes.Ldc_I4_1);
            ilgen2.Emit(OpCodes.Ret);

            // create the type whose method will be invoking the MethodInfo method
            Type tp2 = tb2.CreateTypeInfo().AsType();

            MethodInfo md = tp2.GetMethod("meth2");

            // meth2 should invoke meth1 which should return value from meth1
            int ret = (int)md.Invoke(null, null);

            Assert.Equal(ret, expectedRet);
        }
    }
}
