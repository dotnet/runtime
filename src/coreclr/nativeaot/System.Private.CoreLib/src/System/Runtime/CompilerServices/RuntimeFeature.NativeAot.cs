// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices
{
    public static partial class RuntimeFeature
    {
        [FeatureSwitchDefinition("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported")]
        public static bool IsDynamicCodeSupported => false;

        [FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
        public static bool IsDynamicCodeCompiled => false;
    }
}
