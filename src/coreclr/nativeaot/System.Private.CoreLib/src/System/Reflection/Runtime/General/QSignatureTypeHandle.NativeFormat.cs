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
        public QSignatureTypeHandle(MetadataReader reader, TypeSignature signature)
        {
            Debug.Assert(reader == signature.Reader);
            _reader = reader;
            _signature = signature;

#if ECMA_METADATA_SUPPORT
            _blobReader = default(global::System.Reflection.Metadata.BlobReader);
#endif
        }
    }
}
