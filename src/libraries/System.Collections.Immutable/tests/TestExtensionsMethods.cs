// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    internal static partial class TestExtensionsMethods
    {
        private static readonly double s_GoldenRatio = (1 + Math.Sqrt(5)) / 2;

        internal static void ValidateDefaultThisBehavior(Action a)
        {
            if (!PlatformDetection.IsNetNative) // ActiveIssue UapAot: TFS 428550, https://github.com/dotnet/corefx/issues/19016 - ImmutableArray's null-guard check (ThrowNullRefIfNotInitialized) can get optimized away by certain compilers.
            {
                Assert.Throws<NullReferenceException>(a);
            }
        }
    }
}
