// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.NET.HostModel.AppHost
{
    /// <summary>
    /// Given app file name is longer than 1024 bytes
    /// </summary>
    public class AppNameTooLongException : AppHostUpdateException
    {
        public string LongName { get; }
        public AppNameTooLongException(string name)
        {
            LongName = name;
        }

    }
}

