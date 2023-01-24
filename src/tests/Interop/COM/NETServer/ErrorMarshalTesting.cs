// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    [PreserveSig]
    public Server.Contract.HResult Return_As_HResult_Struct(int hresultToReturn)
    {
        return new Server.Contract.HResult { hr = hresultToReturn };
    }

    public void Throw_HResult_HelpLink(int hresultToReturn, string helpLink, uint helpContext)
    {
        Marshal.GetExceptionForHR(hresultToReturn);

        Exception e = Marshal.GetExceptionForHR(hresultToReturn);
        e.HelpLink = $"{helpLink}#{helpContext}";
        throw e;
    }
}
