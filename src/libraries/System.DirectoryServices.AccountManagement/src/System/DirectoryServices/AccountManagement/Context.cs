// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Configuration;
using System.DirectoryServices.Protocols;
using System.DirectoryServices;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections;

namespace System.DirectoryServices.AccountManagement
{
    internal struct ServerProperties
    {
        public string dnsHostName;
        public DomainControllerMode OsVersion;
        public ContextType contextType;
        public string[] SupportCapabilities;
        public int portSSL;
        public int portLDAP;
    };

    internal enum DomainControllerMode
    {
        Win2k = 0,
        Win2k3 = 2,
        WinLH = 3
    };

    internal static class CapabilityMap
    {
        public const string LDAP_CAP_ACTIVE_DIRECTORY_OID = "1.2.840.113556.1.4.800";
        public const string LDAP_CAP_ACTIVE_DIRECTORY_V51_OID = "1.2.840.113556.1.4.1670";
        public const string LDAP_CAP_ACTIVE_DIRECTORY_LDAP_INTEG_OID = "1.2.840.113556.1.4.1791";
        public const string LDAP_CAP_ACTIVE_DIRECTORY_ADAM_OID = "1.2.840.113556.1.4.1851";
        public const string LDAP_CAP_ACTIVE_DIRECTORY_PARTIAL_SECRETS_OID = "1.2.840.113556.1.4.1920";
        public const string LDAP_CAP_ACTIVE_DIRECTORY_V61_OID = "1.2.840.113556.1.4.1935";
    }

    internal sealed class CredentialValidator : IDisposable
    {
        private enum AuthMethod
        {
            Simple = 1,
            Negotiate = 2
        }

        private bool _fastConcurrentSupported = true;

        private readonly Hashtable _connCache = new Hashtable(4);
        private LdapDirectoryIdentifier _directoryIdent;
        private readonly object _cacheLock = new object();

        private AuthMethod _lastBindMethod = AuthMethod.Simple;
        private readonly string _serverName;
        private readonly ContextType _contextType;
        private ServerProperties _serverProperties;

        private const ContextOptions defaultContextOptionsNegotiate = ContextOptions.Signing | ContextOptions.Sealing | ContextOptions.Negotiate;
        private const ContextOptions defaultContextOptionsSimple = ContextOptions.SecureSocketLayer | ContextOptions.SimpleBind;

        public CredentialValidator(ContextType contextType, string serverName, ServerProperties serverProperties)
        {
            _fastConcurrentSupported = !(serverProperties.OsVersion == DomainControllerMode.Win2k);

            if (contextType == ContextType.Machine && serverName == null)
            {
                _serverName = Environment.MachineName;
            }
            else
            {
                _serverName = serverName;
            }

            _contextType = contextType;
            _serverProperties = serverProperties;
        }

        private bool BindSam(string target, string userName, string password)
        {
            string adsPath = $"WinNT://{_serverName},computer";
            Guid g = new Guid("fd8256d0-fd15-11ce-abc4-02608c9e7553"); // IID_IUnknown
            object value = null;
            // always attempt secure auth..
            int authenticationType = 1;
            object unmanagedResult = null;

            try
            {
                if (Thread.CurrentThread.GetApartmentState() == ApartmentState.Unknown)
                    Thread.CurrentThread.SetApartmentState(ApartmentState.MTA);
                // We need the credentials to be in the form <machine>\\<user>
                // if they just passed user then append the machine name here.
                if (null != userName)
                {
                    int index = userName.IndexOf('\\');
                    if (index == -1)
                    {
                        userName = _serverName + "\\" + userName;
                    }
                }

                int hr = UnsafeNativeMethods.ADsOpenObject(adsPath, userName, password, (int)authenticationType, ref g, out value);

                if (hr != 0)
                {
                    if (hr == unchecked((int)(ExceptionHelper.ERROR_HRESULT_LOGON_FAILURE)))
                    {
                        // This is the invalid credetials case.  We want to return false
                        // instead of throwing an exception
                        return false;
                    }
                    else
                    {
                        throw ExceptionHelper.GetExceptionFromErrorCode(hr);
                    }
                }

                unmanagedResult = ((UnsafeNativeMethods.IADs)value).Get("name");
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                if (e.ErrorCode == unchecked((int)(ExceptionHelper.ERROR_HRESULT_LOGON_FAILURE)))
                {
                    return false;
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(e);
                }
            }
            finally
            {
                if (value != null)
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(value);
            }

            return true;
        }

        private bool BindLdap(NetworkCredential creds, ContextOptions contextOptions)
        {
            LdapConnection current = null;
            bool useSSL = (ContextOptions.SecureSocketLayer & contextOptions) > 0;

            if (_contextType == ContextType.ApplicationDirectory)
            {
                _directoryIdent = new LdapDirectoryIdentifier(_serverProperties.dnsHostName, useSSL ? _serverProperties.portSSL : _serverProperties.portLDAP);
            }
            else
            {
                _directoryIdent = new LdapDirectoryIdentifier(_serverName, useSSL ? LdapConstants.LDAP_SSL_PORT : LdapConstants.LDAP_PORT);
            }

            bool attemptFastConcurrent = useSSL && _fastConcurrentSupported;
            int index = Convert.ToInt32(attemptFastConcurrent) * 2 + Convert.ToInt32(useSSL);

            if (!_connCache.Contains(index))
            {
                lock (_cacheLock)
                {
                    if (!_connCache.Contains(index))
                    {
                        current = new LdapConnection(_directoryIdent);
                        // First attempt to turn on SSL
                        current.SessionOptions.SecureSocketLayer = useSSL;

                        if (attemptFastConcurrent)
                        {
                            try
                            {
                                current.SessionOptions.FastConcurrentBind();
                            }
                            catch (PlatformNotSupportedException)
                            {
                                current.Dispose();
                                current = null;
                                _fastConcurrentSupported = false;
                                index = Convert.ToInt32(useSSL);
                                current = new LdapConnection(_directoryIdent);
                                // We have fallen back to another connection so we need to set SSL again.
                                current.SessionOptions.SecureSocketLayer = useSSL;
                            }
                        }

                        _connCache.Add(index, current);
                    }
                    else
                    {
                        current = (LdapConnection)_connCache[index];
                    }
                }
            }
            else
            {
                current = (LdapConnection)_connCache[index];
            }

            // If we are performing fastConcurrentBind there is no need to prevent multithreadaccess.  FSB is thread safe and multi cred safe
            // FSB also always has the same contextoptions so there is no need to lock the code that is modifying the current connection
            if (attemptFastConcurrent && _fastConcurrentSupported)
            {
                lockedLdapBind(current, creds, contextOptions);
            }
            else
            {
                lock (_cacheLock)
                {
                    lockedLdapBind(current, creds, contextOptions);
                }
            }
            return true;
        }

        private void lockedLdapBind(LdapConnection current, NetworkCredential creds, ContextOptions contextOptions)
        {
            current.AuthType = ((ContextOptions.SimpleBind & contextOptions) > 0 ? AuthType.Basic : AuthType.Negotiate);
            current.SessionOptions.Signing = ((ContextOptions.Signing & contextOptions) > 0 ? true : false);
            current.SessionOptions.Sealing = ((ContextOptions.Sealing & contextOptions) > 0 ? true : false);
            if ((null == creds.UserName) && (null == creds.Password))
            {
                current.Bind();
            }
            else
            {
                current.Bind(creds);
            }
        }

        public bool Validate(string userName, string password)
        {
            NetworkCredential networkCredential = new NetworkCredential(userName, password);

            // empty username and password on the local box
            // causes authentication to succeed.  If the username is empty we should just fail it
            // here.
            if (userName != null && userName.Length == 0)
                return false;

            if (_contextType == ContextType.Domain || _contextType == ContextType.ApplicationDirectory)
            {
                try
                {
                    if (_lastBindMethod == AuthMethod.Simple && (_fastConcurrentSupported || _contextType == ContextType.ApplicationDirectory))
                    {
                        try
                        {
                            BindLdap(networkCredential, defaultContextOptionsSimple);
                            _lastBindMethod = AuthMethod.Simple;
                            return true;
                        }
                        catch (LdapException)
                        {
                            // we don't return false here even if we failed with ERROR_LOGON_FAILURE. We must check Negotiate
                            // because there might be cases in which SSL fails and Negotiate succeeds
                        }
                        catch (DirectoryOperationException)
                        {
                            // see LdapException
                        }

                        BindLdap(networkCredential, defaultContextOptionsNegotiate);
                        _lastBindMethod = AuthMethod.Negotiate;
                        return true;
                    }
                    else
                    {
                        try
                        {
                            BindLdap(networkCredential, defaultContextOptionsNegotiate);
                            _lastBindMethod = AuthMethod.Negotiate;
                            return true;
                        }
                        catch (LdapException)
                        {
                            // we don't return false here even if we failed with ERROR_LOGON_FAILURE. We must check SSL
                            // because there might be cases in which Negotiate fails and SSL succeeds
                        }

                        BindLdap(networkCredential, defaultContextOptionsSimple);
                        _lastBindMethod = AuthMethod.Simple;
                        return true;
                    }
                }
                catch (LdapException ldapex)
                {
                    // If we got here it means that both SSL and Negotiate failed. Tough luck.
                    if (ldapex.ErrorCode == ExceptionHelper.ERROR_LOGON_FAILURE)
                    {
                        return false;
                    }

                    throw;
                }
            }
            else
            {
                Debug.Assert(_contextType == ContextType.Machine);
                return (BindSam(_serverName, userName, password));
            }
        }

        public bool Validate(string userName, string password, ContextOptions connectionMethod)
        {
            // empty username and password on the local box
            // causes authentication to succeed.  If the username is empty we should just fail it
            // here.
            if (userName != null && userName.Length == 0)
                return false;

            if (_contextType == ContextType.Domain || _contextType == ContextType.ApplicationDirectory)
            {
                try
                {
                    NetworkCredential networkCredential = new NetworkCredential(userName, password);
                    BindLdap(networkCredential, connectionMethod);
                    return true;
                }
                catch (LdapException ldapex)
                {
                    if (ldapex.ErrorCode == ExceptionHelper.ERROR_LOGON_FAILURE)
                    {
                        return false;
                    }

                    throw;
                }
            }
            else
            {
                return (BindSam(_serverName, userName, password));
            }
        }

        public void Dispose()
        {
            foreach (LdapConnection connection in _connCache.Values)
            {
                connection.Dispose();
            }
        }
    }
    // ********************************************
    public class PrincipalContext : IDisposable
    {
        //
        // Public Constructors
        //

        public PrincipalContext(ContextType contextType) :
            this(contextType, null, null, PrincipalContext.GetDefaultOptionForStore(contextType), null, null)
        { }

        public PrincipalContext(ContextType contextType, string name) :
            this(contextType, name, null, PrincipalContext.GetDefaultOptionForStore(contextType), null, null)
        { }

        public PrincipalContext(ContextType contextType, string name, string container) :
            this(contextType, name, container, PrincipalContext.GetDefaultOptionForStore(contextType), null, null)
        { }

        public PrincipalContext(ContextType contextType, string name, string container, ContextOptions options) :
            this(contextType, name, container, options, null, null)
        { }

        public PrincipalContext(ContextType contextType, string name, string userName, string password) :
            this(contextType, name, null, PrincipalContext.GetDefaultOptionForStore(contextType), userName, password)
        { }

        public PrincipalContext(ContextType contextType, string name, string container, string userName, string password) :
            this(contextType, name, container, PrincipalContext.GetDefaultOptionForStore(contextType), userName, password)
        { }

        public PrincipalContext(
                    ContextType contextType, string name, string container, ContextOptions options, string userName, string password)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "Entering ctor");

            if ((userName == null && password != null) ||
                (userName != null && password == null))
                throw new ArgumentException(SR.ContextBadUserPwdCombo);

            if ((options & ~(ContextOptions.Signing | ContextOptions.Negotiate | ContextOptions.Sealing | ContextOptions.SecureSocketLayer | ContextOptions.SimpleBind | ContextOptions.ServerBind)) != 0)
                throw new InvalidEnumArgumentException(nameof(options), (int)options, typeof(ContextOptions));

            if (contextType == ContextType.Machine && ((options & ~ContextOptions.Negotiate) != 0))
            {
                throw new ArgumentException(SR.InvalidContextOptionsForMachine);
            }

            if ((contextType == ContextType.Domain || contextType == ContextType.ApplicationDirectory) &&
                (((options & (ContextOptions.Negotiate | ContextOptions.SimpleBind)) == 0) ||
                (((options & (ContextOptions.Negotiate | ContextOptions.SimpleBind)) == ((ContextOptions.Negotiate | ContextOptions.SimpleBind))))))
            {
                throw new ArgumentException(SR.InvalidContextOptionsForAD);
            }

            if ((contextType != ContextType.Machine) &&
                (contextType != ContextType.Domain) &&
                (contextType != ContextType.ApplicationDirectory)
#if TESTHOOK
                && (contextType != ContextType.Test)
#endif
                )
            {
                throw new InvalidEnumArgumentException(nameof(contextType), (int)contextType, typeof(ContextType));
            }

            if ((contextType == ContextType.Machine) && (container != null))
                throw new ArgumentException(SR.ContextNoContainerForMachineCtx);

            if ((contextType == ContextType.ApplicationDirectory) && ((string.IsNullOrEmpty(container)) || (string.IsNullOrEmpty(name))))
                throw new ArgumentException(SR.ContextNoContainerForApplicationDirectoryCtx);

            _contextType = contextType;
            _name = name;
            _container = container;
            _options = options;

            _username = userName;
            _password = password;

            DoServerVerifyAndPropRetrieval();

            _credValidate = new CredentialValidator(contextType, name, _serverProperties);
        }

        //
        // Public Properties
        //

        public ContextType ContextType
        {
            get
            {
                CheckDisposed();

                return _contextType;
            }
        }

        public string Name
        {
            get
            {
                CheckDisposed();

                return _name;
            }
        }

        public string Container
        {
            get
            {
                CheckDisposed();

                return _container;
            }
        }

        public string UserName
        {
            get
            {
                CheckDisposed();

                return _username;
            }
        }

        public ContextOptions Options
        {
            get
            {
                CheckDisposed();

                return _options;
            }
        }

        public string ConnectedServer
        {
            get
            {
                CheckDisposed();

                Initialize();

                // Unless we're not initialized, connectedServer should not be null
                Debug.Assert(_connectedServer != null || _initialized == false);

                // connectedServer should never be an empty string
                Debug.Assert(_connectedServer == null || _connectedServer.Length != 0);

                return _connectedServer;
            }
        }

        /// <summary>
        /// Validate the passed credentials against the directory supplied.
        /// This function will use the best determined method to do the evaluation.
        /// </summary>

        public bool ValidateCredentials(string userName, string password)
        {
            CheckDisposed();

            if ((userName == null && password != null) ||
                (userName != null && password == null))
                throw new ArgumentException(SR.ContextBadUserPwdCombo);

#if TESTHOOK
                if ( contextType == ContextType.Test )
                {
                    return true;
                }
#endif

            return (_credValidate.Validate(userName, password));
        }

        /// <summary>
        /// Validate the passed credentials against the directory supplied.
        /// The supplied options will determine the directory method for credential validation.
        /// </summary>
        public bool ValidateCredentials(string userName, string password, ContextOptions options)
        {
            // Perform credential validation using fast concurrent bind...
            CheckDisposed();

            if ((userName == null && password != null) ||
                (userName != null && password == null))
                throw new ArgumentException(SR.ContextBadUserPwdCombo);

            if (options != ContextOptions.Negotiate && _contextType == ContextType.Machine)
                throw new ArgumentException(SR.ContextOptionsNotValidForMachineStore);

#if TESTHOOK
                if ( contextType == ContextType.Test )
                {
                    return true;
                }
#endif

            return (_credValidate.Validate(userName, password, options));
        }

        //
        // Private methods for initialization
        //
        private void Initialize()
        {
            if (!_initialized)
            {
                lock (_initializationLock)
                {
                    if (_initialized)
                        return;

                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "Initializing Context");

                    switch (_contextType)
                    {
                        case ContextType.Domain:
                            DoDomainInit();
                            break;

                        case ContextType.Machine:
                            DoMachineInit();
                            break;

                        case ContextType.ApplicationDirectory:
                            DoApplicationDirectoryInit();
                            break;
#if TESTHOOK
                        case ContextType.Test:
                            // do nothing
                            break;
#endif
                        default:
                            // Internal error
                            Debug.Fail("PrincipalContext.Initialize: fell off end looking for " + _contextType.ToString());
                            break;
                    }

                    _initialized = true;
                }
            }
        }

        private void DoApplicationDirectoryInit()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "Entering DoApplicationDirecotryInit");

            Debug.Assert(_contextType == ContextType.ApplicationDirectory);

            if (_container == null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "DoApplicationDirecotryInit: using no-container path");
                DoLDAPDirectoryInitNoContainer();
            }
            else
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "DoApplicationDirecotryInit: using container path");
                DoLDAPDirectoryInit();
            }
        }

        private void DoMachineInit()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "Entering DoMachineInit");

            Debug.Assert(_contextType == ContextType.Machine);
            Debug.Assert(_container == null);

            DirectoryEntry de = null;

            try
            {
                string hostname = _name;

                if (hostname == null)
                    hostname = Utils.GetComputerFlatName();

                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "DoMachineInit: hostname is " + hostname);

                // use the options they specified
                AuthenticationTypes authTypes = SDSUtils.MapOptionsToAuthTypes(_options);

                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "DoMachineInit: authTypes is " + authTypes.ToString());

                de = new DirectoryEntry("WinNT://" + hostname + ",computer", _username, _password, authTypes);

                // Force ADSI to connect so we detect if the server is down or if the servername is invalid
                de.RefreshCache();

                StoreCtx storeCtx = CreateContextFromDirectoryEntry(de);

                _queryCtx = storeCtx;
                _userCtx = storeCtx;
                _groupCtx = storeCtx;
                _computerCtx = storeCtx;

                _connectedServer = hostname;
                de = null;
            }
            catch (Exception e)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Error,
                                                  "PrincipalContext",
                                                  "DoMachineInit: caught exception of type "
                                                   + e.GetType().ToString() +
                                                   " and message " + e.Message);

                // Cleanup the DE on failure
                if (de != null)
                    de.Dispose();

                throw;
            }
        }

        private void DoDomainInit()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "Entering DoDomainInit");

            Debug.Assert(_contextType == ContextType.Domain);

            if (_container == null)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "DoDomainInit: using no-container path");
                DoLDAPDirectoryInitNoContainer();
                return;
            }
            else
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "DoDomainInit: using container path");
                DoLDAPDirectoryInit();
                return;
            }
        }

        private void DoServerVerifyAndPropRetrieval()
        {
            _serverProperties = default;
            if (_contextType == ContextType.ApplicationDirectory || _contextType == ContextType.Domain)
            {
                ReadServerConfig(_name, ref _serverProperties);

                if (_serverProperties.contextType != _contextType)
                {
                    throw new ArgumentException(SR.Format(SR.PassedContextTypeDoesNotMatchDetectedType, _serverProperties.contextType.ToString()));
                }
            }
        }

        private void DoLDAPDirectoryInit()
        {
            // use the servername if they gave us one, else let ADSI figure it out
            string serverName = "";

            if (_name != null)
            {
                if (_contextType == ContextType.ApplicationDirectory)
                {
                    serverName = _serverProperties.dnsHostName + ":" +
                        ((ContextOptions.SecureSocketLayer & _options) > 0 ? _serverProperties.portSSL : _serverProperties.portLDAP);
                }
                else
                {
                    serverName = _name;
                }

                serverName += "/";
            }

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "DoLDAPDirectoryInit: serverName is " + serverName);

            // use the options they specified
            AuthenticationTypes authTypes = SDSUtils.MapOptionsToAuthTypes(_options);

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "DoLDAPDirectoryInit: authTypes is " + authTypes.ToString());

            DirectoryEntry de = new DirectoryEntry("LDAP://" + serverName + _container, _username, _password, authTypes);

            try
            {
                // Set the password port to the ssl port read off of the rootDSE.  Without this
                // password change/set won't work when we connect without SSL and ADAM is running
                // on non-standard port numbers.  We have already verified directory connectivity at this point
                // so this should always succeed.
                if (_serverProperties.portSSL > 0)
                {
                    de.Options.PasswordPort = _serverProperties.portSSL;
                }

                StoreCtx storeCtx = CreateContextFromDirectoryEntry(de);

                _queryCtx = storeCtx;
                _userCtx = storeCtx;
                _groupCtx = storeCtx;
                _computerCtx = storeCtx;

                _connectedServer = ADUtils.GetServerName(de);
                de = null;
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }
            catch (Exception e)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Error, "PrincipalContext",
                                                   "DoLDAPDirectoryInit: caught exception of type "
                                                   + e.GetType().ToString() +
                                                   " and message " + e.Message);

                throw;
            }
            finally
            {
                // Cleanup the DE on failure
                if (de != null)
                    de.Dispose();
            }
        }

        private void DoLDAPDirectoryInitNoContainer()
        {
            byte[] USERS_CONTAINER_GUID = new byte[] { 0xa9, 0xd1, 0xca, 0x15, 0x76, 0x88, 0x11, 0xd1, 0xad, 0xed, 0x00, 0xc0, 0x4f, 0xd8, 0xd5, 0xcd };
            byte[] COMPUTERS_CONTAINER_GUID = new byte[] { 0xaa, 0x31, 0x28, 0x25, 0x76, 0x88, 0x11, 0xd1, 0xad, 0xed, 0x00, 0xc0, 0x4f, 0xd8, 0xd5, 0xcd };

            // The StoreCtxs that will be used in the PrincipalContext, and their associated DirectoryEntry objects.
            DirectoryEntry deUserGroupOrg = null;
            DirectoryEntry deComputer = null;
            DirectoryEntry deBase = null;

            ADStoreCtx storeCtxUserGroupOrg = null;
            ADStoreCtx storeCtxComputer = null;
            ADStoreCtx storeCtxBase = null;

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "Entering DoLDAPDirectoryInitNoContainer");

            //
            // Build a DirectoryEntry that represents the root of the domain.
            //

            // Use the RootDSE to find the default naming context
            DirectoryEntry deRootDse = null;
            string adsPathBase;

            // use the servername if they gave us one, else let ADSI figure it out
            string serverName = "";
            if (_name != null)
            {
                serverName = _name + "/";
            }

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "DoLDAPDirectoryInitNoContainer: serverName is " + serverName);

            // use the options they specified
            AuthenticationTypes authTypes = SDSUtils.MapOptionsToAuthTypes(_options);

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "DoLDAPDirectoryInitNoContainer: authTypes is " + authTypes.ToString());

            try
            {
                deRootDse = new DirectoryEntry("LDAP://" + serverName + "rootDse", _username, _password, authTypes);

                // This will also detect if the server is down or nonexistent
                string domainNC = (string)deRootDse.Properties["defaultNamingContext"][0];
                adsPathBase = "LDAP://" + serverName + domainNC;

                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "DoLDAPDirectoryInitNoContainer: domainNC is " + domainNC);
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "DoLDAPDirectoryInitNoContainer: adsPathBase is " + adsPathBase);
            }
            finally
            {
                // Don't allow the DE to leak
                if (deRootDse != null)
                    deRootDse.Dispose();
            }

            try
            {
                // Build a DE for the root of the domain using the retrieved naming context
                deBase = new DirectoryEntry(adsPathBase, _username, _password, authTypes);

                // Set the password port to the ssl port read off of the rootDSE.  Without this
                // password change/set won't work when we connect without SSL and ADAM is running
                // on non-standard port numbers.  We have already verified directory connectivity at this point
                // so this should always succeed.
                if (_serverProperties.portSSL > 0)
                {
                    deBase.Options.PasswordPort = _serverProperties.portSSL;
                }

                //
                // Use the wellKnownObjects attribute to determine the default location
                // for users and computers.
                //
                string adsPathUserGroupOrg = null;
                string adsPathComputer = null;

                PropertyValueCollection wellKnownObjectValues = deBase.Properties["wellKnownObjects"];

                foreach (UnsafeNativeMethods.IADsDNWithBinary value in wellKnownObjectValues)
                {
                    if (Utils.AreBytesEqual(USERS_CONTAINER_GUID, (byte[])value.BinaryValue))
                    {
                        Debug.Assert(adsPathUserGroupOrg == null);
                        adsPathUserGroupOrg = "LDAP://" + serverName + value.DNString;

                        GlobalDebug.WriteLineIf(
                                GlobalDebug.Info,
                                "PrincipalContext",
                                "DoLDAPDirectoryInitNoContainer: found USER, adsPathUserGroupOrg is " + adsPathUserGroupOrg);
                    }

                    // Is it the computer container?
                    if (Utils.AreBytesEqual(COMPUTERS_CONTAINER_GUID, (byte[])value.BinaryValue))
                    {
                        Debug.Assert(adsPathComputer == null);
                        adsPathComputer = "LDAP://" + serverName + value.DNString;

                        GlobalDebug.WriteLineIf(
                                GlobalDebug.Info,
                                "PrincipalContext",
                                "DoLDAPDirectoryInitNoContainer: found COMPUTER, adsPathComputer is " + adsPathComputer);
                    }
                }

                if ((adsPathUserGroupOrg == null) || (adsPathComputer == null))
                {
                    // Something's wrong with the domain, it's not exposing the proper
                    // well-known object fields.
                    throw new PrincipalOperationException(SR.ContextNoWellKnownObjects);
                }

                //
                // Build DEs for the Users and Computers containers.
                // The Users container will also be used as the default for Groups.
                // The reason there are different contexts for groups, users and computers is so that
                // when a principal is created it will go into the appropriate default container.  This is so users don't
                // by default create principals in the root of their directory.  When a search happens the base context is used so that
                // the whole directory will be covered.
                //
                deUserGroupOrg = new DirectoryEntry(adsPathUserGroupOrg, _username, _password, authTypes);
                deComputer = new DirectoryEntry(adsPathComputer, _username, _password, authTypes);

                StoreCtx userStore = CreateContextFromDirectoryEntry(deUserGroupOrg);

                _userCtx = userStore;
                _groupCtx = userStore;
                deUserGroupOrg = null;  // since we handed off ownership to the StoreCtx

                _computerCtx = CreateContextFromDirectoryEntry(deComputer);

                deComputer = null;

                _queryCtx = CreateContextFromDirectoryEntry(deBase);

                _connectedServer = ADUtils.GetServerName(deBase);

                deBase = null;
            }
            catch (Exception e)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Error,
                                        "PrincipalContext",
                                        "DoLDAPDirectoryInitNoContainer: caught exception of type "
                                         + e.GetType().ToString() +
                                         " and message " + e.Message);

                // Cleanup on failure.  Once a DE has been successfully handed off to a ADStoreCtx,
                // that ADStoreCtx will handle Dispose()'ing it
                if (deUserGroupOrg != null)
                    deUserGroupOrg.Dispose();

                if (deComputer != null)
                    deComputer.Dispose();

                if (deBase != null)
                    deBase.Dispose();

                if (storeCtxUserGroupOrg != null)
                    storeCtxUserGroupOrg.Dispose();

                if (storeCtxComputer != null)
                    storeCtxComputer.Dispose();

                if (storeCtxBase != null)
                    storeCtxBase.Dispose();

                throw;
            }
        }

#if TESTHOOK

        public static PrincipalContext Test
        {
            get
            {
                StoreCtx storeCtx = new TestStoreCtx(true);
                PrincipalContext ctx = new PrincipalContext(ContextType.Test);
                ctx.SetupContext(storeCtx);
                ctx.initialized = true;

                storeCtx.OwningContext = ctx;
                return ctx;
            }
        }

        public static PrincipalContext TestAltValidation
        {
            get
            {
                TestStoreCtx storeCtx = new TestStoreCtx(true);
                storeCtx.SwitchValidationMode = true;
                PrincipalContext ctx = new PrincipalContext(ContextType.Test);
                ctx.SetupContext(storeCtx);
                ctx.initialized = true;

                storeCtx.OwningContext = ctx;
                return ctx;
            }
        }

        public static PrincipalContext TestNoTimeLimited
        {
            get
            {
                TestStoreCtx storeCtx = new TestStoreCtx(true);
                storeCtx.SupportTimeLimited = false;
                PrincipalContext ctx = new PrincipalContext(ContextType.Test);
                ctx.SetupContext(storeCtx);
                ctx.initialized = true;

                storeCtx.OwningContext = ctx;
                return ctx;
            }
        }

#endif // TESTHOOK

        //
        // Public Methods
        //

        public void Dispose()
        {
            if (!_disposed)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "Dispose: disposing");

                // Note that we may end up calling Dispose multiple times on the same
                // StoreCtx (since, for example, it might be that userCtx == groupCtx).
                // This is okay, since StoreCtxs allow multiple Dispose() calls, and ignore
                // all but the first call.

                if (_userCtx != null)
                    _userCtx.Dispose();

                if (_groupCtx != null)
                    _groupCtx.Dispose();

                if (_computerCtx != null)
                    _computerCtx.Dispose();

                if (_queryCtx != null)
                    _queryCtx.Dispose();

                _credValidate.Dispose();

                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        //
        // Private Implementation
        //

        // Are we initialized?
        private bool _initialized;
        private readonly object _initializationLock = new object();

        // Have we been disposed?
        private bool _disposed;
        internal bool Disposed { get { return _disposed; } }

        // Our constructor parameters

        // encryption nor zeroing out the string when you're done with it.
        private readonly string _username;
        private readonly string _password;

        // Cached connections to the server for fast credential validation
        private readonly CredentialValidator _credValidate;
        private ServerProperties _serverProperties;

        internal ServerProperties ServerInformation
        {
            get
            {
                return _serverProperties;
            }
        }

        private readonly string _name;
        private readonly string _container;
        private readonly ContextOptions _options;
        private readonly ContextType _contextType;

        // The server we're connected to
        private string _connectedServer;

        // The reason there are different contexts for groups, users and computers is so that
        // when a principal is created it will go into the appropriate default container.  This is so users don't
        // by default create principals in the root of their directory.  When a search happens the base context is used so that
        // the whole directory will be covered.  User and Computers default are the same ( USERS container ), Computers are
        // put under COMPUTERS container.  If a container is specified then all the contexts will point to the same place.

        // The StoreCtx to be used when inserting a new User/Computer/Group Principal into this
        // PrincipalContext.
        private StoreCtx _userCtx;
        private StoreCtx _computerCtx;
        private StoreCtx _groupCtx;

        // The StoreCtx to be used when querying against this PrincipalContext for Principals
        private StoreCtx _queryCtx;

        internal StoreCtx QueryCtx
        {
            get
            {
                Initialize();
                return _queryCtx;
            }

            set
            {
                _queryCtx = value;
            }
        }

        internal void ReadServerConfig(string serverName, ref ServerProperties properties)
        {
            string[] proplist = new string[] { "msDS-PortSSL", "msDS-PortLDAP", "domainControllerFunctionality", "dnsHostName", "supportedCapabilities" };
            LdapConnection ldapConnection = null;

            try
            {
                bool useSSL = (_options & ContextOptions.SecureSocketLayer) > 0;

                if (useSSL && _contextType == ContextType.Domain)
                {
                    LdapDirectoryIdentifier directoryid = new LdapDirectoryIdentifier(serverName, LdapConstants.LDAP_SSL_PORT);
                    ldapConnection = new LdapConnection(directoryid);
                }
                else
                {
                    ldapConnection = new LdapConnection(serverName);
                }

                ldapConnection.AutoBind = false;
                // If SSL was enabled on the initial connection then turn it on for the search.
                // This is required bc the appended port number will be SSL and we don't know what port LDAP is running on.
                ldapConnection.SessionOptions.SecureSocketLayer = useSSL;

                string baseDN = null; // specify base as null for RootDSE search
                string ldapSearchFilter = "(objectClass=*)";
                SearchResponse searchResponse = null;

                SearchRequest searchRequest = new SearchRequest(baseDN, ldapSearchFilter, System.DirectoryServices.Protocols
                    .SearchScope.Base, proplist);

                try
                {
                    searchResponse = (SearchResponse)ldapConnection.SendRequest(searchRequest);
                }
                catch (LdapException ex)
                {
                    throw new PrincipalServerDownException(SR.ServerDown, ex);
                }

                // Fill in the struct with the casted properties from the serach results.
                // there will always be only 1 item on the rootDSE so all entry indexes are 0
                properties.dnsHostName = (string)searchResponse.Entries[0].Attributes["dnsHostName"][0];
                properties.SupportCapabilities = new string[searchResponse.Entries[0].Attributes["supportedCapabilities"].Count];
                for (int i = 0; i < searchResponse.Entries[0].Attributes["supportedCapabilities"].Count; i++)
                {
                    properties.SupportCapabilities[i] = (string)searchResponse.Entries[0].Attributes["supportedCapabilities"][i];
                }

                foreach (string capability in properties.SupportCapabilities)
                {
                    if (CapabilityMap.LDAP_CAP_ACTIVE_DIRECTORY_ADAM_OID == capability)
                    {
                        properties.contextType = ContextType.ApplicationDirectory;
                    }
                    else if (CapabilityMap.LDAP_CAP_ACTIVE_DIRECTORY_OID == capability)
                    {
                        properties.contextType = ContextType.Domain;
                    }
                }

                // If we can't determine the OS vesion so we must fall back to lowest level of functionality
                if (searchResponse.Entries[0].Attributes.Contains("domainControllerFunctionality"))
                {
                    properties.OsVersion = (DomainControllerMode)Convert.ToInt32(searchResponse.Entries[0].Attributes["domainControllerFunctionality"][0], CultureInfo.InvariantCulture);
                }
                else
                {
                    properties.OsVersion = DomainControllerMode.Win2k;
                }

                if (properties.contextType == ContextType.ApplicationDirectory)
                {
                    if (searchResponse.Entries[0].Attributes.Contains("msDS-PortSSL"))
                    {
                        properties.portSSL = Convert.ToInt32(searchResponse.Entries[0].Attributes["msDS-PortSSL"][0]);
                    }
                    if (searchResponse.Entries[0].Attributes.Contains("msDS-PortLDAP"))
                    {
                        properties.portLDAP = Convert.ToInt32(searchResponse.Entries[0].Attributes["msDS-PortLDAP"][0]);
                    }
                }

                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ReadServerConfig", "OsVersion : " + properties.OsVersion.ToString());
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ReadServerConfig", "dnsHostName : " + properties.dnsHostName);
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ReadServerConfig", "contextType : " + properties.contextType.ToString());
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ReadServerConfig", "portSSL : " + properties.portSSL.ToString(CultureInfo.InvariantCulture));
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ReadServerConfig", "portLDAP :" + properties.portLDAP.ToString(CultureInfo.InvariantCulture));
            }
            finally
            {
                if (ldapConnection != null)
                {
                    ldapConnection.Dispose();
                }
            }
        }

        private StoreCtx CreateContextFromDirectoryEntry(DirectoryEntry entry)
        {
            StoreCtx storeCtx;

            Debug.Assert(entry != null);

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "CreateContextFromDirectoryEntry: path is " + entry.Path);

            if (entry.Path.StartsWith("LDAP:", StringComparison.Ordinal))
            {
                if (this.ContextType == ContextType.ApplicationDirectory)
                {
                    storeCtx = new ADAMStoreCtx(entry, true, _username, _password, _name, _options);
                }
                else
                {
                    storeCtx = new ADStoreCtx(entry, true, _username, _password, _options);
                }
            }
            else
            {
                Debug.Assert(entry.Path.StartsWith("WinNT:", StringComparison.Ordinal));
                storeCtx = new SAMStoreCtx(entry, true, _username, _password, _options);
            }

            storeCtx.OwningContext = this;
            return storeCtx;
        }

        // Checks if we're already been disposed, and throws an appropriate
        // exception if so.
        internal void CheckDisposed()
        {
            if (_disposed)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "PrincipalContext", "CheckDisposed: accessing disposed object");

                throw new ObjectDisposedException("PrincipalContext");
            }
        }

        // Match the default context options to the store type.
        private static ContextOptions GetDefaultOptionForStore(ContextType storeType)
        {
            if (storeType == ContextType.Machine)
            {
                return DefaultContextOptions.MachineDefaultContextOption;
            }
            else
            {
                return DefaultContextOptions.ADDefaultContextOption;
            }
        }

        // Helper method: given a typeof(User/Computer/etc.), returns the userCtx/computerCtx/etc.
        internal StoreCtx ContextForType(Type t)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "ContextForType: type is " + t.ToString());

            Initialize();

            if (t == typeof(System.DirectoryServices.AccountManagement.UserPrincipal) || t.IsSubclassOf(typeof(System.DirectoryServices.AccountManagement.UserPrincipal)))
            {
                return _userCtx;
            }
            else if (t == typeof(System.DirectoryServices.AccountManagement.ComputerPrincipal) || t.IsSubclassOf(typeof(System.DirectoryServices.AccountManagement.ComputerPrincipal)))
            {
                return _computerCtx;
            }
            else if (t == typeof(System.DirectoryServices.AccountManagement.AuthenticablePrincipal) || t.IsSubclassOf(typeof(System.DirectoryServices.AccountManagement.AuthenticablePrincipal)))
            {
                return _userCtx;
            }
            else
            {
                Debug.Assert(t == typeof(System.DirectoryServices.AccountManagement.GroupPrincipal) || t.IsSubclassOf(typeof(System.DirectoryServices.AccountManagement.GroupPrincipal)));
                return _groupCtx;
            }
        }
    }
}
