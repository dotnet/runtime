// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [Guid(_guid)]
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(Utf16StringMarshaller))]
    internal partial interface ICustomStringMarshallingUtf16
    {
        public string GetString();

        public void SetString(string value);

        public const string _guid = "E11D5F3E-DD57-41A6-A59E-7D110551A760";
    }
}
