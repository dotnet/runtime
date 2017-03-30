// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data
{
    public enum UpdateRowSource
    {
        None = 0,

        OutputParameters = 1,

        FirstReturnedRecord = 2,

        Both = 3,
    }
}
