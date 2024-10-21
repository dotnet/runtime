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

        public static string FormatMethodName(MetadataReader metadataReader, Handle owningType, ConstantStringValueHandle name, MethodSignatureHandle signature, ConstantStringArrayHandle genericArguments)
        {
            MethodNameFormatter formatter = new MethodNameFormatter(metadataReader, SigTypeContext.FromMethod(metadataReader, owningType, genericArguments));
            formatter.EmitTypeName(owningType, Flags.NamespaceQualify);
            formatter._outputBuilder.Append('.');
            formatter.EmitString(name);

            if (!genericArguments.IsNil)
            {
                var args = metadataReader.GetConstantStringArray(genericArguments);
                bool first = true;
                foreach (Handle handle in args.Value)
                {
                    if (first)
                    {
                        first = false;
                        formatter._outputBuilder.Append('[');
                    }
                    else
                    {
                        formatter._outputBuilder.Append(',');
                    }
                    formatter.EmitString(handle.ToConstantStringValueHandle(metadataReader));
                }
                if (!first)
                {
                    formatter._outputBuilder.Append(']');
                }
            }

            formatter.EmitMethodParameters(metadataReader.GetMethodSignature(signature));

            return formatter._outputBuilder.ToString();
        }

        public static string FormatMethodName(MetadataReader metadataReader, TypeDefinitionHandle enclosingTypeHandle, MethodHandle methodHandle)
        {
            MethodNameFormatter formatter = new MethodNameFormatter(metadataReader, SigTypeContext.FromMethod(metadataReader, enclosingTypeHandle, methodHandle));

            Method method = metadataReader.GetMethod(methodHandle);
            formatter.EmitTypeName(enclosingTypeHandle, Flags.NamespaceQualify);
            formatter._outputBuilder.Append('.');
            formatter.EmitString(method.Name);

            bool first = true;
            foreach (GenericParameterHandle handle in method.GenericParameters)
            {
                if (first)
                {
                    first = false;
                    formatter._outputBuilder.Append('[');
                }
                else
                {
                    formatter._outputBuilder.Append(',');
                }
                formatter.EmitTypeName(handle, Flags.None);
            }
            if (!first)
            {
                formatter._outputBuilder.Append(']');
            }

            formatter.EmitMethodParameters(methodHandle);

            return formatter._outputBuilder.ToString();
        }

        /// <summary>
        /// Emit parenthesized method argument type list.
        /// </summary>
        /// <param name="methodSignature">Method signature to use for parameter formatting</param>
        private void EmitMethodParameters(MethodSignature methodSignature)
        {
            _outputBuilder.Append('(');
            EmitTypeVector(methodSignature.Parameters);
            _outputBuilder.Append(')');
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
            HandleCollection typeVector = method.Signature.GetMethodSignature(_metadataReader).Parameters;
            ParameterHandleCollection.Enumerator parameters = method.Parameters.GetEnumerator();

            bool hasParameter = TryGetNextParameter(ref parameters, out Parameter parameter);
            if (hasParameter && parameter.Sequence == 0)
            {
                hasParameter = TryGetNextParameter(ref parameters, out parameter);
            }

            _outputBuilder.Append('(');

            uint typeIndex = 0;
            foreach (Handle type in typeVector)
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

            _outputBuilder.Append(')');
        }

        /// <summary>
        /// Emit comma-separated list of type names into the output string builder.
        /// </summary>
        /// <param name="typeVector">Enumeration of type handles to output</param>
        private void EmitTypeVector(HandleCollection typeVector)
        {
            bool first = true;
            foreach (Handle handle in typeVector)
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

                case HandleType.TypeInstantiationSignature:
                    EmitTypeInstantiationName(typeHandle.ToTypeInstantiationSignatureHandle(_metadataReader), flags);
                    break;

                case HandleType.SZArraySignature:
                    EmitSZArrayTypeName(typeHandle.ToSZArraySignatureHandle(_metadataReader), flags);
                    break;

                case HandleType.ArraySignature:
                    EmitArrayTypeName(typeHandle.ToArraySignatureHandle(_metadataReader), flags);
                    break;

                case HandleType.PointerSignature:
                    EmitPointerTypeName(typeHandle.ToPointerSignatureHandle(_metadataReader));
                    break;

                case HandleType.ByReferenceSignature:
                    EmitByRefTypeName(typeHandle.ToByReferenceSignatureHandle(_metadataReader));
                    break;

                case HandleType.TypeDefinition:
                    EmitTypeDefinitionName(typeHandle.ToTypeDefinitionHandle(_metadataReader), flags);
                    break;

                case HandleType.TypeVariableSignature:
                    EmitTypeName(_typeContext.GetTypeVariable(typeHandle.ToTypeVariableSignatureHandle(_metadataReader).GetTypeVariableSignature(_metadataReader).Number), flags);
                    break;

                case HandleType.MethodTypeVariableSignature:
                    EmitTypeName(_typeContext.GetMethodVariable(typeHandle.ToMethodTypeVariableSignatureHandle(_metadataReader).GetMethodTypeVariableSignature(_metadataReader).Number), flags);
                    break;

                case HandleType.GenericParameter:
                    EmitString(typeHandle.ToGenericParameterHandle(_metadataReader).GetGenericParameter(_metadataReader).Name);
                    break;

                case HandleType.FunctionPointerSignature:
                    EmitFunctionPointerTypeName();
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

        /// <summary>
        /// Emit namespace reference.
        /// </summary>
        /// <param name="namespaceRefHandle">Namespace reference handle</param>
        private void EmitNamespaceReferenceName(NamespaceReferenceHandle namespaceRefHandle)
        {
            NamespaceReference namespaceRef = _metadataReader.GetNamespaceReference(namespaceRefHandle);
            if (!namespaceRef.ParentScopeOrNamespace.IsNil &&
                namespaceRef.ParentScopeOrNamespace.HandleType == HandleType.NamespaceReference)
            {
                int charsWritten = _outputBuilder.Length;
                EmitNamespaceReferenceName(namespaceRef.ParentScopeOrNamespace.ToNamespaceReferenceHandle(_metadataReader));
                if (_outputBuilder.Length - charsWritten > 0)
                    _outputBuilder.Append('.');
            }
            EmitString(namespaceRef.Name);
        }

        private void EmitNamespaceDefinitionName(NamespaceDefinitionHandle namespaceDefHandle)
        {
            NamespaceDefinition namespaceDef = _metadataReader.GetNamespaceDefinition(namespaceDefHandle);
            if (!namespaceDef.ParentScopeOrNamespace.IsNil &&
                namespaceDef.ParentScopeOrNamespace.HandleType == HandleType.NamespaceDefinition)
            {
                int charsWritten = _outputBuilder.Length;
                EmitNamespaceDefinitionName(namespaceDef.ParentScopeOrNamespace.ToNamespaceDefinitionHandle(_metadataReader));
                if (_outputBuilder.Length - charsWritten > 0)
                    _outputBuilder.Append('.');
            }
            EmitString(namespaceDef.Name);
        }

        /// <summary>
        /// Emit type reference.
        /// </summary>
        private void EmitTypeReferenceName(TypeReferenceHandle typeRefHandle, Flags flags)
        {
            TypeReference typeRef = _metadataReader.GetTypeReference(typeRefHandle);
            if (!typeRef.ParentNamespaceOrType.IsNil)
            {
                if (typeRef.ParentNamespaceOrType.HandleType != HandleType.NamespaceReference)
                {
                    // Nested type
                    EmitTypeName(typeRef.ParentNamespaceOrType, flags);
                    if ((flags & Flags.ReflectionFormat) != 0)
                        _outputBuilder.Append('+');
                    else
                        _outputBuilder.Append('.');
                }
                else if ((flags & Flags.NamespaceQualify) != 0)
                {
                    int charsWritten = _outputBuilder.Length;
                    EmitNamespaceReferenceName(typeRef.ParentNamespaceOrType.ToNamespaceReferenceHandle(_metadataReader));
                    if (_outputBuilder.Length - charsWritten > 0)
                        _outputBuilder.Append('.');
                }
            }
            EmitString(typeRef.TypeName);
        }

        private void EmitTypeDefinitionName(TypeDefinitionHandle typeDefHandle, Flags flags)
        {
            TypeDefinition typeDef = _metadataReader.GetTypeDefinition(typeDefHandle);
            if (!typeDef.EnclosingType.IsNil)
            {
                // Nested type
                EmitTypeName(typeDef.EnclosingType, flags);
                if ((flags & Flags.ReflectionFormat) != 0)
                    _outputBuilder.Append('+');
                else
                    _outputBuilder.Append('.');
            }
            else if ((flags & Flags.NamespaceQualify) != 0)
            {
                int charsWritten = _outputBuilder.Length;
                EmitNamespaceDefinitionName(typeDef.NamespaceDefinition);
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
        /// Emit generic instantiation type.
        /// </summary>
        private void EmitTypeInstantiationName(TypeInstantiationSignatureHandle typeInstHandle, Flags flags)
        {
            // Stack trace metadata ignores the instantiation arguments of the type in the CLR
            TypeInstantiationSignature typeInst = _metadataReader.GetTypeInstantiationSignature(typeInstHandle);
            EmitTypeName(typeInst.GenericType, flags);
        }

        /// <summary>
        /// Emit SZArray (single-dimensional array with zero lower bound) type.
        /// </summary>
        private void EmitSZArrayTypeName(SZArraySignatureHandle szArraySigHandle, Flags flags)
        {
            SZArraySignature szArraySig = _metadataReader.GetSZArraySignature(szArraySigHandle);
            EmitTypeName(szArraySig.ElementType, flags);
            _outputBuilder.Append("[]");
        }

        /// <summary>
        /// Emit multi-dimensional array type.
        /// </summary>
        private void EmitArrayTypeName(ArraySignatureHandle arraySigHandle, Flags flags)
        {
            ArraySignature arraySig = _metadataReader.GetArraySignature(arraySigHandle);
            EmitTypeName(arraySig.ElementType, flags);
            _outputBuilder.Append('[');
            if (arraySig.Rank > 1)
            {
                _outputBuilder.Append(',', arraySig.Rank - 1);
            }
            else
            {
                _outputBuilder.Append('*');
            }
            _outputBuilder.Append(']');
        }

        /// <summary>
        /// Emit pointer type.
        /// </summary>
        /// <param name="pointerSigHandle">Pointer type specification signature handle</param>
        private void EmitPointerTypeName(PointerSignatureHandle pointerSigHandle)
        {
            PointerSignature pointerSig = _metadataReader.GetPointerSignature(pointerSigHandle);
            EmitTypeName(pointerSig.Type, Flags.None);
            _outputBuilder.Append('*');
        }

        /// <summary>
        /// Emit function pointer type.
        /// </summary>
        private static void EmitFunctionPointerTypeName()
        {
            // Function pointer types have no textual representation and we have tests making sure
            // they show up as empty strings in stack traces, so deliberately do nothing.
        }

        /// <summary>
        /// Emit by-reference type.
        /// </summary>
        /// <param name="byRefSigHandle">ByReference type specification signature handle</param>
        private void EmitByRefTypeName(ByReferenceSignatureHandle byRefSigHandle)
        {
            ByReferenceSignature byRefSig = _metadataReader.GetByReferenceSignature(byRefSigHandle);
            EmitTypeName(byRefSig.Type, Flags.None);
            _outputBuilder.Append('&');
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

            public static Handle GetHandleAt(HandleCollection collection, int index)
            {
                int currentIndex = 0;

                foreach (var currentArg in collection)
                {
                    if (currentIndex == index)
                        return currentArg;
                    currentIndex++;
                }

                Debug.Assert(false);
                return default(Handle);
            }

            public static Handle GetHandleAt(GenericParameterHandleCollection collection, int index)
            {
                int currentIndex = 0;

                foreach (var currentArg in collection)
                {
                    if (currentIndex == index)
                        return currentArg;
                    currentIndex++;
                }

                Debug.Assert(false);
                return default(Handle);
            }

            public Handle GetTypeVariable(int index)
            {
                return _typeContext is GenericParameterHandleCollection ?
                    GetHandleAt((GenericParameterHandleCollection)_typeContext, index) :
                    GetHandleAt((HandleCollection)_typeContext, index);
            }

            public Handle GetMethodVariable(int index)
            {
                return _methodContext is GenericParameterHandleCollection ?
                    GetHandleAt((GenericParameterHandleCollection)_methodContext, index) :
                    GetHandleAt((HandleCollection)_methodContext, index);
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
                        return default(HandleCollection);

                    case HandleType.TypeSpecification:
                        TypeSpecification typeSpec = handle.ToTypeSpecificationHandle(metadataReader).GetTypeSpecification(metadataReader);
                        if (typeSpec.Signature.HandleType != HandleType.TypeInstantiationSignature)
                        {
                            Debug.Assert(false);
                            return default(HandleCollection);
                        }
                        return typeSpec.Signature.ToTypeInstantiationSignatureHandle(metadataReader).GetTypeInstantiationSignature(metadataReader).GenericTypeArguments;

                    default:
                        Debug.Assert(false);
                        return default(HandleCollection);
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
