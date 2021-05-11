// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Serialization;

namespace System.Reflection
{
    [Obsolete(Obsoletions.StrongNameKeyPairMessage, DiagnosticId = Obsoletions.StrongNameKeyPairDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    public class StrongNameKeyPair : IDeserializationCallback, ISerializable
    {
        public StrongNameKeyPair(FileStream keyPairFile) =>
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_StrongNameSigning);

        public StrongNameKeyPair(byte[] keyPairArray) =>
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_StrongNameSigning);

        protected StrongNameKeyPair(SerializationInfo info, StreamingContext context) =>
            throw new PlatformNotSupportedException();

        public StrongNameKeyPair(string keyPairContainer) =>
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_StrongNameSigning);

        public byte[] PublicKey =>
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_StrongNameSigning);

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) =>
            throw new PlatformNotSupportedException();

        void IDeserializationCallback.OnDeserialization(object? sender) =>
            throw new PlatformNotSupportedException();
    }
}
