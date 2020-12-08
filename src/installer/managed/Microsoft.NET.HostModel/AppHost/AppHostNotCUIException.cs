// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.NET.HostModel.AppHost
{
    /// <summary>
    /// Unable to use the input file as application host executable because it's not a
    /// Windows executable for the CUI (Console) subsystem.
    /// </summary>
    public class AppHostNotCUIException : AppHostUpdateException
    {
    }
}
