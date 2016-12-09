//------------------------------------------------------------------------------
// <copyright file="ActiveDirectorySchemaProperty.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices.ActiveDirectory {
	using System;
	using System.Text;
	using System.Diagnostics;
	using System.Globalization;
	using System.ComponentModel;
	using System.Runtime.InteropServices;
       using System.Security.Permissions;

	internal enum SearchFlags: int {
		None = 0,
		IsIndexed = 1,
		IsIndexedOverContainer = 2,
		IsInAnr = 4,
		IsOnTombstonedObject = 8,
		IsTupleIndexed = 32
	}

       [DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted=true)]
	public class ActiveDirectorySchemaProperty : IDisposable {

		// private variables
		private DirectoryEntry schemaEntry = null;
		private DirectoryEntry propertyEntry = null;
		private DirectoryEntry abstractPropertyEntry = null;
		private NativeComInterfaces.IAdsProperty iadsProperty = null;
		private DirectoryContext context = null;
		internal bool isBound = false;
		private bool disposed = false;
		private ActiveDirectorySchema schema = null;
		private bool propertiesFromSchemaContainerInitialized = false;
		private bool isDefunctOnServer = false;
		private SearchResult propertyValuesFromServer = null;

		// private variables for caching properties
		private string ldapDisplayName = null;
		private string commonName = null;
		private string oid = null;
		private ActiveDirectorySyntax syntax = (ActiveDirectorySyntax)(-1);
		private bool syntaxInitialized = false;
		private string description = null;
		private bool descriptionInitialized = false;
		private bool isSingleValued = false;
		private bool isSingleValuedInitialized = false;
		private bool isInGlobalCatalog = false;
		private bool isInGlobalCatalogInitialized = false;
		private Nullable<Int32> rangeLower = null;
		private bool rangeLowerInitialized = false;
		private Nullable<Int32> rangeUpper = null;
		private bool rangeUpperInitialized = false;
		private bool isDefunct = false;
		private SearchFlags searchFlags = SearchFlags.None;
		private bool searchFlagsInitialized = false;
		private ActiveDirectorySchemaProperty linkedProperty = null;
		private bool linkedPropertyInitialized = false;
		private Nullable<Int32> linkId = null;
		private bool linkIdInitialized = false;
		private byte[] schemaGuidBinaryForm = null;

		// OMObjectClass values for the syntax
		//0x2B0C0287731C00854A
		private static OMObjectClass dnOMObjectClass = new OMObjectClass(new byte[] {0x2B, 0x0C, 0x02, 0x87, 0x73, 0x1C, 0x00, 0x85, 0x4A});
		//0x2A864886F7140101010C
		private static OMObjectClass dNWithStringOMObjectClass = new OMObjectClass(new byte[] { 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x14, 0x01, 0x01, 0x01, 0x0C });
		//0x2A864886F7140101010B
		private static OMObjectClass dNWithBinaryOMObjectClass = new OMObjectClass(new byte[] { 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x14, 0x01, 0x01, 0x01, 0x0B });
		//0x2A864886F71401010106
		private static OMObjectClass replicaLinkOMObjectClass = new OMObjectClass(new byte[] { 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x14, 0x01, 0x01, 0x01, 0x06 });
		//0x2B0C0287731C00855C
		private static OMObjectClass presentationAddressOMObjectClass = new OMObjectClass(new byte[] { 0x2B, 0x0C, 0x02, 0x87, 0x73, 0x1C, 0x00, 0x85, 0x5C });
		//0x2B0C0287731C00853E
		private static OMObjectClass accessPointDnOMObjectClass = new OMObjectClass(new byte[] { 0x2B, 0x0C, 0x02, 0x87, 0x73, 0x1C, 0x00, 0x85, 0x3E });
		//0x56060102050B1D
		private static OMObjectClass oRNameOMObjectClass = new OMObjectClass(new byte[] { 0x56, 0x06, 0x01, 0x02, 0x05, 0x0B, 0x1D });

		// syntaxes
		private static int SyntaxesCount = 23;
		private static Syntax[] syntaxes = {/* CaseExactString */ new Syntax("2.5.5.3", 27, null),
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
											   /* DN */ new Syntax("2.5.5.1", 127, dnOMObjectClass),
											   /* DNWithBinary */ new Syntax("2.5.5.7", 127, dNWithBinaryOMObjectClass),
											   /* DNWithString */ new Syntax("2.5.5.14", 127, dNWithStringOMObjectClass),
											   /* Enumeration */ new Syntax("2.5.5.9", 10, null),
											   /* IA5String */ new Syntax("2.5.5.5", 22, null),
											   /* PrintableString */ new Syntax("2.5.5.5", 19, null),
											   /* Sid */ new Syntax("2.5.5.17", 4, null),
											   /* AccessPointDN */ new Syntax("2.5.5.14", 127, accessPointDnOMObjectClass),
											   /* ORName */ new Syntax("2.5.5.7", 127, oRNameOMObjectClass),
											   /* PresentationAddress */ new Syntax("2.5.5.13", 127, presentationAddressOMObjectClass),
											   /* ReplicaLink */ new Syntax("2.5.5.10", 127, replicaLinkOMObjectClass)};
		

		#region constructors
		public ActiveDirectorySchemaProperty(DirectoryContext context, string ldapDisplayName) {

                     if (context == null) {
				throw new ArgumentNullException("context");
			}

			if ((context.Name == null) && (!context.isRootDomain())) {
				throw new ArgumentException(Res.GetString(Res.ContextNotAssociatedWithDomain), "context");
			}

			if (context.Name != null) {
				// the target should be a valid forest name or a server
				if (!((context.isRootDomain())||(context.isADAMConfigSet()) ||(context.isServer()))) {
					throw new ArgumentException(Res.GetString(Res.NotADOrADAM), "context");
				}
			}
    
			if (ldapDisplayName == null) {
				throw new ArgumentNullException("ldapDisplayName");
			}

			if (ldapDisplayName.Length == 0) {
				throw new ArgumentException(Res.GetString(Res.EmptyStringParameter), "ldapDisplayName");
			}

			this.context = new DirectoryContext(context);

			// validate the context 
			schemaEntry = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.SchemaNamingContext);
			schemaEntry.Bind(true);
			
			this.ldapDisplayName = ldapDisplayName;
			// common name of the property defaults to the ldap display name
			this.commonName = ldapDisplayName;

			// set the bind flag
			this.isBound = false;
		}

		// internal constructor
		internal ActiveDirectorySchemaProperty(DirectoryContext context, string ldapDisplayName, DirectoryEntry propertyEntry, DirectoryEntry schemaEntry) {
			this.context = context;
			this.ldapDisplayName = ldapDisplayName;

			// common name of the property defaults to the ldap display name
			this.propertyEntry = propertyEntry;
			this.isDefunctOnServer = false;
			this.isDefunct = isDefunctOnServer;

			try {
				// initialize the directory entry for the abstract schema class
				this.abstractPropertyEntry = DirectoryEntryManager.GetDirectoryEntryInternal(context, "LDAP://" + context.GetServerName() + "/schema/" + ldapDisplayName);
				iadsProperty = (NativeComInterfaces.IAdsProperty) abstractPropertyEntry.NativeObject;
			}
			catch (COMException e) {
				if (e.ErrorCode == unchecked((int) 0x80005000)) {
					throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.DSNotFound), typeof(ActiveDirectorySchemaProperty), ldapDisplayName);
				}
				else {
					throw ExceptionHelper.GetExceptionFromCOMException(context, e);
				}
			}
			catch (InvalidCastException) {
				// this means that we found an object but it is not a schema class
				throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.DSNotFound), typeof(ActiveDirectorySchemaProperty), ldapDisplayName);
			}
			catch (ActiveDirectoryObjectNotFoundException) {
				// this is the case where the context is a config set and we could not find an ADAM instance in that config set
				throw new ActiveDirectoryOperationException(Res.GetString(Res.ADAMInstanceNotFoundInConfigSet, context.Name));
			}
			
			// set the bind flag
			this.isBound = true;
		}

		internal ActiveDirectorySchemaProperty(DirectoryContext context, string commonName, SearchResult propertyValuesFromServer, DirectoryEntry schemaEntry) {

			this.context = context;
			this.schemaEntry = schemaEntry;

			// all relevant properties have already been retrieved from the server
			this.propertyValuesFromServer = propertyValuesFromServer;
			Debug.Assert(this.propertyValuesFromServer != null);
			propertiesFromSchemaContainerInitialized = true;
			this.propertyEntry = GetSchemaPropertyDirectoryEntry();

			// names
			this.commonName = commonName;
			this.ldapDisplayName = (string) GetValueFromCache(PropertyManager.LdapDisplayName, true);

			// this constructor is only called for defunct classes
			this.isDefunctOnServer = true;
			this.isDefunct = isDefunctOnServer;

			// set the bind flag
			this.isBound = true;
		}

		internal ActiveDirectorySchemaProperty(DirectoryContext context, string commonName, string ldapDisplayName, DirectoryEntry propertyEntry, DirectoryEntry schemaEntry)  {
			
			this.context = context;
			this.schemaEntry = schemaEntry;
			this.propertyEntry = propertyEntry;

			// names
			this.commonName = commonName;
			this.ldapDisplayName = ldapDisplayName;

			// this constructor is only called for defunct properties
			this.isDefunctOnServer = true;
			this.isDefunct = isDefunctOnServer;

			// set the bind flag
			this.isBound = true;
		}

		#endregion constructors

		#region IDisposable

		public void Dispose() {
			Dispose(true);
		}

		// private Dispose method
		protected virtual void Dispose(bool disposing) {
			if (!this.disposed)	{
				// check if this is an explicit Dispose
				// only then clean up the directory entries
				if (disposing)	{
					// dispose schema entry
					if (schemaEntry != null)	{
						schemaEntry.Dispose();
						schemaEntry = null;
					}
					// dispose property entry
					if (propertyEntry != null)	{
						propertyEntry.Dispose();
						propertyEntry = null;
					}
					// dispose abstract class entry
					if (abstractPropertyEntry != null)  {
						abstractPropertyEntry.Dispose();
						abstractPropertyEntry = null;
					}
					// dispose the schema object
					if (schema != null) {
						schema.Dispose();
					}
				}

				this.disposed = true;
			}
		}
		#endregion IDisposable

		#region public methods
		public static ActiveDirectorySchemaProperty FindByName(DirectoryContext context, string ldapDisplayName) {
			ActiveDirectorySchemaProperty schemaProperty = null;

                     if (context == null) {
				throw new ArgumentNullException("context");
			}

			if ((context.Name == null) && (!context.isRootDomain())) {
				throw new ArgumentException(Res.GetString(Res.ContextNotAssociatedWithDomain), "context");
			}

			if (context.Name != null) {
				// the target should be a valid forest name or a server
				if (!((context.isRootDomain())||(context.isADAMConfigSet()) ||(context.isServer()))) {
					throw new ArgumentException(Res.GetString(Res.NotADOrADAM), "context");
				}
			}
    
			if (ldapDisplayName == null) {
				throw new ArgumentNullException("ldapDisplayName");
			}

			if (ldapDisplayName.Length == 0) {
				throw new ArgumentException(Res.GetString(Res.EmptyStringParameter), "ldapDisplayName");
			}

			//  work with copy of the context
			context = new DirectoryContext(context);
				
			// create a schema property 
			schemaProperty = new ActiveDirectorySchemaProperty(context, ldapDisplayName, (DirectoryEntry) null, null);
			
			return schemaProperty;
		}

		
		public void Save() {

			CheckIfDisposed();

			if (!isBound) {

				try {
					// create a new directory entry for this class
					if (schemaEntry == null) {
						schemaEntry = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.SchemaNamingContext);
					}

					// this will create the class and set the CN value
					string rdn = "CN=" + commonName;
      					rdn = Utils.GetEscapedPath(rdn);
					propertyEntry = schemaEntry.Children.Add(rdn, "attributeSchema");
				}
				catch (COMException e) {
					throw ExceptionHelper.GetExceptionFromCOMException(context, e);
				}
				catch (ActiveDirectoryObjectNotFoundException) {
					// this is the case where the context is a config set and we could not find an ADAM instance in that config set
					throw new ActiveDirectoryOperationException(Res.GetString(Res.ADAMInstanceNotFoundInConfigSet, context.Name));
				}

				// set the ldap display name property
				SetProperty(PropertyManager.LdapDisplayName, ldapDisplayName);

				// set the oid value
				SetProperty(PropertyManager.AttributeID, oid);

				// set the syntax
				if (syntax != (ActiveDirectorySyntax)(-1)) {
					SetSyntax(syntax);
				}

				// set the description
				SetProperty(PropertyManager.Description, description);

				// set the isSingleValued attribute
				propertyEntry.Properties[PropertyManager.IsSingleValued].Value = isSingleValued;

				// set the isGlobalCatalogReplicated attribute
				propertyEntry.Properties[PropertyManager.IsMemberOfPartialAttributeSet].Value = isInGlobalCatalog;

				// set the isDefunct attribute
				propertyEntry.Properties[PropertyManager.IsDefunct].Value = isDefunct;

				// set the range lower attribute
				if (rangeLower != null) {
					propertyEntry.Properties[PropertyManager.RangeLower].Value = (int) rangeLower.Value;
				}

				// set the range upper attribute
				if (rangeUpper != null) {
					propertyEntry.Properties[PropertyManager.RangeUpper].Value = (int) rangeUpper.Value;
				}

				// set the searchFlags attribute
				if (searchFlags != SearchFlags.None) {
					propertyEntry.Properties[PropertyManager.SearchFlags].Value = (int)searchFlags;
				}

				// set the link id
				if (linkId != null) {
					propertyEntry.Properties[PropertyManager.LinkID].Value = (int) linkId.Value;
				}

				// set the schemaIDGuid property
				if (schemaGuidBinaryForm != null) {
					SetProperty(PropertyManager.SchemaIDGuid, schemaGuidBinaryForm);
				}
			}

			try {
				// commit the classEntry to server
				propertyEntry.CommitChanges();

				// Refresh the schema cache on the schema role owner
				if (schema == null) {

					ActiveDirectorySchema schemaObject = ActiveDirectorySchema.GetSchema(context);
					bool alreadyUsingSchemaRoleOwnerContext = false;
					DirectoryServer schemaRoleOwner = null;
					
					try {
						
						//
						// if we are not already talking to the schema role owner, change the context
						//
						schemaRoleOwner = schemaObject.SchemaRoleOwner;
						if (Utils.Compare(schemaRoleOwner.Name, context.GetServerName()) != 0) {
							DirectoryContext schemaRoleOwnerContext = Utils.GetNewDirectoryContext(schemaRoleOwner.Name, DirectoryContextType.DirectoryServer, context);
							schema = ActiveDirectorySchema.GetSchema(schemaRoleOwnerContext);
						}
						else {
							alreadyUsingSchemaRoleOwnerContext = true;
							schema = schemaObject;
						}
					}
					finally {
						if (schemaRoleOwner != null) {
							schemaRoleOwner.Dispose();
						}
						if (!alreadyUsingSchemaRoleOwnerContext) {
							schemaObject.Dispose();
						}
					}
				}
				schema.RefreshSchema();
			}
			catch (COMException e) {
				throw ExceptionHelper.GetExceptionFromCOMException(context, e);
			}

			// now that the changes are committed to the server
			// update the defunct/non-defunct status of the class on the server
			isDefunctOnServer = isDefunct;

			// invalidate all properties
			commonName = null;
			oid = null;
			syntaxInitialized = false;
			descriptionInitialized = false;
			isSingleValuedInitialized = false;
			isInGlobalCatalogInitialized = false;
			rangeLowerInitialized = false;
			rangeUpperInitialized = false;
			searchFlagsInitialized = false;
			linkedPropertyInitialized = false;
			linkIdInitialized = false;
			schemaGuidBinaryForm = null;
			propertiesFromSchemaContainerInitialized = false;
			
			// set bind flag
			isBound = true;
		}

		public override string ToString() {
			return Name;
		}

		public DirectoryEntry GetDirectoryEntry() {
			CheckIfDisposed();
			
			if (!isBound)	{
				throw new InvalidOperationException(Res.GetString(Res.CannotGetObject));
			}
			
			GetSchemaPropertyDirectoryEntry();
			Debug.Assert(propertyEntry != null);
			
			return DirectoryEntryManager.GetDirectoryEntryInternal(context, propertyEntry.Path);
		}
		#endregion public methods

		#region public properties

		public string Name {
			get {
				CheckIfDisposed();
				return ldapDisplayName;
			}
		}

		public string CommonName {
			get {
				CheckIfDisposed();
				
				if (isBound) {
					if (commonName == null) {
						// get the property from the server 
						commonName = (string) GetValueFromCache(PropertyManager.Cn, true);
					}
				}
				return commonName;
			}
			set {
				CheckIfDisposed();

				if (value != null && value.Length == 0)
					throw new ArgumentException(Res.GetString(Res.EmptyStringParameter), "value");
				
				if (isBound) {
					
					// set the value on the directory entry
					SetProperty(PropertyManager.Cn, value);
				}
				commonName = value;
			}
		}

		public string Oid {
			get {
				CheckIfDisposed();
				
				if (isBound) {

					if (oid == null) {
						// get the property from the abstract schema/ schema container
						// (for non-defunt classes this property is available in the abstract schema)
						if (!isDefunctOnServer) {
							try {
								oid = iadsProperty.OID;
							}
							catch (COMException e) {
								throw ExceptionHelper.GetExceptionFromCOMException(context, e);
							}
						}
						else {
							oid = (string) GetValueFromCache(PropertyManager.AttributeID, true);
						}
					}
				}
				return oid;
			}
			set {
				CheckIfDisposed();

				if (value != null && value.Length == 0)
					throw new ArgumentException(Res.GetString(Res.EmptyStringParameter), "value");
				
				if (isBound) {

					// set the value on the directory entry
					SetProperty(PropertyManager.AttributeID, value);
				}
				oid = value;
			}
		}

		public ActiveDirectorySyntax Syntax {
			get {
				CheckIfDisposed();
				
				if (isBound) {

					if (!syntaxInitialized) {

						byte[] omObjectClassBinaryForm = (byte[]) GetValueFromCache(PropertyManager.OMObjectClass, false);
						OMObjectClass omObjectClass = (omObjectClassBinaryForm != null) ? new OMObjectClass(omObjectClassBinaryForm) : null;
					
						syntax = MapSyntax((string) GetValueFromCache(PropertyManager.AttributeSyntax, true), 
							(int) GetValueFromCache(PropertyManager.OMSyntax, true), 
							omObjectClass);
						syntaxInitialized = true;
					}
				}
				return syntax;
			}
			set {
				CheckIfDisposed();
				
                            if (value < ActiveDirectorySyntax.CaseExactString || value > ActiveDirectorySyntax.ReplicaLink) {
				       throw new InvalidEnumArgumentException("value", (int)value, typeof(ActiveDirectorySyntax));
			       }
                
				if (isBound) {

					// set the value on the directory entry
					SetSyntax(value);
				}
				syntax = value;
			}
		}

		public string Description {
			get {
				CheckIfDisposed();
				
				if (isBound) {
					if (!descriptionInitialized) { 
						// get the property from the server 
						description = (string) GetValueFromCache(PropertyManager.Description, false);
						descriptionInitialized = true;
					}
				}
				return description;
			}
			set {
				CheckIfDisposed();

				if (value != null && value.Length == 0)
					throw new ArgumentException(Res.GetString(Res.EmptyStringParameter), "value");
				
				if (isBound) {

					// set the value on the directory entry
					SetProperty(PropertyManager.Description, value);
				}
				description = value;
			}
		}

		public bool IsSingleValued {
			get {
				CheckIfDisposed();
				
				if (isBound) {
					if (!isSingleValuedInitialized) {
						// get the property from the abstract schema/ schema container
						// (for non-defunt classes this property is available in the abstract schema)
						if (!isDefunctOnServer) {
							try {
								isSingleValued = !iadsProperty.MultiValued;
							}
							catch (COMException e) {
								throw ExceptionHelper.GetExceptionFromCOMException(context, e);
							}
						}
						else {
							isSingleValued = (bool) GetValueFromCache(PropertyManager.IsSingleValued, true);
						}
						isSingleValuedInitialized = true;
					}
				}
				return isSingleValued;
			}
			set {
				CheckIfDisposed();

				if (isBound) {
					
					// get the distinguished name to construct the directory entry 
					GetSchemaPropertyDirectoryEntry();
					Debug.Assert(propertyEntry != null);
					
					// set the value on the directory entry
					propertyEntry.Properties[PropertyManager.IsSingleValued].Value = value;
				}
				isSingleValued = value;
			}
		}

		public bool IsIndexed {
			get {
				CheckIfDisposed();
				
				return IsSetInSearchFlags(SearchFlags.IsIndexed);
			}
			set {
				CheckIfDisposed();
				
				if (value) {
					SetBitInSearchFlags(SearchFlags.IsIndexed);
				}
				else {
					ResetBitInSearchFlags(SearchFlags.IsIndexed);
				}
			}
		}

		public bool IsIndexedOverContainer {
			get {
				CheckIfDisposed();
				
				return IsSetInSearchFlags(SearchFlags.IsIndexedOverContainer);
			}
			set {
				CheckIfDisposed();
				
				if (value) {
					SetBitInSearchFlags(SearchFlags.IsIndexedOverContainer);
				}
				else {
					ResetBitInSearchFlags(SearchFlags.IsIndexedOverContainer);
				}
			}
		}

		public bool IsInAnr {
			get {
				CheckIfDisposed();
				
				return IsSetInSearchFlags(SearchFlags.IsInAnr);
			}
			set {
				CheckIfDisposed();
				
				if (value) {
					SetBitInSearchFlags(SearchFlags.IsInAnr);
				}
				else {
					ResetBitInSearchFlags(SearchFlags.IsInAnr);
				}
			}
		}

		public bool IsOnTombstonedObject {
			get {
				CheckIfDisposed();
				
				return IsSetInSearchFlags(SearchFlags.IsOnTombstonedObject);
			}
			set {
				CheckIfDisposed();
				
				if (value) {
					SetBitInSearchFlags(SearchFlags.IsOnTombstonedObject);
				}
				else {
					ResetBitInSearchFlags(SearchFlags.IsOnTombstonedObject);
				}
			}
		}

		public bool IsTupleIndexed {
			get {
				CheckIfDisposed();
				
				return IsSetInSearchFlags(SearchFlags.IsTupleIndexed);
			}
			set {
				CheckIfDisposed();
				
				if (value) {
					SetBitInSearchFlags(SearchFlags.IsTupleIndexed);
				}
				else {
					ResetBitInSearchFlags(SearchFlags.IsTupleIndexed);
				}
			}
		}

		public bool IsInGlobalCatalog {
			get {
				CheckIfDisposed();
				
				if (isBound) {
					if (!isInGlobalCatalogInitialized) {
						// get the property from the server 
						object value = GetValueFromCache(PropertyManager.IsMemberOfPartialAttributeSet, false);
						isInGlobalCatalog = (value != null) ? (bool) value : false;
						isInGlobalCatalogInitialized = true;
					}
				}
				return isInGlobalCatalog;
			}
			set {
				CheckIfDisposed();
				
				if (isBound) {
					
					// get the distinguished name to construct the directory entry 
					GetSchemaPropertyDirectoryEntry();
					Debug.Assert(propertyEntry != null);
					
					// set the value on the directory entry
					propertyEntry.Properties[PropertyManager.IsMemberOfPartialAttributeSet].Value = value;
				}
				isInGlobalCatalog = value;
			}
		}

		public Nullable<Int32> RangeLower {
			get {
				CheckIfDisposed();
				
				if (isBound) {
					if (!rangeLowerInitialized) {

						// get the property from the server 
						// if the property is not set then we will return null
						object value = GetValueFromCache(PropertyManager.RangeLower, false);
						if (value == null) {
							rangeLower = null;
						}
						else {
							rangeLower = (int) value;
						}
						rangeLowerInitialized = true;
					}
				}
				return rangeLower;
			}
			set {
				CheckIfDisposed();

				if (isBound) {
					
					// get the distinguished name to construct the directory entry 
					GetSchemaPropertyDirectoryEntry();
					Debug.Assert(propertyEntry != null);
					
					// set the value on the directory entry
					if (value == null) {
						if (propertyEntry.Properties.Contains(PropertyManager.RangeLower)) {
							propertyEntry.Properties[PropertyManager.RangeLower].Clear();
						}
					}
                    			else {
						propertyEntry.Properties[PropertyManager.RangeLower].Value = (int) value.Value;
                    			}
				}
				rangeLower = value;

			}
		}

		public Nullable<Int32> RangeUpper {
			get {
				CheckIfDisposed();
				
				if (isBound) {
					if (!rangeUpperInitialized) {
						
						// get the property from the server 
						// if the property is not set then we will return null
						object value = GetValueFromCache(PropertyManager.RangeUpper, false);
						if (value == null) {
							rangeUpper = null;
						}
						else {
							rangeUpper = (int) value;
						}
						rangeUpperInitialized = true;
					}
				}
				return rangeUpper;
			}
			set {
				CheckIfDisposed();

				if (isBound) {
					
					// get the distinguished name to construct the directory entry 
					GetSchemaPropertyDirectoryEntry();
					Debug.Assert(propertyEntry != null);
					
					// set the value on the directory entry
					if (value == null) {
						if (propertyEntry.Properties.Contains(PropertyManager.RangeUpper)) {
							propertyEntry.Properties[PropertyManager.RangeUpper].Clear();
						}
					}
                    			else {
						propertyEntry.Properties[PropertyManager.RangeUpper].Value = (int) value.Value;
                    			}
				}
				rangeUpper = value;

			}
		}

		public bool IsDefunct {
			get {
				CheckIfDisposed();
				// this is initialized for bound properties in the constructor
				return isDefunct;
			}
			set {
				CheckIfDisposed();
				
				if (isBound) {
					
					// set the value on the directory entry
					SetProperty(PropertyManager.IsDefunct, value);
				}
				isDefunct = value;
			}
		}

		public ActiveDirectorySchemaProperty Link {
			get {
				CheckIfDisposed();
				
				if (isBound) {
					if (!linkedPropertyInitialized) {

						object value = GetValueFromCache(PropertyManager.LinkID, false);
						int tempLinkId = (value != null) ? (int) value : -1;
						
						if (tempLinkId != -1) {

							int linkIdToSearch = tempLinkId - 2 * (tempLinkId % 2) + 1;

							try {
								if (schemaEntry == null) {
									schemaEntry = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.SchemaNamingContext);
								}

								string filter = "(&(" + PropertyManager.ObjectCategory + "=attributeSchema)" + "(" + PropertyManager.LinkID + "=" + linkIdToSearch + "))";
								ReadOnlyActiveDirectorySchemaPropertyCollection linkedProperties = ActiveDirectorySchema.GetAllProperties(context, schemaEntry, filter);

								if (linkedProperties.Count != 1) {
									throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.LinkedPropertyNotFound, linkIdToSearch), typeof(ActiveDirectorySchemaProperty), null);
								}

								linkedProperty = linkedProperties[0];
							}
							catch (COMException e) {
								throw ExceptionHelper.GetExceptionFromCOMException(context, e);
							}

						}
						linkedPropertyInitialized = true;
					}
				}
				return linkedProperty;
			}
		}

		public Nullable<Int32> LinkId {
			get {
				CheckIfDisposed();
				
				if (isBound) {

					if (!linkIdInitialized) {
						object value = GetValueFromCache(PropertyManager.LinkID, false);
						// if the property was not set we will return null
						if (value == null) {
							linkId = null;
						}
						else {
							linkId = (int) value;
						}
						linkIdInitialized = true;
					}		
				}
				return linkId;
			}
			set {

				CheckIfDisposed();
				
				if (isBound) {
					
					// get the distinguished name to construct the directory entry 
					GetSchemaPropertyDirectoryEntry();
					Debug.Assert(propertyEntry != null);
					
					// set the value on the directory entry
					if (value == null) {
						if (propertyEntry.Properties.Contains(PropertyManager.LinkID)) {
							propertyEntry.Properties[PropertyManager.LinkID].Clear();
						}
					}
                    			else {
						propertyEntry.Properties[PropertyManager.LinkID].Value = (int) value.Value;
                    			}
				}
				linkId = value;
			}
		}

		public Guid SchemaGuid {
			get {
				CheckIfDisposed();

				Guid schemaGuid = Guid.Empty;
				
				if (isBound) {

					if (schemaGuidBinaryForm == null) {
						// get the property from the server 
						schemaGuidBinaryForm = (byte[]) GetValueFromCache(PropertyManager.SchemaIDGuid, true);
					}
				}

				// we cache the byte array and create a new guid each time
				return new Guid(schemaGuidBinaryForm);
			}
			set {
				CheckIfDisposed();

				if (isBound) {
					
					// set the value on the directory entry
					SetProperty(PropertyManager.SchemaIDGuid, (value.Equals(Guid.Empty)) ? null : value.ToByteArray());
				}
				schemaGuidBinaryForm = (value.Equals(Guid.Empty)) ? null : value.ToByteArray();
			}
			
		}

		#endregion public properties

		#region private methods

		private void CheckIfDisposed() {
			if (this.disposed) {
				throw new ObjectDisposedException(GetType().Name);
			}
		}

		//
		// This method retrieves the value of a property (single valued) from the values
		// that were retrieved from the server. The "mustExist" parameter controls whether or
		// not an exception should be thrown if a value does not exist. If mustExist is true, this 
		// will throw an exception is value does not exist.
		//
		private object GetValueFromCache(string propertyName, bool mustExist) {

			object value = null;

			// retrieve the properties from the server if necessary
			InitializePropertiesFromSchemaContainer();

			Debug.Assert(propertyValuesFromServer != null);

			ResultPropertyValueCollection propertyValues = null;
			try {
				propertyValues = propertyValuesFromServer.Properties[propertyName];
				if ((propertyValues == null) || (propertyValues.Count < 1)){
					if (mustExist) {
						throw new ActiveDirectoryOperationException(Res.GetString(Res.PropertyNotFound, propertyName));
					}
				}
				else {
					value = propertyValues[0];
				}
			}
			catch (COMException e) {
				throw ExceptionHelper.GetExceptionFromCOMException(context, e);
			}

			return value;
		}

		//
		// Just calls the static method GetPropertiesFromSchemaContainer with the correct context
		//
		private void InitializePropertiesFromSchemaContainer() {
			if (!propertiesFromSchemaContainerInitialized) {

				if (schemaEntry == null) {
					schemaEntry = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.SchemaNamingContext);
				}
				
				propertyValuesFromServer = GetPropertiesFromSchemaContainer(context, schemaEntry, (isDefunctOnServer) ? commonName : ldapDisplayName, isDefunctOnServer);
				propertiesFromSchemaContainerInitialized = true;
			}
		}

		//
		// This method retrieves properties for this schema class from the schema container
		// on the server. For non-defunct classes only properties that are not available in the abstract 
		// schema are retrieved.  For defunct classes, all the properties are retrieved.
		// The retrieved values are stored in a class variable "propertyValuesFromServer" which is a 
		// hashtable indexed on the property name.
		//
		internal static SearchResult GetPropertiesFromSchemaContainer(DirectoryContext context, DirectoryEntry schemaEntry, string name, bool isDefunctOnServer) {
			SearchResult propertyValuesFromServer = null;
			

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
			str.Append("(");
			if (!isDefunctOnServer) {
				str.Append(PropertyManager.LdapDisplayName);
			}
			else {
				str.Append(PropertyManager.Cn);
			}
			str.Append("=");
			str.Append(Utils.GetEscapedFilterValue(name));
			str.Append(")");
			if (!isDefunctOnServer) {
				str.Append("(!(");
			}
			else {
				str.Append("(");
			}
			str.Append(PropertyManager.IsDefunct);
			if (!isDefunctOnServer) {
				str.Append("=TRUE)))");
			}
			else {
				str.Append("=TRUE))");
			}

			string[] propertiesToLoad = null;
			if (!isDefunctOnServer) {
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
			else {
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

			try {
				propertyValuesFromServer = searcher.FindOne();
			}
			catch (COMException e) {
				if (e.ErrorCode == unchecked((int)  0x80072030)) {
					// object is not found since we cannot even find the container in which to search
					throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.DSNotFound), typeof(ActiveDirectorySchemaProperty), name);
				}
				else {
					throw ExceptionHelper.GetExceptionFromCOMException(context, e);
				}
			}

			if (propertyValuesFromServer == null) {
				throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.DSNotFound), typeof(ActiveDirectorySchemaProperty), name);
			}

			return propertyValuesFromServer;
		}

		internal DirectoryEntry GetSchemaPropertyDirectoryEntry() {

			if (propertyEntry == null) {
			
				InitializePropertiesFromSchemaContainer();
				propertyEntry = DirectoryEntryManager.GetDirectoryEntry(context, (string) GetValueFromCache(PropertyManager.DistinguishedName, true));
			}

			return propertyEntry;
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
                if (!this.searchFlagsInitialized)
                {
                    object value = GetValueFromCache(PropertyManager.SearchFlags, false);

                    if (value != null)
                    {
                        this.searchFlags = (SearchFlags)((int)value);
                    }
                    this.searchFlagsInitialized = true;
                }
            }

        }

		private bool IsSetInSearchFlags(SearchFlags searchFlagBit) {
			InitializeSearchFlags();
			return (((int)searchFlags & (int)searchFlagBit) != 0);
		}

		private void SetBitInSearchFlags(SearchFlags searchFlagBit) 
		{
			InitializeSearchFlags();
			searchFlags = (SearchFlags)((int)searchFlags | (int)searchFlagBit);

			if (isBound) {
				// get the distinguished name to construct the directory entry 
				GetSchemaPropertyDirectoryEntry();
				Debug.Assert(propertyEntry != null);
				
				// set the value on the directory entry
				propertyEntry.Properties[PropertyManager.SearchFlags].Value = (int) searchFlags;
			}
			
		}
			
		private void ResetBitInSearchFlags(SearchFlags searchFlagBit) 
		{
			InitializeSearchFlags();
			searchFlags = (SearchFlags)((int)searchFlags &  ~((int)searchFlagBit));

			if (isBound) {
				// get the distinguished name to construct the directory entry 
				GetSchemaPropertyDirectoryEntry();
				Debug.Assert(propertyEntry != null);
				
				// set the value on the directory entry
				propertyEntry.Properties[PropertyManager.SearchFlags].Value = (int) searchFlags;
			}
		}

		private void SetProperty(string propertyName, object value) {

			// get the distinguished name to construct the directory entry 
			GetSchemaPropertyDirectoryEntry();
			Debug.Assert(propertyEntry != null);
			
			if (value == null) {
				if (propertyEntry.Properties.Contains(propertyName)) {
				propertyEntry.Properties[propertyName].Clear();
				}
			}
			else {
				propertyEntry.Properties[propertyName].Value = value;
			}
		}

		private ActiveDirectorySyntax MapSyntax(string syntaxId, int oMID, OMObjectClass oMObjectClass) {
			for (int i = 0; i < SyntaxesCount; i++) {
				if (syntaxes[i].Equals(new Syntax(syntaxId, oMID, oMObjectClass))) {
					return (ActiveDirectorySyntax)i;
				}
			}
			throw new ActiveDirectoryOperationException(Res.GetString(Res.UnknownSyntax, ldapDisplayName));
		}

		private void SetSyntax(ActiveDirectorySyntax syntax) {

			if ((((int)syntax) < 0) || (((int)syntax) > (SyntaxesCount - 1))) {
				throw new InvalidEnumArgumentException("syntax", (int)syntax, typeof(ActiveDirectorySyntax));
			}

			// get the distinguished name to construct the directory entry 
			GetSchemaPropertyDirectoryEntry();
			Debug.Assert(propertyEntry != null);

			propertyEntry.Properties[PropertyManager.AttributeSyntax].Value = syntaxes[(int)syntax].attributeSyntax;
			propertyEntry.Properties[PropertyManager.OMSyntax].Value = syntaxes[(int)syntax].oMSyntax;
			OMObjectClass oMObjectClass = syntaxes[(int)syntax].oMObjectClass;
			if (oMObjectClass != null) {
				propertyEntry.Properties[PropertyManager.OMObjectClass].Value = oMObjectClass.Data;
			}
		}
		#endregion private methods
	}
}
