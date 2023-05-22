// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Collection of "qualified handle" tuples.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Runtime.TypeInfos;
using System.Runtime.CompilerServices;

namespace System.Reflection.Runtime.General
{
    [CLSCompliant(false)]
    public partial struct QSignatureTypeHandle
    {
        public object Reader { get { return _reader; } }
        private object _reader;
#if ECMA_METADATA_SUPPORT
        private readonly global::System.Reflection.Metadata.BlobReader _blobReader;
#endif
        private global::Internal.Metadata.NativeFormat.Handle _handle;

        internal RuntimeTypeInfo Resolve(TypeContext typeContext)
        {
            Exception? exception = null;
            RuntimeTypeInfo? runtimeType = TryResolve(typeContext, ref exception);
            if (runtimeType == null)
                throw exception!;
            return runtimeType;
        }

        internal RuntimeTypeInfo? TryResolve(TypeContext typeContext, ref Exception? exception)
        {
            if (Reader is global::Internal.Metadata.NativeFormat.MetadataReader)
            {
                return _handle.TryResolve((global::Internal.Metadata.NativeFormat.MetadataReader)Reader, typeContext, ref exception);
            }

#if ECMA_METADATA_SUPPORT
            if (Reader is global::System.Reflection.Metadata.MetadataReader ecmaReader)
            {
                return TryResolveSignature(typeContext, ref exception);
            }
#endif

            throw new BadImageFormatException();  // Expected TypeRef, Def or Spec with MetadataReader
        }

        // Return any custom modifiers modifying the passed-in type and whose required/optional bit matches the passed in boolean.
        // Because this is intended to service the GetCustomModifiers() apis, this helper will always return a freshly allocated array
        // safe for returning to api callers.
        internal Type[] GetCustomModifiers(TypeContext typeContext, bool optional)
        {
#if ECMA_METADATA_SUPPORT
            throw new NotImplementedException();
#else
            return _handle.GetCustomModifiers((global::Internal.Metadata.NativeFormat.MetadataReader)Reader, typeContext, optional);
#endif
        }

        internal Type GetModifiedType(TypeContext typeContext)
        {
            return ModifiedType.Create(Resolve(typeContext), (global::Internal.Metadata.NativeFormat.MetadataReader)Reader, _handle);
        }
    }
}
