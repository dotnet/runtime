// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography
{
    internal abstract class DecryptorPal : IDisposable
    {
        internal DecryptorPal(RecipientInfoCollection recipientInfos)
        {
            RecipientInfos = recipientInfos;
        }

        /// <summary>
        /// Return the managed representation of the recipients.
        /// 
        /// Desktop compat: Unlike the desktop, we compute this once and then latch it. Since both RecipientInfo and RecipientInfoCollection are immutable objects, this should be 
        /// a safe optimization to make.
        /// </summary>
        public RecipientInfoCollection RecipientInfos { get; }

        /// <summary>
        /// Attempt to decrypt the CMS using the specified "cert". If successful, return the ContentInfo that contains the decrypted content. If unsuccessful, return null and set "exception"
        /// to a valid Exception object. Do not throw the exception as EnvelopedCms will want to continue decryption attempts against other recipients. Only if all the recipients fail to
        /// decrypt will then EnvelopedCms throw the exception from the last failed attempt.
        /// </summary>
        public abstract ContentInfo TryDecrypt(RecipientInfo recipientInfo, X509Certificate2 cert, X509Certificate2Collection originatorCerts, X509Certificate2Collection extraStore, out Exception exception);

        public abstract void Dispose();
    }
}

