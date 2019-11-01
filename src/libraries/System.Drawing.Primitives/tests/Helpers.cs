// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Tests
{
    public static class Helpers
    {
        public const string IsDrawingSupported = nameof(Helpers) + "." + nameof(GetIsDrawingSupported);

        public static bool GetIsDrawingSupported() => /* ActiveIssue(24525) */ PlatformDetection.IsNotRedHatFamily6 && PlatformDetection.IsDrawingSupported;
    }
}
