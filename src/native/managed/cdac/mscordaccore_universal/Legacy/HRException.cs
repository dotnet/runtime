// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Used to return a specific HResult when in a try/catch block.
/// Useful to short circuit logic in a COM interface implementation while still
/// running through the verification block (if applicable).
/// </summary>
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
