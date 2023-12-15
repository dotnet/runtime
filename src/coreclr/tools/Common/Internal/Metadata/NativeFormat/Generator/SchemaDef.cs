// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace NativeFormatGen;

/// <summary>
/// Defines the set of flags that may be applied to a schema record definition.
/// </summary>
[Flags]
public enum RecordDefFlags
{
    Enum = 0x01, // Indicates that the record is actually an enum.
    Flags = 0x02, // Indicates that [Flags] should be applied to enum definition.
    CustomCompare = 0x04, // Indicates a specific set of members have been flagged for use in
                          // implementing equality functionality; else all members are used.
    ReentrantEquals = 0x08, // The generated Equals method is potentially reentrant on the same instance
                            // and should have a fast exit path to protect from infinite recursion.
}

/// <summary>
/// Defines the set of flags that may be applied to the member definition of a schema record.
/// </summary>
[Flags]
public enum MemberDefFlags
{
    Map = 0x0001,   // => List<RecordType> for writer
    List = 0x0002,  // => List<RecordType> for writer
    Array = 0x0004, // => RecordType[] for writer

    Collection = MemberDefFlags.Map | MemberDefFlags.List | MemberDefFlags.Array,
    Sequence = MemberDefFlags.List | MemberDefFlags.Array,

    RecordRef = 0x0008,

    Child = 0x0010, // Member instance is logically defined and owned by record;
                    // otherwise instance may be shared (such as a TypeRef).
    Name = 0x0020, // May be used as the member's simple name for diagnostics.
    NotPersisted = 0x0040, // Indicates member is not written to or read from metadata.
    Compare = 0x0080, // Indicates member should be used for equality functionality.
    EnumerateForHashCode = 0x0100, // Indicates that the collection is safe to be enumerated in GetHashCode
                                   // without causing reentrancy
    CustomCompare = 0x0200, // Indicates that this member uses a custom comparer
}

public enum MemberTypeKind
{
    Accessor,
    ReaderField,
    WriterField
};

/// <summary>
/// Encapsulates definition of member in schema record definition.
/// </summary>
public class MemberDef
{
    public MemberDef(string name, object typeName = null, MemberDefFlags flags = 0, string value = null, string comment = null)
    {
        Name = name;
        TypeName = typeName;
        Flags = flags;
        Value = value;
        Comment = comment;
    }

    public readonly string Name;
    public readonly object TypeName;
    public readonly MemberDefFlags Flags;
    public readonly string Value;
    public readonly string Comment;

    public string GetMemberType(MemberTypeKind kind = MemberTypeKind.Accessor)
    {
        string typeName;
        if ((Flags & MemberDefFlags.RecordRef) != 0)
        {
            if (TypeName is string[])
            {
                typeName = (kind == MemberTypeKind.WriterField) ? "MetadataRecord" : "Handle";
            }
            else
            {
                typeName = (kind == MemberTypeKind.WriterField) ?
                    (TypeName != null ? (string)TypeName : "MetadataRecord"): $"{TypeName}Handle";
            }
        }
        else
        {
            typeName = (string)TypeName;
        }
        if ((Flags & MemberDefFlags.Collection) != 0)
        {
            if (kind == MemberTypeKind.WriterField)
            {
                if ((Flags & (MemberDefFlags.List | MemberDefFlags.Map)) != 0)
                    return $"List<{typeName}>";
                else
                    return $"{typeName}[]";
            }

            return $"{typeName}Collection";
        }
        return typeName;
    }

    public string GetMemberFieldName()
    {
        return "_" + char.ToLower(Name[0], System.Globalization.CultureInfo.InvariantCulture) + Name.Substring(1);
    }

    public string GetMemberDescription()
    {
        var typeSet = TypeName as string[];
        if (typeSet == null)
            return null;

        return "One of: " + string.Join(", ", typeSet);
    }
}

/// <summary>
/// Encapsulates definition of schema record.
/// </summary>
public class RecordDef
{
    public RecordDef(string name, string baseTypeName = null, RecordDefFlags flags = 0, string comment = null, MemberDef[] members = null)
    {
        Name = name;
        BaseTypeName = baseTypeName;
        Flags = flags;
        Comment = comment;
        Members = members;
    }

    public readonly string Name;
    public readonly string BaseTypeName;
    public readonly RecordDefFlags Flags;
    public readonly string Comment;
    public readonly MemberDef[] Members;
}

public class EnumType
{
    public EnumType(string name, string underlyingType)
    {
        Name = name;
        UnderlyingType = underlyingType;
    }

    public readonly string Name;
    public readonly string UnderlyingType;
}

public class PrimitiveType
{
    public PrimitiveType(string name, string typeName, bool customCompare = false)
    {
        Name = name;
        TypeName = typeName;
        CustomCompare = customCompare;
    }

    public readonly string Name;
    public readonly string TypeName;
    public readonly bool CustomCompare;
}

/// <summary>
/// This class defines the metadata schema that is consumed by all generators.
/// </summary>
internal sealed class SchemaDef
{
    public static readonly EnumType[] EnumTypes = new EnumType[]
    {
        new EnumType("AssemblyFlags", "uint"),
        new EnumType("AssemblyHashAlgorithm", "uint"),
        new EnumType("CallingConventions", "ushort"),
        new EnumType("SignatureCallingConvention", "byte"),
        new EnumType("EventAttributes", "ushort"),
        new EnumType("FieldAttributes", "ushort"),
        new EnumType("GenericParameterAttributes", "ushort"),
        new EnumType("GenericParameterKind", "byte"),
        new EnumType("MethodAttributes", "ushort"),
        new EnumType("MethodImplAttributes", "ushort"),
        new EnumType("MethodSemanticsAttributes", "ushort"),
        new EnumType("NamedArgumentMemberKind", "byte"),
        new EnumType("ParameterAttributes", "ushort"),
        new EnumType("PInvokeAttributes", "ushort"),
        new EnumType("PropertyAttributes", "ushort"),
        new EnumType("TypeAttributes", "uint"),
    };

    public static readonly PrimitiveType[] PrimitiveTypes = new PrimitiveType[]
    {
        new PrimitiveType("bool", "Boolean"),
        new PrimitiveType("char", "Char"),
        new PrimitiveType("byte", "Byte"),
        new PrimitiveType("sbyte", "SByte"),
        new PrimitiveType("short", "Int16"),
        new PrimitiveType("ushort", "UInt16"),
        new PrimitiveType("int", "Int32"),
        new PrimitiveType("uint", "UInt32"),
        new PrimitiveType("long", "Int64"),
        new PrimitiveType("ulong", "UInt64"),
        new PrimitiveType("float", "Single", customCompare: true),
        new PrimitiveType("double", "Double", customCompare: true),
    };

    // These enums supplement those defined by System.Reflection.Primitives.
    public static readonly RecordDef[] EnumSchema = new RecordDef[]
    {
        // AssemblyFlags - as defined in ECMA
        new RecordDef(
            name: "AssemblyFlags",
            baseTypeName: "uint",
            flags: RecordDefFlags.Enum | RecordDefFlags.Flags,
            members: new MemberDef[] {
                new MemberDef(name: "PublicKey", value: "0x1", comment: "The assembly reference holds the full (unhashed) public key."),
                new MemberDef(name: "Retargetable", value: "0x100", comment: "The implementation of this assembly used at runtime is not expected to match the version seen at compile time."),
                new MemberDef(name: "DisableJITcompileOptimizer", value: "0x4000", comment: "Reserved."),
                new MemberDef(name: "EnableJITcompileTracking", value: "0x8000", comment: "Reserved."),
            }
        ),
        // AssemblyHashAlgorithm - as defined in ECMA
        new RecordDef(
            name: "AssemblyHashAlgorithm",
            baseTypeName: "uint",
            flags: RecordDefFlags.Enum,
            members: new MemberDef[] {
                new MemberDef(name: "None", value: "0x0"),
                new MemberDef(name: "Reserved", value: "0x8003"),
                new MemberDef(name: "SHA1", value: "0x8004"),
            }
        ),
        // NamedArgumentMemberKind - used to disambiguate the referenced members of the named
        // arguments to a custom attribute instance.
        new RecordDef(
            name: "NamedArgumentMemberKind",
            baseTypeName: "byte",
            flags: RecordDefFlags.Enum,
            members: new MemberDef[] {
                new MemberDef(name: "Property", value: "0x0", comment: "Specifies the name of a property"),
                new MemberDef(name: "Field", value: "0x1", comment: "Specifies the name of a field"),
            }
        ),
        // GenericParameterKind - used to distinguish between generic type and generic method type parameters.
        new RecordDef(
            name: "GenericParameterKind",
            baseTypeName: "byte",
            flags: RecordDefFlags.Enum,
            members: new MemberDef[] {
                new MemberDef(name: "GenericTypeParameter", value: "0x0", comment: "Represents a type parameter for a generic type."),
                new MemberDef(name: "GenericMethodParameter", value: "0x1", comment: "Represents a type parameter from a generic method."),
            }
        ),
        new RecordDef(
            name: "SignatureCallingConvention",
            baseTypeName: "byte",
            flags: RecordDefFlags.Enum,
            members: new MemberDef[] {
                new MemberDef(name: "HasThis", value: "0x20"),
                new MemberDef(name: "ExplicitThis", value: "0x40"),
                new MemberDef(name: "Default", value: "0x00"),
                new MemberDef(name: "Vararg", value: "0x05"),
                new MemberDef(name: "Cdecl", value: "0x01"),
                new MemberDef(name: "StdCall", value: "0x02"),
                new MemberDef(name: "ThisCall", value: "0x03"),
                new MemberDef(name: "FastCall", value: "0x04"),
                new MemberDef(name: "Unmanaged", value: "0x09"),
                new MemberDef(name: "UnmanagedCallingConventionMask", value: "0x0F"),
            }
        ),
    }
    .OrderBy(record => record.Name, StringComparer.Ordinal)
    .ToArray();

    //
    //  Record schema definition
    //

    //
    // ConstantXXXValue, ConstantXXXArray, and corresponding Handle types
    //

    // Set of record schema definitions (see format description in "Metadata records" section below)
    // that represent constant primitive type values. Adds concept of constant managed reference, which
    // must always have a null value (thus the use of the NotPersisted flag).

    private static readonly RecordDef[] ConstantValueRecordSchema =
        (
            from primitiveType in PrimitiveTypes
            select new RecordDef(
                    name: "Constant" + primitiveType.TypeName + "Value",
                    members: new MemberDef[] {
                        new MemberDef(name: "Value", typeName: primitiveType.Name,
                            flags: primitiveType.CustomCompare ? MemberDefFlags.CustomCompare : 0)
                    }
                )
        )
        .Concat
        (
            new RecordDef[] {
                new RecordDef(
                    name: "ConstantReferenceValue",
                    members: new MemberDef[] {
                        new MemberDef(name: "Value", typeName: "Object", flags: MemberDefFlags.NotPersisted)
                    }
                ),
                new RecordDef(
                    name: "ConstantStringValue",
                    members: new MemberDef[] {
                        new MemberDef(name: "Value", typeName: "string")
                    }
                )
            }
        )
        .ToArray();

    // Set of record schema definitions (see format description in "Metadata records" section below)
    // that represent constant arrays primitive type values. Adds concept of a constant array of handle values (currently used to store
    // an array TypeDefOrRefOrSpec handles corresponding to System.Type arguments to the instantiation of a custom attribute, or to store
    // custom initialized object[] arrays in custom attributes).

    private static readonly RecordDef[] ConstantArrayRecordSchema =
        (
            from primitiveType in PrimitiveTypes
            select new RecordDef(
                    name: "Constant" + primitiveType.TypeName + "Array",
                    members: new MemberDef[] {
                        new MemberDef(name: "Value", typeName: primitiveType.TypeName,
                            flags: MemberDefFlags.Array | (primitiveType.CustomCompare ? MemberDefFlags.CustomCompare : 0))
                    }
                )
        )
        .Concat
        (
            new RecordDef[] {
                new RecordDef(
                    name: "ConstantHandleArray",
                    members: new MemberDef[] {
                        new MemberDef(name: "Value", flags: MemberDefFlags.RecordRef | MemberDefFlags.List)
                    }
                ),
                new RecordDef(
                    name: "ConstantStringArray",
                    members: new MemberDef[] {
                        new MemberDef(name: "Value", typeName: new string[] { "ConstantStringValue", "ConstantReferenceValue" }, flags: MemberDefFlags.RecordRef | MemberDefFlags.List)
                    }
                ),
                new RecordDef(
                    name: "ConstantEnumArray",
                    members: new MemberDef[] {
                        new MemberDef("ElementType", TypeDefOrRefOrSpec, MemberDefFlags.RecordRef),
                        new MemberDef("Value", ConstantEnumArrayValue, MemberDefFlags.RecordRef)
                    }
                ),
            }
        )
        .ToArray();

    private static readonly RecordDef[] ConstantRecordSchema =
        ConstantValueRecordSchema.Concat(ConstantArrayRecordSchema)
        .OrderBy(record => record.Name, StringComparer.Ordinal)
        .ToArray();

    //
    // Common tuple definitions
    //

    private static readonly string[] EnumConstantValue = new string[]
    {
        "ConstantByteValue",
        "ConstantSByteValue",
        "ConstantInt16Value",
        "ConstantUInt16Value",
        "ConstantInt32Value",
        "ConstantUInt32Value",
        "ConstantInt64Value",
        "ConstantUInt64Value",
    };

    private static readonly string[] ConstantEnumArrayValue = new string[]
    {
        "ConstantByteArray",
        "ConstantSByteArray",
        "ConstantInt16Array",
        "ConstantUInt16Array",
        "ConstantInt32Array",
        "ConstantUInt32Array",
        "ConstantInt64Array",
        "ConstantUInt64Array",
    };

    private static readonly string[] TypeDefOrRef = new string[]
    {
        "TypeDefinition",
        "TypeReference",
    };

    private static readonly string[] TypeDefOrRefOrSpec = new string[]
    {
        "TypeDefinition",
        "TypeReference",
        "TypeSpecification",
    };

    private static readonly string[] TypeDefOrRefOrSpecOrMod = new string[]
    {
        "TypeDefinition",
        "TypeReference",
        "TypeSpecification",
        "ModifiedType",
    };

    private static readonly string[] TypeSig = new string[]
    {
        "TypeInstantiationSignature",
        "SZArraySignature",
        "ArraySignature",
        "PointerSignature",
        "FunctionPointerSignature",
        "ByReferenceSignature",
        "TypeVariableSignature",
        "MethodTypeVariableSignature",
    };

    private static readonly string[] TypeDefOrRefOrSpecOrConstant =
        TypeDefOrRefOrSpec.Concat(from constantRecord in ConstantRecordSchema select constantRecord.Name).ToArray();

    private static readonly string[] MethodDefOrRef = new string[]
    {
        "QualifiedMethod",
        "MemberReference",
    };

    //
    // Metadata records
    // The record schema is defined as a list of tuples, one for each record type.
    // Record tuple format: (name, base type (not currently used), flags, [members])
    // Member tuple format: (name, type, flags)
    // These are largely based on the definitions in ECMA335.
    //
    public static readonly RecordDef[] RecordSchema = new RecordDef[]
    {
        new RecordDef(
            name: "TypeDefinition",
            flags: RecordDefFlags.CustomCompare,
            members: new MemberDef[] {
                new MemberDef("Flags", "TypeAttributes"),
                new MemberDef("BaseType", TypeDefOrRefOrSpec, MemberDefFlags.RecordRef),
                new MemberDef("NamespaceDefinition", "NamespaceDefinition", MemberDefFlags.RecordRef | MemberDefFlags.Compare),
                new MemberDef("Name", "ConstantStringValue", MemberDefFlags.RecordRef | MemberDefFlags.Child | MemberDefFlags.Compare),
                new MemberDef("Size", "uint"),
                new MemberDef("PackingSize", "ushort"),
                new MemberDef("EnclosingType", "TypeDefinition", MemberDefFlags.RecordRef | MemberDefFlags.Compare),
                new MemberDef("NestedTypes", "TypeDefinition", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("Methods", "Method", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("Fields", "Field", MemberDefFlags.Map | MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("Properties", "Property", MemberDefFlags.Map | MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("Events", "Event", MemberDefFlags.Map | MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("GenericParameters", "GenericParameter", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("Interfaces", TypeDefOrRefOrSpec, MemberDefFlags.List | MemberDefFlags.RecordRef),
                // COMPLETENESS: new MemberDef("MethodImpls", "MethodImpl", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("CustomAttributes", "CustomAttribute", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child),
            }
        ),
        new RecordDef(
            name: "TypeReference",
            members: new MemberDef[] {
                new MemberDef("ParentNamespaceOrType", new string[] { "NamespaceReference", "TypeReference" }, MemberDefFlags.RecordRef),
                new MemberDef("TypeName", "ConstantStringValue", MemberDefFlags.RecordRef | MemberDefFlags.Child | MemberDefFlags.Name),
                // COMPLETENESS: new MemberDef("CustomAttributes", "CustomAttribute", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child),
            }
        ),
        new RecordDef(
            name: "TypeSpecification",
            members: new MemberDef[] {
                new MemberDef("Signature", TypeDefOrRef.Concat(TypeSig).ToArray(), MemberDefFlags.RecordRef | MemberDefFlags.Child),
                // COMPLETENESS: new MemberDef("CustomAttributes", "CustomAttribute", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child),
            }
        ),
        new RecordDef(
            name: "ScopeDefinition",
            flags: RecordDefFlags.CustomCompare,
            members: new MemberDef[] {
                new MemberDef("Flags", "AssemblyFlags", MemberDefFlags.Compare),
                new MemberDef("Name", "ConstantStringValue", MemberDefFlags.RecordRef | MemberDefFlags.Child | MemberDefFlags.Compare),
                new MemberDef("HashAlgorithm", "AssemblyHashAlgorithm", MemberDefFlags.Compare),
                new MemberDef("MajorVersion", "ushort", MemberDefFlags.Compare),
                new MemberDef("MinorVersion", "ushort", MemberDefFlags.Compare),
                new MemberDef("BuildNumber", "ushort", MemberDefFlags.Compare),
                new MemberDef("RevisionNumber", "ushort", MemberDefFlags.Compare),
                new MemberDef("PublicKey", "Byte", MemberDefFlags.Array | MemberDefFlags.Compare),
                new MemberDef("Culture", "ConstantStringValue", MemberDefFlags.RecordRef | MemberDefFlags.Child | MemberDefFlags.Compare),
                new MemberDef("RootNamespaceDefinition", "NamespaceDefinition", MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("EntryPoint", "QualifiedMethod", MemberDefFlags.RecordRef),
                new MemberDef("GlobalModuleType", "TypeDefinition", MemberDefFlags.RecordRef),
                new MemberDef("CustomAttributes", "CustomAttribute", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("ModuleName", "ConstantStringValue", MemberDefFlags.RecordRef | MemberDefFlags.Child | MemberDefFlags.Compare),
                new MemberDef("Mvid", "Byte", MemberDefFlags.Array | MemberDefFlags.Compare),
                new MemberDef("ModuleCustomAttributes", "CustomAttribute", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child),
            }
        ),
        new RecordDef(
            name: "ScopeReference",
            members: new MemberDef[] {
                new MemberDef("Flags", "AssemblyFlags"),
                new MemberDef("Name", "ConstantStringValue", MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("MajorVersion", "ushort"),
                new MemberDef("MinorVersion", "ushort"),
                new MemberDef("BuildNumber", "ushort"),
                new MemberDef("RevisionNumber", "ushort"),
                new MemberDef("PublicKeyOrToken", "Byte", MemberDefFlags.Array),
                new MemberDef("Culture", "ConstantStringValue", MemberDefFlags.RecordRef | MemberDefFlags.Child),
                // COMPLETENESS: new MemberDef("CustomAttributes", "CustomAttribute", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child),
            }
        ),
        new RecordDef(
            name: "NamespaceDefinition",
            flags: RecordDefFlags.CustomCompare,
            members: new MemberDef[] {
                new MemberDef("ParentScopeOrNamespace", new string[] { "NamespaceDefinition", "ScopeDefinition" }, MemberDefFlags.RecordRef | MemberDefFlags.Compare),
                new MemberDef("Name", "ConstantStringValue", MemberDefFlags.RecordRef | MemberDefFlags.Child | MemberDefFlags.Compare),
                new MemberDef("TypeDefinitions", "TypeDefinition", MemberDefFlags.Map | MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("TypeForwarders", "TypeForwarder", MemberDefFlags.Map | MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("NamespaceDefinitions", "NamespaceDefinition", MemberDefFlags.Map | MemberDefFlags.RecordRef | MemberDefFlags.Child),
            }
        ),
        new RecordDef(
            name: "NamespaceReference",
            members: new MemberDef[] {
                new MemberDef("ParentScopeOrNamespace", new string[] { "NamespaceReference", "ScopeReference" }, MemberDefFlags.RecordRef),
                new MemberDef("Name", "ConstantStringValue", MemberDefFlags.RecordRef | MemberDefFlags.Child),
            }
        ),
        new RecordDef(
            name: "Method",
            members: new MemberDef[] {
                new MemberDef("Flags", "MethodAttributes"),
                new MemberDef("ImplFlags", "MethodImplAttributes"),
                new MemberDef("Name", "ConstantStringValue", MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("Signature", "MethodSignature", MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("Parameters", "Parameter", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child | MemberDefFlags.EnumerateForHashCode),
                new MemberDef("GenericParameters", "GenericParameter", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child | MemberDefFlags.EnumerateForHashCode),
                new MemberDef("CustomAttributes", "CustomAttribute", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child),
            }
        ),
        new RecordDef(
            name: "QualifiedMethod",
            members: new MemberDef[] {
                new MemberDef("Method", "Method", MemberDefFlags.RecordRef),
                new MemberDef("EnclosingType", "TypeDefinition", MemberDefFlags.RecordRef),
            }
        ),
        new RecordDef(
            name: "MethodInstantiation",
            members: new MemberDef[] {
                new MemberDef("Method", MethodDefOrRef, MemberDefFlags.RecordRef),
                new MemberDef("GenericTypeArguments", TypeDefOrRefOrSpecOrMod, MemberDefFlags.List | MemberDefFlags.RecordRef),
                // COMPLETENESS: new MemberDef("CustomAttributes", "CustomAttribute", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child),
            }
        ),
        new RecordDef(
            name: "MemberReference",
            members: new MemberDef[] {
                new MemberDef("Parent", TypeDefOrRefOrSpec, MemberDefFlags.RecordRef),
                new MemberDef("Name", "ConstantStringValue", MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("Signature", new string[] { "MethodSignature", "FieldSignature" }, MemberDefFlags.RecordRef | MemberDefFlags.Child),
                // COMPLETENESS: new MemberDef("CustomAttributes", "CustomAttribute", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child),
            }
        ),
        new RecordDef(
            name: "Field",
            members: new MemberDef[] {
                new MemberDef("Flags", "FieldAttributes"),
                new MemberDef("Name", "ConstantStringValue", MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("Signature", "FieldSignature", MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("DefaultValue", TypeDefOrRefOrSpecOrConstant, MemberDefFlags.RecordRef),
                new MemberDef("Offset", "uint"),
                new MemberDef("CustomAttributes", "CustomAttribute", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child | MemberDefFlags.EnumerateForHashCode),
            }
        ),
        new RecordDef(
            name: "QualifiedField",
            members: new MemberDef[] {
                new MemberDef("Field", "Field", MemberDefFlags.RecordRef),
                new MemberDef("EnclosingType", "TypeDefinition", MemberDefFlags.RecordRef),
            }
        ),
        new RecordDef(
            name: "Property",
            members: new MemberDef[] {
                new MemberDef("Flags", "PropertyAttributes"),
                new MemberDef("Name", "ConstantStringValue", MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("Signature", "PropertySignature", MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("MethodSemantics", "MethodSemantics", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child | MemberDefFlags.EnumerateForHashCode),
                new MemberDef("DefaultValue", TypeDefOrRefOrSpecOrConstant, MemberDefFlags.RecordRef),
                new MemberDef("CustomAttributes", "CustomAttribute", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child | MemberDefFlags.EnumerateForHashCode),
            }
        ),
        new RecordDef(
            name: "Event",
            members: new MemberDef[] {
                new MemberDef("Flags", "EventAttributes"),
                new MemberDef("Name", "ConstantStringValue", MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("Type", TypeDefOrRefOrSpec, MemberDefFlags.RecordRef),
                new MemberDef("MethodSemantics", "MethodSemantics", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child | MemberDefFlags.EnumerateForHashCode),
                new MemberDef("CustomAttributes", "CustomAttribute", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child | MemberDefFlags.EnumerateForHashCode),
            }
        ),
        new RecordDef(
            name: "CustomAttribute",
            flags: RecordDefFlags.ReentrantEquals,
            members: new MemberDef[] {
                new MemberDef("Constructor", MethodDefOrRef, MemberDefFlags.RecordRef),
                new MemberDef("FixedArguments", TypeDefOrRefOrSpecOrConstant, MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.EnumerateForHashCode),
                new MemberDef("NamedArguments", "NamedArgument", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child | MemberDefFlags.EnumerateForHashCode),
            }
        ),
        new RecordDef(
            name: "NamedArgument",
            members: new MemberDef[] {
                new MemberDef("Flags", "NamedArgumentMemberKind"),
                new MemberDef("Name", "ConstantStringValue", MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("Type", TypeDefOrRefOrSpec, MemberDefFlags.RecordRef),
                new MemberDef("Value", TypeDefOrRefOrSpecOrConstant, MemberDefFlags.RecordRef),
            }
        ),
        new RecordDef(
            name: "ConstantBoxedEnumValue",
            members: new MemberDef[] {
                new MemberDef("Value", EnumConstantValue, MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("Type", TypeDefOrRefOrSpec, MemberDefFlags.RecordRef)
            }
        ),
        new RecordDef(
            name: "GenericParameter",
            members: new MemberDef[] {
                new MemberDef("Number", "ushort"),
                new MemberDef("Flags", "GenericParameterAttributes"),
                new MemberDef("Kind", "GenericParameterKind"),
                new MemberDef("Name", "ConstantStringValue", MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("Constraints", TypeDefOrRefOrSpecOrMod, MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.EnumerateForHashCode),
                new MemberDef("CustomAttributes", "CustomAttribute", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child),
            }
        ),
        /* COMPLETENESS new RecordDef(
            name: "MethodImpl",
            members: new MemberDef[] {
                new MemberDef("MethodBody", MethodDefOrRef, MemberDefFlags.RecordRef),
                new MemberDef("MethodDeclaration", MethodDefOrRef, MemberDefFlags.RecordRef),
            }
        ),*/
        new RecordDef(
            name: "Parameter",
            members: new MemberDef[] {
                new MemberDef("Flags", "ParameterAttributes"),
                new MemberDef("Sequence", "ushort"),
                new MemberDef("Name", "ConstantStringValue", MemberDefFlags.RecordRef | MemberDefFlags.Child),
                new MemberDef("DefaultValue", TypeDefOrRefOrSpecOrConstant, MemberDefFlags.RecordRef),
                new MemberDef("CustomAttributes", "CustomAttribute", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child),
            }
        ),
        new RecordDef(
            name: "MethodSemantics",
            members: new MemberDef[] {
                new MemberDef("Attributes", "MethodSemanticsAttributes"),
                new MemberDef("Method", "Method", MemberDefFlags.RecordRef),
            }
        ),
        new RecordDef(
            name: "TypeInstantiationSignature",
            members: new MemberDef[] {
                new MemberDef("GenericType", TypeDefOrRefOrSpec, MemberDefFlags.RecordRef),
                new MemberDef("GenericTypeArguments", TypeDefOrRefOrSpecOrMod, MemberDefFlags.List | MemberDefFlags.RecordRef),
            }
        ),
        new RecordDef(
            name: "SZArraySignature",
            members: new MemberDef[] {
                new MemberDef("ElementType", TypeDefOrRefOrSpecOrMod, MemberDefFlags.RecordRef),
            }
        ),
        new RecordDef(
            name: "ArraySignature",
            members: new MemberDef[] {
                new MemberDef("ElementType", TypeDefOrRefOrSpecOrMod, MemberDefFlags.RecordRef),
                new MemberDef("Rank", "int"),
                new MemberDef("Sizes", "Int32", MemberDefFlags.Array),
                new MemberDef("LowerBounds", "Int32", MemberDefFlags.Array),
            }
        ),
        new RecordDef(
            name: "ByReferenceSignature",
            members: new MemberDef[] {
                new MemberDef("Type", TypeDefOrRefOrSpecOrMod, MemberDefFlags.RecordRef),
            }
        ),
        new RecordDef(
            name: "PointerSignature",
            members: new MemberDef[] {
                new MemberDef("Type", TypeDefOrRefOrSpecOrMod, MemberDefFlags.RecordRef),
            }
        ),
        new RecordDef(
            name: "FunctionPointerSignature",
            members: new MemberDef[] {
                new MemberDef("Signature", "MethodSignature", MemberDefFlags.RecordRef),
            }
        ),
        new RecordDef(
            name: "TypeVariableSignature",
            members: new MemberDef[] {
                new MemberDef("Number", "int"),
            }
        ),
        new RecordDef(
            name: "MethodTypeVariableSignature",
            members: new MemberDef[] {
                new MemberDef("Number", "int"),
            }
        ),
        new RecordDef(
            name: "FieldSignature",
            members: new MemberDef[] {
                new MemberDef("Type", TypeDefOrRefOrSpecOrMod, MemberDefFlags.RecordRef),
            }
        ),
        new RecordDef(
            name: "PropertySignature",
            members: new MemberDef[] {
                new MemberDef("CallingConvention", "CallingConventions"),
                new MemberDef("Type", TypeDefOrRefOrSpecOrMod, MemberDefFlags.RecordRef),
                new MemberDef("Parameters", TypeDefOrRefOrSpecOrMod, MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.EnumerateForHashCode),
            }
        ),
        new RecordDef(
            name: "MethodSignature",
            members: new MemberDef[] {
                new MemberDef("CallingConvention", "SignatureCallingConvention"),
                new MemberDef("GenericParameterCount", "int"),
                new MemberDef("ReturnType", TypeDefOrRefOrSpecOrMod, MemberDefFlags.RecordRef),
                new MemberDef("Parameters", TypeDefOrRefOrSpecOrMod, MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.EnumerateForHashCode),
                new MemberDef("VarArgParameters", TypeDefOrRefOrSpecOrMod, MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.EnumerateForHashCode),
            }
        ),
        new RecordDef(
            name: "TypeForwarder",
            members: new MemberDef[] {
                new MemberDef("Scope", "ScopeReference", MemberDefFlags.RecordRef),
                new MemberDef("Name", "ConstantStringValue", MemberDefFlags.RecordRef | MemberDefFlags.Child | MemberDefFlags.Name),
                new MemberDef("NestedTypes", "TypeForwarder", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child),
                // COMPLETENESS: new MemberDef("CustomAttributes", "CustomAttribute", MemberDefFlags.List | MemberDefFlags.RecordRef | MemberDefFlags.Child),
            }
        ),
        new RecordDef(
            name: "ModifiedType",
            members: new MemberDef[] {
                new MemberDef("IsOptional", "bool"),
                new MemberDef("ModifierType", TypeDefOrRefOrSpec, MemberDefFlags.RecordRef),
                new MemberDef("Type", TypeDefOrRefOrSpecOrMod, MemberDefFlags.RecordRef),
            }
        )
    }
    // ConstantXXXValue provided in ConstantRecordSchema and appended to end of this list.
    .Concat(ConstantRecordSchema)
    .OrderBy(record => record.Name, StringComparer.Ordinal)
    .ToArray();

    /// <summary>
    // Contains a list of records with corresponding Handle types (currently all of them).
    /// </summary>
    public static readonly string[] HandleSchema = (from record in RecordSchema select record.Name).ToArray();

    public static readonly string[] TypeNamesWithCollectionTypes =
        RecordSchema.SelectMany(r =>
            from member in r.Members
            let memberTypeName = member.TypeName as string
            where memberTypeName != null &&
                (member.Flags & MemberDefFlags.Collection) != 0 &&
                !PrimitiveTypes.Any(pt => pt.TypeName == memberTypeName)
            select memberTypeName
        ).Concat(new[] { "ScopeDefinition" }).Distinct().ToArray();
}
