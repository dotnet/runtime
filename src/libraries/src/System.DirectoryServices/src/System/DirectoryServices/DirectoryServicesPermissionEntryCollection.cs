//----------------------------------------------------
// <copyright file="DirectoryServicesPermissionEntryCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices {
    using System.Security.Permissions;
    using System.Collections;

    /// <include file='doc\DirectoryServicesPermissionEntryCollection.uex' path='docs/doc[@for="DirectoryServicesPermissionEntryCollection"]/*' />        
    [
    Serializable()
    ]
    public class DirectoryServicesPermissionEntryCollection : CollectionBase {
        DirectoryServicesPermission owner;
        
        /// <include file='doc\DirectoryServicesPermissionEntryCollection.uex' path='docs/doc[@for="DirectoryServicesPermissionEntryCollection.DirectoryServicesPermissionEntryCollection"]/*' />        
        ///<internalonly/>   
        internal DirectoryServicesPermissionEntryCollection(DirectoryServicesPermission owner, ResourcePermissionBaseEntry[] entries) {
            this.owner = owner;
            for (int index = 0; index < entries.Length; ++index)
                this.InnerList.Add(new DirectoryServicesPermissionEntry(entries[index]));
        } 
        
        /// <include file='doc\DirectoryServicesPermissionEntryCollection.uex' path='docs/doc[@for="DirectoryServicesPermissionEntryCollection.DirectoryServicesPermissionEntryCollection"]/*' />                   
        internal DirectoryServicesPermissionEntryCollection() {
        }                                                                                                            
                                                                                                            
        /// <include file='doc\DirectoryServicesPermissionEntryCollection.uex' path='docs/doc[@for="DirectoryServicesPermissionEntryCollection.this"]/*' />        
        public DirectoryServicesPermissionEntry this[int index] {
            get {
                return (DirectoryServicesPermissionEntry)List[index];
            }
            set {
                List[index] = value;
            }
            
        }
        
        /// <include file='doc\DirectoryServicesPermissionEntryCollection.uex' path='docs/doc[@for="DirectoryServicesPermissionEntryCollection.Add"]/*' />
        public int Add(DirectoryServicesPermissionEntry value) {   
            return List.Add(value);
        }
        
        /// <include file='doc\DirectoryServicesPermissionEntryCollection.uex' path='docs/doc[@for="DirectoryServicesPermissionEntryCollection.AddRange"]/*' />
        public void AddRange(DirectoryServicesPermissionEntry[] value) {            
            if (value == null) {
                throw new ArgumentNullException("value");
            }
            for (int i = 0; ((i) < (value.Length)); i = ((i) + (1))) {
                this.Add(value[i]);
            }
        }
    
        /// <include file='doc\DirectoryServicesPermissionEntryCollection.uex' path='docs/doc[@for="DirectoryServicesPermissionEntryCollection.AddRange1"]/*' />
        public void AddRange(DirectoryServicesPermissionEntryCollection value) {            
            if (value == null) {
                throw new ArgumentNullException("value");
            }
            int currentCount = value.Count;
            for (int i = 0; i < currentCount; i = ((i) + (1))) {
                this.Add(value[i]);
            }
        }         
    
        /// <include file='doc\DirectoryServicesPermissionEntryCollection.uex' path='docs/doc[@for="DirectoryServicesPermissionEntryCollection.Contains"]/*' />
        public bool Contains(DirectoryServicesPermissionEntry value) {            
            return List.Contains(value);
        }
    
        /// <include file='doc\DirectoryServicesPermissionEntryCollection.uex' path='docs/doc[@for="DirectoryServicesPermissionEntryCollection.CopyTo"]/*' />
        public void CopyTo(DirectoryServicesPermissionEntry[] array, int index) {            
            List.CopyTo(array, index);
        }
    
        /// <include file='doc\DirectoryServicesPermissionEntryCollection.uex' path='docs/doc[@for="DirectoryServicesPermissionEntryCollection.IndexOf"]/*' />
        public int IndexOf(DirectoryServicesPermissionEntry value) {            
            return List.IndexOf(value);
        }
        
        /// <include file='doc\DirectoryServicesPermissionEntryCollection.uex' path='docs/doc[@for="DirectoryServicesPermissionEntryCollection.Insert"]/*' />
        public void Insert(int index, DirectoryServicesPermissionEntry value) {            
            List.Insert(index, value);
        }
                
        /// <include file='doc\DirectoryServicesPermissionEntryCollection.uex' path='docs/doc[@for="DirectoryServicesPermissionEntryCollection.Remove"]/*' />
        public void Remove(DirectoryServicesPermissionEntry value) {
            List.Remove(value);                     
        }
        
        /// <include file='doc\DirectoryServicesPermissionEntryCollection.uex' path='docs/doc[@for="DirectoryServicesPermissionEntryCollection.OnClear"]/*' />        
        ///<internalonly/>                          
        protected override void OnClear() {   
            this.owner.Clear();         
        }
        
        /// <include file='doc\DirectoryServicesPermissionEntryCollection.uex' path='docs/doc[@for="DirectoryServicesPermissionEntryCollection.OnInsert"]/*' />        
        ///<internalonly/>                          
        protected override void OnInsert(int index, object value) {        
            this.owner.AddPermissionAccess((DirectoryServicesPermissionEntry)value);
        }
        
        /// <include file='doc\DirectoryServicesPermissionEntryCollection.uex' path='docs/doc[@for="DirectoryServicesPermissionEntryCollection.OnRemove"]/*' />
        ///<internalonly/>                          
        protected override void OnRemove(int index, object value) {
            this.owner.RemovePermissionAccess((DirectoryServicesPermissionEntry)value);
        }
                 
        /// <include file='doc\DirectoryServicesPermissionEntryCollection.uex' path='docs/doc[@for="DirectoryServicesPermissionEntryCollection.OnSet"]/*' />
        ///<internalonly/>                          
        protected override void OnSet(int index, object oldValue, object newValue) {     
            this.owner.RemovePermissionAccess((DirectoryServicesPermissionEntry)oldValue);
            this.owner.AddPermissionAccess((DirectoryServicesPermissionEntry)newValue);       
        } 
    }
}

