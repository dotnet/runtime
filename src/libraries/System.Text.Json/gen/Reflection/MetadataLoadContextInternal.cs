// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace System.Text.Json.Reflection
{
    internal class MetadataLoadContextInternal
    {
        private readonly Dictionary<string, IAssemblySymbol> _assemblies = new Dictionary<string, IAssemblySymbol>(StringComparer.OrdinalIgnoreCase);

        private readonly Compilation _compilation;

        private IAssemblySymbol? _collectionsAssemblySymbol;

        public MetadataLoadContextInternal(Compilation compilation)
        {
            _compilation = compilation;
            Dictionary<AssemblyName, IAssemblySymbol> assemblies = compilation.References
                .OfType<PortableExecutableReference>()
                .ToDictionary(
                    r => string.IsNullOrWhiteSpace(r.FilePath) ?
                        new AssemblyName(r.Display) : AssemblyName.GetAssemblyName(r.FilePath),
                    r => (IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(r)!);

            foreach (var item in assemblies)
            {
                string key = item.Key.Name;
                _assemblies[key] = item.Value!;

                if (_collectionsAssemblySymbol == null && key == "System.Collections")
                {
                    _collectionsAssemblySymbol = item.Value!;
                }
            }

            CoreAssembly = new AssemblyWrapper(compilation.GetTypeByMetadataName("System.Object")!.ContainingAssembly, this);
            MainAssembly = new AssemblyWrapper(compilation.Assembly, this);
        }

        public Type? Resolve(Type type)
        {
            string assemblyName = type.Assembly.GetName().Name;
            IAssemblySymbol assemblySymbol;

            if (assemblyName == "System.Private.CoreLib" || assemblyName == "mscorlib" || assemblyName == "System.Runtime" || assemblyName == "System.Private.Uri")
            {
                Type resolvedType = ResolveFromAssembly(type, CoreAssembly.Symbol);
                if (resolvedType != null)
                {
                    return resolvedType;
                }

                if (_collectionsAssemblySymbol != null && typeof(IEnumerable).IsAssignableFrom(type))
                {
                    resolvedType = ResolveFromAssembly(type, _collectionsAssemblySymbol);
                    if (resolvedType != null)
                    {
                        return resolvedType;
                    }
                }
            }

            CustomAttributeData? typeForwardedFrom = type.GetCustomAttributeData(typeof(TypeForwardedFromAttribute));
            if (typeForwardedFrom != null)
            {
                assemblyName = typeForwardedFrom.GetConstructorArgument<string>(0);
            }

            if (!_assemblies.TryGetValue(new AssemblyName(assemblyName).Name, out assemblySymbol))
            {
                return null;
            }

            return ResolveFromAssembly(type, assemblySymbol);
        }

        private Type? ResolveFromAssembly(Type type, IAssemblySymbol assemblySymbol)
        {
            if (type.IsArray)
            {
                var typeSymbol = assemblySymbol.GetTypeByMetadataName(type.GetElementType().FullName);
                if (typeSymbol == null)
                {
                    return null!;
                }

                return _compilation.CreateArrayTypeSymbol(typeSymbol).AsType(this);
            }

            // Resolve the full name
            return assemblySymbol.GetTypeByMetadataName(type.FullName)!.AsType(this);
        }

        private AssemblyWrapper CoreAssembly { get; }
        public Assembly MainAssembly { get; }

        internal Assembly LoadFromAssemblyName(string fullName)
        {
            if (_assemblies.TryGetValue(new AssemblyName(fullName).Name, out var assembly))
            {
                return new AssemblyWrapper(assembly, this);
            }
            return null!;
        }
    }
}
