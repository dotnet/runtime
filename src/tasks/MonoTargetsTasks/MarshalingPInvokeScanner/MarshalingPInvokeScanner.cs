// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.PortableExecutable;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

// For some valuetypes we cannot determine if they are compatible with disabled
// runtime marshaling without first resolving their base types. In this case we
// first mark the assembly as Inconclusive and do a second pass over the collected
// base type references in order to decide. If the base types are System.Enum,
// then the valuetypes are enumerations, and are compatible.
internal enum Compatibility
{
    Compatible,
    Incompatible,
    Inconclusive
}

internal sealed class InconclusiveCompatibilityCollection
{
    private Dictionary<string, HashSet<string>> _data = new();

    public bool IsEmpty => _data.Count == 0;

    public void Add(string assyName, string namespaceName, string typeName)
    {
        HashSet<string>? incAssyTypes;

        if(!_data.TryGetValue(assyName, out incAssyTypes))
        {
            incAssyTypes = new();
            _data.Add(assyName, incAssyTypes);
        }

        incAssyTypes.Add(string.Join(":", namespaceName, typeName));
    }

    public HashSet<string> EnumerateForAssembly(string assyName)
    {
        if(_data.TryGetValue(assyName, out HashSet<string>? incAssyTypes))
            return incAssyTypes!;

        return new HashSet<string>();
    }
}

internal sealed class MinimalMarshalingTypeCompatibilityProvider : ISignatureTypeProvider<Compatibility, object>
{
    // assembly name -> set of types needed for second pass
    private InconclusiveCompatibilityCollection _inconclusive = new();

    public bool IsSecondPassNeeded => !_inconclusive.IsEmpty;
    public HashSet<string> GetInconclusiveTypesForAssembly(string assyName) => _inconclusive.EnumerateForAssembly(assyName);

    public Compatibility GetArrayType(Compatibility elementType, ArrayShape shape) => Compatibility.Incompatible;
    public Compatibility GetByReferenceType(Compatibility elementType) => Compatibility.Incompatible;
    public Compatibility GetFunctionPointerType(MethodSignature<Compatibility> signature) => Compatibility.Compatible;
    public Compatibility GetGenericInstantiation(Compatibility genericType, ImmutableArray<Compatibility> typeArguments) => genericType;
    public Compatibility GetGenericMethodParameter(object genericContext, int index) => Compatibility.Incompatible;
    public Compatibility GetGenericTypeParameter(object genericContext, int index) => Compatibility.Incompatible;
    public Compatibility GetModifiedType(Compatibility modifier, Compatibility unmodifiedType, bool isRequired) => Compatibility.Incompatible;
    public Compatibility GetPinnedType(Compatibility elementType) => Compatibility.Compatible;
    public Compatibility GetPointerType(Compatibility elementType) => Compatibility.Compatible;
    public Compatibility GetPrimitiveType(PrimitiveTypeCode typeCode)
    {
        return typeCode switch
        {
           PrimitiveTypeCode.Object => Compatibility.Incompatible,
           PrimitiveTypeCode.String => Compatibility.Incompatible,
           PrimitiveTypeCode.TypedReference => Compatibility.Incompatible,
           _ => Compatibility.Compatible
        };
    }

    public Compatibility GetSZArrayType(Compatibility elementType) => Compatibility.Incompatible;

    public Compatibility GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        TypeDefinition typeDef = reader.GetTypeDefinition(handle);
        if (reader.GetString(typeDef.Name) == "Enum")
            return Compatibility.Compatible;

        try
        {
            EntityHandle baseTypeHandle = typeDef.BaseType;
            if(baseTypeHandle.Kind == HandleKind.TypeReference)
            {
                TypeReference baseType = reader.GetTypeReference((TypeReferenceHandle)baseTypeHandle);
                if (reader.GetString(baseType.Name) == "Enum")
                    return Compatibility.Compatible;
            }
            else if(baseTypeHandle.Kind == HandleKind.TypeSpecification)
            {
                TypeSpecification specInner = reader.GetTypeSpecification((TypeSpecificationHandle)baseTypeHandle);
                return specInner.DecodeSignature<Compatibility, object>(this, new object());
            }
            else if(baseTypeHandle.Kind == HandleKind.TypeDefinition)
            {
                TypeDefinitionHandle handleInner = (TypeDefinitionHandle)baseTypeHandle;
                if(handle != handleInner)
                    return GetTypeFromDefinition(reader, handleInner, rawTypeKind);
            }
        }
        catch(BadImageFormatException)
        {
            return Compatibility.Incompatible;
        }

        return Compatibility.Incompatible;
    }

    public Compatibility GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        if (rawTypeKind == 0x11 /*ELEMENT_TYPE_VALUETYPE*/)
        {
            TypeReference typeRef = reader.GetTypeReference(handle);
            EntityHandle scope = typeRef.ResolutionScope;

            if (scope.Kind == HandleKind.AssemblyReference)
            {
                AssemblyReferenceHandle assyRefHandle = (AssemblyReferenceHandle)typeRef.ResolutionScope;
                AssemblyReference assyRef = reader.GetAssemblyReference(assyRefHandle);

                _inconclusive.Add(reader.GetString(assyRef.Name), reader.GetString(typeRef.Namespace), reader.GetString(typeRef.Name));
                return Compatibility.Inconclusive;
            }
            else
            {
                throw new NotImplementedException(scope.Kind.ToString());
            }
        }

        return Compatibility.Incompatible;
    }

    public Compatibility GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
    {
        TypeSpecification spec = reader.GetTypeSpecification((TypeSpecificationHandle)handle);
        return spec.DecodeSignature<Compatibility, object>(this, genericContext);
    }
}



public class MarshalingPInvokeScanner : Task
{
    [Required]
    public string[]? Assemblies { get; set; }

    [Output]
    public string[]? IncompatibleAssemblies { get; private set; }


    public override bool Execute()
    {
        if (Assemblies is null || Assemblies!.Length == 0)
        {
            Log.LogError($"{nameof(MarshalingPInvokeScanner)}.{nameof(Assemblies)} cannot be empty");
            return false;
        }

        try
        {
            ExecuteInternal();
            return !Log.HasLoggedErrors;
        }
        catch (LogAsErrorException e)
        {
            Log.LogError(e.Message);
            return false;
        }
    }

    private void ExecuteInternal()
    {
        if (Assemblies is not null)
            IncompatibleAssemblies = ScanAssemblies(Assemblies);
    }

    private string[] ScanAssemblies(string[] assemblies)
    {
        HashSet<string> incompatible = new HashSet<string>();
        MinimalMarshalingTypeCompatibilityProvider mmtcp =  new MinimalMarshalingTypeCompatibilityProvider();
        foreach (string aname in assemblies)
        {
            if (IsAssemblyIncompatible(aname, mmtcp))
                incompatible.Add(aname);
        }

        if (mmtcp.IsSecondPassNeeded)
        {
            foreach (string aname in assemblies)
                ResolveInconclusiveTypes(incompatible, aname, mmtcp);
        }

        return incompatible.ToArray();
    }

    private static string GetMethodName(MetadataReader mr, MethodDefinition md)
    {
        return mr.GetString(md.Name);
    }

    private void ResolveInconclusiveTypes(HashSet<string> incompatible, string assyPath, MinimalMarshalingTypeCompatibilityProvider mmtcp)
    {
        string assyName = MetadataReader.GetAssemblyName(assyPath).Name!;
        HashSet<string> inconclusiveTypes = mmtcp.GetInconclusiveTypesForAssembly(assyName);
        if(inconclusiveTypes.Count == 0)
            return;

        using FileStream file = new FileStream(assyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using PEReader peReader = new PEReader(file);
        MetadataReader mdtReader = peReader.GetMetadataReader();

        SignatureDecoder<Compatibility, object> decoder = new SignatureDecoder<Compatibility, object>(
            mmtcp, mdtReader, null!);

        foreach (TypeDefinitionHandle typeDefHandle in mdtReader.TypeDefinitions)
        {
            TypeDefinition typeDef = mdtReader.GetTypeDefinition(typeDefHandle);
            string fullTypeName = string.Join(":", mdtReader.GetString(typeDef.Namespace), mdtReader.GetString(typeDef.Name));

            // This is not perfect, but should work right for enums defined in other assemblies,
            // which is the only case where we use Compatibility.Inconclusive.
            if (inconclusiveTypes.Contains(fullTypeName) &&
                mmtcp.GetTypeFromDefinition(mdtReader, typeDefHandle, 0) != Compatibility.Compatible)
            {
                Log.LogMessage(MessageImportance.Low, string.Format("Type {0} is marshaled and requires marshal-ilgen.", fullTypeName));

                incompatible.Add("(unknown assembly)");
            }
        }
    }

    private bool IsAssemblyIncompatible(string path, MinimalMarshalingTypeCompatibilityProvider mmtcp)
    {
        using FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using PEReader peReader = new PEReader(file);
        MetadataReader mdtReader = peReader.GetMetadataReader();

        foreach(CustomAttributeHandle attrHandle in mdtReader.CustomAttributes)
        {
            CustomAttribute attr = mdtReader.GetCustomAttribute(attrHandle);

            try
            {
                MethodDefinitionHandle mdh = (MethodDefinitionHandle)attr.Constructor;
                MethodDefinition md = mdtReader.GetMethodDefinition(mdh);
                TypeDefinitionHandle tdh = md.GetDeclaringType();
                TypeDefinition td = mdtReader.GetTypeDefinition(tdh);

                if(mdtReader.GetString(td.Name) == "DisableRuntimeMarshallingAttribute")
                    return false;
            }
            catch(InvalidCastException)
            {
            }
        }

        foreach (TypeDefinitionHandle typeDefHandle in mdtReader.TypeDefinitions)
        {
            TypeDefinition typeDef = mdtReader.GetTypeDefinition(typeDefHandle);
            string ns = mdtReader.GetString(typeDef.Namespace);
            string name = mdtReader.GetString(typeDef.Name);

            foreach(MethodDefinitionHandle mthDefHandle in typeDef.GetMethods())
            {
                MethodDefinition mthDef = mdtReader.GetMethodDefinition(mthDefHandle);
                if(!mthDef.Attributes.HasFlag(MethodAttributes.PinvokeImpl))
                    continue;

                BlobReader sgnBlobReader = mdtReader.GetBlobReader(mthDef.Signature);
                SignatureDecoder<Compatibility, object> decoder = new SignatureDecoder<Compatibility, object>(
                    mmtcp, mdtReader, null!);

                MethodSignature<Compatibility> sgn = decoder.DecodeMethodSignature(ref sgnBlobReader);
                if(sgn.ReturnType == Compatibility.Incompatible || sgn.ParameterTypes.Any(p => p == Compatibility.Incompatible))
                {
                    Log.LogMessage(MessageImportance.Low, string.Format("Assebly {0} requires marhsal-ilgen for method {1}.{2}:{3} (first pass).",
                        path, ns, name, mdtReader.GetString(mthDef.Name)));

                    return true;
                }
            }
        }

        return false;
    }
}
