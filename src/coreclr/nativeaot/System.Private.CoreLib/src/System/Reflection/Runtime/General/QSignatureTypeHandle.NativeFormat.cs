// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Collection of "qualified handle" tuples.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.Metadata.NativeFormat;
using Internal.Runtime.TypeLoader;

namespace System.Reflection.Runtime.General
{
    public partial struct QSignatureTypeHandle
    {
        public QSignatureTypeHandle(MetadataReader reader, Handle handle, bool skipCheck = false)
        {
            if (!skipCheck)
            {
                if (!handle.IsTypeDefRefSpecOrModifiedTypeHandle(reader))
                    throw new BadImageFormatException();
            }

            Debug.Assert(handle.IsTypeDefRefSpecOrModifiedTypeHandle(reader));
            _reader = reader;
            _handle = handle;

#if ECMA_METADATA_SUPPORT
            _blobReader = default(global::System.Reflection.Metadata.BlobReader);
#endif
        }
    }
}
