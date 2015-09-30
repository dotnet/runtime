// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace System.Data.SqlClient.SNI
{
    /// <summary>
    /// SNI error
    /// </summary>
    internal class SNIError
    {
        public SNIProviders provider;
        public String errorMessage;
        public uint nativeError;
        public uint sniError;
        public String function;
        public uint lineNumber;

        public SNIError(SNIProviders provider, uint nativeError, uint sniErrorCode, String errorMessage)
        {
            this.lineNumber = 0;
            this.function = "";
            this.provider = provider;
            this.nativeError = nativeError;
            this.sniError = sniErrorCode;

            // For now hardcode "TCP Provider" to keep TDS parser happy
            //
            this.errorMessage = "TCP Provider: " + errorMessage + "\r\n";
        }
    }
}