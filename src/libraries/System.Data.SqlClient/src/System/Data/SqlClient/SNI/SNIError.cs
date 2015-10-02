// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


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

        public SNIError(SNIProviders provider, uint nativeError, uint sniErrorCode, string errorMessage)
        {
            this.lineNumber = 0;
            this.function = "";
            this.provider = provider;
            this.nativeError = nativeError;
            this.sniError = sniErrorCode;

            // Hardcode "TCP Provider" to keep TDS parser happy
            this.errorMessage = "TCP Provider: " + errorMessage + Environment.NewLine;
        }
    }
}