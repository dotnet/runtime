//------------------------------------------------------------------------------
// <copyright file="PropertyValueCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.DirectoryServices {

    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Diagnostics;
    using System.DirectoryServices.Interop;
    using System.Security.Permissions;

    /// <include file='doc\PropertyValueCollection.uex' path='docs/doc[@for="PropertyValueCollection"]/*' />
    /// <devdoc>
    ///    <para>Holds a collection of values for a multi-valued property.</para>
    /// </devdoc>
    [DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted=true)]
    public class PropertyValueCollection : CollectionBase {
        internal enum UpdateType {
            Add = 0,
            Delete = 1,
            Update = 2,
            None = 3
        }
            
        private DirectoryEntry entry;
        private string propertyName;
        private UpdateType updateType = UpdateType.None;
        private ArrayList changeList = null;        
        private bool allowMultipleChange = false;
        private bool needNewBehavior = false;


        internal PropertyValueCollection(DirectoryEntry entry, string propertyName) {
            this.entry = entry;
            this.propertyName = propertyName;
            PopulateList();            
            ArrayList tempList = new ArrayList();
            changeList = ArrayList.Synchronized(tempList);
            this.allowMultipleChange = entry.allowMultipleChange;
            string tempPath = entry.Path;
            if (tempPath == null || tempPath.Length == 0)
            {
                // user does not specify path, so we bind to default naming context using LDAP provider.
                needNewBehavior = true;
            }
            else
            {
                if(tempPath.StartsWith("LDAP:", StringComparison.Ordinal))
                    needNewBehavior = true;
            }

        }
        
        /// <include file='doc\PropertyValueCollection.uex' path='docs/doc[@for="PropertyValueCollection.this"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public object this[int index] {
            get {
                return List[index];
            }
            set {
                if(needNewBehavior && !allowMultipleChange)
                    throw new NotSupportedException();
                else
                {
                    List[index] = value;
                }
            }
            
        }

        /// <include file='doc\PropertyValueCollection.uex' path='docs/doc[@for="PropertyValueCollection.PropertyName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [ComVisible(false)]
        public string PropertyName {
            get {                                                 
                return propertyName;                
            }
        }
                
        /// <include file='doc\PropertyValueCollection.uex' path='docs/doc[@for="PropertyValueCollection.Value"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public object Value {
            get {
                if (this.Count == 0)
                    return null;
                else if (this.Count == 1) 
                    return List[0];
                else {
                    object[] objectArray = new object[this.Count];
                    List.CopyTo(objectArray, 0);                    
                    return objectArray;
                }                                        
            }
            
            set {   
            	try
            	{
            	    this.Clear();
            	}
            	catch ( System.Runtime.InteropServices.COMException e ) {
            	    if ( e.ErrorCode != unchecked((int)0x80004005) || (value == null))   
            	        // WinNT provider throws E_FAIL when null value is specified though actually ADS_PROPERTY_CLEAR option is used, need to catch exception
            	        // here. But at the same time we don't want to catch the exception if user explicitly sets the value to null.                                                                                                                  
            	        throw;
                }
                                             
                if (value == null)
                    return;     

                // we could not do Clear and Add, we have to bypass the existing collection cache
                changeList.Clear();
                
                if (value is Array) {                
                    // byte[] is a special case, we will follow what ADSI is doing, it must be an octet string. So treat it as a single valued attribute
                    if(value is byte[])
                        changeList.Add(value);
                    else if (value is object[])                        
                        changeList.AddRange((object[])value);
                    else {
                        //Need to box value type array elements.
                        object[] objArray = new object[((Array)value).Length];
                        ((Array)value).CopyTo(objArray, 0);
                        changeList.AddRange((object[])objArray);
                    }			
                }	 
                else
                    changeList.Add(value);

                object[] allValues = new object[changeList.Count];
                changeList.CopyTo(allValues, 0);
                entry.AdsObject.PutEx((int) AdsPropertyOperation.Update, propertyName, allValues);

                entry.CommitIfNotCaching();

                // populate the new context
                PopulateList();                                                                        
            }
        }
        
        /// <include file='doc\PropertyValueCollection.uex' path='docs/doc[@for="PropertyValueCollection.Add"]/*' />
        /// <devdoc>
        ///    <para>Appends the value to the set of values for this property.</para>
        /// </devdoc>
        public int Add(object value) {   
            return List.Add(value);
        }
        
        /// <include file='doc\PropertyValueCollection.uex' path='docs/doc[@for="PropertyValueCollection.AddRange"]/*' />
        /// <devdoc>
        ///    <para>Appends the values to the set of values for this property.</para>
        /// </devdoc>
        public void AddRange(object[] value) {            
            if (value == null) {
                throw new ArgumentNullException("value");
            }
            for (int i = 0; ((i) < (value.Length)); i = ((i) + (1))) {
                this.Add(value[i]);
            }
        }
    
        /// <include file='doc\PropertyValueCollection.uex' path='docs/doc[@for="PropertyValueCollection.AddRange1"]/*' />
        /// <devdoc>
        ///    <para>Appends the values to the set of values for this property.</para>
        /// </devdoc>
        public void AddRange(PropertyValueCollection value) {            
            if (value == null) {
                throw new ArgumentNullException("value");
            }
            int currentCount = value.Count;
            for (int i = 0; i < currentCount; i = ((i) + (1))) {
                this.Add(value[i]);
            }
        }         
    
        /// <include file='doc\PropertyValueCollection.uex' path='docs/doc[@for="PropertyValueCollection.Contains"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(object value) {            
            return List.Contains(value);
        }
    
        /// <include file='doc\PropertyValueCollection.uex' path='docs/doc[@for="PropertyValueCollection.CopyTo"]/*' />
        /// <devdoc>
        /// <para>Copies the elements of this instance into an <see cref='System.Array'/>,
        ///    starting at a particular index
        ///    into the given <paramref name="array"/>.</para>
        /// </devdoc>
        public void CopyTo(object[] array, int index) {            
            List.CopyTo(array, index);
        }        
    
        /// <include file='doc\PropertyValueCollection.uex' path='docs/doc[@for="PropertyValueCollection.IndexOf"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int IndexOf(object value) {            
            return List.IndexOf(value);
        }
        
        /// <include file='doc\PropertyValueCollection.uex' path='docs/doc[@for="PropertyValueCollection.Insert"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Insert(int index, object value) {            
            List.Insert(index, value);
        }
        
        /// <include file='doc\PropertyValueCollection.uex' path='docs/doc[@for="PropertyValueCollection.PopulateList"]/*' />
        ///<internalonly/>                           
        private void PopulateList() {      
            
            //No need to fill the cache here, when GetEx is calles, an implicit 
            //call to GetInfo will be called against an uninitialized property 
            //cache. Which is exactly what FillCache does.            
            //entry.FillCache(propertyName);
            object var;
            int unmanagedResult = entry.AdsObject.GetEx(propertyName, out var);            
            if(unmanagedResult != 0)
            {
                //  property not found (IIS provider returns 0x80005006, other provides return 0x8000500D).
                if((unmanagedResult == unchecked((int)0x8000500D)) || (unmanagedResult == unchecked((int)0x80005006)))
                {
                    return;
                }
                else
                {
                    throw COMExceptionHelper.CreateFormattedComException(unmanagedResult);
                }
                
            }          
            if (var is ICollection)
                InnerList.AddRange((ICollection)var);                        
            else                    
                InnerList.Add(var);                        

        }

        /// <include file='doc\PropertyValueCollection.uex' path='docs/doc[@for="PropertyValueCollection.Remove"]/*' />
        /// <devdoc>
        ///    <para>Removes the value from the collection.</para>
        /// </devdoc>
        public void Remove(object value) {
            if(needNewBehavior)
            {
                try
                {
                    List.Remove(value);                     
                }
                catch(ArgumentException)
                {                
                    // exception is thrown because value does not exist in the current cache, but it actually might do exist just because it is a very
                    // large multivalued attribute, the value has not been downloaded yet.
                    OnRemoveComplete(0, value);
                }
            }
            else
                List.Remove(value);                     
        }        
                                                  
        /// <include file='doc\PropertyValueCollection.uex' path='docs/doc[@for="PropertyValueCollection.OnClear"]/*' />
        ///<internalonly/>                           
        protected override void OnClearComplete() {
            if(needNewBehavior && !allowMultipleChange && updateType != UpdateType.None && updateType != UpdateType.Update)
            {
                throw new InvalidOperationException(Res.GetString(Res.DSPropertyValueSupportOneOperation));
            }            
            entry.AdsObject.PutEx((int) AdsPropertyOperation.Clear, propertyName, null);
            updateType = UpdateType.Update;
            try {
                entry.CommitIfNotCaching();
            }
            catch ( System.Runtime.InteropServices.COMException e ) {
                // On ADSI 2.5 if property has not been assigned any value before, 
                // then IAds::SetInfo() in CommitIfNotCaching returns bad HREsult 0x8007200A, which we ignore. 
                if ( e.ErrorCode != unchecked((int)0x8007200A) )    //  ERROR_DS_NO_ATTRIBUTE_OR_VALUE
                    throw;
            }
        }
        
        /// <include file='doc\PropertyValueCollection.uex' path='docs/doc[@for="PropertyValueCollection.OnInsert"]/*' />
        ///<internalonly/>
        protected override void OnInsertComplete(int index, object value) {        
            if(needNewBehavior)
            {
                if(!allowMultipleChange)
                {
                    if(updateType != UpdateType.None && updateType != UpdateType.Add)
                    {
                        throw new InvalidOperationException(Res.GetString(Res.DSPropertyValueSupportOneOperation));
                    }      
                    
                    changeList.Add(value);
                    
                    object[] allValues = new object[changeList.Count];
                    changeList.CopyTo(allValues, 0);
                    entry.AdsObject.PutEx((int) AdsPropertyOperation.Append, propertyName, allValues);

                    updateType = UpdateType.Add;
                }
                else
                {                
                    entry.AdsObject.PutEx((int) AdsPropertyOperation.Append, propertyName, new object[] {value});
                }
            }
            else
            {
                object[] allValues = new object[InnerList.Count];
                InnerList.CopyTo(allValues, 0);
                entry.AdsObject.PutEx((int) AdsPropertyOperation.Update, propertyName, allValues);
            }
            entry.CommitIfNotCaching();
        }
        
        /// <include file='doc\PropertyValueCollection.uex' path='docs/doc[@for="PropertyValueCollection.OnRemove"]/*' />
        ///<internalonly/>                          
        protected override void OnRemoveComplete(int index, object value) {
            if(needNewBehavior)
            {
                if(!allowMultipleChange)
                {
                    if(updateType != UpdateType.None && updateType != UpdateType.Delete)
                    {
                        throw new InvalidOperationException(Res.GetString(Res.DSPropertyValueSupportOneOperation));
                    } 

                    changeList.Add(value);
                    object[] allValues = new object[changeList.Count];
                    changeList.CopyTo(allValues, 0);
                    entry.AdsObject.PutEx((int) AdsPropertyOperation.Delete, propertyName, allValues);
     
                    updateType = UpdateType.Delete;
                }
                else
                {
                    entry.AdsObject.PutEx((int) AdsPropertyOperation.Delete, propertyName, new object[]{value});
                }
            }
            else
            {
                object[] allValues = new object[InnerList.Count];
                InnerList.CopyTo(allValues, 0);
                entry.AdsObject.PutEx((int) AdsPropertyOperation.Update, propertyName, allValues);
            }
            
            entry.CommitIfNotCaching();
        }
                 
        /// <include file='doc\PropertyValueCollection.uex' path='docs/doc[@for="PropertyValueCollection.OnSet"]/*' />
        ///<internalonly/>                          
        protected override void OnSetComplete(int index, object oldValue, object newValue) {
            // no need to consider the not allowing accumulative change case as it does not support Set
            if (Count <= 1) {
                entry.AdsObject.Put(propertyName, newValue);                
            }
            else {
                if(needNewBehavior)
                {
                    entry.AdsObject.PutEx((int) AdsPropertyOperation.Delete, propertyName, new object[] {oldValue});
                    entry.AdsObject.PutEx((int) AdsPropertyOperation.Append, propertyName, new object[] {newValue});
                }
                else
                {
                    object[] allValues = new object[InnerList.Count];
                    InnerList.CopyTo(allValues, 0);
                    entry.AdsObject.PutEx((int) AdsPropertyOperation.Update, propertyName, allValues);
                }
            }                
            
            entry.CommitIfNotCaching();                        
        }    
    }
}
