// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf8)]
    [Guid(_guid)]
    internal partial interface IStringMarshallingOverrideDerived : IStringMarshallingOverride
    {
        public new const string _guid = "3AFFE3FD-D11E-4195-8250-0C73321977A0";
        string StringMarshallingUtf8_2(string input);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string MarshalAsLPWString_2([MarshalAs(UnmanagedType.LPWStr)] string input);

        [return: MarshalUsing(typeof(Utf16StringMarshaller))]
        string MarshalUsingUtf16_2([MarshalUsing(typeof(Utf16StringMarshaller))] string input);
    }
}
