// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

internal static unsafe class RuntimeHostTest
{
    internal const int InvalidOperationHResult = unchecked((int)0x80131509);

    [DllImport("coreclr.dll", ExactSpelling = true)]
    private static extern int GetCLRRuntimeHost(Guid* interfaceId, nint* host);

    [DynamicDependency(nameof(ThrowManaged))]
    internal static int ExecuteManagedException()
    {
        Guid interfaceId = new("90F1A06C-7712-4762-86B5-7A5EBA6BDB02");
        nint host = 0;
        int hr = GetCLRRuntimeHost(&interfaceId, &host);
        if (hr != 0 || host == 0)
        {
            throw new InvalidOperationException($"GetCLRRuntimeHost failed: {hr:X8}");
        }

        nint* vtable = *(nint**)host;
        try
        {
            string path = typeof(RuntimeHostTest).Assembly.Location;
            uint result = 0;
            fixed (char* pathPointer = path)
            fixed (char* typeName = nameof(RuntimeHostTest))
            fixed (char* methodName = nameof(ThrowManaged))
            {
                var execute = (delegate* unmanaged[Stdcall]<nint, char*, char*, char*, char*, uint*, int>)vtable[11];
                return execute(host, pathPointer, typeName, methodName, null, &result);
            }
        }
        finally
        {
            ((delegate* unmanaged[Stdcall]<nint, uint>)vtable[2])(host);
        }
    }

    public static int ThrowManaged(string argument) => throw new InvalidOperationException();
}

internal static class ErrorInfoTest
{
    [DllImport("oleaut32.dll", ExactSpelling = true)]
    private static extern int CreateErrorInfo(out nint errorInfo);

    [DllImport("oleaut32.dll", ExactSpelling = true)]
    private static extern int SetErrorInfo(uint reserved, nint errorInfo);

    internal static int SetStaleErrorInfo(out nint errorInfo)
    {
        errorInfo = 0;
        int hr = CreateErrorInfo(out nint creator);
        if (hr != 0)
        {
            return hr;
        }

        bool success = false;
        try
        {
            Guid iid = new("1CF2B120-547D-101B-8E65-08002B2BD119");
            hr = Marshal.QueryInterface(creator, in iid, out errorInfo);
            if (hr == 0)
            {
                hr = SetErrorInfo(0, errorInfo);
                success = hr == 0;
            }

            return hr;
        }
        finally
        {
            if (!success && errorInfo != 0)
            {
                Marshal.Release(errorInfo);
                errorInfo = 0;
            }

            Marshal.Release(creator);
        }
    }
}
