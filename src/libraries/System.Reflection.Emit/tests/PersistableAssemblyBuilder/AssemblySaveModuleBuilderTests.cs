// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class AssemblySaveModuleBuilderTests
    {
        [Fact]
        public void DefineGlobalMethodAndCreateGlobalFunctionsTest()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndSaveMethod(new AssemblyName("MyAssembly"), out MethodInfo saveMethod);
                ModuleBuilder module = ab.DefineDynamicModule("MyModule");
                MethodBuilder method = module.DefineGlobalMethod("TestMethod", MethodAttributes.Static | MethodAttributes.Public, null, null);
                ILGenerator ilGenerator = method.GetILGenerator();
                ilGenerator.EmitWriteLine("Hello World from global method.");
                ilGenerator.Emit(OpCodes.Ret);

                MethodBuilder method2 = module.DefineGlobalMethod("MyMethod", MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, typeof(int), [typeof(string), typeof(int)]);
                ilGenerator = method2.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", [typeof(string)]));
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Ret);

                module.CreateGlobalFunctions();
                Assert.Null(method.DeclaringType);
                Assert.Equal(typeof(void), method.ReturnType);
                Assert.Equal(method, module.GetMethod("TestMethod"));
                Assert.Equal(method2, module.GetMethod("MyMethod", [typeof(string), typeof(int)]));
                Assert.Equal(2, module.GetMethods().Length);

                saveMethod.Invoke(ab, [file.Path]);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Assembly assemblyFromDisk = mlc.LoadFromAssemblyPath(file.Path);
                    MethodInfo globalMethod1 = assemblyFromDisk.ManifestModule.GetMethod("TestMethod");
                    Assert.NotNull(globalMethod1);
                    Assert.True(globalMethod1.IsStatic);
                    Assert.True(globalMethod1.IsPublic);
                    Assert.Equal(0, globalMethod1.GetParameters().Length);
                    Assert.Equal(method.ReturnType.FullName, globalMethod1.ReturnType.FullName);
                    Assert.NotNull(globalMethod1.DeclaringType);

                    MethodInfo globalMethod2 = assemblyFromDisk.ManifestModule.GetMethod("MyMethod");
                    Assert.NotNull(globalMethod2);
                    Assert.True(globalMethod2.IsStatic);
                    Assert.True(globalMethod2.IsPublic);
                    Assert.Equal(2, globalMethod2.GetParameters().Length);
                    Assert.Equal(method.ReturnType.FullName, globalMethod1.ReturnType.FullName);
                    Assert.Equal("<Module>", globalMethod2.DeclaringType.Name);
                }
            }
        }

        [Fact]
        public void DefineGlobalMethodAndCreateGlobalFunctions_Validations()
        {
            AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndSaveMethod(new AssemblyName("MyAssembly"), out MethodInfo _);
            ModuleBuilder module = ab.DefineDynamicModule("MyModule");
            Assert.Throws<ArgumentException>(() => module.DefineGlobalMethod("TestMethod", MethodAttributes.Public, null, null)); // must be static
            MethodBuilder method = module.DefineGlobalMethod("TestMethod", MethodAttributes.Static | MethodAttributes.Public, null, null);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.EmitWriteLine("Hello World from global method.");
            ilGenerator.Emit(OpCodes.Ret);

            Assert.Throws<NotSupportedException>(() => module.GetMethod("TestMethod")); // not supported when not created
            Assert.Throws<NotSupportedException>(() => module.GetMethods());
            module.CreateGlobalFunctions();

            Assert.Null(method.DeclaringType);
            Assert.Throws<InvalidOperationException>(() => module.CreateGlobalFunctions()); // already created
            Assert.Throws<InvalidOperationException>(() => module.DefineGlobalMethod("TestMethod2", MethodAttributes.Static | MethodAttributes.Public, null, null));
        }

        [Fact]
        public static void DefinePInvokeMethodTest()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndSaveMethod(new AssemblyName("MyAssembly"), out MethodInfo saveMethod);
                DpmParams p = new DpmParams() { MethodName = "A2", LibName = "Foo2.dll", EntrypointName = "Wha2", ReturnType = typeof(int),
                    ParameterTypes = [typeof(int)], NativeCallConv = CallingConvention.Cdecl };

                ModuleBuilder modb = ab.DefineDynamicModule("MyModule");
                MethodBuilder mb = modb.DefinePInvokeMethod(p.MethodName, p.LibName, p.EntrypointName, p.Attributes, p.ManagedCallConv, p.ReturnType,
                    p.ParameterTypes, p.NativeCallConv, p.Charset);
                mb.SetImplementationFlags(mb.GetMethodImplementationFlags() | MethodImplAttributes.PreserveSig);

                modb.CreateGlobalFunctions();
                saveMethod.Invoke(ab, [file.Path]);
                MethodInfo m = modb.GetMethod(p.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, CallingConventions.Any, p.ParameterTypes, null);
                Assert.NotNull(m);

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Module moduleFromDisk = mlc.LoadFromAssemblyPath(file.Path).GetModule("MyModule");
                    Assert.Equal(1, moduleFromDisk.GetMethods().Length);
                    MethodInfo method = moduleFromDisk.GetMethod(p.MethodName);
                    Assert.NotNull(method);
                    AssemblySaveMethodBuilderTests.VerifyPInvokeMethod(method.DeclaringType, method, p, mlc.CoreAssembly);
                }
            }
        }
    }
}
