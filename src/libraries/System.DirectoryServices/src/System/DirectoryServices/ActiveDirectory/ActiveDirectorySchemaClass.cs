// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.ComponentModel;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ActiveDirectorySchemaClass : IDisposable
    {
        // private variables
        private DirectoryEntry _classEntry = null;
        private DirectoryEntry _schemaEntry = null;
        private DirectoryEntry _abstractClassEntry = null;
        private NativeComInterfaces.IAdsClass _iadsClass = null;
        private DirectoryContext _context = null;
        internal bool isBound = false;
        private bool _disposed = false;
        private ActiveDirectorySchema _schema = null;
        private bool _propertiesFromSchemaContainerInitialized = false;
        private bool _isDefunctOnServer = false;
        private Hashtable _propertyValuesFromServer = null;

        // private variables for all the properties of this class
        private string _ldapDisplayName = null;
        private string _commonName = null;
        private string _oid = null;
        private string _description = null;
        private bool _descriptionInitialized = false;
        private bool _isDefunct = false;
        private ActiveDirectorySchemaClassCollection _possibleSuperiors = null;
        private ActiveDirectorySchemaClassCollection _auxiliaryClasses = null;
        private ReadOnlyActiveDirectorySchemaClassCollection _possibleInferiors = null;
        private ActiveDirectorySchemaPropertyCollection _mandatoryProperties = null;
        private ActiveDirectorySchemaPropertyCollection _optionalProperties = null;
        private ActiveDirectorySchemaClass _subClassOf = null;
        private SchemaClassType _type = SchemaClassType.Structural;
        private bool _typeInitialized = false;
        private byte[] _schemaGuidBinaryForm = null;
        private string _defaultSDSddlForm = null;
        private bool _defaultSDSddlFormInitialized = false;

        #region constructors
        public ActiveDirectorySchemaClass(DirectoryContext context, string ldapDisplayName)
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
            // the common name will default to the ldap display name
            _commonName = ldapDisplayName;

            // set the bind flag
            this.isBound = false;
        }

        // Internal constructor
        internal ActiveDirectorySchemaClass(DirectoryContext context, string ldapDisplayName, DirectoryEntry classEntry, DirectoryEntry schemaEntry)
        {
            _context = context;
            _ldapDisplayName = ldapDisplayName;
            _classEntry = classEntry;
            _schemaEntry = schemaEntry;

            // this constructor is only called for non-defunct classes
            _isDefunctOnServer = false;
            _isDefunct = _isDefunctOnServer;

            // initialize the directory entry for the abstract schema class
            try
            {
                _abstractClassEntry = DirectoryEntryManager.GetDirectoryEntryInternal(context, "LDAP://" + context.GetServerName() + "/schema/" + ldapDisplayName);
                _iadsClass = (NativeComInterfaces.IAdsClass)_abstractClassEntry.NativeObject;
            }
            catch (COMException e)
            {
                if (e.ErrorCode == unchecked((int)0x80005000))
                {
                    throw new ActiveDirectoryObjectNotFoundException(SR.DSNotFound, typeof(ActiveDirectorySchemaClass), ldapDisplayName);
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }
            catch (InvalidCastException)
            {
                // this means that we found an object but it is not a schema class
                throw new ActiveDirectoryObjectNotFoundException(SR.DSNotFound, typeof(ActiveDirectorySchemaClass), ldapDisplayName);
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                // this is the case where the context is a config set and we could not find an ADAM instance in that config set
                throw new ActiveDirectoryOperationException(SR.Format(SR.ADAMInstanceNotFoundInConfigSet , context.Name));
            }

            // set the bind flag
            this.isBound = true;
        }

        internal ActiveDirectorySchemaClass(DirectoryContext context, string commonName, Hashtable propertyValuesFromServer, DirectoryEntry schemaEntry)
        {
            _context = context;
            _schemaEntry = schemaEntry;

            // all relevant properties have already been retrieved from the server
            _propertyValuesFromServer = propertyValuesFromServer;
            Debug.Assert(_propertyValuesFromServer != null);
            _propertiesFromSchemaContainerInitialized = true;
            _classEntry = GetSchemaClassDirectoryEntry();

            // names
            _commonName = commonName;
            _ldapDisplayName = (string)GetValueFromCache(PropertyManager.LdapDisplayName, true);

            // this constructor is only called for defunct classes
            _isDefunctOnServer = true;
            _isDefunct = _isDefunctOnServer;

            // set the bind flag
            this.isBound = true;
        }

        internal ActiveDirectorySchemaClass(DirectoryContext context, string commonName, string ldapDisplayName, DirectoryEntry classEntry, DirectoryEntry schemaEntry)
        {
            _context = context;
            _schemaEntry = schemaEntry;
            _classEntry = classEntry;

            // names
            _commonName = commonName;
            _ldapDisplayName = ldapDisplayName;

            // this constructor is only called for defunct classes
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
                    // dispose class entry
                    if (_classEntry != null)
                    {
                        _classEntry.Dispose();
                        _classEntry = null;
                    }
                    // dispose abstract class entry
                    if (_abstractClassEntry != null)
                    {
                        _abstractClassEntry.Dispose();
                        _abstractClassEntry = null;
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
        public static ActiveDirectorySchemaClass FindByName(DirectoryContext context, string ldapDisplayName)
        {
            ActiveDirectorySchemaClass schemaClass = null;

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
                if (!(context.isRootDomain() || context.isServer() || context.isADAMConfigSet()))
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

            // create a schema class 
            schemaClass = new ActiveDirectorySchemaClass(context, ldapDisplayName, (DirectoryEntry)null, null);

            return schemaClass;
        }

        public ReadOnlyActiveDirectorySchemaPropertyCollection GetAllProperties()
        {
            CheckIfDisposed();

            ArrayList properties = new ArrayList();

            // get the mandatory properties 
            properties.AddRange(MandatoryProperties);
            // get the optional properties
            properties.AddRange(OptionalProperties);

            return new ReadOnlyActiveDirectorySchemaPropertyCollection(properties);
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
                    _classEntry = _schemaEntry.Children.Add(rdn, "classSchema");
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                }
                catch (ActiveDirectoryObjectNotFoundException)
                {
                    // this is the case where the context is a config set and we could not find an ADAM instance in that config set
                    throw new ActiveDirectoryOperationException(SR.Format(SR.ADAMInstanceNotFoundInConfigSet , _context.Name));
                }

                // set the ldap display name property
                SetProperty(PropertyManager.LdapDisplayName, _ldapDisplayName);

                // set the oid value
                SetProperty(PropertyManager.GovernsID, _oid);

                // set the description
                SetProperty(PropertyManager.Description, _description);

                // set the possibleSuperiors property
                if (_possibleSuperiors != null)
                {
                    _classEntry.Properties[PropertyManager.PossibleSuperiors].AddRange(_possibleSuperiors.GetMultiValuedProperty());
                }

                // set the mandatoryProperties property
                if (_mandatoryProperties != null)
                {
                    _classEntry.Properties[PropertyManager.MustContain].AddRange(_mandatoryProperties.GetMultiValuedProperty());
                }

                // set the optionalProperties property
                if (_optionalProperties != null)
                {
                    _classEntry.Properties[PropertyManager.MayContain].AddRange(_optionalProperties.GetMultiValuedProperty());
                }

                // set the subClassOf property
                if (_subClassOf != null)
                {
                    SetProperty(PropertyManager.SubClassOf, _subClassOf.Name);
                }
                else
                {
                    // if no super class is specified, set it to "top"
                    SetProperty(PropertyManager.SubClassOf, "top");
                }

                // set the objectClassCategory property
                SetProperty(PropertyManager.ObjectClassCategory, _type);

                // set the schemaIDGuid property
                if (_schemaGuidBinaryForm != null)
                {
                    SetProperty(PropertyManager.SchemaIDGuid, _schemaGuidBinaryForm);
                }

                // set the default security descriptor
                if (_defaultSDSddlForm != null)
                {
                    SetProperty(PropertyManager.DefaultSecurityDescriptor, _defaultSDSddlForm);
                }
            }

            try
            {
                // commit the classEntry to server
                _classEntry.CommitChanges();

                // Refresh the schema cache on the schema role owner
                if (_schema == null)
                {
                    ActiveDirectorySchema schemaObject = ActiveDirectorySchema.GetSchema(_context);
                    bool alreadyUsingSchemaRoleOwnerContext = false;
                    DirectoryServer schemaRoleOwner = null;

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
            _description = null;
            _descriptionInitialized = false;
            _possibleSuperiors = null;
            _auxiliaryClasses = null;
            _possibleInferiors = null;
            _mandatoryProperties = null;
            _optionalProperties = null;
            _subClassOf = null;
            _typeInitialized = false;
            _schemaGuidBinaryForm = null;
            _defaultSDSddlForm = null;
            _defaultSDSddlFormInitialized = false;
            _propertiesFromSchemaContainerInitialized = false;

            // set bind flag
            isBound = true;
        }

        public override string ToString()
        {
            return Name;
        }

        public DirectoryEntry GetDirectoryEntry()
        {
            CheckIfDisposed();

            if (!isBound)
            {
                throw new InvalidOperationException(SR.CannotGetObject);
            }

            GetSchemaClassDirectoryEntry();
            Debug.Assert(_classEntry != null);

            return DirectoryEntryManager.GetDirectoryEntryInternal(_context, _classEntry.Path);
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

        public string CommonName
        {
            get
            {
                CheckIfDisposed();

                if (isBound)
                {
                    if (_commonName == null)
                    {
                        // get the property from the server 
                        _commonName = (string)GetValueFromCache(PropertyManager.Cn, true);
                    }
                }
                return _commonName;
            }
            set
            {
                CheckIfDisposed();

                if (isBound)
                {
                    // set the value on the directory entry
                    SetProperty(PropertyManager.Cn, value);
                }
                _commonName = value;
            }
        }

        public string Oid
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
                                _oid = _iadsClass.OID;
                            }
                            catch (COMException e)
                            {
                                throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                            }
                        }
                        else
                        {
                            _oid = (string)GetValueFromCache(PropertyManager.GovernsID, true);
                        }
                    }
                }
                return _oid;
            }
            set
            {
                CheckIfDisposed();

                if (isBound)
                {
                    // set the value on the directory entry
                    SetProperty(PropertyManager.GovernsID, value);
                }
                _oid = value;
            }
        }

        public string Description
        {
            get
            {
                CheckIfDisposed();

                if (isBound)
                {
                    if (!_descriptionInitialized)
                    {
                        // get the property from the server 
                        _description = (string)GetValueFromCache(PropertyManager.Description, false);
                        _descriptionInitialized = true;
                    }
                }
                return _description;
            }
            set
            {
                CheckIfDisposed();

                if (isBound)
                {
                    // set the value on the directory entry
                    SetProperty(PropertyManager.Description, value);
                }
                _description = value;
            }
        }

        public bool IsDefunct
        {
            get
            {
                CheckIfDisposed();
                // this is initialized for bound classes in the constructor
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

        public ActiveDirectorySchemaClassCollection PossibleSuperiors
        {
            get
            {
                CheckIfDisposed();

                if (_possibleSuperiors == null)
                {
                    if (isBound)
                    {
                        // get the property from the abstract schema/ schema container
                        // (for non-defunt classes this property is available in the abstract schema)
                        if (!_isDefunctOnServer)
                        {
                            ArrayList possibleSuperiorsList = new ArrayList();
                            bool listEmpty = false;

                            //
                            // IADsClass.PossibleSuperiors can return either a collection or a string
                            // (if there is only one value)
                            //
                            object value = null;
                            try
                            {
                                value = _iadsClass.PossibleSuperiors;
                            }
                            catch (COMException e)
                            {
                                if (e.ErrorCode == unchecked((int)0x8000500D))
                                {
                                    listEmpty = true;
                                }
                                else
                                {
                                    throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                                }
                            }

                            if (!listEmpty)
                            {
                                if (value is ICollection)
                                {
                                    possibleSuperiorsList.AddRange((ICollection)value);
                                }
                                else
                                {
                                    // single value
                                    possibleSuperiorsList.Add((string)value);
                                }

                                _possibleSuperiors = new ActiveDirectorySchemaClassCollection(_context, this, true /* isBound */, PropertyManager.PossibleSuperiors, possibleSuperiorsList, true /* onlyNames */);
                            }
                            else
                            {
                                // there are no superiors, return an emtpy collection
                                _possibleSuperiors = new ActiveDirectorySchemaClassCollection(_context, this, true /* is Bound */, PropertyManager.PossibleSuperiors, new ArrayList());
                            }
                        }
                        else
                        {
                            ArrayList possibleSuperiorsList = new ArrayList();
                            possibleSuperiorsList.AddRange(GetValuesFromCache(PropertyManager.PossibleSuperiors));
                            possibleSuperiorsList.AddRange(GetValuesFromCache(PropertyManager.SystemPossibleSuperiors));

                            _possibleSuperiors = new ActiveDirectorySchemaClassCollection(_context, this, true /* isBound */, PropertyManager.PossibleSuperiors, GetClasses(possibleSuperiorsList));
                        }
                    }
                    else
                    {
                        _possibleSuperiors = new ActiveDirectorySchemaClassCollection(_context, this, false /* is Bound */, PropertyManager.PossibleSuperiors, new ArrayList());
                    }
                }

                return _possibleSuperiors;
            }
        }

        public ReadOnlyActiveDirectorySchemaClassCollection PossibleInferiors
        {
            get
            {
                CheckIfDisposed();

                if (_possibleInferiors == null)
                {
                    if (isBound)
                    {
                        // get the value from the server
                        _possibleInferiors = new ReadOnlyActiveDirectorySchemaClassCollection(GetClasses(GetValuesFromCache(PropertyManager.PossibleInferiors)));
                    }
                    else
                    {
                        _possibleInferiors = new ReadOnlyActiveDirectorySchemaClassCollection(new ArrayList());
                    }
                }
                return _possibleInferiors;
            }
        }

        public ActiveDirectorySchemaPropertyCollection MandatoryProperties
        {
            get
            {
                CheckIfDisposed();

                if (_mandatoryProperties == null)
                {
                    if (isBound)
                    {
                        // get the property from the abstract schema/ schema container
                        // (for non-defunt classes this property is available in the abstract schema)
                        if (!_isDefunctOnServer)
                        {
                            ArrayList mandatoryPropertiesList = new ArrayList();
                            bool listEmpty = false;

                            //
                            // IADsClass.MandatoryProperties can return either a collection or a string
                            // (if there is only one value)
                            //
                            object value = null;
                            try
                            {
                                value = _iadsClass.MandatoryProperties;
                            }
                            catch (COMException e)
                            {
                                if (e.ErrorCode == unchecked((int)0x8000500D))
                                {
                                    listEmpty = true;
                                }
                                else
                                {
                                    throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                                }
                            }

                            if (!listEmpty)
                            {
                                if (value is ICollection)
                                {
                                    mandatoryPropertiesList.AddRange((ICollection)value);
                                }
                                else
                                {
                                    // single value
                                    mandatoryPropertiesList.Add((string)value);
                                }

                                _mandatoryProperties = new ActiveDirectorySchemaPropertyCollection(_context, this, true /* isBound */, PropertyManager.MustContain, mandatoryPropertiesList, true /* onlyNames */);
                            }
                            else
                            {
                                // there are no mandatory properties, return an emtpy collection
                                _mandatoryProperties = new ActiveDirectorySchemaPropertyCollection(_context, this, true /* isBound */, PropertyManager.MustContain, new ArrayList());
                            }
                        }
                        else
                        {
                            string[] propertyNames = new string[2];
                            propertyNames[0] = PropertyManager.SystemMustContain;
                            propertyNames[1] = PropertyManager.MustContain;

                            _mandatoryProperties = new ActiveDirectorySchemaPropertyCollection(_context, this, true /* isBound */, PropertyManager.MustContain, GetProperties(GetPropertyValuesRecursively(propertyNames)));
                        }
                    }
                    else
                    {
                        _mandatoryProperties = new ActiveDirectorySchemaPropertyCollection(_context, this, false /* isBound */, PropertyManager.MustContain, new ArrayList());
                    }
                }
                return _mandatoryProperties;
            }
        }

        public ActiveDirectorySchemaPropertyCollection OptionalProperties
        {
            get
            {
                CheckIfDisposed();

                if (_optionalProperties == null)
                {
                    if (isBound)
                    {
                        // get the property from the abstract schema/ schema container
                        // (for non-defunt classes this property is available in the abstract schema)
                        if (!_isDefunctOnServer)
                        {
                            ArrayList optionalPropertiesList = new ArrayList();
                            bool listEmpty = false;

                            //
                            // IADsClass.OptionalProperties can return either a collection or a string
                            // (if there is only one value)
                            //
                            object value = null;
                            try
                            {
                                value = _iadsClass.OptionalProperties;
                            }
                            catch (COMException e)
                            {
                                if (e.ErrorCode == unchecked((int)0x8000500D))
                                {
                                    listEmpty = true;
                                }
                                else
                                {
                                    throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                                }
                            }

                            if (!listEmpty)
                            {
                                if (value is ICollection)
                                {
                                    optionalPropertiesList.AddRange((ICollection)value);
                                }
                                else
                                {
                                    // single value
                                    optionalPropertiesList.Add((string)value);
                                }

                                _optionalProperties = new ActiveDirectorySchemaPropertyCollection(_context, this, true /* isBound */, PropertyManager.MayContain, optionalPropertiesList, true /* onlyNames */);
                            }
                            else
                            {
                                // there are no optional properties, return an emtpy collection
                                _optionalProperties = new ActiveDirectorySchemaPropertyCollection(_context, this, true /* isBound */, PropertyManager.MayContain, new ArrayList());
                            }
                        }
                        else
                        {
                            string[] propertyNames = new string[2];
                            propertyNames[0] = PropertyManager.SystemMayContain;
                            propertyNames[1] = PropertyManager.MayContain;

                            ArrayList optionalPropertyList = new ArrayList();
                            foreach (string propertyName in GetPropertyValuesRecursively(propertyNames))
                            {
                                if (!MandatoryProperties.Contains(propertyName))
                                {
                                    optionalPropertyList.Add(propertyName);
                                }
                            }

                            _optionalProperties = new ActiveDirectorySchemaPropertyCollection(_context, this, true /* isBound */, PropertyManager.MayContain, GetProperties(optionalPropertyList));
                        }
                    }
                    else
                    {
                        _optionalProperties = new ActiveDirectorySchemaPropertyCollection(_context, this, false /* isBound */, PropertyManager.MayContain, new ArrayList());
                    }
                }
                return _optionalProperties;
            }
        }

        public ActiveDirectorySchemaClassCollection AuxiliaryClasses
        {
            get
            {
                CheckIfDisposed();

                if (_auxiliaryClasses == null)
                {
                    if (isBound)
                    {
                        // get the property from the abstract schema/ schema container
                        // (for non-defunt classes this property is available in the abstract schema)
                        if (!_isDefunctOnServer)
                        {
                            ArrayList auxiliaryClassesList = new ArrayList();
                            bool listEmpty = false;

                            //
                            // IADsClass.AuxDerivedFrom can return either a collection or a string
                            // (if there is only one value)
                            //
                            object value = null;
                            try
                            {
                                value = _iadsClass.AuxDerivedFrom;
                            }
                            catch (COMException e)
                            {
                                if (e.ErrorCode == unchecked((int)0x8000500D))
                                {
                                    listEmpty = true;
                                }
                                else
                                {
                                    throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                                }
                            }

                            if (!listEmpty)
                            {
                                if (value is ICollection)
                                {
                                    auxiliaryClassesList.AddRange((ICollection)value);
                                }
                                else
                                {
                                    // single value
                                    auxiliaryClassesList.Add((string)value);
                                }

                                _auxiliaryClasses = new ActiveDirectorySchemaClassCollection(_context, this, true /* isBound */, PropertyManager.AuxiliaryClass, auxiliaryClassesList, true /* onlyNames */);
                            }
                            else
                            {
                                // there are no auxiliary classes, return an emtpy collection
                                _auxiliaryClasses = new ActiveDirectorySchemaClassCollection(_context, this, true /* is Bound */, PropertyManager.AuxiliaryClass, new ArrayList());
                            }
                        }
                        else
                        {
                            string[] propertyNames = new string[2];
                            propertyNames[0] = PropertyManager.AuxiliaryClass;
                            propertyNames[1] = PropertyManager.SystemAuxiliaryClass;

                            _auxiliaryClasses = new ActiveDirectorySchemaClassCollection(_context, this, true /* isBound */, PropertyManager.AuxiliaryClass, GetClasses(GetPropertyValuesRecursively(propertyNames)));
                        }
                    }
                    else
                    {
                        _auxiliaryClasses = new ActiveDirectorySchemaClassCollection(_context, this, false /* isBound */, PropertyManager.AuxiliaryClass, new ArrayList());
                    }
                }
                return _auxiliaryClasses;
            }
        }

        public ActiveDirectorySchemaClass SubClassOf
        {
            get
            {
                CheckIfDisposed();

                if (isBound)
                {
                    if (_subClassOf == null)
                    {
                        // get the property from the server 
                        _subClassOf = new ActiveDirectorySchemaClass(_context, (string)GetValueFromCache(PropertyManager.SubClassOf, true), (DirectoryEntry)null, _schemaEntry);
                    }
                }
                return _subClassOf;
            }
            set
            {
                CheckIfDisposed();

                if (isBound)
                {
                    // set the value on the directory entry
                    SetProperty(PropertyManager.SubClassOf, value);
                }
                _subClassOf = value;
            }
        }

        public SchemaClassType Type
        {
            get
            {
                CheckIfDisposed();

                if (isBound)
                {
                    if (!_typeInitialized)
                    {
                        // get the property from the server 
                        _type = (SchemaClassType)((int)GetValueFromCache(PropertyManager.ObjectClassCategory, true));
                        _typeInitialized = true;
                    }
                }
                return _type;
            }
            set
            {
                CheckIfDisposed();

                // validate the value that is being set
                if (value < SchemaClassType.Type88 || value > SchemaClassType.Auxiliary)
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(SchemaClassType));
                }

                if (isBound)
                {
                    // set the value on the directory entry
                    SetProperty(PropertyManager.ObjectClassCategory, value);
                }
                _type = value;
            }
        }

        public Guid SchemaGuid
        {
            get
            {
                CheckIfDisposed();

                Guid schemaGuid = Guid.Empty;

                if (isBound)
                {
                    if (_schemaGuidBinaryForm == null)
                    {
                        // get the property from the server 
                        _schemaGuidBinaryForm = (byte[])GetValueFromCache(PropertyManager.SchemaIDGuid, true);
                    }
                }

                // we cache the byte array and create a new guid each time
                return new Guid(_schemaGuidBinaryForm);
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

        public ActiveDirectorySecurity DefaultObjectSecurityDescriptor
        {
            get
            {
                CheckIfDisposed();

                ActiveDirectorySecurity defaultObjectSecurityDescriptor = null;

                if (isBound)
                {
                    if (!_defaultSDSddlFormInitialized)
                    {
                        // get the property from the server 
                        _defaultSDSddlForm = (string)GetValueFromCache(PropertyManager.DefaultSecurityDescriptor, false);
                        _defaultSDSddlFormInitialized = true;
                    }
                }

                // we cache the sddl form and create a ActiveDirectorySecurity object each time
                if (_defaultSDSddlForm != null)
                {
                    defaultObjectSecurityDescriptor = new ActiveDirectorySecurity();
                    defaultObjectSecurityDescriptor.SetSecurityDescriptorSddlForm(_defaultSDSddlForm);
                }

                return defaultObjectSecurityDescriptor;
            }
            set
            {
                CheckIfDisposed();

                if (isBound)
                {
                    // set the value on the directory entry
                    SetProperty(PropertyManager.DefaultSecurityDescriptor, (value == null) ? null : value.GetSecurityDescriptorSddlForm(AccessControlSections.All));
                }
                _defaultSDSddlForm = (value == null) ? null : value.GetSecurityDescriptorSddlForm(AccessControlSections.All);
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
        private object GetValueFromCache(string propertyName, bool mustExist)
        {
            object value = null;

            // retrieve the properties from the server if necessary
            InitializePropertiesFromSchemaContainer();

            Debug.Assert(_propertyValuesFromServer != null);
            ArrayList values = (ArrayList)_propertyValuesFromServer[propertyName.ToLower(CultureInfo.InvariantCulture)];

            Debug.Assert(values != null);
            if (values.Count < 1 && mustExist)
            {
                throw new ActiveDirectoryOperationException(SR.Format(SR.PropertyNotFound , propertyName));
            }
            else if (values.Count > 0)
            {
                value = values[0];
            }

            return value;
        }

        //
        // This method retrieves all the values of a property (single valued) from the values
        // that were retrieved from the server. 
        //
        private ICollection GetValuesFromCache(string propertyName)
        {
            // retrieve the properties from the server if necessary
            InitializePropertiesFromSchemaContainer();

            Debug.Assert(_propertyValuesFromServer != null);
            ArrayList values = (ArrayList)_propertyValuesFromServer[propertyName.ToLower(CultureInfo.InvariantCulture)];

            Debug.Assert(values != null);
            return values;
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

                _propertyValuesFromServer = GetPropertiesFromSchemaContainer(_context, _schemaEntry, (_isDefunctOnServer) ? _commonName : _ldapDisplayName, _isDefunctOnServer);
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
        internal static Hashtable GetPropertiesFromSchemaContainer(DirectoryContext context, DirectoryEntry schemaEntry, string name, bool isDefunctOnServer)
        {
            Hashtable propertyValuesFromServer = null;

            //
            // The properties that are loaded from the schemaContainer for non-defunct classes:
            // DistinguishedName
            // CommonName
            // Description
            // PossibleInferiors
            // SubClassOf
            // Type
            // SchemaGuid
            // DefaultObjectSecurityDescriptor
            // AuxiliaryClasses
            //

            //
            // For defunct class we also load the remaining properties
            // LdapDisplayName
            // Oid
            // PossibleSuperiors
            // MandatoryProperties
            // OptionalProperties
            // AuxiliaryClasses
            //

            // build the filter
            StringBuilder str = new StringBuilder(15);
            str.Append("(&(");
            str.Append(PropertyManager.ObjectCategory);
            str.Append("=classSchema)");
            str.Append("(");
            if (!isDefunctOnServer)
            {
                str.Append(PropertyManager.LdapDisplayName);
            }
            else
            {
                str.Append(PropertyManager.Cn);
            }
            str.Append("=");
            str.Append(Utils.GetEscapedFilterValue(name));
            str.Append(")");
            if (!isDefunctOnServer)
            {
                str.Append("(!(");
            }
            else
            {
                str.Append("(");
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

            //
            // Get all the values using range retrieval
            //
            ArrayList propertyNamesWithRangeRetrieval = new ArrayList();
            ArrayList propertyNamesWithoutRangeRetrieval = new ArrayList();

            propertyNamesWithoutRangeRetrieval.Add(PropertyManager.DistinguishedName);
            propertyNamesWithoutRangeRetrieval.Add(PropertyManager.Cn);
            propertyNamesWithoutRangeRetrieval.Add(PropertyManager.Description);
            propertyNamesWithoutRangeRetrieval.Add(PropertyManager.PossibleInferiors);
            propertyNamesWithoutRangeRetrieval.Add(PropertyManager.SubClassOf);
            propertyNamesWithoutRangeRetrieval.Add(PropertyManager.ObjectClassCategory);
            propertyNamesWithoutRangeRetrieval.Add(PropertyManager.SchemaIDGuid);
            propertyNamesWithoutRangeRetrieval.Add(PropertyManager.DefaultSecurityDescriptor);
            propertyNamesWithRangeRetrieval.Add(PropertyManager.AuxiliaryClass);
            propertyNamesWithRangeRetrieval.Add(PropertyManager.SystemAuxiliaryClass);
            propertyNamesWithRangeRetrieval.Add(PropertyManager.MustContain);
            propertyNamesWithRangeRetrieval.Add(PropertyManager.SystemMustContain);
            propertyNamesWithRangeRetrieval.Add(PropertyManager.MayContain);
            propertyNamesWithRangeRetrieval.Add(PropertyManager.SystemMayContain);

            // if defunct, we need to retrieve all the properties from the server
            if (isDefunctOnServer)
            {
                propertyNamesWithoutRangeRetrieval.Add(PropertyManager.LdapDisplayName);
                propertyNamesWithoutRangeRetrieval.Add(PropertyManager.GovernsID);
                propertyNamesWithRangeRetrieval.Add(PropertyManager.SystemPossibleSuperiors);
                propertyNamesWithRangeRetrieval.Add(PropertyManager.PossibleSuperiors);
            }

            try
            {
                propertyValuesFromServer = Utils.GetValuesWithRangeRetrieval(schemaEntry, str.ToString(), propertyNamesWithRangeRetrieval, propertyNamesWithoutRangeRetrieval, SearchScope.OneLevel);
            }
            catch (COMException e)
            {
                if (e.ErrorCode == unchecked((int)0x80072030))
                {
                    // object is not found since we cannot even find the container in which to search
                    throw new ActiveDirectoryObjectNotFoundException(SR.DSNotFound, typeof(ActiveDirectorySchemaClass), name);
                }
                else
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(context, e);
                }
            }

            return propertyValuesFromServer;
        }

        internal DirectoryEntry GetSchemaClassDirectoryEntry()
        {
            if (_classEntry == null)
            {
                InitializePropertiesFromSchemaContainer();
                _classEntry = DirectoryEntryManager.GetDirectoryEntry(_context, (string)GetValueFromCache(PropertyManager.DistinguishedName, true));
            }

            return _classEntry;
        }

        private void SetProperty(string propertyName, object value)
        {
            // get the distinguished name to construct the directory entry 
            GetSchemaClassDirectoryEntry();
            Debug.Assert(_classEntry != null);

            try
            {
                if ((value == null))
                {
                    if (_classEntry.Properties.Contains(propertyName))
                    {
                        _classEntry.Properties[propertyName].Clear();
                    }
                }
                else
                {
                    _classEntry.Properties[propertyName].Value = value;
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
            }
        }

        //
        // This method searches in the schema container for all non-defunct classes of the 
        // specified name (ldapDisplayName).
        //
        private ArrayList GetClasses(ICollection ldapDisplayNames)
        {
            ArrayList classes = new ArrayList();
            SearchResultCollection resCol = null;

            try
            {
                if (ldapDisplayNames.Count < 1)
                {
                    return classes;
                }

                if (_schemaEntry == null)
                {
                    _schemaEntry = DirectoryEntryManager.GetDirectoryEntry(_context, WellKnownDN.SchemaNamingContext);
                }

                // constructing the filter
                StringBuilder str = new StringBuilder(100);

                if (ldapDisplayNames.Count > 1)
                {
                    str.Append("(|");
                }
                foreach (string ldapDisplayName in ldapDisplayNames)
                {
                    str.Append("(");
                    str.Append(PropertyManager.LdapDisplayName);
                    str.Append("=");
                    str.Append(Utils.GetEscapedFilterValue(ldapDisplayName));
                    str.Append(")");
                }
                if (ldapDisplayNames.Count > 1)
                {
                    str.Append(")");
                }

                string filter = "(&(" + PropertyManager.ObjectCategory + "=classSchema)" + str.ToString() + "(!(" + PropertyManager.IsDefunct + "=TRUE)))";

                string[] propertiesToLoad = new string[1];
                propertiesToLoad[0] = PropertyManager.LdapDisplayName;

                ADSearcher searcher = new ADSearcher(_schemaEntry, filter, propertiesToLoad, SearchScope.OneLevel);
                resCol = searcher.FindAll();

                foreach (SearchResult res in resCol)
                {
                    string ldapDisplayName = (string)PropertyManager.GetSearchResultPropertyValue(res, PropertyManager.LdapDisplayName);
                    DirectoryEntry de = res.GetDirectoryEntry();

                    de.AuthenticationType = Utils.DefaultAuthType;
                    de.Username = _context.UserName;
                    de.Password = _context.Password;

                    ActiveDirectorySchemaClass schemaClass = new ActiveDirectorySchemaClass(_context, ldapDisplayName, de, _schemaEntry);

                    classes.Add(schemaClass);
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
            }
            finally
            {
                if (resCol != null)
                {
                    resCol.Dispose();
                }
            }

            return classes;
        }

        //
        // This method searches in the schema container for all non-defunct properties of the 
        // specified name (ldapDisplayName).
        //
        private ArrayList GetProperties(ICollection ldapDisplayNames)
        {
            ArrayList properties = new ArrayList();
            SearchResultCollection resCol = null;

            try
            {
                if (ldapDisplayNames.Count < 1)
                {
                    return properties;
                }

                if (_schemaEntry == null)
                {
                    _schemaEntry = DirectoryEntryManager.GetDirectoryEntry(_context, WellKnownDN.SchemaNamingContext);
                }

                // constructing the filter
                StringBuilder str = new StringBuilder(100);

                if (ldapDisplayNames.Count > 1)
                {
                    str.Append("(|");
                }
                foreach (string ldapDisplayName in ldapDisplayNames)
                {
                    str.Append("(");
                    str.Append(PropertyManager.LdapDisplayName);
                    str.Append("=");
                    str.Append(Utils.GetEscapedFilterValue(ldapDisplayName));
                    str.Append(")");
                }
                if (ldapDisplayNames.Count > 1)
                {
                    str.Append(")");
                }

                string filter = "(&(" + PropertyManager.ObjectCategory + "=attributeSchema)" + str.ToString() + "(!(" + PropertyManager.IsDefunct + "=TRUE)))";

                string[] propertiesToLoad = new string[1];
                propertiesToLoad[0] = PropertyManager.LdapDisplayName;

                ADSearcher searcher = new ADSearcher(_schemaEntry, filter, propertiesToLoad, SearchScope.OneLevel);
                resCol = searcher.FindAll();

                foreach (SearchResult res in resCol)
                {
                    string ldapDisplayName = (string)PropertyManager.GetSearchResultPropertyValue(res, PropertyManager.LdapDisplayName);
                    DirectoryEntry de = res.GetDirectoryEntry();

                    de.AuthenticationType = Utils.DefaultAuthType;
                    de.Username = _context.UserName;
                    de.Password = _context.Password;

                    ActiveDirectorySchemaProperty schemaProperty = new ActiveDirectorySchemaProperty(_context, ldapDisplayName, de, _schemaEntry);

                    properties.Add(schemaProperty);
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
            }
            finally
            {
                if (resCol != null)
                {
                    resCol.Dispose();
                }
            }

            return properties;
        }

        private ArrayList GetPropertyValuesRecursively(string[] propertyNames)
        {
            ArrayList values = new ArrayList();

            // get the properties of the super class
            try
            {
                if (Utils.Compare(SubClassOf.Name, Name) != 0)
                {
                    foreach (string value in SubClassOf.GetPropertyValuesRecursively(propertyNames))
                    {
                        if (!values.Contains(value))
                        {
                            values.Add(value);
                        }
                    }
                }

                // get the properties of the auxiliary classes
                foreach (string auxSchemaClassName in GetValuesFromCache(PropertyManager.AuxiliaryClass))
                {
                    ActiveDirectorySchemaClass auxSchemaClass = new ActiveDirectorySchemaClass(_context, auxSchemaClassName, (DirectoryEntry)null, null);

                    foreach (string property in auxSchemaClass.GetPropertyValuesRecursively(propertyNames))
                    {
                        if (!values.Contains(property))
                        {
                            values.Add(property);
                        }
                    }
                }
                foreach (string auxSchemaClassName in GetValuesFromCache(PropertyManager.SystemAuxiliaryClass))
                {
                    ActiveDirectorySchemaClass auxSchemaClass = new ActiveDirectorySchemaClass(_context, auxSchemaClassName, (DirectoryEntry)null, null);

                    foreach (string property in auxSchemaClass.GetPropertyValuesRecursively(propertyNames))
                    {
                        if (!values.Contains(property))
                        {
                            values.Add(property);
                        }
                    }
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
            }

            foreach (string propertyName in propertyNames)
            {
                foreach (string value in GetValuesFromCache(propertyName))
                {
                    if (!values.Contains(value))
                    {
                        values.Add(value);
                    }
                }
            }

            return values;
        }

        #endregion private methods
    }
}
