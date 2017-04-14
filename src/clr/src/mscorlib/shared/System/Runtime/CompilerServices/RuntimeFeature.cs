// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
    public static class RuntimeFeature
    {
        /// <summary>
        /// Checks whether a certain feature is supported by the Runtime.
        /// </summary>
        public static bool IsSupported(string feature)
        {
            // No features are supported for now.
            // These features should be added as public const string fields in the same class.
            // Example: public const string FeatureName = nameof(FeatureName);

            return false;
        }
    }
}
