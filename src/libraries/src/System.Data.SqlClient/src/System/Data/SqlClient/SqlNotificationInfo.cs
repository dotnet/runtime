// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.SqlClient
{
    public enum SqlNotificationInfo
    {
        Truncate = 0,
        Insert = 1,
        Update = 2,
        Delete = 3,
        Drop = 4,
        Alter = 5,
        Restart = 6,
        Error = 7,
        Query = 8,
        Invalid = 9,
        Options = 10,
        Isolation = 11,
        Expired = 12,
        Resource = 13,
        PreviousFire = 14,
        TemplateLimit = 15,
        Merge = 16,

        // use negative values for client-only-generated values
        Unknown = -1,
        AlreadyChanged = -2
    }
}