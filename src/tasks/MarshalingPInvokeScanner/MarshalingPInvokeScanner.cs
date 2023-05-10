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

        incAssyTypes.Add(string.Join(':', namespaceName, typeName));
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
    public Compatibility GetByReferenceType(Compatibility elementType) => Compatibility.Compatible;
    public Compatibility GetFunctionPointerType(MethodSignature<Compatibility> signature) => Compatibility.Compatible;
    public Compatibility GetGenericInstantiation(Compatibility gennricType, ImmutableArray<Compatibility> typeArguments) => Compatibility.Incompatible;
    public Compatibility GetGenericMethodParameter(object genericContext, int index) => Compatibility.Incompatible;
    public Compatibility GetGenericTypeParameter(object genericContext, int index) => Compatibility.Incompatible;
    public Compatibility GetModifiedType(Compatibility modifier, Compatibility unmodifiedType, bool isRequired) => Compatibility.Incompatible;
    public Compatibility GetPinnedType(Compatibility elementType) => Compatibility.Compatible; // TODO: really?
    public Compatibility GetPointerType(Compatibility elementType) => Compatibility.Compatible;
    public Compatibility GetPrimitiveType(PrimitiveTypeCode typeCode)
    {
        return typeCode switch
        {
           PrimitiveTypeCode.Object => Compatibility.Incompatible,
           PrimitiveTypeCode.String => Compatibility.Incompatible,
           PrimitiveTypeCode.TypedReference => Compatibility.Incompatible, // TODO: really?
           _ => Compatibility.Compatible
        };
    }

    public Compatibility GetSZArrayType(Compatibility elementType) => Compatibility.Incompatible;

    public Compatibility GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        TypeDefinition typeDef = reader.GetTypeDefinition(handle);

        EntityHandle baseTypeHandle = typeDef.BaseType;
        if(baseTypeHandle.Kind == HandleKind.TypeReference)
        {
            TypeReference baseType = reader.GetTypeReference((TypeReferenceHandle)baseTypeHandle);
            if (reader.GetString(baseType.Name) == "Enum")
                return Compatibility.Compatible;
        }
        else
        {
            throw new NotImplementedException();
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
                throw new NotImplementedException();
            }
        }

        return Compatibility.Incompatible;
    }

    public Compatibility GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
        Compatibility.Incompatible; // TODO: Really?
}





public class MarshalingPInvokeScanner : Task
{
    [Required]
    public string[]? Assemblies { get; set; }

    [Output]
    public string[]? IncompatibleAssemblies { get; private set; }

    private static readonly char[] s_charsToReplace = new char[] { '.', '-', '+' };

    // Avoid sharing this cache with all the invocations of this task throughout the build
    private readonly Dictionary<string, string> _symbolNameFixups = new Dictionary<string, string>();

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

    private static string[] ScanAssemblies(string[] assemblies)
    {
        HashSet<string> incompatible = new HashSet<string>();

        PathAssemblyResolver resolver = new PathAssemblyResolver(assemblies);
        using MetadataLoadContext mlc = new MetadataLoadContext(resolver, "System.Private.CoreLib");

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

    private static void ResolveInconclusiveTypes(HashSet<string> incompatible, string assyPath, MinimalMarshalingTypeCompatibilityProvider mmtcp)
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
            string fullTypeName = string.Join(':', mdtReader.GetString(typeDef.Namespace), mdtReader.GetString(typeDef.Name));

            if (inconclusiveTypes.Contains(fullTypeName))
                Console.WriteLine("Resolving inconclusive: " + fullTypeName);

            // This is not perfect, but should work right for enums defined in other assemblies,
            // which is the only case where we use Compatibility.Inconclusive.
            if (inconclusiveTypes.Contains(fullTypeName) &&
                mmtcp.GetTypeFromDefinition(mdtReader, typeDefHandle, 0) != Compatibility.Compatible)
            {
                incompatible.Add("(unknown assembly)");
            }
        }
    }

    private static bool IsAssemblyIncompatible(string path, MinimalMarshalingTypeCompatibilityProvider mmtcp)
    {
        using FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using PEReader peReader = new PEReader(file);
        MetadataReader mdtReader = peReader.GetMetadataReader();

        Console.WriteLine(path);

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
                {
                    Console.WriteLine(string.Format("Assembly {0} is annotated with DisableRuntimeMarshallingAttribute.", path));
                    return false;
                }
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
                if(sgn.ReturnType == Compatibility.Incompatible)
                {
                    Console.WriteLine("   " + GetMethodName(mdtReader, mthDef) + " - incompatible return type.");
                    return true;
                }

                if(sgn.ParameterTypes.Any(p => p == Compatibility.Incompatible)) // if not all are true
                {
                    Console.WriteLine("   " + GetMethodName(mdtReader, mthDef) + " - incompatible parameter type.");
                    return true;
                }
            }
        }

        return false;
    }



    #pragma warning disable IDE0060
    private bool IsAssemblyIncompatible(Assembly assy, List<PInvoke> pivs, List<string> strs, List<PInvokeCallback> cbks)
    {
        // Assembly is incompatible with the lightweight mono marshaler if it does not have the
        // DisableRuntimeMarshallingAttribute and has P/Invokes with nonblittable types.
        IList<CustomAttributeData> attrs = assy.GetCustomAttributesData();
        foreach (CustomAttributeData attr in attrs)
        {
            if (attr.AttributeType == typeof(DisableRuntimeMarshallingAttribute))
                return false;
        }

        try
        {
            foreach (PInvoke piv in pivs)
            {
                foreach (ParameterInfo parInfo in piv.Method.GetParameters())
                {
                    if (!PInvokeCollector.IsBlittable(parInfo.ParameterType))
                        return true;
                }

                if (!PInvokeCollector.IsBlittable(piv.Method.ReturnType) &&
                    piv.Method.ReturnType.FullName != "System.Void")
                    return true;
            }
        }
        catch (NotSupportedException ex)
        {
            Log.LogWarning(null, "WASM0001", "", "", 0, 0, 0, 0,
                $"Could not parse method signature because '{ex.Message}'. This will result in the assembly being marked as incompatible with the lightweight Mono marshaler, potentially as a false positive. ");
            return true;
        }

        return false;
    }
    #pragma warning restore IDE0060


    public string FixupSymbolName(string name)
    {
        if (_symbolNameFixups.TryGetValue(name, out string? fixedName))
            return fixedName;

        UTF8Encoding utf8 = new();
        byte[] bytes = utf8.GetBytes(name);
        StringBuilder sb = new();

        foreach (byte b in bytes)
        {
            if ((b >= (byte)'0' && b <= (byte)'9') ||
                (b >= (byte)'a' && b <= (byte)'z') ||
                (b >= (byte)'A' && b <= (byte)'Z') ||
                (b == (byte)'_'))
            {
                sb.Append((char)b);
            }
            else if (s_charsToReplace.Contains((char)b))
            {
                sb.Append('_');
            }
            else
            {
                sb.Append($"_{b:X}_");
            }
        }

        fixedName = sb.ToString();
        _symbolNameFixups[name] = fixedName;
        return fixedName;
    }
}
