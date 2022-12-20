// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.Assemblies;
using System.Reflection.Runtime.TypeParsing;

using Internal.Reflection.Core;
using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.General
{
    internal static partial class TypeResolver
    {
        //
        // Main routine to resolve a typeDef/Ref/Spec.
        //
        internal static RuntimeTypeInfo Resolve(this QTypeDefRefOrSpec typeDefOrRefOrSpec, TypeContext typeContext)
        {
            Exception? exception = null;
            RuntimeTypeInfo runtimeType = typeDefOrRefOrSpec.TryResolve(typeContext, ref exception);
            if (runtimeType == null)
                throw exception!;
            return runtimeType;
        }

        internal static RuntimeTypeInfo TryResolve(this QTypeDefRefOrSpec typeDefOrRefOrSpec, TypeContext typeContext, ref Exception? exception)
        {
            if (typeDefOrRefOrSpec.IsNativeFormatMetadataBased)
            {
                return global::Internal.Metadata.NativeFormat.Handle.FromIntToken(typeDefOrRefOrSpec.Handle).TryResolve((global::Internal.Metadata.NativeFormat.MetadataReader)typeDefOrRefOrSpec.Reader, typeContext, ref exception);
            }

#if ECMA_METADATA_SUPPORT
            if (typeDefOrRefOrSpec.Reader is global::System.Reflection.Metadata.MetadataReader ecmaReader)
                return global::System.Reflection.Metadata.Ecma335.MetadataTokens.Handle(typeDefOrRefOrSpec.Handle).TryResolve(ecmaReader, typeContext, ref exception);
#endif

            throw new BadImageFormatException();  // Expected TypeRef, Def or Spec with MetadataReader
        }
    }
}
