// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderIsGenericTypeDefinition
    {
        [Fact]
        public void IsGenericTypeDefinition_GenericType_ReturnsTrue()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            TypeBuilder type1 = module.DefineType("Sample", TypeAttributes.Class | TypeAttributes.Public);
            GenericTypeParameterBuilder[] typeParams = type1.DefineGenericParameters("T");

            ConstructorBuilder ctor = type1.DefineDefaultConstructor(MethodAttributes.PrivateScope | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            FieldBuilder field = type1.DefineField("Field", typeParams[0].AsType(), FieldAttributes.Public);

            MethodBuilder genericMethod = type1.DefineMethod("GM", MethodAttributes.Public | MethodAttributes.Static);
            GenericTypeParameterBuilder[] methodParams = genericMethod.DefineGenericParameters("U");
            genericMethod.SetSignature(null, null, null, new Type[] { methodParams[0].AsType() }, null, null);

            ILGenerator ilGenerator = genericMethod.GetILGenerator();
            Type genericUType = type1.MakeGenericType(methodParams[0].AsType());
            ilGenerator.DeclareLocal(genericUType);

            ConstructorInfo genericUConstructor = TypeBuilder.GetConstructor(genericUType, ctor);
            ilGenerator.Emit(OpCodes.Newobj, genericUConstructor);

            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ldarg_0);

            FieldInfo genericUField = TypeBuilder.GetField(genericUType, field);

            ilGenerator.Emit(OpCodes.Stfld, genericUField);

            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ldfld, genericUField);
            ilGenerator.Emit(OpCodes.Box, methodParams[0].AsType());

            MethodInfo writeLineObj = typeof(Console).GetMethod("WriteLine", new Type[] { typeof(object) });
            ilGenerator.EmitCall(OpCodes.Call, writeLineObj, null);
            ilGenerator.Emit(OpCodes.Ret);

            TypeBuilder type2 = module.DefineType("Dummy", TypeAttributes.Class | TypeAttributes.NotPublic);
            MethodBuilder entryPoint = type2.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, null, null);
            ilGenerator = entryPoint.GetILGenerator();

            Type genericIntType = type1.MakeGenericType(typeof(int));
            MethodInfo genericIntMethod = TypeBuilder.GetMethod(genericIntType, genericMethod);
            MethodInfo genericStringMethod = genericIntMethod.MakeGenericMethod(typeof(string));

            ilGenerator.Emit(OpCodes.Ldstr, "Hello, world!");
            ilGenerator.EmitCall(OpCodes.Call, genericStringMethod, null);
            ilGenerator.Emit(OpCodes.Ret);

            type1.CreateType();
            Assert.True(type1.IsGenericTypeDefinition);
        }

        [Fact]
        public void IsGenericTypeDefinition_NonGenericType_ReturnsFalse()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, null, null);

            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldstr, "Test string here.");
            MethodInfo writeLine = typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) });
            ilGenerator.EmitCall(OpCodes.Call, writeLine, null);
            ilGenerator.Emit(OpCodes.Ret);

            type.CreateType();
            Assert.False(type.IsGenericTypeDefinition);
        }
    }
}
