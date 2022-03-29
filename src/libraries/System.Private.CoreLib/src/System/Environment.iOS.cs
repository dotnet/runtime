// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System
{
    public static partial class Environment
    {
        // iOS/tvOS aren't allowed to call libproc APIs so return 0 here, this also matches what we returned in earlier releases
        public static long WorkingSet => 0;
    }
}
