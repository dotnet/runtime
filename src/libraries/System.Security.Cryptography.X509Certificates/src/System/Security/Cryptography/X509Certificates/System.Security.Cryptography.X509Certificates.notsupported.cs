// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Security.Cryptography.X509Certificates
{
    public partial class X509Certificate : System.IDisposable, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
    {
        void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object? sender) { throw new System.PlatformNotSupportedException(System.SR.Cryptography_PlatformNotSupported); }
    }
}