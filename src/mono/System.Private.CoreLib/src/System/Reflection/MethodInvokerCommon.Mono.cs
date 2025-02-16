// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal static partial class MethodInvokerCommon
    {
        internal static bool UseInterpretedPath => LocalAppContextSwitches.ForceInterpretedInvoke || !RuntimeFeature.IsDynamicCodeSupported;
    }
}
