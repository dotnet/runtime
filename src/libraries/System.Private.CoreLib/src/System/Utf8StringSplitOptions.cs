// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    // TODO_UTF8STRING: This should be removed and we should use regular StringSplitOptions
    // once a 'TrimEntries' flag gets added to the type.

    [Flags]
    public enum Utf8StringSplitOptions
    {
        None = 0,
        RemoveEmptyEntries = 1,
        TrimEntries = 2
    }
}
