// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Security.Principal;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.IO;
using System.Diagnostics.CodeAnalysis;

namespace System.DirectoryServices.ActiveDirectory
{
    public enum DirectoryContextType
    {
        Domain = 0,
        Forest = 1,
        DirectoryServer = 2,
        ConfigurationSet = 3,
        ApplicationPartition = 4
    }

    public class DirectoryContext
    {
        private string? _name;
        private DirectoryContextType _contextType;
        private NetworkCredential _credential;
        internal string? serverName;
        internal bool usernameIsNull;
        internal bool passwordIsNull;
        private bool _validated;
        private bool _contextIsValid;

        internal static LoadLibrarySafeHandle ADHandle;
        internal static LoadLibrarySafeHandle ADAMHandle;

        #region constructors

        static DirectoryContext()
        {
            // load ntdsapi.dll for AD and ADAM
            GetLibraryHandle();
        }

        // Internal Constructors
        [MemberNotNull(nameof(_credential))]
        internal void InitializeDirectoryContext(DirectoryContextType contextType, string? name, string? username, string? password)
        {
            _name = name;
            _contextType = contextType;
            _credential = new NetworkCredential(username, password);
            if (username == null)
            {
                usernameIsNull = true;
            }
            if (password == null)
            {
                passwordIsNull = true;
            }
        }

        internal DirectoryContext(DirectoryContextType contextType, string? name, DirectoryContext? context)
        {
            _name = name;
            _contextType = contextType;

            if (context != null)
            {
                _credential = context.Credential;
                this.usernameIsNull = context.usernameIsNull;
                this.passwordIsNull = context.passwordIsNull;
            }
            else
            {
                _credential = new NetworkCredential(null, "", null);
                this.usernameIsNull = true;
                this.passwordIsNull = true;
            }
        }

        internal DirectoryContext(DirectoryContext context)
        {
            _name = context.Name;
            _contextType = context.ContextType;
            _credential = context.Credential;
            this.usernameIsNull = context.usernameIsNull;
            this.passwordIsNull = context.passwordIsNull;
            if (context.ContextType != DirectoryContextType.ConfigurationSet)
            {
                //
                // only for configurationset, we select a server, so we should not copy over that
                // information, for all other types, this is either the same as name of the target or if the target is netbios name
                // (for domain and forest) it could be the dns name. We should copy over this information.
                //
                this.serverName = context.serverName;
            }
        }
        #endregion constructors

        #region public constructors

        public DirectoryContext(DirectoryContextType contextType)
        {
            //
            // this constructor can only be called for DirectoryContextType.Forest or DirectoryContextType.Domain
            // since all other types require the name to be specified
            //
            if (contextType != DirectoryContextType.Domain && contextType != DirectoryContextType.Forest)
            {
                throw new ArgumentException(SR.OnlyDomainOrForest, nameof(contextType));
            }

            InitializeDirectoryContext(contextType, null, null, null);
        }

        public DirectoryContext(DirectoryContextType contextType, string name)
        {
            if (contextType < DirectoryContextType.Domain || contextType > DirectoryContextType.ApplicationPartition)
            {
                throw new InvalidEnumArgumentException(nameof(contextType), (int)contextType, typeof(DirectoryContextType));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, nameof(name));
            }

            InitializeDirectoryContext(contextType, name, null, null);
        }

        public DirectoryContext(DirectoryContextType contextType, string? username, string? password)
        {
            //
            // this constructor can only be called for DirectoryContextType.Forest or DirectoryContextType.Domain
            // since all other types require the name to be specified
            //
            if (contextType != DirectoryContextType.Domain && contextType != DirectoryContextType.Forest)
            {
                throw new ArgumentException(SR.OnlyDomainOrForest, nameof(contextType));
            }

            InitializeDirectoryContext(contextType, null, username, password);
        }

        public DirectoryContext(DirectoryContextType contextType, string name, string? username, string? password)
        {
            if (contextType < DirectoryContextType.Domain || contextType > DirectoryContextType.ApplicationPartition)
            {
                throw new InvalidEnumArgumentException(nameof(contextType), (int)contextType, typeof(DirectoryContextType));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, nameof(name));
            }

            InitializeDirectoryContext(contextType, name, username, password);
        }

        #endregion public methods

        #region public properties

        public string? Name => _name;

        public string? UserName => usernameIsNull ? null : _credential.UserName;

        internal string? Password
        {
            get => passwordIsNull ? null : _credential.Password;
        }

        public DirectoryContextType ContextType => _contextType;

        internal NetworkCredential Credential => _credential;

        #endregion public properties

        #region private methods
        internal static bool IsContextValid(DirectoryContext context, DirectoryContextType contextType)
        {
            bool contextIsValid = false;

            if ((contextType == DirectoryContextType.Domain) || ((contextType == DirectoryContextType.Forest) && (context.Name == null)))
            {
                string? tmpTarget = context.Name;

                if (tmpTarget == null)
                {
                    // GetLoggedOnDomain returns the dns name of the logged on user's domain
                    context.serverName = GetLoggedOnDomain();
                    contextIsValid = true;
                }
                else
                {
                    // check for domain
                    int errorCode = 0;
                    DomainControllerInfo domainControllerInfo;
                    errorCode = Locator.DsGetDcNameWrapper(null, tmpTarget, null, (long)PrivateLocatorFlags.DirectoryServicesRequired, out domainControllerInfo);

                    if (errorCode == NativeMethods.ERROR_NO_SUCH_DOMAIN)
                    {
                        // try with force rediscovery

                        errorCode = Locator.DsGetDcNameWrapper(null, tmpTarget, null, (long)PrivateLocatorFlags.DirectoryServicesRequired | (long)LocatorOptions.ForceRediscovery, out domainControllerInfo);

                        if (errorCode == NativeMethods.ERROR_NO_SUCH_DOMAIN)
                        {
                            contextIsValid = false;
                        }
                        else if (errorCode != 0)
                        {
                            throw ExceptionHelper.GetExceptionFromErrorCode(errorCode);
                        }
                        else
                        {
                            Debug.Assert(domainControllerInfo != null);
                            Debug.Assert(domainControllerInfo.DomainName != null);
                            context.serverName = domainControllerInfo.DomainName;
                            contextIsValid = true;
                        }
                    }
                    else if (errorCode == NativeMethods.ERROR_INVALID_DOMAIN_NAME_FORMAT)
                    {
                        // we can get this error if the target it server:port (not a valid domain)
                        contextIsValid = false;
                    }
                    else if (errorCode != 0)
                    {
                        throw ExceptionHelper.GetExceptionFromErrorCode(errorCode);
                    }
                    else
                    {
                        Debug.Assert(domainControllerInfo != null);
                        Debug.Assert(domainControllerInfo.DomainName != null);
                        context.serverName = domainControllerInfo.DomainName;
                        contextIsValid = true;
                    }
                }
            }
            else if (contextType == DirectoryContextType.Forest)
            {
                Debug.Assert(context.Name != null);

                // check for forest
                int errorCode = 0;
                DomainControllerInfo domainControllerInfo;
                errorCode = Locator.DsGetDcNameWrapper(null, context.Name, null, (long)(PrivateLocatorFlags.GCRequired | PrivateLocatorFlags.DirectoryServicesRequired), out domainControllerInfo);

                if (errorCode == NativeMethods.ERROR_NO_SUCH_DOMAIN)
                {
                    // try with force rediscovery

                    errorCode = Locator.DsGetDcNameWrapper(null, context.Name, null, (long)((PrivateLocatorFlags.GCRequired | PrivateLocatorFlags.DirectoryServicesRequired)) | (long)LocatorOptions.ForceRediscovery, out domainControllerInfo);

                    if (errorCode == NativeMethods.ERROR_NO_SUCH_DOMAIN)
                    {
                        contextIsValid = false;
                    }
                    else if (errorCode != 0)
                    {
                        throw ExceptionHelper.GetExceptionFromErrorCode(errorCode);
                    }
                    else
                    {
                        Debug.Assert(domainControllerInfo != null);
                        Debug.Assert(domainControllerInfo.DnsForestName != null);
                        context.serverName = domainControllerInfo.DnsForestName;
                        contextIsValid = true;
                    }
                }
                else if (errorCode == NativeMethods.ERROR_INVALID_DOMAIN_NAME_FORMAT)
                {
                    // we can get this error if the target it server:port (not a valid forest)
                    contextIsValid = false;
                }
                else if (errorCode != 0)
                {
                    throw ExceptionHelper.GetExceptionFromErrorCode(errorCode);
                }
                else
                {
                    Debug.Assert(domainControllerInfo != null);
                    Debug.Assert(domainControllerInfo.DnsForestName != null);
                    context.serverName = domainControllerInfo.DnsForestName;
                    contextIsValid = true;
                }
            }
            else if (contextType == DirectoryContextType.ApplicationPartition)
            {
                Debug.Assert(context.Name != null);

                // check for application partition
                int errorCode = 0;
                DomainControllerInfo domainControllerInfo;
                errorCode = Locator.DsGetDcNameWrapper(null, context.Name, null, (long)PrivateLocatorFlags.OnlyLDAPNeeded, out domainControllerInfo);

                if (errorCode == NativeMethods.ERROR_NO_SUCH_DOMAIN)
                {
                    // try with force rediscovery

                    errorCode = Locator.DsGetDcNameWrapper(null, context.Name, null, (long)PrivateLocatorFlags.OnlyLDAPNeeded | (long)LocatorOptions.ForceRediscovery, out domainControllerInfo);

                    if (errorCode == NativeMethods.ERROR_NO_SUCH_DOMAIN)
                    {
                        contextIsValid = false;
                    }
                    else if (errorCode != 0)
                    {
                        throw ExceptionHelper.GetExceptionFromErrorCode(errorCode);
                    }
                    else
                    {
                        contextIsValid = true;
                    }
                }
                else if (errorCode == NativeMethods.ERROR_INVALID_DOMAIN_NAME_FORMAT)
                {
                    // we can get this error if the target it server:port (not a valid application partition)
                    contextIsValid = false;
                }
                else if (errorCode != 0)
                {
                    throw ExceptionHelper.GetExceptionFromErrorCode(errorCode);
                }
                else
                {
                    contextIsValid = true;
                }
            }
            else if (contextType == DirectoryContextType.DirectoryServer)
            {
                //
                // if the servername contains a port number, then remove that
                //
                string? tempServerName = null;
                string? portNumber;
                tempServerName = Utils.SplitServerNameAndPortNumber(context.Name!, out portNumber);

                //
                // this will validate that the name specified in the context is truely the name of a machine (and not of a domain)
                //
                DirectoryEntry de = new DirectoryEntry("WinNT://" + tempServerName + ",computer", context.UserName, context.Password, Utils.DefaultAuthType);
                try
                {
                    de.Bind(true);
                    contextIsValid = true;
                }
                catch (COMException e)
                {
                    if ((e.ErrorCode == unchecked((int)0x80070035)) || (e.ErrorCode == unchecked((int)0x80070033)) || (e.ErrorCode == unchecked((int)0x80005000)))
                    {
                        // if this returns bad network path
                        contextIsValid = false;
                    }
                    else
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                }
                finally
                {
                    de.Dispose();
                }
            }
            else
            {
                // no special validation for ConfigurationSet
                contextIsValid = true;
            }

            return contextIsValid;
        }

        internal bool isRootDomain()
        {
            if (_contextType != DirectoryContextType.Forest)
                return false;

            if (!_validated)
            {
                _contextIsValid = IsContextValid(this, DirectoryContextType.Forest);
                _validated = true;
            }
            return _contextIsValid;
        }

        internal bool isDomain()
        {
            if (_contextType != DirectoryContextType.Domain)
                return false;

            if (!_validated)
            {
                _contextIsValid = IsContextValid(this, DirectoryContextType.Domain);
                _validated = true;
            }
            return _contextIsValid;
        }

        internal bool isNdnc()
        {
            if (_contextType != DirectoryContextType.ApplicationPartition)
                return false;

            if (!_validated)
            {
                _contextIsValid = IsContextValid(this, DirectoryContextType.ApplicationPartition);
                _validated = true;
            }
            return _contextIsValid;
        }

        internal bool isServer()
        {
            if (_contextType != DirectoryContextType.DirectoryServer)
                return false;

            if (!_validated)
            {
                _contextIsValid = IsContextValid(this, DirectoryContextType.DirectoryServer);
                _validated = true;
            }
            return _contextIsValid;
        }

        internal bool isADAMConfigSet()
        {
            if (_contextType != DirectoryContextType.ConfigurationSet)
                return false;

            if (!_validated)
            {
                _contextIsValid = IsContextValid(this, DirectoryContextType.ConfigurationSet);
                _validated = true;
            }
            return _contextIsValid;
        }

        //
        // this method is called when the forest name is explicitly specified
        // and we want to check if that matches the current logged on forest
        //
        internal bool isCurrentForest()
        {
            bool result = false;

            Debug.Assert(_name != null);
            DomainControllerInfo domainControllerInfo = Locator.GetDomainControllerInfo(null, _name, null, (long)(PrivateLocatorFlags.DirectoryServicesRequired | PrivateLocatorFlags.ReturnDNSName));

            DomainControllerInfo currentDomainControllerInfo;
            string loggedOnDomain = GetLoggedOnDomain();

            int errorCode = Locator.DsGetDcNameWrapper(null, loggedOnDomain, null, (long)(PrivateLocatorFlags.DirectoryServicesRequired | PrivateLocatorFlags.ReturnDNSName), out currentDomainControllerInfo);

            if (errorCode == 0)
            {
                Debug.Assert(domainControllerInfo.DnsForestName != null);
                Debug.Assert(currentDomainControllerInfo.DnsForestName != null);

                result = (Utils.Compare(domainControllerInfo.DnsForestName, currentDomainControllerInfo.DnsForestName) == 0);
            }
            //
            // if there is no forest associated with the logged on domain, then we return false
            //
            else if (errorCode != NativeMethods.ERROR_NO_SUCH_DOMAIN)
            {
                throw ExceptionHelper.GetExceptionFromErrorCode(errorCode);
            }

            return result;
        }

        internal bool useServerBind()
        {
            return ((ContextType == DirectoryContextType.DirectoryServer) || (ContextType == DirectoryContextType.ConfigurationSet));
        }

        internal string? GetServerName()
        {
            if (serverName == null)
            {
                switch (_contextType)
                {
                    case DirectoryContextType.ConfigurationSet:
                        {
                            AdamInstance adamInst = ConfigurationSet.FindAnyAdamInstance(this);
                            try
                            {
                                serverName = adamInst.Name;
                            }
                            finally
                            {
                                adamInst.Dispose();
                            }
                            break;
                        }
                    case DirectoryContextType.Domain:
                    case DirectoryContextType.Forest:
                        {
                            //
                            // if the target is not specified OR
                            // if the forest name was explicitly specified and the forest is the same as the current forest
                            // we want to find a DC in the current domain
                            //
                            if ((_name == null) || ((_contextType == DirectoryContextType.Forest) && (isCurrentForest())))
                            {
                                serverName = GetLoggedOnDomain();
                            }
                            else
                            {
                                serverName = GetDnsDomainName(_name);
                            }
                            break;
                        }
                    case DirectoryContextType.ApplicationPartition:
                        {
                            // if this is an appNC the target should not be null
                            Debug.Assert(_name != null);

                            serverName = _name;
                            break;
                        }
                    case DirectoryContextType.DirectoryServer:
                        {
                            // this should not happen (We should have checks for this earlier itself)
                            Debug.Assert(_name != null);
                            serverName = _name;
                            break;
                        }
                    default:
                        {
                            Debug.Fail("DirectoryContext::GetServerName - Unknown contextType");
                            break;
                        }
                }
            }

            return serverName;
        }

        internal static string GetLoggedOnDomain()
        {
            string? domainName = null;

            NegotiateCallerNameRequest requestBuffer = new NegotiateCallerNameRequest();
            int requestBufferLength = (int)Marshal.SizeOf(requestBuffer);

            IntPtr pResponseBuffer = IntPtr.Zero;
            NegotiateCallerNameResponse responseBuffer = new NegotiateCallerNameResponse();
            int responseBufferLength;
            int protocolStatus;
            int result;

            LsaLogonProcessSafeHandle lsaHandle;

            //
            // since we are using safe handles, we don't need to explicitly call NativeMethods.LsaDeregisterLogonProcess(lsaHandle)
            //
            result = NativeMethods.LsaConnectUntrusted(out lsaHandle);

            if (result == 0)
            {
                //
                // initialize the request buffer
                //
                requestBuffer.messageType = NativeMethods.NegGetCallerName;

                result = NativeMethods.LsaCallAuthenticationPackage(lsaHandle, 0, requestBuffer, requestBufferLength, out pResponseBuffer, out responseBufferLength, out protocolStatus);

                try
                {
                    if (result == 0 && protocolStatus == 0)
                    {
                        Marshal.PtrToStructure(pResponseBuffer, responseBuffer);

                        //
                        // callerName is of the form domain\username
                        //
                        Debug.Assert((responseBuffer.callerName != null), "NativeMethods.LsaCallAuthenticationPackage returned null callerName.");
                        int index = responseBuffer.callerName.IndexOf('\\');
                        Debug.Assert((index != -1), "NativeMethods.LsaCallAuthenticationPackage returned callerName not in domain\\username format.");
                        domainName = responseBuffer.callerName.Substring(0, index);
                    }
                    else
                    {
                        if (result == NativeMethods.STATUS_QUOTA_EXCEEDED)
                        {
                            throw new OutOfMemoryException();
                        }
                        else if ((result == 0) && (UnsafeNativeMethods.LsaNtStatusToWinError(protocolStatus) == NativeMethods.ERROR_NO_SUCH_LOGON_SESSION))
                        {
                            // If this is a directory user, extract domain info from username
                            if (!Utils.IsSamUser())
                            {
                                WindowsIdentity identity = WindowsIdentity.GetCurrent();

                                int index = identity.Name.IndexOf('\\');
                                Debug.Assert(index != -1);
                                domainName = identity.Name.Substring(0, index);
                            }
                        }
                        else
                        {
                            throw ExceptionHelper.GetExceptionFromErrorCode(UnsafeNativeMethods.LsaNtStatusToWinError((result != 0) ? result : protocolStatus));
                        }
                    }
                }
                finally
                {
                    if (pResponseBuffer != IntPtr.Zero)
                    {
                        NativeMethods.LsaFreeReturnBuffer(pResponseBuffer);
                    }
                }
            }
            else if (result == NativeMethods.STATUS_QUOTA_EXCEEDED)
            {
                throw new OutOfMemoryException();
            }
            else
            {
                throw ExceptionHelper.GetExceptionFromErrorCode(UnsafeNativeMethods.LsaNtStatusToWinError(result));
            }

            // If we're running as a local user (i.e. NT AUTHORITY\LOCAL SYSTEM, IIS APPPOOL\APPPoolIdentity, etc.),
            // domainName will be null and we fall back to the machine's domain
            domainName = GetDnsDomainName(domainName);

            if (domainName == null)
            {
                //
                // we should never get to this point here since we should have already verified that the context is valid
                // by the time we get to this point
                //
                throw new ActiveDirectoryOperationException(SR.ContextNotAssociatedWithDomain);
            }

            return domainName;
        }

        internal static string? GetDnsDomainName(string? domainName)
        {
            DomainControllerInfo domainControllerInfo;
            int errorCode = 0;

            //
            // Locator.DsGetDcNameWrapper internally passes the ReturnDNSName flag when calling DsGetDcName
            //
            errorCode = Locator.DsGetDcNameWrapper(null, domainName, null, (long)PrivateLocatorFlags.DirectoryServicesRequired, out domainControllerInfo);
            if (errorCode == NativeMethods.ERROR_NO_SUCH_DOMAIN)
            {
                // try again with force rediscovery
                errorCode = Locator.DsGetDcNameWrapper(null, domainName, null, (long)((long)PrivateLocatorFlags.DirectoryServicesRequired | (long)LocatorOptions.ForceRediscovery), out domainControllerInfo);
                if (errorCode == NativeMethods.ERROR_NO_SUCH_DOMAIN)
                {
                    return null;
                }
                else if (errorCode != 0)
                {
                    throw ExceptionHelper.GetExceptionFromErrorCode(errorCode);
                }
            }
            else if (errorCode != 0)
            {
                throw ExceptionHelper.GetExceptionFromErrorCode(errorCode);
            }

            Debug.Assert(domainControllerInfo != null);
            Debug.Assert(domainControllerInfo.DomainName != null);

            return domainControllerInfo.DomainName;
        }

        [MemberNotNull(nameof(ADHandle))]
        [MemberNotNull(nameof(ADAMHandle))]
        private static void GetLibraryHandle()
        {
            // first get AD handle
            string systemPath = Environment.SystemDirectory;
            IntPtr tempHandle = UnsafeNativeMethods.LoadLibrary(systemPath + "\\ntdsapi.dll");
            if (tempHandle == (IntPtr)0)
            {
                throw ExceptionHelper.GetExceptionFromErrorCode(Marshal.GetLastWin32Error());
            }
            else
            {
                ADHandle = new LoadLibrarySafeHandle(tempHandle);
            }

            // not get the ADAM handle
            // got to the windows\adam directory
            DirectoryInfo windowsDirectory = Directory.GetParent(systemPath)!;
            tempHandle = UnsafeNativeMethods.LoadLibrary(windowsDirectory.FullName + "\\ADAM\\ntdsapi.dll");
            if (tempHandle == (IntPtr)0)
            {
                ADAMHandle = ADHandle;
            }
            else
            {
                ADAMHandle = new LoadLibrarySafeHandle(tempHandle);
            }
        }

        #endregion private methods
    }
}
