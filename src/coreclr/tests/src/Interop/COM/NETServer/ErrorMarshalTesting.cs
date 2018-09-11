// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

[ComVisible(true)]
[Guid(Server.Contract.Guids.ErrorMarshalTesting)]
public class ErrorMarshalTesting : Server.Contract.IErrorMarshalTesting
{
    public void Throw_HResult(int hresultToReturn)
    {
        // This GetExceptionForHR call is needed to 'eat' the IErrorInfo put to TLS by
        // any previous exception on this thread. If this isn't done, calls can return
        // previous exception objects that have occurred.
        Marshal.GetExceptionForHR(hresultToReturn);

        Exception e = Marshal.GetExceptionForHR(hresultToReturn);
        throw e;
    }

    [PreserveSig]
    public int Return_As_HResult(int hresultToReturn)
    {
        return hresultToReturn;
    }
}