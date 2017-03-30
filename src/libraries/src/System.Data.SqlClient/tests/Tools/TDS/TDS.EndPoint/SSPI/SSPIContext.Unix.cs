// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Microsoft.SqlServer.TDS.EndPoint.SSPI
{
    /// <summary>
    /// SSPI context
    /// </summary>
    public class SSPIContext : IDisposable
    {
        /// <summary>
        /// FInalizer
        /// </summary>
        ~SSPIContext()
        {
            // Indicate that we're being destructed
            Dispose(false);
        }

        /// <summary>
        /// Create SSPI context for server
        /// </summary>
        public static SSPIContext CreateServer()
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Create SSPI context for client
        /// </summary>
        public static SSPIContext CreateClient()
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Initialize authentication sequence for the server
        /// </summary>
        /// <param name="clientToken">Token received from the client</param>
        /// <returns>Token to be sent to the client in response</returns>
        public SSPIResponse StartServerAuthentication(byte[] clientToken)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Continue authentication sequence for the server
        /// </summary>
        /// <param name="clientToken">Token received from the client</param>
        /// <returns>Token to be sent to the client in response</returns>
        public SSPIResponse ContinueServerAuthentication(byte[] clientToken)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Initialize authentication sequence for the client
        /// </summary>
        /// <returns>Token to be sent to the server</returns>
        public SSPIResponse StartClientAuthentication(string targetMachine, uint targetPort)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Initialize authentication sequence for the client
        /// </summary>
        /// <param name="clientToken">Payload received from the server</param>
        /// <returns>Token to be sent to the server</returns>
        public SSPIResponse ContinueClientAuthentication(byte[] clientToken)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Return identity of the party on the other end
        /// </summary>
        /// <returns></returns>
        public IIdentity GetRemoteIdentity()
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            // Indicate that we're disposing
            Dispose(true);
        }

        /// <summary>
        /// Dispose the instance
        /// </summary>
        /// <param name="bIsDisposing"></param>
        protected void Dispose(bool bIsDisposing)
        {
            
        }
    }
}
