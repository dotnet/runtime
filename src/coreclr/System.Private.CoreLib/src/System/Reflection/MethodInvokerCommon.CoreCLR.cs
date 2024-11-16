// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal static partial class MethodInvokerCommon
    {
        // For CoreClr, we may be able to remove the interpreted path; it is not used in the CoreCLR implementation
        // unless the feature switch is enabled. Unlike Mono, there are no interpreted-only platforms.
        internal static bool UseInterpretedPath => LocalAppContextSwitches.ForceInterpretedInvoke || !RuntimeFeature.IsDynamicCodeSupported;
    }
}
