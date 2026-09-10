// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    internal DeclarativeSecurityAction ParseSecurityAction(IToken token)
        => token.Text switch
        {
            "request" => DeclarativeSecurityAction.Request,
            "demand" => DeclarativeSecurityAction.Demand,
            "assert" => DeclarativeSecurityAction.Assert,
            "deny" => DeclarativeSecurityAction.Deny,
            "permitonly" => DeclarativeSecurityAction.PermitOnly,
            "linkcheck" => DeclarativeSecurityAction.LinkDemand,
            "inheritcheck" => DeclarativeSecurityAction.InheritanceDemand,
            "reqmin" => DeclarativeSecurityAction.RequestMinimum,
            "reqopt" => DeclarativeSecurityAction.RequestOptional,
            "reqrefuse" => DeclarativeSecurityAction.RequestRefuse,
            "prejitgrant" => DeclarativeSecurityAction.PrejitGrant,
            "prejitdeny" => DeclarativeSecurityAction.PrejitDeny,
            "noncasdemand" => DeclarativeSecurityAction.NonCasDemand,
            "noncaslinkdemand" => DeclarativeSecurityAction.NonCasLinkDemand,
            "noncasinheritance" => DeclarativeSecurityAction.NonCasInheritanceDemand,
            _ => throw new UnreachableException(),
        };

    internal SecurityDeclarationValue CreateNamedPermissionDeclaration(
        DeclarativeSecurityAction action,
        TypeSpecificationValue permissionType,
        ImmutableArray<SecurityNameValuePairValue> pairs)
        => new NamedPermissionDeclarationValue(
            action,
            permissionType,
            pairs);

    internal SecurityDeclarationValue CreateStructuredPermissionDeclaration(
        DeclarativeSecurityAction action,
        TypeSpecificationValue permissionType,
        CustomAttributeBlobValue value)
        => new StructuredPermissionDeclarationValue(
            action,
            permissionType,
            value);

    internal SecurityDeclarationValue CreateEmptyPermissionDeclaration(
        DeclarativeSecurityAction action,
        TypeSpecificationValue permissionType)
        => new EmptyPermissionDeclarationValue(
            action,
            permissionType);

    internal SecurityDeclarationValue CreateRawPermissionSetDeclaration(
        DeclarativeSecurityAction action,
        ImmutableArray<byte> value)
        => new RawPermissionSetValue(action, value);

    internal SecurityDeclarationValue CreateStringPermissionSetDeclaration(
        DeclarativeSecurityAction action,
        string value)
        => new StringPermissionSetValue(action, value);

    internal SecurityDeclarationValue CreateAttributePermissionSetDeclaration(
        DeclarativeSecurityAction action,
        ImmutableArray<SecurityAttributeValue> value)
        => new AttributePermissionSetValue(action, value);

    internal void EndSecurityDeclaration(
        CILParser.SecDeclContext context,
        int initialSyntaxErrorCount)
    {
        context.HasSyntaxError =
            HasSyntaxErrorsSince(initialSyntaxErrorCount) ||
            context.exception is not null;
        if (context.HasSyntaxError)
        {
            context.Value = null;
        }
    }

    internal SecurityAttributeValue CreateNamedSecurityAttribute(
        IToken name,
        ImmutableArray<CustomAttributeNamedArgumentValue> arguments)
        => new SecurityAttributeValue(
            StringHelpers.ParseQuotedString(name.Text),
            null,
            arguments);

    internal SecurityAttributeValue CreateTypedSecurityAttribute(
        TypeSpecificationValue type,
        ImmutableArray<CustomAttributeNamedArgumentValue> arguments)
        => new SecurityAttributeValue(
            null,
            type,
            arguments);

    internal SecurityNameValuePairValue CreateSecurityNameValuePair(
        string name,
        SecurityCaValue value)
        => new(name, value);

    internal SecurityCaValue CreateSecurityBooleanValue(bool value)
        => new SecurityBooleanValue(value);

    internal SecurityCaValue CreateSecurityInt32Value(IToken value)
        => new SecurityInt32Value(ParseInt32(value));

    internal SecurityCaValue CreateSecurityStringValue(string value)
        => new SecurityStringValue(value);

    internal SecurityCaValue CreateSecurityEnumValue(
        ClassNameValue type,
        IToken kind,
        IToken value)
        => new SecurityEnumValue(
            type,
            kind.Text switch
            {
                "int8" => 1,
                "int16" => 2,
                "int32" => 4,
                _ => throw new UnreachableException(),
            },
            ParseInt32(value));

    internal SecurityCaValue CreateSecurityEnumValue(
        ClassNameValue type,
        IToken value)
        => new SecurityEnumValue(type, 4, ParseInt32(value));

    private EntityRegistry.DeclarativeSecurityAttributeEntity? MaterializeSecurityDeclaration(
        SecurityDeclarationValue value,
        IToken location)
    {
        if (value is PermissionDeclarationValue)
        {
            ReportError(
                DiagnosticIds.UnsupportedSecurityDeclaration,
                DiagnosticMessageTemplates.UnsupportedSecurityDeclaration,
                location);
            return null;
        }

        BlobBuilder permissionSet = value switch
        {
            RawPermissionSetValue raw => CreateRawPermissionSet(raw.Value),
            StringPermissionSetValue text => CreateStringPermissionSet(text.Value),
            AttributePermissionSetValue attributes =>
                MaterializeSecurityAttributeSet(attributes.Attributes),
            _ => throw new UnreachableException(),
        };
        return _entityRegistry.CreateDeclarativeSecurityAttribute(value.Action, permissionSet);
    }

    private static BlobBuilder CreateRawPermissionSet(ImmutableArray<byte> value)
    {
        BlobBuilder blob = new(value.Length);
        blob.WriteBytes(value);
        return blob;
    }

    private static BlobBuilder CreateStringPermissionSet(string value)
    {
        BlobBuilder blob = new();
        blob.WriteUTF16(value);
        blob.WriteUTF16("\0");
        return blob;
    }

    private BlobBuilder MaterializeSecurityAttributeSet(
        ImmutableArray<SecurityAttributeValue> attributes)
    {
        BlobBuilder blob = new();
        blob.WriteByte((byte)'.');
        blob.WriteCompressedInteger(attributes.Length);
        foreach (SecurityAttributeValue attribute in attributes)
        {
            MaterializeSecurityAttribute(attribute).WriteContentTo(blob);
        }

        return blob;
    }

    private BlobBuilder MaterializeSecurityAttribute(SecurityAttributeValue attribute)
    {
        string attributeName = attribute.Name ?? string.Empty;
        if (attribute.Type is { } type &&
            ResolveTypeSpecification(type) is EntityRegistry.IHasReflectionNotation reflectionNotation)
        {
            attributeName = reflectionNotation.ReflectionNotation;
        }

        BlobBuilder blob = new();
        blob.WriteSerializedString(attributeName);
        WriteCustomBlobNamedArguments(blob, attribute.Arguments);
        return blob;
    }

    internal EntityRegistry.DeclarativeSecurityAttributeEntity? MaterializeSecurityDeclaration(
        CILParser.SecDeclContext context)
        => context.Value is SecurityDeclarationValue value
            ? MaterializeSecurityDeclaration(value, context.Start)
            : null;

}
#pragma warning restore CA1822
