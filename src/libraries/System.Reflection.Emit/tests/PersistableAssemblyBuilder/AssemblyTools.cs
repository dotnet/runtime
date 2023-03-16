// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit.Experiment.Tests
{
    internal class AssemblyTools
    {
        internal static void WriteAssemblyToDisk(AssemblyName assemblyName, Type[] types, string fileLocation)
        {
            WriteAssemblyToDisk(assemblyName, types, fileLocation, null);
        }

        internal static void WriteAssemblyToDisk(AssemblyName assemblyName, Type[] types, string fileLocation, List<CustomAttributeBuilder> assemblyAttributes)
        {
            Type assemblyType = Type.GetType(
                    "System.Reflection.Emit.Experiment.PersistableAssemblyBuilder, System.Reflection.Emit",
                    throwOnError: true)!;

            MethodInfo defineDynamicAssemblyMethod = assemblyType.GetMethod("DefineDynamicAssembly", BindingFlags.Public | BindingFlags.Static,
                new Type[] { typeof(AssemblyName), typeof(List<CustomAttributeBuilder>) });

            MethodInfo saveMethod = assemblyType.GetMethod("Save", BindingFlags.Public | BindingFlags.Instance, new Type[] { typeof(string) });

            AssemblyBuilder assemblyBuilder = (AssemblyBuilder)defineDynamicAssemblyMethod.Invoke(null, new object[] { assemblyName, assemblyAttributes });

            ModuleBuilder mb = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

            SetMembers(types, mb);

            saveMethod.Invoke(assemblyBuilder, new object[] { fileLocation });
        }

        private static void SetMembers(Type[] types, ModuleBuilder mb)
        {
            foreach (Type type in types)
            {
                TypeBuilder tb = mb.DefineType(type.FullName, type.Attributes, type.BaseType);

                var methods = type.IsInterface ? type.GetMethods() : type.GetMethods(BindingFlags.DeclaredOnly);
                foreach (var method in methods)
                {
                    var paramTypes = Array.ConvertAll(method.GetParameters(), item => item.ParameterType);
                    tb.DefineMethod(method.Name, method.Attributes, method.CallingConvention, method.ReturnType, paramTypes);
                }

                foreach (var field in type.GetFields())
                {
                    tb.DefineField(field.Name, field.FieldType, field.GetRequiredCustomModifiers(),
                        field.GetOptionalCustomModifiers(), field.Attributes);
                }
            }
        }

        internal static void WriteAssemblyToStream(AssemblyName assemblyName, Type[] types, Stream stream)
        {
            WriteAssemblyToStream(assemblyName, types, stream, null);
        }

        internal static void WriteAssemblyToStream(AssemblyName assemblyName, Type[] types, Stream stream, List<CustomAttributeBuilder>? assemblyAttributes)
        {
            Type assemblyType = Type.GetType(
                    "System.Reflection.Emit.Experiment.PersistableAssemblyBuilder, System.Reflection.Emit",
                    throwOnError: true)!;

            MethodInfo defineDynamicAssemblyMethod = assemblyType.GetMethod("DefineDynamicAssembly", BindingFlags.Public | BindingFlags.Static,
                new Type[] { typeof(AssemblyName), typeof(List<CustomAttributeBuilder>) });

            MethodInfo saveMethod = assemblyType.GetMethod("Save", BindingFlags.Public | BindingFlags.Instance, new Type[] { typeof(Stream) });

            AssemblyBuilder assemblyBuilder = (AssemblyBuilder)defineDynamicAssemblyMethod.Invoke(null, new object[] { assemblyName, assemblyAttributes });

            ModuleBuilder mb = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

            SetMembers(types, mb);

            saveMethod.Invoke(assemblyBuilder, new object[] { stream });
        }

        internal static Assembly TryLoadAssembly(string filePath)
        {
            // Get the array of runtime assemblies.
            string[] runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");

            // Create the list of assembly paths consisting of runtime assemblies and the inspected assembly.
            var paths = new List<string>(runtimeAssemblies);
            paths.Add(filePath);

            // Create PathAssemblyResolver that can resolve assemblies using the created list.
            var resolver = new PathAssemblyResolver(paths);
            var mlc = new MetadataLoadContext(resolver);

            // Load assembly into MetadataLoadContext.
            return mlc.LoadFromAssemblyPath(filePath);
        }

        internal static Assembly TryLoadAssembly(Stream stream)
        {
            var resolver = new CoreMetadataAssemblyResolver();
            var mlc = new MetadataLoadContext(resolver);

            // Load assembly into MetadataLoadContext.
            return mlc.LoadFromStream(stream);
        }
    }

    // The resolver copied from MLC tests
    public class CoreMetadataAssemblyResolver : MetadataAssemblyResolver
    {
        public CoreMetadataAssemblyResolver() { }

        public override Assembly Resolve(MetadataLoadContext context, AssemblyName assemblyName)
        {
            string name = assemblyName.Name;

            if (name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("System.Private.CoreLib", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("System.Runtime", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("netstandard", StringComparison.OrdinalIgnoreCase) ||
                // For interop attributes such as DllImport and Guid:
                name.Equals("System.Runtime.InteropServices", StringComparison.OrdinalIgnoreCase))
            {
                if (_coreAssembly == null)
                {
                    _coreAssembly = context.LoadFromStream(CreateStreamForCoreAssembly());
                }

                return _coreAssembly;
            }

            return null;
        }

        private Assembly _coreAssembly;

        public static Stream CreateStreamForCoreAssembly()
        {
            // We need a core assembly in IL form. Since this version of this code is for Jitted platforms, the System.Private.Corelib
            // of the underlying runtime will do just fine.
            if (PlatformDetection.IsNotBrowser)
            {
                string assumedLocationOfCoreLibrary = typeof(object).Assembly.Location;
                if (assumedLocationOfCoreLibrary == null || assumedLocationOfCoreLibrary == string.Empty)
                {
                    throw new Exception("Could not find a core assembly to use for tests as 'typeof(object).Assembly.Location` returned " +
                        "a null or empty value. The most likely cause is that you built the tests for a Jitted runtime but are running them " +
                        "on an AoT runtime.");
                }
            }

            return File.OpenRead(AssemblyPathHelper.GetAssemblyLocation(typeof(object).Assembly));
        }
    }
}
