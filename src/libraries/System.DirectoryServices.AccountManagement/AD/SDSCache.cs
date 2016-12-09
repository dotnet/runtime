/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    SDSCache.cs

Abstract:


History:

    17-July-2004    MattRim     Created

--*/

using System;
using System.Diagnostics;
using System.Collections;
using System.Globalization;
using System.DirectoryServices;
using System.Net;
using System.Threading;
 /// This is a class designed to cache DirectoryEntires instead of creating them every time.
namespace System.DirectoryServices.AccountManagement
{
    class SDSCache
    {

        public static SDSCache Domain
        {
            get
            {
                return SDSCache.domainCache;
            }
        }

        public static SDSCache LocalMachine
        {
            get
            {
                return SDSCache.localMachineCache;
            }
        }

        static SDSCache domainCache = new SDSCache(false);
        static SDSCache localMachineCache = new SDSCache(true);

        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="PrincipalContext.get_Disposed():System.Boolean" />
        // <SatisfiesLinkDemand Name="PrincipalContext..ctor(System.DirectoryServices.AccountManagement.ContextType,System.String,System.String,System.DirectoryServices.AccountManagement.ContextOptions,System.String,System.String)" />
        // <ReferencesCritical Name="Method: NetCred.get_Domain():System.String" Ring="1" />
        // <ReferencesCritical Name="Method: PrincipalContext.get_Disposed():System.Boolean" Ring="1" />
        // <Asserts Name="Declarative: [DirectoryServicesPermission(SecurityAction.Assert, Unrestricted = true)]" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        [System.DirectoryServices.DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Assert, Unrestricted=true)]
        public PrincipalContext GetContext(string name, NetCred credentials, ContextOptions contextOptions)
        {        
            string contextName = name;
            string userName = null;
            bool explicitCreds = false;
            if (credentials != null && credentials.UserName != null)
            {
                if (credentials.Domain != null)
                    userName = credentials.Domain + "\\" + credentials.UserName;
                else
                    userName = credentials.UserName;
                    
                explicitCreds = true;
            }
            else
            {
                userName = Utils.GetNT4UserName();
            }

            GlobalDebug.WriteLineIf(
                        GlobalDebug.Info,
                        "SDSCache",
                        "GetContext: looking for context for server {0}, user {1}, explicitCreds={2}, options={3}",
                        name,
                        userName,
                        explicitCreds.ToString(),
                        contextOptions.ToString());

            if (!isSAM)
            {
                // Determine the domain DNS name

                // DS_RETURN_DNS_NAME | DS_DIRECTORY_SERVICE_REQUIRED | DS_BACKGROUND_ONLY
                int flags = unchecked((int) (0x40000000 | 0x00000010 | 0x00000100));
                UnsafeNativeMethods.DomainControllerInfo info = Utils.GetDcName(null, contextName, null, flags);
                contextName = info.DomainName;
            }

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SDSCache", "GetContext: final contextName is " + contextName);


            ManualResetEvent contextReadyEvent = null;

            while (true)
            {
                Hashtable credTable = null;
                PrincipalContext ctx = null;

                // Wait for the PrincipalContext to be ready
                if (contextReadyEvent != null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "SDSCache", "GetContext: waiting");
                
                    contextReadyEvent.WaitOne();
                }

                contextReadyEvent = null;
            
                lock (this.tableLock)
                {
                    CredHolder credHolder = (CredHolder) this.table[contextName];

                    if (credHolder != null)
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "SDSCache", "GetContext: found a credHolder for " + contextName);
                    
                        credTable = (explicitCreds ? credHolder.explicitCreds : credHolder.defaultCreds);
                        Debug.Assert(credTable != null);

                        object o = credTable[userName];

                        if (o is Placeholder)
                        {
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SDSCache", "GetContext: credHolder for " + contextName + " has a Placeholder");
                        
                            // A PrincipalContext is currently being constructed by another thread.
                            // Wait for it.
                            contextReadyEvent = ((Placeholder)o).contextReadyEvent;
                            continue;
                        }
                                                                            
                        WeakReference refToContext = o as WeakReference;
                        if (refToContext != null)
                        {
                            GlobalDebug.WriteLineIf(GlobalDebug.Info, "SDSCache", "GetContext: refToContext is non-null");

                            ctx = (PrincipalContext) refToContext.Target;  // null if GC'ed

                            // If the PrincipalContext hasn't been GCed or disposed, use it.
                            // Otherwise, we'll need to create a new one
                            if (ctx != null && ctx.Disposed == false)
                            {
                                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SDSCache", "GetContext: using found refToContext");                                                        
                                return ctx;
                            }
                            else
                            {
                                GlobalDebug.WriteLineIf(GlobalDebug.Info, "SDSCache", "GetContext: refToContext is GCed/disposed, removing");                            
                                credTable.Remove(userName);      
                            }
                        }
                    }

                    // Either credHolder/credTable are null (no contexts exist for the contextName), or credHolder/credTable
                    // are non-null (contexts exist, but none for the userName).  Either way, we need to create a PrincipalContext.

                    if (credHolder == null)
                    {
                        GlobalDebug.WriteLineIf(
                                GlobalDebug.Info,
                                "SDSCache", 
                                "GetContext: null credHolder for " + contextName +", explicitCreds=" + explicitCreds.ToString());
                    
                        // No contexts exist for the contextName.  Create a CredHolder for the contextName so we have a place
                        // to store the PrincipalContext we'll be creating.
                        credHolder = new CredHolder();
                        this.table[contextName] = credHolder;

                        credTable = (explicitCreds ? credHolder.explicitCreds : credHolder.defaultCreds);
                    }

                    // Put a placeholder on the contextName/userName slot, so that other threads that come along after
                    // we release the tableLock know we're in the process of creating the needed PrincipalContext and will wait for us
                    credTable[userName] = new Placeholder();
                }

                   
                // Now we just need to create a PrincipalContext for the contextName and credentials
                GlobalDebug.WriteLineIf(
                        GlobalDebug.Info, 
                        "SDSCache", 
                        "GetContext: creating context, contextName=" + contextName + ", options=" + contextOptions.ToString());
                
                ctx = new PrincipalContext(
                                (isSAM ? ContextType.Machine : ContextType.Domain),
                                contextName,
                                null,
                                contextOptions,
                                (credentials != null ? credentials.UserName : null),
                                (credentials != null ? credentials.Password : null)
                                );

                lock(this.tableLock)
                {
                    Placeholder placeHolder = (Placeholder) credTable[userName];

                    // Replace the placeholder with the newly-created PrincipalContext
                    credTable[userName] = new WeakReference(ctx);

                    // Signal waiting threads to continue.  We do this after inserting the PrincipalContext
                    // into the table, so that the PrincipalContext is ready as soon as the other threads wake up.
                    // (Actually, the order probably doesn't matter, since even if we did it in the
                    // opposite order and the other thread woke up before we inserted the PrincipalContext, it would
                    // just block as soon as it tries to acquire the tableLock that we're currently holding.)
                    bool f = placeHolder.contextReadyEvent.Set();
                    Debug.Assert(f == true);
                }
                
                return ctx;
            }
        }


        //
        private SDSCache(bool isSAM)
        {
            this.isSAM = isSAM;
        }


        Hashtable table = new Hashtable();
        object tableLock = new object();
        bool isSAM;

        private class CredHolder
        {
            public Hashtable explicitCreds = new Hashtable();
            public Hashtable defaultCreds = new Hashtable();
        }

        private class Placeholder
        {
            // initially non-signaled
            public ManualResetEvent contextReadyEvent = new ManualResetEvent(false);
        }

    }


}
