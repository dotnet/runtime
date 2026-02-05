// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
#if DESIGNTIMEINTERFACES
    internal interface IImportExportShape<TSelf> where TSelf : class, IImportExportShape<TSelf>
    {
        static abstract TSelf ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source);
        static abstract TSelf ImportPkcs8PrivateKey(ReadOnlySpan<byte> source);
        static abstract TSelf ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source);
        static abstract TSelf ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source);

        static abstract TSelf ImportFromPem(ReadOnlySpan<char> source);
        static abstract TSelf ImportFromEncryptedPem(ReadOnlySpan<char> source, ReadOnlySpan<char> password);
        static abstract TSelf ImportFromEncryptedPem(ReadOnlySpan<char> source, ReadOnlySpan<byte> passwordBytes);

        byte[] ExportSubjectPublicKeyInfo();
        bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten);
        string ExportSubjectPublicKeyInfoPem();

        byte[] ExportPkcs8PrivateKey();
        bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten);
        string ExportPkcs8PrivateKeyPem();

        byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters);
        bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten);
        string ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<char> password, PbeParameters pbeParameters);

        byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters);
        bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten);
        string ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters);
    }
#endif
}
