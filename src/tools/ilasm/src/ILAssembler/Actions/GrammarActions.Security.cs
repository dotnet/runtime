// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    private readonly Stack<SecurityAttributeSetFrame> _securityAttributeSetFrames = new();
    private readonly Stack<SecurityNameValuePairsFrame> _securityNameValuePairFrames = new();

    private abstract record SecurityDeclarationValue(DeclarativeSecurityAction Action);

    private sealed record PermissionDeclarationValue(
        DeclarativeSecurityAction Action,
        object PermissionType,
        object? Value) : SecurityDeclarationValue(Action);

    private sealed record RawPermissionSetValue(
        DeclarativeSecurityAction Action,
        ImmutableArray<byte> Value) : SecurityDeclarationValue(Action);

    private sealed record StringPermissionSetValue(
        DeclarativeSecurityAction Action,
        string Value) : SecurityDeclarationValue(Action);

    private sealed record AttributePermissionSetValue(
        DeclarativeSecurityAction Action,
        ImmutableArray<SecurityAttributeValue> Attributes) : SecurityDeclarationValue(Action);

    private sealed record SecurityAttributeValue(
        string? Name,
        TypeSpecificationValue? Type,
        ImmutableArray<CustomAttributeNamedArgumentValue> Arguments);

    private sealed record SecurityNameValuePairValue(string Name, SecurityCaValue Value);

    private abstract record SecurityCaValue;

    private sealed record SecurityBooleanValue(bool Value) : SecurityCaValue;

    private sealed record SecurityInt32Value(int Value) : SecurityCaValue;

    private sealed record SecurityStringValue(string Value) : SecurityCaValue;

    private sealed record SecurityEnumValue(
        ClassNameValue Type,
        byte Size,
        int Value) : SecurityCaValue;

    private sealed class SecurityAttributeSetFrame
    {
        public SecurityAttributeSetFrame(CILParser.SecAttrSetBlobContext owner)
        {
            Owner = owner;
        }

        public CILParser.SecAttrSetBlobContext Owner { get; }

        public ImmutableArray<SecurityAttributeValue>.Builder? Attributes { get; set; }
    }

    private sealed class SecurityNameValuePairsFrame
    {
        public SecurityNameValuePairsFrame(CILParser.NameValPairsContext owner)
        {
            Owner = owner;
        }

        public CILParser.NameValPairsContext Owner { get; }

        public ImmutableArray<SecurityNameValuePairValue>.Builder? Pairs { get; set; }
    }

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

    internal object CreateNamedPermissionDeclaration(
        DeclarativeSecurityAction action,
        object? permissionType,
        object? pairs)
        => new PermissionDeclarationValue(
            action,
            GetTypeSpecificationValue(permissionType),
            pairs);

    internal object CreateStructuredPermissionDeclaration(
        DeclarativeSecurityAction action,
        object? permissionType,
        object? value)
        => new PermissionDeclarationValue(
            action,
            GetTypeSpecificationValue(permissionType),
            value);

    internal object CreateEmptyPermissionDeclaration(
        DeclarativeSecurityAction action,
        object? permissionType)
        => new PermissionDeclarationValue(
            action,
            GetTypeSpecificationValue(permissionType),
            null);

    internal object CreateRawPermissionSetDeclaration(
        DeclarativeSecurityAction action,
        ImmutableArray<byte> value)
        => new RawPermissionSetValue(action, value);

    internal object CreateStringPermissionSetDeclaration(
        DeclarativeSecurityAction action,
        string value)
        => new StringPermissionSetValue(action, value);

    internal object CreateAttributePermissionSetDeclaration(
        DeclarativeSecurityAction action,
        object? value)
        => new AttributePermissionSetValue(action, GetSecurityAttributes(value));

    internal void EndSecurityDeclaration(CILParser.SecDeclContext context)
    {
        context.HasSyntaxError = EndSemanticRoot(context);
        if (context.HasSyntaxError)
        {
            context.Value = null;
        }
    }

    internal void BeginSecurityAttributeSet(CILParser.SecAttrSetBlobContext context)
        => _securityAttributeSetFrames.Push(new(context));

    internal void AddSecurityAttribute(
        CILParser.SecAttrSetBlobContext context,
        object? value)
    {
        if (TryGetSecurityAttributeSetFrame(context) is { } frame &&
            value is SecurityAttributeValue attribute)
        {
            (frame.Attributes ??= ImmutableArray.CreateBuilder<SecurityAttributeValue>())
                .Add(attribute);
        }
    }

    internal object EndSecurityAttributeSet(CILParser.SecAttrSetBlobContext context)
    {
        if (TryGetSecurityAttributeSetFrame(context) is not { } frame)
        {
            return ImmutableArray<SecurityAttributeValue>.Empty;
        }

        _securityAttributeSetFrames.Pop();
        return frame.Attributes?.ToImmutable() ?? ImmutableArray<SecurityAttributeValue>.Empty;
    }

    internal object CreateNamedSecurityAttribute(IToken name, object? arguments)
        => new SecurityAttributeValue(
            StringHelpers.ParseQuotedString(name.Text),
            null,
            GetSecurityAttributeArguments(arguments));

    internal object CreateTypedSecurityAttribute(object? type, object? arguments)
        => new SecurityAttributeValue(
            null,
            GetTypeSpecificationValue(type),
            GetSecurityAttributeArguments(arguments));

    internal void BeginSecurityNameValuePairs(CILParser.NameValPairsContext context)
        => _securityNameValuePairFrames.Push(new(context));

    internal void AddSecurityNameValuePair(CILParser.NameValPairsContext context, object? value)
    {
        if (TryGetSecurityNameValuePairsFrame(context) is { } frame &&
            value is SecurityNameValuePairValue pair)
        {
            (frame.Pairs ??= ImmutableArray.CreateBuilder<SecurityNameValuePairValue>())
                .Add(pair);
        }
    }

    internal object EndSecurityNameValuePairs(CILParser.NameValPairsContext context)
    {
        if (TryGetSecurityNameValuePairsFrame(context) is not { } frame)
        {
            return ImmutableArray<SecurityNameValuePairValue>.Empty;
        }

        _securityNameValuePairFrames.Pop();
        return frame.Pairs?.ToImmutable() ?? ImmutableArray<SecurityNameValuePairValue>.Empty;
    }

    internal object CreateSecurityNameValuePair(string name, object? value)
        => new SecurityNameValuePairValue(name, GetSecurityCaValue(value));

    internal object CreateSecurityBooleanValue(bool value) => new SecurityBooleanValue(value);

    internal object CreateSecurityInt32Value(IToken value)
        => new SecurityInt32Value(ParseInt32(value));

    internal object CreateSecurityStringValue(string value) => new SecurityStringValue(value);

    internal object CreateSecurityEnumValue(object? type, IToken kind, IToken value)
        => new SecurityEnumValue(
            GetClassNameValue(type),
            kind.Text switch
            {
                "int8" => 1,
                "int16" => 2,
                "int32" => 4,
                _ => throw new UnreachableException(),
            },
            ParseInt32(value));

    internal object CreateSecurityEnumValue(object? type, IToken value)
        => new SecurityEnumValue(GetClassNameValue(type), 4, ParseInt32(value));

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

    private BlobBuilder MaterializeSecurityCaValue(SecurityCaValue value)
    {
        BlobBuilder blob = new();
        switch (value)
        {
            case SecurityBooleanValue boolean:
                blob.WriteByte((byte)SerializationTypeCode.Boolean);
                blob.WriteBoolean(boolean.Value);
                break;
            case SecurityInt32Value integer:
                blob.WriteByte((byte)SerializationTypeCode.Int32);
                blob.WriteInt32(integer.Value);
                break;
            case SecurityStringValue text:
                blob.WriteUTF8(text.Value);
                blob.WriteByte(0);
                break;
            case SecurityEnumValue enumeration:
                blob.WriteByte((byte)SerializationTypeCode.Enum);
                EntityRegistry.TypeEntity enumType = ResolveClassName(enumeration.Type);
                blob.WriteUTF8(
                    (enumType as EntityRegistry.IHasReflectionNotation)?.ReflectionNotation
                        ?? string.Empty);
                blob.WriteByte(0);
                blob.WriteByte(enumeration.Size);
                blob.WriteInt32(enumeration.Value);
                break;
            default:
                throw new UnreachableException();
        }

        return blob;
    }

    private static ImmutableArray<SecurityAttributeValue> GetSecurityAttributes(object? value)
        => value is ImmutableArray<SecurityAttributeValue> attributes ? attributes : [];

    private static ImmutableArray<CustomAttributeNamedArgumentValue> GetSecurityAttributeArguments(
        object? value)
        => value is ImmutableArray<CustomAttributeNamedArgumentValue> arguments ? arguments : [];

    private static SecurityCaValue GetSecurityCaValue(object? value)
        => value as SecurityCaValue ?? new SecurityInt32Value(0);

    private SecurityAttributeSetFrame? TryGetSecurityAttributeSetFrame(
        CILParser.SecAttrSetBlobContext context)
    {
        Debug.Assert(_securityAttributeSetFrames.Count > 0);
        SecurityAttributeSetFrame? frame =
            _securityAttributeSetFrames.Count == 0 ? null : _securityAttributeSetFrames.Peek();
        Debug.Assert(frame is null || ReferenceEquals(frame.Owner, context));
        return frame is not null && ReferenceEquals(frame.Owner, context) ? frame : null;
    }

    private SecurityNameValuePairsFrame? TryGetSecurityNameValuePairsFrame(
        CILParser.NameValPairsContext context)
    {
        Debug.Assert(_securityNameValuePairFrames.Count > 0);
        SecurityNameValuePairsFrame? frame =
            _securityNameValuePairFrames.Count == 0 ? null : _securityNameValuePairFrames.Peek();
        Debug.Assert(frame is null || ReferenceEquals(frame.Owner, context));
        return frame is not null && ReferenceEquals(frame.Owner, context) ? frame : null;
    }

    GrammarResult ICILVisitor<GrammarResult>.VisitSecAction(CILParser.SecActionContext context)
        => VisitSecAction(context);

    public static GrammarResult.Literal<DeclarativeSecurityAction> VisitSecAction(
        CILParser.SecActionContext context)
        => new(context.Value);

    GrammarResult ICILVisitor<GrammarResult>.VisitSecDecl(CILParser.SecDeclContext context)
        => VisitSecDecl(context);

    public GrammarResult.Literal<EntityRegistry.DeclarativeSecurityAttributeEntity?> VisitSecDecl(
        CILParser.SecDeclContext context)
        => new(
            context.Value is SecurityDeclarationValue value
                ? MaterializeSecurityDeclaration(value, context.Start)
                : null);

    GrammarResult ICILVisitor<GrammarResult>.VisitSecAttrSetBlob(
        CILParser.SecAttrSetBlobContext context)
        => VisitSecAttrSetBlob(context);

    public GrammarResult.FormattedBlob VisitSecAttrSetBlob(
        CILParser.SecAttrSetBlobContext context)
        => new(MaterializeSecurityAttributeSet(GetSecurityAttributes(context.Value)));

    GrammarResult ICILVisitor<GrammarResult>.VisitSecAttrBlob(CILParser.SecAttrBlobContext context)
        => VisitSecAttrBlob(context);

    public GrammarResult.FormattedBlob VisitSecAttrBlob(CILParser.SecAttrBlobContext context)
        => new(
            MaterializeSecurityAttribute(
                context.Value as SecurityAttributeValue
                    ?? new SecurityAttributeValue(string.Empty, null, [])));

    GrammarResult ICILVisitor<GrammarResult>.VisitNameValPairs(CILParser.NameValPairsContext context)
        => VisitNameValPairs(context);

    public GrammarResult.Sequence<KeyValuePair<string, BlobBuilder>> VisitNameValPairs(
        CILParser.NameValPairsContext context)
    {
        ImmutableArray<SecurityNameValuePairValue> values =
            context.Value is ImmutableArray<SecurityNameValuePairValue> pairs ? pairs : [];
        ImmutableArray<KeyValuePair<string, BlobBuilder>>.Builder result =
            ImmutableArray.CreateBuilder<KeyValuePair<string, BlobBuilder>>(values.Length);
        foreach (SecurityNameValuePairValue pair in values)
        {
            result.Add(new(pair.Name, MaterializeSecurityCaValue(pair.Value)));
        }

        return new(result.MoveToImmutable());
    }

    GrammarResult ICILVisitor<GrammarResult>.VisitNameValPair(CILParser.NameValPairContext context)
        => VisitNameValPair(context);

    public GrammarResult.Literal<KeyValuePair<string, BlobBuilder>> VisitNameValPair(
        CILParser.NameValPairContext context)
    {
        SecurityNameValuePairValue value =
            context.Value as SecurityNameValuePairValue
                ?? new SecurityNameValuePairValue(string.Empty, new SecurityInt32Value(0));
        return new(new(value.Name, MaterializeSecurityCaValue(value.Value)));
    }

    GrammarResult ICILVisitor<GrammarResult>.VisitCaValue(CILParser.CaValueContext context)
        => VisitCaValue(context);

    public GrammarResult.FormattedBlob VisitCaValue(CILParser.CaValueContext context)
        => new(MaterializeSecurityCaValue(GetSecurityCaValue(context.Value)));
}
#pragma warning restore CA1822
