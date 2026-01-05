// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.Cryptography;
using CryptProtectDataFlags = Interop.Crypt32.CryptProtectDataFlags;
using DATA_BLOB = Interop.Crypt32.DATA_BLOB;

namespace System.Security.Cryptography
{
    public static partial class ProtectedData
    {
        private static readonly byte[] s_nonEmpty = new byte[1];

        public static byte[] Protect(byte[] userData, byte[]? optionalEntropy, DataProtectionScope scope)
        {
            CheckPlatformSupport();

            ArgumentNullException.ThrowIfNull(userData);

            byte[]? outputData;
            bool result = TryProtectOrUnprotect(
                userData,
                optionalEntropy,
                scope,
                protect: true,
                allocateArray: true,
                bytesWritten: out _,
                outputData: out outputData);
            Debug.Assert(result);
            Debug.Assert(outputData != null);
            return outputData;
        }

#if NET
        /// <summary>
        /// Encrypts the data in a specified byte span and returns a byte array that contains the encrypted data.
        /// </summary>
        /// <param name="userData">A buffer that contains the data to encrypt.</param>
        /// <param name="scope">One of the enumeration values that specifies the scope of encryption.</param>
        /// <param name="optionalEntropy">
        /// An optional additional byte span used to increase the complexity of the encryption,
        /// or empty for no additional complexity.
        /// </param>
        /// <returns>A byte array representing the encrypted data.</returns>
        /// <exception cref="CryptographicException">The encryption failed.</exception>
        /// <exception cref="NotSupportedException">The operating system does not support this method.</exception>
        /// <exception cref="OutOfMemoryException">The system ran out of memory while encrypting the data.</exception>
        /// <exception cref="PlatformNotSupportedException">
        /// Calls to the Protect method are supported on Windows operating systems only.
        /// </exception>
        public static byte[] Protect(
            ReadOnlySpan<byte> userData,
            DataProtectionScope scope,
            ReadOnlySpan<byte> optionalEntropy = default)
        {
            CheckPlatformSupport();

            byte[]? outputData;
            bool result = TryProtectOrUnprotect(
                userData,
                optionalEntropy,
                scope,
                protect: true,
                allocateArray: true,
                bytesWritten: out _,
                outputData: out outputData);
            Debug.Assert(result);
            Debug.Assert(outputData != null);
            return outputData;
        }

        /// <summary>
        /// Encrypts the data in a specified buffer and writes the encrypted data to a destination buffer.
        /// </summary>
        /// <param name="userData">A buffer that contains data to encrypt.</param>
        /// <param name="scope">One of the enumeration values that specifies the scope of encryption.</param>
        /// <param name="destination">The buffer to receive the encrypted data.</param>
        /// <param name="bytesWritten">
        /// When this method returns, contains the number of bytes
        /// written to <paramref name="destination"/>.
        /// </param>
        /// <param name="optionalEntropy">
        /// An optional additional buffer used to increase the complexity of the encryption,
        /// or empty for no additional complexity.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="destination"/> was large enough to receive the decrypted data;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The buffer in <paramref name="destination"/> is too small to hold the encrypted data.
        /// </exception>
        /// <exception cref="CryptographicException">The encryption failed.</exception>
        /// <exception cref="NotSupportedException">The operating system does not support this method.</exception>
        /// <exception cref="OutOfMemoryException">The system ran out of memory while encrypting the data.</exception>
        /// <exception cref="PlatformNotSupportedException">
        /// Calls to the Protect method are supported on Windows operating systems only.
        /// </exception>
        public static bool TryProtect(
            ReadOnlySpan<byte> userData,
            DataProtectionScope scope,
            Span<byte> destination,
            out int bytesWritten,
            ReadOnlySpan<byte> optionalEntropy = default)
        {
            CheckPlatformSupport();

            return TryProtectOrUnprotect(
                userData,
                optionalEntropy,
                scope,
                protect: true,
                allocateArray: false,
                outputSpan: destination,
                bytesWritten: out bytesWritten,
                outputData: out _);
        }

        /// <summary>
        /// Encrypts the data in a specified buffer and writes the encrypted data to a destination buffer.
        /// </summary>
        /// <param name="userData">A buffer that contains data to encrypt.</param>
        /// <param name="scope">One of the enumeration values that specifies the scope of encryption.</param>
        /// <param name="destination">The buffer to receive the encrypted data.</param>
        /// <param name="optionalEntropy">
        /// An optional additional buffer used to increase the complexity of the encryption,
        /// or empty for no additional complexity.
        /// </param>
        /// <returns>The total number of bytes written to <paramref name="destination"/></returns>
        /// <exception cref="ArgumentException">
        /// The buffer in <paramref name="destination"/> is too small to hold the encrypted data.
        /// </exception>
        /// <exception cref="CryptographicException">The encryption failed.</exception>
        /// <exception cref="NotSupportedException">The operating system does not support this method.</exception>
        /// <exception cref="OutOfMemoryException">The system ran out of memory while encrypting the data.</exception>
        /// <exception cref="PlatformNotSupportedException">
        /// Calls to the Protect method are supported on Windows operating systems only.
        /// </exception>
        public static int Protect(
            ReadOnlySpan<byte> userData,
            DataProtectionScope scope,
            Span<byte> destination,
            ReadOnlySpan<byte> optionalEntropy = default)
        {
            CheckPlatformSupport();

            int bytesWritten;
            if (!TryProtectOrUnprotect(
                    userData,
                    optionalEntropy,
                    scope,
                    protect: true,
                    allocateArray: false,
                    outputSpan: destination,
                    bytesWritten: out bytesWritten,
                    outputData: out _))
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            return bytesWritten;
        }
#endif

        public static byte[] Unprotect(byte[] encryptedData, byte[]? optionalEntropy, DataProtectionScope scope)
        {
            CheckPlatformSupport();

            ArgumentNullException.ThrowIfNull(encryptedData);

            byte[]? outputData;
            bool result = TryProtectOrUnprotect(
                encryptedData,
                optionalEntropy,
                scope,
                protect: false,
                allocateArray: true,
                bytesWritten: out _,
                outputData: out outputData);

            Debug.Assert(result);
            Debug.Assert(outputData != null);
            return outputData;
        }

#if NET
        /// <summary>
        /// Decrypts the data in a specified byte array and returns a byte array that contains the decrypted data.
        /// </summary>
        /// <param name="encryptedData">A buffer that contains data to decrypt.</param>
        /// <param name="scope">One of the enumeration values that specifies the scope of encryption.</param>
        /// <param name="optionalEntropy">
        /// An optional additional buffer used to increase the complexity of the encryption,
        /// or empty for no additional complexity.
        /// </param>
        /// <returns>A byte array representing the encrypted data.</returns>
        /// <exception cref="CryptographicException">The encryption failed.</exception>
        /// <exception cref="NotSupportedException">The operating system does not support this method.</exception>
        /// <exception cref="OutOfMemoryException">The system ran out of memory while decrypting the data.</exception>
        /// <exception cref="PlatformNotSupportedException">
        /// Calls to the Unprotect method are supported on Windows operating systems only.
        /// </exception>
        public static byte[] Unprotect(
            ReadOnlySpan<byte> encryptedData,
            DataProtectionScope scope,
            ReadOnlySpan<byte> optionalEntropy = default)
        {
            CheckPlatformSupport();

            byte[]? outputData;
            bool result = TryProtectOrUnprotect(
                encryptedData,
                optionalEntropy,
                scope,
                protect: false,
                allocateArray: true,
                bytesWritten: out _,
                outputData: out outputData);

            Debug.Assert(result);
            Debug.Assert(outputData != null);
            return outputData;
        }

        /// <summary>
        /// Decrypts the data in a specified buffer and writes the decrypted data to a destination buffer.
        /// </summary>
        /// <param name="encryptedData">A buffer that contains data to decrypt.</param>
        /// <param name="scope">One of the enumeration values that specifies the scope of encryption.</param>
        /// <param name="destination">The buffer to receive the decrypted data.</param>
        /// <param name="bytesWritten">
        /// When this method returns, contains the number of bytes
        /// written to <paramref name="destination"/>.
        /// </param>
        /// <param name="optionalEntropy">
        /// An optional additional buffer used to increase the complexity of the encryption,
        /// or empty for no additional complexity.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="destination"/> was large enough to receive the decrypted data;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The buffer in <paramref name="destination"/> is too small to hold the decrypted data.
        /// </exception>
        /// <exception cref="CryptographicException">The encryption failed.</exception>
        /// <exception cref="NotSupportedException">The operating system does not support this method.</exception>
        /// <exception cref="OutOfMemoryException">The system ran out of memory while encrypting the data.</exception>
        /// <exception cref="PlatformNotSupportedException">
        /// Calls to the Unprotect method are supported on Windows operating systems only.
        /// </exception>
        public static bool TryUnprotect(
            ReadOnlySpan<byte> encryptedData,
            DataProtectionScope scope,
            Span<byte> destination,
            out int bytesWritten,
            ReadOnlySpan<byte> optionalEntropy = default)
        {
            CheckPlatformSupport();

            return TryProtectOrUnprotect(
                encryptedData,
                optionalEntropy,
                scope,
                protect: false,
                allocateArray: false,
                outputSpan: destination,
                bytesWritten: out bytesWritten,
                outputData: out _);
        }

        /// <summary>
        /// Decrypts the data in a specified buffer and writes the decrypted data to a destination buffer.
        /// </summary>
        /// <param name="encryptedData">A buffer that contains data to decrypt.</param>
        /// <param name="scope">One of the enumeration values that specifies the scope of encryption.</param>
        /// <param name="destination">The buffer to receive the decrypted data.</param>
        /// <param name="optionalEntropy">
        /// An optional additional buffer used to increase the complexity of the encryption,
        /// or empty for no additional complexity.
        /// </param>
        /// <returns>The total number of bytes written to <paramref name="destination"/></returns>
        /// <exception cref="ArgumentException">
        /// The buffer in <paramref name="destination"/> is too small to hold the decrypted data.
        /// </exception>
        /// <exception cref="CryptographicException">The encryption failed.</exception>
        /// <exception cref="NotSupportedException">The operating system does not support this method.</exception>
        /// <exception cref="OutOfMemoryException">The system ran out of memory while encrypting the data.</exception>
        /// <exception cref="PlatformNotSupportedException">
        /// Calls to the Unprotect method are supported on Windows operating systems only.
        /// </exception>
        public static int Unprotect(
            ReadOnlySpan<byte> encryptedData,
            DataProtectionScope scope,
            Span<byte> destination,
            ReadOnlySpan<byte> optionalEntropy = default)
        {
            CheckPlatformSupport();

            int bytesWritten;
            if (!TryProtectOrUnprotect(
                    encryptedData,
                    optionalEntropy,
                    scope,
                    protect: false,
                    allocateArray: false,
                    outputSpan: destination,
                    bytesWritten: out bytesWritten,
                    outputData: out _))
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            return bytesWritten;
        }
#endif

        private static bool TryProtectOrUnprotect(
            ReadOnlySpan<byte> inputData,
            ReadOnlySpan<byte> optionalEntropy,
            DataProtectionScope scope,
            bool protect,
            out int bytesWritten,
            out byte[]? outputData,
            bool allocateArray,
            Span<byte> outputSpan = default)
        {
            unsafe
            {
                // The Win32 API will reject pbData == nullptr, and the fixed statement
                // maps empty arrays to nullptr... so when the input is empty use the address of a
                // different array, but still assign cbData to 0.
                ReadOnlySpan<byte> relevantData = inputData.IsEmpty ? s_nonEmpty : inputData;

                fixed (byte* pInputData = relevantData, pOptionalEntropy = optionalEntropy)
                {
                    DATA_BLOB userDataBlob = new DATA_BLOB((IntPtr)pInputData, (uint)(inputData.Length));
                    DATA_BLOB optionalEntropyBlob = default(DATA_BLOB);
                    if (!optionalEntropy.IsEmpty)
                    {
                        optionalEntropyBlob = new DATA_BLOB((IntPtr)pOptionalEntropy, (uint)(optionalEntropy.Length));
                    }

                    // For .NET Framework compat, we ignore unknown bits in the "scope" value rather than throwing.
                    CryptProtectDataFlags flags = CryptProtectDataFlags.CRYPTPROTECT_UI_FORBIDDEN;
                    if (scope == DataProtectionScope.LocalMachine)
                    {
                        flags |= CryptProtectDataFlags.CRYPTPROTECT_LOCAL_MACHINE;
                    }

                    DATA_BLOB outputBlob = default(DATA_BLOB);
                    Span<byte> interopSpan = default;
                    try
                    {
                        bool success = protect ?
                            Interop.Crypt32.CryptProtectData(in userDataBlob, null, ref optionalEntropyBlob, IntPtr.Zero, IntPtr.Zero, flags, out outputBlob) :
                            Interop.Crypt32.CryptUnprotectData(in userDataBlob, IntPtr.Zero, ref optionalEntropyBlob, IntPtr.Zero, IntPtr.Zero, flags, out outputBlob);
                        if (!success)
                        {
#if NET
                            int lastWin32Error = Marshal.GetLastPInvokeError();
#else
                            int lastWin32Error = Marshal.GetLastWin32Error();
#endif
                            if (protect && ErrorMayBeCausedByUnloadedProfile(lastWin32Error))
                                throw new CryptographicException(SR.Cryptography_DpApi_ProfileMayNotBeLoaded);
                            else
                                throw lastWin32Error.ToCryptographicException();
                        }


                        // In some cases, the API would fail due to OOM but simply return a null pointer.
                        if (outputBlob.pbData == IntPtr.Zero)
                            throw new OutOfMemoryException();

                        int length = (int)(outputBlob.cbData);
                        interopSpan = new Span<byte>(outputBlob.pbData.ToPointer(), length);

                        if (allocateArray)
                        {
                            outputData = interopSpan.ToArray();
                            bytesWritten = length;
                            return true;
                        }

                        if (outputBlob.cbData > outputSpan.Length)
                        {
                            bytesWritten = 0;
                            outputData = null;
                            return false;
                        }

                        interopSpan.CopyTo(outputSpan);
                        bytesWritten = length;
                        outputData = null;
                        return true;
                    }
                    finally
                    {
                        if (outputBlob.pbData != IntPtr.Zero)
                        {
                            interopSpan.Clear();
                            Marshal.FreeHGlobal(outputBlob.pbData);
                        }
                    }
                }
            }
        }

        // Determine if an error code may have been caused by trying to do a crypto operation while the
        // current user's profile is not yet loaded.
        private static bool ErrorMayBeCausedByUnloadedProfile(int errorCode)
        {
            // CAPI returns a file not found error if the user profile is not yet loaded
            return errorCode == HResults.E_FILENOTFOUND ||
                   errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND;
        }

        private static void CheckPlatformSupport()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException();
            }
        }
    }
}
