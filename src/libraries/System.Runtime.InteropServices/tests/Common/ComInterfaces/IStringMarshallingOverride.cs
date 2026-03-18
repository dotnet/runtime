// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface(StringMarshalling = System.Runtime.InteropServices.StringMarshalling.Utf8)]
    [Guid(IID)]
    internal partial interface IStringMarshallingOverride
    {
        public const string IID = "5146B7DB-0588-469B-B8E5-B38090A2FC15";
        string StringMarshallingUtf8(string input);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string MarshalAsLPWString([MarshalAs(UnmanagedType.LPWStr)] string input);

        [return: MarshalUsing(typeof(Utf16StringMarshaller))]
        string MarshalUsingUtf16([MarshalUsing(typeof(Utf16StringMarshaller))] string input);
    }
}
