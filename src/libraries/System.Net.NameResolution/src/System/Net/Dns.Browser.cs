// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Net
{
    public static partial class Dns
    {
        public static string GetHostName()
        {
            return Environment.MachineName;
        }
    }
}
