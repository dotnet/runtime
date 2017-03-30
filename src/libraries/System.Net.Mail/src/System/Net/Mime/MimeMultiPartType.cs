// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Net.Mime
{
    internal enum MimeMultiPartType
    {
        Mixed = 0,
        Alternative = 1,
        Parallel = 2,
        Related = 3,
        Unknown = -1
    }
}
