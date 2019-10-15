// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Data.SqlClient.SNI
{
    /// <summary>
    /// SNI error
    /// </summary>
    internal class SNIError
    {
        public readonly SNIProviders provider;
        public readonly string errorMessage;
        public readonly uint nativeError;
        public readonly uint sniError;
        public readonly string function;
        public readonly uint lineNumber;
        public readonly Exception exception;

        public SNIError(SNIProviders provider, uint nativeError, uint sniErrorCode, string errorMessage)
        {
            this.lineNumber = 0;
            this.function = string.Empty;
            this.provider = provider;
            this.nativeError = nativeError;
            this.sniError = sniErrorCode;
            this.errorMessage = errorMessage;
            this.exception = null;
        }

        public SNIError(SNIProviders provider, uint sniErrorCode, Exception sniException)
        {
            this.lineNumber = 0;
            this.function = string.Empty;
            this.provider = provider;
            this.nativeError = 0;
            this.sniError = sniErrorCode;
            this.errorMessage = string.Empty;
            this.exception = sniException;
        }
    }
}
