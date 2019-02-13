// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Net
{
    internal static partial class Interop
    {
        // WININET.DLL errors propagated as HRESULT's using FACILITY=WIN32
        // as returned by the WinRT Windows.Web.Http APIs.  These HRESULT values
        // come from the Windows SDK winerror.h file.  These values are set into
        // the Exception.HResult property.
        //
        // NOTE: Although .NET Core does not use wininet, .NET Native does and some
        // sources are shared between the two. Therefore although this may appear to
        // be dead code--note the lack of references to wininet elsewhere--it is not.

        // No more Internet handles can be allocated
        public const int WININET_E_OUT_OF_HANDLES = unchecked((int)0x80072EE1);

        // The operation timed out
        public const int WININET_E_TIMEOUT = unchecked((int)0x80072EE2);

        // The server returned extended information
        public const int WININET_E_EXTENDED_ERROR = unchecked((int)0x80072EE3);

        // An internal error occurred in the Microsoft Internet extensions
        public const int WININET_E_INTERNAL_ERROR = unchecked((int)0x80072EE4);

        // The URL is invalid
        public const int WININET_E_INVALID_URL = unchecked((int)0x80072EE5);

        // The URL does not use a recognized protocol
        public const int WININET_E_UNRECOGNIZED_SCHEME = unchecked((int)0x80072EE6);

        // The server name or address could not be resolved
        public const int WININET_E_NAME_NOT_RESOLVED = unchecked((int)0x80072EE7);

        // A protocol with the required capabilities was not found
        public const int WININET_E_PROTOCOL_NOT_FOUND = unchecked((int)0x80072EE8);

        // The option is invalid
        public const int WININET_E_INVALID_OPTION = unchecked((int)0x80072EE9);

        // The length is incorrect for the option type
        public const int WININET_E_BAD_OPTION_LENGTH = unchecked((int)0x80072EEA);

        // The option value cannot be set
        public const int WININET_E_OPTION_NOT_SETTABLE = unchecked((int)0x80072EEB);

        // Microsoft Internet Extension support has been shut down
        public const int WININET_E_SHUTDOWN = unchecked((int)0x80072EEC);

        // The user name was not allowed
        public const int WININET_E_INCORRECT_USER_NAME = unchecked((int)0x80072EED);

        // The password was not allowed
        public const int WININET_E_INCORRECT_PASSWORD = unchecked((int)0x80072EEE);

        // The login request was denied
        public const int WININET_E_LOGIN_FAILURE = unchecked((int)0x80072EEF);

        // The requested operation is invalid
        public const int WININET_E_INVALID_OPERATION = unchecked((int)0x80072EF0);

        // The operation has been canceled
        public const int WININET_E_OPERATION_CANCELLED = unchecked((int)0x80072EF1);

        // The supplied handle is the wrong type for the requested operation
        public const int WININET_E_INCORRECT_HANDLE_TYPE = unchecked((int)0x80072EF2);

        // The handle is in the wrong state for the requested operation
        public const int WININET_E_INCORRECT_HANDLE_STATE = unchecked((int)0x80072EF3);

        // The request cannot be made on a Proxy session
        public const int WININET_E_NOT_PROXY_REQUEST = unchecked((int)0x80072EF4);

        // The registry value could not be found
        public const int WININET_E_REGISTRY_VALUE_NOT_FOUND = unchecked((int)0x80072EF5);

        // The registry parameter is incorrect
        public const int WININET_E_BAD_REGISTRY_PARAMETER = unchecked((int)0x80072EF6);

        // Direct Internet access is not available
        public const int WININET_E_NO_DIRECT_ACCESS = unchecked((int)0x80072EF7);

        // No context value was supplied
        public const int WININET_E_NO_CONTEXT = unchecked((int)0x80072EF8);

        // No status callback was supplied
        public const int WININET_E_NO_CALLBACK = unchecked((int)0x80072EF9);

        // There are outstanding requests
        public const int WININET_E_REQUEST_PENDING = unchecked((int)0x80072EFA);

        // The information format is incorrect
        public const int WININET_E_INCORRECT_FORMAT = unchecked((int)0x80072EFB);

        // The requested item could not be found
        public const int WININET_E_ITEM_NOT_FOUND = unchecked((int)0x80072EFC);

        // A connection with the server could not be established
        public const int WININET_E_CANNOT_CONNECT = unchecked((int)0x80072EFD);

        // The connection with the server was terminated abnormally
        public const int WININET_E_CONNECTION_ABORTED = unchecked((int)0x80072EFE);

        // The connection with the server was reset
        public const int WININET_E_CONNECTION_RESET = unchecked((int)0x80072EFF);

        // The action must be retried
        public const int WININET_E_FORCE_RETRY = unchecked((int)0x80072F00);

        // The proxy request is invalid
        public const int WININET_E_INVALID_PROXY_REQUEST = unchecked((int)0x80072F01);

        // User interaction is required to complete the operation
        public const int WININET_E_NEED_UI = unchecked((int)0x80072F02);

        // The handle already exists
        public const int WININET_E_HANDLE_EXISTS = unchecked((int)0x80072F04);

        // The date in the certificate is invalid or has expired
        public const int WININET_E_SEC_CERT_DATE_INVALID = unchecked((int)0x80072F05);

        // The host name in the certificate is invalid or does not match
        public const int WININET_E_SEC_CERT_CN_INVALID = unchecked((int)0x80072F06);

        // A redirect request will change a non-secure to a secure connection
        public const int WININET_E_HTTP_TO_HTTPS_ON_REDIR = unchecked((int)0x80072F07);

        // A redirect request will change a secure to a non-secure connection
        public const int WININET_E_HTTPS_TO_HTTP_ON_REDIR = unchecked((int)0x80072F08);

        // Mixed secure and non-secure connections
        public const int WININET_E_MIXED_SECURITY = unchecked((int)0x80072F09);

        // Changing to non-secure post
        public const int WININET_E_CHG_POST_IS_NON_SECURE = unchecked((int)0x80072F0A);

        // Data is being posted on a non-secure connection
        public const int WININET_E_POST_IS_NON_SECURE = unchecked((int)0x80072F0B);

        // A certificate is required to complete client authentication
        public const int WININET_E_CLIENT_AUTH_CERT_NEEDED = unchecked((int)0x80072F0C);

        // The certificate authority is invalid or incorrect
        public const int WININET_E_INVALID_CA = unchecked((int)0x80072F0D);

        // Client authentication has not been correctly installed
        public const int WININET_E_CLIENT_AUTH_NOT_SETUP = unchecked((int)0x80072F0E);

        // An error has occurred in a Wininet asynchronous thread. You may need to restart
        public const int WININET_E_ASYNC_THREAD_FAILED = unchecked((int)0x80072F0F);

        // The protocol scheme has changed during a redirect operation
        public const int WININET_E_REDIRECT_SCHEME_CHANGE = unchecked((int)0x80072F10);

        // There are operations awaiting retry
        public const int WININET_E_DIALOG_PENDING = unchecked((int)0x80072F11);

        // The operation must be retried
        public const int WININET_E_RETRY_DIALOG = unchecked((int)0x80072F12);

        // There are no new cache containers
        public const int WININET_E_NO_NEW_CONTAINERS = unchecked((int)0x80072F13);

        // A security zone check indicates the operation must be retried
        public const int WININET_E_HTTPS_HTTP_SUBMIT_REDIR = unchecked((int)0x80072F14);

        // The SSL certificate contains errors.
        public const int WININET_E_SEC_CERT_ERRORS = unchecked((int)0x80072F17);

        // It was not possible to connect to the revocation server or a definitive response could not be obtained.
        public const int WININET_E_SEC_CERT_REV_FAILED = unchecked((int)0x80072F19);

        // The requested header was not found
        public const int WININET_E_HEADER_NOT_FOUND = unchecked((int)0x80072F76);

        // The server does not support the requested protocol level
        public const int WININET_E_DOWNLEVEL_SERVER = unchecked((int)0x80072F77);

        // The server returned an invalid or unrecognized response
        public const int WININET_E_INVALID_SERVER_RESPONSE = unchecked((int)0x80072F78);

        // The supplied HTTP header is invalid
        public const int WININET_E_INVALID_HEADER = unchecked((int)0x80072F79);

        // The request for a HTTP header is invalid
        public const int WININET_E_INVALID_QUERY_REQUEST = unchecked((int)0x80072F7A);

        // The HTTP header already exists
        public const int WININET_E_HEADER_ALREADY_EXISTS = unchecked((int)0x80072F7B);

        // The HTTP redirect request failed
        public const int WININET_E_REDIRECT_FAILED = unchecked((int)0x80072F7C);

        // An error occurred in the secure channel support
        public const int WININET_E_SECURITY_CHANNEL_ERROR = unchecked((int)0x80072F7D);

        // The file could not be written to the cache
        public const int WININET_E_UNABLE_TO_CACHE_FILE = unchecked((int)0x80072F7E);

        // The TCP/IP protocol is not installed properly
        public const int WININET_E_TCPIP_NOT_INSTALLED = unchecked((int)0x80072F7F);

        // The computer is disconnected from the network
        public const int WININET_E_DISCONNECTED = unchecked((int)0x80072F83);

        // The server is unreachable
        public const int WININET_E_SERVER_UNREACHABLE = unchecked((int)0x80072F84);

        // The proxy server is unreachable
        public const int WININET_E_PROXY_SERVER_UNREACHABLE = unchecked((int)0x80072F85);

        // The proxy auto-configuration script is in error
        public const int WININET_E_BAD_AUTO_PROXY_SCRIPT = unchecked((int)0x80072F86);

        // Could not download the proxy auto-configuration script file
        public const int WININET_E_UNABLE_TO_DOWNLOAD_SCRIPT = unchecked((int)0x80072F87);

        // The supplied certificate is invalid
        public const int WININET_E_SEC_INVALID_CERT = unchecked((int)0x80072F89);

        // The supplied certificate has been revoked
        public const int WININET_E_SEC_CERT_REVOKED = unchecked((int)0x80072F8A);

        // The Dialup failed because file sharing was turned on and a failure was requested if security check was needed
        public const int WININET_E_FAILED_DUETOSECURITYCHECK = unchecked((int)0x80072F8B);

        // Initialization of the WinINet API has not occurred
        public const int WININET_E_NOT_INITIALIZED = unchecked((int)0x80072F8C);

        // Login failed and the client should display the entity body to the user
        public const int WININET_E_LOGIN_FAILURE_DISPLAY_ENTITY_BODY = unchecked((int)0x80072F8E);

        // Content decoding has failed
        public const int WININET_E_DECODING_FAILED = unchecked((int)0x80072F8F);

        // The HTTP request was not redirected
        public const int WININET_E_NOT_REDIRECTED = unchecked((int)0x80072F80);

        // A cookie from the server must be confirmed by the user
        public const int WININET_E_COOKIE_NEEDS_CONFIRMATION = unchecked((int)0x80072F81);

        // A cookie from the server has been declined acceptance
        public const int WININET_E_COOKIE_DECLINED = unchecked((int)0x80072F82);

        // The HTTP redirect request must be confirmed by the user
        public const int WININET_E_REDIRECT_NEEDS_CONFIRMATION = unchecked((int)0x80072F88);
    }
}
