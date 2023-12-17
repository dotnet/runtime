// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.MethodInfos;
using System.Reflection.Runtime.TypeInfos;

using Internal.Metadata.NativeFormat;

namespace System.Reflection.Runtime.TypeInfos.NativeFormat
{
    internal sealed partial class NativeFormatRuntimeGenericParameterTypeInfoForMethods : NativeFormatRuntimeGenericParameterTypeInfo, IKeyedItem<NativeFormatRuntimeGenericParameterTypeInfoForMethods.UnificationKey>
    {
        private NativeFormatRuntimeGenericParameterTypeInfoForMethods(MetadataReader reader, GenericParameterHandle genericParameterHandle, RuntimeNamedMethodInfo declaringRuntimeNamedMethodInfo)
           : base(reader, genericParameterHandle, genericParameterHandle.GetGenericParameter(reader))
        {
            Debug.Assert(declaringRuntimeNamedMethodInfo.DeclaringType.IsTypeDefinition);
            _declaringRuntimeNamedMethodInfo = declaringRuntimeNamedMethodInfo;
        }

        public sealed override bool IsGenericTypeParameter => false;
        public sealed override bool IsGenericMethodParameter => true;

        public sealed override MethodBase DeclaringMethod
        {
            get
            {
                return _declaringRuntimeNamedMethodInfo;
            }
        }

        //
        // Implements IKeyedItem.Key.
        //
        // Produce the key. This is a high-traffic property and is called while the hash table's lock is held. Thus, it should
        // return a precomputed stored value and refrain from invoking other methods.
        //
        public UnificationKey Key
        {
            get
            {
                return new UnificationKey(_declaringRuntimeNamedMethodInfo, Reader, GenericParameterHandle);
            }
        }

        internal sealed override RuntimeTypeInfo InternalDeclaringType
        {
            get
            {
                return _declaringRuntimeNamedMethodInfo.DeclaringType.ToRuntimeTypeInfo();
            }
        }

        internal sealed override TypeContext TypeContext
        {
            get
            {
                TypeContext typeContext = this.InternalDeclaringType.TypeContext;
                return new TypeContext(typeContext.GenericTypeArguments, _declaringRuntimeNamedMethodInfo.RuntimeGenericArgumentsOrParameters);
            }
        }

        private readonly RuntimeNamedMethodInfo _declaringRuntimeNamedMethodInfo;
    }
}
