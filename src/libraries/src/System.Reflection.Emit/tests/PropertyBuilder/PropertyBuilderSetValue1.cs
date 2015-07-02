// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class PropertyBuilderTest16
    {
        private const string DynamicAssemblyName = "TestDynamicAssembly";
        private const string DynamicModuleName = "TestDynamicModule";
        private const string DynamicTypeName = "TestDynamicType";
        private const string DynamicFieldName = "TestDynamicFieldA";
        private const string DynamicPropertyName = "TestDynamicProperty";
        private const string DynamicMethodName = "DynamicMethodA";

        private TypeBuilder GetTypeBuilder(TypeAttributes typeAtt)
        {
            AssemblyName myAssemblyName = new AssemblyName();
            myAssemblyName.Name = DynamicAssemblyName;
            AssemblyBuilder myAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(myAssemblyName,
                                                                            AssemblyBuilderAccess.Run);

            ModuleBuilder myModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(myAssemblyBuilder,
                                                                                DynamicModuleName);

            return myModuleBuilder.DefineType(DynamicTypeName, typeAtt);
        }

        [Fact]
        public void TestThrowsExceptionForNotSupported()
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;
            FieldBuilder myFieldBuilder;
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;
            myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);
            myFieldBuilder = myTypeBuilder.DefineField(DynamicFieldName,
                                                       typeof(int),
                                                       FieldAttributes.Private);
            myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                             PropertyAttributes.None,
                                                             typeof(int),
                                                             null);
            myMethodBuilder = myTypeBuilder.DefineMethod(DynamicMethodName,
                                             getMethodAttr,
                                             typeof(int),
                                             null);
            ILGenerator methodILGenerator = myMethodBuilder.GetILGenerator();

            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ldfld, myFieldBuilder);
            methodILGenerator.Emit(OpCodes.Ret);
            myPropertyBuilder.SetGetMethod(myMethodBuilder);
            Type myType = myTypeBuilder.CreateTypeInfo().AsType();
            object obj = myType.GetConstructor(new Type[0]).Invoke(null);
            Assert.Throws<NotSupportedException>(() => { myPropertyBuilder.SetValue(obj, 99, null); });
        }
    }
}
