
/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    Context.cs

Abstract:

    Implements the PrincipalContext class.

History:

    04-May-2004    MattRim     Created
    17-Aug-2004    MattRim     Redesign from Context to PrincipalContext

--*/


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
using System.Security.Permissions;

[assembly:System.Security.SecurityCritical]

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
        
        static internal class CapabilityMap
        {
            public const string LDAP_CAP_ACTIVE_DIRECTORY_OID = "1.2.840.113556.1.4.800";
            public const string LDAP_CAP_ACTIVE_DIRECTORY_V51_OID = "1.2.840.113556.1.4.1670";
            public const string LDAP_CAP_ACTIVE_DIRECTORY_LDAP_INTEG_OID = "1.2.840.113556.1.4.1791";
            public const string LDAP_CAP_ACTIVE_DIRECTORY_ADAM_OID = "1.2.840.113556.1.4.1851";
            public const string LDAP_CAP_ACTIVE_DIRECTORY_PARTIAL_SECRETS_OID = "1.2.840.113556.1.4.1920";
            public const string LDAP_CAP_ACTIVE_DIRECTORY_V61_OID = "1.2.840.113556.1.4.1935";
        }



    internal sealed class CredentialValidator
    {
        enum AuthMethod
        {
            Simple = 1,
            Negotiate = 2
        }

        bool fastConcurrentSupported = true;

        Hashtable connCache = new Hashtable(4);        
        LdapDirectoryIdentifier  directoryIdent;
        object cacheLock = new object();

        AuthMethod lastBindMethod = AuthMethod.Simple;
        string serverName;
        ContextType contextType;
        ServerProperties serverProperties;

        const ContextOptions defaultContextOptionsNegotiate = ContextOptions.Signing | ContextOptions.Sealing | ContextOptions.Negotiate;
        const ContextOptions defaultContextOptionsSimple = ContextOptions.SecureSocketLayer | ContextOptions.SimpleBind;

        public CredentialValidator(ContextType contextType, string serverName, ServerProperties serverProperties)
        {
            fastConcurrentSupported = !(serverProperties.OsVersion == DomainControllerMode.Win2k);

            if ( contextType == ContextType.Machine && serverName == null )
            {
                this.serverName = Environment.MachineName;
            }
            else
            {
                this.serverName = serverName;
            }
            
            this.contextType = contextType;
            this.serverProperties = serverProperties;
        }

        [System.Security.SecurityCritical]    
        bool BindSam(string target, string userName, string password)
        {
            StringBuilder adsPath= new StringBuilder();
            adsPath.Append("WinNT://");
            adsPath.Append(serverName);
            adsPath.Append(",computer");
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
                    int index = userName.IndexOf("\\", StringComparison.Ordinal);
                    if (index == -1)
                    {
                        userName = serverName + "\\" + userName;
                    }
                }
                
                int hr = UnsafeNativeMethods.ADsOpenObject(adsPath.ToString(), userName, password, (int)authenticationType, ref g, out value);
                
                if (hr != 0 )
                {
                    if ( hr == unchecked((int)(ExceptionHelper.ERROR_HRESULT_LOGON_FAILURE )))
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
                
	         unmanagedResult  = ((UnsafeNativeMethods.IADs)value).Get("name");
             
            }
            catch(System.Runtime.InteropServices.COMException e )
            {
                if (e.ErrorCode == unchecked((int)(ExceptionHelper.ERROR_HRESULT_LOGON_FAILURE )))
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
                if ( value != null )
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(value);
            }

            return true;
        }

        bool BindLdap(NetworkCredential creds, ContextOptions contextOptions)
        {

            LdapConnection current = null;
            bool useSSL = (ContextOptions.SecureSocketLayer & contextOptions) > 0;

            if ( contextType == ContextType.ApplicationDirectory )
            {
                directoryIdent = new LdapDirectoryIdentifier( serverProperties.dnsHostName, useSSL ? serverProperties.portSSL : serverProperties.portLDAP );
            }
            else
            {
                directoryIdent = new LdapDirectoryIdentifier(this.serverName, useSSL ? LdapConstants.LDAP_SSL_PORT : LdapConstants.LDAP_PORT);
            }
           
            bool attemptFastConcurrent = useSSL && fastConcurrentSupported;
            int index = Convert.ToInt32(attemptFastConcurrent) * 2 + Convert.ToInt32(useSSL);
            
            if (!connCache.Contains(index))
            {
                lock( cacheLock )
                {                    
                    if (!connCache.Contains(index))
                    {                
                        current = new LdapConnection(directoryIdent);
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
                                fastConcurrentSupported = false;
                                index = Convert.ToInt32(useSSL);
                                current = new LdapConnection(directoryIdent);
                                // We have fallen back to another connection so we need to set SSL again.
                                current.SessionOptions.SecureSocketLayer = useSSL;
                                
                            }
                        }

                        connCache.Add(index, current);
                    }
                    else
                    {
                        current = (LdapConnection)connCache[index];
                    }
                 }
                
            }
            else
            {
                current = (LdapConnection)connCache[index];
            }

            // If we are performing fastConcurrentBind there is no need to prevent multithreadaccess.  FSB is thread safe and multi cred safe
            // FSB also always has the same contextoptions so there is no need to lock the code that is modifying the current connection
            if ( attemptFastConcurrent && fastConcurrentSupported )
            {
                lockedLdapBind( current, creds, contextOptions);
            }
            else
            {
                lock( cacheLock )
                {
                    lockedLdapBind( current, creds, contextOptions);
                }
            }
            return true;
        }

        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="LdapConnection.Bind():System.Void" />
        // <SatisfiesLinkDemand Name="LdapConnection.Bind(System.Net.NetworkCredential):System.Void" />
        // </SecurityKernel>
        [System.Security.SecuritySafeCritical]
        void lockedLdapBind( LdapConnection current, NetworkCredential creds, ContextOptions contextOptions)
        {
                current.AuthType = ((ContextOptions.SimpleBind & contextOptions) > 0 ? AuthType.Basic : AuthType.Negotiate);
                current.SessionOptions.Signing = ((ContextOptions.Signing & contextOptions) > 0 ? true : false);
                current.SessionOptions.Sealing = ((ContextOptions.Sealing & contextOptions) > 0 ? true : false);
                if ( (null == creds.UserName) && (null == creds.Password) )
                {
    			current.Bind();
    		   }
    		   else
    		   {
    			current.Bind(creds);
    		   }
       
        }


       // <SecurityKernel Critical="True" Ring="0">
       // <Asserts Name="Declarative: [DirectoryServicesPermission(SecurityAction.Assert, Unrestricted = true)]" />
       // <ReferencesCritical Name="Method: BindSam(String, String, String):Boolean" Ring="1" />
       // <ReferencesCritical Name="Method: BindLdap(NetworkCredential, ContextOptions):Boolean" Ring="2" />
       // </SecurityKernel>
       [System.Security.SecurityCritical]
       [DirectoryServicesPermission(SecurityAction.Assert, Unrestricted=true)]        
        public bool Validate(string userName, string password)
        {

            NetworkCredential networkCredential = new NetworkCredential( userName, password );

            // empty username and password on the local box
            // causes authentication to succeed.  If the username is empty we should just fail it
            // here.
            if (userName != null && userName.Length == 0)
                return false;
            
        
            if (contextType == ContextType.Domain || contextType == ContextType.ApplicationDirectory)
            {

                try
                {
                    if (lastBindMethod == AuthMethod.Simple && (fastConcurrentSupported || contextType == ContextType.ApplicationDirectory))
                    {
                        try
                        {
                            BindLdap(networkCredential , defaultContextOptionsSimple );
                            lastBindMethod = AuthMethod.Simple;
                            return true;
                        }
                        catch (LdapException)
                        {
                            // we don't return false here even if we failed with ERROR_LOGON_FAILURE. We must check Negotiate
                            // because there might be cases in which SSL fails and Negotiate succeeds
                        }
                        
                        BindLdap(networkCredential , defaultContextOptionsNegotiate);
                        lastBindMethod = AuthMethod.Negotiate;
                        return true;
                    }
                    else
                    {

                        try
                        {
                            BindLdap(networkCredential , defaultContextOptionsNegotiate);
                            lastBindMethod = AuthMethod.Negotiate;
                            return true;
                        }
                        catch (LdapException)
                        {
                            // we don't return false here even if we failed with ERROR_LOGON_FAILURE. We must check SSL
                            // because there might be cases in which Negotiate fails and SSL succeeds
                        }

                        BindLdap(networkCredential , defaultContextOptionsSimple);
                        lastBindMethod = AuthMethod.Simple;
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
                Debug.Assert( contextType == ContextType.Machine );
                return ( BindSam(serverName, userName, password) );
            }

        }

       // <SecurityKernel Critical="True" Ring="0">
       // <Asserts Name="Declarative: [DirectoryServicesPermission(SecurityAction.Assert, Unrestricted = true)]" />
       // <ReferencesCritical Name="Method: BindSam(String, String, String):Boolean" Ring="1" />
       // <ReferencesCritical Name="Method: BindLdap(NetworkCredential, ContextOptions):Boolean" Ring="2" />
       // </SecurityKernel>
       [System.Security.SecurityCritical]
       [DirectoryServicesPermission(SecurityAction.Assert, Unrestricted=true)]        
        public bool Validate(string userName, string password, ContextOptions connectionMethod)
        {

            // empty username and password on the local box
            // causes authentication to succeed.  If the username is empty we should just fail it
            // here.
            if (userName != null && userName.Length == 0)
                return false;
        
            if (contextType == ContextType.Domain || contextType == ContextType.ApplicationDirectory)
            {
                try
                {
                    NetworkCredential networkCredential = new NetworkCredential( userName, password );                    
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
                return ( BindSam(serverName, userName, password) );
            }
        }
    }
// ********************************************
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Assert, 
                                                Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
    // <SecurityKernel Critical="True" Ring="0">
    // <Asserts Name="Declarative: [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]" />
    // </SecurityKernel>
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.LinkDemand, Unrestricted = true)]
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, Unrestricted = true)]
//    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Assert, Unrestricted=true)]
    public class PrincipalContext : IDisposable
    {

        //
        // Public Constructors
        //

        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Demand, Unrestricted = true)]
        public PrincipalContext(ContextType contextType) :
            this(contextType, null, null, PrincipalContext.GetDefaultOptionForStore(contextType) , null, null) {}
            
        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Demand, Unrestricted = true)]
        public PrincipalContext(ContextType contextType, string name) :
            this(contextType, name, null, PrincipalContext.GetDefaultOptionForStore(contextType) , null, null) { }
        
        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Demand, Unrestricted = true)]
        public PrincipalContext(ContextType contextType, string name, string container) :
            this(contextType, name, container, PrincipalContext.GetDefaultOptionForStore(contextType) , null, null) {}

        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Demand, Unrestricted = true)]
        public PrincipalContext(ContextType contextType, string name, string container, ContextOptions options) :
            this(contextType, name, container, options, null, null) {}

        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Demand, Unrestricted = true)]
        public PrincipalContext(ContextType contextType, string name, string userName, string password) :
            this(contextType, name, null, PrincipalContext.GetDefaultOptionForStore(contextType), userName, password) { }

        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Demand, Unrestricted = true)]
        public PrincipalContext(ContextType contextType, string name, string container, string userName, string password) :
            this(contextType, name, container, PrincipalContext.GetDefaultOptionForStore(contextType), userName, password) { }

        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Demand, Unrestricted = true)]
        public PrincipalContext(
                    ContextType contextType, string name, string container, ContextOptions options, string userName, string password)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "Entering ctor");
        
            if ((userName == null && password != null) ||
                (userName != null && password == null))
                throw new ArgumentException(StringResources.ContextBadUserPwdCombo);

            if ( (options & ~(ContextOptions.Signing | ContextOptions.Negotiate | ContextOptions.Sealing | ContextOptions.SecureSocketLayer | ContextOptions.SimpleBind | ContextOptions.ServerBind )) != 0)
                throw new InvalidEnumArgumentException("options", (int)options, typeof(ContextOptions));


            if ( contextType == ContextType.Machine && ((options & ~ContextOptions.Negotiate) != 0))
            {
                throw new ArgumentException(StringResources.InvalidContextOptionsForMachine);
            }

            if ( ( contextType == ContextType.Domain || contextType == ContextType.ApplicationDirectory) && 
                ((( options & (ContextOptions.Negotiate | ContextOptions.SimpleBind)) == 0 ) || 
                ((( options & (ContextOptions.Negotiate | ContextOptions.SimpleBind)) == ((ContextOptions.Negotiate | ContextOptions.SimpleBind))  )))) 
            {
                throw new ArgumentException(StringResources.InvalidContextOptionsForAD);
            }                        
                        
            if ((contextType != ContextType.Machine) && 
                (contextType != ContextType.Domain) &&
                (contextType != ContextType.ApplicationDirectory)
#if TESTHOOK                
                && (contextType != ContextType.Test)
#endif
                )
            {
                throw new InvalidEnumArgumentException("contextType", (int)contextType, typeof(ContextType));
            }

            if ((contextType == ContextType.Machine) && (container != null))
                throw new ArgumentException(StringResources.ContextNoContainerForMachineCtx);
                
            if ((contextType == ContextType.ApplicationDirectory) && ((String.IsNullOrEmpty(container)) || (String.IsNullOrEmpty(name))))
                throw new ArgumentException(StringResources.ContextNoContainerForApplicationDirectoryCtx);

            this.contextType = contextType;
            this.name = name;
            this.container = container;
            this.options = options;

            this.username = userName;
            this.password = password;

            DoServerVerifyAndPropRetrieval();

            this.credValidate = new CredentialValidator(contextType, name, serverProperties);            
        }


        //
        // Public Properties
        //

        public ContextType ContextType
        {
            get
            {
                CheckDisposed();
                            
                return this.contextType;
            }
        }

        public string Name
        {
            get
            {
                CheckDisposed();

                return this.name;
            }
        }

        public string Container
        {
            get
            {
                CheckDisposed();

                return this.container;
            }
        }

        public string UserName
        {
            get
            {
                CheckDisposed();

                return this.username;
            }
        }

        public ContextOptions Options
        {
            get
            {
                CheckDisposed();
                            
                return this.options;
            }
        }

        public string ConnectedServer
        {
            get
            {
                CheckDisposed();

                Initialize();

                // Unless we're not initialized, connectedServer should not be null
                Debug.Assert(this.connectedServer != null || this.initialized == false);

                // connectedServer should never be an empty string
                Debug.Assert(this.connectedServer == null || this.connectedServer.Length != 0);

                return this.connectedServer;                                
            }
        }

        
        /// <summary>
        /// Validate the passed credentials against the directory supplied.
        //   This function will use the best determined method to do the evaluation
        /// </summary>
        
        public bool ValidateCredentials(string userName, string password)
        {

            CheckDisposed();
            
            if ((userName == null && password != null) ||
                (userName != null && password == null))
                throw new ArgumentException(StringResources.ContextBadUserPwdCombo);

#if TESTHOOK
                if ( contextType == ContextType.Test )
                {
                    return true;
                }
#endif
        
            return ( credValidate.Validate( userName, password) );            
        }
        
        /// <summary>
        /// Validate the passed credentials against the directory supplied.
        //   The supplied options will determine the directory method for credential validation.
        /// </summary>
        public bool ValidateCredentials(string userName, string password, ContextOptions options)
        {
            // Perform credential validation using fast concurrent bind...            
            CheckDisposed();

            if ((userName == null && password != null) ||
                (userName != null && password == null))
                throw new ArgumentException(StringResources.ContextBadUserPwdCombo);

            if ( options != ContextOptions.Negotiate  && contextType == ContextType.Machine )
                throw new ArgumentException(StringResources.ContextOptionsNotValidForMachineStore);

#if TESTHOOK
                if ( contextType == ContextType.Test )
                {
                    return true;
                }
#endif


            return ( credValidate.Validate( userName, password, options ) );

        }


        //
        // Private methods for initialization
        //                
       [DirectoryServicesPermission(SecurityAction.Assert, Unrestricted=true)]        
        void Initialize()
        {
            if (!this.initialized)
            {        
                lock (this.initializationLock)
                {
                    if (this.initialized)
                        return;

                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "Initializing Context");

                    switch (this.contextType)
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
                            Debug.Fail("PrincipalContext.Initialize: fell off end looking for " + this.contextType.ToString());
                            break;
                    }

                    this.initialized = true;
                }
            }
        }


        void DoApplicationDirectoryInit()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "Entering DoApplicationDirecotryInit");
        
            Debug.Assert(this.contextType == ContextType.ApplicationDirectory);        

            if (this.container == null)
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

        void DoMachineInit()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "Entering DoMachineInit");
        
            Debug.Assert(this.contextType == ContextType.Machine);
            Debug.Assert(this.container == null);

            DirectoryEntry de = null;

            try
            {
                string hostname = this.name;

                if (hostname == null)
                    hostname = Utils.GetComputerFlatName();

                GlobalDebug.WriteLineIf(GlobalDebug.Info,"PrincipalContext",  "DoMachineInit: hostname is " + hostname);

                // use the options they specified
                AuthenticationTypes authTypes = SDSUtils.MapOptionsToAuthTypes(this.options);

                GlobalDebug.WriteLineIf(GlobalDebug.Info,"PrincipalContext",  "DoMachineInit: authTypes is " + authTypes.ToString());

                de = new DirectoryEntry("WinNT://" + hostname + ",computer", this.username, this.password, authTypes);

                // Force ADSI to connect so we detect if the server is down or if the servername is invalid
                de.RefreshCache();

                StoreCtx storeCtx = CreateContextFromDirectoryEntry(de);
                
                this.queryCtx = storeCtx;
                this.userCtx = storeCtx;
                this.groupCtx = storeCtx;
                this.computerCtx = storeCtx;
                
                this.connectedServer = hostname;
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

        void DoDomainInit()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "Entering DoDomainInit");
        
            Debug.Assert(this.contextType == ContextType.Domain);

            if (this.container == null)
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


        void DoServerVerifyAndPropRetrieval()
        {        
            this.serverProperties = new ServerProperties();
            if ( this.contextType == ContextType.ApplicationDirectory || this.contextType == ContextType.Domain )
            {
                ReadServerConfig(this.name, ref this.serverProperties);
                
                if ( this.serverProperties.contextType != this.contextType )
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, StringResources.PassedContextTypeDoesNotMatchDetectedType, this.serverProperties.contextType.ToString()));
                }
            }
        }

        
        void DoLDAPDirectoryInit()
        {

            // use the servername if they gave us one, else let ADSI figure it out
            string serverName = "";

            if (this.name != null)
            {
                if ( contextType == ContextType.ApplicationDirectory )
                {
                    serverName = this.serverProperties.dnsHostName + ":" +
                        ((ContextOptions.SecureSocketLayer & this.options) > 0 ? this.serverProperties.portSSL : this.serverProperties.portLDAP );
                }
                else
                {
                    serverName = this.name;
                }
                
                serverName += "/";
            }

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "DoLDAPDirectoryInit: serverName is " + serverName);

            // use the options they specified
            AuthenticationTypes authTypes = SDSUtils.MapOptionsToAuthTypes(this.options);

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "DoLDAPDirectoryInit: authTypes is " + authTypes.ToString());

            DirectoryEntry de = new DirectoryEntry("LDAP://" + serverName + this.container, this.username, this.password, authTypes);

            try
            {


                // Set the password port to the ssl port read off of the rootDSE.  Without this
                // password change/set won't work when we connect without SSL and ADAM is running
                // on non-standard port numbers.  We have already verified directory connectivity at this point
                // so this should always succeed.
                if (this.serverProperties.portSSL > 0)
                {
                    de.Options.PasswordPort = this.serverProperties.portSSL;
                }

                StoreCtx storeCtx = CreateContextFromDirectoryEntry(de);

                this.queryCtx = storeCtx;
                this.userCtx = storeCtx;
                this.groupCtx = storeCtx;
                this.computerCtx = storeCtx;

                this.connectedServer = ADUtils.GetServerName(de);
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

        void DoLDAPDirectoryInitNoContainer()
        {
            byte[] USERS_CONTAINER_GUID     = new byte[]{0xa9,0xd1,0xca,0x15,0x76,0x88,0x11,0xd1,0xad,0xed,0x00,0xc0,0x4f,0xd8,0xd5,0xcd};
            byte[] COMPUTERS_CONTAINER_GUID = new byte[]{0xaa,0x31,0x28,0x25,0x76,0x88,0x11,0xd1,0xad,0xed,0x00,0xc0,0x4f,0xd8,0xd5,0xcd};

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
            if (this.name != null)
            {
                serverName = this.name + "/";
            }


            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "DoLDAPDirectoryInitNoContainer: serverName is " + serverName);

            // use the options they specified
            AuthenticationTypes authTypes = SDSUtils.MapOptionsToAuthTypes(this.options);

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "DoLDAPDirectoryInitNoContainer: authTypes is " + authTypes.ToString());
            
            try
            {
                deRootDse = new DirectoryEntry("LDAP://" + serverName + "rootDse", this.username, this.password, authTypes);

                // This will also detect if the server is down or nonexistent
                string domainNC = (string) deRootDse.Properties["defaultNamingContext"][0];
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
                deBase = new DirectoryEntry(adsPathBase, this.username, this.password, authTypes);

                // Set the password port to the ssl port read off of the rootDSE.  Without this
                // password change/set won't work when we connect without SSL and ADAM is running
                // on non-standard port numbers.  We have already verified directory connectivity at this point
                // so this should always succeed.
                if ( this.serverProperties.portSSL > 0 )
                {
                    deBase.Options.PasswordPort = this.serverProperties.portSSL;
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
                    if (Utils.AreBytesEqual(USERS_CONTAINER_GUID, (byte[]) value.BinaryValue))
                    {                    
                        Debug.Assert(adsPathUserGroupOrg == null);
                        adsPathUserGroupOrg = "LDAP://" + serverName + value.DNString;

                        GlobalDebug.WriteLineIf(
                                GlobalDebug.Info, 
                                "PrincipalContext", 
                                "DoLDAPDirectoryInitNoContainer: found USER, adsPathUserGroupOrg is " + adsPathUserGroupOrg);                        
                    }

                    // Is it the computer container?
                    if (Utils.AreBytesEqual(COMPUTERS_CONTAINER_GUID, (byte[]) value.BinaryValue))
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
                    throw new PrincipalOperationException(StringResources.ContextNoWellKnownObjects);
                }

                //
                // Build DEs for the Users and Computers containers.
                // The Users container will also be used as the default for Groups.
                // The reason there are different contexts for groups, users and computers is so that
                // when a principal is created it will go into the appropriate default container.  This is so users don't 
                // be default create principals in the root of their directory.  When a search happens the base context is used so that
                // the whole directory will be covered.
                //
                deUserGroupOrg = new DirectoryEntry(adsPathUserGroupOrg, this.username, this.password, authTypes);
                deComputer = new DirectoryEntry(adsPathComputer, this.username, this.password, authTypes);
                
                StoreCtx userStore = CreateContextFromDirectoryEntry( deUserGroupOrg);
                
                this.userCtx = userStore;
                this.groupCtx = userStore;
                deUserGroupOrg = null;  // since we handed off ownership to the StoreCtx
                
                this.computerCtx = CreateContextFromDirectoryEntry( deComputer);
                
                deComputer = null;

                this.queryCtx = CreateContextFromDirectoryEntry(deBase);
                
                this.connectedServer = ADUtils.GetServerName(deBase);

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

        static public PrincipalContext Test
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

        static public PrincipalContext TestAltValidation
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

        static public PrincipalContext TestNoTimeLimited
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
            if (!this.disposed)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "Dispose: disposing");                        

            
                // Note that we may end up calling Dispose multiple times on the same
                // StoreCtx (since, for example, it might be that userCtx == groupCtx).
                // This is okay, since StoreCtxs allow multiple Dispose() calls, and ignore
                // all but the first call.
            
                if (this.userCtx != null)
                    this.userCtx.Dispose();

                if (this.groupCtx != null)
                    this.groupCtx.Dispose();

                if (this.computerCtx != null)
                    this.computerCtx.Dispose();

                if (this.queryCtx != null)
                    this.queryCtx.Dispose();

                this.disposed = true;
                GC.SuppressFinalize(this);
            }
        }
        

        //
        // Private Implementation
        //


        // Are we initialized?
        bool initialized = false;        
        object initializationLock = new object();

        // Have we been disposed?
        bool disposed = false;
        internal bool Disposed { get {return this.disposed;} }

        // Our constructor parameters
 
        // encryption nor zeroing out the string when you're done with it.
        string username;
        string password;
 

        // Cached connections to the server for fast credential validation
        CredentialValidator credValidate;
        ServerProperties serverProperties;

        internal ServerProperties ServerInformation
        {
            get
            {
                return serverProperties;
            }
        }
        
        
        string name;
        string container;
        ContextOptions options;
        ContextType contextType;

        // The server we're connected to
        string connectedServer = null;

        // The reason there are different contexts for groups, users and computers is so that
        // when a principal is created it will go into the appropriate default container.  This is so users don't 
        // by default create principals in the root of their directory.  When a search happens the base context is used so that
        // the whole directory will be covered.  User and Computers default are the same ( USERS container ), Computers are
        // put under COMPUTERS container.  If a container is specified then all the contexts will point to the same place.

        // The StoreCtx to be used when inserting a new User/Computer/Group Principal into this
        // PrincipalContext.
        StoreCtx userCtx     = null;
        StoreCtx computerCtx = null;
        StoreCtx groupCtx    = null;
       
        
        // The StoreCtx to be used when querying against this PrincipalContext for Principals
        StoreCtx queryCtx = null;
        
        internal StoreCtx QueryCtx 
        {
            [System.Security.SecuritySafeCritical]        
            get 
            {
                Initialize();
                return this.queryCtx;
            } 

            set
            {
                this.queryCtx = value;
            }
        }


       [DirectoryServicesPermission(SecurityAction.Assert, Unrestricted=true)]        
        internal void ReadServerConfig( string serverName, ref ServerProperties properties )
        {

            string[] proplist = new string[] { "msDS-PortSSL", "msDS-PortLDAP", "domainControllerFunctionality", "dnsHostName", "supportedCapabilities"};
            LdapConnection ldapConnection = null;

            try
            {
                bool useSSL = (this.options & ContextOptions.SecureSocketLayer) > 0;

                if (useSSL && contextType == ContextType.Domain)
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
                // This is requried bc the appended port number will be SSL and we don't know what port LDAP is running on.
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
                    throw new PrincipalServerDownException(StringResources.ServerDown, ex);
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

        StoreCtx CreateContextFromDirectoryEntry(DirectoryEntry entry)
        {
            StoreCtx storeCtx;

            Debug.Assert(entry != null);

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext", "CreateContextFromDirectoryEntry: path is " + entry.Path);                        
            
            if (entry.Path.StartsWith("LDAP:", StringComparison.Ordinal))
            {
                if ( this.ContextType == ContextType.ApplicationDirectory)
                {
                    storeCtx = new ADAMStoreCtx(entry, true, this.username, this.password, this.name, this.options);
                }
                else
                {
                    storeCtx = new ADStoreCtx(entry, true, this.username, this.password, this.options);
                }
            }
            else
            {
                Debug.Assert(entry.Path.StartsWith("WinNT:", StringComparison.Ordinal));
                storeCtx = new SAMStoreCtx(entry, true, this.username, this.password, this.options);
            }

            storeCtx.OwningContext = this;
            return storeCtx;
        }

        // Checks if we're already been disposed, and throws an appropriate
        // exception if so.
        internal void CheckDisposed()
        {
            if (this.disposed)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "PrincipalContext", "CheckDisposed: accessing disposed object");                        
            
                throw new ObjectDisposedException("PrincipalContext");
            }
        }

        // Match the default context options to the store type.
        private static ContextOptions GetDefaultOptionForStore( ContextType storeType )
        {
            if ( storeType == ContextType.Machine )
            {
                return DefaultContextOptions.MachineDefaultContextOption;
            }
            else
            {   
                Debug.Assert( storeType == ContextType.Domain || storeType == ContextType.ApplicationDirectory);
                return DefaultContextOptions.ADDefaultContextOption;
            }            
        }

        // Helper method: given a typeof(User/Computer/etc.), returns the userCtx/computerCtx/etc.
        internal StoreCtx ContextForType(Type t)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalContext",  "ContextForType: type is " + t.ToString());                        
        
            Initialize();
        
            if (t == typeof(System.DirectoryServices.AccountManagement.UserPrincipal) || t.IsSubclassOf(typeof(System.DirectoryServices.AccountManagement.UserPrincipal)))
            {
                return this.userCtx;
            }
            else if (t == typeof(System.DirectoryServices.AccountManagement.ComputerPrincipal) || t.IsSubclassOf(typeof(System.DirectoryServices.AccountManagement.ComputerPrincipal)))
            {
                return this.computerCtx;
            }
            else if (t == typeof(System.DirectoryServices.AccountManagement.AuthenticablePrincipal) || t.IsSubclassOf(typeof(System.DirectoryServices.AccountManagement.AuthenticablePrincipal)))
            {
                return this.userCtx;
            }
            else
            {
                Debug.Assert(t == typeof(System.DirectoryServices.AccountManagement.GroupPrincipal) || t.IsSubclassOf(typeof(System.DirectoryServices.AccountManagement.GroupPrincipal)));
                return this.groupCtx;                
            }
        }
    }
}
















