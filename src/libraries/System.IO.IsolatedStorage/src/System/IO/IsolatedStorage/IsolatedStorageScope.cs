// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.IO.IsolatedStorage
{
    [Serializable]
    [Flags]
    public enum IsolatedStorageScope
    {
        None = 0,
        User = 1,
        Domain = 2,
        Assembly = 4,
        Roaming = 8,
        Machine = 16,
        Application = 32
    }
}
