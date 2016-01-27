// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Tests.Common
{
    internal static class CompareHelper
    {
        public static int NormalizeCompare(int i)
        {
            return
                i == 0 ? 0 :
                i > 0 ? 1 :
                -1;
        }
    }
}
