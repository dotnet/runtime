// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Xunit;

public class CapacityTests
{
    [Fact]
    public static void TestLargeClassWithIntMethods()
    {
        // Scenario 1: allocate an instance of a class with 40000 methods that return int
        // The allocation should succeed
        var instance = CreateTypeWithMethods("LargeClassWithIntMethods", 40000, typeof(int));
        Assert.NotNull(instance);
    }

    [Fact]
    public static void TestLargeClassWithTaskMethods_Success()
    {
        // Scenario 2: allocate an instance of a class with 32750 methods that return Task
        // The allocation should succeed
        var instance = CreateTypeWithMethods("LargeClassWithTaskMethods_Success", 32750, typeof(Task));
        Assert.NotNull(instance);
    }

    [Fact]
    public static void TestLargeClassWithTaskMethods_Exception()
    {
        // Scenario 3: make a call to a method that allocates an instance of a class with 32763 methods that return Task
        // The call should throw an exception
        Assert.Throws<TypeLoadException>(() =>
        {
            CreateTypeWithMethods("LargeClassWithTaskMethods_Exception", 32763, typeof(Task));
        });
    }

    private static object CreateTypeWithMethods(string typeName, int methodCount, Type returnType)
    {
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("DynamicCapacityAssembly_" + typeName),
            AssemblyBuilderAccess.Run);

        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicCapacityModule_" + typeName);

        TypeBuilder typeBuilder = moduleBuilder.DefineType(
            typeName,
            TypeAttributes.Public);

        // Define a default constructor
        ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            Type.EmptyTypes);

        ILGenerator constructorIL = constructorBuilder.GetILGenerator();
        constructorIL.Emit(OpCodes.Ldarg_0);
        ConstructorInfo? objectCtor = typeof(object).GetConstructor(Type.EmptyTypes);
        if (objectCtor is null)
            throw new InvalidOperationException("Could not find object constructor");
        constructorIL.Emit(OpCodes.Call, objectCtor);
        constructorIL.Emit(OpCodes.Ret);

        // Generate the specified number of methods
        for (int i = 0; i < methodCount; i++)
        {
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                $"Method{i}",
                MethodAttributes.Public,
                returnType,
                Type.EmptyTypes);

            ILGenerator methodIL = methodBuilder.GetILGenerator();

            if (returnType == typeof(int))
            {
                // Return 0 for int methods
                methodIL.Emit(OpCodes.Ldc_I4_0);
                methodIL.Emit(OpCodes.Ret);
            }
            else if (returnType == typeof(Task))
            {
                // Return Task.CompletedTask for Task methods
                MethodInfo? completedTaskGetter = typeof(Task).GetProperty("CompletedTask")?.GetGetMethod();
                if (completedTaskGetter is null)
                    throw new InvalidOperationException("Could not find Task.CompletedTask getter");
                methodIL.Emit(OpCodes.Call, completedTaskGetter);
                methodIL.Emit(OpCodes.Ret);
            }
        }

        // Create the type - this is where the TypeLoadException might be thrown
        Type? createdType = typeBuilder.CreateType();
        if (createdType is null)
            throw new InvalidOperationException("Failed to create type");

        // Create an instance of the type
        object? instance = Activator.CreateInstance(createdType);
        if (instance is null)
            throw new InvalidOperationException("Failed to create instance");
        return instance;
    }
}
