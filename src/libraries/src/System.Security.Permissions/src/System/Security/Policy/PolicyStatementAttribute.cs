// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    [Flags]
    public enum PolicyStatementAttribute
    {
        All = 3,
        Exclusive = 1,
        LevelFinal = 2,
        Nothing = 0,
    }
}
