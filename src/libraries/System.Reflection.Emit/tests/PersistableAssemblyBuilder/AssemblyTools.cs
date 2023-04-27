// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    internal static class AssemblyTools
    {
        internal static void WriteAssemblyToDisk(AssemblyName assemblyName, Type[] types, string fileLocation)
        {
            AssemblyBuilder assemblyBuilder = PopulateAssemblyBuilderAndSaveMethod(
                assemblyName, null, typeof(string), out MethodInfo saveMethod);

            ModuleBuilder mb = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
            PopulateMembersForModule(mb, types);

            saveMethod.Invoke(assemblyBuilder, new object[] { fileLocation });
        }

        private static void PopulateMembersForModule(ModuleBuilder mb, Type[] types)
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
            AssemblyBuilder assemblyBuilder = PopulateAssemblyBuilderAndSaveMethod(
                assemblyName, null, typeof(Stream), out MethodInfo saveMethod);

            ModuleBuilder mb = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
            PopulateMembersForModule(mb, types);

            saveMethod.Invoke(assemblyBuilder, new object[] { stream });
        }

        internal static AssemblyBuilder PopulateAssemblyBuilderAndSaveMethod(AssemblyName assemblyName,
            List<CustomAttributeBuilder>? assemblyAttributes, Type parameterType, out MethodInfo saveMethod)
        {
            Type assemblyType = Type.GetType("System.Reflection.Emit.AssemblyBuilderImpl, System.Reflection.Emit", throwOnError: true)!;

            saveMethod = assemblyType.GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance, new Type[] { parameterType });

            MethodInfo defineDynamicAssemblyMethod = assemblyType.GetMethod("DefinePersistedAssembly", BindingFlags.NonPublic | BindingFlags.Static,
                new Type[] { typeof(AssemblyName), typeof(Assembly), typeof(List<CustomAttributeBuilder>) });

            return (AssemblyBuilder)defineDynamicAssemblyMethod.Invoke(null,
                new object[] { assemblyName, CoreMetadataAssemblyResolver.s_coreAssembly, assemblyAttributes });
        }

        internal static Assembly LoadAssemblyFromPath(string filePath) =>
            new MetadataLoadContext(new CoreMetadataAssemblyResolver()).LoadFromAssemblyPath(filePath);

        internal static Assembly LoadAssemblyFromStream(Stream stream) =>
            new MetadataLoadContext(new CoreMetadataAssemblyResolver()).LoadFromStream(stream);

        internal static void AssertAssemblyNameAndModule(AssemblyName sourceAName, AssemblyName aNameFromDisk, Module moduleFromDisk)
        {
            // Runtime assemblies adding AssemblyNameFlags.PublicKey in Assembly.GetName() overloads
            Assert.Equal(sourceAName.Flags | AssemblyNameFlags.PublicKey, aNameFromDisk.Flags);
            Assert.Equal(sourceAName.Name, aNameFromDisk.Name);
            Assert.Equal(sourceAName.Version, aNameFromDisk.Version);
            Assert.Equal(sourceAName.CultureInfo, aNameFromDisk.CultureInfo);
            Assert.Equal(sourceAName.CultureName, aNameFromDisk.CultureName);
            Assert.Equal(sourceAName.ContentType, aNameFromDisk.ContentType);

            Assert.NotNull(moduleFromDisk);
            Assert.Equal(sourceAName.Name, moduleFromDisk.ScopeName);
            Assert.Empty(moduleFromDisk.GetTypes());
        }

        internal static void AssertTypeProperties(Type sourceType, Type typeFromDisk)
        {
            Assert.Equal(sourceType.Name, typeFromDisk.Name);
            Assert.Equal(sourceType.Namespace, typeFromDisk.Namespace);
            Assert.Equal(sourceType.Attributes, typeFromDisk.Attributes);
            Assert.Equal(sourceType.IsInterface, typeFromDisk.IsInterface);
            Assert.Equal(sourceType.IsValueType, typeFromDisk.IsValueType);
        }

        internal static void AssertFields(FieldInfo[] declaredFields, FieldInfo[] fieldsFromDisk)
        {
            Assert.Equal(declaredFields.Length, fieldsFromDisk.Length);

            for (int j = 0; j < declaredFields.Length; j++)
            {
                FieldInfo sourceField = declaredFields[j];
                FieldInfo fieldFromDisk = fieldsFromDisk[j];

                Assert.Equal(sourceField.Name, fieldFromDisk.Name);
                Assert.Equal(sourceField.Attributes, fieldFromDisk.Attributes);
                Assert.Equal(sourceField.FieldType.FullName, fieldFromDisk.FieldType.FullName);
            }
        }

        internal static void AssertMethods(MethodInfo[] sourceMethods, MethodInfo[] methodsFromDisk)
        {
            Assert.Equal(sourceMethods.Length, methodsFromDisk.Length);

            for (int j = 0; j < sourceMethods.Length; j++)
            {
                MethodInfo sourceMethod = sourceMethods[j];
                MethodInfo methodFromDisk = methodsFromDisk[j];

                Assert.Equal(sourceMethod.Name, methodFromDisk.Name);
                Assert.Equal(sourceMethod.Attributes, methodFromDisk.Attributes);
                Assert.Equal(sourceMethod.ReturnType.FullName, methodFromDisk.ReturnType.FullName);
            }
        }
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
