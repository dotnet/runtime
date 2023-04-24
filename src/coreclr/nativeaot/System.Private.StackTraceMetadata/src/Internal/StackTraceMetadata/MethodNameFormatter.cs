// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

using Internal.Metadata.NativeFormat;

namespace Internal.StackTraceMetadata
{
    class MethodNameFormatter
    {
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

        public static string FormatMethodName(MetadataReader metadataReader, Handle methodHandle)
        {
            MethodNameFormatter formatter = new MethodNameFormatter(metadataReader, SigTypeContext.FromMethod(metadataReader, methodHandle));
            formatter.EmitMethodName(methodHandle);
            return formatter._outputBuilder.ToString();
        }

        public static string FormatMethodName(MetadataReader metadataReader, TypeDefinitionHandle enclosingTypeHandle, MethodHandle methodHandle)
        {
            MethodNameFormatter formatter = new MethodNameFormatter(metadataReader, SigTypeContext.FromMethod(metadataReader, enclosingTypeHandle, methodHandle));

            Method method = metadataReader.GetMethod(methodHandle);
            formatter.EmitTypeName(enclosingTypeHandle, namespaceQualified: true);
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
                formatter.EmitTypeName(handle, namespaceQualified: false);
            }
            if (!first)
            {
                formatter._outputBuilder.Append(']');
            }

            formatter.EmitMethodParameters(methodHandle);

            return formatter._outputBuilder.ToString();
        }

        /// <summary>
        /// Emit a given method signature to a specified string builder.
        /// </summary>
        /// <param name="methodHandle">Method reference or instantiation token</param>
        private void EmitMethodName(Handle methodHandle)
        {
            switch (methodHandle.HandleType)
            {
                case HandleType.MemberReference:
                    EmitMethodReferenceName(methodHandle.ToMemberReferenceHandle(_metadataReader));
                    break;

                case HandleType.MethodInstantiation:
                    EmitMethodInstantiationName(methodHandle.ToMethodInstantiationHandle(_metadataReader));
                    break;

                case HandleType.QualifiedMethod:
                    EmitMethodDefinitionName(methodHandle.ToQualifiedMethodHandle(_metadataReader));
                    break;

                default:
                    Debug.Assert(false);
                    _outputBuilder.Append("???");
                    break;
            }
        }

        /// <summary>
        /// Emit method reference to the output string builder.
        /// </summary>
        /// <param name="memberRefHandle">Member reference handle</param>
        private void EmitMethodReferenceName(MemberReferenceHandle memberRefHandle)
        {
            MemberReference methodRef = _metadataReader.GetMemberReference(memberRefHandle);
            MethodSignature methodSignature;
            EmitContainingTypeAndMethodName(methodRef, out methodSignature);
            EmitMethodParameters(methodSignature);
        }

        /// <summary>
        /// Emit generic method instantiation to the output string builder.
        /// </summary>
        /// <param name="methodInstHandle">Method instantiation handle</param>
        private void EmitMethodInstantiationName(MethodInstantiationHandle methodInstHandle)
        {
            MethodInstantiation methodInst = _metadataReader.GetMethodInstantiation(methodInstHandle);

            if (methodInst.Method.HandleType == HandleType.MemberReference)
            {
                MemberReferenceHandle methodRefHandle = methodInst.Method.ToMemberReferenceHandle(_metadataReader);
                MemberReference methodRef = methodRefHandle.GetMemberReference(_metadataReader);
                EmitContainingTypeAndMethodName(methodRef, out MethodSignature methodSignature);
                EmitGenericArguments(methodInst.GenericTypeArguments);
                EmitMethodParameters(methodSignature);
            }
            else
            {
                QualifiedMethodHandle qualifiedMethodHandle = methodInst.Method.ToQualifiedMethodHandle(_metadataReader);
                QualifiedMethod qualifiedMethod = _metadataReader.GetQualifiedMethod(qualifiedMethodHandle);
                EmitContainingTypeAndMethodName(qualifiedMethod);
                EmitGenericArguments(methodInst.GenericTypeArguments);
                EmitMethodParameters(qualifiedMethod.Method);
            }
        }

        private void EmitMethodDefinitionName(QualifiedMethodHandle qualifiedMethodHandle)
        {
            QualifiedMethod qualifiedMethod = _metadataReader.GetQualifiedMethod(qualifiedMethodHandle);
            EmitContainingTypeAndMethodName(qualifiedMethod);
            EmitMethodParameters(qualifiedMethod.Method);
        }

        /// <summary>
        /// Emit containing type and method name and extract the method signature from a method reference.
        /// </summary>
        /// <param name="methodRef">Method reference to format</param>
        /// <param name="methodSignature">Output method signature</param>
        private void EmitContainingTypeAndMethodName(MemberReference methodRef, out MethodSignature methodSignature)
        {
            methodSignature = _metadataReader.GetMethodSignature(methodRef.Signature.ToMethodSignatureHandle(_metadataReader));
            EmitTypeName(methodRef.Parent, namespaceQualified: true);
            _outputBuilder.Append('.');
            EmitString(methodRef.Name);
        }

        private void EmitContainingTypeAndMethodName(QualifiedMethod qualifiedMethod)
        {
            Method method = _metadataReader.GetMethod(qualifiedMethod.Method);
            EmitTypeName(qualifiedMethod.EnclosingType, namespaceQualified: true);
            _outputBuilder.Append('.');
            EmitString(method.Name);
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

                EmitTypeName(type, namespaceQualified: false);

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
                EmitTypeName(handle, namespaceQualified: false);
            }
        }

        /// <summary>
        /// Emit the name of a given type to the output string builder.
        /// </summary>
        /// <param name="typeHandle">Type handle to format</param>
        /// <param name="namespaceQualified">When set to true, include namespace information</param>
        private void EmitTypeName(Handle typeHandle, bool namespaceQualified)
        {
            switch (typeHandle.HandleType)
            {
                case HandleType.TypeReference:
                    EmitTypeReferenceName(typeHandle.ToTypeReferenceHandle(_metadataReader), namespaceQualified);
                    break;

                case HandleType.TypeSpecification:
                    EmitTypeSpecificationName(typeHandle.ToTypeSpecificationHandle(_metadataReader), namespaceQualified);
                    break;

                case HandleType.TypeInstantiationSignature:
                    EmitTypeInstantiationName(typeHandle.ToTypeInstantiationSignatureHandle(_metadataReader), namespaceQualified);
                    break;

                case HandleType.SZArraySignature:
                    EmitSZArrayTypeName(typeHandle.ToSZArraySignatureHandle(_metadataReader), namespaceQualified);
                    break;

                case HandleType.ArraySignature:
                    EmitArrayTypeName(typeHandle.ToArraySignatureHandle(_metadataReader), namespaceQualified);
                    break;

                case HandleType.PointerSignature:
                    EmitPointerTypeName(typeHandle.ToPointerSignatureHandle(_metadataReader));
                    break;

                case HandleType.ByReferenceSignature:
                    EmitByRefTypeName(typeHandle.ToByReferenceSignatureHandle(_metadataReader));
                    break;

                case HandleType.TypeDefinition:
                    EmitTypeDefinitionName(typeHandle.ToTypeDefinitionHandle(_metadataReader), namespaceQualified);
                    break;

                case HandleType.TypeVariableSignature:
                    EmitTypeName(_typeContext.GetTypeVariable(typeHandle.ToTypeVariableSignatureHandle(_metadataReader).GetTypeVariableSignature(_metadataReader).Number), namespaceQualified);
                    break;

                case HandleType.MethodTypeVariableSignature:
                    EmitTypeName(_typeContext.GetMethodVariable(typeHandle.ToMethodTypeVariableSignatureHandle(_metadataReader).GetMethodTypeVariableSignature(_metadataReader).Number), namespaceQualified);
                    break;

                case HandleType.GenericParameter:
                    EmitString(typeHandle.ToGenericParameterHandle(_metadataReader).GetGenericParameter(_metadataReader).Name);
                    break;

                case HandleType.FunctionPointerSignature:
                    EmitFunctionPointerTypeName();
                    break;

                default:
                    Debug.Assert(false, $"Type handle {typeHandle.HandleType} was not handled");
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
            if (!namespaceRef.ParentScopeOrNamespace.IsNull(_metadataReader) &&
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
            if (!namespaceDef.ParentScopeOrNamespace.IsNull(_metadataReader) &&
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
        /// <param name="typeRefHandle">Type reference handle</param>
        /// <param name="namespaceQualified">When set to true, include namespace information</param>
        private void EmitTypeReferenceName(TypeReferenceHandle typeRefHandle, bool namespaceQualified)
        {
            TypeReference typeRef = _metadataReader.GetTypeReference(typeRefHandle);
            if (!typeRef.ParentNamespaceOrType.IsNull(_metadataReader))
            {
                if (typeRef.ParentNamespaceOrType.HandleType != HandleType.NamespaceReference)
                {
                    // Nested type
                    EmitTypeName(typeRef.ParentNamespaceOrType, namespaceQualified);
                    _outputBuilder.Append('.');
                }
                else if (namespaceQualified)
                {
                    int charsWritten = _outputBuilder.Length;
                    EmitNamespaceReferenceName(typeRef.ParentNamespaceOrType.ToNamespaceReferenceHandle(_metadataReader));
                    if (_outputBuilder.Length - charsWritten > 0)
                        _outputBuilder.Append('.');
                }
            }
            EmitString(typeRef.TypeName);
        }

        private void EmitTypeDefinitionName(TypeDefinitionHandle typeDefHandle, bool namespaceQualified)
        {
            TypeDefinition typeDef = _metadataReader.GetTypeDefinition(typeDefHandle);
            if (!typeDef.EnclosingType.IsNull(_metadataReader))
            {
                // Nested type
                EmitTypeName(typeDef.EnclosingType, namespaceQualified);
                _outputBuilder.Append('.');
            }
            else if (namespaceQualified)
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
        /// <param name="typeSpecHandle">Type specification handle</param>
        /// <param name="namespaceQualified">When set to true, include namespace information</param>
        private void EmitTypeSpecificationName(TypeSpecificationHandle typeSpecHandle, bool namespaceQualified)
        {
            TypeSpecification typeSpec = _metadataReader.GetTypeSpecification(typeSpecHandle);
            EmitTypeName(typeSpec.Signature, namespaceQualified);
        }

        /// <summary>
        /// Emit generic instantiation type.
        /// </summary>
        /// <param name="typeInstHandle">Instantiated type specification signature handle</param>
        /// <param name="namespaceQualified">When set to true, include namespace information</param>
        private void EmitTypeInstantiationName(TypeInstantiationSignatureHandle typeInstHandle, bool namespaceQualified)
        {
            // Stack trace metadata ignores the instantiation arguments of the type in the CLR
            TypeInstantiationSignature typeInst = _metadataReader.GetTypeInstantiationSignature(typeInstHandle);
            EmitTypeName(typeInst.GenericType, namespaceQualified);
        }

        /// <summary>
        /// Emit SZArray (single-dimensional array with zero lower bound) type.
        /// </summary>
        /// <param name="szArraySigHandle">SZArray type specification signature handle</param>
        /// <param name="namespaceQualified">When set to true, include namespace information</param>
        private void EmitSZArrayTypeName(SZArraySignatureHandle szArraySigHandle, bool namespaceQualified)
        {
            SZArraySignature szArraySig = _metadataReader.GetSZArraySignature(szArraySigHandle);
            EmitTypeName(szArraySig.ElementType, namespaceQualified);
            _outputBuilder.Append("[]");
        }

        /// <summary>
        /// Emit multi-dimensional array type.
        /// </summary>
        /// <param name="arraySigHandle">Multi-dimensional array type specification signature handle</param>
        /// <param name="namespaceQualified">When set to true, include namespace information</param>
        private void EmitArrayTypeName(ArraySignatureHandle arraySigHandle, bool namespaceQualified)
        {
            ArraySignature arraySig = _metadataReader.GetArraySignature(arraySigHandle);
            EmitTypeName(arraySig.ElementType, namespaceQualified);
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
            EmitTypeName(pointerSig.Type, namespaceQualified: false);
            _outputBuilder.Append('*');
        }

        /// <summary>
        /// Emit function pointer type.
        /// </summary>
        private void EmitFunctionPointerTypeName()
        {
            _outputBuilder.Append("IntPtr");
        }

        /// <summary>
        /// Emit by-reference type.
        /// </summary>
        /// <param name="byRefSigHandle">ByReference type specification signature handle</param>
        private void EmitByRefTypeName(ByReferenceSignatureHandle byRefSigHandle)
        {
            ByReferenceSignature byRefSig = _metadataReader.GetByReferenceSignature(byRefSigHandle);
            EmitTypeName(byRefSig.Type, namespaceQualified: false);
            _outputBuilder.Append('&');
        }

        /// <summary>
        /// Emit angle-bracketed list of type / method generic arguments.
        /// </summary>
        /// <param name="genericArguments">Collection of generic argument type handles</param>
        private void EmitGenericArguments(HandleCollection genericArguments)
        {
            _outputBuilder.Append('[');
            EmitTypeVector(genericArguments);
            _outputBuilder.Append(']');
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

            public static SigTypeContext FromMethod(MetadataReader metadataReader, Handle methodHandle)
            {
                object typeContext;
                object methodContext;

                switch (methodHandle.HandleType)
                {
                    case HandleType.MemberReference:
                        typeContext = GetTypeContext(metadataReader, methodHandle);
                        methodContext = default(HandleCollection);
                        break;

                    case HandleType.MethodInstantiation:
                        MethodInstantiation methodInst = methodHandle.ToMethodInstantiationHandle(metadataReader).GetMethodInstantiation(metadataReader);
                        typeContext = GetTypeContext(metadataReader, methodInst.Method);
                        methodContext = methodInst.GenericTypeArguments;
                        break;

                    case HandleType.QualifiedMethod:
                        QualifiedMethod qualifiedMethod = methodHandle.ToQualifiedMethodHandle(metadataReader).GetQualifiedMethod(metadataReader);
                        typeContext = GetTypeContext(metadataReader, qualifiedMethod.EnclosingType);
                        methodContext = qualifiedMethod.Method.GetMethod(metadataReader).GenericParameters;
                        break;
                    default:
                        Debug.Assert(false);
                        return default(SigTypeContext);
                }

                return new SigTypeContext(typeContext, methodContext);
            }

            public static SigTypeContext FromMethod(MetadataReader metadataReader, TypeDefinitionHandle enclosingTypeHandle, MethodHandle methodHandle)
            {
                Method method = metadataReader.GetMethod(methodHandle);
                return new SigTypeContext(GetTypeContext(metadataReader, enclosingTypeHandle), method.GenericParameters);
            }
        }
    }
}
