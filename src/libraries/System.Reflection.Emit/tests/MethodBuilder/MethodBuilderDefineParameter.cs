// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using TestLibrary;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderDefineParameter
    {
        [Fact]
        public void TestDefineParameter()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;
            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);

            Type[] paramTypes = new Type[] { typeof(string), typeof(object) };
            string[] paramNames = new string[] { "Param1", "Param2" };
            ParameterAttributes[] paramAttrs = new ParameterAttributes[] { ParameterAttributes.In, ParameterAttributes.Out };
            Type returnType = typeof(int);
            ParameterAttributes returnAttrs = ParameterAttributes.None;

            MethodBuilder methbuild = tpbuild.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static,
                                                            returnType, paramTypes);

            methbuild.DefineParameter(0, returnAttrs, "returnName");
            methbuild.DefineParameter(1, paramAttrs[0], paramNames[0]);
            methbuild.DefineParameter(2, paramAttrs[1], paramNames[1]);

            int expectedRet = 3;
            ILGenerator ilgen = methbuild.GetILGenerator();
            ilgen.Emit(OpCodes.Ldc_I4, expectedRet);
            ilgen.Emit(OpCodes.Ret);

            Type tp = tpbuild.CreateTypeInfo().AsType();

            ParameterInfo[] paramArray = methbuild.GetParameters();

            Assert.Equal("System.String Param1", paramArray[0].ToString());
            Assert.Equal("System.Object Param2", paramArray[1].ToString());
            // invoke method to verify it still works correctly
            MethodInfo mi = tp.GetMethod("meth1");
            int ret = (int)mi.Invoke(null, new object[] { "hello", new object() });
            Assert.Equal(expectedRet, ret);
        }

        [Fact]
        public void TestThrowsExceptionForTypeCreated()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);

            Type[] paramTypes = new Type[] { typeof(string), typeof(object) };
            string[] paramNames = new string[] { "Param1", "Param2" };
            ParameterAttributes[] paramAttrs = new ParameterAttributes[] { ParameterAttributes.In, ParameterAttributes.Out };
            Type returnType = typeof(int);

            MethodBuilder methbuild = tpbuild.DefineMethod("meth1", MethodAttributes.Public, returnType, paramTypes);
            ILGenerator ilgen = methbuild.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);

            Type tp = tpbuild.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() => { methbuild.DefineParameter(1, ParameterAttributes.Retval, "param1"); });
        }

        [Fact]
        public void TestThrowsExceptionForNegativePosition()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;
            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");

            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);

            Type[] paramTypes = new Type[] { typeof(string), typeof(object) };
            string[] paramNames = new string[] { "Param1", "Param2" };
            ParameterAttributes[] paramAttrs = new ParameterAttributes[] { ParameterAttributes.In, ParameterAttributes.Out };
            Type returnType = typeof(int);

            MethodBuilder methbuild = tpbuild.DefineMethod("meth1", MethodAttributes.Public, returnType, paramTypes);

            Assert.Throws<ArgumentOutOfRangeException>(() => { methbuild.DefineParameter(-1, ParameterAttributes.None, "Param1"); });
        }

        [Fact]
        public void TestThrowsExceptionForNoParameters()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;
            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);

            MethodBuilder methbuild = tpbuild.DefineMethod("meth1", MethodAttributes.Public);

            Assert.Throws<ArgumentOutOfRangeException>(() => { methbuild.DefineParameter(1, ParameterAttributes.None, "Param1"); });
        }

        [Fact]
        public void TestThrowsExceptionForPositionGreaterThanNumberOfParameters()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;
            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");

            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);

            Type[] paramTypes = new Type[] { typeof(string), typeof(object) };
            string[] paramNames = new string[] { "Param1", "Param2" };
            ParameterAttributes[] paramAttrs = new ParameterAttributes[] { ParameterAttributes.In, ParameterAttributes.Out };
            Type returnType = typeof(int);

            MethodBuilder methbuild = tpbuild.DefineMethod("meth1", MethodAttributes.Public, returnType, paramTypes);

            Assert.Throws<ArgumentOutOfRangeException>(() => { methbuild.DefineParameter(3, ParameterAttributes.None, "Param1"); });
        }
    }
}
