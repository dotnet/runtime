// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Test.Common
{
    public static partial class Capability
    {
        public static bool IsNtlmInstalled()
        {
            return Interop.NetSecurityNative.IsNtlmInstalled();
        }
    }
}
