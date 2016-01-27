// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using TestLibrary;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{    
    public class ILOffsetTests
    {
        [Fact]
        public void TestPropertyBeforeEmit()
        {
            AssemblyName asmname = new AssemblyName("Assembly1");

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            MethodBuilder methbuild = modbuild.DefineGlobalMethod("method1", MethodAttributes.Public | MethodAttributes.Static, typeof(Type), new Type[] { });
            ILGenerator ilgen = methbuild.GetILGenerator();

            // since method has not been emitted.
            Assert.Equal(0, ilgen.ILOffset);
        }

        [Fact]
        public void TestPropertyAfterEmit()
        {
            AssemblyName asmName = new AssemblyName("Assembly1");
            AssemblyBuilder asmBuild = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            ModuleBuilder modBuild = TestLibrary.Utilities.GetModuleBuilder(asmBuild, "Module1");
            MethodBuilder methBuild = modBuild.DefineGlobalMethod("method1", MethodAttributes.Public | MethodAttributes.Static, typeof(Type), new Type[] { });
            ILGenerator ilgen = methBuild.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);

            Assert.Equal(1, ilgen.ILOffset);
        }
    }
}

