// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.NET.HostModel.AppHost
{
    /// <summary>
    /// Failed to delete apphost when trying to delete incomplete appphost
    /// </summary>
    public class FailedToDeleteApphostException : AppHostUpdateException
    {
        public string ExceptionMessage { get; }
        public FailedToDeleteApphostException(string exceptionMessage)
        {
            ExceptionMessage = exceptionMessage;
        }
    }
}

