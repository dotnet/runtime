// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.ActiveDirectory
{
    internal enum SearchFlags : int
    {
        None = 0,
        IsIndexed = 1,
        IsIndexedOverContainer = 2,
        IsInAnr = 4,
        IsOnTombstonedObject = 8,
        IsTupleIndexed = 32
    }

    public class ActiveDirectorySchemaProperty : IDisposable
    {
        // private variables
        private DirectoryEntry? _schemaEntry;
        private DirectoryEntry? _propertyEntry;
        private DirectoryEntry? _abstractPropertyEntry;
        private readonly NativeComInterfaces.IAdsProperty? _iadsProperty;
        private readonly DirectoryContext _context;
        internal bool isBound;
        private bool _disposed;
        private ActiveDirectorySchema? _schema;
        private bool _propertiesFromSchemaContainerInitialized;
        private bool _isDefunctOnServer;
        private SearchResult? _propertyValuesFromServer;

        // private variables for caching properties
        private readonly string _ldapDisplayName;
        private string? _commonName;
        private string? _oid;
        private ActiveDirectorySyntax _syntax = (ActiveDirectorySyntax)(-1);
        private bool _syntaxInitialized;
        private string? _description;
        private bool _descriptionInitialized;
        private bool _isSingleValued;
        private bool _isSingleValuedInitialized;
        private bool _isInGlobalCatalog;
        private bool _isInGlobalCatalogInitialized;
        private Nullable<int> _rangeLower;
        private bool _rangeLowerInitialized;
        private Nullable<int> _rangeUpper;
        private bool _rangeUpperInitialized;
        private bool _isDefunct;
        private SearchFlags _searchFlags = SearchFlags.None;
        private bool _searchFlagsInitialized;
        private ActiveDirectorySchemaProperty? _linkedProperty;
        private bool _linkedPropertyInitialized;
        private Nullable<int> _linkId;
        private bool _linkIdInitialized;
        private byte[]? _schemaGuidBinaryForm;

        // OMObjectClass values for the syntax
        //0x2B0C0287731C00854A
        private static readonly OMObjectClass s_dnOMObjectClass = new OMObjectClass(new byte[] { 0x2B, 0x0C, 0x02, 0x87, 0x73, 0x1C, 0x00, 0x85, 0x4A });
        //0x2A864886F7140101010C
        private static readonly OMObjectClass s_dNWithStringOMObjectClass = new OMObjectClass(new byte[] { 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x14, 0x01, 0x01, 0x01, 0x0C });
        //0x2A864886F7140101010B
        private static readonly OMObjectClass s_dNWithBinaryOMObjectClass = new OMObjectClass(new byte[] { 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x14, 0x01, 0x01, 0x01, 0x0B });
        //0x2A864886F71401010106
        private static readonly OMObjectClass s_replicaLinkOMObjectClass = new OMObjectClass(new byte[] { 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x14, 0x01, 0x01, 0x01, 0x06 });
        //0x2B0C0287731C00855C
        private static readonly OMObjectClass s_presentationAddressOMObjectClass = new OMObjectClass(new byte[] { 0x2B, 0x0C, 0x02, 0x87, 0x73, 0x1C, 0x00, 0x85, 0x5C });
        //0x2B0C0287731C00853E
        private static readonly OMObjectClass s_accessPointDnOMObjectClass = new OMObjectClass(new byte[] { 0x2B, 0x0C, 0x02, 0x87, 0x73, 0x1C, 0x00, 0x85, 0x3E });
        //0x56060102050B1D
        private static readonly OMObjectClass s_oRNameOMObjectClass = new OMObjectClass(new byte[] { 0x56, 0x06, 0x01, 0x02, 0x05, 0x0B, 0x1D });

        // syntaxes
        private static readonly Syntax[] s_syntaxes = {/* CaseExactString */ new Syntax("2.5.5.3", 27, null),
                                              /* CaseIgnoreString */ new Syntax("2.5.5.4", 20, null),
                                              /* NumericString */ new Syntax("2.5.5.6", 18, null),
                                              /* DirectoryString */ new Syntax("2.5.5.12", 64, null),
                                              /* OctetString */ new Syntax("2.5.5.10", 4, null),
                                              /* SecurityDescriptor */ new Syntax("2.5.5.15", 66, null),
                                              /* Int */ new Syntax("2.5.5.9", 2, null),
                                              /* Int64 */ new Syntax("2.5.5.16", 65, null),
                                              /* Bool */ new Syntax("2.5.5.8", 1, null),
                                              /* Oid */ new Syntax("2.5.5.2", 6, null),
                                              /* GeneralizedTime */ new Syntax("2.5.5.11", 24, null),
                                              /* UtcTime */ new Syntax("2.5.5.11", 23, null),
                                              /* DN */ new Syntax("2.5.5.1", 127, s_dnOMObjectClass),
                                              /* DNWithBinary */ new Syntax("2.5.5.7", 127, s_dNWithBinaryOMObjectClass),
                                              /* DNWithString */ new Syntax("2.5.5.14", 127, s_dNWithStringOMObjectClass),
                                              /* Enumeration */ new Syntax("2.5.5.9", 10, null),
                                              /* IA5String */ new Syntax("2.5.5.5", 22, null),
                                              /* PrintableString */ new Syntax("2.5.5.5", 19, null),
                                              /* Sid */ new Syntax("2.5.5.17", 4, null),
                                              /* AccessPointDN */ new Syntax("2.5.5.14", 127, s_accessPointDnOMObjectClass),
                                              /* ORName */ new Syntax("2.5.5.7", 127, s_oRNameOMObjectClass),
                                              /* PresentationAddress */ new Syntax("2.5.5.13", 127, s_presentationAddressOMObjectClass),
                                              /* ReplicaLink */ new Syntax("2.5.5.10", 127, s_replicaLinkOMObjectClass)};

        #region constructors
        public ActiveDirectorySchemaProperty(DirectoryContext context, string ldapDisplayName)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if ((context.Name == null) && (!context.isRootDomain()))
            {
                throw new ArgumentException(SR.ContextNotAssociatedWithDomain, nameof(context));
            }

            if (context.Name != null)
            {
                // the target should be a valid forest name or a server
                if (!((context.isRootDomain()) || (context.isADAMConfigSet()) || (context.isServer())))
                {
                    throw new ArgumentException(SR.NotADOrADAM, nameof(context));
                }
            }

            if (ldapDisplayName == null)
            {
                throw new ArgumentNullException(nameof(ldapDisplayName));
            }

            if (ldapDisplayName.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, nameof(ldapDisplayName));
            }

            _context = new DirectoryContext(context);

            // validate the context
            _schemaEntry = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.SchemaNamingContext);
            _schemaEntry.Bind(true);

            _ldapDisplayName = ldapDisplayName;
            // common name of the property defaults to the ldap display name
            _commonName = ldapDisplayName;

            // set the bind flag
            this.isBound = false;
        }

        // internal constructor
        internal ActiveDirectorySchemaProperty(DirectoryContext context, string ldapDisplayName, DirectoryEntry? propertyEntry, DirectoryEntry? schemaEntry)
        {
            _context = context;
            _ldapDisplayName = ldapDisplayName;

            // common name of the property defaults to the ldap display name
            _propertyEntry = propertyEntry;
            _isDefunctOnServer = false;
            _isDefunct = _isDefunctOnServer;

            try
            {
                // initialize the directory entry for the abstract schema class
                _abstractPropertyEntry = DirectoryEntryManager.GetDirectoryEntryInternal(context, "LDAP://" + context.GetServerName() + "/schema/" + ldapDisplayName);
                _iadsProperty = (NativeComInterfaces.IAdsProperty)_abstractPropertyEntry.NativeObject;
            }
            catch (COMException e)
            {
                if (e.ErrorCode == unchecked((int)0x80005000))
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.DSNotFound, typeof(ActiveDirectorySchemaProperty), ldapDisplayName);
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
            catch (InvalidCastException)
            {
                // this means that we found an object but it is not a schema class
                throw new ActiveDirectoryObjectNotFoundException(SR.DSNotFound, typeof(ActiveDirectorySchemaProperty), ldapDisplayName);
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                // this is the case where the context is a config set and we could not find an ADAM instance in that config set
                throw new ActiveDirectoryOperationException(SR.Format(SR.ADAMInstanceNotFoundInConfigSet, context.Name));
            }

            // set the bind flag
            this.isBound = true;
        }

        internal ActiveDirectorySchemaProperty(DirectoryContext context, string commonName, SearchResult propertyValuesFromServer, DirectoryEntry schemaEntry)
        {
            _context = context;
            _schemaEntry = schemaEntry;

            // all relevant properties have already been retrieved from the server
            _propertyValuesFromServer = propertyValuesFromServer;
            Debug.Assert(_propertyValuesFromServer != null);
            _propertiesFromSchemaContainerInitialized = true;
            _propertyEntry = GetSchemaPropertyDirectoryEntry();

            // names
            _commonName = commonName;
            _ldapDisplayName = (string)GetValueFromCache(PropertyManager.LdapDisplayName, true)!;

            // this constructor is only called for defunct classes
            _isDefunctOnServer = true;
            _isDefunct = _isDefunctOnServer;

            // set the bind flag
            this.isBound = true;
        }

        internal ActiveDirectorySchemaProperty(DirectoryContext context, string commonName, string ldapDisplayName, DirectoryEntry propertyEntry, DirectoryEntry schemaEntry)
        {
            _context = context;
            _schemaEntry = schemaEntry;
            _propertyEntry = propertyEntry;

            // names
            _commonName = commonName;
            _ldapDisplayName = ldapDisplayName;

            // this constructor is only called for defunct properties
            _isDefunctOnServer = true;
            _isDefunct = _isDefunctOnServer;

            // set the bind flag
            this.isBound = true;
        }

        #endregion constructors

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
        }

        // private Dispose method
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                // check if this is an explicit Dispose
                // only then clean up the directory entries
                if (disposing)
                {
                    // dispose schema entry
                    if (_schemaEntry != null)
                    {
                        _schemaEntry.Dispose();
                        _schemaEntry = null;
                    }
                    // dispose property entry
                    if (_propertyEntry != null)
                    {
                        _propertyEntry.Dispose();
                        _propertyEntry = null;
                    }
                    // dispose abstract class entry
                    if (_abstractPropertyEntry != null)
                    {
                        _abstractPropertyEntry.Dispose();
                        _abstractPropertyEntry = null;
                    }
                    // dispose the schema object
                    if (_schema != null)
                    {
                        _schema.Dispose();
                    }
                }

                _disposed = true;
            }
        }
        #endregion IDisposable

        #region public methods
        public static ActiveDirectorySchemaProperty FindByName(DirectoryContext context, string ldapDisplayName)
        {
            ActiveDirectorySchemaProperty? schemaProperty = null;

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if ((context.Name == null) && (!context.isRootDomain()))
            {
                throw new ArgumentException(SR.ContextNotAssociatedWithDomain, nameof(context));
            }

            if (context.Name != null)
            {
                // the target should be a valid forest name or a server
                if (!((context.isRootDomain()) || (context.isADAMConfigSet()) || (context.isServer())))
                {
                    throw new ArgumentException(SR.NotADOrADAM, nameof(context));
                }
            }

            if (ldapDisplayName == null)
            {
                throw new ArgumentNullException(nameof(ldapDisplayName));
            }

            if (ldapDisplayName.Length == 0)
            {
                throw new ArgumentException(SR.EmptyStringParameter, nameof(ldapDisplayName));
            }

            //  work with copy of the context
            context = new DirectoryContext(context);

            // create a schema property
            schemaProperty = new ActiveDirectorySchemaProperty(context, ldapDisplayName, (DirectoryEntry?)null, null);

            return schemaProperty;
        }

        public void Save()
        {
            CheckIfDisposed();

            if (!isBound)
            {
                try
                {
                    // create a new directory entry for this class
                    if (_schemaEntry == null)
                    {
                        _schemaEntry = DirectoryEntryManager.GetDirectoryEntry(_context, WellKnownDN.SchemaNamingContext);
                    }

                    // this will create the class and set the CN value
                    string rdn = "CN=" + _commonName;
                    rdn = Utils.GetEscapedPath(rdn);
                    _propertyEntry = _schemaEntry.Children.Add(rdn, "attributeSchema");
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                }
                catch (ActiveDirectoryObjectNotFoundException)
                {
                    // this is the case where the context is a config set and we could not find an ADAM instance in that config set
                    throw new ActiveDirectoryOperationException(SR.Format(SR.ADAMInstanceNotFoundInConfigSet, _context.Name));
                }

                // set the ldap display name property
                SetProperty(PropertyManager.LdapDisplayName, _ldapDisplayName);

                // set the oid value
                SetProperty(PropertyManager.AttributeID, _oid);

                // set the syntax
                if (_syntax != (ActiveDirectorySyntax)(-1))
                {
                    SetSyntax(_syntax);
                }

                // set the description
                SetProperty(PropertyManager.Description, _description);

                // set the isSingleValued attribute
                _propertyEntry.Properties[PropertyManager.IsSingleValued].Value = _isSingleValued;

                // set the isGlobalCatalogReplicated attribute
                _propertyEntry.Properties[PropertyManager.IsMemberOfPartialAttributeSet].Value = _isInGlobalCatalog;

                // set the isDefunct attribute
                _propertyEntry.Properties[PropertyManager.IsDefunct].Value = _isDefunct;

                // set the range lower attribute
                if (_rangeLower != null)
                {
                    _propertyEntry.Properties[PropertyManager.RangeLower].Value = (int)_rangeLower.Value;
                }

                // set the range upper attribute
                if (_rangeUpper != null)
                {
                    _propertyEntry.Properties[PropertyManager.RangeUpper].Value = (int)_rangeUpper.Value;
                }

                // set the searchFlags attribute
                if (_searchFlags != SearchFlags.None)
                {
                    _propertyEntry.Properties[PropertyManager.SearchFlags].Value = (int)_searchFlags;
                }

                // set the link id
                if (_linkId != null)
                {
                    _propertyEntry.Properties[PropertyManager.LinkID].Value = (int)_linkId.Value;
                }

                // set the schemaIDGuid property
                if (_schemaGuidBinaryForm != null)
                {
                    SetProperty(PropertyManager.SchemaIDGuid, _schemaGuidBinaryForm);
                }
            }

            try
            {
                // commit the classEntry to server
                _propertyEntry!.CommitChanges();

                // Refresh the schema cache on the schema role owner
                if (_schema == null)
                {
                    ActiveDirectorySchema schemaObject = ActiveDirectorySchema.GetSchema(_context);
                    bool alreadyUsingSchemaRoleOwnerContext = false;
                    DirectoryServer? schemaRoleOwner = null;

                    try
                    {
                        //
                        // if we are not already talking to the schema role owner, change the context
                        //
                        schemaRoleOwner = schemaObject.SchemaRoleOwner;
                        if (Utils.Compare(schemaRoleOwner.Name, _context.GetServerName()) != 0)
                        {
                            DirectoryContext schemaRoleOwnerContext = Utils.GetNewDirectoryContext(schemaRoleOwner.Name, DirectoryContextType.DirectoryServer, _context);
                            _schema = ActiveDirectorySchema.GetSchema(schemaRoleOwnerContext);
                        }
                        else
                        {
                            alreadyUsingSchemaRoleOwnerContext = true;
                            _schema = schemaObject;
                        }
                    }
                    finally
                    {
                        if (schemaRoleOwner != null)
                        {
                            schemaRoleOwner.Dispose();
                        }
                        if (!alreadyUsingSchemaRoleOwnerContext)
                        {
                            schemaObject.Dispose();
                        }
                    }
                }
                _schema.RefreshSchema();
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
            }

            // now that the changes are committed to the server
            // update the defunct/non-defunct status of the class on the server
            _isDefunctOnServer = _isDefunct;

            // invalidate all properties
            _commonName = null;
            _oid = null;
            _syntaxInitialized = false;
            _descriptionInitialized = false;
            _isSingleValuedInitialized = false;
            _isInGlobalCatalogInitialized = false;
            _rangeLowerInitialized = false;
            _rangeUpperInitialized = false;
            _searchFlagsInitialized = false;
            _linkedPropertyInitialized = false;
            _linkIdInitialized = false;
            _schemaGuidBinaryForm = null;
            _propertiesFromSchemaContainerInitialized = false;

            // set bind flag
            isBound = true;
        }

        public override string ToString() => Name;

        public DirectoryEntry GetDirectoryEntry()
        {
            CheckIfDisposed();

            if (!isBound)
            {
                throw new InvalidOperationException(SR.CannotGetObject);
            }

            GetSchemaPropertyDirectoryEntry();
            Debug.Assert(_propertyEntry != null);

            return DirectoryEntryManager.GetDirectoryEntryInternal(_context, _propertyEntry.Path);
        }
        #endregion public methods

        #region public properties

        public string Name
        {
            get
            {
                CheckIfDisposed();
                return _ldapDisplayName;
            }
        }

        public string? CommonName
        {
            get
            {
                CheckIfDisposed();

                if (isBound)
                {
                    if (_commonName == null)
                    {
                        // get the property from the server
                        _commonName = (string)GetValueFromCache(PropertyManager.Cn, true)!;
                    }
                }
                return _commonName;
            }
            set
            {
                CheckIfDisposed();

                if (value != null && value.Length == 0)
                    throw new ArgumentException(SR.EmptyStringParameter, nameof(value));

                if (isBound)
                {
                    // set the value on the directory entry
                    SetProperty(PropertyManager.Cn, value);
                }
                _commonName = value;
            }
        }

        public string? Oid
        {
            get
            {
                CheckIfDisposed();

                if (isBound)
                {
                    if (_oid == null)
                    {
                        // get the property from the abstract schema/ schema container
                        // (for non-defunt classes this property is available in the abstract schema)
                        if (!_isDefunctOnServer)
                        {
                            try
                            {
                                _oid = _iadsProperty!.OID;
                            }
                            catch (COMException e)
                            {
                                throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                            }
                        }
                        else
                        {
                            _oid = (string)GetValueFromCache(PropertyManager.AttributeID, true)!;
                        }
                    }
                }
                return _oid;
            }
            set
            {
                CheckIfDisposed();

                if (value != null && value.Length == 0)
                    throw new ArgumentException(SR.EmptyStringParameter, nameof(value));

                if (isBound)
                {
                    // set the value on the directory entry
                    SetProperty(PropertyManager.AttributeID, value);
                }
                _oid = value;
            }
        }

        public ActiveDirectorySyntax Syntax
        {
            get
            {
                CheckIfDisposed();

                if (isBound)
                {
                    if (!_syntaxInitialized)
                    {
                        byte[]? omObjectClassBinaryForm = (byte[]?)GetValueFromCache(PropertyManager.OMObjectClass, false);
                        OMObjectClass? omObjectClass = (omObjectClassBinaryForm != null) ? new OMObjectClass(omObjectClassBinaryForm) : null;

                        _syntax = MapSyntax((string)GetValueFromCache(PropertyManager.AttributeSyntax, true)!,
                            (int)GetValueFromCache(PropertyManager.OMSyntax, true)!,
                            omObjectClass);
                        _syntaxInitialized = true;
                    }
                }
                return _syntax;
            }
            set
            {
                CheckIfDisposed();

                if (value < ActiveDirectorySyntax.CaseExactString || value > ActiveDirectorySyntax.ReplicaLink)
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ActiveDirectorySyntax));
                }

                if (isBound)
                {
                    // set the value on the directory entry
                    SetSyntax(value);
                }
                _syntax = value;
            }
        }

        public string? Description
        {
            get
            {
                CheckIfDisposed();

                if (isBound)
                {
                    if (!_descriptionInitialized)
                    {
                        // get the property from the server
                        _description = (string?)GetValueFromCache(PropertyManager.Description, false);
                        _descriptionInitialized = true;
                    }
                }
                return _description;
            }
            set
            {
                CheckIfDisposed();

                if (value != null && value.Length == 0)
                    throw new ArgumentException(SR.EmptyStringParameter, nameof(value));

                if (isBound)
                {
                    // set the value on the directory entry
                    SetProperty(PropertyManager.Description, value);
                }
                _description = value;
            }
        }

        public bool IsSingleValued
        {
            get
            {
                CheckIfDisposed();

                if (isBound)
                {
                    if (!_isSingleValuedInitialized)
                    {
                        // get the property from the abstract schema/ schema container
                        // (for non-defunt classes this property is available in the abstract schema)
                        if (!_isDefunctOnServer)
                        {
                            try
                            {
                                _isSingleValued = !_iadsProperty!.MultiValued;
                            }
                            catch (COMException e)
                            {
                                throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                            }
                        }
                        else
                        {
                            _isSingleValued = (bool)GetValueFromCache(PropertyManager.IsSingleValued, true)!;
                        }
                        _isSingleValuedInitialized = true;
                    }
                }
                return _isSingleValued;
            }
            set
            {
                CheckIfDisposed();

                if (isBound)
                {
                    // get the distinguished name to construct the directory entry
                    GetSchemaPropertyDirectoryEntry();
                    Debug.Assert(_propertyEntry != null);

                    // set the value on the directory entry
                    _propertyEntry.Properties[PropertyManager.IsSingleValued].Value = value;
                }
                _isSingleValued = value;
            }
        }

        public bool IsIndexed
        {
            get
            {
                CheckIfDisposed();

                return IsSetInSearchFlags(SearchFlags.IsIndexed);
            }
            set
            {
                CheckIfDisposed();

                if (value)
                {
                    SetBitInSearchFlags(SearchFlags.IsIndexed);
                }
                else
                {
                    ResetBitInSearchFlags(SearchFlags.IsIndexed);
                }
            }
        }

        public bool IsIndexedOverContainer
        {
            get
            {
                CheckIfDisposed();

                return IsSetInSearchFlags(SearchFlags.IsIndexedOverContainer);
            }
            set
            {
                CheckIfDisposed();

                if (value)
                {
                    SetBitInSearchFlags(SearchFlags.IsIndexedOverContainer);
                }
                else
                {
                    ResetBitInSearchFlags(SearchFlags.IsIndexedOverContainer);
                }
            }
        }

        public bool IsInAnr
        {
            get
            {
                CheckIfDisposed();

                return IsSetInSearchFlags(SearchFlags.IsInAnr);
            }
            set
            {
                CheckIfDisposed();

                if (value)
                {
                    SetBitInSearchFlags(SearchFlags.IsInAnr);
                }
                else
                {
                    ResetBitInSearchFlags(SearchFlags.IsInAnr);
                }
            }
        }

        public bool IsOnTombstonedObject
        {
            get
            {
                CheckIfDisposed();

                return IsSetInSearchFlags(SearchFlags.IsOnTombstonedObject);
            }
            set
            {
                CheckIfDisposed();

                if (value)
                {
                    SetBitInSearchFlags(SearchFlags.IsOnTombstonedObject);
                }
                else
                {
                    ResetBitInSearchFlags(SearchFlags.IsOnTombstonedObject);
                }
            }
        }

        public bool IsTupleIndexed
        {
            get
            {
                CheckIfDisposed();

                return IsSetInSearchFlags(SearchFlags.IsTupleIndexed);
            }
            set
            {
                CheckIfDisposed();

                if (value)
                {
                    SetBitInSearchFlags(SearchFlags.IsTupleIndexed);
                }
                else
                {
                    ResetBitInSearchFlags(SearchFlags.IsTupleIndexed);
                }
            }
        }

        public bool IsInGlobalCatalog
        {
            get
            {
                CheckIfDisposed();

                if (isBound)
                {
                    if (!_isInGlobalCatalogInitialized)
                    {
                        // get the property from the server
                        object? value = GetValueFromCache(PropertyManager.IsMemberOfPartialAttributeSet, false);
                        _isInGlobalCatalog = (value != null) ? (bool)value : false;
                        _isInGlobalCatalogInitialized = true;
                    }
                }
                return _isInGlobalCatalog;
            }
            set
            {
                CheckIfDisposed();

                if (isBound)
                {
                    // get the distinguished name to construct the directory entry
                    GetSchemaPropertyDirectoryEntry();
                    Debug.Assert(_propertyEntry != null);

                    // set the value on the directory entry
                    _propertyEntry.Properties[PropertyManager.IsMemberOfPartialAttributeSet].Value = value;
                }
                _isInGlobalCatalog = value;
            }
        }

        public Nullable<int> RangeLower
        {
            get
            {
                CheckIfDisposed();

                if (isBound)
                {
                    if (!_rangeLowerInitialized)
                    {
                        // get the property from the server
                        // if the property is not set then we will return null
                        object? value = GetValueFromCache(PropertyManager.RangeLower, false);
                        if (value == null)
                        {
                            _rangeLower = null;
                        }
                        else
                        {
                            _rangeLower = (int)value;
                        }
                        _rangeLowerInitialized = true;
                    }
                }
                return _rangeLower;
            }
            set
            {
                CheckIfDisposed();

                if (isBound)
                {
                    // get the distinguished name to construct the directory entry
                    GetSchemaPropertyDirectoryEntry();
                    Debug.Assert(_propertyEntry != null);

                    // set the value on the directory entry
                    if (value == null)
                    {
                        if (_propertyEntry.Properties.Contains(PropertyManager.RangeLower))
                        {
                            _propertyEntry.Properties[PropertyManager.RangeLower].Clear();
                        }
                    }
                    else
                    {
                        _propertyEntry.Properties[PropertyManager.RangeLower].Value = (int)value.Value;
                    }
                }
                _rangeLower = value;
            }
        }

        public Nullable<int> RangeUpper
        {
            get
            {
                CheckIfDisposed();

                if (isBound)
                {
                    if (!_rangeUpperInitialized)
                    {
                        // get the property from the server
                        // if the property is not set then we will return null
                        object? value = GetValueFromCache(PropertyManager.RangeUpper, false);
                        if (value == null)
                        {
                            _rangeUpper = null;
                        }
                        else
                        {
                            _rangeUpper = (int)value;
                        }
                        _rangeUpperInitialized = true;
                    }
                }
                return _rangeUpper;
            }
            set
            {
                CheckIfDisposed();

                if (isBound)
                {
                    // get the distinguished name to construct the directory entry
                    GetSchemaPropertyDirectoryEntry();
                    Debug.Assert(_propertyEntry != null);

                    // set the value on the directory entry
                    if (value == null)
                    {
                        if (_propertyEntry.Properties.Contains(PropertyManager.RangeUpper))
                        {
                            _propertyEntry.Properties[PropertyManager.RangeUpper].Clear();
                        }
                    }
                    else
                    {
                        _propertyEntry.Properties[PropertyManager.RangeUpper].Value = (int)value.Value;
                    }
                }
                _rangeUpper = value;
            }
        }

        public bool IsDefunct
        {
            get
            {
                CheckIfDisposed();
                // this is initialized for bound properties in the constructor
                return _isDefunct;
            }
            set
            {
                CheckIfDisposed();

                if (isBound)
                {
                    // set the value on the directory entry
                    SetProperty(PropertyManager.IsDefunct, value);
                }
                _isDefunct = value;
            }
        }

        public ActiveDirectorySchemaProperty? Link
        {
            get
            {
                CheckIfDisposed();

                if (isBound)
                {
                    if (!_linkedPropertyInitialized)
                    {
                        object? value = GetValueFromCache(PropertyManager.LinkID, false);
                        int tempLinkId = (value != null) ? (int)value : -1;

                        if (tempLinkId != -1)
                        {
                            int linkIdToSearch = tempLinkId - 2 * (tempLinkId % 2) + 1;

                            try
                            {
                                if (_schemaEntry == null)
                                {
                                    _schemaEntry = DirectoryEntryManager.GetDirectoryEntry(_context, WellKnownDN.SchemaNamingContext);
                                }

                                string filter = "(&(" + PropertyManager.ObjectCategory + "=attributeSchema)" + "(" + PropertyManager.LinkID + "=" + linkIdToSearch + "))";
                                ReadOnlyActiveDirectorySchemaPropertyCollection linkedProperties = ActiveDirectorySchema.GetAllProperties(_context, _schemaEntry, filter);

                                if (linkedProperties.Count != 1)
                                {
                                    throw new ActiveDirectoryObjectNotFoundException(SR.Format(SR.LinkedPropertyNotFound, linkIdToSearch), typeof(ActiveDirectorySchemaProperty), null);
                                }

                                _linkedProperty = linkedProperties[0];
                            }
                            catch (COMException e)
                            {
                                throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                            }
                        }
                        _linkedPropertyInitialized = true;
                    }
                }
                return _linkedProperty;
            }
        }

        public Nullable<int> LinkId
        {
            get
            {
                CheckIfDisposed();

                if (isBound)
                {
                    if (!_linkIdInitialized)
                    {
                        object? value = GetValueFromCache(PropertyManager.LinkID, false);
                        // if the property was not set we will return null
                        if (value == null)
                        {
                            _linkId = null;
                        }
                        else
                        {
                            _linkId = (int)value;
                        }
                        _linkIdInitialized = true;
                    }
                }
                return _linkId;
            }
            set
            {
                CheckIfDisposed();

                if (isBound)
                {
                    // get the distinguished name to construct the directory entry
                    GetSchemaPropertyDirectoryEntry();
                    Debug.Assert(_propertyEntry != null);

                    // set the value on the directory entry
                    if (value == null)
                    {
                        if (_propertyEntry.Properties.Contains(PropertyManager.LinkID))
                        {
                            _propertyEntry.Properties[PropertyManager.LinkID].Clear();
                        }
                    }
                    else
                    {
                        _propertyEntry.Properties[PropertyManager.LinkID].Value = (int)value.Value;
                    }
                }
                _linkId = value;
            }
        }

        public Guid SchemaGuid
        {
            get
            {
                CheckIfDisposed();

                if (isBound)
                {
                    if (_schemaGuidBinaryForm == null)
                    {
                        // get the property from the server
                        _schemaGuidBinaryForm = (byte[])GetValueFromCache(PropertyManager.SchemaIDGuid, true)!;
                    }
                }

                // we cache the byte array and create a new guid each time
                return new Guid(_schemaGuidBinaryForm!);
            }
            set
            {
                CheckIfDisposed();

                if (isBound)
                {
                    // set the value on the directory entry
                    SetProperty(PropertyManager.SchemaIDGuid, (value.Equals(Guid.Empty)) ? null : value.ToByteArray());
                }
                _schemaGuidBinaryForm = (value.Equals(Guid.Empty)) ? null : value.ToByteArray();
            }
        }

        #endregion public properties

        #region private methods

        private void CheckIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        //
        // This method retrieves the value of a property (single valued) from the values
        // that were retrieved from the server. The "mustExist" parameter controls whether or
        // not an exception should be thrown if a value does not exist. If mustExist is true, this
        // will throw an exception is value does not exist.
        //
        private object? GetValueFromCache(string propertyName, bool mustExist)
        {
            object? value = null;

            // retrieve the properties from the server if necessary
            InitializePropertiesFromSchemaContainer();

            Debug.Assert(_propertyValuesFromServer != null);

            ResultPropertyValueCollection? propertyValues = null;
            try
            {
                propertyValues = _propertyValuesFromServer.Properties[propertyName];
                if ((propertyValues == null) || (propertyValues.Count < 1))
                {
                    if (mustExist)
                    {
                        throw new ActiveDirectoryOperationException(SR.Format(SR.PropertyNotFound, propertyName));
                    }
                }
                else
                {
                    value = propertyValues[0];
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
            }

            return value;
        }

        //
        // Just calls the static method GetPropertiesFromSchemaContainer with the correct context
        //
        private void InitializePropertiesFromSchemaContainer()
        {
            if (!_propertiesFromSchemaContainerInitialized)
            {
                if (_schemaEntry == null)
                {
                    _schemaEntry = DirectoryEntryManager.GetDirectoryEntry(_context, WellKnownDN.SchemaNamingContext);
                }

                _propertyValuesFromServer = GetPropertiesFromSchemaContainer(_context, _schemaEntry, (_isDefunctOnServer) ? _commonName! : _ldapDisplayName, _isDefunctOnServer);
                _propertiesFromSchemaContainerInitialized = true;
            }
        }

        //
        // This method retrieves properties for this schema class from the schema container
        // on the server. For non-defunct classes only properties that are not available in the abstract
        // schema are retrieved.  For defunct classes, all the properties are retrieved.
        // The retrieved values are stored in a class variable "propertyValuesFromServer" which is a
        // hashtable indexed on the property name.
        //
        internal static SearchResult GetPropertiesFromSchemaContainer(DirectoryContext context, DirectoryEntry schemaEntry, string name, bool isDefunctOnServer)
        {
            SearchResult? propertyValuesFromServer = null;

            //
            // The properties that are loaded from the schemaContainer for non-defunct classes:
            // DistinguishedName
            // CommonName
            // Syntax - AttributeSyntax, OMSyntax, OMObjectClass
            // Description
            // IsIndexed, IsIndexedOverContainer, IsInAnr, IsOnTombstonedObject, IsTupleIndexed - SearchFlags
            // IsInGlobalCatalog - IsMemberOfPartialAttributeSet
            // LinkId (Link)
            // SchemaGuid - SchemaIdGuid
            // RangeLower
            // RangeUpper

            //
            // For defunct class we also load teh remaining properties
            // LdapDisplayName
            // Oid
            // IsSingleValued
            //

            // build the filter
            StringBuilder str = new StringBuilder(15);
            str.Append("(&(");
            str.Append(PropertyManager.ObjectCategory);
            str.Append("=attributeSchema)");
            str.Append('(');
            if (!isDefunctOnServer)
            {
                str.Append(PropertyManager.LdapDisplayName);
            }
            else
            {
                str.Append(PropertyManager.Cn);
            }
            str.Append('=');
            str.Append(Utils.GetEscapedFilterValue(name));
            str.Append(')');
            if (!isDefunctOnServer)
            {
                str.Append("(!(");
            }
            else
            {
                str.Append('(');
            }
            str.Append(PropertyManager.IsDefunct);
            if (!isDefunctOnServer)
            {
                str.Append("=TRUE)))");
            }
            else
            {
                str.Append("=TRUE))");
            }

            string[]? propertiesToLoad = null;
            if (!isDefunctOnServer)
            {
                propertiesToLoad = new string[12];

                propertiesToLoad[0] = PropertyManager.DistinguishedName;
                propertiesToLoad[1] = PropertyManager.Cn;
                propertiesToLoad[2] = PropertyManager.AttributeSyntax;
                propertiesToLoad[3] = PropertyManager.OMSyntax;
                propertiesToLoad[4] = PropertyManager.OMObjectClass;
                propertiesToLoad[5] = PropertyManager.Description;
                propertiesToLoad[6] = PropertyManager.SearchFlags;
                propertiesToLoad[7] = PropertyManager.IsMemberOfPartialAttributeSet;
                propertiesToLoad[8] = PropertyManager.LinkID;
                propertiesToLoad[9] = PropertyManager.SchemaIDGuid;
                propertiesToLoad[10] = PropertyManager.RangeLower;
                propertiesToLoad[11] = PropertyManager.RangeUpper;
            }
            else
            {
                propertiesToLoad = new string[15];

                propertiesToLoad[0] = PropertyManager.DistinguishedName;
                propertiesToLoad[1] = PropertyManager.Cn;
                propertiesToLoad[2] = PropertyManager.AttributeSyntax;
                propertiesToLoad[3] = PropertyManager.OMSyntax;
                propertiesToLoad[4] = PropertyManager.OMObjectClass;
                propertiesToLoad[5] = PropertyManager.Description;
                propertiesToLoad[6] = PropertyManager.SearchFlags;
                propertiesToLoad[7] = PropertyManager.IsMemberOfPartialAttributeSet;
                propertiesToLoad[8] = PropertyManager.LinkID;
                propertiesToLoad[9] = PropertyManager.SchemaIDGuid;
                propertiesToLoad[10] = PropertyManager.AttributeID;
                propertiesToLoad[11] = PropertyManager.IsSingleValued;
                propertiesToLoad[12] = PropertyManager.RangeLower;
                propertiesToLoad[13] = PropertyManager.RangeUpper;
                propertiesToLoad[14] = PropertyManager.LdapDisplayName;
            }

            //
            // Get all the values (don't need to use range retrieval as there are no multivalued attributes)
            //
            ADSearcher searcher = new ADSearcher(schemaEntry, str.ToString(), propertiesToLoad, SearchScope.OneLevel, false /* paged search */, false /* cache results */);

            try
            {
                propertyValuesFromServer = searcher.FindOne();
            }
            catch (COMException e)
            {
                if (e.ErrorCode == unchecked((int)0x80072030))
                {
                    // object is not found since we cannot even find the container in which to search
                    throw new ActiveDirectoryObjectNotFoundException(SR.DSNotFound, typeof(ActiveDirectorySchemaProperty), name);
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }

            if (propertyValuesFromServer == null)
            {
                throw new ActiveDirectoryObjectNotFoundException(SR.DSNotFound, typeof(ActiveDirectorySchemaProperty), name);
            }

            return propertyValuesFromServer;
        }

        internal DirectoryEntry GetSchemaPropertyDirectoryEntry()
        {
            if (_propertyEntry == null)
            {
                InitializePropertiesFromSchemaContainer();
                _propertyEntry = DirectoryEntryManager.GetDirectoryEntry(_context, (string)GetValueFromCache(PropertyManager.DistinguishedName, true)!);
            }

            return _propertyEntry;
        }

        ///
        /// <summary>
        /// Initializes the search flags attribute value i.e. fetches it from
        /// the directory, if this object is bound.
        /// </summary>
        ///
        private void InitializeSearchFlags()
        {
            if (isBound)
            {
                if (!_searchFlagsInitialized)
                {
                    object? value = GetValueFromCache(PropertyManager.SearchFlags, false);

                    if (value != null)
                    {
                        _searchFlags = (SearchFlags)((int)value);
                    }
                    _searchFlagsInitialized = true;
                }
            }
        }

        private bool IsSetInSearchFlags(SearchFlags searchFlagBit)
        {
            InitializeSearchFlags();
            return (((int)_searchFlags & (int)searchFlagBit) != 0);
        }

        private void SetBitInSearchFlags(SearchFlags searchFlagBit)
        {
            InitializeSearchFlags();
            _searchFlags = (SearchFlags)((int)_searchFlags | (int)searchFlagBit);

            if (isBound)
            {
                // get the distinguished name to construct the directory entry
                GetSchemaPropertyDirectoryEntry();
                Debug.Assert(_propertyEntry != null);

                // set the value on the directory entry
                _propertyEntry.Properties[PropertyManager.SearchFlags].Value = (int)_searchFlags;
            }
        }

        private void ResetBitInSearchFlags(SearchFlags searchFlagBit)
        {
            InitializeSearchFlags();
            _searchFlags = (SearchFlags)((int)_searchFlags & ~((int)searchFlagBit));

            if (isBound)
            {
                // get the distinguished name to construct the directory entry
                GetSchemaPropertyDirectoryEntry();
                Debug.Assert(_propertyEntry != null);

                // set the value on the directory entry
                _propertyEntry.Properties[PropertyManager.SearchFlags].Value = (int)_searchFlags;
            }
        }

        private void SetProperty(string propertyName, object? value)
        {
            // get the distinguished name to construct the directory entry
            GetSchemaPropertyDirectoryEntry();
            Debug.Assert(_propertyEntry != null);

            if (value == null)
            {
                if (_propertyEntry.Properties.Contains(propertyName))
                {
                    _propertyEntry.Properties[propertyName].Clear();
                }
            }
            else
            {
                _propertyEntry.Properties[propertyName].Value = value;
            }
        }

        private ActiveDirectorySyntax MapSyntax(string syntaxId, int oMID, OMObjectClass? oMObjectClass)
        {
            for (int i = 0; i < s_syntaxes.Length; i++)
            {
                if (s_syntaxes[i].Equals(new Syntax(syntaxId, oMID, oMObjectClass)))
                {
                    return (ActiveDirectorySyntax)i;
                }
            }
            throw new ActiveDirectoryOperationException(SR.Format(SR.UnknownSyntax, _ldapDisplayName));
        }

        private void SetSyntax(ActiveDirectorySyntax syntax)
        {
            if ((((int)syntax) < 0) || (((int)syntax) > (s_syntaxes.Length - 1)))
            {
                throw new InvalidEnumArgumentException(nameof(syntax), (int)syntax, typeof(ActiveDirectorySyntax));
            }

            // get the distinguished name to construct the directory entry
            GetSchemaPropertyDirectoryEntry();
            Debug.Assert(_propertyEntry != null);

            _propertyEntry.Properties[PropertyManager.AttributeSyntax].Value = s_syntaxes[(int)syntax].attributeSyntax;
            _propertyEntry.Properties[PropertyManager.OMSyntax].Value = s_syntaxes[(int)syntax].oMSyntax;
            OMObjectClass? oMObjectClass = s_syntaxes[(int)syntax].oMObjectClass;
            if (oMObjectClass != null)
            {
                _propertyEntry.Properties[PropertyManager.OMObjectClass].Value = oMObjectClass.Data;
            }
        }
        #endregion private methods
    }
}
