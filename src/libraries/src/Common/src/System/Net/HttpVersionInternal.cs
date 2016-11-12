// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Net
{
    internal static class HttpVersionInternal
    {
        public static readonly Version Unknown = new Version(0, 0);
        public static readonly Version Version10 = new Version(1, 0);
        public static readonly Version Version11 = new Version(1, 1);
        public static readonly Version Version20 = new Version(2, 0);
    }
}
