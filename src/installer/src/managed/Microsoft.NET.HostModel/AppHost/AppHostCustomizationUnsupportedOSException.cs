// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.NET.HostModel.AppHost
{
    /// <summary>
    /// The application host executable cannot be customized because adding resources requires 
    /// that the build be performed on Windows (excluding Nano Server).
    /// </summary>
    public class AppHostCustomizationUnsupportedOSException : AppHostUpdateException
    {
    }
}

