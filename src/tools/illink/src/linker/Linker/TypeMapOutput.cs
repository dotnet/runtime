// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ILLink.Shared;
using Mono.Cecil;
using AssemblyNameInfo = System.Reflection.Metadata.AssemblyNameInfo;
using TypeName = System.Reflection.Metadata.TypeName;
using Map = ILLink.Shared.TypeMapXmlWriter.Map;

namespace Mono.Linker;

internal static class TypeMapOutput
{
    public static void Write(LinkContext context, string fileName)
    {
        Debug.Assert(context.TypeMapHandler is not null);
        TypeMapHandler handler = context.TypeMapHandler;
        var groups = new SortedDictionary<string, (Map? External, Map? Proxy)>(StringComparer.Ordinal);

        foreach (TypeReference group in handler.ExternalTypeMapGroups)
            groups[GetTypeName(group).AssemblyQualifiedName] = (GetMap(group, proxy: false), null);

        foreach (TypeReference group in handler.ProxyTypeMapGroups)
        {
            string name = GetTypeName(group).AssemblyQualifiedName;
            groups.TryGetValue(name, out var maps);
            maps.Proxy = GetMap(group, proxy: true);
            groups[name] = maps;
        }

        using (FileStream output = new FileStream(fileName, FileMode.Create))
        {
            TypeMapXmlWriter.WriteTypeMapsToStream(output, groups);
            output.Flush();
        }

        Map GetMap(TypeReference groupType, bool proxy)
        {
            var map = new Map();

            if ((handler.EntryPointAssembly ?? context.TypeMapOutputAssembly) is not AssemblyDefinition entryPoint)
                return map;

            var visited = new HashSet<AssemblyDefinition>();
            var pending = new Queue<AssemblyDefinition>();
            pending.Enqueue(entryPoint);

            while (pending.Count > 0)
            {
                AssemblyDefinition assembly = pending.Dequeue();
                if (!visited.Add(assembly) ||
                    context.Annotations.GetAction(assembly) is not (AssemblyAction.Link or AssemblyAction.Save or AssemblyAction.Copy or AssemblyAction.AddBypassNGen))
                    continue;

                foreach (CustomAttribute attribute in assembly.CustomAttributes)
                {
                    if (attribute.AttributeType is not GenericInstanceType
                        {
                            Namespace: "System.Runtime.InteropServices",
                            GenericArguments: [TypeReference attributeGroup]
                        } ||
                        !TypeReferenceEqualityComparer.AreEqual(groupType, attributeGroup, context))
                        continue;

                    switch (attribute.AttributeType.Name)
                    {
                        case "TypeMapAssemblyTargetAttribute`1":
                            if (attribute.ConstructorArguments is not [{ Value: string targetAssemblyName }])
                                throw new BadImageFormatException(string.Format(SharedStrings.TypeMapOutputInvalidAttribute, attribute));
                            if (context.TryResolve(AssemblyNameReference.Parse(targetAssemblyName)) is AssemblyDefinition targetAssembly)
                                pending.Enqueue(targetAssembly);
                            break;

                        case "TypeMapAttribute`1" when !proxy:
                            if (attribute.ConstructorArguments.Count is not (2 or 3) ||
                                attribute.ConstructorArguments is not [{ Value: string key }, { Value: TypeReference type }, ..])
                                throw new BadImageFormatException(string.Format(SharedStrings.TypeMapOutputInvalidAttribute, attribute));
                            map.Entries.Add((key, GetTypeName(type).AssemblyQualifiedName));
                            break;

                        case "TypeMapAssociationAttribute`1" when proxy:
                            if (attribute.ConstructorArguments is not [{ Value: TypeReference source }, { Value: TypeReference target }])
                                throw new BadImageFormatException(string.Format(SharedStrings.TypeMapOutputInvalidAttribute, attribute));
                            map.Entries.Add((GetTypeName(source).AssemblyQualifiedName, GetTypeName(target).AssemblyQualifiedName));
                            break;
                    }
                }
            }

            return map;
        }

        TypeName GetTypeName(TypeReference type) => type switch
        {
            ArrayType array => array.IsVector
                ? GetTypeName(array.ElementType).MakeSZArrayTypeName()
                : GetTypeName(array.ElementType).MakeArrayTypeName(array.Rank),
            PointerType pointer => GetTypeName(pointer.ElementType).MakePointerTypeName(),
            ByReferenceType byRef => GetTypeName(byRef.ElementType).MakeByRefTypeName(),
            GenericInstanceType instance => GetTypeName(instance.ElementType)
                .MakeGenericTypeName(instance.GenericArguments.Select(GetTypeName).ToImmutableArray()),
            TypeSpecification or GenericParameter => throw new NotSupportedException(string.Format(SharedStrings.TypeMapOutputUnsupportedType, type.FullName)),
            _ => GetSimpleTypeName(context.TryResolve(type) ?? type)
        };

        TypeName GetSimpleTypeName(TypeReference type)
        {
            var name = new StringBuilder();
            if (type.DeclaringType is TypeReference declaringType)
                name.Append(GetTypeName(declaringType).FullName).Append('+');
            else if (type.Namespace.Length > 0)
                name.Append(TypeMapXmlWriter.EscapeTypeName(type.Namespace)).Append('.');
            name.Append(TypeMapXmlWriter.EscapeTypeName(type.Name));
            string assemblyName = type.Scope is AssemblyNameReference assembly
                ? assembly.Name : type.Module.Assembly.Name.Name;

            return TypeName.Parse(name.ToString()).WithAssemblyName(new AssemblyNameInfo(assemblyName));
        }
    }
}
