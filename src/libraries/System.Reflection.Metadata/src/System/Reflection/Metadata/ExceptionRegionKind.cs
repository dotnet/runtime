// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata
{
    public enum ExceptionRegionKind : ushort
    {
        Catch = 0,
        Filter = 1,
        Finally = 2,
        Fault = 4,
    }
}
