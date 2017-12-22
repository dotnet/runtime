// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace System.DirectoryServices.AccountManagement
{
    class CertificateCollectionDeltas
    {
        public CertificateCollectionDeltas(X509Certificate2Collection currentCerts, List<string> originalThumbprints)
        {
            List<string> originalThumbprintsRemaining = new List<string>(originalThumbprints);
        
            foreach (X509Certificate2 cert in currentCerts)
            {
                string thumbprint = cert.Thumbprint;

                // If this cert isn't in the list of original certs, it must have been added
                if (!originalThumbprints.Contains(thumbprint))
                    this.addedCerts.Add(cert);

                // We've visited this thumbprint, so remove it from the list of unvisited thumbprints
                originalThumbprintsRemaining.Remove(thumbprint);
            }

            foreach (string thumbprint in originalThumbprintsRemaining)
            {
                // these are the removed certs
            }
        }

        public List<X509Certificate2> Inserted
        {
            get { return this.addedCerts; }
        }

        public List<X509Certificate2> Removed
        {
            get { return this.removedCerts; }
        }

        //
        //
        //
        List<X509Certificate2> addedCerts = new List<X509Certificate2>();
        List<X509Certificate2> removedCerts = new List<X509Certificate2>();
        
    }
}
