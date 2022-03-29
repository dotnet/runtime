// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.Runtime.Augments;
using Internal.Runtime.TypeLoader;
using Internal.Runtime.CompilerServices;

using Internal.TypeSystem;
using Internal.TypeSystem.NativeFormat;

namespace Internal.Runtime.TypeLoader
{
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
    /// <summary>
    /// Resolver structure for that can be used to resolve tokens in a given context
    /// </summary>
    internal struct FixupCellMetadataResolver
    {
        public FixupCellMetadataResolver(NativeFormatMetadataUnit metadataUnit)
        {
            _metadataUnit = metadataUnit;
            _typeContext = null;
            _methodContext = null;
            _loadContextFromNativeLayout = null;
        }

        public FixupCellMetadataResolver(NativeFormatMetadataUnit metadataUnit, TypeDesc typeContext)
        {
            _metadataUnit = metadataUnit;
            _typeContext = typeContext;
            _methodContext = null;
            _loadContextFromNativeLayout = null;
        }

        public FixupCellMetadataResolver(NativeFormatMetadataUnit metadataUnit, MethodDesc methodContext)
        {
            _metadataUnit = metadataUnit;
            _methodContext = methodContext;
            _typeContext = methodContext.OwningType;
            _loadContextFromNativeLayout = null;
        }

        public FixupCellMetadataResolver(NativeFormatMetadataUnit metadataUnit, NativeLayoutInfoLoadContext loadContext)
        {
            _metadataUnit = metadataUnit;
            _methodContext = null;
            _typeContext = null;
            _loadContextFromNativeLayout = loadContext;
        }

        private NativeFormatMetadataUnit _metadataUnit;
        private TypeDesc _typeContext;
        private MethodDesc _methodContext;
        private NativeLayoutInfoLoadContext _loadContextFromNativeLayout;

        public TypeDesc GetType(Internal.Metadata.NativeFormat.Handle token)
        {
            TypeDesc type = _metadataUnit.GetType(token);
            TypeDesc instantiatedType = type.InstantiateSignature(TypeInstantiation, MethodInstantiation);
            return instantiatedType;
        }

        public MethodDesc GetMethod(Internal.Metadata.NativeFormat.Handle token)
        {
            MethodDesc method = _metadataUnit.GetMethod(token, null);
            MethodDesc instantiatedMethod = method.InstantiateSignature(TypeInstantiation, MethodInstantiation);
            return instantiatedMethod;
        }

        public FieldDesc GetField(Internal.Metadata.NativeFormat.Handle token)
        {
            FieldDesc field = _metadataUnit.GetField(token, null);
            FieldDesc instantiatedField = field.InstantiateSignature(TypeInstantiation, MethodInstantiation);
            return instantiatedField;
        }

        public RuntimeSignature GetSignature(Internal.Metadata.NativeFormat.Handle token)
        {
            switch (token.HandleType)
            {
                // These are the only valid token types for creating a method signature
                case Internal.Metadata.NativeFormat.HandleType.Method:
                case Internal.Metadata.NativeFormat.HandleType.MemberReference:
                case Internal.Metadata.NativeFormat.HandleType.QualifiedMethod:
                case Internal.Metadata.NativeFormat.HandleType.MethodInstantiation:
                case Internal.Metadata.NativeFormat.HandleType.MethodSignature:
                    break;

                default:
                    Environment.FailFast("Unknown and invalid handle type");
                    break;
            }
            return RuntimeSignature.CreateFromMethodHandle(_metadataUnit.RuntimeModule, token.ToInt());
        }

        public Instantiation TypeInstantiation
        {
            get
            {
                if (_loadContextFromNativeLayout != null)
                    return _loadContextFromNativeLayout._typeArgumentHandles;
                else
                    return _typeContext?.Instantiation ?? Instantiation.Empty;
            }
        }

        public Instantiation MethodInstantiation
        {
            get
            {
                if (_loadContextFromNativeLayout != null)
                    return _loadContextFromNativeLayout._methodArgumentHandles;
                else
                    return _methodContext?.Instantiation ?? Instantiation.Empty;
            }
        }
    }
#endif
}
