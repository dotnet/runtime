// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit.Experiment.Tests
{
    internal class AssemblyTools
    {
        internal static void WriteAssemblyToDisk(AssemblyName assemblyName, Type[] types, string fileLocation)
        {
            WriteAssemblyToDisk(assemblyName, types, fileLocation, null);
        }

        internal static void WriteAssemblyToDisk(AssemblyName assemblyName, Type[] types, string fileLocation, List<CustomAttributeBuilder> assemblyAttributes,
            List<CustomAttributeBuilder> moduleAttributes = null, List<CustomAttributeBuilder> typeAttributes = null,
            List<CustomAttributeBuilder> methodAttributes = null, List<CustomAttributeBuilder> fieldAttributes = null)
        {
            PersistableAssemblyBuilder assemblyBuilder = PersistableAssemblyBuilder.DefineDynamicAssembly(assemblyName, assemblyAttributes);

            PersistableModuleBuilder mb = (PersistableModuleBuilder)assemblyBuilder.DefineDynamicModule(assemblyName.Name);

            if (moduleAttributes != null)
            {
                foreach (var attribute in moduleAttributes)
                {
                    mb.SetCustomAttribute(attribute);
                }
            }

            foreach (Type type in types)
            {
                TypeBuilder tb = mb.DefineType(type.FullName, type.Attributes, type.BaseType);

                if (typeAttributes != null)
                {
                    foreach (CustomAttributeBuilder typeAttribute in typeAttributes)
                    {
                        tb.SetCustomAttribute(typeAttribute);
                    }
                }

                var methods = type.IsInterface ? type.GetMethods() : type.GetMethods(BindingFlags.DeclaredOnly);
                foreach (var method in methods)
                {
                    var paramTypes = Array.ConvertAll(method.GetParameters(), item => item.ParameterType);
                    MethodBuilder meb = tb.DefineMethod(method.Name, method.Attributes, method.CallingConvention, method.ReturnType, paramTypes);
                    if (methodAttributes != null)
                    {
                        foreach(CustomAttributeBuilder typeAttribute in methodAttributes)
                        {
                            meb.SetCustomAttribute(typeAttribute);
                        }
                    }
                }

                foreach (var field in type.GetFields())
                {
                    FieldBuilder fb = tb.DefineField(field.Name, field.FieldType, field.GetRequiredCustomModifiers(),
                        field.GetOptionalCustomModifiers(), field.Attributes);

                    if (fieldAttributes != null)
                    {
                        foreach(var attribute in fieldAttributes)
                        {
                            fb.SetCustomAttribute(attribute);
                        }
                    }
                }
            }

            assemblyBuilder.Save(fileLocation);
        }

        internal static void WriteAssemblyToStream(AssemblyName assemblyName, Type[] types, Stream stream)
        {
            WriteAssemblyToStream(assemblyName, types, stream, null);
        }

        internal static void WriteAssemblyToStream(AssemblyName assemblyName, Type[] types, Stream stream, List<CustomAttributeBuilder>? assemblyAttributes)
        {
            PersistableAssemblyBuilder assemblyBuilder = PersistableAssemblyBuilder.DefineDynamicAssembly(assemblyName, assemblyAttributes);

            PersistableModuleBuilder mb = (PersistableModuleBuilder)assemblyBuilder.DefineDynamicModule(assemblyName.Name);

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
                    tb.DefineField(field.Name, field.FieldType, field.GetRequiredCustomModifiers(), field.GetOptionalCustomModifiers(), field.Attributes);
                }
            }

            assemblyBuilder.Save(stream);
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

        internal static void MetadataReader(string filename)
        {
            Debug.WriteLine("Using MetadataReader class");

            using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var peReader = new PEReader(fs);

            MetadataReader mr = peReader.GetMetadataReader();

            Debug.WriteLine("Number of types is " + mr.TypeDefinitions.Count);

            foreach (TypeDefinitionHandle tDefH in mr.TypeDefinitions)
            {
                TypeDefinition tDef = mr.GetTypeDefinition(tDefH);
                string ns = mr.GetString(tDef.Namespace);
                string name = mr.GetString(tDef.Name);
                Debug.WriteLine($"Name of type is {ns}.{name}");
            }

            Debug.WriteLine("Number of methods is " + mr.MethodDefinitions.Count);

            foreach (MethodDefinitionHandle mDefH in mr.MethodDefinitions)
            {
                MethodDefinition mDef = mr.GetMethodDefinition(mDefH);
                string mName = mr.GetString(mDef.Name);
                var owner = mr.GetTypeDefinition(mDef.GetDeclaringType());
                Debug.WriteLine($"Method name: {mName} is owned by {mr.GetString(owner.Name)}.");
            }

            Debug.WriteLine("Ended MetadataReader class");
        }
    }
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
