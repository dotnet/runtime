// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.AppHost
{
    /// <summary>
    /// Unable to sign the apphost binary.
    /// </summary>
    public class AppHostSigningException : AppHostUpdateException
    {
        public string ErrorMessage { get; }
        public AppHostSigningException(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
}
