// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    public static partial class RuntimeFeature
    {
        public static bool IsDynamicCodeSupported { get; } =
            AppContext.TryGetSwitch("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", out bool isDynamicCodeSupported) ? isDynamicCodeSupported : true;

        public static bool IsDynamicCodeCompiled => IsDynamicCodeSupported;
    }
}
