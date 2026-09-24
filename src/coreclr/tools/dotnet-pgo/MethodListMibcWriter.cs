// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;

using ILCompiler;
using ILCompiler.IBC;

using Internal.TypeSystem;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    internal static class MethodListMibcWriter
    {
        private sealed class MethodListDocument
        {
            public string Runtime { get; set; }

            public string Os { get; set; }

            public string Architecture { get; set; }

            public MethodListEntry[] Methods { get; set; }
        }

        private sealed class MethodListEntry
        {
            public string Type { get; set; }

            public string Name { get; set; }

            public string[] ParameterTypes { get; set; }

            public string[] GenericArguments { get; set; }
        }

        public static int Run(string methodListPath, string outputPath, IReadOnlyList<string> references, bool compressed, bool validate)
        {
            try
            {
                ValidateInputPaths(methodListPath, outputPath, references);
                MethodListDocument document = ReadDocument(methodListPath);

                using var context = new MethodListTypeSystemContext(references);
                List<MethodDesc> methods = ResolveMethods(context, document);
                methods.Sort(static (left, right) => StringComparer.Ordinal.Compare(GetSortKey(left), GetSortKey(right)));

                var config = new MibcConfig
                {
                    Runtime = document.Runtime,
                    Os = document.Os,
                    Arch = document.Architecture,
                };

                var profileData = new MethodProfileData[methods.Count];
                for (int i = 0; i < methods.Count; i++)
                {
                    profileData[i] = new MethodProfileData(
                        methods[i],
                        MethodProfilingDataFlags.ReadMethodCode,
                        exclusiveWeight: 0,
                        callWeights: null,
                        scenarioMask: 0xFFFFFFFF,
                        schemaData: null);
                }

                return MibcEmitter.GenerateMibcFile(
                    config,
                    context,
                    new FileInfo(outputPath),
                    profileData,
                    validate,
                    uncompressed: !compressed);
            }
            catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or TypeSystemException or BadImageFormatException or FormatException)
            {
                Program.PrintError(ex.Message);
                return -6;
            }
        }

        private static string GetSortKey(MethodDesc method)
        {
            return $"{((MetadataType)method.OwningType.GetTypeDefinition()).Module.Assembly.GetName().Name}:{method}";
        }

        private static MethodListDocument ReadDocument(string methodListPath)
        {
            MethodListDocument document = JsonSerializer.Deserialize<MethodListDocument>(
                File.ReadAllText(methodListPath),
                new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                });

            if (document is null)
            {
                throw new InvalidDataException($"Method list '{methodListPath}' is empty.");
            }

            if (string.IsNullOrWhiteSpace(document.Runtime) ||
                string.IsNullOrWhiteSpace(document.Os) ||
                string.IsNullOrWhiteSpace(document.Architecture))
            {
                throw new InvalidDataException("The method list must specify non-empty runtime, os, and architecture values.");
            }

            if (document.Methods is null || document.Methods.Length == 0)
            {
                throw new InvalidDataException($"Method list '{methodListPath}' contains no methods.");
            }

            return document;
        }

        private static List<MethodDesc> ResolveMethods(MethodListTypeSystemContext context, MethodListDocument document)
        {
            var methods = new List<MethodDesc>(document.Methods.Length);
            var seenMethods = new HashSet<MethodDesc>();

            foreach (MethodListEntry entry in document.Methods)
            {
                MethodDesc method = ResolveMethod(context, entry);
                if (!seenMethods.Add(method))
                {
                    throw new InvalidDataException($"Method list contains duplicate entry '{GetSortKey(method)}'.");
                }

                methods.Add(method);
            }

            return methods;
        }

        private static MethodDesc ResolveMethod(MethodListTypeSystemContext context, MethodListEntry entry)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.Type) || string.IsNullOrWhiteSpace(entry.Name))
            {
                throw new InvalidDataException("Each method entry must specify non-empty type and name values.");
            }

            TypeDesc owningType = ResolveClosedType(context, entry.Type, $"declaring type for method '{entry.Name}'");
            TypeDesc[] genericArguments = ResolveTypes(context, entry.GenericArguments, $"generic arguments for '{entry.Type}.{entry.Name}'");
            TypeDesc[] parameterTypes = ResolveTypes(context, entry.ParameterTypes, $"parameter types for '{entry.Type}.{entry.Name}'");

            MethodDesc match = null;
            bool foundName = false;
            bool foundArity = false;
            foreach (MethodDesc candidate in owningType.GetMethods())
            {
                if (!candidate.Name.StringEquals(entry.Name))
                {
                    continue;
                }

                foundName = true;
                if (candidate.Instantiation.Length != genericArguments.Length)
                {
                    continue;
                }

                foundArity = true;
                MethodDesc instantiatedCandidate = genericArguments.Length == 0
                    ? candidate
                    : candidate.MakeInstantiatedMethod(genericArguments);

                if (entry.ParameterTypes is not null && !ParametersMatch(instantiatedCandidate.Signature, parameterTypes))
                {
                    continue;
                }

                if (match is not null)
                {
                    throw new InvalidDataException(
                        $"Method '{entry.Type}.{entry.Name}' is ambiguous; specify parameterTypes to select an overload.");
                }

                match = instantiatedCandidate;
            }

            if (match is not null)
            {
                return match;
            }

            if (foundName && !foundArity)
            {
                throw new InvalidDataException(
                    $"Method '{entry.Type}.{entry.Name}' has no overload with generic arity {genericArguments.Length}.");
            }

            throw new InvalidDataException($"Unable to resolve method '{entry.Type}.{entry.Name}'.");
        }

        private static TypeDesc ResolveClosedType(MethodListTypeSystemContext context, string typeName, string description)
        {
            if (!CustomAttributeTypeNameParser.IsAssemblyQualifiedTypeName(typeName))
            {
                throw new InvalidDataException($"Type '{typeName}' in {description} must be assembly-qualified.");
            }

            int assemblySeparator = GetAssemblySeparator(typeName);
            Debug.Assert(assemblySeparator >= 0);
            AssemblyNameInfo assemblyName = AssemblyNameInfo.Parse(typeName.AsSpan(assemblySeparator + 1).Trim());
            if (!context.HasAssembly(assemblyName))
            {
                throw new FileNotFoundException(
                    $"No reference was supplied for assembly '{assemblyName.Name}' required by type '{typeName}'.");
            }

            TypeDesc type = context.SystemModule.GetTypeByCustomAttributeTypeName(typeName, throwIfNotFound: false);
            if (type is null)
            {
                throw new InvalidDataException($"Unable to resolve type '{typeName}' in {description}.");
            }

            if (type.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true))
            {
                throw new InvalidDataException($"Open generic type '{typeName}' in {description} is not supported.");
            }

            return type;
        }

        private static TypeDesc[] ResolveTypes(MethodListTypeSystemContext context, string[] typeNames, string description)
        {
            if (typeNames is null)
            {
                return Array.Empty<TypeDesc>();
            }

            var types = new TypeDesc[typeNames.Length];
            for (int i = 0; i < typeNames.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(typeNames[i]))
                {
                    throw new InvalidDataException($"Type names in {description} must be non-empty.");
                }

                types[i] = ResolveClosedType(context, typeNames[i], description);
            }

            return types;
        }

        private static int GetAssemblySeparator(string typeName)
        {
            int bracketDepth = 0;
            bool escaped = false;
            for (int i = 0; i < typeName.Length; i++)
            {
                char character = typeName[i];
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '[')
                {
                    bracketDepth++;
                }
                else if (character == ']')
                {
                    bracketDepth--;
                }
                else if (character == ',' && bracketDepth == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool ParametersMatch(MethodSignature signature, TypeDesc[] parameterTypes)
        {
            if (signature.Length != parameterTypes.Length)
            {
                return false;
            }

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                if (signature[i] != parameterTypes[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static void ValidateInputPaths(string methodListPath, string outputPath, IReadOnlyList<string> references)
        {
            if (string.IsNullOrWhiteSpace(methodListPath))
            {
                throw new InvalidDataException("A method list filename must be specified.");
            }

            if (!File.Exists(methodListPath))
            {
                throw new FileNotFoundException($"Unable to find method list '{methodListPath}'.", methodListPath);
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new InvalidDataException("An output filename must be specified.");
            }

            if (!outputPath.EndsWith(".mibc", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The Mibc output filename must end with '.mibc'.");
            }

            if (references is null || references.Count == 0)
            {
                throw new InvalidDataException("At least one reference assembly must be specified.");
            }
        }
    }
}
