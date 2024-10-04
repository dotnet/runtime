// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using System.Security.Cryptography;

#pragma warning disable CS3016 // Arrays as attribute arguments are not CLS Compliant
#pragma warning disable SYSLIB1051

namespace Swift
{
    /// <summary>
    /// Represents ChaChaPoly in C#.
    /// </summary>
    internal unsafe partial struct ChaChaPoly
    {
        /// <summary>
        /// Represents Nonce in C#.
        /// </summary>
        internal sealed unsafe partial class Nonce
        {
            private const int _payloadSize = 16;
            internal void* payload;

            internal Nonce()
            {
                payload = Marshal.AllocHGlobal(_payloadSize).ToPointer();
                SwiftIndirectResult swiftIndirectResult = new SwiftIndirectResult(payload);
                CryptoKit.PInvoke_ChaChaPoly_Nonce_Init(swiftIndirectResult);
            }

            internal Nonce(Data data)
            {
                payload = Marshal.AllocHGlobal(_payloadSize).ToPointer();
                SwiftIndirectResult swiftIndirectResult = new SwiftIndirectResult(payload);

                void* metadata = Swift.Runtime.GetMetadata(data);
                void* conformanceDescriptor = IDataProtocol.GetConformanceDescriptor;
                void* witnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, metadata, null);

                CryptoKit.PInvoke_ChaChaPoly_Nonce_Init2(swiftIndirectResult, &data, metadata, witnessTable, out SwiftError error);

                if (error.Value != null)
                {
                    throw new CryptographicException();
                }
            }

            ~Nonce()
            {
                Marshal.FreeHGlobal(new IntPtr(payload));
            }
        }

        /// <summary>
        /// Represents SealedBox in C#.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Size = 16)]
        internal unsafe partial struct SealedBox
        {
            internal Data combined;
            internal readonly Data Ciphertext { get => CryptoKit.PInvoke_ChaChaPoly_SealedBox_GetCiphertext(this); }
            internal readonly Data Tag { get => CryptoKit.PInvoke_ChaChaPoly_SealedBox_GetTag(this); }

            internal SealedBox(ChaChaPoly.Nonce nonce, Data ciphertext, Data tag)
            {
                void* ciphertextMetadata = Swift.Runtime.GetMetadata(ciphertext);
                void* tagMetadata = Swift.Runtime.GetMetadata(tag);
                void* conformanceDescriptor = IDataProtocol.GetConformanceDescriptor;
                void* ciphertextWitnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, ciphertextMetadata, null);
                void* tagWitnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, tagMetadata, null);

                this = CryptoKit.PInvoke_ChaChaPoly_SealedBox_Init(
                    nonce.payload,
                    &ciphertext,
                    &tag,
                    ciphertextMetadata,
                    tagMetadata,
                    ciphertextWitnessTable,
                    tagWitnessTable,
                    out SwiftError error);

                if (error.Value != null)
                {
                    throw new CryptographicException();
                }
            }
        }

        /// <summary>
        /// Encrypts the plaintext using the key, nonce, and authenticated data.
        /// </summary>
        internal static unsafe SealedBox seal<Plaintext, AuthenticateData>(Plaintext plaintext, SymmetricKey key, Nonce nonce, AuthenticateData aad, out SwiftError error) where Plaintext : unmanaged, ISwiftObject where AuthenticateData : unmanaged, ISwiftObject {
            void* plaintextMetadata = Swift.Runtime.GetMetadata(plaintext);
            void* aadMetadata = Swift.Runtime.GetMetadata(aad);
            void* conformanceDescriptor = IDataProtocol.GetConformanceDescriptor;
            void* plaintextWitnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, plaintextMetadata, null);
            void* aadWitnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, aadMetadata, null);

            var sealedBox = CryptoKit.PInvoke_ChaChaPoly_Seal(
                &plaintext,
                key.payload,
                nonce.payload,
                &aad,
                plaintextMetadata,
                aadMetadata,
                plaintextWitnessTable,
                aadWitnessTable,
                out error);

            return sealedBox;
        }

        /// <summary>
        /// Decrypts the sealed box using the key and authenticated data.
        /// </summary>
        internal static unsafe Data open<AuthenticateData>(SealedBox sealedBox, SymmetricKey key, AuthenticateData aad, out SwiftError error) where AuthenticateData : unmanaged, ISwiftObject {
            void* metadata = Swift.Runtime.GetMetadata(aad);
            void* conformanceDescriptor = IDataProtocol.GetConformanceDescriptor;
            void* witnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, metadata, null);

            var data = CryptoKit.PInvoke_ChaChaPoly_Open(
                sealedBox,
                key.payload,
                &aad,
                metadata,
                witnessTable,
                out error);

            return data;
        }
    }

    /// <summary>
    /// Represents AesGcm in C#.
    /// </summary>
    internal unsafe partial struct AesGcm
    {
        /// <summary>
        /// Represents Nonce in C#.
        /// </summary>
        internal sealed unsafe partial class Nonce
        {
            private const int _payloadSize = 16;
            internal void* payload;

            internal Nonce()
            {
                payload = Marshal.AllocHGlobal(_payloadSize).ToPointer();
                SwiftIndirectResult swiftIndirectResult = new SwiftIndirectResult(payload);
                CryptoKit.PInvoke_AesGcm_Nonce_Init(swiftIndirectResult);
            }

            internal Nonce(Data data)
            {
                payload = Marshal.AllocHGlobal(_payloadSize).ToPointer();
                SwiftIndirectResult swiftIndirectResult = new SwiftIndirectResult(payload);

                void* metadata = Swift.Runtime.GetMetadata(data);
                void* conformanceDescriptor = IDataProtocol.GetConformanceDescriptor;
                void* witnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, metadata, null);

                CryptoKit.PInvoke_AesGcm_Nonce_Init2(swiftIndirectResult, &data, metadata, witnessTable, out SwiftError error);

                if (error.Value != null)
                {
                    throw new CryptographicException();
                }
            }

            ~Nonce()
            {
                Marshal.FreeHGlobal(new IntPtr(payload));
            }
        }

        /// <summary>
        /// Represents SealedBox in C#.
        /// </summary>
        internal sealed unsafe partial class SealedBox
        {
            private const int _payloadSize = 24;
            internal void* payload;
            public Data Ciphertext { get => CryptoKit.PInvoke_AesGcm_SealedBox_GetCiphertext(new SwiftSelf(payload)); }
            public Data Tag { get => CryptoKit.PInvoke_AesGcm_SealedBox_GetTag(new SwiftSelf(payload)); }
            internal SealedBox(AesGcm.Nonce nonce, Data ciphertext, Data tag)
            {
                payload = Marshal.AllocHGlobal(_payloadSize).ToPointer();
                SwiftIndirectResult swiftIndirectResult = new SwiftIndirectResult(payload);

                void* ciphertextMetadata = Swift.Runtime.GetMetadata(ciphertext);
                void* tagMetadata = Swift.Runtime.GetMetadata(tag);
                void* conformanceDescriptor = IDataProtocol.GetConformanceDescriptor;
                void* ciphertextWitnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, ciphertextMetadata, null);
                void* tagWitnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, tagMetadata, null);

                CryptoKit.PInvoke_AesGcm_SealedBox_Init(
                    swiftIndirectResult,
                    nonce.payload,
                    &ciphertext,
                    &tag,
                    ciphertextMetadata,
                    tagMetadata,
                    ciphertextWitnessTable,
                    tagWitnessTable,
                    out SwiftError error);

                if (error.Value != null)
                {
                    throw new CryptographicException();
                }
            }

            internal SealedBox()
            {
                payload = Marshal.AllocHGlobal(_payloadSize).ToPointer();
            }

            ~SealedBox()
            {
                Marshal.FreeHGlobal(new IntPtr(payload));
            }
        }

        /// <summary>
        /// Encrypts the plaintext using the key, nonce, and authenticated data.
        /// </summary>
        internal static unsafe SealedBox seal<Plaintext, AuthenticateData>(Plaintext plaintext, SymmetricKey key, Nonce nonce, AuthenticateData aad, out SwiftError error) where Plaintext : unmanaged, ISwiftObject where AuthenticateData : unmanaged, ISwiftObject {
            var sealedBox = new AesGcm.SealedBox();
            SwiftIndirectResult swiftIndirectResult = new SwiftIndirectResult(sealedBox.payload);

            void* plaintextMetadata = Swift.Runtime.GetMetadata(plaintext);
            void* aadMetadata = Swift.Runtime.GetMetadata(aad);
            void* conformanceDescriptor = IDataProtocol.GetConformanceDescriptor;
            void* plaintextWitnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, plaintextMetadata, null);
            void* aadWitnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, aadMetadata, null);

            CryptoKit.PInvoke_AesGcm_Seal(
                swiftIndirectResult,
                &plaintext,
                key.payload,
                nonce.payload,
                &aad,
                plaintextMetadata,
                aadMetadata,
                plaintextWitnessTable,
                aadWitnessTable,
                out error);

            return sealedBox;
        }

        /// <summary>
        /// Decrypts the sealed box using the key and authenticated data.
        /// </summary>
        internal static unsafe Data open<AuthenticateData>(SealedBox sealedBox, SymmetricKey key, AuthenticateData aad, out SwiftError error) where AuthenticateData : unmanaged, ISwiftObject {
            void* metadata = Swift.Runtime.GetMetadata(aad);
            void* conformanceDescriptor = IDataProtocol.GetConformanceDescriptor;
            void* witnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, metadata, null);

            var data = CryptoKit.PInvoke_AesGcm_Open(
                sealedBox.payload,
                key.payload,
                &aad,
                metadata,
                witnessTable,
                out error);

            return data;
        }
    }

    /// <summary>
    /// Represents SymmetricKey in C#.
    /// </summary>
    internal sealed unsafe partial class SymmetricKey
    {
        private const int _payloadSize = 8;
        internal void* payload;

        internal SymmetricKey(SymmetricKeySize symmetricKeySize)
        {
            payload = Marshal.AllocHGlobal(_payloadSize).ToPointer();
            SwiftIndirectResult swiftIndirectResult = new SwiftIndirectResult(payload);
            CryptoKit.PInvoke_SymmetricKey_Init(swiftIndirectResult, &symmetricKeySize);
        }

        internal SymmetricKey(Data data)
        {
            payload = Marshal.AllocHGlobal(_payloadSize).ToPointer();
            SwiftIndirectResult swiftIndirectResult = new SwiftIndirectResult(payload);

            void* metadata = Swift.Runtime.GetMetadata(data);
            void* conformanceDescriptor = IContiguousBytes.GetConformanceDescriptor;
            void* witnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, metadata, null);

            CryptoKit.PInvoke_SymmetricKey_Init2(swiftIndirectResult, &data, metadata, witnessTable);
        }

        ~SymmetricKey()
        {
            Marshal.FreeHGlobal(new IntPtr(payload));
        }
    }

    /// <summary>
    /// Represents SymmetricKeySize in C#.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    internal unsafe partial struct SymmetricKeySize
    {
        internal nint bitCount;

        internal SymmetricKeySize(nint bitCount)
        {
            SymmetricKeySize instance;
            PInvoke_init(new SwiftIndirectResult(&instance), bitCount);
            this = instance;
        }
    }

    /// <summary>
    /// Swift CryptoKit PInvoke methods in C#.
    /// </summary>
    internal static partial class CryptoKit
    {
        internal const string Path = "/System/Library/Frameworks/CryptoKit.framework/CryptoKit";

        [LibraryImport(Path, EntryPoint = "$s9CryptoKit03ChaC4PolyO5NonceVAEycfC")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial void PInvoke_ChaChaPoly_Nonce_Init(SwiftIndirectResult result);

        [LibraryImport(Path, EntryPoint = "$s9CryptoKit03ChaC4PolyO5NonceV4dataAEx_tKc10Foundation12DataProtocolRzlufC")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial void PInvoke_ChaChaPoly_Nonce_Init2(SwiftIndirectResult result, void* data, void* metadata, void* witnessTable, out SwiftError error);

        [LibraryImport(Path, EntryPoint = "$s9CryptoKit03ChaC4PolyO9SealedBoxV10ciphertext10Foundation4DataVvg")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial Data PInvoke_ChaChaPoly_SealedBox_GetCiphertext(ChaChaPoly.SealedBox sealedBox);

        [LibraryImport(Path, EntryPoint = "$s9CryptoKit03ChaC4PolyO9SealedBoxV3tag10Foundation4DataVvg")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial Data PInvoke_ChaChaPoly_SealedBox_GetTag(ChaChaPoly.SealedBox sealedBox);

        [LibraryImport(Path, EntryPoint = "$s9CryptoKit03ChaC4PolyO9SealedBoxV5nonce10ciphertext3tagAeC5NonceV_xq_tKc10Foundation12DataProtocolRzAkLR_r0_lufC")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial ChaChaPoly.SealedBox PInvoke_ChaChaPoly_SealedBox_Init(void* nonce, void* ciphertext, void* tag, void* ciphertextMetadata, void* tagMetadata, void* ciphertextWitnessTable, void* tagWitnessTable, out SwiftError error);

        [LibraryImport(Path, EntryPoint = "$s9CryptoKit3AESO3GCMO5NonceVAGycfC")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial void PInvoke_AesGcm_Nonce_Init(SwiftIndirectResult result);

        [LibraryImport(Path, EntryPoint = "$s9CryptoKit3AESO3GCMO5NonceV4dataAGx_tKc10Foundation12DataProtocolRzlufC")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial void PInvoke_AesGcm_Nonce_Init2(SwiftIndirectResult result, void* data, void* metadata, void* witnessTable, out SwiftError error);

        [LibraryImport(Path, EntryPoint = "$s9CryptoKit3AESO3GCMO9SealedBoxV10ciphertext10Foundation4DataVvg")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial  Data PInvoke_AesGcm_SealedBox_GetCiphertext(SwiftSelf sealedBox);

        [LibraryImport(Path, EntryPoint = "$s9CryptoKit3AESO3GCMO9SealedBoxV3tag10Foundation4DataVvg")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial  Data PInvoke_AesGcm_SealedBox_GetTag(SwiftSelf sealedBox);

        [LibraryImport(Path, EntryPoint = "$s9CryptoKit3AESO3GCMO9SealedBoxV5nonce10ciphertext3tagAgE5NonceV_xq_tKc10Foundation12DataProtocolRzAmNR_r0_lufC")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial void PInvoke_AesGcm_SealedBox_Init(SwiftIndirectResult result, void* nonce, void* ciphertext, void* tag, void* ciphertextMetadata, void* tagMetadata, void* ciphertextWitnessTable, void* tagWitnessTable, out SwiftError error);

        [LibraryImport(Path, EntryPoint = "$s9CryptoKit12SymmetricKeyV4sizeAcA0cD4SizeV_tcfC")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial void PInvoke_SymmetricKey_Init(SwiftIndirectResult result, SymmetricKeySize* symmetricKeySize);

        [LibraryImport(Path, EntryPoint = "$s9CryptoKit12SymmetricKeyV4dataACx_tc10Foundation15ContiguousBytesRzlufC")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial void PInvoke_SymmetricKey_Init2(SwiftIndirectResult result, void* data, void* metadata, void* witnessTable);

        [LibraryImport(Path, EntryPoint = "$s9CryptoKit16SymmetricKeySizeV8bitCountACSi_tcfC")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial void PInvoke_init(SwiftIndirectResult result, nint bitCount);

        [LibraryImport(Path, EntryPoint = "$s9CryptoKit03ChaC4PolyO4seal_5using5nonce14authenticatingAC9SealedBoxVx_AA12SymmetricKeyVAC5NonceVSgq_tK10Foundation12DataProtocolRzAoPR_r0_lFZ")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial ChaChaPoly.SealedBox PInvoke_ChaChaPoly_Seal(void* plaintext, void* key, void* nonce, void* aad, void* plaintextMetadata, void* aadMetadata, void* plaintextWitnessTable, void* aadWitnessTable, out SwiftError error);

        [LibraryImport(Path, EntryPoint = "$s9CryptoKit03ChaC4PolyO4open_5using14authenticating10Foundation4DataVAC9SealedBoxV_AA12SymmetricKeyVxtKAG0I8ProtocolRzlFZ")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial Data PInvoke_ChaChaPoly_Open(ChaChaPoly.SealedBox sealedBox, void* key, void* aad, void* metadata, void* witnessTable, out SwiftError error);

        [LibraryImport(Path, EntryPoint = "$s9CryptoKit3AESO3GCMO4seal_5using5nonce14authenticatingAE9SealedBoxVx_AA12SymmetricKeyVAE5NonceVSgq_tK10Foundation12DataProtocolRzAqRR_r0_lFZ")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial void PInvoke_AesGcm_Seal(SwiftIndirectResult result, void* plaintext, void* key, void* nonce, void* aad, void* plaintextMetadata, void* aadMetadata, void* plaintextWitnessTable, void* aadWitnessTable, out SwiftError error);

        [LibraryImport(Path, EntryPoint = "$s9CryptoKit3AESO3GCMO4open_5using14authenticating10Foundation4DataVAE9SealedBoxV_AA12SymmetricKeyVxtKAI0I8ProtocolRzlFZ")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial Data PInvoke_AesGcm_Open(void* sealedBox, void* key, void* aad, void* metadata, void* witnessTable, out SwiftError error);
    }
}
