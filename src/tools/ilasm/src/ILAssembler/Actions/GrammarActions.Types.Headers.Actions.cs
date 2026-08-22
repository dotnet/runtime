// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    internal void PrepareNamespaceHeader()
        => ClearPendingCustomAttributeOwners();

    internal CILParser.ClassHeaderBuilder PrepareClassHeader()
    {
        ClearPendingCustomAttributeOwners();
        return new CILParser.ClassHeaderBuilder();
    }

    internal void AddClassHeaderAttribute(
        CILParser.ClassHeaderBuilder builder,
        ClassAttributeValue value)
        => builder.Attributes.Add(value);

    internal ClassHeaderValue CreateClassHeader(
        CILParser.ClassHeadContext context,
        CILParser.ClassHeaderBuilder builder,
        int initialSyntaxErrorCount,
        IToken nameToken,
        string fullName,
        ImmutableArray<GenericParameterDeclarationValue> genericParameters,
        TypeSpecificationValue? baseType,
        ImmutableArray<TypeSpecificationValue> interfaces)
    {
        if (HasSyntaxErrorsSince(initialSyntaxErrorCount) ||
            context.exception is not null)
        {
            return ClassHeaderValue.Error;
        }

        return new ClassHeaderValue(
            true,
            nameToken,
            fullName,
            builder.Attributes.ToImmutable(),
            genericParameters,
            baseType,
            interfaces);
    }

    internal ClassAttributeValue CreateClassAttribute(IToken token)
    {
        return token.Text switch
        {
            "public" => CreateClassAttribute(
                TypeAttributes.Public,
                TypeAttributes.VisibilityMask),
            "private" => CreateClassAttribute(
                TypeAttributes.NotPublic,
                TypeAttributes.VisibilityMask),
            "value" => CreateClassAttribute(
                TypeAttributes.Sealed,
                fallbackBase: EntityRegistry.WellKnownBaseType.System_ValueType,
                requireSealed: true),
            "enum" => CreateClassAttribute(
                0,
                fallbackBase: EntityRegistry.WellKnownBaseType.System_Enum),
            "interface" => CreateClassAttribute(TypeAttributes.Interface | TypeAttributes.Abstract),
            "sealed" => CreateClassAttribute(TypeAttributes.Sealed),
            "abstract" => CreateClassAttribute(TypeAttributes.Abstract),
            "auto" => CreateClassAttribute(TypeAttributes.AutoLayout, TypeAttributes.LayoutMask),
            "sequential" => CreateClassAttribute(
                TypeAttributes.SequentialLayout,
                TypeAttributes.LayoutMask),
            "explicit" => CreateClassAttribute(TypeAttributes.ExplicitLayout),
            "extended" => CreateClassAttribute(TypeAttributes.ExtendedLayout, TypeAttributes.LayoutMask),
            "ansi" => CreateClassAttribute(TypeAttributes.AnsiClass, TypeAttributes.StringFormatMask),
            "unicode" => CreateClassAttribute(
                TypeAttributes.UnicodeClass,
                TypeAttributes.StringFormatMask),
            "autochar" => CreateClassAttribute(
                TypeAttributes.AutoClass,
                TypeAttributes.StringFormatMask),
            "import" => CreateClassAttribute(TypeAttributes.Import),
#pragma warning disable SYSLIB0050
            "serializable" => CreateClassAttribute(TypeAttributes.Serializable),
#pragma warning restore SYSLIB0050
            "windowsruntime" => CreateClassAttribute(TypeAttributes.WindowsRuntime),
            "beforefieldinit" => CreateClassAttribute(TypeAttributes.BeforeFieldInit),
            "specialname" => CreateClassAttribute(TypeAttributes.SpecialName),
            "rtspecialname" => CreateClassAttribute(TypeAttributes.RTSpecialName),
            _ => throw new UnreachableException(),
        };
    }

    internal ClassAttributeValue CreateNestedClassAttribute(IToken visibility)
    {
        TypeAttributes attribute = visibility.Text switch
        {
            "public" => TypeAttributes.NestedPublic,
            "private" => TypeAttributes.NestedPrivate,
            "family" => TypeAttributes.NestedFamily,
            "assembly" => TypeAttributes.NestedAssembly,
            "famandassem" => TypeAttributes.NestedFamANDAssem,
            "famorassem" => TypeAttributes.NestedFamORAssem,
            _ => throw new UnreachableException(),
        };
        return CreateClassAttribute(attribute, TypeAttributes.VisibilityMask);
    }

    internal ClassAttributeValue CreateRawClassAttribute(IToken token)
    {
        int value = ParseInt32(token);
        bool requireSealed = false;
        EntityRegistry.WellKnownBaseType? fallbackBase = null;
        if ((value & 0x80000000) != 0)
        {
            requireSealed = true;
            fallbackBase = EntityRegistry.WellKnownBaseType.System_ValueType;
        }
        if ((value & 0x40000000) != 0)
        {
            fallbackBase = EntityRegistry.WellKnownBaseType.System_Enum;
        }

        value &= unchecked((int)~0xC0000000);
        return new ClassAttributeValue(
            new((TypeAttributes)value, 0, false),
            fallbackBase,
            requireSealed);
    }

    internal TypeSpecificationValue? CreateEmptyClassBase() => null;

    internal TypeSpecificationValue CreateClassBase(TypeSpecificationValue value)
        => value;

    internal ImmutableArray<TypeSpecificationValue> CreateEmptyInterfaceList()
        => [];

    private static ClassAttributeValue CreateClassAttribute(
        TypeAttributes value,
        TypeAttributes groupMask = 0,
        EntityRegistry.WellKnownBaseType? fallbackBase = null,
        bool requireSealed = false)
        => new(new CILParser.AttributeValue<TypeAttributes>(value, groupMask, true), fallbackBase, requireSealed);
}
#pragma warning restore CA1822
