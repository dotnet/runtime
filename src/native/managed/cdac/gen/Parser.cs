// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.DataGenerator;

internal static class Parser
{
    private const string AttrNs = "Microsoft.Diagnostics.DataContractReader";
    public const string CdacTypeAttributeFqn = AttrNs + ".CdacTypeAttribute";
    public const string FieldAttributeFqn = AttrNs + ".FieldAttribute";
    public const string RawOffsetAttributeFqn = AttrNs + ".RawOffsetAttribute";
    public const string FieldAddressAttributeFqn = AttrNs + ".FieldAddressAttribute";
    public const string InstanceDataStartAttributeFqn = AttrNs + ".InstanceDataStartAttribute";
    public const string StaticAddressAttributeFqn = AttrNs + ".StaticAddressAttribute";
    public const string StaticReferenceAttributeFqn = AttrNs + ".StaticReferenceAttribute";
    public const string ThreadStaticAddressAttributeFqn = AttrNs + ".ThreadStaticAddressAttribute";

    public static CdacTypeModel? Parse(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        AttributeData? cdacAttr = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == CdacTypeAttributeFqn);
        if (cdacAttr is null)
        {
            return null;
        }

        EquatableArray<string> names = EquatableArray<string>.FromEnumerable(
            cdacAttr.ConstructorArguments[0].Values
                .Select(v => (string)v.Value!)
                .ToList());
        bool hasTypeHandle = GetNamedBool(cdacAttr, "HasTypeHandle");

        List<MemberModel> members = new();
        foreach (ISymbol member in classSymbol.GetMembers())
        {
            switch (member)
            {
                case IPropertySymbol prop:
                    if (TryParseProperty(prop, out MemberModel? pm))
                    {
                        members.Add(pm!);
                    }
                    break;
                case IMethodSymbol method:
                    if (TryParseStaticMethod(method, out MemberModel? mm))
                    {
                        members.Add(mm!);
                    }
                    break;
            }
        }

        string ns = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        string accessibility = classSymbol.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            _ => "internal",
        };

        SyntaxReference? syntaxRef = classSymbol.DeclaringSyntaxReferences.FirstOrDefault();
        bool isPartial = false;
        if (syntaxRef?.GetSyntax() is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax cds)
        {
            isPartial = cds.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword));
        }

        return new CdacTypeModel(
            Namespace: ns,
            ClassName: classSymbol.Name,
            Accessibility: accessibility,
            IsSealed: classSymbol.IsSealed,
            IsPartial: isPartial,
            Names: names,
            HasTypeHandle: hasTypeHandle,
            Members: EquatableArray<MemberModel>.FromEnumerable(members));
    }
    private static bool GetNamedBool(AttributeData attr, string name)
    {
        foreach (KeyValuePair<string, TypedConstant> kv in attr.NamedArguments)
        {
            if (kv.Key == name && kv.Value.Value is bool b)
                return b;
        }
        return false;
    }

    private static string[]? ReadStringArray(TypedConstant constant)
    {
        if (constant.Kind != TypedConstantKind.Array || constant.IsNull)
            return null;

        var result = new List<string>();
        foreach (TypedConstant item in constant.Values)
        {
            if (item.Value is string s)
                result.Add(s);
        }
        return result.ToArray();
    }

    private static string[]? GetNamedStringArray(AttributeData attr, string name)
    {
        foreach (KeyValuePair<string, TypedConstant> kv in attr.NamedArguments)
        {
            if (kv.Key == name)
                return ReadStringArray(kv.Value);
        }
        return null;
    }

    private static string? GetUnderlyingBoolType(AttributeData attr, string name)
    {
        foreach (KeyValuePair<string, TypedConstant> kv in attr.NamedArguments)
        {
            if (kv.Key == name && kv.Value.Value is INamedTypeSymbol typeSymbol)
            {
                // Use MinimallyQualified to get C# keywords (int, byte, etc.) instead of System.Int32
                return typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            }
        }
        return null;
    }

    /// <summary>
    /// Build the priority-ordered candidate names for a member: explicit
    /// positional names first, with the C# property name appended as the
    /// lowest-priority fallback when <paramref name="usePropertyName"/> is
    /// true (the default). The property name is skipped if it's already
    /// present in <paramref name="names"/> to avoid duplicate lookups.
    /// </summary>
    private static EquatableArray<string> ComputeFieldNames(string propertyName, string[]? names, bool usePropertyName)
    {
        var result = new List<string>();
        if (names is not null)
        {
            foreach (string n in names)
            {
                result.Add(n);
            }
        }
        if (usePropertyName && !result.Contains(propertyName))
        {
            result.Add(propertyName);
        }
        if (result.Count == 0)
        {
            // UsePropertyName=false with no explicit names; default to the
            // property name anyway so the cascade has something to try.
            result.Add(propertyName);
        }
        return EquatableArray<string>.FromEnumerable(result);
    }

    private static bool TryParseProperty(IPropertySymbol prop, out MemberModel? model)
    {
        model = null;
        AttributeData? fieldAttr = null;
        AttributeData? fieldOffsetAttr = null;
        AttributeData? addrAttr = null;
        AttributeData? startAttr = null;

        foreach (AttributeData a in prop.GetAttributes())
        {
            string fqn = a.AttributeClass?.ToDisplayString() ?? string.Empty;
            if (fqn == FieldAttributeFqn)
            {
                fieldAttr = a;
            }
            else if (fqn == RawOffsetAttributeFqn)
            {
                fieldOffsetAttr = a;
            }
            else if (fqn == FieldAddressAttributeFqn)
            {
                addrAttr = a;
            }
            else if (fqn == InstanceDataStartAttributeFqn)
            {
                startAttr = a;
            }
        }

        if (fieldAttr is null && fieldOffsetAttr is null && addrAttr is null && startAttr is null)
        {
            return false;
        }

        if (fieldOffsetAttr is not null)
        {
            int offset = fieldOffsetAttr.ConstructorArguments.Length > 0 && fieldOffsetAttr.ConstructorArguments[0].Value is int o ? o : 0;
            bool littleEndian = GetNamedBool(fieldOffsetAttr, "LittleEndian");
            (FieldReadKind readKind, string? dataTypeArg, bool isNullable) = ClassifyFieldRead(prop, isPointer: false);
            string fqnType = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            model = new MemberModel(
                Name: prop.Name,
                Kind: MemberKind.Field,
                DescriptorOrFieldName: prop.Name,
                PropertyOrReturnTypeFqn: fqnType,
                ReadKind: readKind,
                DataTypeArgumentFqn: dataTypeArg,
                IsOptional: false,
                IsNullable: isNullable,
                RawOffset: offset,
                LittleEndian: littleEndian,
                HasSetter: false,
                BoolUnderlyingType: null,
                Names: EquatableArray<string>.FromEnumerable(new[] { prop.Name }));
            return true;
        }

        if (fieldAttr is not null)
        {
            // [Field(params string[] names)] -- the positional args (if any)
            // become the candidate name list. We also accept a Names = [..]
            // named-arg form, which the constructor populates the same way.
            string[]? ctorNames = fieldAttr.ConstructorArguments.Length > 0 ? ReadStringArray(fieldAttr.ConstructorArguments[0]) : null;
            string[]? rawNames = ctorNames ?? GetNamedStringArray(fieldAttr, "Names");
            bool usePropertyName = !fieldAttr.NamedArguments.Any(kv => kv.Key == "UsePropertyName" && kv.Value.Value is false);

            bool isPointer = GetNamedBool(fieldAttr, "Pointer");
            string? boolUnderlyingType = GetUnderlyingBoolType(fieldAttr, "UnderlyingBoolType");
            (FieldReadKind readKind, string? dataTypeArg, bool isNullable) = ClassifyFieldRead(prop, isPointer);
            string fqnType = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // DescriptorOrFieldName is retained for static-accessor emit paths.
            // For [Field] codegen, only the Names array is used.
            string descriptorName = rawNames is { Length: > 0 } ? rawNames[0] : prop.Name;

            model = new MemberModel(
                Name: prop.Name,
                Kind: MemberKind.Field,
                DescriptorOrFieldName: descriptorName,
                PropertyOrReturnTypeFqn: fqnType,
                ReadKind: readKind,
                DataTypeArgumentFqn: dataTypeArg,
                IsOptional: isNullable,
                IsNullable: isNullable,
                RawOffset: null,
                LittleEndian: false,
                HasSetter: GetNamedBool(fieldAttr, "Writable"),
                BoolUnderlyingType: boolUnderlyingType,
                Names: ComputeFieldNames(prop.Name, rawNames, usePropertyName));
            return true;
        }

        if (addrAttr is not null)
        {
            string[]? ctorNames = addrAttr.ConstructorArguments.Length > 0 ? ReadStringArray(addrAttr.ConstructorArguments[0]) : null;
            string[]? rawNames = ctorNames ?? GetNamedStringArray(addrAttr, "Names");
            bool usePropertyName = !addrAttr.NamedArguments.Any(kv => kv.Key == "UsePropertyName" && kv.Value.Value is false);
            string descriptorName = rawNames is { Length: > 0 } ? rawNames[0] : prop.Name;

            bool isNullable = prop.Type is INamedTypeSymbol named
                && named.IsGenericType
                && named.ConstructedFrom.ToDisplayString() == "System.Nullable<T>";

            model = new MemberModel(
                Name: prop.Name,
                Kind: MemberKind.FieldAddress,
                DescriptorOrFieldName: descriptorName,
                PropertyOrReturnTypeFqn: prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ReadKind: FieldReadKind.Pointer,
                DataTypeArgumentFqn: null,
                IsOptional: isNullable,
                IsNullable: isNullable,
                RawOffset: null,
                LittleEndian: false,
                HasSetter: false,
                BoolUnderlyingType: null,
                Names: ComputeFieldNames(prop.Name, rawNames, usePropertyName));
            return true;
        }

        if (startAttr is not null)
        {
            model = new MemberModel(
                Name: prop.Name,
                Kind: MemberKind.InstanceDataStart,
                DescriptorOrFieldName: prop.Name,
                PropertyOrReturnTypeFqn: prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ReadKind: FieldReadKind.Pointer,
                DataTypeArgumentFqn: null,
                IsOptional: false,
                IsNullable: false,
                RawOffset: null,
                LittleEndian: false,
                HasSetter: false,
                BoolUnderlyingType: null,
                Names: EquatableArray<string>.FromEnumerable(new[] { prop.Name }));
            return true;
        }

        return false;
    }

    private static bool TryParseStaticMethod(IMethodSymbol method, out MemberModel? model)
    {
        model = null;
        AttributeData? a = method.GetAttributes()
            .FirstOrDefault(x => x.AttributeClass?.ToDisplayString() is
                StaticAddressAttributeFqn or
                StaticReferenceAttributeFqn or
                ThreadStaticAddressAttributeFqn);
        if (a is null)
        {
            return false;
        }

        MemberKind kind = a.AttributeClass!.ToDisplayString() switch
        {
            StaticAddressAttributeFqn => MemberKind.StaticAddress,
            StaticReferenceAttributeFqn => MemberKind.StaticReference,
            ThreadStaticAddressAttributeFqn => MemberKind.ThreadStaticAddress,
            _ => MemberKind.StaticAddress,
        };

        string fieldName = (a.ConstructorArguments.Length > 0 && a.ConstructorArguments[0].Value is string s) ? s : method.Name;

        model = new MemberModel(
            Name: method.Name,
            Kind: kind,
            DescriptorOrFieldName: fieldName,
            PropertyOrReturnTypeFqn: method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ReadKind: FieldReadKind.Pointer,
            DataTypeArgumentFqn: null,
            IsOptional: false,
            IsNullable: false,
            RawOffset: null,
            LittleEndian: false,
            HasSetter: false,
            BoolUnderlyingType: null,
            Names: EquatableArray<string>.FromEnumerable(new[] { fieldName }));
        return true;
    }

    private static (FieldReadKind, string?, bool) ClassifyFieldRead(IPropertySymbol prop, bool isPointer)
    {
        ITypeSymbol type = prop.Type;
        bool isNullable = false;
        if (type is INamedTypeSymbol named && named.IsGenericType
            && named.ConstructedFrom.ToDisplayString() == "System.Nullable<T>")
        {
            isNullable = true;
            type = named.TypeArguments[0];
        }
        else if (ImplementsIData(type) && prop.NullableAnnotation == NullableAnnotation.Annotated)
        {
            // IData<T>? field: emitter treats it as optional (ContainsKey
            // guard + default(null) when the descriptor omits the field).
            isNullable = true;
        }

        string fqn = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (ImplementsIData(type))
        {
            return (isPointer ? FieldReadKind.DataPointer : FieldReadKind.DataInPlace, fqn, isNullable);
        }

        return fqn switch
        {
            "bool"
                => (FieldReadKind.Bool, null, isNullable),
            "global::Microsoft.Diagnostics.DataContractReader.TargetPointer"
                => (FieldReadKind.Pointer, null, isNullable),
            "global::Microsoft.Diagnostics.DataContractReader.TargetNUInt"
                => (FieldReadKind.NUInt, null, isNullable),
            "global::Microsoft.Diagnostics.DataContractReader.TargetNInt"
                => (FieldReadKind.NInt, null, isNullable),
            "global::Microsoft.Diagnostics.DataContractReader.TargetCodePointer"
                => (FieldReadKind.CodePointer, null, isNullable),
            _ => (FieldReadKind.Primitive, fqn, isNullable),
        };
    }

    private static bool ImplementsIData(ITypeSymbol type)
    {
        foreach (INamedTypeSymbol i in type.AllInterfaces)
        {
            if (i.OriginalDefinition.ToDisplayString() == "Microsoft.Diagnostics.DataContractReader.Data.IData<TSelf>")
            {
                return true;
            }
        }

        return false;
    }
}
