// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Reflection;
using System.Reflection.Emit;

namespace System.Runtime.Loader.Tests
{
    public class TestClass
    {
        public static Assembly LoadFromDefaultContext(string assemblyNameStr)
        {
            var assemblyName = new AssemblyName(assemblyNameStr);
            return Assembly.Load(assemblyName);
        }

        public static Assembly GetRefEmitAssembly(string assemblyNameStr, AssemblyBuilderAccess builderType)
        {
            var assemblyName = new AssemblyName(assemblyNameStr);

            AssemblyBuilder builder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, builderType);
            ModuleBuilder moduleBuilder = builder.DefineDynamicModule("RefEmitModule");

            TypeBuilder typeBuilder = moduleBuilder.DefineType("RefEmitTestType", TypeAttributes.Public);

            // Define "Assembly LoadStaticAssembly(string)" method that will load a static assembly
            MethodBuilder methodBuilder = typeBuilder.DefineMethod("LoadStaticAssembly", MethodAttributes.Public|MethodAttributes.Static, typeof(Assembly), new Type[]{typeof(string)});
            ILGenerator ilGenerator = methodBuilder.GetILGenerator();

            // Generate the following code:
            //
            // AssemblyName name = new AssemblyName(name); // name is the argument passed to LoadStaticAssembly
            // Assembly.Load(name);

            // Declare the locals
            LocalBuilder localAssemblyName = ilGenerator.DeclareLocal(typeof(AssemblyName));
            LocalBuilder localAssembly = ilGenerator.DeclareLocal(typeof(Assembly));

            // Fetch reference to AssemblyName ctor we want to invoke
            ConstructorInfo ctorAssemblyName = TypeExtensions.GetConstructor(typeof(System.Reflection.AssemblyName), new Type[] {typeof(string)});

            // Load incoming assemblyname string
            ilGenerator.Emit(OpCodes.Ldarg_0);

            // Create new object of the type AssemblyName
            ilGenerator.Emit(OpCodes.Newobj, ctorAssemblyName);

            // "this" for assemblyname instance is already at the top of evaluation stack.
            //
            // Invoke Assembly.Load
            MethodInfo miAssemblyLoad = TypeExtensions.GetMethod(typeof(System.Reflection.Assembly), "Load", new Type[] {typeof(System.Reflection.AssemblyName)});
            ilGenerator.Emit(OpCodes.Call, miAssemblyLoad);

            // Return the reference to the loaded assembly
            ilGenerator.Emit(OpCodes.Ret);

            // Generate the type and return the associated assembly
            return typeBuilder.CreateType().Assembly;
        }
    }
}
