// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    internal CILParser.FieldDeclarationBuilder PrepareFieldDeclaration()
    {
        ClearPendingCustomAttributeOwners();
        return new CILParser.FieldDeclarationBuilder();
    }

    internal void AddFieldAttribute(
        CILParser.FieldDeclarationBuilder builder,
        CILParser.AttributeValue<FieldAttributes> value)
        => builder.Attributes = ApplyAttribute(builder.Attributes, value);

    internal void SetFieldMarshalling(
        CILParser.FieldDeclarationBuilder builder,
        MarshallingDescriptorValue value)
        => builder.Marshalling = value;

    internal FieldDeclarationValue CreateFieldDeclaration(
        CILParser.FieldDeclContext context,
        CILParser.FieldDeclarationBuilder builder,
        int initialSyntaxErrorCount,
        CILParser.RepeatOptContext offset,
        TypeValue fieldType,
        string name,
        string? dataDeclarationName,
        FieldInitializerValue initializer)
    {
        if (HasSyntaxErrorsSince(initialSyntaxErrorCount) ||
            context.exception is not null)
        {
            return FieldDeclarationValue.Error;
        }

        FieldAttributes attributes = builder.Attributes;
        if (attributes.HasFlag(FieldAttributes.RTSpecialName))
        {
            attributes |= FieldAttributes.SpecialName;
        }

        return new FieldDeclarationValue(
            true,
            attributes,
            fieldType,
            name,
            builder.Marshalling,
            dataDeclarationName,
            offset.HasValue ? offset.Value : null,
            initializer);
    }

    internal void DefineField(
        CILParser.FieldDeclContext context,
        FieldDeclarationValue value)
    {
        _ = context;
        if (!value.IsValid)
        {
            return;
        }

        FieldDeclarationValue declaration = value;

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
        if (declaration.Initializer.HasValue)
        {
            field.ConstantValue = declaration.Initializer.ConstantValue;
            field.HasConstant = true;
        }
    }

    internal CILParser.AttributeValue<FieldAttributes> CreateFieldAttribute(IToken token)
        => token.Text switch
        {
            "static" => new CILParser.AttributeValue<FieldAttributes>(FieldAttributes.Static, 0, true),
            "public" => new CILParser.AttributeValue<FieldAttributes>(
                FieldAttributes.Public,
                FieldAttributes.FieldAccessMask,
                true),
            "private" => new CILParser.AttributeValue<FieldAttributes>(
                FieldAttributes.Private,
                FieldAttributes.FieldAccessMask,
                true),
            "family" => new CILParser.AttributeValue<FieldAttributes>(
                FieldAttributes.Family,
                FieldAttributes.FieldAccessMask,
                true),
            "initonly" => new CILParser.AttributeValue<FieldAttributes>(FieldAttributes.InitOnly, 0, true),
            "rtspecialname" => new CILParser.AttributeValue<FieldAttributes>(FieldAttributes.RTSpecialName, 0, true),
            "specialname" => new CILParser.AttributeValue<FieldAttributes>(FieldAttributes.SpecialName, 0, true),
            "assembly" => new CILParser.AttributeValue<FieldAttributes>(
                FieldAttributes.Assembly,
                FieldAttributes.FieldAccessMask,
                true),
            "famandassem" => new CILParser.AttributeValue<FieldAttributes>(
                FieldAttributes.FamANDAssem,
                FieldAttributes.FieldAccessMask,
                true),
            "famorassem" => new CILParser.AttributeValue<FieldAttributes>(
                FieldAttributes.FamORAssem,
                FieldAttributes.FieldAccessMask,
                true),
            "privatescope" => new CILParser.AttributeValue<FieldAttributes>(
                FieldAttributes.PrivateScope,
                FieldAttributes.FieldAccessMask,
                true),
            "literal" => new CILParser.AttributeValue<FieldAttributes>(FieldAttributes.Literal, 0, true),
#pragma warning disable SYSLIB0050
            "notserialized" => new CILParser.AttributeValue<FieldAttributes>(FieldAttributes.NotSerialized, 0, true),
#pragma warning restore SYSLIB0050
            "volatile" => new CILParser.AttributeValue<FieldAttributes>(0, 0, true),
            _ => throw new UnreachableException(),
        };

    internal CILParser.AttributeValue<FieldAttributes> CreateRawFieldAttribute(IToken token)
        => new((FieldAttributes)ParseInt32(token), 0, false);

    internal string GetFieldDataName(IToken token)
        => ParseIdentifier(token);

    internal string GetFieldDataOffset(IToken token)
        => ParseInt32(token).ToString(CultureInfo.InvariantCulture);

    internal void SetFieldOffset(CILParser.RepeatOptContext context, IToken token)
    {
        context.Value = ParseInt32(token);
        context.HasValue = true;
    }

    internal EntityRegistry.EntityBase MaterializeFieldReference(
        CILParser.FieldRefContext context)
        => MaterializeFieldReference(context.Value);
}
