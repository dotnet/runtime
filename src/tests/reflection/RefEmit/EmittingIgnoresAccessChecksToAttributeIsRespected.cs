// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

class BaseClass1 { }

class Test_EmittingIgnoresAccessChecksToAttributeIsRespected
{

    public static int Main()
    {
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("testassembly"), AssemblyBuilderAccess.Run);
        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("testmodule");
        ConstructorInfo ignoreAccessChecksToAttributeCtor = DefineIgnoresAccessChecksToAttribute(moduleBuilder);

        {
            Type type = typeof(BaseClass1);
            AddInstanceOfIgnoresAccessChecksToAttribute(assemblyBuilder, ignoreAccessChecksToAttributeCtor, type.Assembly);
            TypeBuilder typeBuilder = moduleBuilder.DefineType("DerivedTypeFor" + type.Name, TypeAttributes.Public, type);
            typeBuilder.CreateType();
        }

        {
            Type type = typeof(BaseClass2);
            AddInstanceOfIgnoresAccessChecksToAttribute(assemblyBuilder, ignoreAccessChecksToAttributeCtor, type.Assembly);
            TypeBuilder typeBuilder = moduleBuilder.DefineType("DerivedTypeFor" + type.Name, TypeAttributes.Public, type);
            typeBuilder.CreateType();
        }
        Console.WriteLine("PASS");
        return 100;
    }

    static void AddInstanceOfIgnoresAccessChecksToAttribute(AssemblyBuilder assemblyBuilder, ConstructorInfo ignoreAccessChecksToAttributeCtor, Assembly assembly)
    {
        // Add this assembly level attribute:
        // [assembly: System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute(assemblyName)]
        ConstructorInfo attributeConstructor = ignoreAccessChecksToAttributeCtor;
        CustomAttributeBuilder customAttributeBuilder =
            new CustomAttributeBuilder(attributeConstructor, new object[] { assembly.GetName().Name });
        assemblyBuilder.SetCustomAttribute(customAttributeBuilder);
    }

    static ConstructorInfo DefineIgnoresAccessChecksToAttribute(ModuleBuilder mb)
    {
        TypeBuilder attributeTypeBuilder =
            mb.DefineType("System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute",
                           TypeAttributes.Public | TypeAttributes.Class,
                           typeof(Attribute));

        // Create backing field as:
        // private string assemblyName;
        FieldBuilder assemblyNameField =
            attributeTypeBuilder.DefineField("assemblyName", typeof(string), FieldAttributes.Private);

        // Create ctor as:
        // public IgnoresAccessChecksToAttribute(string)
        ConstructorBuilder constructorBuilder = attributeTypeBuilder.DefineConstructor(MethodAttributes.Public,
                                                     CallingConventions.HasThis,
                                                     new Type[] { assemblyNameField.FieldType });

        ILGenerator il = constructorBuilder.GetILGenerator();

        // Create ctor body as:
        // this.assemblyName = {ctor parameter 0}
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg, 1);
        il.Emit(OpCodes.Stfld, assemblyNameField);

        // return
        il.Emit(OpCodes.Ret);

        // Define property as:
        // public string AssemblyName {get { return this.assemblyName; } }
        PropertyBuilder propertyBuilder = attributeTypeBuilder.DefineProperty(
                "AssemblyName",
                PropertyAttributes.None,
                CallingConventions.HasThis,
                returnType: typeof(string),
                parameterTypes: null);

        MethodBuilder getterMethodBuilder = attributeTypeBuilder.DefineMethod(
                                               "get_AssemblyName",
                                               MethodAttributes.Public,
                                               CallingConventions.HasThis,
                                               returnType: typeof(string),
                                               parameterTypes: null);
        propertyBuilder.SetGetMethod(getterMethodBuilder);

        // Generate body:
        // return this.assemblyName;
        il = getterMethodBuilder.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, assemblyNameField);
        il.Emit(OpCodes.Ret);

        // Generate the AttributeUsage attribute for this attribute type:
        // [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
        TypeInfo attributeUsageTypeInfo = typeof(AttributeUsageAttribute).GetTypeInfo();

        // Find the ctor that takes only AttributeTargets
        ConstructorInfo attributeUsageConstructorInfo =
            attributeUsageTypeInfo.DeclaredConstructors
                .Single(c => c.GetParameters().Length == 1 &&
                             c.GetParameters()[0].ParameterType == typeof(AttributeTargets));

        // Find the property to set AllowMultiple
        PropertyInfo allowMultipleProperty =
            attributeUsageTypeInfo.DeclaredProperties
                .Single(f => string.Equals(f.Name, "AllowMultiple"));

        // Create a builder to construct the instance via the ctor and property
        CustomAttributeBuilder customAttributeBuilder =
            new CustomAttributeBuilder(attributeUsageConstructorInfo,
                                        new object[] { AttributeTargets.Assembly },
                                        new PropertyInfo[] { allowMultipleProperty },
                                        new object[] { true });

        // Attach this attribute instance to the newly defined attribute type
        attributeTypeBuilder.SetCustomAttribute(customAttributeBuilder);

        // Make the TypeInfo real so the constructor can be used.
        return attributeTypeBuilder.CreateTypeInfo().DeclaredConstructors.Single();
    }
}


