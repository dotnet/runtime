// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.X509Certificates
{
    /// <summary>
    ///   The exception that is thrown when importing a PKCS#12/PFX has failed
    ///   due to violating a specified limit.
    /// </summary>
    public sealed class Pkcs12LoadLimitExceededException : CryptographicException
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="Pkcs12LoadLimitExceededException"/>
        ///   class.
        /// </summary>
        /// <param name="propertyName">
        ///   The name of the property representing the limit that was exceeded.
        /// </param>
        public Pkcs12LoadLimitExceededException(string propertyName)
            : base(SR.Format(SR.Cryptography_X509_PKCS12_LimitExceeded, propertyName))
        {
        }
    }
}
