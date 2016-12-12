//------------------------------------------------------------------------------
// <copyright file="ActiveDirectorySchemaClass.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices.ActiveDirectory {
	using System;
	using System.Text;
	using System.ComponentModel;
	using System.Collections;
	using System.Diagnostics;
	using System.Globalization;
	using System.Runtime.InteropServices;
	using System.Security.AccessControl;
       using System.Security.Permissions;

       [DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted=true)]
	public class ActiveDirectorySchemaClass : IDisposable {

		// private variables
		private DirectoryEntry classEntry = null;
		private DirectoryEntry schemaEntry = null;
		private DirectoryEntry abstractClassEntry = null;
		NativeComInterfaces.IAdsClass iadsClass = null;
		private DirectoryContext context = null;
		internal bool isBound = false;
		private bool disposed = false;
		private ActiveDirectorySchema schema = null;
		private bool propertiesFromSchemaContainerInitialized = false;
		private bool isDefunctOnServer = false;
		private Hashtable propertyValuesFromServer = null;
		
		// private variables for all the properties of this class
		private string ldapDisplayName = null;
		private string commonName = null;
		private string oid = null;
		private string description = null;
		private bool descriptionInitialized = false;
		private bool isDefunct = false;
		private ActiveDirectorySchemaClassCollection possibleSuperiors = null;
		private ActiveDirectorySchemaClassCollection auxiliaryClasses = null;
		private ReadOnlyActiveDirectorySchemaClassCollection possibleInferiors = null;
		private ActiveDirectorySchemaPropertyCollection mandatoryProperties = null;
		private ActiveDirectorySchemaPropertyCollection optionalProperties = null;
		private ActiveDirectorySchemaClass subClassOf = null;
		private SchemaClassType type = SchemaClassType.Structural;
		private bool typeInitialized = false;
		private byte[] schemaGuidBinaryForm = null;
		private string defaultSDSddlForm = null;
		private bool defaultSDSddlFormInitialized = false;

		#region constructors
		public ActiveDirectorySchemaClass(DirectoryContext context, string ldapDisplayName)	{

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
			// the common name will default to the ldap display name
			this.commonName = ldapDisplayName;
			
			// set the bind flag
			this.isBound = false;
		}

		// Internal constructor
		internal ActiveDirectorySchemaClass(DirectoryContext context, string ldapDisplayName, DirectoryEntry classEntry, DirectoryEntry schemaEntry)
		{
			this.context = context;
			this.ldapDisplayName = ldapDisplayName;
			this.classEntry = classEntry;
			this.schemaEntry = schemaEntry;

			// this constructor is only called for non-defunct classes
			this.isDefunctOnServer = false;
			this.isDefunct = isDefunctOnServer;

			// initialize the directory entry for the abstract schema class
			try {
				this.abstractClassEntry = DirectoryEntryManager.GetDirectoryEntryInternal(context, "LDAP://" + context.GetServerName() + "/schema/" + ldapDisplayName);
				iadsClass = (NativeComInterfaces.IAdsClass) abstractClassEntry.NativeObject;
			}
			catch (COMException e) {
				if (e.ErrorCode == unchecked((int) 0x80005000)) {
					throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.DSNotFound), typeof(ActiveDirectorySchemaClass), ldapDisplayName);
				}
				else {
					throw ExceptionHelper.GetExceptionFromCOMException(context, e);
				}
			}
			catch (InvalidCastException) {
				// this means that we found an object but it is not a schema class
				throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.DSNotFound), typeof(ActiveDirectorySchemaClass), ldapDisplayName);
			}
			catch (ActiveDirectoryObjectNotFoundException) {
				// this is the case where the context is a config set and we could not find an ADAM instance in that config set
				throw new ActiveDirectoryOperationException(Res.GetString(Res.ADAMInstanceNotFoundInConfigSet, context.Name));
			}

			// set the bind flag
			this.isBound = true;
		}

		internal ActiveDirectorySchemaClass(DirectoryContext context, string commonName, Hashtable propertyValuesFromServer, DirectoryEntry schemaEntry) {

			this.context = context;
			this.schemaEntry = schemaEntry;

			// all relevant properties have already been retrieved from the server
			this.propertyValuesFromServer = propertyValuesFromServer;
			Debug.Assert(this.propertyValuesFromServer != null);
			propertiesFromSchemaContainerInitialized = true;
			this.classEntry = GetSchemaClassDirectoryEntry();

			// names
			this.commonName = commonName;
			this.ldapDisplayName = (string) GetValueFromCache(PropertyManager.LdapDisplayName, true);

			// this constructor is only called for defunct classes
			this.isDefunctOnServer = true;
			this.isDefunct = isDefunctOnServer;

			// set the bind flag
			this.isBound = true;
		}

		internal ActiveDirectorySchemaClass(DirectoryContext context, string commonName, string ldapDisplayName, DirectoryEntry classEntry, DirectoryEntry schemaEntry) {

			this.context = context;
			this.schemaEntry = schemaEntry;
			this.classEntry = classEntry;

			// names
			this.commonName = commonName;
			this.ldapDisplayName = ldapDisplayName;

			// this constructor is only called for defunct classes
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
			if (!this.disposed) {
				// check if this is an explicit Dispose
				// only then clean up the directory entries
				if (disposing) {
					// dispose schema entry
					if (schemaEntry != null) {
						schemaEntry.Dispose();
						schemaEntry = null;
					}
					// dispose class entry
					if (classEntry != null)  {
						classEntry.Dispose();
						classEntry = null;
					}
					// dispose abstract class entry
					if (abstractClassEntry != null)  {
						abstractClassEntry.Dispose();
						abstractClassEntry = null;
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
		public static ActiveDirectorySchemaClass FindByName(DirectoryContext context, string ldapDisplayName) {
			ActiveDirectorySchemaClass schemaClass = null;

                     if (context == null) {
				throw new ArgumentNullException("context");
			}

			if ((context.Name == null) && (!context.isRootDomain())) {
				throw new ArgumentException(Res.GetString(Res.ContextNotAssociatedWithDomain), "context");
			}

			if(context.Name != null)
			{
				if(!(context.isRootDomain() || context.isServer() || context.isADAMConfigSet())) {
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
			
			// create a schema class 
			schemaClass = new ActiveDirectorySchemaClass(context, ldapDisplayName, (DirectoryEntry) null, null);		

			return schemaClass;
		}
	

		public ReadOnlyActiveDirectorySchemaPropertyCollection GetAllProperties() {
			CheckIfDisposed();
			
			ArrayList properties = new ArrayList();

			// get the mandatory properties 
			properties.AddRange(MandatoryProperties);
			// get the optional properties
			properties.AddRange(OptionalProperties);

			return new ReadOnlyActiveDirectorySchemaPropertyCollection(properties);
		}

		public void Save(){

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
					classEntry = schemaEntry.Children.Add(rdn, "classSchema");
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
				SetProperty(PropertyManager.GovernsID, oid);

				// set the description
				SetProperty(PropertyManager.Description, description);

				// set the possibleSuperiors property
				if (possibleSuperiors != null)	{
					classEntry.Properties[PropertyManager.PossibleSuperiors].AddRange(possibleSuperiors.GetMultiValuedProperty());
				}

				// set the mandatoryProperties property
				if (mandatoryProperties != null) {
					classEntry.Properties[PropertyManager.MustContain].AddRange(mandatoryProperties.GetMultiValuedProperty());
				}

				// set the optionalProperties property
				if (optionalProperties != null) {
					classEntry.Properties[PropertyManager.MayContain].AddRange(optionalProperties.GetMultiValuedProperty());
				}

				// set the subClassOf property
				if (subClassOf != null) {
					SetProperty(PropertyManager.SubClassOf, subClassOf.Name);
				}
				else {
					// if no super class is specified, set it to "top"
					SetProperty(PropertyManager.SubClassOf, "top");
				}

				// set the objectClassCategory property
				SetProperty(PropertyManager.ObjectClassCategory, type);
				
				// set the schemaIDGuid property
				if (schemaGuidBinaryForm != null) {
					SetProperty(PropertyManager.SchemaIDGuid, schemaGuidBinaryForm);
				}

				// set the default security descriptor
				if (defaultSDSddlForm != null) {
					SetProperty(PropertyManager.DefaultSecurityDescriptor, defaultSDSddlForm);
				}
				
			}


			try {
				// commit the classEntry to server
				classEntry.CommitChanges();

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
			description = null;
			descriptionInitialized = false;
			possibleSuperiors = null;
			auxiliaryClasses = null;
			possibleInferiors = null;
			mandatoryProperties = null;
			optionalProperties = null;
			subClassOf = null;
			typeInitialized = false;
			schemaGuidBinaryForm = null;
			defaultSDSddlForm = null;
			defaultSDSddlFormInitialized = false;
			propertiesFromSchemaContainerInitialized = false;
			
			// set bind flag
			isBound = true;
		}

		public override string ToString() {
			return Name;
		}

		public DirectoryEntry GetDirectoryEntry() {

			CheckIfDisposed();
			
			if (!isBound) {
				throw new InvalidOperationException(Res.GetString(Res.CannotGetObject));
			}

			GetSchemaClassDirectoryEntry();
			Debug.Assert(classEntry != null);
			
			return DirectoryEntryManager.GetDirectoryEntryInternal(context, classEntry.Path);
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
								oid = iadsClass.OID;
							}
							catch (COMException e) {
								throw ExceptionHelper.GetExceptionFromCOMException(context, e);
							}
						}
						else {
							oid = (string) GetValueFromCache(PropertyManager.GovernsID, true);
						}
					}
				}
				return oid;
			}
			set {
				CheckIfDisposed();
				
				if (isBound) {

					// set the value on the directory entry
					SetProperty(PropertyManager.GovernsID, value);
				}
				oid = value;
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
				
				if (isBound) {

					// set the value on the directory entry
					SetProperty(PropertyManager.Description, value);
				}
				description = value;
			}
		}

		public bool IsDefunct {
			get {
				CheckIfDisposed();
				// this is initialized for bound classes in the constructor
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

		public ActiveDirectorySchemaClassCollection PossibleSuperiors {
			get {
				CheckIfDisposed();
				
				if (possibleSuperiors == null) {

					if (isBound) {
					
						// get the property from the abstract schema/ schema container
						// (for non-defunt classes this property is available in the abstract schema)
						if (!isDefunctOnServer) {

							ArrayList possibleSuperiorsList = new ArrayList();
							bool listEmpty = false;

							//
							// IADsClass.PossibleSuperiors can return either a collection or a string
							// (if there is only one value)
							//
							object value = null;
							try {
								value = iadsClass.PossibleSuperiors;
							}
							catch (COMException e) {
								if (e.ErrorCode == unchecked((int) 0x8000500D)) {
									listEmpty = true;
                                				}
								else {
									throw ExceptionHelper.GetExceptionFromCOMException(context, e);
								}
							}

							if (!listEmpty) {
								if (value is ICollection) {
									possibleSuperiorsList.AddRange((ICollection) value);
								}
								else {
									// single value
									possibleSuperiorsList.Add((string) value);
								}
			
								possibleSuperiors = new ActiveDirectorySchemaClassCollection(context, this, true /* isBound */, PropertyManager.PossibleSuperiors, possibleSuperiorsList, true /* onlyNames */);
							}
							else {
								// there are no superiors, return an emtpy collection
								possibleSuperiors = new ActiveDirectorySchemaClassCollection(context, this, true /* is Bound */, PropertyManager.PossibleSuperiors, new ArrayList());
							}
						}
						else {
							ArrayList possibleSuperiorsList = new ArrayList();
							possibleSuperiorsList.AddRange(GetValuesFromCache(PropertyManager.PossibleSuperiors));
							possibleSuperiorsList.AddRange(GetValuesFromCache(PropertyManager.SystemPossibleSuperiors));
							
							possibleSuperiors = new ActiveDirectorySchemaClassCollection(context, this, true /* isBound */, PropertyManager.PossibleSuperiors, GetClasses(possibleSuperiorsList));
						}
					}
					else {
						possibleSuperiors = new ActiveDirectorySchemaClassCollection(context, this, false /* is Bound */, PropertyManager.PossibleSuperiors, new ArrayList());
					}
				}
				
				return possibleSuperiors;
			}
		}

		public ReadOnlyActiveDirectorySchemaClassCollection PossibleInferiors {
			get {
				CheckIfDisposed();

				if (possibleInferiors == null) {
					if (isBound) {
						// get the value from the server
						possibleInferiors = new ReadOnlyActiveDirectorySchemaClassCollection(GetClasses(GetValuesFromCache(PropertyManager.PossibleInferiors)));
					}
					else {
						possibleInferiors = new ReadOnlyActiveDirectorySchemaClassCollection(new ArrayList());
					}
				}
				return possibleInferiors;
			}
		}

		public ActiveDirectorySchemaPropertyCollection MandatoryProperties {
			get {
				CheckIfDisposed();

				if (mandatoryProperties == null) {
					
					if (isBound) {
					
						// get the property from the abstract schema/ schema container
						// (for non-defunt classes this property is available in the abstract schema)
						if (!isDefunctOnServer) {
							
							ArrayList mandatoryPropertiesList = new ArrayList();
							bool listEmpty = false;

							//
							// IADsClass.MandatoryProperties can return either a collection or a string
							// (if there is only one value)
							//
							object value = null;
							try {
								value = iadsClass.MandatoryProperties;
							}
							catch (COMException e) {
								if (e.ErrorCode == unchecked((int) 0x8000500D)) {
									listEmpty = true;
                                				}
								else {
									throw ExceptionHelper.GetExceptionFromCOMException(context, e);
								}
							}

							if (!listEmpty) {
								if (value is ICollection) {
									mandatoryPropertiesList.AddRange((ICollection) value);
								}
								else {
									// single value
									mandatoryPropertiesList.Add((string) value);
								}
								
								mandatoryProperties = new ActiveDirectorySchemaPropertyCollection(context, this, true /* isBound */, PropertyManager.MustContain, mandatoryPropertiesList, true /* onlyNames */);
							}
							else {
								// there are no mandatory properties, return an emtpy collection
								mandatoryProperties = new ActiveDirectorySchemaPropertyCollection(context, this, true /* isBound */, PropertyManager.MustContain, new ArrayList());
							}
						}
						else {
							string[] propertyNames = new string[2];
							propertyNames[0] = PropertyManager.SystemMustContain;
							propertyNames[1] = PropertyManager.MustContain;
							
							mandatoryProperties = new ActiveDirectorySchemaPropertyCollection(context, this, true /* isBound */, PropertyManager.MustContain, GetProperties(GetPropertyValuesRecursively(propertyNames)));
						}
					}
					else {
						mandatoryProperties = new ActiveDirectorySchemaPropertyCollection(context, this, false /* isBound */, PropertyManager.MustContain, new ArrayList());
					}
				}
				return mandatoryProperties;
			}
		}

		public ActiveDirectorySchemaPropertyCollection OptionalProperties {
			get {
				CheckIfDisposed();

				if (optionalProperties == null) {
					
					if (isBound) {
						
						// get the property from the abstract schema/ schema container
						// (for non-defunt classes this property is available in the abstract schema)
						if (!isDefunctOnServer) {

							ArrayList optionalPropertiesList = new ArrayList();
							bool listEmpty = false;

							//
							// IADsClass.OptionalProperties can return either a collection or a string
							// (if there is only one value)
							//
							object value = null;
							try {
								value = iadsClass.OptionalProperties;
							}
							catch (COMException e) {
								if (e.ErrorCode == unchecked((int) 0x8000500D)) {
									listEmpty = true;
								}
								else {	
									throw ExceptionHelper.GetExceptionFromCOMException(context, e);
								}
							}

							if (!listEmpty) {
								if (value is ICollection) {
									optionalPropertiesList.AddRange((ICollection) value);
								}
								else {
									// single value
									optionalPropertiesList.Add((string) value);
								}
								
								optionalProperties = new ActiveDirectorySchemaPropertyCollection(context, this, true /* isBound */, PropertyManager.MayContain, optionalPropertiesList, true /* onlyNames */);
							}
							else {
								// there are no optional properties, return an emtpy collection
								optionalProperties = new ActiveDirectorySchemaPropertyCollection(context, this, true /* isBound */, PropertyManager.MayContain, new ArrayList());
							}
						}
						else {
							string[] propertyNames = new string[2];
							propertyNames[0] = PropertyManager.SystemMayContain;
							propertyNames[1] = PropertyManager.MayContain;

							ArrayList optionalPropertyList = new ArrayList();
							foreach (string propertyName in GetPropertyValuesRecursively(propertyNames)) {
								if (!MandatoryProperties.Contains(propertyName)) {
									optionalPropertyList.Add(propertyName);
								}
							}
							
							optionalProperties = new ActiveDirectorySchemaPropertyCollection(context, this, true /* isBound */, PropertyManager.MayContain, GetProperties(optionalPropertyList));
						}
					}
					else {
						optionalProperties = new ActiveDirectorySchemaPropertyCollection(context, this, false /* isBound */, PropertyManager.MayContain, new ArrayList());
					}
				}
				return optionalProperties;
			}
		}

		public ActiveDirectorySchemaClassCollection AuxiliaryClasses {
			get {
				CheckIfDisposed();

				if (auxiliaryClasses == null) {

					if (isBound) {

						// get the property from the abstract schema/ schema container
						// (for non-defunt classes this property is available in the abstract schema)
						if (!isDefunctOnServer) {

							ArrayList auxiliaryClassesList = new ArrayList();
							bool listEmpty = false;

							//
							// IADsClass.AuxDerivedFrom can return either a collection or a string
							// (if there is only one value)
							//
							object value = null;
							try {
								value = iadsClass.AuxDerivedFrom;
							}
							catch (COMException e) {
								if (e.ErrorCode == unchecked((int) 0x8000500D)) {
									listEmpty = true;
                                				}
								else {
									throw ExceptionHelper.GetExceptionFromCOMException(context, e);
								}
							}

							if (!listEmpty) {
								if (value is ICollection) {
									auxiliaryClassesList.AddRange((ICollection) value);
								}
								else {
									// single value
									auxiliaryClassesList.Add((string) value);
								}
								
								auxiliaryClasses = new ActiveDirectorySchemaClassCollection(context, this, true /* isBound */, PropertyManager.AuxiliaryClass, auxiliaryClassesList, true /* onlyNames */);
							}
							else {
								// there are no auxiliary classes, return an emtpy collection
								auxiliaryClasses = new ActiveDirectorySchemaClassCollection(context, this, true /* is Bound */, PropertyManager.AuxiliaryClass, new ArrayList());
							}
						}
						else {
							string[] propertyNames = new string[2];
							propertyNames[0] = PropertyManager.AuxiliaryClass;
							propertyNames[1] = PropertyManager.SystemAuxiliaryClass;
							
							auxiliaryClasses = new ActiveDirectorySchemaClassCollection(context, this, true /* isBound */, PropertyManager.AuxiliaryClass, GetClasses(GetPropertyValuesRecursively(propertyNames)));
						}
					}
					else {
						auxiliaryClasses = new ActiveDirectorySchemaClassCollection(context, this, false /* isBound */, PropertyManager.AuxiliaryClass, new ArrayList());
					}
				}
				return auxiliaryClasses;
			}
		}
		

		public ActiveDirectorySchemaClass SubClassOf {
			get {
				CheckIfDisposed();
				
				if (isBound) {

					if (subClassOf == null) {
						// get the property from the server 
						subClassOf = new ActiveDirectorySchemaClass(context, (string) GetValueFromCache(PropertyManager.SubClassOf, true), (DirectoryEntry) null, schemaEntry);
					}
				}
				return subClassOf;
			}
			set {
				CheckIfDisposed();
				
				if (isBound) {

					// set the value on the directory entry
					SetProperty(PropertyManager.SubClassOf, value);
				}
				subClassOf = value;
			}
			
		}

		public SchemaClassType Type {
			get {
				CheckIfDisposed();
				
				if (isBound) {

					if (!typeInitialized) {
						// get the property from the server 
						type = (SchemaClassType) ((int)GetValueFromCache(PropertyManager.ObjectClassCategory, true));
						typeInitialized = true;
					}
				}
				return type;
			}
			set {
				CheckIfDisposed();

				// validate the value that is being set
				if (value < SchemaClassType.Type88 || value > SchemaClassType.Auxiliary) {
					throw new InvalidEnumArgumentException("value", (int)value, typeof(SchemaClassType));
				}
				
				if (isBound) {

					// set the value on the directory entry
					SetProperty(PropertyManager.ObjectClassCategory, value);
				}
				type = value;
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

		public ActiveDirectorySecurity DefaultObjectSecurityDescriptor {
			get {
				CheckIfDisposed();

				ActiveDirectorySecurity defaultObjectSecurityDescriptor = null;
				
				if (isBound) {

					if (!defaultSDSddlFormInitialized) {
						// get the property from the server 
						defaultSDSddlForm = (string) GetValueFromCache(PropertyManager.DefaultSecurityDescriptor, false);
						defaultSDSddlFormInitialized = true;
					}
				}

				// we cache the sddl form and create a ActiveDirectorySecurity object each time
				if (defaultSDSddlForm != null) {
					defaultObjectSecurityDescriptor = new ActiveDirectorySecurity();
					defaultObjectSecurityDescriptor.SetSecurityDescriptorSddlForm(defaultSDSddlForm);
				}

				return defaultObjectSecurityDescriptor;
			}
			set {
				CheckIfDisposed();
				
				if (isBound) {

					// set the value on the directory entry
					SetProperty(PropertyManager.DefaultSecurityDescriptor, (value == null) ? null : value.GetSecurityDescriptorSddlForm(AccessControlSections.All));
				}
				defaultSDSddlForm = (value == null) ? null : value.GetSecurityDescriptorSddlForm(AccessControlSections.All);
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
			ArrayList values = (ArrayList) propertyValuesFromServer[propertyName.ToLower(CultureInfo.InvariantCulture)];

			Debug.Assert(values != null);
			if (values.Count < 1 && mustExist) {
				throw new ActiveDirectoryOperationException(Res.GetString(Res.PropertyNotFound, propertyName));
			}
			else if (values.Count > 0) {
				value = values[0];
			}

			return value;
		}

		//
		// This method retrieves all the values of a property (single valued) from the values
		// that were retrieved from the server. 
		//
		private ICollection GetValuesFromCache(string propertyName) {

			// retrieve the properties from the server if necessary
			InitializePropertiesFromSchemaContainer();

			Debug.Assert(propertyValuesFromServer != null);
			ArrayList values = (ArrayList) propertyValuesFromServer[propertyName.ToLower(CultureInfo.InvariantCulture)];

			Debug.Assert(values != null);
			return values;
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
		internal static Hashtable GetPropertiesFromSchemaContainer(DirectoryContext context, DirectoryEntry schemaEntry, string name, bool isDefunctOnServer) {

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
			if (isDefunctOnServer) {

				propertyNamesWithoutRangeRetrieval.Add(PropertyManager.LdapDisplayName);
				propertyNamesWithoutRangeRetrieval.Add(PropertyManager.GovernsID);
				propertyNamesWithRangeRetrieval.Add(PropertyManager.SystemPossibleSuperiors);
				propertyNamesWithRangeRetrieval.Add(PropertyManager.PossibleSuperiors);	
			}

			try {
				propertyValuesFromServer = Utils.GetValuesWithRangeRetrieval(schemaEntry, str.ToString(), propertyNamesWithRangeRetrieval, propertyNamesWithoutRangeRetrieval, SearchScope.OneLevel);
			}
			catch (COMException e) {
				if (e.ErrorCode == unchecked((int)  0x80072030)) {
					// object is not found since we cannot even find the container in which to search
					throw new ActiveDirectoryObjectNotFoundException(Res.GetString(Res.DSNotFound), typeof(ActiveDirectorySchemaClass), name);
				}
				else {
					throw ExceptionHelper.GetExceptionFromCOMException(context, e);
				}
			}
			
			return propertyValuesFromServer;

		}

		internal DirectoryEntry GetSchemaClassDirectoryEntry() {

			if (classEntry == null) {
			
				InitializePropertiesFromSchemaContainer();
				classEntry = DirectoryEntryManager.GetDirectoryEntry(context, (string) GetValueFromCache(PropertyManager.DistinguishedName, true));
			}

			return classEntry;
		}

		private void SetProperty(string propertyName, object value) {

			// get the distinguished name to construct the directory entry 
			GetSchemaClassDirectoryEntry();
			Debug.Assert(classEntry != null);

			try {
				if ((value == null)) {
					if (classEntry.Properties.Contains(propertyName)) {
						classEntry.Properties[propertyName].Clear();
					}
				}
				else {
					classEntry.Properties[propertyName].Value = value;
				}
			}
			catch (COMException e) {
				throw ExceptionHelper.GetExceptionFromCOMException(context, e);
			}
		}

		//
		// This method searches in the schema container for all non-defunct classes of the 
		// specified name (ldapDisplayName).
		//
		private ArrayList GetClasses(ICollection ldapDisplayNames) {
			ArrayList classes = new ArrayList();
			SearchResultCollection resCol = null;
			
			try {

				if (ldapDisplayNames.Count < 1) {
					return classes;
				}
			
				if (schemaEntry == null) {
					schemaEntry = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.SchemaNamingContext);
				}

				// constructing the filter
				StringBuilder str = new StringBuilder(100);

				if (ldapDisplayNames.Count > 1) {
					str.Append("(|");
				}
				foreach (string ldapDisplayName in ldapDisplayNames) {
					str.Append("(");
					str.Append(PropertyManager.LdapDisplayName);
					str.Append("=");
					str.Append(Utils.GetEscapedFilterValue(ldapDisplayName));
					str.Append(")");
				}
				if (ldapDisplayNames.Count > 1) {
					str.Append(")");
				}
				
				string filter = "(&(" + PropertyManager.ObjectCategory + "=classSchema)" + str.ToString() + "(!(" + PropertyManager.IsDefunct + "=TRUE)))";

				string[] propertiesToLoad = new String[1];
				propertiesToLoad[0] = PropertyManager.LdapDisplayName;

				ADSearcher searcher = new ADSearcher(schemaEntry, filter, propertiesToLoad, SearchScope.OneLevel);
				resCol = searcher.FindAll();

				foreach (SearchResult res in resCol) {
					string ldapDisplayName = (string)PropertyManager.GetSearchResultPropertyValue(res, PropertyManager.LdapDisplayName);
					DirectoryEntry de = res.GetDirectoryEntry();

                                   de.AuthenticationType = Utils.DefaultAuthType;
					de.Username = context.UserName;
					de.Password = context.Password;

					ActiveDirectorySchemaClass schemaClass = new ActiveDirectorySchemaClass(context, ldapDisplayName, de, schemaEntry);

					classes.Add(schemaClass);
				}				

			}
			catch (COMException e) {
				throw ExceptionHelper.GetExceptionFromCOMException(context, e);
			}
			finally {
				if (resCol != null)  {
					resCol.Dispose();
				}
			}
			
			return classes;
		}

		//
		// This method searches in the schema container for all non-defunct properties of the 
		// specified name (ldapDisplayName).
		//
		private ArrayList GetProperties(ICollection ldapDisplayNames) {
			ArrayList properties = new ArrayList();
			SearchResultCollection resCol = null;
			
			try {

				if (ldapDisplayNames.Count < 1) {
					return properties;
				}
			
				if (schemaEntry == null) {
					schemaEntry = DirectoryEntryManager.GetDirectoryEntry(context, WellKnownDN.SchemaNamingContext);
				}

				// constructing the filter
				StringBuilder str = new StringBuilder(100);

				if (ldapDisplayNames.Count > 1) {
					str.Append("(|");
				}
				foreach (string ldapDisplayName in ldapDisplayNames) {
					str.Append("(");
					str.Append(PropertyManager.LdapDisplayName);
					str.Append("=");
					str.Append(Utils.GetEscapedFilterValue(ldapDisplayName));
					str.Append(")");
				}
				if (ldapDisplayNames.Count > 1) {
					str.Append(")");
				}
				
				string filter = "(&(" + PropertyManager.ObjectCategory + "=attributeSchema)" + str.ToString() + "(!(" + PropertyManager.IsDefunct + "=TRUE)))";

				string[] propertiesToLoad = new String[1];
				propertiesToLoad[0] = PropertyManager.LdapDisplayName;

				ADSearcher searcher = new ADSearcher(schemaEntry, filter, propertiesToLoad, SearchScope.OneLevel);
				resCol = searcher.FindAll();

				foreach (SearchResult res in resCol) {
					string ldapDisplayName = (string)PropertyManager.GetSearchResultPropertyValue(res, PropertyManager.LdapDisplayName);
					DirectoryEntry de = res.GetDirectoryEntry();

                                      de.AuthenticationType = Utils.DefaultAuthType;
					de.Username = context.UserName;
					de.Password = context.Password;

					ActiveDirectorySchemaProperty schemaProperty = new ActiveDirectorySchemaProperty(context, ldapDisplayName, de, schemaEntry);

					properties.Add(schemaProperty);
				}				

			}
			catch (COMException e) {
				throw ExceptionHelper.GetExceptionFromCOMException(context, e);
			}
			finally {
				if (resCol != null)  {
					resCol.Dispose();
				}
			}
			
			return properties;
		}

		private ArrayList GetPropertyValuesRecursively(string[] propertyNames) {
			ArrayList values = new ArrayList();

			// get the properties of the super class
			try {
				if (Utils.Compare(SubClassOf.Name, Name) != 0) {
					foreach (string value in SubClassOf.GetPropertyValuesRecursively(propertyNames)) {
						if (!values.Contains(value)) {
							values.Add(value);
						}
					}
				}

				// get the properties of the auxiliary classes
				foreach (string auxSchemaClassName in GetValuesFromCache(PropertyManager.AuxiliaryClass)) {

					ActiveDirectorySchemaClass auxSchemaClass = new ActiveDirectorySchemaClass(context, auxSchemaClassName, (DirectoryEntry) null, null);
				
					foreach (string property in auxSchemaClass.GetPropertyValuesRecursively(propertyNames)) {
						if (!values.Contains(property)) {
							values.Add(property);
						}
					}
				}
				foreach (string auxSchemaClassName in GetValuesFromCache(PropertyManager.SystemAuxiliaryClass)) {

					ActiveDirectorySchemaClass auxSchemaClass = new ActiveDirectorySchemaClass(context, auxSchemaClassName, (DirectoryEntry) null, null);
					
					foreach (string property in auxSchemaClass.GetPropertyValuesRecursively(propertyNames)) {
						if (!values.Contains(property)) {
							values.Add(property);
						}
					}
				}
				
			}
			catch (COMException e) {
				throw ExceptionHelper.GetExceptionFromCOMException(context, e);
			}

			foreach (string propertyName in propertyNames) {
				
				foreach (string value in GetValuesFromCache(propertyName)) {
					if (!values.Contains(value)) {
						values.Add(value);
					}
				}
			}

			return values;
		}

		#endregion private methods
	} 
}
