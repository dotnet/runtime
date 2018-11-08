// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.TypeLoading.Ecma
{
    internal static class MetadataExtensions
    {
        // So code can be written in a way that flows from left to right...
        public static AssemblyFile GetAssemblyFile(this AssemblyFileHandle handle, MetadataReader reader) => reader.GetAssemblyFile(handle);
        public static AssemblyReference GetAssemblyReference(this AssemblyReferenceHandle handle, MetadataReader reader) => reader.GetAssemblyReference(handle);
        public static byte[] GetBlobBytes(this BlobHandle handle, MetadataReader reader) => reader.GetBlobBytes(handle);
        public static ImmutableArray<byte> GetBlobContent(this BlobHandle handle, MetadataReader reader) => reader.GetBlobContent(handle);
        public static BlobReader GetBlobReader(this BlobHandle handle, MetadataReader reader) => reader.GetBlobReader(handle);
        public static BlobReader GetBlobReader(this StringHandle handle, MetadataReader reader) => reader.GetBlobReader(handle);
        public static Constant GetConstant(this ConstantHandle handle, MetadataReader reader) => reader.GetConstant(handle);
        public static CustomAttribute GetCustomAttribute(this CustomAttributeHandle handle, MetadataReader reader) => reader.GetCustomAttribute(handle);
        public static CustomAttributeHandleCollection GetCustomAttributes(this EntityHandle handle, MetadataReader reader) => reader.GetCustomAttributes(handle);
        public static CustomDebugInformation GetCustomDebugInformation(this CustomDebugInformationHandle handle, MetadataReader reader) => reader.GetCustomDebugInformation(handle);
        public static CustomDebugInformationHandleCollection GetCustomDebugInformation(this EntityHandle handle, MetadataReader reader) => reader.GetCustomDebugInformation(handle);
        public static DeclarativeSecurityAttribute GetDeclarativeSecurityAttribute(this DeclarativeSecurityAttributeHandle handle, MetadataReader reader) => reader.GetDeclarativeSecurityAttribute(handle);
        public static Document GetDocument(this DocumentHandle handle, MetadataReader reader) => reader.GetDocument(handle);
        public static EventDefinition GetEventDefinition(this EventDefinitionHandle handle, MetadataReader reader) => reader.GetEventDefinition(handle);
        public static ExportedType GetExportedType(this ExportedTypeHandle handle, MetadataReader reader) => reader.GetExportedType(handle);
        public static FieldDefinition GetFieldDefinition(this FieldDefinitionHandle handle, MetadataReader reader) => reader.GetFieldDefinition(handle);
        public static GenericParameter GetGenericParameter(this GenericParameterHandle handle, MetadataReader reader) => reader.GetGenericParameter(handle);
        public static GenericParameterConstraint GetGenericParameterConstraint(this GenericParameterConstraintHandle handle, MetadataReader reader) => reader.GetGenericParameterConstraint(handle);
        public static Guid GetGuid(this GuidHandle handle, MetadataReader reader) => reader.GetGuid(handle);
        public static ImportScope GetImportScope(this ImportScopeHandle handle, MetadataReader reader) => reader.GetImportScope(handle);
        public static InterfaceImplementation GetInterfaceImplementation(this InterfaceImplementationHandle handle, MetadataReader reader) => reader.GetInterfaceImplementation(handle);
        public static LocalConstant GetLocalConstant(this LocalConstantHandle handle, MetadataReader reader) => reader.GetLocalConstant(handle);
        public static LocalScope GetLocalScope(this LocalScopeHandle handle, MetadataReader reader) => reader.GetLocalScope(handle);
        public static LocalScopeHandleCollection GetLocalScopes(this MethodDefinitionHandle handle, MetadataReader reader) => reader.GetLocalScopes(handle);
        public static LocalScopeHandleCollection GetLocalScopes(this MethodDebugInformationHandle handle, MetadataReader reader) => reader.GetLocalScopes(handle);
        public static LocalVariable GetLocalVariable(this LocalVariableHandle handle, MetadataReader reader) => reader.GetLocalVariable(handle);
        public static ManifestResource GetManifestResource(this ManifestResourceHandle handle, MetadataReader reader) => reader.GetManifestResource(handle);
        public static MemberReference GetMemberReference(this MemberReferenceHandle handle, MetadataReader reader) => reader.GetMemberReference(handle);
        public static MethodDebugInformation GetMethodDebugInformation(this MethodDebugInformationHandle handle, MetadataReader reader) => reader.GetMethodDebugInformation(handle);
        public static MethodDebugInformation GetMethodDebugInformation(this MethodDefinitionHandle handle, MetadataReader reader) => reader.GetMethodDebugInformation(handle);
        public static MethodDefinition GetMethodDefinition(this MethodDefinitionHandle handle, MetadataReader reader) => reader.GetMethodDefinition(handle);
        public static MethodImplementation GetMethodImplementation(this MethodImplementationHandle handle, MetadataReader reader) => reader.GetMethodImplementation(handle);
        public static MethodSpecification GetMethodSpecification(this MethodSpecificationHandle handle, MetadataReader reader) => reader.GetMethodSpecification(handle);
        public static ModuleReference GetModuleReference(this ModuleReferenceHandle handle, MetadataReader reader) => reader.GetModuleReference(handle);
        public static NamespaceDefinition GetNamespaceDefinition(this NamespaceDefinitionHandle handle, MetadataReader reader) => reader.GetNamespaceDefinition(handle);
        public static Parameter GetParameter(this ParameterHandle handle, MetadataReader reader) => reader.GetParameter(handle);
        public static PropertyDefinition GetPropertyDefinition(this PropertyDefinitionHandle handle, MetadataReader reader) => reader.GetPropertyDefinition(handle);
        public static StandaloneSignature GetStandaloneSignature(this StandaloneSignatureHandle handle, MetadataReader reader) => reader.GetStandaloneSignature(handle);
        public static string GetString(this StringHandle handle, MetadataReader reader) => reader.GetString(handle);
        public static string GetString(this NamespaceDefinitionHandle handle, MetadataReader reader) => reader.GetString(handle);
        public static string GetString(this DocumentNameBlobHandle handle, MetadataReader reader) => reader.GetString(handle);
        public static TypeDefinition GetTypeDefinition(this TypeDefinitionHandle handle, MetadataReader reader) => reader.GetTypeDefinition(handle);
        public static TypeReference GetTypeReference(this TypeReferenceHandle handle, MetadataReader reader) => reader.GetTypeReference(handle);
        public static TypeSpecification GetTypeSpecification(this TypeSpecificationHandle handle, MetadataReader reader) => reader.GetTypeSpecification(handle);
        public static string GetUserString(this UserStringHandle handle, MetadataReader reader) => reader.GetUserString(handle);

        public static int GetToken(this Handle handle) => MetadataTokens.GetToken(handle);
        public static int GetToken(this EntityHandle handle) => MetadataTokens.GetToken(handle);
        public static int GetToken(this TypeDefinitionHandle handle) => MetadataTokens.GetToken(handle);
        public static int GetToken(this TypeReferenceHandle handle) => MetadataTokens.GetToken(handle);
        public static int GetToken(this TypeSpecificationHandle handle) => MetadataTokens.GetToken(handle);
        public static int GetToken(this GenericParameterHandle handle) => MetadataTokens.GetToken(handle);
        public static int GetToken(this GenericParameterConstraintHandle handle) => MetadataTokens.GetToken(handle);
        public static int GetToken(this FieldDefinitionHandle handle) => MetadataTokens.GetToken(handle);
        public static int GetToken(this EventDefinitionHandle handle) => MetadataTokens.GetToken(handle);
        public static int GetToken(this MethodDefinitionHandle handle) => MetadataTokens.GetToken(handle);
        public static int GetToken(this PropertyDefinitionHandle handle) => MetadataTokens.GetToken(handle);
        public static int GetToken(this ParameterHandle handle) => MetadataTokens.GetToken(handle);
        public static int GetToken(this StandaloneSignatureHandle handle) => MetadataTokens.GetToken(handle);
        public static int GetToken(this AssemblyFileHandle handle) => MetadataTokens.GetToken(handle);

        public static string GetStringOrNull(this StringHandle handle, MetadataReader reader) => handle.IsNil ? null : reader.GetString(handle);

        public static bool Equals(this StringHandle handle, string value, MetadataReader reader) => reader.StringComparer.Equals(handle, value, ignoreCase: false);

        // 
        // utf8.Length does *not* include NUL terminator.
        //
        public static unsafe bool Equals(this StringHandle handle, ReadOnlySpan<byte> utf8, MetadataReader reader)
        {
            //TODO: Perf - GetBlobReader() scans the string handle for a NUL terminator to compute the length making it an O(N)
            // operation. It might be worth memoizing the pointer/length combo per TypeDefToken and ExportedTypeToken. But even better
            // would be to get UTF8 Equals overloads added to MetadataStringComparer.
            BlobReader br = handle.GetBlobReader(reader);
            ReadOnlySpan<byte> actual = new ReadOnlySpan<byte>(br.CurrentPointer, br.Length);
            return utf8.SequenceEqual(actual);
        }

        public static Handle ToHandle(this int token) => MetadataTokens.Handle(token);
        public static TypeDefinitionHandle ToTypeDefinitionHandle(this int token) => MetadataTokens.TypeDefinitionHandle(token);
        public static TypeReferenceHandle ToTypeReferenceHandle(this int token) => MetadataTokens.TypeReferenceHandle(token);
        public static TypeSpecificationHandle ToTypeSpecificationHandle(this int token) => MetadataTokens.TypeSpecificationHandle(token);
    }
}
