// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security
{
    internal static class SslStreamPal
    {
        private static readonly bool UseNewCryptoApi =
            // On newer Windows version we use new API to get TLS1.3.
            // API is supported since Windows 10 1809 (17763) but there is no reason to use at the moment.
            Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 18836;

        private const string SecurityPackage = "Microsoft Unified Security Protocol Provider";

        private const Interop.SspiCli.ContextFlags RequiredFlags =
            Interop.SspiCli.ContextFlags.ReplayDetect |
            Interop.SspiCli.ContextFlags.SequenceDetect |
            Interop.SspiCli.ContextFlags.Confidentiality |
            Interop.SspiCli.ContextFlags.AllocateMemory;

        private const Interop.SspiCli.ContextFlags ServerRequiredFlags =
            RequiredFlags | Interop.SspiCli.ContextFlags.AcceptStream | Interop.SspiCli.ContextFlags.AcceptExtendedError;

        public static Exception GetException(SecurityStatusPal status)
        {
            int win32Code = (int)SecurityStatusAdapterPal.GetInteropFromSecurityStatusPal(status);
            return new Win32Exception(win32Code);
        }

        internal const bool StartMutualAuthAsAnonymous = true;
        internal const bool CanEncryptEmptyMessage = true;

        public static void VerifyPackageInfo()
        {
            SSPIWrapper.GetVerifyPackageInfo(GlobalSSPI.SSPISecureChannel, SecurityPackage, true);
        }

        public static byte[] ConvertAlpnProtocolListToByteArray(List<SslApplicationProtocol> protocols)
        {
            return Interop.Sec_Application_Protocols.ToByteArray(protocols);
        }

        public static SecurityStatusPal AcceptSecurityContext(ref SafeFreeCredentials? credentialsHandle, ref SafeDeleteSslContext? context, ReadOnlySpan<byte> inputBuffer, ref byte[]? outputBuffer, SslAuthenticationOptions sslAuthenticationOptions)
        {
            Interop.SspiCli.ContextFlags unusedAttributes = default;

            InputSecurityBuffers inputBuffers = default;
            inputBuffers.SetNextBuffer(new InputSecurityBuffer(inputBuffer, SecurityBufferType.SECBUFFER_TOKEN));
            inputBuffers.SetNextBuffer(new InputSecurityBuffer(default, SecurityBufferType.SECBUFFER_EMPTY));

            if (sslAuthenticationOptions.ApplicationProtocols != null && sslAuthenticationOptions.ApplicationProtocols.Count != 0)
            {
                byte[] alpnBytes = ConvertAlpnProtocolListToByteArray(sslAuthenticationOptions.ApplicationProtocols);
                inputBuffers.SetNextBuffer(new InputSecurityBuffer(new ReadOnlySpan<byte>(alpnBytes), SecurityBufferType.SECBUFFER_APPLICATION_PROTOCOLS));
            }

            var resultBuffer = new SecurityBuffer(outputBuffer, SecurityBufferType.SECBUFFER_TOKEN);

            int errorCode = SSPIWrapper.AcceptSecurityContext(
                GlobalSSPI.SSPISecureChannel,
                credentialsHandle,
                ref context,
                ServerRequiredFlags | (sslAuthenticationOptions.RemoteCertRequired ? Interop.SspiCli.ContextFlags.MutualAuth : Interop.SspiCli.ContextFlags.Zero),
                Interop.SspiCli.Endianness.SECURITY_NATIVE_DREP,
                inputBuffers,
                ref resultBuffer,
                ref unusedAttributes);

            outputBuffer = resultBuffer.token;
            return SecurityStatusAdapterPal.GetSecurityStatusPalFromNativeInt(errorCode);
        }

        public static SecurityStatusPal InitializeSecurityContext(ref SafeFreeCredentials? credentialsHandle, ref SafeDeleteSslContext? context, string? targetName, ReadOnlySpan<byte> inputBuffer, ref byte[]? outputBuffer, SslAuthenticationOptions sslAuthenticationOptions)
        {
            Interop.SspiCli.ContextFlags unusedAttributes = default;

            InputSecurityBuffers inputBuffers = default;
            inputBuffers.SetNextBuffer(new InputSecurityBuffer(inputBuffer, SecurityBufferType.SECBUFFER_TOKEN));
            inputBuffers.SetNextBuffer(new InputSecurityBuffer(default, SecurityBufferType.SECBUFFER_EMPTY));
            if (sslAuthenticationOptions.ApplicationProtocols != null && sslAuthenticationOptions.ApplicationProtocols.Count != 0)
            {
                byte[] alpnBytes = ConvertAlpnProtocolListToByteArray(sslAuthenticationOptions.ApplicationProtocols);
                inputBuffers.SetNextBuffer(new InputSecurityBuffer(new ReadOnlySpan<byte>(alpnBytes), SecurityBufferType.SECBUFFER_APPLICATION_PROTOCOLS));
            }

            var resultBuffer = new SecurityBuffer(outputBuffer, SecurityBufferType.SECBUFFER_TOKEN);

            int errorCode = SSPIWrapper.InitializeSecurityContext(
                            GlobalSSPI.SSPISecureChannel,
                            ref credentialsHandle,
                            ref context,
                            targetName,
                            RequiredFlags | Interop.SspiCli.ContextFlags.InitManualCredValidation,
                            Interop.SspiCli.Endianness.SECURITY_NATIVE_DREP,
                            inputBuffers,
                            ref resultBuffer,
                            ref unusedAttributes);

            outputBuffer = resultBuffer.token;
            return SecurityStatusAdapterPal.GetSecurityStatusPalFromNativeInt(errorCode);
        }

        public static SecurityStatusPal Renegotiate(ref SafeFreeCredentials? credentialsHandle, ref SafeDeleteSslContext? context, SslAuthenticationOptions sslAuthenticationOptions, out byte[]? outputBuffer )
        {
            byte[]? output = Array.Empty<byte>();
            SecurityStatusPal status =  AcceptSecurityContext(ref credentialsHandle, ref context, Span<byte>.Empty, ref output, sslAuthenticationOptions);
            outputBuffer = output;
            return status;
        }

        public static SafeFreeCredentials AcquireCredentialsHandle(SslStreamCertificateContext? certificateContext, SslProtocols protocols, EncryptionPolicy policy, bool isServer)
        {
            Console.WriteLine("AcquireCredentialsHandle called for {0} {1}", UseNewCryptoApi, isServer);
            // New crypto API supports TLS1.3 but it does not allow to force NULL encryption.
            return true || !UseNewCryptoApi || policy == EncryptionPolicy.NoEncryption ?
                        AcquireCredentialsHandleSchannelCred(certificateContext?.Certificate, protocols, policy, isServer) :
                        AcquireCredentialsHandleSchCredentials(certificateContext?.Certificate, protocols, policy, isServer);
        }

        // This is legacy crypto API used on .NET Framework and older Windows versions.
        // It only supports TLS up to 1.2
        public static unsafe SafeFreeCredentials AcquireCredentialsHandleSchannelCred(X509Certificate? certificate, SslProtocols protocols, EncryptionPolicy policy, bool isServer)
        {
            int protocolFlags = GetProtocolFlagsFromSslProtocols(protocols, isServer);
            Interop.SspiCli.SCHANNEL_CRED.Flags flags;
            Interop.SspiCli.CredentialUse direction;

            Console.WriteLine("AcquireCredentialsHandleSchannelCred  called!!!!");

            IntPtr storeHandle = (IntPtr)0;
            //string storeName = "Enterprise Trust";
            string storeName = "TestSignRoot";

            string storeName2 = "TestFurt2";

            //X509Store store = new X509Store("Enterprise Trust", StoreLocation.);
            fixed (char* name2 = storeName2)
            {
                SafeCertStoreHandle certStore = Interop.Crypt32.CertOpenStore(
                    (IntPtr)Internal.Cryptography.Pal.Native.CertStoreProvider.CERT_STORE_PROV_SYSTEM_W,
                    (uint)Internal.Cryptography.Pal.Native.CertEncodingType.All,
                    IntPtr.Zero,
                    (uint)(Internal.Cryptography.Pal.Native.CertStoreFlags.CERT_STORE_CREATE_NEW_FLAG | Internal.Cryptography.Pal.Native.CertStoreFlags.CERT_SYSTEM_STORE_LOCAL_MACHINE),
                    storeName2);
                Console.WriteLine("Got handle {0} inavled? {1} error {2}", certStore, certStore.IsInvalid, Marshal.GetLastWin32Error());
            }



            X509Store store = new X509Store(storeName, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            storeHandle = store.StoreHandle;
            Console.WriteLine("Enterprise Trust opened {0} {1} ({2}) count={3}", store, store.Name, storeHandle, store.Certificates.Count);
            foreach (var c in store.Certificates)
            {
                Console.WriteLine("Got {0}", c.Subject);
            }


            if (!isServer)
            {
                direction = Interop.SspiCli.CredentialUse.SECPKG_CRED_OUTBOUND;
                flags =
                    Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_CRED_MANUAL_CRED_VALIDATION |
                    Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_CRED_NO_DEFAULT_CREDS |
                    Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_SEND_AUX_RECORD;

                // Always opt-in SCH_USE_STRONG_CRYPTO for TLS.
                if (((protocolFlags == 0) ||
                        (protocolFlags & ~(Interop.SChannel.SP_PROT_SSL2 | Interop.SChannel.SP_PROT_SSL3)) != 0)
                     && (policy != EncryptionPolicy.AllowNoEncryption) && (policy != EncryptionPolicy.NoEncryption))
                {
                    flags |= Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_USE_STRONG_CRYPTO;
                }
            }
            else
            {
                direction = Interop.SspiCli.CredentialUse.SECPKG_CRED_INBOUND;
                flags = Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_SEND_AUX_RECORD;
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info($"flags=({flags}), ProtocolFlags=({protocolFlags}), EncryptionPolicy={policy}");
            Interop.SspiCli.SCHANNEL_CRED secureCredential = CreateSecureCredential(
                flags,
                protocolFlags,
                policy);

            Interop.Crypt32.CERT_CONTEXT* certificateHandle = null;
            if (certificate != null)
            {
                secureCredential.cCreds = 1;
                certificateHandle = (Interop.Crypt32.CERT_CONTEXT*)certificate.Handle;
                secureCredential.paCred = &certificateHandle;
            }
           // secureCredential.hRootStore = storeHandle;
            //Console.WriteLine("ALL API with {0}", secureCredential.hRootStore);


            SafeFreeCredentials cred =  AcquireCredentialsHandle(direction, &secureCredential);
            //Console.WriteLine("Got creed = {0} {1}", cred, cred.DangerousGetHandle());
            unsafe
            {
                if (isServer && cred != null)
                {
                    var guid = Guid.NewGuid().ToByteArray();

                    int ssize = sizeof(Interop.SspiCli.SecPkgCred_ClientCertPolicy2);
                    Span<byte> buffer = stackalloc byte[sizeof(Interop.SspiCli.SecPkgCred_ClientCertPolicy2) + ((storeName.Length + 1) * 2)];
                    buffer.Clear();
                    Encoding.Unicode.GetBytes(storeName, buffer.Slice(ssize));


                    Span<Interop.SspiCli.SecPkgCred_ClientCertPolicy2> policy_s = MemoryMarshal.Cast<byte, Interop.SspiCli.SecPkgCred_ClientCertPolicy2>(buffer);

                    byte[] bytes = Encoding.Unicode.GetBytes(storeName);
                    bytes.CopyTo(buffer.Slice(ssize));



                    //   guid.CopyTo(buffer.Slice(4, 16));
                    Console.WriteLine("Buffer is {0} and struct is {1} giud len = {2}", buffer.Length, ssize, guid.Length);
                    //policy_s[0].pwszSslCtlStoreName =


                    byte[] bytes2 = new byte[bytes.Length + 2];

                    Span<byte> name = new Span<byte>(bytes2);
                    name.Clear();
                    bytes.CopyTo(name);

                    //Console.WriteLine("Nove jemno je {0} old name is {1} ({2})", name.Length, storeName.Length, storeName);
                    //Span<Interop.SspiCli.SecPkgCred_ClientCertPolicy> boo2 = stackalloc Interop.SspiCli.SecPkgCred_ClientCertPolicy[1];
                    Interop.SspiCli.SecPkgCred_ClientCertPolicy clientCertPolicy = default;
                    clientCertPolicy.dwFlags = 0;
                    clientCertPolicy.dwCertFlags = 0;
                    clientCertPolicy.fCheckRevocationFreshnessTime = false;
                    clientCertPolicy.fOmitUsageCheck = false;
                    clientCertPolicy.pwszSslCtlIdentifier = null;
                    //clientCertPolicy.pwszSslCtlIdentifier = IntPtr.Zero;
                    //clientCertPolicy.pwszSslCtlStoreName = "Test Roots";
                    //clientCertPolicy.pwszSslCtlStoreName = null;
                    clientCertPolicy.pwszSslCtlStoreName = storeName;
                    clientCertPolicy.dwRevocationFreshnessTime = 0;
                    clientCertPolicy.dwUrlRetrievalTimeout = 0;
               //     clientCertPolicy.guid[0] = 11;
               //     clientCertPolicy.guid[1] = 22;
               //     clientCertPolicy.guid[5] = 66;
               //     clientCertPolicy.guid[10] = 11;

                    Interop.SspiCli.CredHandle credentialHandle = cred._handle;
                    //Console.WriteLine(" cred is {0} {1} {2}", cred._handle, cred, Marshal.SizeOf(clientCertPolicy));
                    //Interop.SspiCli.CredHandle credentialHandle = cred._handle;
                    Interop.SECURITY_STATUS status = Interop.SECURITY_STATUS.OK;
                    unsafe
                    {
                        fixed (void* ptr1 = buffer)
                        {
                            //        var s1 = Interop.SspiCli.QueryCredentialsAttributesW(ref credentialHandle, (long)Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CLIENT_CERT_POLICY, ptr1);
                            //       Span<Interop.SspiCli.SecPkgCred_ClientCertPolicy2> attr = MemoryMarshal.Cast<byte, Interop.SspiCli.SecPkgCred_ClientCertPolicy2>(buffer);
                            //        Console.WriteLine("query done with {0}", s1);
                        }
                    }
                    unsafe
                    {
                        fixed (char* name2 = storeName)
                        {
                            fixed (void* ptr3 = buffer)
                            {
                                policy_s[0].pwszSslCtlStoreName = (System.IntPtr)name2;
                                //policy_s[0].pwszSslCtlStoreName = (IntPtr)ptr3 + ssize;
                                //    fixed (void* ptr2 = &credentialHandle)
                                {
                                    //Console.WriteLine("client policy is et {0} pwszSslCtlStoreName={1} ", (IntPtr)ptr3, policy_s[0].pwszSslCtlStoreName);
                                    //fixed (vo)
                                    fixed (void* ptr = name)
                                    {
                                        //  clientCertPolicy.pwszSslCtlStoreName = ptr;
                                        //clientCertPolicy.pwszSslCtlStoreName = (IntPtr)ptr;
                                        // status = Interop.SspiCli.SetCredentialsAttributesW(ref credentialHandle, (long)Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CLIENT_CERT_POLICY, ref clientCertPolicy, 56); // sizeof(Interop.SspiCli.SecPkgCred_ClientCertPolicy));
                                        //  Console.WriteLine("Nove volanio {0}", status);
                                        //status = Interop.SspiCli.SetCredentialsAttributesW(ref credentialHandle, (long)Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CLIENT_CERT_POLICY, ref clientCertPolicy, 56); // sizeof(Interop.SspiCli.SecPkgCred_ClientCertPolicy));
                                        status = Interop.SspiCli.SetCredentialsAttributesW(ref credentialHandle, (long)Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CLIENT_CERT_POLICY, ptr3, buffer.Length); // sizeof(Interop.SspiCli.SecPkgCred_ClientCertPolicy));




                                        // Console.WriteLine("size is {0}", sizeof(Interop.SspiCli.SecPkgCred_ClientCertPolicy));
                                        //status = Interop.SspiCli.SetCredentialsAttributesW(&credentialHandle, (long)Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CLIENT_CERT_POLICY, ptr3, 56);
                                    }
                                }
                            }
                        }
                    }
                    Console.WriteLine("SetCredentialsAttributesW finished with {0} {1} {1:x}", status, (int)status);

                }
            }
            return cred!;
        }

        // This function uses new crypto API to support TLS 1.3 and beyond.
        public static unsafe SafeFreeCredentials AcquireCredentialsHandleSchCredentials(X509Certificate? certificate, SslProtocols protocols, EncryptionPolicy policy, bool isServer)
        {
            int protocolFlags = GetProtocolFlagsFromSslProtocols(protocols, isServer);
            Interop.SspiCli.SCH_CREDENTIALS.Flags flags;
            Interop.SspiCli.CredentialUse direction;
            IntPtr storeHandle = (IntPtr)0;

            Console.WriteLine("AcquireCredentialsHandleSchCredentials called!!!!");
            if (isServer)
            {
                direction = Interop.SspiCli.CredentialUse.SECPKG_CRED_INBOUND;
                flags = Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_SEND_AUX_RECORD;

                X509Store store = new X509Store("Enterprise Trust", StoreLocation.CurrentUser);
                Console.WriteLine("Enterprise Trust opened {0} {1}", store, store.Name);
                store.Open(OpenFlags.ReadOnly);
                storeHandle = store.StoreHandle;
            }
            else
            {
                direction = Interop.SspiCli.CredentialUse.SECPKG_CRED_OUTBOUND;
                flags =
                    Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_CRED_MANUAL_CRED_VALIDATION |
                    Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_CRED_NO_DEFAULT_CREDS |
                    Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_SEND_AUX_RECORD;
            }

            if (policy == EncryptionPolicy.RequireEncryption)
            {
                // Always opt-in SCH_USE_STRONG_CRYPTO for TLS.
                if (!isServer && ((protocolFlags & Interop.SChannel.SP_PROT_SSL3) == 0))
                {
                    flags |= Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_USE_STRONG_CRYPTO;
                }
            }
            else if (policy == EncryptionPolicy.AllowNoEncryption)
            {
                // Allow null encryption cipher in addition to other ciphers.
                flags |= Interop.SspiCli.SCH_CREDENTIALS.Flags.SCH_ALLOW_NULL_ENCRYPTION;
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.net_invalid_enum, "EncryptionPolicy"), nameof(policy));
            }

            Interop.SspiCli.SCH_CREDENTIALS credential = default;
            credential.dwVersion = Interop.SspiCli.SCH_CREDENTIALS.CurrentVersion;
            credential.dwFlags = flags;
            credential.hRootStore = storeHandle;
            Console.WriteLine("hRootStore = {0}", storeHandle);
            Interop.Crypt32.CERT_CONTEXT *certificateHandle = null;
            if (certificate != null)
            {
                credential.cCreds = 1;
                certificateHandle = (Interop.Crypt32.CERT_CONTEXT *)certificate.Handle;
                credential.paCred = &certificateHandle;
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info($"flags=({flags}), ProtocolFlags=({protocolFlags}), EncryptionPolicy={policy}");

            if (protocolFlags != 0)
            {
                // If we were asked to do specific protocol we need to fill TLS_PARAMETERS.
                Interop.SspiCli.TLS_PARAMETERS tlsParameters = default;
                tlsParameters.grbitDisabledProtocols = (uint)protocolFlags ^ uint.MaxValue;

                credential.cTlsParameters = 1;
                credential.pTlsParameters = &tlsParameters;
            }

            return AcquireCredentialsHandle(direction, &credential);
        }

        internal static byte[]? GetNegotiatedApplicationProtocol(SafeDeleteContext context)
        {
            Interop.SecPkgContext_ApplicationProtocol alpnContext = default;
            bool success = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPISecureChannel, context, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_APPLICATION_PROTOCOL, ref alpnContext);

            // Check if the context returned is alpn data, with successful negotiation.
            if (success &&
                alpnContext.ProtoNegoExt == Interop.ApplicationProtocolNegotiationExt.ALPN &&
                alpnContext.ProtoNegoStatus == Interop.ApplicationProtocolNegotiationStatus.Success)
            {
                return alpnContext.Protocol;
            }

            return null;
        }

        public static unsafe SecurityStatusPal EncryptMessage(SafeDeleteSslContext securityContext, ReadOnlyMemory<byte> input, int headerSize, int trailerSize, ref byte[] output, out int resultSize)
        {
            // Ensure that there is sufficient space for the message output.
            int bufferSizeNeeded = checked(input.Length + headerSize + trailerSize);
            if (output == null || output.Length < bufferSizeNeeded)
            {
                output = new byte[bufferSizeNeeded];
            }

            // Copy the input into the output buffer to prepare for SCHANNEL's expectations
            input.Span.CopyTo(new Span<byte>(output, headerSize, input.Length));

            const int NumSecBuffers = 4; // header + data + trailer + empty
            Interop.SspiCli.SecBuffer* unmanagedBuffer = stackalloc Interop.SspiCli.SecBuffer[NumSecBuffers];
            Interop.SspiCli.SecBufferDesc sdcInOut = new Interop.SspiCli.SecBufferDesc(NumSecBuffers)
            {
                pBuffers = unmanagedBuffer
            };
            fixed (byte* outputPtr = output)
            {
                Interop.SspiCli.SecBuffer* headerSecBuffer = &unmanagedBuffer[0];
                headerSecBuffer->BufferType = SecurityBufferType.SECBUFFER_STREAM_HEADER;
                headerSecBuffer->pvBuffer = (IntPtr)outputPtr;
                headerSecBuffer->cbBuffer = headerSize;

                Interop.SspiCli.SecBuffer* dataSecBuffer = &unmanagedBuffer[1];
                dataSecBuffer->BufferType = SecurityBufferType.SECBUFFER_DATA;
                dataSecBuffer->pvBuffer = (IntPtr)(outputPtr + headerSize);
                dataSecBuffer->cbBuffer = input.Length;

                Interop.SspiCli.SecBuffer* trailerSecBuffer = &unmanagedBuffer[2];
                trailerSecBuffer->BufferType = SecurityBufferType.SECBUFFER_STREAM_TRAILER;
                trailerSecBuffer->pvBuffer = (IntPtr)(outputPtr + headerSize + input.Length);
                trailerSecBuffer->cbBuffer = trailerSize;

                Interop.SspiCli.SecBuffer* emptySecBuffer = &unmanagedBuffer[3];
                emptySecBuffer->BufferType = SecurityBufferType.SECBUFFER_EMPTY;
                emptySecBuffer->cbBuffer = 0;
                emptySecBuffer->pvBuffer = IntPtr.Zero;

                int errorCode = GlobalSSPI.SSPISecureChannel.EncryptMessage(securityContext, ref sdcInOut, 0);

                if (errorCode != 0)
                {
                    if (NetEventSource.Log.IsEnabled())
                        NetEventSource.Info(securityContext, $"Encrypt ERROR {errorCode:X}");
                    resultSize = 0;
                    return SecurityStatusAdapterPal.GetSecurityStatusPalFromNativeInt(errorCode);
                }

                Debug.Assert(headerSecBuffer->cbBuffer >= 0 && dataSecBuffer->cbBuffer >= 0 && trailerSecBuffer->cbBuffer >= 0);
                Debug.Assert(checked(headerSecBuffer->cbBuffer + dataSecBuffer->cbBuffer + trailerSecBuffer->cbBuffer) <= output.Length);

                resultSize = checked(headerSecBuffer->cbBuffer + dataSecBuffer->cbBuffer + trailerSecBuffer->cbBuffer);
                return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
            }
        }

        public static unsafe SecurityStatusPal DecryptMessage(SafeDeleteSslContext? securityContext, byte[] buffer, ref int offset, ref int count)
        {
            const int NumSecBuffers = 4; // data + empty + empty + empty
            fixed (byte* bufferPtr = buffer)
            {
                Interop.SspiCli.SecBuffer* unmanagedBuffer = stackalloc Interop.SspiCli.SecBuffer[NumSecBuffers];
                Interop.SspiCli.SecBuffer* dataBuffer = &unmanagedBuffer[0];
                dataBuffer->BufferType = SecurityBufferType.SECBUFFER_DATA;
                dataBuffer->pvBuffer = (IntPtr)bufferPtr + offset;
                dataBuffer->cbBuffer = count;

                for (int i = 1; i < NumSecBuffers; i++)
                {
                    Interop.SspiCli.SecBuffer* emptyBuffer = &unmanagedBuffer[i];
                    emptyBuffer->BufferType = SecurityBufferType.SECBUFFER_EMPTY;
                    emptyBuffer->pvBuffer = IntPtr.Zero;
                    emptyBuffer->cbBuffer = 0;
                }

                Interop.SspiCli.SecBufferDesc sdcInOut = new Interop.SspiCli.SecBufferDesc(NumSecBuffers)
                {
                    pBuffers = unmanagedBuffer
                };
                Interop.SECURITY_STATUS errorCode = (Interop.SECURITY_STATUS)GlobalSSPI.SSPISecureChannel.DecryptMessage(securityContext!, ref sdcInOut, 0);

                // Decrypt may repopulate the sec buffers, likely with header + data + trailer + empty.
                // We need to find the data.
                count = 0;
                for (int i = 0; i < NumSecBuffers; i++)
                {
                    // Successfully decoded data and placed it at the following position in the buffer,
                    if ((errorCode == Interop.SECURITY_STATUS.OK && unmanagedBuffer[i].BufferType == SecurityBufferType.SECBUFFER_DATA)
                        // or we failed to decode the data, here is the encoded data.
                        || (errorCode != Interop.SECURITY_STATUS.OK && unmanagedBuffer[i].BufferType == SecurityBufferType.SECBUFFER_EXTRA))
                    {
                        offset = (int)((byte*)unmanagedBuffer[i].pvBuffer - bufferPtr);
                        count = unmanagedBuffer[i].cbBuffer;

                        Debug.Assert(offset >= 0 && count >= 0, $"Expected offset and count greater than 0, got {offset} and {count}");
                        Debug.Assert(checked(offset + count) <= buffer.Length, $"Expected offset+count <= buffer.Length, got {offset}+{count}>={buffer.Length}");

                        break;
                    }
                }

                return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(errorCode);
            }
        }

        public static SecurityStatusPal ApplyAlertToken(ref SafeFreeCredentials? credentialsHandle, SafeDeleteContext? securityContext, TlsAlertType alertType, TlsAlertMessage alertMessage)
        {
            var alertToken = new Interop.SChannel.SCHANNEL_ALERT_TOKEN
            {
                dwTokenType = Interop.SChannel.SCHANNEL_ALERT,
                dwAlertType = (uint)alertType,
                dwAlertNumber = (uint)alertMessage
            };
            byte[] buffer = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref alertToken, 1)).ToArray();
            var securityBuffer = new SecurityBuffer(buffer, SecurityBufferType.SECBUFFER_TOKEN);

            var errorCode = (Interop.SECURITY_STATUS)SSPIWrapper.ApplyControlToken(
                GlobalSSPI.SSPISecureChannel,
                ref securityContext,
                in securityBuffer);

            return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(errorCode, attachException: true);
        }

        private static readonly byte[] s_schannelShutdownBytes = BitConverter.GetBytes(Interop.SChannel.SCHANNEL_SHUTDOWN);

        public static SecurityStatusPal ApplyShutdownToken(ref SafeFreeCredentials? credentialsHandle, SafeDeleteContext? securityContext)
        {
            var securityBuffer = new SecurityBuffer(s_schannelShutdownBytes, SecurityBufferType.SECBUFFER_TOKEN);

            var errorCode = (Interop.SECURITY_STATUS)SSPIWrapper.ApplyControlToken(
                GlobalSSPI.SSPISecureChannel,
                ref securityContext,
                in securityBuffer);

            return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(errorCode, attachException: true);
        }

        public static SafeFreeContextBufferChannelBinding? QueryContextChannelBinding(SafeDeleteContext securityContext, ChannelBindingKind attribute)
        {
            return SSPIWrapper.QueryContextChannelBinding(GlobalSSPI.SSPISecureChannel, securityContext, (Interop.SspiCli.ContextAttribute)attribute);
        }

        public static void QueryContextStreamSizes(SafeDeleteContext securityContext, out StreamSizes streamSizes)
        {
            SecPkgContext_StreamSizes interopStreamSizes = default;
            bool success = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPISecureChannel, securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_STREAM_SIZES, ref interopStreamSizes);
            Debug.Assert(success);
            streamSizes = new StreamSizes(interopStreamSizes);
        }

        public static void QueryContextConnectionInfo(SafeDeleteContext securityContext, out SslConnectionInfo connectionInfo)
        {
            SecPkgContext_ConnectionInfo interopConnectionInfo = default;
            bool success = SSPIWrapper.QueryBlittableContextAttributes(
                GlobalSSPI.SSPISecureChannel,
                securityContext,
                Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CONNECTION_INFO,
                ref interopConnectionInfo);
            Debug.Assert(success);

            TlsCipherSuite cipherSuite = default;
            SecPkgContext_CipherInfo cipherInfo = default;

            success = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPISecureChannel, securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CIPHER_INFO, ref cipherInfo);
            if (success)
            {
                cipherSuite = (TlsCipherSuite)cipherInfo.dwCipherSuite;
            }

            connectionInfo = new SslConnectionInfo(interopConnectionInfo, cipherSuite);
        }

        private static int GetProtocolFlagsFromSslProtocols(SslProtocols protocols, bool isServer)
        {
            int protocolFlags = (int)protocols;

            if (isServer)
            {
                protocolFlags &= Interop.SChannel.ServerProtocolMask;
            }
            else
            {
                protocolFlags &= Interop.SChannel.ClientProtocolMask;
            }

            return protocolFlags;
        }

        private static Interop.SspiCli.SCHANNEL_CRED CreateSecureCredential(
            Interop.SspiCli.SCHANNEL_CRED.Flags flags,
            int protocols, EncryptionPolicy policy)
        {
            var credential = new Interop.SspiCli.SCHANNEL_CRED()
            {
                hRootStore = IntPtr.Zero,
                aphMappers = IntPtr.Zero,
                palgSupportedAlgs = IntPtr.Zero,
                paCred = null,
                cCreds = 0,
                cMappers = 0,
                cSupportedAlgs = 0,
                dwSessionLifespan = 0,
                reserved = 0,
                dwVersion = Interop.SspiCli.SCHANNEL_CRED.CurrentVersion
            };

            if (policy == EncryptionPolicy.RequireEncryption)
            {
                // Prohibit null encryption cipher.
                credential.dwMinimumCipherStrength = 0;
                credential.dwMaximumCipherStrength = 0;
            }
            else if (policy == EncryptionPolicy.AllowNoEncryption)
            {
                // Allow null encryption cipher in addition to other ciphers.
                credential.dwMinimumCipherStrength = -1;
                credential.dwMaximumCipherStrength = 0;
            }
            else if (policy == EncryptionPolicy.NoEncryption)
            {
                // Suppress all encryption and require null encryption cipher only
                credential.dwMinimumCipherStrength = -1;
                credential.dwMaximumCipherStrength = -1;
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.net_invalid_enum, "EncryptionPolicy"), nameof(policy));
            }

            credential.dwFlags = flags;
            credential.grbitEnabledProtocols = protocols;

            return credential;
        }

        //
        // Security: we temporarily reset thread token to open the handle under process account.
        //
        private static unsafe SafeFreeCredentials AcquireCredentialsHandle(Interop.SspiCli.CredentialUse credUsage, Interop.SspiCli.SCHANNEL_CRED* secureCredential)
        {
            // First try without impersonation, if it fails, then try the process account.
            // I.E. We don't know which account the certificate context was created under.
            try
            {
                //
                // For app-compat we want to ensure the credential are accessed under >>process<< account.
                //
                return WindowsIdentity.RunImpersonated<SafeFreeCredentials>(SafeAccessTokenHandle.InvalidHandle, () =>
                {
                    return SSPIWrapper.AcquireCredentialsHandle(GlobalSSPI.SSPISecureChannel, SecurityPackage, credUsage, secureCredential);
                });
            }
            catch
            {
                return SSPIWrapper.AcquireCredentialsHandle(GlobalSSPI.SSPISecureChannel, SecurityPackage, credUsage, secureCredential);
            }
        }

        private static unsafe SafeFreeCredentials AcquireCredentialsHandle(Interop.SspiCli.CredentialUse credUsage, Interop.SspiCli.SCH_CREDENTIALS* secureCredential)
        {
            // First try without impersonation, if it fails, then try the process account.
            // I.E. We don't know which account the certificate context was created under.
            try
            {
                //
                // For app-compat we want to ensure the credential are accessed under >>process<< account.
                //
                return WindowsIdentity.RunImpersonated<SafeFreeCredentials>(SafeAccessTokenHandle.InvalidHandle, () =>
                {
                    return SSPIWrapper.AcquireCredentialsHandle(GlobalSSPI.SSPISecureChannel, SecurityPackage, credUsage, secureCredential);
                });
            }
            catch
            {
                return SSPIWrapper.AcquireCredentialsHandle(GlobalSSPI.SSPISecureChannel, SecurityPackage, credUsage, secureCredential);
            }
        }

    }
}
