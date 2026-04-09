// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.Runtime
{
    //
    // The low 2 bits of the interface dispatch cell's cache pointer are treated specially so that we can avoid the
    // need for extra fields on the type.
    //
    // Keep these in sync with the native copy in src\Native\Runtime\inc\rhbinder.h
    //
    public enum InterfaceDispatchCellCachePointerFlags
    {
        CachePointerPointsAtCache = 0x0,
        CachePointerIsInterfacePointerOrMetadataToken = 0x1,
        CachePointerIsIndirectedInterfaceRelativePointer = 0x2,
        CachePointerIsInterfaceRelativePointer = 0x3,
        CachePointerMask = 0x3,
        CachePointerMaskShift = 0x2,
        MaxVTableOffsetPlusOne = 0x1000
    }
}
