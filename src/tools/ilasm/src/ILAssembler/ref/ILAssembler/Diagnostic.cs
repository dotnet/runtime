// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILAssembler
{
    public record Diagnostic(string Id, DiagnosticSeverity Severity, string Message, Location Location);

    public static class DiagnosticIds
    {
        public const string AbstractMethodNotInAbstractType = "ILA0021";
        public const string ArgumentNotFound = "ILA0018";
        public const string AssemblyNotFound = "ILA0014";
        public const string BaseOutsideClass = "ILA0004";
        public const string ByteArrayTooShort = "ILA0016";
        public const string DeprecatedCustomMarshaller = "ILA0025";
        public const string DeprecatedNativeType = "ILA0024";
        public const string DuplicateMethod = "ILA0030";
        public const string ExportedTypeNotFound = "ILA0015";
        public const string FileNotFound = "ILA0013";
        public const string GenericParameterIndexOutOfRange = "ILA0027";
        public const string GenericParameterNotFound = "ILA0011";
        public const string InvalidMetadataToken = "ILA0012";
        public const string InvalidPInvokeSignature = "ILA0022";
        public const string KeyFileError = "ILA0032";
        public const string LabelNotFound = "ILA0017";
        public const string LiteralOutOfRange = "ILA0001";
        public const string LocalNotFound = "ILA0019";
        public const string MethodTypeParameterOutsideMethod = "ILA0009";
        public const string MissingExportedTypeImplementation = "ILA0031";
        public const string MissingInstanceCallConv = "ILA0023";
        public const string ModuleNotFound = "ILA0007";
        public const string NesterOutsideNestedClass = "ILA0006";
        public const string NoBaseType = "ILA0005";
        public const string ParameterIndexOutOfRange = "ILA0029";
        public const string PseudoCustomAttributeInvalidBlob = "ILA0036";
        public const string PseudoCustomAttributeInvalidGuid = "ILA0037";
        public const string PseudoCustomAttributeInvalidTarget = "ILA0034";
        public const string PseudoCustomAttributeInvalidValue = "ILA0035";
        public const string PseudoCustomAttributeRepeatedArgument = "ILA0039";
        public const string PseudoCustomAttributeUnknownArgument = "ILA0038";
        public const string ThisOutsideClass = "ILA0003";
        public const string TypeNotFound = "ILA0008";
        public const string TypeParameterOutsideType = "ILA0010";
        public const string TypedefNotFound = "ILA0020";
        public const string UnknownGenericParameter = "ILA0028";
        public const string UnsealedValueType = "ILA0002";
        public const string UnsupportedSecurityDeclaration = "ILA0026";
    }

    public enum DiagnosticSeverity
    {
        Error,
        Warning,
        Info,
        Hidden
    }
}
