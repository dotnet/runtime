// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface(StringMarshalling = System.Runtime.InteropServices.StringMarshalling.Utf8)]
    [Guid(IID)]
    internal partial interface IRefStrings
    {
        public const string IID = "5146B7DB-0588-469B-B8E5-B38090A2FC15";
        void RefString(ref string value);
        void InString(in string value);
        void OutString(out string value);
    }
}
