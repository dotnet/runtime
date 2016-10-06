// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data
{
    public enum DataRowVersion
    {
        Original = 0x0100,
        Current = 0x0200,
        Proposed = 0x0400,
        Default = Proposed | Current,
    }
}
