// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;

namespace System.Data.SqlClient.SNI
{
    /// <summary>
    /// Global SNI settings and status
    /// </summary>
    internal class SNILoadHandle
    {
        static public SNILoadHandle SingletonInstance = new SNILoadHandle();

        private ThreadLocal<SNIError> _lastError = new ThreadLocal<SNIError>(() => { return new SNIError(SNIProviders.INVALID_PROV, 0, 0, "No SNI Error has been reported yet."); });
        private readonly UInt32 _status = TdsEnums.SNI_SUCCESS;
        public readonly EncryptionOptions _encryptionOption = EncryptionOptions.OFF;

        /// <summary>
        /// Last SNI error
        /// </summary>
        public SNIError LastError
        {
            get
            {
                return _lastError.Value;
            }

            set
            {
                _lastError = new ThreadLocal<SNIError>(() => { return value; });
            }
        }

        /// <summary>
        /// SNI library status
        /// </summary>
        public UInt32 Status
        {
            get
            {
                return _status;
            }
        }

        /// <summary>
        /// Encryption options setting
        /// </summary>
        public EncryptionOptions Options
        {
            get
            {
                return _encryptionOption;
            }
        }
    }
}