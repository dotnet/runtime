// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    /// <summary>
    ///     Trace support for debugging issues signing and verifying XML signatures.
    /// </summary>
    internal static class SignedXmlDebugLog
    {
        //
        // In order to enable XML digital signature debug logging, applications should setup their config
        // file to be similar to the following:
        //
        // <configuration>
        //   <system.diagnostics>
        //     <sources>
        //       <source name="System.Security.Cryptography.Xml.SignedXml"
        //               switchName="XmlDsigLogSwitch">
        //         <listeners>
        //           <add name="logFile" />
        //         </listeners>
        //       </source>
        //     </sources>
        //     <switches>
        //       <add name="XmlDsigLogSwitch" value="Verbose" />
        //     </switches>
        //     <sharedListeners>
        //       <add name="logFile"
        //            type="System.Diagnostics.TextWriterTraceListener"
        //            initializeData="XmlDsigLog.txt"/>
        //     </sharedListeners>
        //     <trace autoflush="true">
        //       <listeners>
        //         <add name="logFile" />
        //       </listeners>
        //     </trace>
        //   </system.diagnostics>
        // </configuration>
        //

        private const string NullString = "(null)";

        private static readonly TraceSource s_traceSource = new TraceSource("System.Security.Cryptography.Xml.SignedXml");
        private static volatile bool s_haveVerboseLogging;
        private static volatile bool s_verboseLogging;
        private static volatile bool s_haveInformationLogging;
        private static volatile bool s_informationLogging;

        /// <summary>
        ///     Types of events that are logged to the debug log
        /// </summary>
        internal enum SignedXmlDebugEvent
        {
            /// <summary>
            ///     Canonicalization of input XML has begun
            /// </summary>
            BeginCanonicalization,

            /// <summary>
            ///     Verification of the signature format itself is beginning
            /// </summary>
            BeginCheckSignatureFormat,

            /// <summary>
            ///     Verification of a signed info is beginning
            /// </summary>
            BeginCheckSignedInfo,

            /// <summary>
            ///     Signing is beginning
            /// </summary>
            BeginSignatureComputation,

            /// <summary>
            ///     Signature verification is beginning
            /// </summary>
            BeginSignatureVerification,

            /// <summary>
            ///     Input data has been transformed to its canonicalized form
            /// </summary>
            CanonicalizedData,

            /// <summary>
            ///     The result of signature format validation
            /// </summary>
            FormatValidationResult,

            /// <summary>
            ///     Namespaces are being propigated into the signature
            /// </summary>
            NamespacePropagation,

            /// <summary>
            ///     Output from a Reference
            /// </summary>
            ReferenceData,

            /// <summary>
            ///     The result of a signature verification
            /// </summary>
            SignatureVerificationResult,

            /// <summary>
            ///     Calculating the final signature
            /// </summary>
            Signing,

            /// <summary>
            ///     A reference is being hashed
            /// </summary>
            SigningReference,

            /// <summary>
            ///     A signature has failed to verify
            /// </summary>
            VerificationFailure,

            /// <summary>
            ///     Verify that a reference has the correct hash value
            /// </summary>
            VerifyReference,

            /// <summary>
            ///     Verification is processing the SignedInfo section of the signature
            /// </summary>
            VerifySignedInfo,

            /// <summary>
            ///     Verification status on the x.509 certificate in use
            /// </summary>
            X509Verification,

            /// <summary>
            ///     The signature is being rejected by the signature format verifier due to having
            ///     a canonicalization algorithm which is not on the known valid list.
            /// </summary>
            UnsafeCanonicalizationMethod,

            /// <summary>
            ///     The signature is being rejected by the signature verifier due to having
            ///     a transform algorithm which is not on the known valid list.
            /// </summary>
            UnsafeTransformMethod,
        }

        /// <summary>
        ///     Check to see if logging should be done in this process
        /// </summary>
        private static bool InformationLoggingEnabled
        {
            get
            {
                if (!s_haveInformationLogging)
                {
                    s_informationLogging = s_traceSource.Switch.ShouldTrace(TraceEventType.Information);
                    s_haveInformationLogging = true;
                }

                return s_informationLogging;
            }
        }

        /// <summary>
        ///     Check to see if verbose log messages should be generated
        /// </summary>
        private static bool VerboseLoggingEnabled
        {
            get
            {
                if (!s_haveVerboseLogging)
                {
                    s_verboseLogging = s_traceSource.Switch.ShouldTrace(TraceEventType.Verbose);
                    s_haveVerboseLogging = true;
                }

                return s_verboseLogging;
            }
        }

        /// <summary>
        ///     Convert the byte array into a hex string
        /// </summary>
        private static string FormatBytes(byte[]? bytes)
        {
            if (bytes == null)
                return NullString;

#if NET9_0_OR_GREATER
            return Convert.ToHexStringLower(bytes);
#else
            return HexConverter.ToString(bytes, HexConverter.Casing.Lower);
#endif
        }

        /// <summary>
        ///     Map a key to a string describing the key
        /// </summary>
        private static string GetKeyName(object key)
        {
            Debug.Assert(key != null, "key != null");

            ICspAsymmetricAlgorithm? cspKey = key as ICspAsymmetricAlgorithm;
            X509Certificate? certificate = key as X509Certificate;
            X509Certificate2? certificate2 = key as X509Certificate2;

            //
            // Use the following sources for key names, if available:
            //
            // * CAPI key         -> key container name
            // * X509Certificate2 -> subject simple name
            // * X509Certificate  -> subject name
            // * All others       -> hash code
            //

            string keyName;
#pragma warning disable CA1416 // This call site is reachable on all platforms. 'CspKeyContainerInfo.KeyContainerName' is supported on: 'windows'.
            if (cspKey != null && cspKey.CspKeyContainerInfo.KeyContainerName != null)
            {
                keyName = "\"" + cspKey.CspKeyContainerInfo.KeyContainerName + "\"";
            }
#pragma warning restore CA1416
            else if (certificate2 != null)
            {
                keyName = "\"" + certificate2.GetNameInfo(X509NameType.SimpleName, false) + "\"";
            }
            else if (certificate != null)
            {
                keyName = "\"" + certificate.Subject + "\"";
            }
            else
            {
                keyName = key.GetHashCode().ToString("x8", CultureInfo.InvariantCulture);
            }

            return $"{key.GetType().Name}#{keyName}";
        }

        /// <summary>
        ///     Map an object to a string describing the object
        /// </summary>
        private static string GetObjectId(object o)
        {
            Debug.Assert(o != null, "o != null");

            return $"{o.GetType().Name}#{o.GetHashCode():x8}";
        }

        /// <summary>
        ///     Map an OID to the friendliest name possible
        /// </summary>
        private static string? GetOidName(Oid oid)
        {
            Debug.Assert(oid != null, "oid != null");

            string? friendlyName = oid.FriendlyName;
            if (string.IsNullOrEmpty(friendlyName))
                friendlyName = oid.Value;

            return friendlyName;
        }

        /// <summary>
        ///     Log that canonicalization has begun on input data
        /// </summary>
        /// <param name="signedXml">SignedXml object doing the signing or verification</param>
        /// <param name="canonicalizationTransform">transform canonicalizing the input</param>
        internal static void LogBeginCanonicalization(SignedXml signedXml, Transform canonicalizationTransform)
        {
            Debug.Assert(signedXml != null, "signedXml != null");
            Debug.Assert(canonicalizationTransform != null, "canonicalizationTransform != null");

            if (InformationLoggingEnabled)
            {
                string logMessage = SR.Format(CultureInfo.InvariantCulture,
                                                  SR.Log_BeginCanonicalization,
                                                  canonicalizationTransform.Algorithm,
                                                  canonicalizationTransform.GetType().Name);
                WriteLine(signedXml,
                          TraceEventType.Information,
                          SignedXmlDebugEvent.BeginCanonicalization,
                          logMessage);
            }

            if (VerboseLoggingEnabled)
            {
                string canonicalizationSettings = SR.Format(CultureInfo.InvariantCulture,
                                                                SR.Log_CanonicalizationSettings,
                                                                canonicalizationTransform.Resolver!.GetType(),
                                                                canonicalizationTransform.BaseURI);
                WriteLine(signedXml,
                          TraceEventType.Verbose,
                          SignedXmlDebugEvent.BeginCanonicalization,
                          canonicalizationSettings);
            }
        }

        /// <summary>
        ///     Log that we're going to be validating the signature format itself
        /// </summary>
        /// <param name="signedXml">SignedXml object doing the verification</param>
        /// <param name="formatValidator">Callback delegate which is being used for format verification</param>
        internal static void LogBeginCheckSignatureFormat(SignedXml signedXml, Func<SignedXml, bool> formatValidator)
        {
            Debug.Assert(signedXml != null, "signedXml != null");
            Debug.Assert(formatValidator != null, "formatValidator != null");

            if (InformationLoggingEnabled)
            {
                MethodInfo validationMethod = formatValidator.Method;

                string logMessage = SR.Format(CultureInfo.InvariantCulture,
                                                  SR.Log_CheckSignatureFormat,
                                                  validationMethod.Module.Assembly.FullName,
                                                  validationMethod.DeclaringType!.FullName,
                                                  validationMethod.Name);
                WriteLine(signedXml, TraceEventType.Information, SignedXmlDebugEvent.BeginCheckSignatureFormat, logMessage);
            }
        }

        /// <summary>
        ///     Log that checking SignedInfo is beginning
        /// </summary>
        /// <param name="signedXml">SignedXml object doing the verification</param>
        /// <param name="signedInfo">SignedInfo object being verified</param>
        internal static void LogBeginCheckSignedInfo(SignedXml signedXml, SignedInfo signedInfo)
        {
            Debug.Assert(signedXml != null, "signedXml != null");
            Debug.Assert(signedInfo != null, " signedInfo != null");

            if (InformationLoggingEnabled)
            {
                string logMessage = SR.Format(CultureInfo.InvariantCulture,
                                                  SR.Log_CheckSignedInfo,
                                                  signedInfo.Id ?? NullString);
                WriteLine(signedXml, TraceEventType.Information, SignedXmlDebugEvent.BeginCheckSignedInfo, logMessage);
            }
        }

        /// <summary>
        ///     Log that signature computation is beginning
        /// </summary>
        /// <param name="signedXml">SignedXml object doing the signing</param>
        /// <param name="context">Context of the signature</param>
        internal static void LogBeginSignatureComputation(SignedXml signedXml, XmlElement context)
        {
            Debug.Assert(signedXml != null, "signedXml != null");

            if (InformationLoggingEnabled)
            {
                WriteLine(signedXml,
                          TraceEventType.Information,
                          SignedXmlDebugEvent.BeginSignatureComputation,
                          SR.Log_BeginSignatureComputation);
            }

            if (VerboseLoggingEnabled)
            {
                string contextData = SR.Format(CultureInfo.InvariantCulture,
                                                   SR.Log_XmlContext,
                                                   context != null ? context.OuterXml : NullString);

                WriteLine(signedXml,
                          TraceEventType.Verbose,
                          SignedXmlDebugEvent.BeginSignatureComputation,
                          contextData);
            }
        }

        /// <summary>
        ///     Log that signature verification is beginning
        /// </summary>
        /// <param name="signedXml">SignedXml object doing the verification</param>
        /// <param name="context">Context of the verification</param>
        internal static void LogBeginSignatureVerification(SignedXml signedXml, XmlElement? context)
        {
            Debug.Assert(signedXml != null, "signedXml != null");

            if (InformationLoggingEnabled)
            {
                WriteLine(signedXml,
                          TraceEventType.Information,
                          SignedXmlDebugEvent.BeginSignatureVerification,
                          SR.Log_BeginSignatureVerification);
            }

            if (VerboseLoggingEnabled)
            {
                string contextData = SR.Format(CultureInfo.InvariantCulture,
                                                   SR.Log_XmlContext,
                                                   context != null ? context.OuterXml : NullString);

                WriteLine(signedXml,
                          TraceEventType.Verbose,
                          SignedXmlDebugEvent.BeginSignatureVerification,
                          contextData);
            }
        }

        /// <summary>
        ///     Log the canonicalized data
        /// </summary>
        /// <param name="signedXml">SignedXml object doing the signing or verification</param>
        /// <param name="canonicalizationTransform">transform canonicalizing the input</param>
        internal static void LogCanonicalizedOutput(SignedXml signedXml, Transform canonicalizationTransform)
        {
            Debug.Assert(signedXml != null, "signedXml != null");
            Debug.Assert(canonicalizationTransform != null, "canonicalizationTransform != null");

            if (VerboseLoggingEnabled)
            {
                using (StreamReader reader = new StreamReader((canonicalizationTransform.GetOutput(typeof(Stream)) as Stream)!))
                {
                    string logMessage = SR.Format(CultureInfo.InvariantCulture,
                                                      SR.Log_CanonicalizedOutput,
                                                      reader.ReadToEnd());
                    WriteLine(signedXml,
                              TraceEventType.Verbose,
                              SignedXmlDebugEvent.CanonicalizedData,
                              logMessage);
                }
            }
        }

        /// <summary>
        ///     Log that the signature format callback has rejected the signature
        /// </summary>
        /// <param name="signedXml">SignedXml object doing the signature verification</param>
        /// <param name="result">result of the signature format verification</param>
        internal static void LogFormatValidationResult(SignedXml signedXml, bool result)
        {
            Debug.Assert(signedXml != null, "signedXml != null");

            if (InformationLoggingEnabled)
            {
                string logMessage = result ? SR.Log_FormatValidationSuccessful :
                                             SR.Log_FormatValidationNotSuccessful;
                WriteLine(signedXml, TraceEventType.Information, SignedXmlDebugEvent.FormatValidationResult, logMessage);
            }
        }

        /// <summary>
        ///     Log that a signature is being rejected as having an invalid format due to its canonicalization
        ///     algorithm not being on the valid list.
        /// </summary>
        /// <param name="signedXml">SignedXml object doing the signature verification</param>
        /// <param name="algorithm">Canonicalization algorithm</param>
        /// <param name="validAlgorithms">List of valid canonicalization algorithms</param>
        internal static void LogUnsafeCanonicalizationMethod(SignedXml signedXml, string algorithm, IEnumerable<string>? validAlgorithms)
        {
            Debug.Assert(signedXml != null, "signedXml != null");
            Debug.Assert(validAlgorithms != null, "validAlgorithms != null");

            if (InformationLoggingEnabled)
            {
                StringBuilder validAlgorithmBuilder = new StringBuilder();
                foreach (string validAlgorithm in validAlgorithms)
                {
                    if (validAlgorithmBuilder.Length != 0)
                    {
                        validAlgorithmBuilder.Append(", ");
                    }

                    validAlgorithmBuilder.Append('"').Append(validAlgorithm).Append('"');
                }

                string logMessage = SR.Format(CultureInfo.InvariantCulture,
                                                  SR.Log_UnsafeCanonicalizationMethod,
                                                  algorithm,
                                                  validAlgorithmBuilder.ToString());

                WriteLine(signedXml, TraceEventType.Information, SignedXmlDebugEvent.UnsafeCanonicalizationMethod, logMessage);
            }
        }

        /// <summary>
        ///     Log that a signature is being rejected as having an invalid signature due to a transform
        ///     algorithm not being on the valid list.
        /// </summary>
        /// <param name="signedXml">SignedXml object doing the signature verification</param>
        /// <param name="algorithm">Transform algorithm that was not allowed</param>
        /// <param name="validC14nAlgorithms">The valid C14N algorithms</param>
        /// <param name="validTransformAlgorithms">The valid C14N algorithms</param>
        internal static void LogUnsafeTransformMethod(
            SignedXml signedXml,
            string algorithm,
            IEnumerable<string>? validC14nAlgorithms,
            IEnumerable<string> validTransformAlgorithms)
        {
            Debug.Assert(signedXml != null, "signedXml != null");
            Debug.Assert(validC14nAlgorithms != null, "validC14nAlgorithms != null");
            Debug.Assert(validTransformAlgorithms != null, "validTransformAlgorithms != null");

            if (InformationLoggingEnabled)
            {
                StringBuilder validAlgorithmBuilder = new StringBuilder();
                foreach (string validAlgorithm in validC14nAlgorithms)
                {
                    if (validAlgorithmBuilder.Length != 0)
                    {
                        validAlgorithmBuilder.Append(", ");
                    }

                    validAlgorithmBuilder.Append('"').Append(validAlgorithm).Append('"');
                }

                foreach (string validAlgorithm in validTransformAlgorithms)
                {
                    if (validAlgorithmBuilder.Length != 0)
                    {
                        validAlgorithmBuilder.Append(", ");
                    }

                    validAlgorithmBuilder.Append('"').Append(validAlgorithm).Append('"');
                }

                string logMessage = SR.Format(CultureInfo.InvariantCulture,
                                                  SR.Log_UnsafeTransformMethod,
                                                  algorithm,
                                                  validAlgorithmBuilder.ToString());

                WriteLine(signedXml, TraceEventType.Information, SignedXmlDebugEvent.UnsafeTransformMethod, logMessage);
            }
        }

        /// <summary>
        ///     Log namespaces which are being propagated into the signature
        /// </summary>
        /// <param name="signedXml">SignedXml doing the signing or verification</param>
        /// <param name="namespaces">namespaces being propagated</param>
        internal static void LogNamespacePropagation(SignedXml signedXml, XmlNodeList? namespaces)
        {
            Debug.Assert(signedXml != null, "signedXml != null");

            if (InformationLoggingEnabled)
            {
                if (namespaces != null)
                {
                    foreach (XmlAttribute propagatedNamespace in namespaces)
                    {
                        string propagationMessage = SR.Format(CultureInfo.InvariantCulture,
                                                                  SR.Log_PropagatingNamespace,
                                                                  propagatedNamespace.Name,
                                                                  propagatedNamespace.Value);

                        WriteLine(signedXml,
                                  TraceEventType.Information,
                                  SignedXmlDebugEvent.NamespacePropagation,
                                  propagationMessage);
                    }
                }
                else
                {
                    WriteLine(signedXml,
                              TraceEventType.Information,
                              SignedXmlDebugEvent.NamespacePropagation,
                              SR.Log_NoNamespacesPropagated);
                }
            }
        }

        /// <summary>
        ///     Log the output of a reference
        /// </summary>
        /// <param name="reference">The reference being processed</param>
        /// <param name="data">Stream containing the output of the reference</param>
        /// <returns>Stream containing the output of the reference</returns>
        [return: NotNullIfNotNull(nameof(data))]
        internal static Stream? LogReferenceData(Reference reference, Stream? data)
        {
            if (VerboseLoggingEnabled)
            {
                //
                // Since the input data stream could be from the network or another source that does not
                // support rewinding, we will read the stream into a temporary MemoryStream that can be used
                // to stringify the output and also return to the reference so that it can produce the hash
                // value.
                //

                MemoryStream ms = new MemoryStream();

                // First read the input stream into our temporary stream
                byte[] buffer = new byte[4096];
                int readBytes;
                do
                {
                    readBytes = data!.Read(buffer, 0, buffer.Length);
                    ms.Write(buffer, 0, readBytes);
                } while (readBytes == buffer.Length);

                // Log out information about it
                string logMessage = SR.Format(CultureInfo.InvariantCulture,
                                                  SR.Log_TransformedReferenceContents,
                                                  Encoding.UTF8.GetString(ms.ToArray()));
                WriteLine(reference,
                          TraceEventType.Verbose,
                          SignedXmlDebugEvent.ReferenceData,
                          logMessage);

                // Rewind to the beginning, so that the entire input stream is hashed
                ms.Seek(0, SeekOrigin.Begin);
                return ms;
            }
            else
            {
                return data;
            }
        }

        /// <summary>
        ///     Log the computation of a signature value when signing with an asymmetric algorithm
        /// </summary>
        /// <param name="signedXml">SignedXml object calculating the signature</param>
        /// <param name="key">key used for signing</param>
        /// <param name="signatureDescription">signature description being used to create the signature</param>
        /// <param name="hash">hash algorithm used to digest the output</param>
        /// <param name="asymmetricSignatureFormatter">signature formatter used to do the signing</param>
        internal static void LogSigning(SignedXml signedXml,
                                        object key,
                                        SignatureDescription signatureDescription,
                                        HashAlgorithm hash,
                                        AsymmetricSignatureFormatter asymmetricSignatureFormatter)
        {
            Debug.Assert(signedXml != null, "signedXml != null");
            Debug.Assert(signatureDescription != null, "signatureDescription != null");
            Debug.Assert(hash != null, "hash != null");
            Debug.Assert(asymmetricSignatureFormatter != null, "asymmetricSignatureFormatter != null");

            if (InformationLoggingEnabled)
            {
                string logMessage = SR.Format(CultureInfo.InvariantCulture,
                                                  SR.Log_SigningAsymmetric,
                                                  GetKeyName(key),
                                                  signatureDescription.GetType().Name,
                                                  hash.GetType().Name,
                                                  asymmetricSignatureFormatter.GetType().Name);

                WriteLine(signedXml,
                          TraceEventType.Information,
                          SignedXmlDebugEvent.Signing,
                          logMessage);
            }
        }

        /// <summary>
        ///     Log the computation of a signature value when signing with a keyed hash algorithm
        /// </summary>
        /// <param name="signedXml">SignedXml object calculating the signature</param>
        /// <param name="key">key the signature is created with</param>
        internal static void LogSigning(SignedXml signedXml, KeyedHashAlgorithm key)
        {
            Debug.Assert(signedXml != null, "signedXml != null");
            Debug.Assert(key != null, "key != null");

            if (InformationLoggingEnabled)
            {
                string logMessage = SR.Format(CultureInfo.InvariantCulture,
                                                  SR.Log_SigningHmac,
                                                  key.GetType().Name);

                WriteLine(signedXml,
                          TraceEventType.Information,
                          SignedXmlDebugEvent.Signing,
                          logMessage);
            }
        }

        /// <summary>
        ///     Log the calculation of a hash value of a reference
        /// </summary>
        /// <param name="signedXml">SignedXml object driving the signature</param>
        /// <param name="reference">Reference being hashed</param>
        [RequiresUnreferencedCode(CryptoHelpers.CreateFromNameUnreferencedCodeMessage)]
        internal static void LogSigningReference(SignedXml signedXml, Reference reference)
        {
            Debug.Assert(signedXml != null, "signedXml != null");
            Debug.Assert(reference != null, "reference != null");

            if (VerboseLoggingEnabled)
            {
                HashAlgorithm? hashAlgorithm = CryptoHelpers.CreateNonTransformFromName<HashAlgorithm>(reference.DigestMethod);
                string hashAlgorithmName = hashAlgorithm == null ? "null" : hashAlgorithm.GetType().Name;
                string logMessage = SR.Format(CultureInfo.InvariantCulture,
                                                  SR.Log_SigningReference,
                                                  GetObjectId(reference),
                                                  reference.Uri,
                                                  reference.Id,
                                                  reference.Type,
                                                  reference.DigestMethod,
                                                  hashAlgorithmName);

                WriteLine(signedXml,
                          TraceEventType.Verbose,
                          SignedXmlDebugEvent.SigningReference,
                          logMessage);
            }
        }

        /// <summary>
        ///     Log the specific point where a signature is determined to not be verifiable
        /// </summary>
        /// <param name="signedXml">SignedXml object doing the verification</param>
        /// <param name="failureLocation">location that the signature was determined to be invalid</param>
        internal static void LogVerificationFailure(SignedXml signedXml, string failureLocation)
        {
            if (InformationLoggingEnabled)
            {
                string logMessage = SR.Format(CultureInfo.InvariantCulture,
                                                  SR.Log_VerificationFailed,
                                                  failureLocation);

                WriteLine(signedXml,
                          TraceEventType.Information,
                          SignedXmlDebugEvent.VerificationFailure,
                          logMessage);
            }
        }

        /// <summary>
        ///     Log the success or failure of a signature verification operation
        /// </summary>
        /// <param name="signedXml">SignedXml object doing the verification</param>
        /// <param name="key">public key used to verify the signature</param>
        /// <param name="verified">true if the signature verified, false otherwise</param>
        internal static void LogVerificationResult(SignedXml signedXml, object key, bool verified)
        {
            Debug.Assert(signedXml != null, "signedXml != null");
            Debug.Assert(key != null, "key != null");

            if (InformationLoggingEnabled)
            {
                string resource = verified ? SR.Log_VerificationWithKeySuccessful :
                                             SR.Log_VerificationWithKeyNotSuccessful;
                string logMessage = string.Format(CultureInfo.InvariantCulture,
                                                  resource,
                                                  GetKeyName(key));

                WriteLine(signedXml,
                          TraceEventType.Information,
                          SignedXmlDebugEvent.SignatureVerificationResult,
                          logMessage);
            }
        }

        /// <summary>
        ///     Log the check for appropriate X509 key usage
        /// </summary>
        /// <param name="signedXml">SignedXml doing the signature verification</param>
        /// <param name="certificate">certificate having its key usages checked</param>
        /// <param name="keyUsages">key usages being examined</param>
        internal static void LogVerifyKeyUsage(SignedXml signedXml, X509Certificate certificate, X509KeyUsageExtension keyUsages)
        {
            Debug.Assert(signedXml != null, "signedXml != null");
            Debug.Assert(certificate != null, "certificate != null");

            if (InformationLoggingEnabled)
            {
                string logMessage = SR.Format(CultureInfo.InvariantCulture,
                                                  SR.Log_KeyUsages,
                                                  keyUsages.KeyUsages,
                                                  GetOidName(keyUsages.Oid!),
                                                  GetKeyName(certificate));

                WriteLine(signedXml,
                          TraceEventType.Verbose,
                          SignedXmlDebugEvent.X509Verification,
                          logMessage);
            }
        }

        /// <summary>
        ///     Log that we are verifying a reference
        /// </summary>
        /// <param name="signedXml">SignedXMl object doing the verification</param>
        /// <param name="reference">reference being verified</param>
        internal static void LogVerifyReference(SignedXml signedXml, Reference reference)
        {
            Debug.Assert(signedXml != null, "signedXml != null");
            Debug.Assert(reference != null, "reference != null");

            if (InformationLoggingEnabled)
            {
                string logMessage = SR.Format(CultureInfo.InvariantCulture,
                                                  SR.Log_VerifyReference,
                                                  GetObjectId(reference),
                                                  reference.Uri,
                                                  reference.Id,
                                                  reference.Type);

                WriteLine(signedXml,
                          TraceEventType.Verbose,
                          SignedXmlDebugEvent.VerifyReference,
                          logMessage);
            }
        }

        /// <summary>
        ///     Log the hash comparison when verifying a reference
        /// </summary>
        /// <param name="signedXml">SignedXml object verifying the signature</param>
        /// <param name="reference">reference being verified</param>
        /// <param name="actualHash">actual hash value of the reference</param>
        /// <param name="expectedHash">hash value the signature expected the reference to have</param>
        [RequiresUnreferencedCode(CryptoHelpers.CreateFromNameUnreferencedCodeMessage)]
        internal static void LogVerifyReferenceHash(SignedXml signedXml,
                                                    Reference reference,
                                                    byte[]? actualHash,
                                                    byte[]? expectedHash)
        {
            Debug.Assert(signedXml != null, "signedXml != null");
            Debug.Assert(reference != null, "reference != null");
            Debug.Assert(actualHash != null, "actualHash != null");
            Debug.Assert(expectedHash != null, "expectedHash != null");

            if (VerboseLoggingEnabled)
            {
                HashAlgorithm? hashAlgorithm = CryptoHelpers.CreateNonTransformFromName<HashAlgorithm>(reference.DigestMethod);
                string hashAlgorithmName = hashAlgorithm == null ? "null" : hashAlgorithm.GetType().Name;
                string logMessage = SR.Format(CultureInfo.InvariantCulture,
                                                  SR.Log_ReferenceHash,
                                                  GetObjectId(reference),
                                                  reference.DigestMethod,
                                                  hashAlgorithmName,
                                                  FormatBytes(actualHash),
                                                  FormatBytes(expectedHash));

                WriteLine(signedXml,
                          TraceEventType.Verbose,
                          SignedXmlDebugEvent.VerifyReference,
                          logMessage);
            }
        }

        /// <summary>
        ///     Log the verification parameters when verifying the SignedInfo section of a signature using an
        ///     asymmetric key
        /// </summary>
        /// <param name="signedXml">SignedXml object doing the verification</param>
        /// <param name="key">key being used to verify the signed info</param>
        /// <param name="signatureDescription">type of signature description class used</param>
        /// <param name="hashAlgorithm">type of hash algorithm used</param>
        /// <param name="asymmetricSignatureDeformatter">type of signature deformatter used</param>
        /// <param name="actualHashValue">hash value of the signed info</param>
        /// <param name="signatureValue">raw signature value</param>
        internal static void LogVerifySignedInfo(SignedXml signedXml,
                                                 AsymmetricAlgorithm key,
                                                 SignatureDescription signatureDescription,
                                                 HashAlgorithm hashAlgorithm,
                                                 AsymmetricSignatureDeformatter asymmetricSignatureDeformatter,
                                                 byte[]? actualHashValue,
                                                 byte[]? signatureValue)
        {
            Debug.Assert(signedXml != null, "signedXml != null");
            Debug.Assert(signatureDescription != null, "signatureDescription != null");
            Debug.Assert(hashAlgorithm != null, "hashAlgorithm != null");
            Debug.Assert(asymmetricSignatureDeformatter != null, "asymmetricSignatureDeformatter != null");

            if (InformationLoggingEnabled)
            {
                string logMessage = SR.Format(CultureInfo.InvariantCulture,
                                                  SR.Log_VerifySignedInfoAsymmetric,
                                                  GetKeyName(key),
                                                  signatureDescription.GetType().Name,
                                                  hashAlgorithm.GetType().Name,
                                                  asymmetricSignatureDeformatter.GetType().Name);
                WriteLine(signedXml,
                          TraceEventType.Information,
                          SignedXmlDebugEvent.VerifySignedInfo,
                          logMessage);
            }

            if (VerboseLoggingEnabled)
            {
                string hashLog = SR.Format(CultureInfo.InvariantCulture,
                                               SR.Log_ActualHashValue,
                                               FormatBytes(actualHashValue));
                WriteLine(signedXml, TraceEventType.Verbose, SignedXmlDebugEvent.VerifySignedInfo, hashLog);

                string signatureLog = SR.Format(CultureInfo.InvariantCulture,
                                                    SR.Log_RawSignatureValue,
                                                    FormatBytes(signatureValue));
                WriteLine(signedXml, TraceEventType.Verbose, SignedXmlDebugEvent.VerifySignedInfo, signatureLog);
            }
        }

        /// <summary>
        ///     Log the verification parameters when verifying the SignedInfo section of a signature using a
        ///     keyed hash algorithm
        /// </summary>
        /// <param name="signedXml">SignedXml object doing the verification</param>
        /// <param name="mac">hash algorithm doing the verification</param>
        /// <param name="actualHashValue">hash value of the signed info</param>
        /// <param name="signatureValue">raw signature value</param>
        internal static void LogVerifySignedInfo(SignedXml signedXml,
                                                 KeyedHashAlgorithm mac,
                                                 byte[]? actualHashValue,
                                                 byte[]? signatureValue)
        {
            Debug.Assert(signedXml != null, "signedXml != null");
            Debug.Assert(mac != null, "mac != null");

            if (InformationLoggingEnabled)
            {
                string logMessage = SR.Format(CultureInfo.InvariantCulture,
                                                  SR.Log_VerifySignedInfoHmac,
                                                  mac.GetType().Name);
                WriteLine(signedXml,
                          TraceEventType.Information,
                          SignedXmlDebugEvent.VerifySignedInfo,
                          logMessage);
            }

            if (VerboseLoggingEnabled)
            {
                string hashLog = SR.Format(CultureInfo.InvariantCulture,
                                               SR.Log_ActualHashValue,
                                               FormatBytes(actualHashValue));
                WriteLine(signedXml, TraceEventType.Verbose, SignedXmlDebugEvent.VerifySignedInfo, hashLog);

                string signatureLog = SR.Format(CultureInfo.InvariantCulture,
                                                    SR.Log_RawSignatureValue,
                                                    FormatBytes(signatureValue));
                WriteLine(signedXml, TraceEventType.Verbose, SignedXmlDebugEvent.VerifySignedInfo, signatureLog);
            }
        }

        /// <summary>
        ///     Log that an X509 chain is being built for a certificate
        /// </summary>
        /// <param name="signedXml">SignedXml object building the chain</param>
        /// <param name="chain">chain built for the certificate</param>
        /// <param name="certificate">certificate having the chain built for it</param>
        internal static void LogVerifyX509Chain(SignedXml signedXml, X509Chain chain, X509Certificate certificate)
        {
            Debug.Assert(signedXml != null, "signedXml != null");
            Debug.Assert(certificate != null, "certificate != null");
            Debug.Assert(chain != null, "chain != null");

            if (InformationLoggingEnabled)
            {
                string buildMessage = SR.Format(CultureInfo.InvariantCulture,
                                                    SR.Log_BuildX509Chain,
                                                    GetKeyName(certificate));
                WriteLine(signedXml,
                          TraceEventType.Information,
                          SignedXmlDebugEvent.X509Verification,
                          buildMessage);
            }

            if (VerboseLoggingEnabled)
            {
                // Dump out the flags and other miscelanious information used for building
                string revocationMode = SR.Format(CultureInfo.InvariantCulture,
                                                      SR.Log_RevocationMode,
                                                      chain.ChainPolicy.RevocationFlag);
                WriteLine(signedXml, TraceEventType.Verbose, SignedXmlDebugEvent.X509Verification, revocationMode);

                string revocationFlag = SR.Format(CultureInfo.InvariantCulture,
                                                      SR.Log_RevocationFlag,
                                                      chain.ChainPolicy.RevocationFlag);
                WriteLine(signedXml, TraceEventType.Verbose, SignedXmlDebugEvent.X509Verification, revocationFlag);

                string verificationFlags = SR.Format(CultureInfo.InvariantCulture,
                                                         SR.Log_VerificationFlag,
                                                         chain.ChainPolicy.VerificationFlags);
                WriteLine(signedXml, TraceEventType.Verbose, SignedXmlDebugEvent.X509Verification, verificationFlags);

                string verificationTime = SR.Format(CultureInfo.InvariantCulture,
                                                        SR.Log_VerificationTime,
                                                        chain.ChainPolicy.VerificationTime);
                WriteLine(signedXml, TraceEventType.Verbose, SignedXmlDebugEvent.X509Verification, verificationTime);

                string urlTimeout = SR.Format(CultureInfo.InvariantCulture,
                                                  SR.Log_UrlTimeout,
                                                  chain.ChainPolicy.UrlRetrievalTimeout);
                WriteLine(signedXml, TraceEventType.Verbose, SignedXmlDebugEvent.X509Verification, urlTimeout);
            }

            // If there were any errors in the chain, make sure to dump those out
            if (InformationLoggingEnabled)
            {
                foreach (X509ChainStatus status in chain.ChainStatus)
                {
                    if (status.Status != X509ChainStatusFlags.NoError)
                    {
                        string logMessage = SR.Format(CultureInfo.InvariantCulture,
                                                          SR.Log_X509ChainError,
                                                          status.Status,
                                                          status.StatusInformation);

                        WriteLine(signedXml,
                                  TraceEventType.Information,
                                  SignedXmlDebugEvent.X509Verification,
                                  logMessage);
                    }
                }
            }

            // Finally, dump out the chain itself
            if (VerboseLoggingEnabled)
            {
                StringBuilder chainElements = new StringBuilder();
                chainElements.Append(SR.Log_CertificateChain);

                foreach (X509ChainElement element in chain.ChainElements)
                {
                    chainElements.Append(' ').Append(GetKeyName(element.Certificate));
                }

                WriteLine(signedXml,
                          TraceEventType.Verbose,
                          SignedXmlDebugEvent.X509Verification,
                          chainElements.ToString());
            }
        }

        /// <summary>
        /// Write information when user hits the Signed XML recursion depth limit issue.
        /// This is helpful in debugging this kind of issues.
        /// </summary>
        /// <param name="signedXml">SignedXml object verifying the signature</param>
        /// <param name="reference">reference being verified</param>
        [RequiresUnreferencedCode(CryptoHelpers.CreateFromNameUnreferencedCodeMessage)]
        internal static void LogSignedXmlRecursionLimit(SignedXml signedXml,
                                                        Reference reference)
        {
            Debug.Assert(signedXml != null, "signedXml != null");
            Debug.Assert(reference != null, "reference != null");

            if (InformationLoggingEnabled)
            {
                HashAlgorithm? hashAlgorithm = CryptoHelpers.CreateNonTransformFromName<HashAlgorithm>(reference.DigestMethod);
                string hashAlgorithmName = hashAlgorithm == null ? "null" : hashAlgorithm.GetType().Name;
                string logMessage = SR.Format(CultureInfo.InvariantCulture,
                                                    SR.Log_SignedXmlRecursionLimit,
                                                    GetObjectId(reference),
                                                    reference.DigestMethod,
                                                    hashAlgorithmName);

                WriteLine(signedXml,
                            TraceEventType.Information,
                            SignedXmlDebugEvent.VerifySignedInfo,
                            logMessage);
            }
        }

        /// <summary>
        ///     Write data to the log
        /// </summary>
        /// <param name="source">object doing the trace</param>
        /// <param name="eventType">severity of the debug event</param>
        /// <param name="data">data being written</param>
        /// <param name="eventId">type of event being traced</param>
        private static void WriteLine(object source, TraceEventType eventType, SignedXmlDebugEvent eventId, string data)
        {
            Debug.Assert(source != null, "source != null");
            Debug.Assert(!string.IsNullOrEmpty(data), "!string.IsNullOrEmpty(data)");
            Debug.Assert(InformationLoggingEnabled, "InformationLoggingEnabled");

            s_traceSource.TraceEvent(eventType,
                                    (int)eventId,
                                    "[{0}, {1}] {2}",
                                    GetObjectId(source),
                                    eventId,
                                    data);
        }
    }
}
