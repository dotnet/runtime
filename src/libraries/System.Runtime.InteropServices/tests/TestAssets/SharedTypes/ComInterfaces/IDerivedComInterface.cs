// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid(_guid)]
    internal partial interface IDerived : IGetAndSetInt
    {
        void SetName([MarshalUsing(typeof(Utf16StringMarshaller))] string name);

        [return:MarshalUsing(typeof(Utf16StringMarshaller))]
        string GetName();

        internal new const string _guid = "7F0DB364-3C04-4487-9193-4BB05DC7B654";
    }
}
