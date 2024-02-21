// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices
{
    public static partial class RuntimeFeature
    {
        [FeatureCheck(typeof(RequiresDynamicCodeAttribute))]
        public static bool IsDynamicCodeSupported => false;

        [FeatureCheck(typeof(RequiresDynamicCodeAttribute))]
        public static bool IsDynamicCodeCompiled => false;
    }
}
