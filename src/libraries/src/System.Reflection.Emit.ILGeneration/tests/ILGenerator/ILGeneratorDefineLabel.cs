// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using TestLibrary;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class ILGeneratorDefineLabel
    {
        private ModuleBuilder modHelper(AssemblyBuilder asmbuild)
        {
            return (ModuleBuilder)asmbuild.ManifestModule;
        }

        [Fact]
        public void PosTest1()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName(name);
            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tb = modbuild.DefineType("C1", TypeAttributes.Public);
            MethodBuilder mbuild = tb.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static, typeof(bool), new Type[] { });
            ILGenerator ilgen = mbuild.GetILGenerator();

            // if no exceptions are thrown the test passes
            // we use labels in other tests in the code so no need to verify that they were placed correctly here.
            for (int i = 0; i < 17; ++i)
            {
                ilgen.DefineLabel();
            }
        }
    }
}
