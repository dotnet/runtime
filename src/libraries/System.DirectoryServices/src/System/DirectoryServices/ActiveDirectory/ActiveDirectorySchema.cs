// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace System.DirectoryServices.ActiveDirectory
{
    public enum SchemaClassType : int
    {
        Type88 = 0,
        Structural = 1,
        Abstract = 2,
        Auxiliary = 3
    }

    [Flags]
    public enum PropertyTypes : int
    {
        Indexed = 2,
        InGlobalCatalog = 4
    }

    public class ActiveDirectorySchema : ActiveDirectoryPartition
    {
        private bool _disposed;
        private DirectoryEntry _schemaEntry;
        private DirectoryEntry? _abstractSchemaEntry;
        private DirectoryServer? _cachedSchemaRoleOwner;

        #region constructors
        internal ActiveDirectorySchema(DirectoryContext context, string distinguishedName)
            : base(context, distinguishedName)
        {
            this.directoryEntryMgr = new DirectoryEntryManager(context);
            _schemaEntry = DirectoryEntryManager.GetDirectoryEntry(context, distinguishedName);
        }

        internal ActiveDirectorySchema(DirectoryContext context, string distinguishedName, DirectoryEntryManager directoryEntryMgr)
            : base(context, distinguishedName)
        {
            this.directoryEntryMgr = directoryEntryMgr;
            _schemaEntry = DirectoryEntryManager.GetDirectoryEntry(context, distinguishedName);
        }
        #endregion constructors

        #region IDisposable
        // private Dispose method
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                try
                {
                    // if there are any managed or unmanaged
                    // resources to be freed, those should be done here
                    // if not an explicit dispose only unmanaged resources should
                    // be disposed
                    if (disposing)
                    {
                        // dispose schema entry
                        if (_schemaEntry != null)
                        {
                            _schemaEntry.Dispose();
                            _schemaEntry = null!;
                        }
                        // dispose the abstract schema entry
                        if (_abstractSchemaEntry != null)
                        {
                            _abstractSchemaEntry.Dispose();
                            _abstractSchemaEntry = null;
                        }
                    }
                    _disposed = true;
                }
                finally
                {
                    base.Dispose();
                }
            }
        }
        #endregion IDisposable

        #region public methods
        public static ActiveDirectorySchema GetSchema(DirectoryContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            // contexttype should be Forest, DirectoryServer or ConfigurationSet
            if ((context.ContextType != DirectoryContextType.Forest) &&
                (context.ContextType != DirectoryContextType.ConfigurationSet) &&
                (context.ContextType != DirectoryContextType.DirectoryServer))
            {
                throw new ArgumentException(SR.NotADOrADAM, nameof(context));
            }

            if ((context.Name == null) && (!context.isRootDomain()))
            {
                throw new ActiveDirectoryObjectNotFoundException(SR.ContextNotAssociatedWithDomain, typeof(ActiveDirectorySchema), null);
            }

            if (context.Name != null)
            {
                // the target should be a valid forest name or a server
                if (!((context.isRootDomain()) || (context.isADAMConfigSet()) || (context.isServer())))
                {
                    if (context.ContextType == DirectoryContextType.Forest)
                    {
                        throw new ActiveDirectoryObjectNotFoundException(SR.ForestNotFound, typeof(ActiveDirectorySchema), context.Name);
                    }
                    else if (context.ContextType == DirectoryContextType.ConfigurationSet)
                    {
                        throw new ActiveDirectoryObjectNotFoundException(SR.ConfigSetNotFound, typeof(ActiveDirectorySchema), context.Name);
                    }
                    else
                    {
                        throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.ServerNotFound, context.Name), typeof(ActiveDirectorySchema), null);
                    }
                }
            }

            //  work with copy of the context
            context = new DirectoryContext(context);

            DirectoryEntryManager directoryEntryMgr = new DirectoryEntryManager(context);
            string schemaNC;
            try
            {
                DirectoryEntry rootDSE = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.RootDSE);

                if ((context.isServer()) && (!Utils.CheckCapability(rootDSE, Capability.ActiveDirectoryOrADAM)))
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.ServerNotFound, context.Name), typeof(ActiveDirectorySchema), null);
                }

                schemaNC = (string)PropertyManager.GetPropertyValue(context, rootDSE, PropertyManager.SchemaNamingContext)!;
            }
            catch (COMException e)
            {
                int errorCode = e.ErrorCode;

                if (errorCode == unchecked((int)0x8007203a))
                {
                    if (context.ContextType == DirectoryContextType.Forest)
                    {
                        throw new ActiveDirectoryObjectNotFoundException(SR.ForestNotFound, typeof(ActiveDirectorySchema), context.Name);
                    }
                    else if (context.ContextType == DirectoryContextType.ConfigurationSet)
                    {
                        throw new ActiveDirectoryObjectNotFoundException(SR.ConfigSetNotFound, typeof(ActiveDirectorySchema), context.Name);
                    }
                    else
                    {
                        throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.ServerNotFound, context.Name), typeof(ActiveDirectorySchema), null);
                    }
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                if (context.ContextType == DirectoryContextType.ConfigurationSet)
                {
                    // this is the case where the context is a config set and we could not find an ADAM instance in that config set
                    throw new ActiveDirectoryObjectNotFoundException(SR.ConfigSetNotFound, typeof(ActiveDirectorySchema), context.Name);
                }
                else
                    throw;
            }

            return new ActiveDirectorySchema(context, schemaNC, directoryEntryMgr);
        }

        public void RefreshSchema()
        {
            CheckIfDisposed();

            // Refresh the schema on the server
            DirectoryEntry? rootDSE = null;
            try
            {
                rootDSE = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.RootDSE);
                rootDSE.Properties[PropertyManager.SchemaUpdateNow].Value = 1;
                rootDSE.CommitChanges();

                // refresh the schema on the client
                // bind to the abstract schema
                _abstractSchemaEntry ??= directoryEntryMgr.GetCachedDirectoryEntry("Schema");
                _abstractSchemaEntry.RefreshCache();
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            finally
            {
                rootDSE?.Dispose();
            }
        }

        //
        // This method finds only among non-defunct classes
        //
        public ActiveDirectorySchemaClass FindClass(string ldapDisplayName)
        {
            CheckIfDisposed();
            return ActiveDirectorySchemaClass.FindByName(context, ldapDisplayName);
        }

        //
        // This method finds only among defunct classes
        //
        public ActiveDirectorySchemaClass FindDefunctClass(string commonName)
        {
            CheckIfDisposed();

            if (commonName == null)
            {
                throw new ArgumentNullException(nameof(commonName));
            }

            if (commonName.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, nameof(commonName));
            }

            // this will bind to the schema container and load the properties of this class
            // (will also check whether or not the class exists)
            Hashtable propertiesFromServer = ActiveDirectorySchemaClass.GetPropertiesFromSchemaContainer(context, _schemaEntry, commonName, true /* isDefunctOnServer */);
            ActiveDirectorySchemaClass schemaClass = new ActiveDirectorySchemaClass(context, commonName, propertiesFromServer, _schemaEntry);

            return schemaClass;
        }

        //
        // This method returns  only non-defunct classes
        //
        public ReadOnlyActiveDirectorySchemaClassCollection FindAllClasses()
        {
            CheckIfDisposed();
            string filter = "(&(" + PropertyManager.ObjectCategory + "=classSchema)" +
                            "(!(" + PropertyManager.IsDefunct + "=TRUE)))";
            return GetAllClasses(context, _schemaEntry, filter);
        }

        //
        // This method returns only non-defunct classes of the specified type
        //
        public ReadOnlyActiveDirectorySchemaClassCollection FindAllClasses(SchemaClassType type)
        {
            CheckIfDisposed();

            // validate the type
            if (type < SchemaClassType.Type88 || type > SchemaClassType.Auxiliary)
            {
                throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SchemaClassType));
            }

            string filter = "(&(" + PropertyManager.ObjectCategory + "=classSchema)" +
                            "(" + PropertyManager.ObjectClassCategory + "=" + (int)type + ")" +
                            "(!(" + PropertyManager.IsDefunct + "=TRUE)))";
            return GetAllClasses(context, _schemaEntry, filter);
        }

        //
        // This method returns only defunct classes
        //
        public ReadOnlyActiveDirectorySchemaClassCollection FindAllDefunctClasses()
        {
            CheckIfDisposed();

            string filter = "(&(" + PropertyManager.ObjectCategory + "=classSchema)" +
                "(" + PropertyManager.IsDefunct + "=TRUE))";
            return GetAllClasses(context, _schemaEntry, filter);
        }

        //
        // This method finds only among non-defunct properties
        //
        public ActiveDirectorySchemaProperty FindProperty(string ldapDisplayName)
        {
            CheckIfDisposed();
            return ActiveDirectorySchemaProperty.FindByName(context, ldapDisplayName);
        }

        //
        // This method finds only among defunct properties
        //
        public ActiveDirectorySchemaProperty FindDefunctProperty(string commonName)
        {
            CheckIfDisposed();

            if (commonName == null)
            {
                throw new ArgumentNullException(nameof(commonName));
            }

            if (commonName.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, nameof(commonName));
            }

            // this will bind to the schema container and load the properties of this property
            // (will also check whether or not the property exists)
            SearchResult propertiesFromServer = ActiveDirectorySchemaProperty.GetPropertiesFromSchemaContainer(context, _schemaEntry, commonName, true /* isDefunctOnServer */);
            ActiveDirectorySchemaProperty schemaProperty = new ActiveDirectorySchemaProperty(context, commonName, propertiesFromServer, _schemaEntry);

            return schemaProperty;
        }

        //
        // This method returns  only non-defunct properties
        //
        public ReadOnlyActiveDirectorySchemaPropertyCollection FindAllProperties()
        {
            CheckIfDisposed();

            string filter = "(&(" + PropertyManager.ObjectCategory + "=attributeSchema)" +
                            "(!(" + PropertyManager.IsDefunct + "=TRUE)))";
            return GetAllProperties(context, _schemaEntry, filter);
        }

        //
        // This method returns  only non-defunct properties meeting the specified criteria
        //
        public ReadOnlyActiveDirectorySchemaPropertyCollection FindAllProperties(PropertyTypes type)
        {
            CheckIfDisposed();

            // check validity of type
            if ((type & (~(PropertyTypes.Indexed | PropertyTypes.InGlobalCatalog))) != 0)
            {
                throw new ArgumentException(SR.InvalidFlags, nameof(type));
            }

            // start the filter
            StringBuilder str = new StringBuilder(25);
            str.Append("(&(");
            str.Append(PropertyManager.ObjectCategory);
            str.Append("=attributeSchema)");
            str.Append("(!(");
            str.Append(PropertyManager.IsDefunct);
            str.Append("=TRUE))");

            if (((int)type & (int)PropertyTypes.Indexed) != 0)
            {
                str.Append('(');
                str.Append(PropertyManager.SearchFlags);
                str.Append(":1.2.840.113556.1.4.804:=");
                str.Append((int)SearchFlags.IsIndexed);
                str.Append(')');
            }

            if (((int)type & (int)PropertyTypes.InGlobalCatalog) != 0)
            {
                str.Append('(');
                str.Append(PropertyManager.IsMemberOfPartialAttributeSet);
                str.Append("=TRUE)");
            }

            str.Append(')'); // end filter
            return GetAllProperties(context, _schemaEntry, str.ToString());
        }

        //
        // This method returns only defunct properties
        //
        public ReadOnlyActiveDirectorySchemaPropertyCollection FindAllDefunctProperties()
        {
            CheckIfDisposed();

            string filter = "(&(" + PropertyManager.ObjectCategory + "=attributeSchema)" +
                            "(" + PropertyManager.IsDefunct + "=TRUE))";
            return GetAllProperties(context, _schemaEntry, filter);
        }

        public override DirectoryEntry GetDirectoryEntry()
        {
            CheckIfDisposed();
            return DirectoryEntryManager.GetDirectoryEntry(context, Name);
        }

        public static ActiveDirectorySchema GetCurrentSchema()
        {
            return ActiveDirectorySchema.GetSchema(new DirectoryContext(DirectoryContextType.Forest));
        }

        #endregion public methods

        #region public properties

        public DirectoryServer SchemaRoleOwner
        {
            get
            {
                CheckIfDisposed();
                return _cachedSchemaRoleOwner ??= GetSchemaRoleOwner();
            }
        }

        #endregion public properties

        #region private methods
        internal static ReadOnlyActiveDirectorySchemaPropertyCollection GetAllProperties(DirectoryContext context, DirectoryEntry schemaEntry, string filter)
        {
            ArrayList propertyList = new ArrayList();

            string[] propertiesToLoad = new string[3];
            propertiesToLoad[0] = PropertyManager.LdapDisplayName;
            propertiesToLoad[1] = PropertyManager.Cn;
            propertiesToLoad[2] = PropertyManager.IsDefunct;

            ADSearcher searcher = new ADSearcher(schemaEntry, filter, propertiesToLoad, SearchScope.OneLevel);
            SearchResultCollection? resCol = null;
            try
            {
                resCol = searcher.FindAll();
                foreach (SearchResult res in resCol)
                {
                    string ldapDisplayName = (string)PropertyManager.GetSearchResultPropertyValue(res, PropertyManager.LdapDisplayName)!;
                    DirectoryEntry directoryEntry = res.GetDirectoryEntry();

                    directoryEntry.AuthenticationType = Utils.DefaultAuthType;
                    directoryEntry.Username = context.UserName;
                    directoryEntry.Password = context.Password;

                    bool isDefunct = false;

                    if ((res.Properties[PropertyManager.IsDefunct] != null) && (res.Properties[PropertyManager.IsDefunct].Count > 0))
                    {
                        isDefunct = (bool)res.Properties[PropertyManager.IsDefunct][0]!;
                    }

                    if (isDefunct)
                    {
                        string commonName = (string)PropertyManager.GetSearchResultPropertyValue(res, PropertyManager.Cn)!;
                        propertyList.Add(new ActiveDirectorySchemaProperty(context, commonName, ldapDisplayName, directoryEntry, schemaEntry));
                    }
                    else
                    {
                        propertyList.Add(new ActiveDirectorySchemaProperty(context, ldapDisplayName, directoryEntry, schemaEntry));
                    }
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            finally
            {
                // dispose off the result collection
                resCol?.Dispose();
            }

            return new ReadOnlyActiveDirectorySchemaPropertyCollection(propertyList);
        }

        internal static ReadOnlyActiveDirectorySchemaClassCollection GetAllClasses(DirectoryContext context, DirectoryEntry schemaEntry, string filter)
        {
            ArrayList classList = new ArrayList();

            string[] propertiesToLoad = new string[3];
            propertiesToLoad[0] = PropertyManager.LdapDisplayName;
            propertiesToLoad[1] = PropertyManager.Cn;
            propertiesToLoad[2] = PropertyManager.IsDefunct;

            ADSearcher searcher = new ADSearcher(schemaEntry, filter, propertiesToLoad, SearchScope.OneLevel);
            SearchResultCollection? resCol = null;
            try
            {
                resCol = searcher.FindAll();
                foreach (SearchResult res in resCol)
                {
                    string ldapDisplayName = (string)PropertyManager.GetSearchResultPropertyValue(res, PropertyManager.LdapDisplayName)!;
                    DirectoryEntry directoryEntry = res.GetDirectoryEntry();

                    directoryEntry.AuthenticationType = Utils.DefaultAuthType;
                    directoryEntry.Username = context.UserName;
                    directoryEntry.Password = context.Password;

                    bool isDefunct = false;

                    if ((res.Properties[PropertyManager.IsDefunct] != null) && (res.Properties[PropertyManager.IsDefunct].Count > 0))
                    {
                        isDefunct = (bool)res.Properties[PropertyManager.IsDefunct][0]!;
                    }

                    if (isDefunct)
                    {
                        string commonName = (string)PropertyManager.GetSearchResultPropertyValue(res, PropertyManager.Cn)!;
                        classList.Add(new ActiveDirectorySchemaClass(context, commonName, ldapDisplayName, directoryEntry, schemaEntry));
                    }
                    else
                    {
                        classList.Add(new ActiveDirectorySchemaClass(context, ldapDisplayName, directoryEntry, schemaEntry));
                    }
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
            finally
            {
                // dispose off the result collection
                resCol?.Dispose();
            }

            return new ReadOnlyActiveDirectorySchemaClassCollection(classList);
        }

        private DirectoryServer GetSchemaRoleOwner()
        {
            try
            {
                _schemaEntry.RefreshCache();

                if (context.isADAMConfigSet())
                {
                    // ADAM
                    string adamInstName = Utils.GetAdamDnsHostNameFromNTDSA(context, (string)PropertyManager.GetPropertyValue(context, _schemaEntry, PropertyManager.FsmoRoleOwner)!);
                    DirectoryContext adamInstContext = Utils.GetNewDirectoryContext(adamInstName, DirectoryContextType.DirectoryServer, context);
                    return new AdamInstance(adamInstContext, adamInstName);
                }
                else
                {
                    // could be AD or adam server

                    DirectoryServer? server = null;
                    DirectoryEntry rootDSE = directoryEntryMgr.GetCachedDirectoryEntry(WellKnownDN.RootDSE);

                    if (Utils.CheckCapability(rootDSE, Capability.ActiveDirectory))
                    {
                        string dcName = Utils.GetDnsHostNameFromNTDSA(context, (string)PropertyManager.GetPropertyValue(context, _schemaEntry, PropertyManager.FsmoRoleOwner)!);
                        DirectoryContext dcContext = Utils.GetNewDirectoryContext(dcName, DirectoryContextType.DirectoryServer, context);
                        server = new DomainController(dcContext, dcName);
                    }
                    else
                    {
                        // ADAM case again
                        string adamInstName = Utils.GetAdamDnsHostNameFromNTDSA(context, (string)PropertyManager.GetPropertyValue(context, _schemaEntry, PropertyManager.FsmoRoleOwner)!);
                        DirectoryContext adamInstContext = Utils.GetNewDirectoryContext(adamInstName, DirectoryContextType.DirectoryServer, context);
                        server = new AdamInstance(adamInstContext, adamInstName);
                    }
                    return server;
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }
        }

        #endregion private methods
    }
}
