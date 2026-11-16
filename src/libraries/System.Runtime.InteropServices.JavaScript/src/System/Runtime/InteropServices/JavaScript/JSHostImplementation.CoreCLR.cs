// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    internal static partial class JSHostImplementation
    {
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "It's kept from trimming by ModuleInitializerAttribute in the generated code.")]
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "It's kept from trimming by ModuleInitializerAttribute in the generated code.")]
        public static Task BindAssemblyExports(string? assemblyName)
        {
            ArgumentException.ThrowIfNullOrEmpty(assemblyName);

            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(assemblyName));
            Type? generatedInitializerType = assembly.GetType("System.Runtime.InteropServices.JavaScript.__GeneratedInitializer", throwOnError: true);
            if (generatedInitializerType == null)
            {
                throw new ArithmeticException($"BindAssemblyExports: __GeneratedInitializer type not found in assembly '{assemblyName}'");
            }
            MethodInfo? registerMethod = generatedInitializerType.GetMethod("__Register_", BindingFlags.NonPublic | BindingFlags.Static, binder: null, Type.EmptyTypes, modifiers: null);
            if (registerMethod == null)
            {
                throw new ArithmeticException($"BindAssemblyExports: __Register_ method not found in type '{generatedInitializerType.FullName}' in assembly '{assemblyName}'");
            }
            registerMethod.Invoke(null, null);
            return Task.CompletedTask;
        }
    }
}
