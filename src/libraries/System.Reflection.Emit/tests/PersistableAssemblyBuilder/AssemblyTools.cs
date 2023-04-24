// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace System.Reflection.Emit.Tests
{
    internal static class AssemblyTools
    {
        internal static void WriteAssemblyToDisk(AssemblyName assemblyName, Type[] types, string fileLocation)
        {
            WriteAssemblyToDisk(assemblyName, types, fileLocation, null);
        }

        internal static void WriteAssemblyToDisk(AssemblyName assemblyName, Type[] types, string fileLocation, List<CustomAttributeBuilder> assemblyAttributes)
        {
            MethodInfo defineDynamicAssemblyMethod = PopulateMethods(typeof(string), out MethodInfo saveMethod);

            AssemblyBuilder assemblyBuilder = (AssemblyBuilder)defineDynamicAssemblyMethod.Invoke(null,
                new object[] { assemblyName, CoreMetadataAssemblyResolver.s_coreAssembly, assemblyAttributes });
            ModuleBuilder mb = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

            PopulateMembersForModule(types, mb);

            saveMethod.Invoke(assemblyBuilder, new object[] { fileLocation });
        }

        private static void PopulateMembersForModule(Type[] types, ModuleBuilder mb)
        {
            foreach (Type type in types)
            {
                TypeBuilder tb = mb.DefineType(type.FullName, type.Attributes, type.BaseType);

                MethodInfo[] methods = type.IsInterface ? type.GetMethods() : type.GetMethods(BindingFlags.DeclaredOnly);
                foreach (var method in methods)
                {
                    MethodBuilder meb = tb.DefineMethod(method.Name, method.Attributes, method.CallingConvention, method.ReturnType, null);
                }

                foreach (FieldInfo field in type.GetFields())
                {
                    tb.DefineField(field.Name, field.FieldType, field.Attributes);
                }
            }
        }

        internal static void WriteAssemblyToStream(AssemblyName assemblyName, Type[] types, Stream stream)
        {
            WriteAssemblyToStream(assemblyName, types, stream, null);
        }

        internal static void WriteAssemblyToStream(AssemblyName assemblyName, Type[] types, Stream stream, List<CustomAttributeBuilder>? assemblyAttributes)
        {
            MethodInfo defineDynamicAssemblyMethod = PopulateMethods(typeof(Stream), out MethodInfo saveMethod);

            AssemblyBuilder assemblyBuilder = (AssemblyBuilder)defineDynamicAssemblyMethod.Invoke(null,
                new object[] { assemblyName, CoreMetadataAssemblyResolver.s_coreAssembly, assemblyAttributes });

            ModuleBuilder mb = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

            PopulateMembersForModule(types, mb);

            saveMethod.Invoke(assemblyBuilder, new object[] { stream });
        }

        internal static MethodInfo PopulateMethods(Type parameterType, out MethodInfo saveMethod)
        {
            Type assemblyType = Type.GetType(
                    "System.Reflection.Emit.AssemblyBuilderImpl, System.Reflection.Emit",
                    throwOnError: true)!;

            saveMethod = assemblyType.GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance, new Type[] { parameterType });

            return assemblyType.GetMethod("DefinePersistedAssembly", BindingFlags.NonPublic | BindingFlags.Static,
                new Type[] { typeof(AssemblyName), typeof(Assembly), typeof(List<CustomAttributeBuilder>) });
        }

        internal static Assembly LoadAssemblyFromPath(string filePath) =>
            new MetadataLoadContext(new CoreMetadataAssemblyResolver()).LoadFromAssemblyPath(filePath);

        internal static Assembly LoadAssemblyFromStream(Stream stream) =>
            new MetadataLoadContext(new CoreMetadataAssemblyResolver()).LoadFromStream(stream);
    }

    // The resolver copied from MLC tests
    internal sealed class CoreMetadataAssemblyResolver : MetadataAssemblyResolver
    {
        public static Assembly s_coreAssembly = typeof(object).Assembly;
        public static Assembly s_emitAssembly = typeof(AssemblyTools).Assembly;
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

            if (name.Equals("System.Reflection.Emit.Tests", StringComparison.OrdinalIgnoreCase))
            {
                if (_emitAssembly == null)
                {
                    _emitAssembly = context.LoadFromStream(CreateStreamForEmitAssembly());
                }

                return _emitAssembly;
            }

            return null;
        }

        private Assembly _emitAssembly;
        private Assembly _coreAssembly;

        private Stream CreateStreamForEmitAssembly() =>
            File.OpenRead(AssemblyPathHelper.GetAssemblyLocation(s_emitAssembly));

        private static Stream CreateStreamForCoreAssembly()
        {
            // We need a core assembly in IL form. Since this version of this code is for Jitted platforms, the System.Private.Corelib
            // of the underlying runtime will do just fine.
            if (PlatformDetection.IsNotBrowser)
            {
                string assumedLocationOfCoreLibrary = typeof(object).Assembly.Location;
                if (string.IsNullOrEmpty(assumedLocationOfCoreLibrary))
                {
                    throw new Exception("Could not find a core assembly to use for tests as 'typeof(object).Assembly.Location` returned " +
                        "a null or empty value. The most likely cause is that you built the tests for a Jitted runtime but are running them " +
                        "on an AoT runtime.");
                }
            }

            return File.OpenRead(AssemblyPathHelper.GetAssemblyLocation(s_coreAssembly));
        }
    }
}
