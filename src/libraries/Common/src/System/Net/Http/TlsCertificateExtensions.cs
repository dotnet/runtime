// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Http
{
    /// <summary>
    /// Defines Extension methods which are meant to be re-used across WinHttp and UnixHttp Handlers
    /// </summary>
    internal static class TLSCertificateExtensions
    {
        private const string ClientCertificateOid = "1.3.6.1.5.5.7.3.2";
        private static Oid s_clientCertOidInst = new Oid(ClientCertificateOid);

        /// <summary>
        ///   returns true if the X509 Certificate can be used  as SSL Client Certificate.
        /// </summary>
        private static bool IsClientCertificate(X509Certificate2 cert)
        {
            Debug.Assert(cert != null, "certificate cannot be null");

            bool foundEku = false;
            bool foundKeyUsages = false;
            bool isClientAuth = true;
            bool isDigitalSignature = true;
            foreach(X509Extension extension in cert.Extensions)
            {
                // check if the extension is an enhanced usage ext.
                // But do this only if needed. No point going over it, if we already have established that our cert has the
                // required extension.
                if (!foundEku)
                {
                    X509EnhancedKeyUsageExtension enhancedUsageExt = extension as X509EnhancedKeyUsageExtension;
                    if (enhancedUsageExt != null)
                    {
                        foundEku = true;
                        isClientAuth = false;
                        foreach(Oid oid in enhancedUsageExt.EnhancedKeyUsages)
                        {
                            if (string.Equals(ClientCertificateOid, oid.Value))
                            {
                                isClientAuth = true;
                                break;
                            }
                        }
                    }
                }

                // Check if the extension is a key usage extension.
                // No point going over it if we have already established that our cert has digital signature
                if (!foundKeyUsages)
                {
                    X509KeyUsageExtension usageExt = extension as X509KeyUsageExtension;
                    if (usageExt != null)
                    {
                        foundKeyUsages = true;
                        isDigitalSignature = (usageExt.KeyUsages & X509KeyUsageFlags.DigitalSignature) != 0;
                    }
                }

                if (foundKeyUsages && foundEku)
                {
                    break;
                }
            }

            return isClientAuth && isDigitalSignature;
        }

        internal static X509Chain BuildNewChain(X509Certificate2 certificate, bool includeClientApplicationPolicy)
        {
            var chain = new X509Chain();
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            if (includeClientApplicationPolicy)
            {
                chain.ChainPolicy.ApplicationPolicy.Add(s_clientCertOidInst);
            }

            if (chain.Build(certificate))
            {
                return chain;
            }
            else
            {
                chain.Dispose();
                return null;
            }
        }

        /// <summary>
        ///   Returns a new collection containing valid client certificates from the given X509Certificate2Collection
        /// </summary>
        internal static bool TryFindClientCertificate(this X509Certificate2Collection certificates,
                                                      ISet<string> allowedIssuers,
                                                      out X509Certificate2 clientCertificate,
                                                      out X509Chain clientCertChain)
        {
            clientCertificate = null;
            clientCertChain = null;
            if (certificates == null)
            {
                return false;
            }

            DateTime now = DateTime.Now;
            foreach(X509Certificate2 cert in certificates)
            {
                if (cert.HasPrivateKey && now >= cert.NotBefore && now <= cert.NotAfter)
                {
                    if (IsClientCertificate(cert))
                    {
                        if (allowedIssuers.Count == 0)
                        {
                            clientCertificate = cert;
                            clientCertChain = null;
                            return true;
                        }

                        X509Chain chain = BuildNewChain(cert, includeClientApplicationPolicy: true);
                        if (chain == null)
                        {
                            continue;
                        }

                        bool isComplete = true;
                        foreach (X509ChainStatus chainStatus in chain.ChainStatus)
                        {
                            if ((chainStatus.Status & X509ChainStatusFlags.PartialChain) == X509ChainStatusFlags.PartialChain)
                            {
                                isComplete = false;
                                break;
                            }
                        }

                        if (chain.ChainElements.Count > 0 && isComplete)
                        {
                            X509Certificate2 trustAnchor = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;
                            if (allowedIssuers.Contains(trustAnchor.SubjectName.Name))
                            {
                                clientCertificate = cert;
                                clientCertChain = chain;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
