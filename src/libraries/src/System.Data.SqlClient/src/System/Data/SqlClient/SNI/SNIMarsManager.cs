// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Data.SqlClient.SNI
{
    /// <summary>
    /// Singleton to manage all MARS connection
    /// </summary>
    internal class SNIMarsManager
    {
        public static SNIMarsManager Singleton = new SNIMarsManager();
        private Dictionary<SNIHandle, SNIMarsConnection> _connections = new Dictionary<SNIHandle, SNIMarsConnection>();

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
            lock (_connections)
            {
                _connections.Add(lowerHandle, connection);
            }

            return connection.StartReceive();
        }

        /// <summary>
        /// Get a MARS connection by lower handle
        /// </summary>
        /// <param name="lowerHandle">Lower SNI handle</param>
        /// <returns>MARS connection</returns>
        public SNIMarsConnection GetConnection(SNIHandle lowerHandle)
        {
            lock (_connections)
            {
                return _connections[lowerHandle];
            }
        }
    }
}