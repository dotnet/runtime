// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Exception used to return a specific HResult in a try/catch block, primarily for control flow rather than error handling.
/// Useful to short-circuit logic in a COM interface implementation while still
/// running through the verification block (if applicable).
/// </summary>
/// <remarks>
/// This exception is intended for use in control flow scenarios, not for reporting errors.
/// Note that using exceptions for control flow can have significant performance implications,
/// as exception handling in .NET is relatively expensive. Use with caution and only when necessary.
/// </remarks>
internal class ReturnHResultException : Exception
{
    public ReturnHResultException(int hr)
        : base()
    {
        HResult = hr;
    }

    public ReturnHResultException(int hr, string message)
        : base(message)
    {
        HResult = hr;
    }

    public override string ToString()
    {
        return $"{base.ToString()}, HResult: 0x{HResult:X8}";
    }
}
