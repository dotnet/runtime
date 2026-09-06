// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using Antlr4.Runtime;

namespace ILAssembler;

public partial class CILParser
{
    public sealed class DottedNameBuilder
    {
        internal StringBuilder Value { get; } = new();

        internal bool HasPart { get; set; }
    }

    public sealed record SourceDirectiveValue(
        bool AutoIncrement,
        int StartLine,
        int StartColumn,
        int EndLine,
        int EndColumn,
        string? DocumentPath);

    public sealed record LanguageDirectiveValue(
        string Language,
        string? Vendor,
        string? DocumentType);

    public abstract record ExceptionRangeValue
    {
        public static ExceptionRangeValue Invalid { get; } =
            new InvalidExceptionRangeValue();
    }

    public sealed record InvalidExceptionRangeValue : ExceptionRangeValue;

    public sealed record ScopeExceptionRangeValue(
        ScopeBlockContext Scope) : ExceptionRangeValue;

    public sealed record LabelExceptionRangeValue(
        string Start,
        string End) : ExceptionRangeValue;

    public sealed record OffsetExceptionRangeValue(
        int Start,
        int End) : ExceptionRangeValue;

    public abstract record ExceptionFilterValue
    {
        public static ExceptionFilterValue Invalid { get; } =
            new InvalidExceptionFilterValue();
    }

    public sealed record InvalidExceptionFilterValue : ExceptionFilterValue;

    public sealed record ScopeExceptionFilterValue(
        ScopeBlockContext Scope) : ExceptionFilterValue;

    public sealed record LabelExceptionFilterValue(
        string Label) : ExceptionFilterValue;

    public sealed record OffsetExceptionFilterValue(
        int Offset) : ExceptionFilterValue;

    public sealed class CatchTypeValue
    {
        internal CatchTypeValue(EntityRegistry.TypeEntity? type, bool isValid)
        {
            Type = type;
            IsValid = isValid;
        }

        public static CatchTypeValue Invalid { get; } = new(null, isValid: false);

        internal EntityRegistry.TypeEntity? Type { get; }

        public bool IsValid { get; }
    }

    public abstract record ExceptionClauseValue(ExceptionRangeValue Handler)
    {
        public static ExceptionClauseValue Invalid { get; } =
            new InvalidExceptionClauseValue(ExceptionRangeValue.Invalid);
    }

    public sealed record InvalidExceptionClauseValue(
        ExceptionRangeValue Handler) : ExceptionClauseValue(Handler);

    public sealed record CatchExceptionClauseValue(
        CatchTypeValue CatchType,
        ExceptionRangeValue Handler) : ExceptionClauseValue(Handler);

    public sealed record FilterExceptionClauseValue(
        ExceptionFilterValue Filter,
        ExceptionRangeValue Handler) : ExceptionClauseValue(Handler);

    public sealed record FinallyExceptionClauseValue(
        ExceptionRangeValue Handler) : ExceptionClauseValue(Handler);

    public sealed record FaultExceptionClauseValue(
        ExceptionRangeValue Handler) : ExceptionClauseValue(Handler);

    public abstract record SecurityDeclarationValue(DeclarativeSecurityAction Action);

    public abstract record PermissionDeclarationValue(
        DeclarativeSecurityAction Action,
        TypeSpecificationValue PermissionType) : SecurityDeclarationValue(Action);

    public sealed record NamedPermissionDeclarationValue(
        DeclarativeSecurityAction Action,
        TypeSpecificationValue PermissionType,
        ImmutableArray<SecurityNameValuePairValue> Pairs)
        : PermissionDeclarationValue(Action, PermissionType);

    public sealed record StructuredPermissionDeclarationValue(
        DeclarativeSecurityAction Action,
        TypeSpecificationValue PermissionType,
        CustomAttributeBlobValue Value)
        : PermissionDeclarationValue(Action, PermissionType);

    public sealed record EmptyPermissionDeclarationValue(
        DeclarativeSecurityAction Action,
        TypeSpecificationValue PermissionType)
        : PermissionDeclarationValue(Action, PermissionType);

    public sealed record RawPermissionSetValue(
        DeclarativeSecurityAction Action,
        ImmutableArray<byte> Value) : SecurityDeclarationValue(Action);

    public sealed record StringPermissionSetValue(
        DeclarativeSecurityAction Action,
        string Value) : SecurityDeclarationValue(Action);

    public sealed record AttributePermissionSetValue(
        DeclarativeSecurityAction Action,
        ImmutableArray<SecurityAttributeValue> Attributes) : SecurityDeclarationValue(Action);

    public sealed record SecurityAttributeValue(
        string? Name,
        TypeSpecificationValue? Type,
        ImmutableArray<CustomAttributeNamedArgumentValue> Arguments)
    {
        public static SecurityAttributeValue Error { get; } =
            new(null, null, []);
    }

    public sealed record SecurityNameValuePairValue(
        string Name,
        SecurityCaValue Value)
    {
        public static SecurityNameValuePairValue Error { get; } =
            new(string.Empty, SecurityCaValue.Error);
    }

    public abstract record SecurityCaValue
    {
        public static SecurityCaValue Error { get; } = new ErrorSecurityCaValue();
    }

    public sealed record ErrorSecurityCaValue : SecurityCaValue;

    public sealed record SecurityBooleanValue(bool Value) : SecurityCaValue;

    public sealed record SecurityInt32Value(int Value) : SecurityCaValue;

    public sealed record SecurityStringValue(string Value) : SecurityCaValue;

    public sealed record SecurityEnumValue(
        ClassNameValue Type,
        byte Size,
        int Value) : SecurityCaValue;

    public sealed class DataDeclarationBuilder
    {
        internal DataDeclarationBuilder(bool shouldCommit)
        {
            ShouldCommit = shouldCommit;
        }

        internal bool ShouldCommit { get; }

        internal BlobBuilder Data { get; } = new();

        internal Dictionary<string, List<Blob>>? ReferenceFixups { get; set; }

        internal string? Name { get; set; }
    }

    public sealed class SwitchInstructionBuilder
    {
        internal SwitchInstructionBuilder(IToken opcodeToken)
        {
            OpcodeToken = opcodeToken;
        }

        internal IToken OpcodeToken { get; }

        internal List<(IToken Token, bool IsOffset)> Operands { get; } = new();
    }
}
