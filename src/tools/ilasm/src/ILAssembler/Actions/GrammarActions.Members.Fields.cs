// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    private readonly Stack<FieldDeclarationFrame> _fieldDeclarationFrames = new();

    private sealed class FieldDeclarationFrame
    {
        public FieldDeclarationFrame(CILParser.FieldDeclContext owner, int initialSyntaxErrorCount)
        {
            Owner = owner;
            InitialSyntaxErrorCount = initialSyntaxErrorCount;
        }

        public CILParser.FieldDeclContext Owner { get; }

        public int InitialSyntaxErrorCount { get; }

        public FieldAttributes Attributes { get; set; }

        public MarshallingDescriptorValue Marshalling { get; set; } =
            GetMarshallingDescriptorValue(null);
    }

    internal void BeginFieldDeclaration(CILParser.FieldDeclContext context)
    {
        ClearPendingCustomAttributeOwners();
        _fieldDeclarationFrames.Push(new(context, _syntaxErrorCount));
    }

    internal void AddFieldAttribute(CILParser.FieldDeclContext context, object? value)
    {
        if (TryGetFieldDeclarationFrame(context) is { } frame)
        {
            frame.Attributes = ApplyAttribute(
                frame.Attributes,
                GetAttributeValue<FieldAttributes>(value));
        }
    }

    internal void SetFieldMarshalling(CILParser.FieldDeclContext context, object? value)
    {
        if (TryGetFieldDeclarationFrame(context) is { } frame)
        {
            frame.Marshalling = GetMarshallingDescriptorValue(value);
        }
    }

    internal object CreateFieldDeclaration(
        CILParser.FieldDeclContext context,
        CILParser.RepeatOptContext offset,
        object? fieldType,
        string name,
        string? dataDeclarationName,
        object? initializer)
    {
        FieldDeclarationFrame? frame = TryGetFieldDeclarationFrame(context);
        if (frame is null ||
            frame.InitialSyntaxErrorCount != _syntaxErrorCount ||
            context.exception is not null)
        {
            return FieldDeclarationValue.Error;
        }

        FieldAttributes attributes = frame.Attributes;
        if (attributes.HasFlag(FieldAttributes.RTSpecialName))
        {
            attributes |= FieldAttributes.SpecialName;
        }

        return new FieldDeclarationValue(
            true,
            attributes,
            GetTypeValue(fieldType),
            name,
            frame.Marshalling,
            dataDeclarationName,
            offset.HasValue ? offset.Value : null,
            initializer);
    }

    internal void DefineField(CILParser.FieldDeclContext context, object? value)
    {
        _ = context;
        FieldDeclarationValue declaration = GetFieldDeclarationValue(value);
        if (!declaration.IsValid)
        {
            return;
        }

        BlobBuilder signature = new();
        _ = new BlobEncoder(signature).Field();
        MaterializeType(declaration.FieldType).WriteContentTo(signature);

        EntityRegistry.FieldDefinitionEntity? field =
            EntityRegistry.CreateUnrecordedFieldDefinition(
                declaration.Attributes,
                _currentTypeDefinition.PeekOrDefault() ?? _entityRegistry.ModuleType,
                declaration.Name,
                signature);
        _lastFieldDefinition = field;
        _pendingClassCustomAttributeOwner = field;

        if (field is null)
        {
            return;
        }

        field.MarshallingDescriptor = MaterializeMarshallingDescriptor(declaration.Marshalling);
        field.DataDeclarationName = declaration.DataDeclarationName;
        field.Offset = declaration.Offset;
        if (declaration.ConstantValue is not NoConstantSentinel)
        {
            field.ConstantValue = declaration.ConstantValue;
            field.HasConstant = true;
        }
    }
    internal void EndFieldDeclaration(CILParser.FieldDeclContext context)
    {
        if (_fieldDeclarationFrames.Count == 0)
        {
            return;
        }

        FieldDeclarationFrame frame = _fieldDeclarationFrames.Peek();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        if (ReferenceEquals(frame.Owner, context))
        {
            _fieldDeclarationFrames.Pop();
        }
    }

    internal object CreateFieldAttribute(IToken token)
        => token.Text switch
        {
            "static" => new AttributeValue<FieldAttributes>(FieldAttributes.Static, 0, true),
            "public" => new AttributeValue<FieldAttributes>(
                FieldAttributes.Public,
                FieldAttributes.FieldAccessMask,
                true),
            "private" => new AttributeValue<FieldAttributes>(
                FieldAttributes.Private,
                FieldAttributes.FieldAccessMask,
                true),
            "family" => new AttributeValue<FieldAttributes>(
                FieldAttributes.Family,
                FieldAttributes.FieldAccessMask,
                true),
            "initonly" => new AttributeValue<FieldAttributes>(FieldAttributes.InitOnly, 0, true),
            "rtspecialname" => new AttributeValue<FieldAttributes>(FieldAttributes.RTSpecialName, 0, true),
            "specialname" => new AttributeValue<FieldAttributes>(FieldAttributes.SpecialName, 0, true),
            "assembly" => new AttributeValue<FieldAttributes>(
                FieldAttributes.Assembly,
                FieldAttributes.FieldAccessMask,
                true),
            "famandassem" => new AttributeValue<FieldAttributes>(
                FieldAttributes.FamANDAssem,
                FieldAttributes.FieldAccessMask,
                true),
            "famorassem" => new AttributeValue<FieldAttributes>(
                FieldAttributes.FamORAssem,
                FieldAttributes.FieldAccessMask,
                true),
            "privatescope" => new AttributeValue<FieldAttributes>(
                FieldAttributes.PrivateScope,
                FieldAttributes.FieldAccessMask,
                true),
            "literal" => new AttributeValue<FieldAttributes>(FieldAttributes.Literal, 0, true),
#pragma warning disable SYSLIB0050
            "notserialized" => new AttributeValue<FieldAttributes>(FieldAttributes.NotSerialized, 0, true),
#pragma warning restore SYSLIB0050
            "volatile" => new AttributeValue<FieldAttributes>(0, 0, true),
            _ => throw new UnreachableException(),
        };

    internal object CreateRawFieldAttribute(IToken token)
        => new AttributeValue<FieldAttributes>((FieldAttributes)ParseInt32(token), 0, false);

    internal string GetFieldDataName(IToken token)
        => ParseIdentifier(token);

    internal string GetFieldDataOffset(IToken token)
        => ParseInt32(token).ToString(CultureInfo.InvariantCulture);

    internal void BeginFieldInitializer(CILParser.InitOptContext context)
    {
        BeginSemanticRoot(context);
        context.Value = NoConstantSentinel.Instance;
    }

    internal void SetFieldInitializer(
        CILParser.InitOptContext context,
        CILParser.FieldInitContext initializer)
    {
        context.Value = VisitFieldInit(initializer).Value;
    }

    internal bool EndFieldInitializer(CILParser.InitOptContext context)
        => EndSemanticRoot(context);

    internal void SetFieldOffset(CILParser.RepeatOptContext context, IToken token)
    {
        context.Value = ParseInt32(token);
        context.HasValue = true;
    }

    GrammarResult ICILVisitor<GrammarResult>.VisitAtOpt(CILParser.AtOptContext context)
        => VisitAtOpt(context);

    public static GrammarResult.Literal<string?> VisitAtOpt(CILParser.AtOptContext context)
        => new(context.Value);

    GrammarResult ICILVisitor<GrammarResult>.VisitFieldAttr(CILParser.FieldAttrContext context)
        => VisitFieldAttr(context);

    public static GrammarResult.Flag<FieldAttributes> VisitFieldAttr(
        CILParser.FieldAttrContext context)
    {
        AttributeValue<FieldAttributes> attribute =
            GetAttributeValue<FieldAttributes>(context.Value);
        return new(attribute.Value, attribute.ShouldAppend, attribute.GroupMask);
    }

    GrammarResult ICILVisitor<GrammarResult>.VisitFieldDecl(CILParser.FieldDeclContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    GrammarResult ICILVisitor<GrammarResult>.VisitFieldInit(CILParser.FieldInitContext context)
        => VisitFieldInit(context);

    public GrammarResult.Literal<object?> VisitFieldInit(CILParser.FieldInitContext context)
    {
        if (context.NULLREF() is not null)
        {
            return new(null);
        }
        if (context.compQstring() is { } composedString)
        {
            return new(VisitCompQstring(composedString).Value);
        }
        if (context.fieldSerInit() is { } serializedInitializer)
        {
            return new(ExtractConstantFromSerInit(VisitFieldSerInit(serializedInitializer).Value));
        }

        return new(null);
    }

    GrammarResult ICILVisitor<GrammarResult>.VisitFieldOrProp(CILParser.FieldOrPropContext context)
        => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

    GrammarResult ICILVisitor<GrammarResult>.VisitFieldRef(CILParser.FieldRefContext context)
        => VisitFieldRef(context);

    public GrammarResult.Literal<EntityRegistry.EntityBase> VisitFieldRef(
        CILParser.FieldRefContext context)
        => new(MaterializeFieldReference(GetFieldReferenceValue(context.Value)));

    GrammarResult ICILVisitor<GrammarResult>.VisitInitOpt(CILParser.InitOptContext context)
        => VisitInitOpt(context);

    public static GrammarResult.Literal<object?> VisitInitOpt(CILParser.InitOptContext context)
        => new(context.Value);

    GrammarResult ICILVisitor<GrammarResult>.VisitMemberRef(CILParser.MemberRefContext context)
        => VisitMemberRef(context);

    public GrammarResult.Literal<EntityRegistry.EntityBase> VisitMemberRef(
        CILParser.MemberRefContext context)
        => new(MaterializeMemberReference(GetMemberReferenceValue(context.Value)));

    GrammarResult ICILVisitor<GrammarResult>.VisitRepeatOpt(CILParser.RepeatOptContext context)
        => VisitRepeatOpt(context);

    public static GrammarResult.Literal<int?> VisitRepeatOpt(CILParser.RepeatOptContext context)
        => new(context.HasValue ? context.Value : null);

    private FieldDeclarationFrame? TryGetFieldDeclarationFrame(CILParser.FieldDeclContext context)
    {
        Debug.Assert(_fieldDeclarationFrames.Count > 0);
        FieldDeclarationFrame? frame =
            _fieldDeclarationFrames.Count == 0 ? null : _fieldDeclarationFrames.Peek();
        Debug.Assert(frame is null || ReferenceEquals(frame.Owner, context));
        return frame is not null && ReferenceEquals(frame.Owner, context) ? frame : null;
    }
}
