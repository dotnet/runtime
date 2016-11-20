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
        /// Security credential for server operation
        /// </summary>
        private SecurityHandle _inboundCredential = new SecurityHandle();

        /// <summary>
        /// Security credential for client operation
        /// </summary>
        private SecurityHandle _outboundCredential = new SecurityHandle();

        /// <summary>
        /// Server context
        /// </summary>
        private SecurityHandle _serverContext = new SecurityHandle();

        /// <summary>
        /// Client context
        /// </summary>
        private SecurityHandle _clientContext = new SecurityHandle();

        /// <summary>
        /// Target server we're authenticating against
        /// </summary>
        private string _targetMachineSPN;

        /// <summary>
        /// Maximum size of the token buffer
        /// </summary>
        private int _maxTokenBufferSize;

        /// <summary>
        /// Default constructor
        /// </summary>
        private SSPIContext()
        {
            // Prepare a list of packages
            IList<string> packages = new List<string>();

            packages.Add(SecConstants.Negotiate);
            packages.Add(SecConstants.Kerberos);
            packages.Add(SecConstants.NTLM);

            // Try each package
            foreach (string package in packages)
            {
                IntPtr packagePtr = IntPtr.Zero;

                // Query security package
                int secReturnCode = SecurityWrapper.QuerySecurityPackageInfo(package, ref packagePtr);

                // Check if package was found
                if (secReturnCode == unchecked((int)SecResult.PackageNotFound))
                {
                    // Move to the next package
                    continue;
                }

                // Check if we succeeded
                if (secReturnCode != (int)SecResult.Ok)
                {
                    // Security package qury failed
                    throw new Win32Exception(secReturnCode, "Failed to query security package");
                }

                try
                {
                    // Map unmanaged structure pointer to managed
                    SecPkgInfo info = (SecPkgInfo)Marshal.PtrToStructure(packagePtr, typeof(SecPkgInfo));

                    // Check if buffer size is larger than what we have
                    if (info.MaxToken > _maxTokenBufferSize)
                    {
                        // Use the token size
                        _maxTokenBufferSize = info.MaxToken;
                    }
                }
                finally
                {
                    // Release security package
                    secReturnCode = SecurityWrapper.FreeContextBuffer(packagePtr);

                    // Check if we succeeded
                    if (secReturnCode != (int)SecResult.Ok)
                    {
                        // Security package qury failed
                        throw new Win32Exception(secReturnCode, "Failed to free security package");
                    }
                }
            }

            // Check if buffer size is valid
            if (_maxTokenBufferSize == 0)
            {
                throw new Exception("Either no security packages found or none of them reported a valid maximum token length");
            }
        }

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
            // Create an instance of the context
            SSPIContext context = new SSPIContext();

            // Initialize token lifetime container
            SecurityInteger lifeTime = new SecurityInteger();

            // Delegate into security API
            int secReturnCode = SecurityWrapper.AcquireCredentialsHandle(null, SecConstants.Negotiate, (int)SecPgkCredentials.Inbound, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, ref context._inboundCredential, ref lifeTime);

            // Check if we succeeded
            if (secReturnCode != (int)SecResult.Ok)
            {
                // We couldn't obtain server credentials handle
                throw new Win32Exception(secReturnCode, "Failed to acquire server credentials handle");
            }

            return context;
        }

        /// <summary>
        /// Create SSPI context for client
        /// </summary>
        public static SSPIContext CreateClient()
        {
            // Create an instance of the context
            SSPIContext context = new SSPIContext();

            // Initialize token lifetime container
            SecurityInteger lifeTime = new SecurityInteger();

            // Delegate into security API
            int secReturnCode = SecurityWrapper.AcquireCredentialsHandle(null, SecConstants.Negotiate, (int)SecPgkCredentials.Outbound, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, ref context._outboundCredential, ref lifeTime);

            // Check if we succeeded
            if (secReturnCode != (int)SecResult.Ok)
            {
                // We couldn't obtain server credentials handle
                throw new Win32Exception(secReturnCode, "Failed to acquire client credentials handle");
            }

            return context;
        }

        /// <summary>
        /// Initialize authentication sequence for the server
        /// </summary>
        /// <param name="clientToken">Token received from the client</param>
        /// <returns>Token to be sent to the client in response</returns>
        public SSPIResponse StartServerAuthentication(byte[] clientToken)
        {
            // Wrap client token with the security buffer
            SecBufferDesc clientSecBuffer = new SecBufferDesc(clientToken);

            try
            {
                // Allocate a new instance of the server security buffer of the specified size
                SecBufferDesc serverSecBuffer = new SecBufferDesc(_maxTokenBufferSize);

                try
                {
                    // Return code from the security API call
                    int secReturnCode = 0;

                    // New context attribute
                    uint contextAttribute = 0;

                    // Initialize token lifetime container
                    SecurityInteger lifeTime = new SecurityInteger();

                    // Delegate into security API
                    secReturnCode = SecurityWrapper.AcceptSecurityContext(ref _inboundCredential,
                        IntPtr.Zero,
                        ref clientSecBuffer,
                        (int)SecContextRequirements.MutualAuthentication,
                        (int)SecDataRepresentation.Native,
                        out _serverContext,
                        out serverSecBuffer,
                        out contextAttribute,
                        out lifeTime);

                    // Check the return code
                    if (secReturnCode != (int)SecResult.Ok && secReturnCode != (int)SecResult.ContinueNeeded)
                    {
                        // Operation failed
                        throw new Win32Exception(secReturnCode, "Failed to accept security context");
                    }

                    // Convert to byte array and indication whether this is a last call
                    return new SSPIResponse(serverSecBuffer.ToArray(), secReturnCode != (int)SecResult.ContinueNeeded);
                }
                finally
                {
                    // Dispose server security buffer
                    serverSecBuffer.Dispose();
                }
            }
            finally
            {
                // Dispose client security buffer
                clientSecBuffer.Dispose();
            }
        }

        /// <summary>
        /// Continue authentication sequence for the server
        /// </summary>
        /// <param name="clientToken">Token received from the client</param>
        /// <returns>Token to be sent to the client in response</returns>
        public SSPIResponse ContinueServerAuthentication(byte[] clientToken)
        {
            // Wrap client token with the security buffer
            SecBufferDesc clientSecBuffer = new SecBufferDesc(clientToken);

            try
            {
                // Allocate a new instance of the server security buffer of the specified size
                SecBufferDesc serverSecBuffer = new SecBufferDesc(_maxTokenBufferSize);

                try
                {
                    // Return code from the security API call
                    int secReturnCode = 0;

                    // New context attribute
                    uint contextAttribute = 0;

                    // Initialize token lifetime container
                    SecurityInteger lifeTime = new SecurityInteger();

                    // Delegate into security API
                    secReturnCode = SecurityWrapper.AcceptSecurityContext(ref _inboundCredential,
                        ref _serverContext,
                        ref clientSecBuffer,
                        (int)SecContextRequirements.MutualAuthentication,
                        (int)SecDataRepresentation.Native,
                        out _serverContext,
                        out serverSecBuffer,
                        out contextAttribute,
                        out lifeTime);

                    // Check the return code
                    if (secReturnCode != (int)SecResult.Ok && secReturnCode != (int)SecResult.ContinueNeeded)
                    {
                        // Operation failed
                        throw new Win32Exception(secReturnCode, "Failed to accept security context");
                    }

                    // Convert to byte array and indication whether this is a last call
                    return new SSPIResponse(serverSecBuffer.ToArray(), secReturnCode != (int)SecResult.ContinueNeeded);
                }
                finally
                {
                    // Dispose server security buffer
                    serverSecBuffer.Dispose();
                }
            }
            finally
            {
                // Dispose client security buffer
                clientSecBuffer.Dispose();
            }
        }

        /// <summary>
        /// Initialize authentication sequence for the client
        /// </summary>
        /// <returns>Token to be sent to the server</returns>
        public SSPIResponse StartClientAuthentication(string targetMachine, uint targetPort)
        {
            // Save the server we're authenticating against
            _targetMachineSPN = string.Format("MSSQLSvc/{0}:{1}", targetMachine, targetPort);

            // Allocate a new instance of the client security buffer of the specified size
            SecBufferDesc clientSecBuffer = new SecBufferDesc(_maxTokenBufferSize);

            try
            {
                // Return code from the security API call
                int secReturnCode = 0;

                // New context attribute
                uint contextAttribute = 0;

                // Initialize token lifetime container
                SecurityInteger lifeTime = new SecurityInteger();

                // Delegate into security API
                secReturnCode = SecurityWrapper.InitializeSecurityContext(ref _outboundCredential,
                    IntPtr.Zero,
                    _targetMachineSPN,
                    (int)(SecContextRequirements.MutualAuthentication | SecContextRequirements.Delegate | SecContextRequirements.ExtendedError),
                    0,
                    (int)SecDataRepresentation.Native,
                    IntPtr.Zero,
                    0,
                    out _clientContext,
                    out clientSecBuffer,
                    out contextAttribute,
                    out lifeTime);

                // Check the return code
                if (secReturnCode != (int)SecResult.Ok && secReturnCode != (int)SecResult.ContinueNeeded)
                {
                    // Operation failed
                    throw new Win32Exception(secReturnCode, "Failed to generate initial security context");
                }

                // Convert to byte array and indication whether this is a last call
                return new SSPIResponse(clientSecBuffer.ToArray(), secReturnCode != (int)SecResult.ContinueNeeded && secReturnCode != (int)SecResult.CompleteAndContinue);
            }
            finally
            {
                // Dispose server security buffer
                clientSecBuffer.Dispose();
            }
        }

        /// <summary>
        /// Initialize authentication sequence for the client
        /// </summary>
        /// <param name="clientToken">Payload received from the server</param>
        /// <returns>Token to be sent to the server</returns>
        public SSPIResponse ContinueClientAuthentication(byte[] clientToken)
        {
            // Wrap client token with the security buffer
            SecBufferDesc serverSecBuffer = new SecBufferDesc(clientToken);

            try
            {
                // Allocate a new instance of the client security buffer of the specified size
                SecBufferDesc clientSecBuffer = new SecBufferDesc(_maxTokenBufferSize);

                try
                {
                    // Return code from the security API call
                    int secReturnCode = 0;

                    // New context attribute
                    uint contextAttribute = 0;

                    // Initialize token lifetime container
                    SecurityInteger lifeTime = new SecurityInteger();

                    // Delegate into security API
                    secReturnCode = SecurityWrapper.InitializeSecurityContext(ref _outboundCredential,
                        ref _clientContext,
                        _targetMachineSPN,
                        (int)(SecContextRequirements.MutualAuthentication | SecContextRequirements.Delegate | SecContextRequirements.ExtendedError),
                        0,
                        (int)SecDataRepresentation.Native,
                        ref serverSecBuffer,
                        0,
                        out _clientContext,
                        out clientSecBuffer,
                        out contextAttribute,
                        out lifeTime);

                    // Check the return code
                    if (secReturnCode != (int)SecResult.Ok && secReturnCode != (int)SecResult.ContinueNeeded && secReturnCode != (int)SecResult.CompleteAndContinue)
                    {
                        // Operation failed
                        throw new Win32Exception(secReturnCode, "Failed to generate security context");
                    }

                    // NOTE: Digest SSP call to "CompleteAuthToken" is intentionally omitted because we don't support Digest today.

                    // Convert to byte array and indication whether this is a last call
                    return new SSPIResponse(clientSecBuffer.ToArray(), secReturnCode != (int)SecResult.ContinueNeeded && secReturnCode != (int)SecResult.CompleteAndContinue);
                }
                finally
                {
                    // Dispose client security buffer
                    clientSecBuffer.Dispose();
                }
            }
            finally
            {
                // Dispose server security buffer
                serverSecBuffer.Dispose();
            }
        }

        /// <summary>
        /// Return identity of the party on the other end
        /// </summary>
        /// <returns></returns>
        public IIdentity GetRemoteIdentity()
        {
            IntPtr token = IntPtr.Zero;

            // Delegate into security API
            int secReturnCode = SecurityWrapper.QuerySecurityContextToken(ref _serverContext, ref token);

            // Check if we succeeded
            if (secReturnCode != (int)SecResult.Ok)
            {
                // We couldn't obtain token identity
                throw new Win32Exception(secReturnCode, "Failed to obtain security context token");
            }

            return new WindowsIdentity(token);
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
            // Check if we have an inbound credential
            if (_inboundCredential.IsValid())
            {
                // Call into security API to release the credentials
                if (SecurityWrapper.FreeCredentialsHandle(ref _inboundCredential) != (int)SecResult.Ok)
                {
                    // Throw an exception
                    throw new Exception("Failed to release inbound credentials handle");
                }

                // Reset inbound credential
                _inboundCredential = new SecurityHandle();
            }

            // Check if we have an outbound credential
            if (_outboundCredential.IsValid())
            {
                // Call into security API to release the credentials
                if (SecurityWrapper.FreeCredentialsHandle(ref _outboundCredential) != (int)SecResult.Ok)
                {
                    // Throw an exception
                    throw new Exception("Failed to release outbound credentials handle");
                }

                // Reset inbound credential
                _outboundCredential = new SecurityHandle();
            }
        }
    }
}
