// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.Runtime
{
    internal enum ReflectionMapBlob
    {
        TypeMap                                     = 1,
        ArrayMap                                    = 2,
        // unused                                   = 3,
        // unused                                   = 4,
        BlockReflectionTypeMap                      = 5,
        InvokeMap                                   = 6,
        VirtualInvokeMap                            = 7,
        CommonFixupsTable                           = 8,
        FieldAccessMap                              = 9,
        CCtorContextMap                             = 10,
        // unused                                   = 11,
        // unused                                   = 12,
        EmbeddedMetadata                            = 13,
        DefaultConstructorMap                       = 14,
        UnboxingAndInstantiatingStubMap             = 15,
        StructMarshallingStubMap                    = 16,
        DelegateMarshallingStubMap                  = 17,
        GenericVirtualMethodTable                   = 18,
        InterfaceGenericVirtualMethodTable          = 19,

        // Reflection template types/methods blobs:
        TypeTemplateMap                             = 21,
        GenericMethodsTemplateMap                   = 22,
        // unused                                   = 23,
        BlobIdResourceIndex                         = 24,
        BlobIdResourceData                          = 25,
        BlobIdStackTraceEmbeddedMetadata            = 26,
        BlobIdStackTraceMethodRvaToTokenMapping     = 27,

        //Native layout blobs:
        NativeLayoutInfo                            = 30,
        NativeReferences                            = 31,
        GenericsHashtable                           = 32,
        NativeStatics                               = 33,
        StaticsInfoHashtable                        = 34,
        GenericMethodsHashtable                     = 35,
        ExactMethodInstantiationsHashtable          = 36,
    }
}
