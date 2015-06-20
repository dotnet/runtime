// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using TestLibrary;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderToString
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
            MethodBuilder methbuild = tpbuild.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.Static,
                                                            typeof(int), new Type[] { });

            string str = methbuild.ToString();

            Assert.True(str.LastIndexOf("Name: method1") != -1 &&
                str.LastIndexOf("Attributes: 22") != -1 &&
                str.LastIndexOf("Method Signature: Length: 3") != -1 &&
                str.LastIndexOf("Arguments: 0") != -1 &&
                str.LastIndexOf("Signature:") != -1 &&
                str.LastIndexOf("0  0  8  0") != -1);
        }

        [Fact]
        public void PosTest2()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");

            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);
            MethodBuilder methbuild = tpbuild.DefineMethod("method1", MethodAttributes.Public,
                                                            typeof(int), new Type[] { });
            methbuild.DefineGenericParameters(new string[] { "T", "U", "V" });
            methbuild.MakeGenericMethod(typeof(string), typeof(int), typeof(object));

            string str = methbuild.ToString();

            Assert.True(str.LastIndexOf("Name: method1") != -1 &&
                str.LastIndexOf("Attributes: 6") != -1 &&
                str.LastIndexOf("Method Signature: Length: 4") != -1 &&
                str.LastIndexOf("Arguments: 0") != -1 &&
                str.LastIndexOf("Signature:") != -1 &&
                str.LastIndexOf("48  3  0  8  0") != -1);
        }
    }
}
