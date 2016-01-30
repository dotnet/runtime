// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace System.Data.SqlClient.SNI
{
    /// <summary>
    /// Singleton to manage all MARS connection
    /// </summary>
    internal class SNIMarsManager
    {
        public static readonly SNIMarsManager Singleton = new SNIMarsManager();
        private ConcurrentDictionary<SNIHandle, SNIMarsConnection> _connections = new ConcurrentDictionary<SNIHandle, SNIMarsConnection>();

        /// <summary>
        /// Constructor
        /// </summary>
        public SNIMarsManager()
        {
        }

        /// <summary>
        /// Create a MARS connection
        /// </summary>
        /// <param name="lowerHandle">Lower SNI handle</param>
        /// <returns>SNI error code</returns>
        public uint CreateMarsConnection(SNIHandle lowerHandle)
        {
            SNIMarsConnection connection = new SNIMarsConnection(lowerHandle);

            if (_connections.TryAdd(lowerHandle, connection))
            {
                return connection.StartReceive();
            }
            else
            {
                return TdsEnums.SNI_ERROR;
            }
        }

        /// <summary>
        /// Get a MARS connection by lower handle
        /// </summary>
        /// <param name="lowerHandle">Lower SNI handle</param>
        /// <returns>MARS connection</returns>
        public SNIMarsConnection GetConnection(SNIHandle lowerHandle)
        {
            return _connections[lowerHandle];
        }
    }
}
