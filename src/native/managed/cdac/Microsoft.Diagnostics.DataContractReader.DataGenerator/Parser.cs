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

        (string? dataTypeEnumValue, string? descriptorName) = ParseDescriptorRef(cdacAttr);
        string? managedFullName = GetNamedArg<string>(cdacAttr, "ManagedFullName");
        bool isValueType = GetNamedArg<bool>(cdacAttr, "IsValueType");

        bool implementsIData = classSymbol.AllInterfaces.Any(i =>
            i.OriginalDefinition.ToDisplayString() == "Microsoft.Diagnostics.DataContractReader.Data.IData<TSelf>");

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
            DataTypeEnumValue: dataTypeEnumValue,
            DescriptorName: descriptorName,
            ManagedFullName: managedFullName,
            IsValueType: isValueType,
            ImplementsIData: implementsIData,
            HintFilePath: syntaxRef?.SyntaxTree.FilePath,
            Members: EquatableArray<MemberModel>.FromEnumerable(members));
    }

    private static (string?, string?) ParseDescriptorRef(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length == 0)
        {
            return (null, null);
        }

        TypedConstant ctor0 = attr.ConstructorArguments[0];

        if (ctor0.Kind == TypedConstantKind.Enum)
        {
            if (ctor0.Type is INamedTypeSymbol enumType && ctor0.Value is not null)
            {
                IFieldSymbol? member = enumType.GetMembers()
                    .OfType<IFieldSymbol>()
                    .FirstOrDefault(f => f.HasConstantValue && f.ConstantValue is not null && f.ConstantValue.Equals(ctor0.Value));
                return (member?.Name, null);
            }
        }

        if (ctor0.Kind == TypedConstantKind.Primitive && ctor0.Value is string s)
        {
            return (null, s);
        }

        return (null, null);
    }

    private static T? GetNamedArg<T>(AttributeData attr, string name)
    {
        foreach (KeyValuePair<string, TypedConstant> kv in attr.NamedArguments)
        {
            if (kv.Key == name && kv.Value.Value is T t)
            {
                return t;
            }
        }

        return default;
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
            bool littleEndian = GetNamedArg<bool>(fieldOffsetAttr, "LittleEndian");
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
                HasSetter: false);
            return true;
        }

        if (fieldAttr is not null)
        {
            string descriptorName = (fieldAttr.ConstructorArguments.Length > 0 && fieldAttr.ConstructorArguments[0].Value is string s)
                ? s
                : prop.Name;
            bool isPointer = GetNamedArg<bool>(fieldAttr, "Pointer");

            (FieldReadKind readKind, string? dataTypeArg, bool isNullable) = ClassifyFieldRead(prop, isPointer);
            string fqnType = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // A nullable-typed property (T? on a value type) implicitly opts into
            // the ContainsKey guard: missing descriptor fields leave the property
            // at its default.
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
                HasSetter: GetNamedArg<bool>(fieldAttr, "Writable"));
            return true;
        }

        if (addrAttr is not null)
        {
            string descriptorName = (addrAttr.ConstructorArguments.Length > 0 && addrAttr.ConstructorArguments[0].Value is string s)
                ? s
                : prop.Name;
            model = new MemberModel(
                Name: prop.Name,
                Kind: MemberKind.FieldAddress,
                DescriptorOrFieldName: descriptorName,
                PropertyOrReturnTypeFqn: prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ReadKind: FieldReadKind.Pointer,
                DataTypeArgumentFqn: null,
                IsOptional: false,
                IsNullable: false,
                RawOffset: null,
                LittleEndian: false,
                HasSetter: false);
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
                HasSetter: false);
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
                HasSetter: false);
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
