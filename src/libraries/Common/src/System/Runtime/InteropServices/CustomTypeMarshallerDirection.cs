// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

#if !TEST_CORELIB
using System.ComponentModel;
#endif

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
    enum CustomTypeMarshallerDirection
    {
#if !TEST_CORELIB
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        None = 0,
        In = 0x1,
        Out = 0x2,
        Ref = In | Out,
    }
}
