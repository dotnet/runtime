// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.SqlServer.TDS.EndPoint.SSPI
{
    /// <summary>
    /// Wrapper for security functions that implement SSPI
    /// </summary>
    internal class SecurityWrapper
    {
        /// <summary>
        /// Acquires a handle to preexisting credentials of a security principal
        /// </summary>
        /// <param name="pszPrincipal">A pointer to a null-terminated string that specifies the name of the principal whose credentials the handle will reference</param>
        /// <param name="pszPackage">A pointer to a null-terminated string that specifies the name of the security package with which these credentials will be used</param>
        /// <param name="fCredentialUse">A flag that indicates how these credentials will be used</param>
        /// <param name="pvLogonID">A pointer to a locally unique identifier (LUID) that identifies the user</param>
        /// <param name="pAuthData">A pointer to package-specific data</param>
        /// <param name="pGetKeyFn">This parameter is not used and should be set to NULL</param>
        /// <param name="pvGetKeyArgument">This parameter is not used and should be set to NULL</param>
        /// <param name="phCredential">A pointer to a CredHandle structure to receive the credential handle</param>
        /// <param name="ptsExpiry">A pointer to a TimeStamp structure that receives the time at which the returned credentials expire</param>
        [DllImport(Interop.Libraries.SspiCli, CharSet = CharSet.Unicode, EntryPoint="AcquireCredentialsHandleW")]
        internal static extern int AcquireCredentialsHandle(
            string pszPrincipal,
            string pszPackage,
            int fCredentialUse,
            IntPtr pvLogonID,
            IntPtr pAuthData,
            int pGetKeyFn,
            IntPtr pvGetKeyArgument,
            ref SecurityHandle phCredential,
            ref SecurityInteger ptsExpiry);

        /// <summary>
        /// The InitializeSecurityContext (Schannel) function initiates the client side, outbound security context from a credential handle.
        /// </summary>
        /// <param name="phCredential">A handle to the credentials returned by AcquireCredentialsHandle</param>
        /// <param name="phContext">A pointer to a CtxtHandle structure</param>
        /// <param name="pszTargetName ">A pointer to a null-terminated string that uniquely identifies the target server</param>
        /// <param name="fContextReq">Bit flags that indicate requests for the context</param>
        /// <param name="Reserved1">This parameter is reserved and must be set to zero.</param>
        /// <param name="TargetDataRep">The data representation, such as byte ordering, on the target</param>
        /// <param name="pInput">A pointer to a SecBufferDesc structure that contains pointers to the buffers supplied as input to the package</param>
        /// <param name="Reserved2">This parameter is reserved and must be set to zero.</param>
        /// <param name="phNewContext">A pointer to a CtxtHandle structure</param>
        /// <param name="pOutput">A pointer to a SecBufferDesc structure that contains the output buffer descriptor</param>
        /// <param name="pfContextAttr">A pointer to a variable that receives a set of bit flags that indicate the attributes of the established context</param>
        /// <param name="ptsTimeStamp">A pointer to a TimeStamp structure that receives the expiration time of the context</param>
        [DllImport(Interop.Libraries.SspiCli, CharSet = CharSet.Unicode, SetLastError = false, EntryPoint="InitializeSecurityContextW")]
        internal static extern int InitializeSecurityContext(ref SecurityHandle phCredential,
            IntPtr phContext,
            string pszTargetName,
            uint fContextReq,
            int Reserved1,
            uint TargetDataRep,
            IntPtr pInput,
            int Reserved2,
            out SecurityHandle phNewContext,
            out SecBufferDesc pOutput,
            out uint pfContextAttr,
            out SecurityInteger ptsTimeStamp);

        /// <summary>
        /// The InitializeSecurityContext (Schannel) function initiates the client side, outbound security context from a credential handle.
        /// </summary>
        /// <param name="phCredential">A handle to the credentials returned by AcquireCredentialsHandle</param>
        /// <param name="phContext">A pointer to a CtxtHandle structure</param>
        /// <param name="pszTargetName ">A pointer to a null-terminated string that uniquely identifies the target server</param>
        /// <param name="fContextReq">Bit flags that indicate requests for the context</param>
        /// <param name="Reserved1">This parameter is reserved and must be set to zero.</param>
        /// <param name="TargetDataRep">The data representation, such as byte ordering, on the target</param>
        /// <param name="pInput">A pointer to a SecBufferDesc structure that contains pointers to the buffers supplied as input to the package</param>
        /// <param name="Reserved2">This parameter is reserved and must be set to zero.</param>
        /// <param name="phNewContext">A pointer to a CtxtHandle structure</param>
        /// <param name="pOutput">A pointer to a SecBufferDesc structure that contains the output buffer descriptor</param>
        /// <param name="pfContextAttr">A pointer to a variable that receives a set of bit flags that indicate the attributes of the established context</param>
        /// <param name="ptsTimeStamp">A pointer to a TimeStamp structure that receives the expiration time of the context</param>
        [DllImport(Interop.Libraries.SspiCli, CharSet = CharSet.Unicode, SetLastError = false, EntryPoint="InitializeSecurityContextW")]
        internal static extern int InitializeSecurityContext(ref SecurityHandle phCredential,
            ref SecurityHandle phContext,
            string pszTargetName,
            uint fContextReq,
            int Reserved1,
            uint TargetDataRep,
            ref SecBufferDesc pInput,
            int Reserved2,
            out SecurityHandle phNewContext,
            out SecBufferDesc pOutput,
            out uint pfContextAttr,
            out SecurityInteger ptsTimeStamp);

        /// <summary>
        /// The CompleteAuthToken function completes an authentication token
        /// </summary>
        /// <param name="phContext">A handle of the context that needs to be completed</param>
        /// <param name="pOutput">A pointer to a SecBufferDesc structure that contains the buffer descriptor for the entire message.</param>
        [DllImport(Interop.Libraries.SspiCli, CharSet = CharSet.Auto)]
        internal static extern int CompleteAuthToken(ref SecurityHandle phContext, out SecBufferDesc pOutput);

        /// <summary>
        /// The AcceptSecurityContext (Negotiate) function enables the server component of a transport application to establish a security context between the server and a remote client
        /// </summary>
        /// <param name="phCredential">A handle to the credentials of the server</param>
        /// <param name="phContext">A pointer to a CtxtHandle structure</param>
        /// <param name="pInput">A pointer to a SecBufferDesc structure generated by a client call to InitializeSecurityContext (Negotiate) that contains the input buffer descriptor</param>
        /// <param name="fContextReq">Bit flags that specify the attributes required by the server to establish the context</param>
        /// <param name="TargetDataRep">The data representation, such as byte ordering, on the target</param>
        /// <param name="phNewContext">A pointer to a CtxtHandle structure</param>
        /// <param name="pOutput">A pointer to a SecBufferDesc structure that contains the output buffer descriptor</param>
        /// <param name="pfContextAttr">A pointer to a variable that receives a set of bit flags that indicate the attributes of the established context</param>
        /// <param name="ptsTimeStamp">A pointer to a TimeStamp structure that receives the expiration time of the context</param>
        [DllImport(Interop.Libraries.SspiCli, CharSet = CharSet.Auto, SetLastError = false)]
        internal static extern int AcceptSecurityContext(ref SecurityHandle phCredential,
            IntPtr phContext,
            ref SecBufferDesc pInput,
            uint fContextReq,
            uint TargetDataRep,
            out SecurityHandle phNewContext,
            out SecBufferDesc pOutput,
            out uint pfContextAttr,
            out SecurityInteger ptsTimeStamp);

        /// <summary>
        /// The AcceptSecurityContext (Negotiate) function enables the server component of a transport application to establish a security context between the server and a remote client
        /// </summary>
        /// <param name="phCredential">A handle to the credentials of the server</param>
        /// <param name="phContext">A pointer to a CtxtHandle structure</param>
        /// <param name="pInput">A pointer to a SecBufferDesc structure generated by a client call to InitializeSecurityContext (Negotiate) that contains the input buffer descriptor</param>
        /// <param name="fContextReq">Bit flags that specify the attributes required by the server to establish the context</param>
        /// <param name="TargetDataRep">The data representation, such as byte ordering, on the target</param>
        /// <param name="phNewContext">A pointer to a CtxtHandle structure</param>
        /// <param name="pOutput">A pointer to a SecBufferDesc structure that contains the output buffer descriptor</param>
        /// <param name="pfContextAttr">A pointer to a variable that receives a set of bit flags that indicate the attributes of the established context</param>
        /// <param name="ptsTimeStamp">A pointer to a TimeStamp structure that receives the expiration time of the context</param>
        [DllImport(Interop.Libraries.SspiCli, CharSet = CharSet.Auto, SetLastError = false)]
        internal static extern int AcceptSecurityContext(ref SecurityHandle phCredential,
            ref SecurityHandle phContext,
            ref SecBufferDesc pInput,
            uint fContextReq,
            uint TargetDataRep,
            out SecurityHandle phNewContext,
            out SecBufferDesc pOutput,
            out uint pfContextAttr,
            out SecurityInteger ptsTimeStamp);

        /// <summary>
        /// The FreeCredentialsHandle function notifies the security system that the credentials are no longer needed
        /// </summary>
        /// <param name="phCredential">A pointer to the CredHandle handle obtained by using the AcquireCredentialsHandle (General) function</param>
        [DllImport(Interop.Libraries.SspiCli, CharSet = CharSet.Auto)]
        internal static extern int FreeCredentialsHandle(ref SecurityHandle phCredential);

        /// <summary>
        /// The FreeCredentialsHandle function notifies the security system that the credentials are no longer needed
        /// </summary>
        /// <param name="phContext">Handle of the context to query</param>
        /// <param name="phToken">Returned handle to the access token</param>
        [DllImport(Interop.Libraries.SspiCli, CharSet = CharSet.Auto)]
        internal static extern int QuerySecurityContextToken(ref SecurityHandle phContext, ref IntPtr phToken);

        /// <summary>
        /// Retrieves information about a specified security package
        /// </summary>
        /// <param name="packageName">Pointer to a null-terminated string that specifies the name of the security package</param>
        /// <param name="ppPackageInfo">Pointer to a variable that receives a pointer to a SecPkgInfo structure containing information about the specified security package</param>
        [DllImport(Interop.Libraries.SspiCli, CharSet = CharSet.Unicode, EntryPoint="QuerySecurityPackageInfoW")]
        internal static extern int QuerySecurityPackageInfo([MarshalAs(UnmanagedType.LPTStr)] string packageName, ref IntPtr ppPackageInfo);

        /// <summary>
        /// The FreeContextBuffer function enables callers of security package functions to free memory buffers allocated by the security package.
        /// </summary>
        /// <param name="pvContextBuffer">A pointer to memory to be freed</param>
        [DllImport(Interop.Libraries.SspiCli, CharSet = CharSet.Auto)]
        internal static extern int FreeContextBuffer(IntPtr pvContextBuffer);
    }
}
