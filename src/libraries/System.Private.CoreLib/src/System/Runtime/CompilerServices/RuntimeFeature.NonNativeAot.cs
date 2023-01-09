// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    public static partial class RuntimeFeature
    {
        public static bool IsDynamicCodeSupported
        {
#if MONO
            [Intrinsic]  // the Mono AOT compiler will change this flag to false for FullAOT scenarios, otherwise this code is used
#endif
            get;
        } = AppContext.TryGetSwitch("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", out bool isDynamicCodeSupported) ? isDynamicCodeSupported : true;

        public static bool IsDynamicCodeCompiled
        {
#if MONO
            [Intrinsic]  // the Mono AOT compiler and Interpreter will change this flag to false for FullAOT and interpreted scenarios, otherwise this code is used
#endif
            get => IsDynamicCodeSupported;
        }
    }
}
