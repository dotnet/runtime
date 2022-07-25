// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Reflection;

namespace System.Runtime.CompilerServices
{
    public partial class RuntimeFeature
    {
        public static bool IsDynamicCodeSupported
        {
            [Intrinsic]  // the JIT/AOT compiler will change this flag to false for FullAOT scenarios, otherwise true
            get => IsDynamicCodeSupported;
        }

        public static bool IsDynamicCodeCompiled
        {
#if TARGET_BROWSER || TARGET_IOS || TARGET_TVOS
            get => false;
#else
            [Intrinsic]  // the JIT/AOT compiler will change this flag to false for FullAOT scenarios, otherwise true
            get => IsDynamicCodeCompiled;
#endif
        }
    }
}
