// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class GenericTypeParameterBuilderSetInterfaceConstraints
    {
        [Fact]
        public void TestInterfaceConstraintsOnCustomInterface()
        {
            AssemblyName myAsmName = new AssemblyName("GenericEmitExample1");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, myAsmName.Name);

            Type baseType = typeof(ExampleBase);

            TypeBuilder myType = myModule.DefineType("Sample", TypeAttributes.Public);

            string[] typeParamNames = { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = myType.DefineGenericParameters(typeParamNames);

            GenericTypeParameterBuilder TFirst = typeParams[0];

            TFirst.SetInterfaceConstraints(typeof(IExample));
            Type type = myType.CreateTypeInfo().AsType();
            Type[] genericTypeParams = type.GetGenericArguments();
            Assert.Equal(1, genericTypeParams.Length);
            Assert.Equal(new Type[] { typeof(IExample) }, genericTypeParams[0].GetTypeInfo().GetGenericParameterConstraints());
        }

        [Fact]
        public void TestInterfaceConstraintsOnNull()
        {
            AssemblyName myAsmName = new AssemblyName("GenericEmitExample1");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, myAsmName.Name);

            Type baseType = typeof(ExampleBase);

            TypeBuilder myType = myModule.DefineType("Sample", TypeAttributes.Public);

            string[] typeParamNames = { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = myType.DefineGenericParameters(typeParamNames);

            GenericTypeParameterBuilder TFirst = typeParams[0];
            TFirst.SetInterfaceConstraints(null);
            Type type = myType.CreateTypeInfo().AsType();
            Type[] genericTypeParams = type.GetGenericArguments();
            Assert.Equal(1, genericTypeParams.Length);
            Assert.Equal(new Type[] { }, genericTypeParams[0].GetTypeInfo().GetGenericParameterConstraints());
        }


        [Fact]
        public void TestMultipleInterfaceConstraints()
        {
            AssemblyName myAsmName = new AssemblyName("GenericEmitExample1");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, myAsmName.Name);

            Type baseType = typeof(ExampleBase);

            TypeBuilder myType = myModule.DefineType("Sample", TypeAttributes.Public);

            string[] typeParamNames = { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = myType.DefineGenericParameters(typeParamNames);

            GenericTypeParameterBuilder TFirst = typeParams[0];
            TFirst.SetInterfaceConstraints(new Type[] { typeof(IExample), typeof(IExampleA) });
            Type type = myType.CreateTypeInfo().AsType();
            Type[] genericTypeParams = type.GetGenericArguments();
            Assert.Equal(1, genericTypeParams.Length);
            Assert.Equal(new Type[] { typeof(IExample), typeof(IExampleA) }, genericTypeParams[0].GetTypeInfo().GetGenericParameterConstraints());
        }
    }

    public interface IExample { }

    public interface IExampleA { }
}
