// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [Guid(_guid)]
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf8)]
    internal partial interface IUTF8Marshalling
    {
        public string GetString();

        public void SetString(string value);

        public const string _guid = "E11D5F3E-DD57-41A6-A59E-7D110551A760";
    }

    [Guid(_guid)]
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
    internal partial interface IUTF16MarshallingFromIUTF8 : IUTF8Marshalling
    {
        public string GetString2();

        public void SetString2(string value);

        public new  const string _guid = "861A0AF1-067D-48F0-8592-F7F48EB88095";
    }

    [Guid(_guid)]
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
    internal partial interface IUTF16Marshalling2levels : IUTF16MarshallingFromIUTF8
    {
        public string GetString3();

        public void SetString3(string value);

        public new const string _guid = "861A0AF1-067D-48F0-8592-F7F48EB88095";
    }
}
