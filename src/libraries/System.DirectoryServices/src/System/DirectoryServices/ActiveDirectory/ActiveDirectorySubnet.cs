// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Diagnostics;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ActiveDirectorySubnet : IDisposable
    {
        private ActiveDirectorySite? _site;
        private readonly string _name;
        internal readonly DirectoryContext context;
        private bool _disposed;

        internal bool existing;
        internal DirectoryEntry cachedEntry = null!;

        public static ActiveDirectorySubnet FindByName(DirectoryContext context, string subnetName)
        {
            ValidateArgument(context, subnetName);

            //  work with copy of the context
            context = new DirectoryContext(context);

            // bind to the rootdse to get the configurationnamingcontext
            DirectoryEntry de;

            try
            {
                de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
                string config = (string)PropertyManager.GetPropertyValue(context, de, PropertyManager.ConfigurationNamingContext)!;
                string subnetdn = "CN=Subnets,CN=Sites," + config;
                de = DirectoryEntryManager.GetDirectoryEntry(context, subnetdn);
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                // this is the case where the context is a config set and we could not find an ADAM instance in that config set
                throw new ActiveDirectoryOperationException(SR.Format(SR.ADAMInstanceNotFoundInConfigSet, context.Name));
            }

            try
            {
                ADSearcher adSearcher = new ADSearcher(de,
                                                      "(&(objectClass=subnet)(objectCategory=subnet)(name=" + Utils.GetEscapedFilterValue(subnetName) + "))",
                                                      new string[] { "distinguishedName" },
                                                      SearchScope.OneLevel,
                                                      false, /* don't need paged search */
                                                      false /* don't need to cache result */);
                SearchResult? srchResult = adSearcher.FindOne();
                if (srchResult == null)
                {
                    // no such subnet object
                    Exception e = new ActiveDirectoryObjectNotFoundException(SR.DSNotFound, typeof(ActiveDirectorySubnet), subnetName);
                    throw e;
                }
                else
                {
                    string? siteName = null;
                    DirectoryEntry connectionEntry = srchResult.GetDirectoryEntry();
                    // try to get the site that this subnet lives in
                    if (connectionEntry.Properties.Contains("siteObject"))
                    {
                        NativeComInterfaces.IAdsPathname pathCracker = (NativeComInterfaces.IAdsPathname)new NativeComInterfaces.Pathname();
                        // need to turn off the escaping for name
                        pathCracker.EscapedMode = NativeComInterfaces.ADS_ESCAPEDMODE_OFF_EX;

                        string tmp = (string)connectionEntry.Properties["siteObject"][0]!;
                        // escaping manipulation
                        pathCracker.Set(tmp, NativeComInterfaces.ADS_SETTYPE_DN);
                        string rdn = pathCracker.Retrieve(NativeComInterfaces.ADS_FORMAT_LEAF);
                        Debug.Assert(rdn != null && Utils.Compare(rdn, 0, 3, "CN=", 0, 3) == 0);
                        siteName = rdn.Substring(3);
                    }

                    // it is an existing subnet object
                    ActiveDirectorySubnet? subnet = null;
                    if (siteName == null)
                        subnet = new ActiveDirectorySubnet(context, subnetName, null, true);
                    else
                        subnet = new ActiveDirectorySubnet(context, subnetName, siteName, true);

                    subnet.cachedEntry = connectionEntry;
                    return subnet;
                }
            }
            catch (COMException e)
            {
                if (e.ErrorCode == unchecked((int)0x80072030))
                {
                    // object is not found since we cannot even find the container in which to search
                    throw new ActiveDirectoryObjectNotFoundException(SR.DSNotFound, typeof(ActiveDirectorySubnet), subnetName);
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
            finally
            {
                if (de != null)
                    de.Dispose();
            }
        }

        public ActiveDirectorySubnet(DirectoryContext context, string subnetName)
        {
            ValidateArgument(context, subnetName);

            //  work with copy of the context
            context = new DirectoryContext(context);

            this.context = context;
            _name = subnetName;

            // bind to the rootdse to get the configurationnamingcontext
            DirectoryEntry? de = null;

            try
            {
                de = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
                string config = (string)PropertyManager.GetPropertyValue(context, de, PropertyManager.ConfigurationNamingContext)!;
                string subnetn = "CN=Subnets,CN=Sites," + config;
                // bind to the subnet container
                de = DirectoryEntryManager.GetDirectoryEntry(context, subnetn);

                string rdn = "cn=" + _name;
                rdn = Utils.GetEscapedPath(rdn);
                cachedEntry = de.Children.Add(rdn, "subnet");
            }
            catch (COMException e)
            {
                ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                // this is the case where the context is a config set and we could not find an ADAM instance in that config set
                throw new ActiveDirectoryOperationException(SR.Format(SR.ADAMInstanceNotFoundInConfigSet, context.Name));
            }
            finally
            {
                if (de != null)
                    de.Dispose();
            }
        }

        public ActiveDirectorySubnet(DirectoryContext context, string subnetName, string siteName) : this(context, subnetName)
        {
            if (siteName == null)
                throw new ArgumentNullException(nameof(siteName));

            if (siteName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(siteName));

            // validate that siteName is valid
            try
            {
                _site = ActiveDirectorySite.FindByName(this.context, siteName);
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                throw new ArgumentException(SR.Format(SR.SiteNotExist, siteName), nameof(siteName));
            }
        }

        internal ActiveDirectorySubnet(DirectoryContext context, string subnetName, string? siteName, bool existing)
        {
            Debug.Assert(existing == true);

            this.context = context;
            _name = subnetName;

            if (siteName != null)
            {
                try
                {
                    _site = ActiveDirectorySite.FindByName(context, siteName);
                }
                catch (ActiveDirectoryObjectNotFoundException)
                {
                    throw new ArgumentException(SR.Format(SR.SiteNotExist, siteName), nameof(siteName));
                }
            }

            this.existing = true;
        }

        public string Name
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                return _name;
            }
        }

        public ActiveDirectorySite? Site
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                return _site;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (value != null)
                {
                    // check whether the site exists or not, you can not create a new site and set it to a subnet object with commit change to site object first
                    if (!value.existing)
                        throw new InvalidOperationException(SR.Format(SR.SiteNotCommitted, value));
                }

                _site = value;
            }
        }

        public string? Location
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                try
                {
                    if (cachedEntry.Properties.Contains("location"))
                        return (string)cachedEntry.Properties["location"][0]!;
                    else
                        return null;
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);

                // if the value is null, it means that user wants to clear the value
                try
                {
                    if (value == null)
                    {
                        if (cachedEntry.Properties.Contains("location"))
                            cachedEntry.Properties["location"].Clear();
                    }
                    else
                    {
                        cachedEntry.Properties["location"].Value = value;
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        public void Save()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            try
            {
                if (existing)
                {
                    // check whether site has been changed or not
                    if (_site == null)
                    {
                        // user wants to remove this subnet object from previous site
                        if (cachedEntry.Properties.Contains("siteObject"))
                            cachedEntry.Properties["siteObject"].Clear();
                    }
                    else
                    {
                        // user configures this subnet object to a particular site
                        cachedEntry.Properties["siteObject"].Value = _site.cachedEntry.Properties["distinguishedName"][0];
                    }
                    cachedEntry.CommitChanges();
                }
                else
                {
                    if (Site != null)
                        cachedEntry.Properties["siteObject"].Add(_site!.cachedEntry.Properties["distinguishedName"][0]);

                    cachedEntry.CommitChanges();

                    // the subnet has been created in the backend store
                    existing = true;
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
        }

        public void Delete()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (!existing)
            {
                throw new InvalidOperationException(SR.CannotDelete);
            }
            else
            {
                try
                {
                    cachedEntry.Parent.Children.Remove(cachedEntry);
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
        }

        public override string ToString()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            return Name;
        }

        public DirectoryEntry GetDirectoryEntry()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (!existing)
            {
                throw new InvalidOperationException(SR.CannotGetObject);
            }
            else
            {
                return DirectoryEntryManager.GetDirectoryEntryInternal(context, cachedEntry.Path);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free other state (managed objects)
                if (cachedEntry != null)
                    cachedEntry.Dispose();
            }

            // free your own state (unmanaged objects)

            _disposed = true;
        }

        private static void ValidateArgument(DirectoryContext context, string subnetName)
        {
            // basic validation first
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // if target is not specified, then we determin the target from the logon credential, so if it is a local user context, it should fail
            if ((context.Name == null) && (!context.isRootDomain()))
            {
                throw new ArgumentException(SR.ContextNotAssociatedWithDomain, nameof(context));
            }

            // more validation for the context, if the target is not null, then it should be either forest name or server name
            if (context.Name != null)
            {
                // we only allow target to be forest, server name or ADAM config set
                if (!(context.isRootDomain() || context.isServer() || context.isADAMConfigSet()))
                    throw new ArgumentException(SR.NotADOrADAM, nameof(context));
            }

            if (subnetName == null)
                throw new ArgumentNullException(nameof(subnetName));

            if (subnetName.Length == 0)
                throw new ArgumentException(SR.EmptyStringParameter, nameof(subnetName));
        }
    }
}
