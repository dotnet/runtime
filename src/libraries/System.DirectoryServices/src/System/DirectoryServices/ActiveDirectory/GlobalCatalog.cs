// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.ActiveDirectory
{
    public class GlobalCatalog : DomainController
    {
        // private variables
        private ActiveDirectorySchema? _schema;
        private bool _disabled;

        #region constructors
        internal GlobalCatalog(DirectoryContext context, string globalCatalogName)
            : base(context, globalCatalogName)
        { }

        internal GlobalCatalog(DirectoryContext context, string globalCatalogName, DirectoryEntryManager directoryEntryMgr)
            : base(context, globalCatalogName, directoryEntryMgr)
        { }
        #endregion constructors

        #region public methods

        public static GlobalCatalog GetGlobalCatalog(DirectoryContext context)
        {
            string? gcDnsName = null;
            bool isGlobalCatalog = false;
            DirectoryEntryManager? directoryEntryMgr = null;

            // check that the context argument is not null
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // target should be GC
            if (context.ContextType != DirectoryContextType.DirectoryServer)
            {
                throw new ArgumentException(SR.TargetShouldBeGC, nameof(context));
            }

            // target should be a server
            if (!(context.isServer()))
            {
                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.GCNotFound, context.Name), typeof(GlobalCatalog), context.Name);
            }

            //  work with copy of the context
            context = new DirectoryContext(context);

            try
            {
                // Get dns name of the dc
                // by binding to root dse and getting the "dnsHostName" attribute
                // (also check that the "isGlobalCatalogReady" attribute is true)
                directoryEntryMgr = new DirectoryEntryManager(context);
                DirectoryEntry rootDSE = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
                if (!Utils.CheckCapability(rootDSE, Capability.ActiveDirectory))
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.GCNotFound, context.Name), typeof(GlobalCatalog), context.Name);
                }

                gcDnsName = (string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.DnsHostName)!;
                isGlobalCatalog = (bool)bool.Parse((string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.IsGlobalCatalogReady)!);
                if (!isGlobalCatalog)
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.GCNotFound, context.Name), typeof(GlobalCatalog), context.Name);
                }
            }
            catch (COMException e)
            {
                int errorCode = e.ErrorCode;

                if (errorCode == unchecked((int)0x8007203a))
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.GCNotFound, context.Name), typeof(GlobalCatalog), context.Name);
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }

            return new GlobalCatalog(context, gcDnsName, directoryEntryMgr);
        }

        public static new GlobalCatalog FindOne(DirectoryContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.ContextType != DirectoryContextType.Forest)
            {
                throw new ArgumentException(SR.TargetShouldBeForest, nameof(context));
            }

            return FindOneWithCredentialValidation(context, null, 0);
        }

        public static new GlobalCatalog FindOne(DirectoryContext context, string siteName)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.ContextType != DirectoryContextType.Forest)
            {
                throw new ArgumentException(SR.TargetShouldBeForest, nameof(context));
            }

            if (siteName == null)
            {
                throw new ArgumentNullException(nameof(siteName));
            }

            return FindOneWithCredentialValidation(context, siteName, 0);
        }

        public static new GlobalCatalog FindOne(DirectoryContext context, LocatorOptions flag)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.ContextType != DirectoryContextType.Forest)
            {
                throw new ArgumentException(SR.TargetShouldBeForest, nameof(context));
            }

            return FindOneWithCredentialValidation(context, null, flag);
        }

        public static new GlobalCatalog FindOne(DirectoryContext context, string siteName, LocatorOptions flag)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.ContextType != DirectoryContextType.Forest)
            {
                throw new ArgumentException(SR.TargetShouldBeForest, nameof(context));
            }

            if (siteName == null)
            {
                throw new ArgumentNullException(nameof(siteName));
            }

            return FindOneWithCredentialValidation(context, siteName, flag);
        }

        public static new GlobalCatalogCollection FindAll(DirectoryContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.ContextType != DirectoryContextType.Forest)
            {
                throw new ArgumentException(SR.TargetShouldBeForest, nameof(context));
            }

            //  work with copy of the context
            context = new DirectoryContext(context);

            return FindAllInternal(context, null);
        }

        public static new GlobalCatalogCollection FindAll(DirectoryContext context, string siteName)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.ContextType != DirectoryContextType.Forest)
            {
                throw new ArgumentException(SR.TargetShouldBeForest, nameof(context));
            }

            if (siteName == null)
            {
                throw new ArgumentNullException(nameof(siteName));
            }

            //  work with copy of the context
            context = new DirectoryContext(context);

            return FindAllInternal(context, siteName);
        }

        public override GlobalCatalog EnableGlobalCatalog()
        {
            CheckIfDisposed();
            throw new InvalidOperationException(SR.CannotPerformOnGCObject);
        }

        public DomainController DisableGlobalCatalog()
        {
            CheckIfDisposed();
            CheckIfDisabled();

            // bind to the server object
            DirectoryEntry serverNtdsaEntry = directoryEntryMgr.GetCachedDirectoryEntry(NtdsaObjectName);

            // reset the NTDSDSA_OPT_IS_GC flag on the "options" property
            int options = 0;

            try
            {
                if (serverNtdsaEntry.Properties[PropertyManager.Options].Value != null)
                {
                    options = (int)serverNtdsaEntry.Properties[PropertyManager.Options].Value!;
                }

                serverNtdsaEntry.Properties[PropertyManager.Options].Value = options & (~1);
                serverNtdsaEntry.CommitChanges();
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }

            // mark as disbaled
            _disabled = true;

            // return a domain controller object
            return new DomainController(context, Name);
        }

        public override bool IsGlobalCatalog()
        {
            CheckIfDisposed();
            CheckIfDisabled();

            // since this is a global catalog object, this should always return true
            return true;
        }

        public ReadOnlyActiveDirectorySchemaPropertyCollection FindAllProperties()
        {
            CheckIfDisposed();
            CheckIfDisabled();

            // create an ActiveDirectorySchema object
            if (_schema == null)
            {
                string? schemaNC = null;
                try
                {
                    schemaNC = directoryEntryMgr.ExpandWellKnownDN(WellKnownDN.SchemaNamingContext);
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
                _ = Utils.GetNewDirectoryContext(Name, DirectoryContextType.DirectoryServer, context);
                _schema = new ActiveDirectorySchema(context, schemaNC);
            }

            // return the global catalog replicated properties
            return _schema.FindAllProperties(PropertyTypes.InGlobalCatalog);
        }

        public override DirectorySearcher GetDirectorySearcher()
        {
            CheckIfDisposed();
            CheckIfDisabled();

            return InternalGetDirectorySearcher();
        }

        #endregion public methods

        #region private methods

        private void CheckIfDisabled()
        {
            if (_disabled)
            {
                throw new InvalidOperationException(SR.GCDisabled);
            }
        }

        internal static new GlobalCatalog FindOneWithCredentialValidation(DirectoryContext context, string? siteName, LocatorOptions flag)
        {
            GlobalCatalog gc;
            bool retry = false;
            bool credsValidated = false;

            //  work with copy of the context
            context = new DirectoryContext(context);

            // authenticate against this GC to validate the credentials
            gc = FindOneInternal(context, context.Name, siteName, flag);
            try
            {
                ValidateCredential(gc, context);
                credsValidated = true;
            }
            catch (COMException e)
            {
                if (e.ErrorCode == unchecked((int)0x8007203a))
                {
                    // server is down , so try again with force rediscovery if the flags did not already contain force rediscovery
                    if ((flag & LocatorOptions.ForceRediscovery) == 0)
                    {
                        retry = true;
                    }
                    else
                    {
                        throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.GCNotFoundInForest, context.Name), typeof(GlobalCatalog), null);
                    }
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
            finally
            {
                if (!credsValidated)
                {
                    gc.Dispose();
                }
            }

            if (retry)
            {
                credsValidated = false;
                gc = FindOneInternal(context, context.Name, siteName, flag | LocatorOptions.ForceRediscovery);
                try
                {
                    ValidateCredential(gc, context);
                    credsValidated = true;
                }
                catch (COMException e)
                {
                    if (e.ErrorCode == unchecked((int)0x8007203a))
                    {
                        // server is down
                        throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.GCNotFoundInForest, context.Name), typeof(GlobalCatalog), null);
                    }
                    else
                    {
                        throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                    }
                }
                finally
                {
                    if (!credsValidated)
                    {
                        gc.Dispose();
                    }
                }
            }

            return gc;
        }

        internal static new GlobalCatalog FindOneInternal(DirectoryContext context, string? forestName, string? siteName, LocatorOptions flag)
        {
            DomainControllerInfo domainControllerInfo;
            int errorCode = 0;

            if (siteName != null && siteName.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, nameof(siteName));
            }

            // check that the flags passed have only the valid bits set
            if (((long)flag & (~((long)LocatorOptions.AvoidSelf | (long)LocatorOptions.ForceRediscovery | (long)LocatorOptions.KdcRequired | (long)LocatorOptions.TimeServerRequired | (long)LocatorOptions.WriteableRequired))) != 0)
            {
                throw new ArgumentException(SR.InvalidFlags, nameof(flag));
            }

            if (forestName == null)
            {
                // get the dns name of the logged on forest
                DomainControllerInfo tempDomainControllerInfo;
                int error = Locator.DsGetDcNameWrapper(null, DirectoryContext.GetLoggedOnDomain(), null, (long)PrivateLocatorFlags.DirectoryServicesRequired, out tempDomainControllerInfo);

                if (error == NativeMethods.ERROR_NO_SUCH_DOMAIN)
                {
                    // throw not found exception
                    throw new ActiveDirectoryObjectNotFoundException(SR.ContextNotAssociatedWithDomain, typeof(GlobalCatalog), null);
                }
                else if (error != 0)
                {
                    throw ExceptionHelper.GetExceptionFromErrorCode(errorCode);
                }

                Debug.Assert(tempDomainControllerInfo.DnsForestName != null);
                forestName = tempDomainControllerInfo.DnsForestName;
            }

            // call DsGetDcName
            errorCode = Locator.DsGetDcNameWrapper(null, forestName, siteName, (long)flag | (long)(PrivateLocatorFlags.GCRequired | PrivateLocatorFlags.DirectoryServicesRequired), out domainControllerInfo);

            if (errorCode == NativeMethods.ERROR_NO_SUCH_DOMAIN)
            {
                throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.GCNotFoundInForest, forestName), typeof(GlobalCatalog), null);
            }
            // this can only occur when flag is being explicitly passed (since the flags that we pass internally are valid)
            if (errorCode == NativeMethods.ERROR_INVALID_FLAGS)
            {
                throw new ArgumentException(SR.InvalidFlags, nameof(flag));
            }
            else if (errorCode != 0)
            {
                throw ExceptionHelper.GetExceptionFromErrorCode(errorCode);
            }

            // create a GlobalCatalog object
            // the name is returned in the form "\\servername", so skip the "\\"
            Debug.Assert(domainControllerInfo.DomainControllerName.Length > 2);
            string globalCatalogName = domainControllerInfo.DomainControllerName.Substring(2);

            // create a new context object for the global catalog
            DirectoryContext gcContext = Utils.GetNewDirectoryContext(globalCatalogName, DirectoryContextType.DirectoryServer, context);

            return new GlobalCatalog(gcContext, globalCatalogName);
        }

        internal static GlobalCatalogCollection FindAllInternal(DirectoryContext context, string? siteName)
        {
            ArrayList gcList = new ArrayList();

            if (siteName != null && siteName.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, nameof(siteName));
            }

            foreach (string gcName in Utils.GetReplicaList(context, null /* not specific to any partition */, siteName, false /* isDefaultNC */, false /* isADAM */, true /* mustBeGC */))
            {
                DirectoryContext gcContext = Utils.GetNewDirectoryContext(gcName, DirectoryContextType.DirectoryServer, context);
                gcList.Add(new GlobalCatalog(gcContext, gcName));
            }

            return new GlobalCatalogCollection(gcList);
        }

        private DirectorySearcher InternalGetDirectorySearcher()
        {
            DirectoryEntry de = new DirectoryEntry("GC://" + Name);

            de.AuthenticationType = Utils.DefaultAuthType | AuthenticationTypes.ServerBind;

            de.Username = context.UserName;
            de.Password = context.Password;

            return new DirectorySearcher(de);
        }

        #endregion
    }
}
