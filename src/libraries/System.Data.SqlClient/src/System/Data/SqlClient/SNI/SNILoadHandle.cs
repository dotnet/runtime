// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.Data.SqlClient.SNI
{
    /// <summary>
    /// Global SNI settings and status
    /// </summary>
    internal class SNILoadHandle
    {
        public static readonly SNILoadHandle SingletonInstance = new SNILoadHandle();

        public readonly EncryptionOptions _encryptionOption = EncryptionOptions.OFF;
        public ThreadLocal<SNIError> _lastError = new ThreadLocal<SNIError>(() => { return new SNIError(SNIProviders.INVALID_PROV, 0, TdsEnums.SNI_SUCCESS, string.Empty); });

        private readonly uint _status = TdsEnums.SNI_SUCCESS;

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
                _lastError.Value = value;
            }
        }

        /// <summary>
        /// SNI library status
        /// </summary>
        public uint Status
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
