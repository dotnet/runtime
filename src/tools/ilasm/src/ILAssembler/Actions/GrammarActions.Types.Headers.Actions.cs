// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    private readonly Stack<NamespaceHeaderFrame> _namespaceHeaderFrames = new();
    private readonly Stack<ClassHeaderFrame> _classHeaderFrames = new();
    private readonly Stack<InterfaceListFrame> _interfaceListFrames = new();

    private sealed record NamespaceHeaderFrame(
        CILParser.NameSpaceHeadContext Owner,
        int InitialSyntaxErrorCount);

    private sealed class ClassHeaderFrame
    {
        public ClassHeaderFrame(CILParser.ClassHeadContext owner, int initialSyntaxErrorCount)
        {
            Owner = owner;
            InitialSyntaxErrorCount = initialSyntaxErrorCount;
        }

        public CILParser.ClassHeadContext Owner { get; }

        public int InitialSyntaxErrorCount { get; }

        public ImmutableArray<ClassAttributeValue>.Builder Attributes { get; } =
            ImmutableArray.CreateBuilder<ClassAttributeValue>();
    }

    private sealed class InterfaceListFrame
    {
        public InterfaceListFrame(CILParser.ImplListContext owner)
        {
            Owner = owner;
        }

        public CILParser.ImplListContext Owner { get; }

        public ImmutableArray<TypeSpecificationValue>.Builder Interfaces { get; } =
            ImmutableArray.CreateBuilder<TypeSpecificationValue>();
    }

    internal void BeginNamespaceHeader(CILParser.NameSpaceHeadContext context)
    {
        ClearPendingCustomAttributeOwners();
        _namespaceHeaderFrames.Push(new(context, _syntaxErrorCount));
    }

    internal void EndNamespaceHeader(CILParser.NameSpaceHeadContext context)
    {
        if (_namespaceHeaderFrames.Count == 0)
        {
            return;
        }

        NamespaceHeaderFrame frame = _namespaceHeaderFrames.Peek();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        if (ReferenceEquals(frame.Owner, context))
        {
            _namespaceHeaderFrames.Pop();
        }
    }

    internal void BeginClassHeader(CILParser.ClassHeadContext context)
    {
        ClearPendingCustomAttributeOwners();
        _classHeaderFrames.Push(new(context, _syntaxErrorCount));
    }

    internal void AddClassHeaderAttribute(CILParser.ClassHeadContext context, object? value)
    {
        if (TryGetClassHeaderFrame(context) is { } frame)
        {
            frame.Attributes.Add(GetClassAttributeValue(value));
        }
    }

    internal object CreateClassHeader(
        CILParser.ClassHeadContext context,
        IToken nameToken,
        string fullName,
        object? genericParameters,
        object? baseType,
        object? interfaces)
    {
        ClassHeaderFrame? frame = TryGetClassHeaderFrame(context);
        if (frame is null ||
            frame.InitialSyntaxErrorCount != _syntaxErrorCount ||
            context.exception is not null)
        {
            return ClassHeaderValue.Error;
        }

        return new ClassHeaderValue(
            true,
            nameToken,
            fullName,
            frame.Attributes.ToImmutable(),
            GetGenericParameterDeclarations(genericParameters),
            baseType as TypeSpecificationValue,
            GetInterfaceTypes(interfaces));
    }

    internal void EndClassHeader(CILParser.ClassHeadContext context)
    {
        if (_classHeaderFrames.Count == 0)
        {
            return;
        }

        ClassHeaderFrame frame = _classHeaderFrames.Peek();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        if (ReferenceEquals(frame.Owner, context))
        {
            _classHeaderFrames.Pop();
        }
    }

    internal object CreateClassAttribute(IToken token)
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

    internal object CreateNestedClassAttribute(IToken visibility)
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

    internal object CreateRawClassAttribute(IToken token)
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

    internal object? CreateEmptyClassBase() => null;

    internal object CreateClassBase(object? value) => GetTypeSpecificationValue(value);

    internal object CreateEmptyInterfaceList() => ImmutableArray<TypeSpecificationValue>.Empty;

    internal void BeginInterfaceList(CILParser.ImplListContext context)
        => _interfaceListFrames.Push(new(context));

    internal void AddInterfaceType(CILParser.ImplListContext context, object? value)
    {
        if (TryGetInterfaceListFrame(context) is { } frame)
        {
            frame.Interfaces.Add(GetTypeSpecificationValue(value));
        }
    }

    internal object EndInterfaceList(CILParser.ImplListContext context)
    {
        if (_interfaceListFrames.Count == 0)
        {
            return ImmutableArray<TypeSpecificationValue>.Empty;
        }

        InterfaceListFrame frame = _interfaceListFrames.Peek();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        if (!ReferenceEquals(frame.Owner, context))
        {
            return ImmutableArray<TypeSpecificationValue>.Empty;
        }

        _interfaceListFrames.Pop();
        return frame.Interfaces.ToImmutable();
    }

    private static ClassAttributeValue CreateClassAttribute(
        TypeAttributes value,
        TypeAttributes groupMask = 0,
        EntityRegistry.WellKnownBaseType? fallbackBase = null,
        bool requireSealed = false)
        => new(new(value, groupMask, true), fallbackBase, requireSealed);

    private ClassHeaderFrame? TryGetClassHeaderFrame(CILParser.ClassHeadContext context)
    {
        Debug.Assert(_classHeaderFrames.Count > 0);
        ClassHeaderFrame? frame = _classHeaderFrames.Count == 0 ? null : _classHeaderFrames.Peek();
        Debug.Assert(frame is null || ReferenceEquals(frame.Owner, context));
        return frame is not null && ReferenceEquals(frame.Owner, context) ? frame : null;
    }

    private InterfaceListFrame? TryGetInterfaceListFrame(CILParser.ImplListContext context)
    {
        Debug.Assert(_interfaceListFrames.Count > 0);
        InterfaceListFrame? frame =
            _interfaceListFrames.Count == 0 ? null : _interfaceListFrames.Peek();
        Debug.Assert(frame is null || ReferenceEquals(frame.Owner, context));
        return frame is not null && ReferenceEquals(frame.Owner, context) ? frame : null;
    }
}
#pragma warning restore CA1822
