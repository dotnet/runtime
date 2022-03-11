// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

//
// Types in this file are used for generated p/invokes (docs/design/features/source-generator-pinvokes.md).
//
namespace System.Runtime.InteropServices
{
    [Flags]
#if LIBRARYIMPORT_GENERATOR_TEST
    public
#else
    internal
#endif
    enum CustomTypeMarshallerFeatures
    {
        None = 0,
        UnmanagedResources = 0x1,
        CallerAllocatedBuffer = 0x2,
        TwoStageMarshalling = 0x4
    }
}
