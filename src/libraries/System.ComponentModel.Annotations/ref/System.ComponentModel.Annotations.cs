// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.ComponentModel.DataAnnotations
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false)]
    [System.CLSCompliantAttribute(false)]
    public partial class AllowedValuesAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        public AllowedValuesAttribute(params object?[] values) { }
        public object?[] Values { get { throw null; } }
        public override bool IsValid(object? value) { throw null; }
    }
    public partial class AssociatedMetadataTypeTypeDescriptionProvider : System.ComponentModel.TypeDescriptionProvider
    {
        public AssociatedMetadataTypeTypeDescriptionProvider(System.Type type) { }
        public AssociatedMetadataTypeTypeDescriptionProvider(System.Type type, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type associatedMetadataType) { }
        public override System.ComponentModel.ICustomTypeDescriptor GetTypeDescriptor([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type objectType, object? instance) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    [System.ObsoleteAttribute("AssociationAttribute has been deprecated and is not supported.")]
    public sealed partial class AssociationAttribute : System.Attribute
    {
        public AssociationAttribute(string name, string thisKey, string otherKey) { }
        public bool IsForeignKey { get { throw null; } set { } }
        public string Name { get { throw null; } }
        public string OtherKey { get { throw null; } }
        public System.Collections.Generic.IEnumerable<string> OtherKeyMembers { get { throw null; } }
        public string ThisKey { get { throw null; } }
        public System.Collections.Generic.IEnumerable<string> ThisKeyMembers { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false)]
    public partial class Base64StringAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        public Base64StringAttribute() { }
        public override bool IsValid(object? value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false)]
    public partial class CompareAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The property referenced by 'otherProperty' may be trimmed. Ensure it is preserved.")]
        public CompareAttribute(string otherProperty) { }
        public string OtherProperty { get { throw null; } }
        public string? OtherPropertyDisplayName { get { throw null; } }
        public override bool RequiresValidationContext { get { throw null; } }
        public override string FormatErrorMessage(string name) { throw null; }
        protected override System.ComponentModel.DataAnnotations.ValidationResult? IsValid(object? value, System.ComponentModel.DataAnnotations.ValidationContext validationContext) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public sealed partial class ConcurrencyCheckAttribute : System.Attribute
    {
        public ConcurrencyCheckAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false)]
    public sealed partial class CreditCardAttribute : System.ComponentModel.DataAnnotations.DataTypeAttribute
    {
        public CreditCardAttribute() : base (default(System.ComponentModel.DataAnnotations.DataType)) { }
        public override bool IsValid(object? value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Field | System.AttributeTargets.Method | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=true)]
    public sealed partial class CustomValidationAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        public CustomValidationAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] System.Type validatorType, string method) { }
        public string Method { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        public System.Type ValidatorType { get { throw null; } }
        public override string FormatErrorMessage(string name) { throw null; }
        protected override System.ComponentModel.DataAnnotations.ValidationResult? IsValid(object? value, System.ComponentModel.DataAnnotations.ValidationContext validationContext) { throw null; }
    }
    public enum DataType
    {
        Custom = 0,
        DateTime = 1,
        Date = 2,
        Time = 3,
        Duration = 4,
        PhoneNumber = 5,
        Currency = 6,
        Text = 7,
        Html = 8,
        MultilineText = 9,
        EmailAddress = 10,
        Password = 11,
        Url = 12,
        ImageUrl = 13,
        CreditCard = 14,
        PostalCode = 15,
        Upload = 16,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Method | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false)]
    public partial class DataTypeAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        public DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType dataType) { }
        public DataTypeAttribute(string customDataType) { }
        public string? CustomDataType { get { throw null; } }
        public System.ComponentModel.DataAnnotations.DataType DataType { get { throw null; } }
        public System.ComponentModel.DataAnnotations.DisplayFormatAttribute? DisplayFormat { get { throw null; } protected set { } }
        public virtual string GetDataTypeName() { throw null; }
        public override bool IsValid(object? value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false)]
    [System.CLSCompliantAttribute(false)]
    public partial class DeniedValuesAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        public DeniedValuesAttribute(params object?[] values) { }
        public object?[] Values { get { throw null; } }
        public override bool IsValid(object? value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Field | System.AttributeTargets.Method | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false)]
    public sealed partial class DisplayAttribute : System.Attribute
    {
        public DisplayAttribute() { }
        public bool AutoGenerateField { get { throw null; } set { } }
        public bool AutoGenerateFilter { get { throw null; } set { } }
        public string? Description { get { throw null; } set { } }
        public string? GroupName { get { throw null; } set { } }
        public string? Name { get { throw null; } set { } }
        public int Order { get { throw null; } set { } }
        public string? Prompt { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
        public System.Type? ResourceType { get { throw null; } set { } }
        public string? ShortName { get { throw null; } set { } }
        public bool? GetAutoGenerateField() { throw null; }
        public bool? GetAutoGenerateFilter() { throw null; }
        public string? GetDescription() { throw null; }
        public string? GetGroupName() { throw null; }
        public string? GetName() { throw null; }
        public int? GetOrder() { throw null; }
        public string? GetPrompt() { throw null; }
        public string? GetShortName() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, Inherited=true, AllowMultiple=false)]
    public partial class DisplayColumnAttribute : System.Attribute
    {
        public DisplayColumnAttribute(string displayColumn) { }
        public DisplayColumnAttribute(string displayColumn, string? sortColumn) { }
        public DisplayColumnAttribute(string displayColumn, string? sortColumn, bool sortDescending) { }
        public string DisplayColumn { get { throw null; } }
        public string? SortColumn { get { throw null; } }
        public bool SortDescending { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple=false)]
    public partial class DisplayFormatAttribute : System.Attribute
    {
        public DisplayFormatAttribute() { }
        public bool ApplyFormatInEditMode { get { throw null; } set { } }
        public bool ConvertEmptyStringToNull { get { throw null; } set { } }
        public string? DataFormatString { get { throw null; } set { } }
        public bool HtmlEncode { get { throw null; } set { } }
        public string? NullDisplayText { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
        public System.Type? NullDisplayTextResourceType { get { throw null; } set { } }
        public string? GetNullDisplayText() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public sealed partial class EditableAttribute : System.Attribute
    {
        public EditableAttribute(bool allowEdit) { }
        public bool AllowEdit { get { throw null; } }
        public bool AllowInitialValue { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false)]
    public sealed partial class EmailAddressAttribute : System.ComponentModel.DataAnnotations.DataTypeAttribute
    {
        public EmailAddressAttribute() : base (default(System.ComponentModel.DataAnnotations.DataType)) { }
        public override bool IsValid(object? value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Method | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false)]
    public sealed partial class EnumDataTypeAttribute : System.ComponentModel.DataAnnotations.DataTypeAttribute
    {
        public EnumDataTypeAttribute(System.Type enumType) : base (default(System.ComponentModel.DataAnnotations.DataType)) { }
        public System.Type EnumType { get { throw null; } }
        public override bool IsValid(object? value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false)]
    public sealed partial class FileExtensionsAttribute : System.ComponentModel.DataAnnotations.DataTypeAttribute
    {
        public FileExtensionsAttribute() : base (default(System.ComponentModel.DataAnnotations.DataType)) { }
        public string Extensions { get { throw null; } set { } }
        public override string FormatErrorMessage(string name) { throw null; }
        public override bool IsValid(object? value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple=false)]
    [System.ObsoleteAttribute("FilterUIHintAttribute has been deprecated and is not supported.")]
    public sealed partial class FilterUIHintAttribute : System.Attribute
    {
        public FilterUIHintAttribute(string filterUIHint) { }
        public FilterUIHintAttribute(string filterUIHint, string? presentationLayer) { }
        public FilterUIHintAttribute(string filterUIHint, string? presentationLayer, params object?[] controlParameters) { }
        public System.Collections.Generic.IDictionary<string, object?> ControlParameters { get { throw null; } }
        public string FilterUIHint { get { throw null; } }
        public string? PresentationLayer { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public partial interface IValidatableObject
    {
        System.Collections.Generic.IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(System.ComponentModel.DataAnnotations.ValidationContext validationContext);
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public sealed partial class KeyAttribute : System.Attribute
    {
        public KeyAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false)]
    public partial class LengthAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Uses reflection to get the 'Count' property on types that don't implement ICollection. This 'Count' property may be trimmed. Ensure it is preserved.")]
        public LengthAttribute(int minimumLength, int maximumLength) { }
        public int MaximumLength { get { throw null; } }
        public int MinimumLength { get { throw null; } }
        public override string FormatErrorMessage(string name) { throw null; }
        public override bool IsValid(object? value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false)]
    public partial class MaxLengthAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Uses reflection to get the 'Count' property on types that don't implement ICollection. This 'Count' property may be trimmed. Ensure it is preserved.")]
        public MaxLengthAttribute() { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Uses reflection to get the 'Count' property on types that don't implement ICollection. This 'Count' property may be trimmed. Ensure it is preserved.")]
        public MaxLengthAttribute(int length) { }
        public int Length { get { throw null; } }
        public override string FormatErrorMessage(string name) { throw null; }
        public override bool IsValid(object? value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=false)]
    public sealed partial class MetadataTypeAttribute : System.Attribute
    {
        public MetadataTypeAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type metadataClassType) { }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
        public System.Type MetadataClassType { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false)]
    public partial class MinLengthAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Uses reflection to get the 'Count' property on types that don't implement ICollection. This 'Count' property may be trimmed. Ensure it is preserved.")]
        public MinLengthAttribute(int length) { }
        public int Length { get { throw null; } }
        public override string FormatErrorMessage(string name) { throw null; }
        public override bool IsValid(object? value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple = false)]
    [System.CLSCompliantAttribute(false)]
    public partial class NotAllowedValuesAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        public NotAllowedValuesAttribute(params object?[] values) { }
        public object?[] Values { get { throw null; } }
        public override bool IsValid(object? value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false)]
    public sealed partial class PhoneAttribute : System.ComponentModel.DataAnnotations.DataTypeAttribute
    {
        public PhoneAttribute() : base (default(System.ComponentModel.DataAnnotations.DataType)) { }
        public override bool IsValid(object? value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false)]
    public partial class RangeAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        public RangeAttribute(double minimum, double maximum) { }
        public RangeAttribute(int minimum, int maximum) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
        public RangeAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type type, string minimum, string maximum) { }
        public bool ConvertValueInInvariantCulture { get { throw null; } set { } }
        public object Maximum { get { throw null; } }
        public bool MaximumIsExclusive { get { throw null; } set { } }
        public object Minimum { get { throw null; } }
        public bool MinimumIsExclusive { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
        public System.Type OperandType { get { throw null; } }
        public bool ParseLimitsInInvariantCulture { get { throw null; } set { } }
        public override string FormatErrorMessage(string name) { throw null; }
        public override bool IsValid(object? value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false)]
    public partial class RegularExpressionAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        public RegularExpressionAttribute([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Regex")] string pattern) { }
        public System.TimeSpan MatchTimeout { get { throw null; } }
        public int MatchTimeoutInMilliseconds { get { throw null; } set { } }
        public string Pattern { get { throw null; } }
        public override string FormatErrorMessage(string name) { throw null; }
        public override bool IsValid(object? value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false)]
    public partial class RequiredAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        public RequiredAttribute() { }
        public bool AllowEmptyStrings { get { throw null; } set { } }
        public override bool IsValid(object? value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple=false)]
    public partial class ScaffoldColumnAttribute : System.Attribute
    {
        public ScaffoldColumnAttribute(bool scaffold) { }
        public bool Scaffold { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false)]
    public partial class StringLengthAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        public StringLengthAttribute(int maximumLength) { }
        public int MaximumLength { get { throw null; } }
        public int MinimumLength { get { throw null; } set { } }
        public override string FormatErrorMessage(string name) { throw null; }
        public override bool IsValid(object? value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public sealed partial class TimestampAttribute : System.Attribute
    {
        public TimestampAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple=true)]
    public partial class UIHintAttribute : System.Attribute
    {
        public UIHintAttribute(string uiHint) { }
        public UIHintAttribute(string uiHint, string? presentationLayer) { }
        public UIHintAttribute(string uiHint, string? presentationLayer, params object?[]? controlParameters) { }
        public System.Collections.Generic.IDictionary<string, object?> ControlParameters { get { throw null; } }
        public string? PresentationLayer { get { throw null; } }
        public string UIHint { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple=false)]
    public sealed partial class UrlAttribute : System.ComponentModel.DataAnnotations.DataTypeAttribute
    {
        public UrlAttribute() : base (default(System.ComponentModel.DataAnnotations.DataType)) { }
        public override bool IsValid(object? value) { throw null; }
    }
    public abstract partial class ValidationAttribute : System.Attribute
    {
        protected ValidationAttribute() { }
        protected ValidationAttribute(System.Func<string> errorMessageAccessor) { }
        protected ValidationAttribute(string errorMessage) { }
        public string? ErrorMessage { get { throw null; } set { } }
        public string? ErrorMessageResourceName { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
        public System.Type? ErrorMessageResourceType { get { throw null; } set { } }
        protected string ErrorMessageString { get { throw null; } }
        public virtual bool RequiresValidationContext { get { throw null; } }
        public virtual string FormatErrorMessage(string name) { throw null; }
        public System.ComponentModel.DataAnnotations.ValidationResult? GetValidationResult(object? value, System.ComponentModel.DataAnnotations.ValidationContext validationContext) { throw null; }
        public virtual bool IsValid(object? value) { throw null; }
        protected virtual System.ComponentModel.DataAnnotations.ValidationResult? IsValid(object? value, System.ComponentModel.DataAnnotations.ValidationContext validationContext) { throw null; }
        public void Validate(object? value, System.ComponentModel.DataAnnotations.ValidationContext validationContext) { }
        public void Validate(object? value, string name) { }
    }
    public sealed partial class ValidationContext : System.IServiceProvider
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of instance cannot be statically discovered and the Type's properties can be trimmed.")]
        public ValidationContext(object instance) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of instance cannot be statically discovered and the Type's properties can be trimmed.")]
        public ValidationContext(object instance, System.Collections.Generic.IDictionary<object, object?>? items) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of instance cannot be statically discovered and the Type's properties can be trimmed.")]
        public ValidationContext(object instance, System.IServiceProvider? serviceProvider, System.Collections.Generic.IDictionary<object, object?>? items) { }
        public string DisplayName { get { throw null; } set { } }
        public System.Collections.Generic.IDictionary<object, object?> Items { get { throw null; } }
        public string? MemberName { get { throw null; } set { } }
        public object ObjectInstance { get { throw null; } }
        public System.Type ObjectType { get { throw null; } }
        public object? GetService(System.Type serviceType) { throw null; }
        public void InitializeServiceProvider(System.Func<System.Type, object?> serviceProvider) { }
    }
    public partial class ValidationException : System.Exception
    {
        public ValidationException() { }
        public ValidationException(System.ComponentModel.DataAnnotations.ValidationResult validationResult, System.ComponentModel.DataAnnotations.ValidationAttribute? validatingAttribute, object? value) { }
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected ValidationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ValidationException(string? message) { }
        public ValidationException(string? errorMessage, System.ComponentModel.DataAnnotations.ValidationAttribute? validatingAttribute, object? value) { }
        public ValidationException(string? message, System.Exception? innerException) { }
        public System.ComponentModel.DataAnnotations.ValidationAttribute? ValidationAttribute { get { throw null; } }
        public System.ComponentModel.DataAnnotations.ValidationResult ValidationResult { get { throw null; } }
        public object? Value { get { throw null; } }
    }
    public partial class ValidationResult
    {
        public static readonly System.ComponentModel.DataAnnotations.ValidationResult? Success;
        protected ValidationResult(System.ComponentModel.DataAnnotations.ValidationResult validationResult) { }
        public ValidationResult(string? errorMessage) { }
        public ValidationResult(string? errorMessage, System.Collections.Generic.IEnumerable<string>? memberNames) { }
        public string? ErrorMessage { get { throw null; } set { } }
        public System.Collections.Generic.IEnumerable<string> MemberNames { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public static partial class Validator
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of instance cannot be statically discovered and the Type's properties can be trimmed.")]
        public static bool TryValidateObject(object instance, System.ComponentModel.DataAnnotations.ValidationContext validationContext, System.Collections.Generic.ICollection<System.ComponentModel.DataAnnotations.ValidationResult>? validationResults) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of instance cannot be statically discovered and the Type's properties can be trimmed.")]
        public static bool TryValidateObject(object instance, System.ComponentModel.DataAnnotations.ValidationContext validationContext, System.Collections.Generic.ICollection<System.ComponentModel.DataAnnotations.ValidationResult>? validationResults, bool validateAllProperties) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of validationContext.ObjectType cannot be statically discovered.")]
        public static bool TryValidateProperty(object? value, System.ComponentModel.DataAnnotations.ValidationContext validationContext, System.Collections.Generic.ICollection<System.ComponentModel.DataAnnotations.ValidationResult>? validationResults) { throw null; }
        public static bool TryValidateValue(object? value, System.ComponentModel.DataAnnotations.ValidationContext validationContext, System.Collections.Generic.ICollection<System.ComponentModel.DataAnnotations.ValidationResult>? validationResults, System.Collections.Generic.IEnumerable<System.ComponentModel.DataAnnotations.ValidationAttribute> validationAttributes) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of instance cannot be statically discovered and the Type's properties can be trimmed.")]
        public static void ValidateObject(object instance, System.ComponentModel.DataAnnotations.ValidationContext validationContext) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of instance cannot be statically discovered and the Type's properties can be trimmed.")]
        public static void ValidateObject(object instance, System.ComponentModel.DataAnnotations.ValidationContext validationContext, bool validateAllProperties) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of validationContext.ObjectType cannot be statically discovered.")]
        public static void ValidateProperty(object? value, System.ComponentModel.DataAnnotations.ValidationContext validationContext) { }
        public static void ValidateValue(object? value, System.ComponentModel.DataAnnotations.ValidationContext validationContext, System.Collections.Generic.IEnumerable<System.ComponentModel.DataAnnotations.ValidationAttribute> validationAttributes) { }
    }
}
namespace System.ComponentModel.DataAnnotations.Schema
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple=false)]
    public partial class ColumnAttribute : System.Attribute
    {
        public ColumnAttribute() { }
        public ColumnAttribute(string name) { }
        public string? Name { get { throw null; } }
        public int Order { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.DisallowNullAttribute]
        public string? TypeName { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=false)]
    public partial class ComplexTypeAttribute : System.Attribute
    {
        public ComplexTypeAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple=false)]
    public partial class DatabaseGeneratedAttribute : System.Attribute
    {
        public DatabaseGeneratedAttribute(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption databaseGeneratedOption) { }
        public System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption DatabaseGeneratedOption { get { throw null; } }
    }
    public enum DatabaseGeneratedOption
    {
        None = 0,
        Identity = 1,
        Computed = 2,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple=false)]
    public partial class ForeignKeyAttribute : System.Attribute
    {
        public ForeignKeyAttribute(string name) { }
        public string Name { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple=false)]
    public partial class InversePropertyAttribute : System.Attribute
    {
        public InversePropertyAttribute(string property) { }
        public string Property { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple=false)]
    public partial class NotMappedAttribute : System.Attribute
    {
        public NotMappedAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=false)]
    public partial class TableAttribute : System.Attribute
    {
        public TableAttribute(string name) { }
        public string Name { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.DisallowNullAttribute]
        public string? Schema { get { throw null; } set { } }
    }
}
