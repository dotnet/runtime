// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;

using Internal.Metadata.NativeFormat;

namespace System.Reflection.Runtime.TypeInfos.NativeFormat
{
    internal sealed partial class NativeFormatRuntimeGenericParameterTypeInfoForTypes : NativeFormatRuntimeGenericParameterTypeInfo
    {
        private NativeFormatRuntimeGenericParameterTypeInfoForTypes(MetadataReader reader, GenericParameterHandle genericParameterHandle, RuntimeTypeDefinitionTypeInfo declaringType)
           : base(reader, genericParameterHandle, genericParameterHandle.GetGenericParameter(reader))
        {
            _declaringType = declaringType;
        }

        public sealed override bool IsGenericTypeParameter => true;
        public sealed override bool IsGenericMethodParameter => false;

        public sealed override MethodBase DeclaringMethod
        {
            get
            {
                return null;
            }
        }

        internal sealed override Type InternalDeclaringType
        {
            get
            {
                return _declaringType;
            }
        }

        internal sealed override TypeContext TypeContext
        {
            get
            {
                return _declaringType.TypeContext;
            }
        }

        private readonly RuntimeTypeDefinitionTypeInfo _declaringType;
    }
}
