// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader;

/// <summary>
/// Exception thrown when a virtual memory read operation fails.
/// </summary>
public class VirtualReadException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualReadException"/> class.
    /// </summary>
    public VirtualReadException()
    {
        HResult = CorDbgHResults.CORDBG_E_READVIRTUAL_FAILURE;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualReadException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public VirtualReadException(string message) : base(message)
    {
        HResult = CorDbgHResults.CORDBG_E_READVIRTUAL_FAILURE;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualReadException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public VirtualReadException(string message, Exception innerException) : base(message, innerException)
    {
        HResult = CorDbgHResults.CORDBG_E_READVIRTUAL_FAILURE;
    }
}
