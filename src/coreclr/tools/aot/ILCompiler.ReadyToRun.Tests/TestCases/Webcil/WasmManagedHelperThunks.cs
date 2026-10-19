// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Webcil;

public sealed class WasmManagedHelperTarget
{
}

public static class WasmManagedHelperThunks
{
    // typeof is compiled to the CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE helper, which the runtime backs with
    // the interpreted RuntimeTypeHandle.GetRuntimeTypeFromHandle(IntPtr) when CoreLib is not in the image.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type GetTargetType() => typeof(WasmManagedHelperTarget);
}
