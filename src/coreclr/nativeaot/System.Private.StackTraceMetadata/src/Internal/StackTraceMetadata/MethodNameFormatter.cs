// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text;

using Internal.Metadata.NativeFormat;

namespace Internal.StackTraceMetadata
{
    internal class MethodNameFormatter
    {
        [Flags]
        private enum Flags
        {
            None = 0,
            NamespaceQualify = 1,
            ReflectionFormat = 2,
        }

        /// <summary>
        /// Metadata reader used for the purpose of method name formatting.
        /// </summary>
        private readonly MetadataReader _metadataReader;

        /// <summary>
        /// String builder used to construct formatted method name.
        /// </summary>
        private readonly StringBuilder _outputBuilder;

        /// <summary>
        /// Represents the instatiation type context.
        /// </summary>
        private readonly SigTypeContext _typeContext;

        /// <summary>
        /// Initialize the reader used for method name formatting.
        /// </summary>
        private MethodNameFormatter(MetadataReader metadataReader, SigTypeContext typeContext)
        {
            _metadataReader = metadataReader;
            _outputBuilder = new StringBuilder();
            _typeContext = typeContext;
        }

        public static string FormatReflectionNotationTypeName(MetadataReader metadataReader, Handle type)
        {
            MethodNameFormatter formatter = new MethodNameFormatter(metadataReader, default);
            formatter.EmitTypeName(type, Flags.NamespaceQualify | Flags.ReflectionFormat);
            return formatter._outputBuilder.ToString();
        }

        public static (string, string, string, string) FormatMethodName(MetadataReader metadataReader, Handle owningType, ConstantStringValueHandle name, MethodSignatureHandle signature, ConstantStringArrayHandle genericArguments)
        {
            MethodNameFormatter formatter = new MethodNameFormatter(metadataReader, SigTypeContext.FromMethod(metadataReader, owningType, genericArguments));
            formatter.EmitTypeName(owningType, Flags.NamespaceQualify);

            string owningTypeString = formatter._outputBuilder.ToString();
            formatter._outputBuilder.Clear();

            string methodName = metadataReader.GetString(name);
            string genericArgs = null;
            if (!genericArguments.IsNil)
            {
                var args = metadataReader.GetConstantStringArray(genericArguments);
                bool first = true;
                foreach (ConstantStringValueHandle handle in args.Value)
                {
                    if (!first)
                        formatter._outputBuilder.Append(',');
                    else
                        first = false;
                    formatter.EmitString(handle);
                }
                genericArgs = formatter._outputBuilder.ToString();
                formatter._outputBuilder.Clear();
            }

            formatter.EmitTypeVector(metadataReader.GetMethodSignature(signature).Parameters);
            string signatureString = formatter._outputBuilder.ToString();

            return (owningTypeString, genericArgs, methodName, signatureString);
        }

        public static (string, string, string, string) FormatMethodName(MetadataReader metadataReader, TypeDefinitionHandle enclosingTypeHandle, MethodHandle methodHandle)
        {
            MethodNameFormatter formatter = new MethodNameFormatter(metadataReader, SigTypeContext.FromMethod(metadataReader, enclosingTypeHandle, methodHandle));

            Method method = metadataReader.GetMethod(methodHandle);
            formatter.EmitTypeName(enclosingTypeHandle, Flags.NamespaceQualify);

            string owningTypeString = formatter._outputBuilder.ToString();
            formatter._outputBuilder.Clear();

            string methodName = metadataReader.GetString(method.Name);
            string genericArgs = null;
            if (method.GenericParameters.Count > 0)
            {
                bool first = true;
                foreach (GenericParameterHandle handle in method.GenericParameters)
                {
                    if (first)
                        first = false;
                    else
                        formatter._outputBuilder.Append(',');
                    formatter.EmitTypeName(handle, Flags.None);
                }
                genericArgs = formatter._outputBuilder.ToString();
                formatter._outputBuilder.Clear();
            }

            formatter.EmitMethodParameters(methodHandle);
            string signatureString = formatter._outputBuilder.ToString();

            return (owningTypeString, genericArgs, methodName, signatureString);
        }

        /// <summary>
        /// Emit parenthesized method argument type list with parameter names.
        /// </summary>
        /// <param name="methodHandle">Method handle to use for parameter formatting</param>
        private void EmitMethodParameters(MethodHandle methodHandle)
        {
            bool TryGetNextParameter(ref ParameterHandleCollection.Enumerator enumerator, out Parameter parameter)
            {
                bool hasNext = enumerator.MoveNext();
                parameter = hasNext ? enumerator.Current.GetParameter(_metadataReader) : default;
                return hasNext;
            }

            Method method = methodHandle.GetMethod(_metadataReader);
            TypeSignatureCollection typeVector = method.Signature.GetMethodSignature(_metadataReader).Parameters;
            ParameterHandleCollection.Enumerator parameters = method.Parameters.GetEnumerator();

            bool hasParameter = TryGetNextParameter(ref parameters, out Parameter parameter);
            if (hasParameter && parameter.Sequence == 0)
            {
                hasParameter = TryGetNextParameter(ref parameters, out parameter);
            }

            uint typeIndex = 0;
            foreach (TypeSignature type in typeVector)
            {
                if (typeIndex != 0)
                {
                    _outputBuilder.Append(", ");
                }

                EmitTypeName(type, Flags.None);

                if (++typeIndex == parameter.Sequence && hasParameter)
                {
                    string name = parameter.Name.GetConstantStringValue(_metadataReader).Value;
                    hasParameter = TryGetNextParameter(ref parameters, out parameter);

                    if (!string.IsNullOrEmpty(name))
                    {
                        _outputBuilder.Append(' ');
                        _outputBuilder.Append(name);
                    }
                }
            }
        }

        /// <summary>
        /// Emit comma-separated list of type names into the output string builder.
        /// </summary>
        /// <param name="typeVector">Enumeration of type handles to output</param>
        private void EmitTypeVector(TypeSignatureCollection typeVector)
        {
            bool first = true;
            foreach (TypeSignature handle in typeVector)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    _outputBuilder.Append(", ");
                }
                EmitTypeName(handle, Flags.None);
            }
        }

        /// <summary>
        /// Emit the name of a given type to the output string builder.
        /// </summary>
        private void EmitTypeName(Handle typeHandle, Flags flags)
        {
            switch (typeHandle.HandleType)
            {
                case HandleType.TypeReference:
                    EmitTypeReferenceName(typeHandle.ToTypeReferenceHandle(_metadataReader), flags);
                    break;

                case HandleType.TypeSpecification:
                    EmitTypeSpecificationName(typeHandle.ToTypeSpecificationHandle(_metadataReader), flags);
                    break;

                case HandleType.TypeDefinition:
                    EmitTypeDefinitionName(typeHandle.ToTypeDefinitionHandle(_metadataReader), flags);
                    break;

                case HandleType.GenericParameter:
                    EmitString(typeHandle.ToGenericParameterHandle(_metadataReader).GetGenericParameter(_metadataReader).Name);
                    break;

                // This is not an actual type, but we don't always bother representing generic arguments on generic methods as types
                case HandleType.ConstantStringValue:
                    EmitString(typeHandle.ToConstantStringValueHandle(_metadataReader));
                    break;

                default:
                    Debug.Fail($"Type handle {typeHandle.HandleType} was not handled");
                    _outputBuilder.Append("???");
                    break;
            }
        }

        private void EmitTypeName(TypeSignature signature, Flags flags)
        {
            if (signature.IsHandle)
            {
                EmitTypeName(signature.Handle, flags);
                return;
            }

            SignatureReader reader = signature.SkipCustomModifiers().GetReader();
            SignatureTypeCode code = (SignatureTypeCode)reader.ReadByte();
            switch (code)
            {
                case SignatureTypeCode.Class:
                case SignatureTypeCode.ValueType:
                    EmitTypeName(reader.ReadTypeHandle(), flags);
                    return;
                case SignatureTypeCode.GenericTypeInstance:
                    // Stack traces display the generic definition, not its arguments.
                    EmitTypeName(reader.ReadType(), flags);
                    return;
                case SignatureTypeCode.GenericTypeParameter:
                case SignatureTypeCode.GenericMethodParameter:
                    _typeContext.EmitVariable(this, reader.ReadCompressedInteger(), code == SignatureTypeCode.GenericMethodParameter, flags);
                    return;
                case SignatureTypeCode.SZArray:
                    EmitTypeName(reader.ReadType(), flags);
                    _outputBuilder.Append("[]");
                    return;
                case SignatureTypeCode.Array:
                    EmitTypeName(reader.ReadType(), flags);
                    int rank = reader.ReadCompressedInteger();
                    _outputBuilder.Append('[');
                    if (rank > 1)
                        _outputBuilder.Append(',', rank - 1);
                    else
                        _outputBuilder.Append('*');
                    _outputBuilder.Append(']');
                    return;
                case SignatureTypeCode.Pointer:
                case SignatureTypeCode.ByReference:
                    EmitTypeName(reader.ReadType(), Flags.None);
                    _outputBuilder.Append(code == SignatureTypeCode.Pointer ? '*' : '&');
                    return;
                case SignatureTypeCode.FunctionPointer:
                    // Function pointers intentionally have no textual stack-trace representation.
                    return;
            }

            if ((flags & Flags.NamespaceQualify) != 0)
                _outputBuilder.Append("System.");
            _outputBuilder.Append(code switch
            {
                SignatureTypeCode.Void => "Void",
                SignatureTypeCode.Boolean => "Boolean",
                SignatureTypeCode.Char => "Char",
                SignatureTypeCode.SByte => "SByte",
                SignatureTypeCode.Byte => "Byte",
                SignatureTypeCode.Int16 => "Int16",
                SignatureTypeCode.UInt16 => "UInt16",
                SignatureTypeCode.Int32 => "Int32",
                SignatureTypeCode.UInt32 => "UInt32",
                SignatureTypeCode.Int64 => "Int64",
                SignatureTypeCode.UInt64 => "UInt64",
                SignatureTypeCode.Single => "Single",
                SignatureTypeCode.Double => "Double",
                SignatureTypeCode.String => "String",
                SignatureTypeCode.TypedReference => "TypedReference",
                SignatureTypeCode.IntPtr => "IntPtr",
                SignatureTypeCode.UIntPtr => "UIntPtr",
                SignatureTypeCode.Object => "Object",
                _ => "???",
            });
        }

        /// <summary>
        /// Emit namespace reference.
        /// </summary>
        /// <param name="namespaceRefHandle">Namespace reference handle</param>
        private void EmitNamespaceReferenceName(NamespaceReferenceHandle namespaceRefHandle)
        {
            NamespaceReference namespaceRef = _metadataReader.GetNamespaceReference(namespaceRefHandle);
            EmitString(namespaceRef.Name);
        }

        private void EmitNamespaceDefinitionName(NamespaceDefinitionHandle namespaceDefHandle)
        {
            NamespaceDefinition namespaceDef = _metadataReader.GetNamespaceDefinition(namespaceDefHandle);
            EmitString(namespaceDef.Name);
        }

        /// <summary>
        /// Emit type reference.
        /// </summary>
        private void EmitTypeReferenceName(TypeReferenceHandle typeRefHandle, Flags flags)
        {
            TypeReference typeRef = _metadataReader.GetTypeReference(typeRefHandle);
            if (!typeRef.NamespaceOrEnclosingType.IsNil)
            {
                if (typeRef.NamespaceOrEnclosingType.HandleType != HandleType.NamespaceReference)
                {
                    // Nested type
                    EmitTypeName(typeRef.NamespaceOrEnclosingType, flags);
                    if ((flags & Flags.ReflectionFormat) != 0)
                        _outputBuilder.Append('+');
                    else
                        _outputBuilder.Append('.');
                }
                else if ((flags & Flags.NamespaceQualify) != 0)
                {
                    int charsWritten = _outputBuilder.Length;
                    EmitNamespaceReferenceName(typeRef.NamespaceOrEnclosingType.ToNamespaceReferenceHandle(_metadataReader));
                    if (_outputBuilder.Length - charsWritten > 0)
                        _outputBuilder.Append('.');
                }
            }
            EmitString(typeRef.TypeName);
        }

        private void EmitTypeDefinitionName(TypeDefinitionHandle typeDefHandle, Flags flags)
        {
            TypeDefinition typeDef = _metadataReader.GetTypeDefinition(typeDefHandle);
            Handle namespaceOrEnclosingType = typeDef.NamespaceOrEnclosingType;
            if (namespaceOrEnclosingType.HandleType == HandleType.TypeDefinition)
            {
                // Nested type
                EmitTypeName(namespaceOrEnclosingType, flags);
                if ((flags & Flags.ReflectionFormat) != 0)
                    _outputBuilder.Append('+');
                else
                    _outputBuilder.Append('.');
            }
            else if (namespaceOrEnclosingType.HandleType == HandleType.NamespaceDefinition && (flags & Flags.NamespaceQualify) != 0)
            {
                int charsWritten = _outputBuilder.Length;
                EmitNamespaceDefinitionName(namespaceOrEnclosingType.ToNamespaceDefinitionHandle(_metadataReader));
                if (_outputBuilder.Length - charsWritten > 0)
                    _outputBuilder.Append('.');
            }
            EmitString(typeDef.Name);
        }

        /// <summary>
        /// Emit an arbitrary type specification.
        /// </summary>
        private void EmitTypeSpecificationName(TypeSpecificationHandle typeSpecHandle, Flags flags)
        {
            TypeSpecification typeSpec = _metadataReader.GetTypeSpecification(typeSpecHandle);
            EmitTypeName(typeSpec.Signature, flags);
        }

        /// <summary>
        /// Emit a string (represented by a serialized ConstantStringValue) to the output string builder.
        /// </summary>
        /// <param name="stringHandle">Constant string value token (offset within stack trace native metadata)</param>
        private void EmitString(ConstantStringValueHandle stringHandle)
        {
            _outputBuilder.Append(_metadataReader.GetConstantStringValue(stringHandle).Value);
        }

        private struct SigTypeContext
        {
            private readonly object _typeContext;
            private readonly object _methodContext;

            public SigTypeContext(object typeContext, object methodContext)
            {
                _typeContext = typeContext;
                _methodContext = methodContext;
            }

            public void EmitVariable(MethodNameFormatter formatter, int index, bool method, Flags flags)
            {
                object context = method ? _methodContext : _typeContext;
                if (context is TypeSignatureCollection types)
                {
                    foreach (TypeSignature type in types)
                        if (index-- == 0)
                        {
                            formatter.EmitTypeName(type, flags);
                            return;
                        }
                }
                else if (context is GenericParameterHandleCollection parameters)
                {
                    foreach (GenericParameterHandle parameter in parameters)
                        if (index-- == 0)
                        {
                            formatter.EmitTypeName(parameter, flags);
                            return;
                        }
                }
                else if (context is ConstantStringValueHandleCollection strings)
                {
                    foreach (ConstantStringValueHandle handle in strings)
                        if (index-- == 0)
                        {
                            formatter.EmitString(handle);
                            return;
                        }
                }

                Debug.Fail("Missing generic parameter name.");
            }

            private static object GetTypeContext(MetadataReader metadataReader, Handle handle)
            {
                switch (handle.HandleType)
                {
                    case HandleType.MemberReference:
                        MemberReference memberRef = handle.ToMemberReferenceHandle(metadataReader).GetMemberReference(metadataReader);
                        return GetTypeContext(metadataReader, memberRef.Parent);

                    case HandleType.QualifiedMethod:
                        QualifiedMethod qualifiedMethod = handle.ToQualifiedMethodHandle(metadataReader).GetQualifiedMethod(metadataReader);
                        return GetTypeContext(metadataReader, qualifiedMethod.EnclosingType);

                    case HandleType.TypeDefinition:
                        TypeDefinition typeDef = handle.ToTypeDefinitionHandle(metadataReader).GetTypeDefinition(metadataReader);
                        return typeDef.GenericParameters;

                    case HandleType.TypeReference:
                        return null;

                    case HandleType.TypeSpecification:
                        TypeSpecification typeSpec = handle.ToTypeSpecificationHandle(metadataReader).GetTypeSpecification(metadataReader);
                        return typeSpec.Signature.GetGenericArguments();

                    default:
                        Debug.Assert(false);
                        return null;
                }
            }

            public static SigTypeContext FromMethod(MetadataReader metadataReader, Handle enclosingTypeHandle, ConstantStringArrayHandle methodInst)
            {
                object methodContext = null;
                if (!methodInst.IsNil)
                    methodContext = methodInst.GetConstantStringArray(metadataReader).Value;
                return new SigTypeContext(GetTypeContext(metadataReader, enclosingTypeHandle), methodContext);
            }

            public static SigTypeContext FromMethod(MetadataReader metadataReader, TypeDefinitionHandle enclosingTypeHandle, MethodHandle methodHandle)
            {
                Method method = metadataReader.GetMethod(methodHandle);
                return new SigTypeContext(GetTypeContext(metadataReader, enclosingTypeHandle), method.GenericParameters);
            }
        }
    }
}
