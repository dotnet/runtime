//------------------------------------------------------------------------------
// <copyright file="ActiveDirectorySchemaPropertyCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices.ActiveDirectory {
	using System;
	using System.Collections;
	using System.Globalization;
	using System.Runtime.InteropServices;

	public class ActiveDirectorySchemaPropertyCollection: CollectionBase {

		private DirectoryEntry classEntry = null;
		private string propertyName = null;
		private ActiveDirectorySchemaClass schemaClass = null;
		private bool isBound = false;
		private DirectoryContext context = null;

		internal ActiveDirectorySchemaPropertyCollection(DirectoryContext context,
														ActiveDirectorySchemaClass schemaClass,
														bool isBound,
														string propertyName,
														ICollection propertyNames,
														bool onlyNames) {
			this.schemaClass = schemaClass;
			this.propertyName = propertyName;
			this.isBound= isBound;
			this.context = context;

			foreach (string ldapDisplayName in propertyNames) {
				// all properties in writeable property collection are non-defunct
				// so calling constructor for non-defunct property
				this.InnerList.Add(new ActiveDirectorySchemaProperty(context, ldapDisplayName, (DirectoryEntry) null, null));
			}
		}

		internal ActiveDirectorySchemaPropertyCollection(DirectoryContext context,
														ActiveDirectorySchemaClass schemaClass,
														bool isBound,
														string propertyName,
														ICollection properties)  {
			this.schemaClass = schemaClass;
			this.propertyName = propertyName;
			this.isBound = isBound;
			this.context = context;

			foreach (ActiveDirectorySchemaProperty schemaProperty in properties) {
				this.InnerList.Add(schemaProperty);
			}
		}

		public ActiveDirectorySchemaProperty this[int index] {
			get {
				return (ActiveDirectorySchemaProperty)List[index];
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");

				if(!value.isBound) {
	                		throw new InvalidOperationException(Res.GetString(Res.SchemaObjectNotCommitted, value.Name));          
				} 

				if (!Contains(value)) {
					List[index] = value;
				}
				else {
					throw new ArgumentException(Res.GetString(Res.AlreadyExistingInCollection, value), "value");
				}
			}
		}

		public int Add(ActiveDirectorySchemaProperty schemaProperty) {
			if (schemaProperty == null) {
				throw new ArgumentNullException("schemaProperty");
			}

			if(!schemaProperty.isBound) {
                		throw new InvalidOperationException(Res.GetString(Res.SchemaObjectNotCommitted, schemaProperty.Name));          
			}  

			if (!Contains(schemaProperty)) {
				return List.Add(schemaProperty);
			}
			else {
				throw new ArgumentException(Res.GetString(Res.AlreadyExistingInCollection, schemaProperty), "schemaProperty");
			}
		}

		public void AddRange(ActiveDirectorySchemaProperty[] properties) {            
			if (properties == null) {
				throw new ArgumentNullException("properties");
			}

			foreach (ActiveDirectorySchemaProperty property in properties) {
				if (property == null) {
					throw new ArgumentException("properties");
				}
			}

			for (int i = 0; ((i) < (properties.Length)); i = ((i) + (1))) {
				this.Add((ActiveDirectorySchemaProperty)properties[i]);
			}
		}

		public void AddRange(ActiveDirectorySchemaPropertyCollection properties) {            
			if (properties == null) {
				throw new ArgumentNullException("properties");
			}

			foreach (ActiveDirectorySchemaProperty property in properties) {
				if (property == null) {
					throw new ArgumentException("properties");
				}
			}

			int currentCount = properties.Count;

			for (int i = 0; i < currentCount; i++) {
				this.Add(properties[i]);
			}
		} 
  
		public void AddRange(ReadOnlyActiveDirectorySchemaPropertyCollection properties) {            
			if (properties == null) {
				throw new ArgumentNullException("properties");
			}

			foreach (ActiveDirectorySchemaProperty property in properties) {
				if (property == null) {
					throw new ArgumentException("properties");
				}
			}

			int currentCount = properties.Count;

			for (int i = 0; i < currentCount; i++) {
				this.Add(properties[i]);
			}
		}

		public void Remove(ActiveDirectorySchemaProperty schemaProperty) {

                     if (schemaProperty == null) {
				throw new ArgumentNullException("schemaProperty");
			}

			if(!schemaProperty.isBound) {
                		throw new InvalidOperationException(Res.GetString(Res.SchemaObjectNotCommitted, schemaProperty.Name));          
			}  
                    
			for (int i = 0; i < InnerList.Count; i++) {
				ActiveDirectorySchemaProperty tmp = (ActiveDirectorySchemaProperty)InnerList[i];
				if (Utils.Compare(tmp.Name, schemaProperty.Name) == 0) {
					List.Remove(tmp);
					return;
				}
			}
			throw new ArgumentException(Res.GetString(Res.NotFoundInCollection, schemaProperty), "schemaProperty"); 
		}

		public void Insert(int index, ActiveDirectorySchemaProperty schemaProperty) {            
			if (schemaProperty == null) {
				throw new ArgumentNullException("schemaProperty");
			}

			if(!schemaProperty.isBound) {
                		throw new InvalidOperationException(Res.GetString(Res.SchemaObjectNotCommitted, schemaProperty.Name));          
			}  

			if (!Contains(schemaProperty)) {
				List.Insert(index, schemaProperty);
			}
			else {
				throw new ArgumentException(Res.GetString(Res.AlreadyExistingInCollection, schemaProperty), "schemaProperty");
			}
		}

		public bool Contains(ActiveDirectorySchemaProperty schemaProperty) {  

                     if (schemaProperty == null) {
				throw new ArgumentNullException("schemaProperty");
			}

			if(!schemaProperty.isBound) {
                		throw new InvalidOperationException(Res.GetString(Res.SchemaObjectNotCommitted, schemaProperty.Name));          
			}  
                      
			for (int i = 0; i < InnerList.Count; i++) {
				ActiveDirectorySchemaProperty tmp = (ActiveDirectorySchemaProperty)InnerList[i];
				if (Utils.Compare(tmp.Name, schemaProperty.Name) == 0) {
					return true;
				}
			}
			return false;
		} 
        
		internal  bool Contains(string propertyName) {
			for (int i = 0; i < InnerList.Count; i++) {
				ActiveDirectorySchemaProperty tmp = (ActiveDirectorySchemaProperty)InnerList[i];

				if (Utils.Compare(tmp.Name, propertyName) == 0) {
					return true;
				}
			}

			return false;
		}             

		public void CopyTo(ActiveDirectorySchemaProperty[] properties, int index) {            
			List.CopyTo(properties, index);
		}  

		public int IndexOf(ActiveDirectorySchemaProperty schemaProperty) {       

                     if (schemaProperty == null) {
				throw new ArgumentNullException("schemaProperty");
			}

			if(!schemaProperty.isBound) {
                		throw new InvalidOperationException(Res.GetString(Res.SchemaObjectNotCommitted, schemaProperty.Name));          
			}  
                      
			for (int i = 0; i < InnerList.Count; i++) {
				ActiveDirectorySchemaProperty tmp = (ActiveDirectorySchemaProperty)InnerList[i];
				if (Utils.Compare(tmp.Name, schemaProperty.Name) == 0) {
					return i;
				}
			}
			return -1;
		}

		protected override void OnClearComplete() {
			if (isBound) {

				if (classEntry == null) {
					classEntry = schemaClass.GetSchemaClassDirectoryEntry();
				}
				
				try {
					if (classEntry.Properties.Contains(propertyName)) {
						classEntry.Properties[propertyName].Clear();
					}
				}
				catch (COMException e) {
					throw ExceptionHelper.GetExceptionFromCOMException(context, e);
				}
			}
		}

		protected override void OnInsertComplete(int index, object value) {        
			if (isBound) {

				if (classEntry == null) {
					classEntry = schemaClass.GetSchemaClassDirectoryEntry();
				}
				
				try {
					classEntry.Properties[propertyName].Add(((ActiveDirectorySchemaProperty)value).Name);
				}
				catch (COMException e) {
					throw ExceptionHelper.GetExceptionFromCOMException(context, e);
				}
			}
		}

		protected override void OnRemoveComplete(int index, object value) {
			if (isBound) {

				if (classEntry == null) {
					classEntry = schemaClass.GetSchemaClassDirectoryEntry();
				}
				
				// because this collection can contain values from the superior classes,
				// these values would not exist in the classEntry 
				// and therefore cannot be removed
				// we need to throw an exception here
				string valueName = ((ActiveDirectorySchemaProperty)value).Name;

				try {
					if (classEntry.Properties[propertyName].Contains(valueName)) {
						classEntry.Properties[propertyName].Remove(valueName);
					}
					else {
						throw new ActiveDirectoryOperationException(Res.GetString(Res.ValueCannotBeModified));
					}
				}
				catch (COMException e) {
					throw ExceptionHelper.GetExceptionFromCOMException(context, e);
				}
			}
		}
		protected override void OnSetComplete(int index, object oldValue, object newValue) {
			if (isBound) {
				// remove the old value
				OnRemoveComplete(index, oldValue);

				// add the new value
				OnInsertComplete(index, newValue);
			}
		}

		protected override void OnValidate(Object value) {
			if (value == null) throw new ArgumentNullException("value");

			if (!(value is ActiveDirectorySchemaProperty))
				throw new ArgumentException("value");

                     if(!((ActiveDirectorySchemaProperty) value).isBound)
                            throw new InvalidOperationException(Res.GetString(Res.SchemaObjectNotCommitted, ((ActiveDirectorySchemaProperty) value).Name));
		}

		internal string[] GetMultiValuedProperty() {
			string[] values = new string[InnerList.Count];
			for (int i = 0; i < InnerList.Count; i++) {
				values[i] = ((ActiveDirectorySchemaProperty)InnerList[i]).Name;
			}
			return values;
		}
	}
}

