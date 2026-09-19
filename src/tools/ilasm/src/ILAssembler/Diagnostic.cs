// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILAssembler;

public enum DiagnosticSeverity
{
    Error,
    Warning,
    Info,
    Hidden
}

public record Diagnostic(string Id, DiagnosticSeverity Severity, string Message, Location Location);

/// <summary>
/// Well-known diagnostic IDs for IL assembler diagnostics.
/// </summary>
public static class DiagnosticIds
{
    public const string LiteralOutOfRange = "ILA0001";
    public const string UnsealedValueType = "ILA0002";
    public const string ThisOutsideClass = "ILA0003";
    public const string BaseOutsideClass = "ILA0004";
    public const string NoBaseType = "ILA0005";
    public const string NesterOutsideNestedClass = "ILA0006";
    public const string ModuleNotFound = "ILA0007";
    public const string TypeNotFound = "ILA0008";
    public const string MethodTypeParameterOutsideMethod = "ILA0009";
    public const string TypeParameterOutsideType = "ILA0010";
    public const string GenericParameterNotFound = "ILA0011";
    public const string InvalidMetadataToken = "ILA0012";
    public const string FileNotFound = "ILA0013";
    public const string AssemblyNotFound = "ILA0014";
    public const string ExportedTypeNotFound = "ILA0015";
    public const string ByteArrayTooShort = "ILA0016";
    public const string LabelNotFound = "ILA0017";
    public const string ArgumentNotFound = "ILA0018";
    public const string LocalNotFound = "ILA0019";
    public const string TypedefNotFound = "ILA0020";
    public const string AbstractMethodNotInAbstractType = "ILA0021";
    public const string InvalidPInvokeSignature = "ILA0022";
    public const string MissingInstanceCallConv = "ILA0023";
    public const string DeprecatedNativeType = "ILA0024";
    public const string DeprecatedCustomMarshaller = "ILA0025";
    public const string UnsupportedSecurityDeclaration = "ILA0026";
    public const string GenericParameterIndexOutOfRange = "ILA0027";
    public const string UnknownGenericParameter = "ILA0028";
    public const string ParameterIndexOutOfRange = "ILA0029";
    public const string DuplicateMethod = "ILA0030";
    public const string MissingExportedTypeImplementation = "ILA0031";
    public const string KeyFileError = "ILA0032";
    public const string TooManyGenericParameters = "ILA0033";
    public const string UnsupportedTlsData = "ILA0034";
    public const string GenericParameterConstraintOwnerOutOfRange = "ILA0035";
    public const string InvalidExportOrdinal = "ILA0036";
    public const string ExportOrdinalRangeTooLarge = "ILA0037";
    public const string InvalidVTableSlotCount = "ILA0038";
    public const string InvalidVTableExport = "ILA0039";
    public const string InsufficientVTableData = "ILA0040";
    public const string UnsupportedNativeExportMachine = "ILA0041";
    public const string InvalidVTableWidth = "ILA0042";
    public const string InvalidVTableEntry = "ILA0043";
    public const string DuplicateExportOrdinal = "ILA0044";
}

internal static class DiagnosticMessageTemplates
{
    public const string LiteralOutOfRange = "The value '{0}' is out of range";
    public const string UnsealedValueType = "The value type '{0}' is unsealed; implicitly sealed.";
    public const string ThisOutsideClass = "'.this' cannot be used outside of a class definition";
    public const string BaseOutsideClass = "'.base' cannot be used outside of a class definition";
    public const string NoBaseType = "Current type does not have a base type";
    public const string NesterOutsideNestedClass = "'.nester' cannot be used outside of a nested class definition";
    public const string ModuleNotFound = "Module '{0}' not found";
    public const string TypeNotFound = "Type '{0}' not found";
    public const string MethodTypeParameterOutsideMethod = "Method type parameter '!!{0}' cannot be used outside of a method definition";
    public const string TypeParameterOutsideType = "Type parameter '!{0}' cannot be used outside of a type definition";
    public const string GenericParameterNotFound = "Generic parameter '{0}' not found";
    public const string InvalidMetadataToken = "Invalid or unresolved metadata token";
    public const string FileNotFound = "File '{0}' not found";
    public const string AssemblyNotFound = "Assembly '{0}' not found";
    public const string ExportedTypeNotFound = "Exported type '{0}' not found";
    public const string ByteArrayTooShort = "Byte array is too short for the specified data type";
    public const string LabelNotFound = "Label '{0}' not found";
    public const string ArgumentNotFound = "Argument '{0}' not found";
    public const string LocalNotFound = "Local variable '{0}' not found";
    public const string TypedefNotFound = "Typedef '{0}' not found";
    public const string AbstractMethodNotInAbstractType = "Abstract method '{0}' cannot be declared in a non-abstract type";
    public const string InvalidPInvokeSignature = "Invalid P/Invoke signature: module name is required";
    public const string MissingInstanceCallConv = "Instance call convention required for method reference";
    public const string DeprecatedNativeType = "Native type '{0}' is deprecated";
    public const string DeprecatedCustomMarshaller = "The 4-string form of custom marshaller is deprecated";
    public const string UnsupportedSecurityDeclaration = "Individual SecurityAttribute permissions are not supported; use PermissionSet instead";
    public const string GenericParameterIndexOutOfRange = "Generic parameter index {0} is out of range";
    public const string UnknownGenericParameter = "Unknown generic parameter '{0}'";
    public const string ParameterIndexOutOfRange = "Parameter index {0} is out of range";
    public const string DuplicateMethod = "Duplicate method definition";
    public const string MissingExportedTypeImplementation = "Undefined implementation in ExportedType '{0}' -- ExportedType not emitted";
    public const string TooManyGenericParameters = "Generic parameter count {0} exceeds the maximum of {1}";
    public const string UnsupportedTlsData = "TLS RVA data declarations are not supported";
    public const string GenericParameterConstraintOwnerOutOfRange = "Generic parameter constraint owner index {0} exceeds the maximum encodable generic parameter index of {1}";
    public const string InvalidExportOrdinal = "Export ordinal {0} must be positive";
    public const string ExportOrdinalRangeTooLarge = "Export ordinal {0} is too far from base ordinal {1}; the export address table index must not exceed {2}";
    public const string InvalidVTableSlotCount = "VTable fixup slot count {0} must be between 0 and {1}";
    public const string InvalidVTableExport = "Export '{0}' does not have a valid VTable entry association";
    public const string InsufficientVTableData = "Data label '{0}' has {1} bytes available, but its VTable fixup requires {2} bytes";
    public const string UnsupportedNativeExportMachine = "Native exports are not supported for target machine '{0}'";
    public const string InvalidVTableWidth = "VTable fixup width flags 0x{0:X} do not match target machine '{1}'; expected exactly '{2}'";
    public const string InvalidVTableEntry = "Method '{0}' references invalid VTable entry {1}";
    public const string InvalidVTableSlot = "Method '{0}' references invalid VTable slot {1}; VTable entry {2} contains {3} slots";
    public const string DuplicateExportOrdinal = "Export '{0}' uses ordinal {1}, which is already assigned to a different VTable target";
}
