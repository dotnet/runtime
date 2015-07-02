// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ConstructorBuilderSetImplementationFlags
    {
        [Fact]
        public void TestImplFlags()
        {
            AssemblyName myAssemblyName = new AssemblyName();
            myAssemblyName.Name = "TempAssembly";
            AssemblyBuilder myAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(myAssemblyName, AssemblyBuilderAccess.Run);

            ModuleBuilder myModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(myAssemblyBuilder, "Module1");

            TypeBuilder myTypeBuilder = myModuleBuilder.DefineType("TempClass", TypeAttributes.Public);
            ConstructorBuilder myConstructor = myTypeBuilder.DefineConstructor(
               MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(string) });

            myConstructor.SetImplementationFlags(MethodImplAttributes.Runtime);

            MethodImplAttributes myMethodAttributes = myConstructor.MethodImplementationFlags;

            int myAttribValue = (int)myMethodAttributes;

            FieldInfo[] myFieldInfo = typeof(MethodImplAttributes).GetFields(BindingFlags.Public | BindingFlags.Static);

            for (int i = 0; i < myFieldInfo.Length; i++)
            {
                if (myFieldInfo[i].Name == "Runtime")
                {
                    int myFieldValue = (int)myFieldInfo[i].GetValue(null);
                    Assert.Equal(myFieldValue, (myFieldValue & myAttribValue));
                }
            }
        }

        [Fact]
        public void TestImplFlagsNotChanged()
        {
            AssemblyName myAssemblyName = new AssemblyName();
            myAssemblyName.Name = "TempAssembly";
            AssemblyBuilder myAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(myAssemblyName, AssemblyBuilderAccess.Run);

            ModuleBuilder myModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(myAssemblyBuilder, "Module1");

            TypeBuilder myTypeBuilder = myModuleBuilder.DefineType("TempClass", TypeAttributes.Public);
            ConstructorBuilder myConstructor = myTypeBuilder.DefineConstructor(
               MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(string) });

            MethodImplAttributes myMethodAttributes = myConstructor.MethodImplementationFlags;

            int myAttribValue = (int)myMethodAttributes;

            FieldInfo[] myFieldInfo = typeof(MethodImplAttributes).GetFields(BindingFlags.Public | BindingFlags.Static);

            for (int i = 0; i < myFieldInfo.Length; i++)
            {
                if (myFieldInfo[i].Name == "Runtime")
                {
                    int myFieldValue = (int)myFieldInfo[i].GetValue(null);
                    Assert.NotEqual((myFieldValue & myAttribValue), myFieldValue);
                }
            }
        }

        [Fact]
        public void TestThrowsExceptionOnCreateTypeCalled()
        {
            AssemblyName myAssemblyName = new AssemblyName();
            myAssemblyName.Name = "TempAssembly";
            AssemblyBuilder myAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(myAssemblyName, AssemblyBuilderAccess.Run);

            ModuleBuilder myModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(myAssemblyBuilder, "Module1");

            TypeBuilder myTypeBuilder = myModuleBuilder.DefineType("TempClass", TypeAttributes.Public);

            ConstructorBuilder myConstructor = myTypeBuilder.DefineConstructor(
               MethodAttributes.Public, CallingConventions.Standard, new Type[] { });
            ILGenerator myILGenerator = myConstructor.GetILGenerator();
            myILGenerator.Emit(OpCodes.Ldarg_1);
            Type myType = myTypeBuilder.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => { myConstructor.SetImplementationFlags(MethodImplAttributes.Runtime); });
        }
    }
}
