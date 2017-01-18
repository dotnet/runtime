
namespace System.DirectoryServices {
    using System;        
    using System.Security.Permissions;    
                                                                        
    /// <include file='doc\DirectoryServicesPermission.uex' path='docs/doc[@for="DirectoryServicesPermission"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [
    Serializable()
    ]
    public sealed class DirectoryServicesPermission : ResourcePermissionBase {    
        private DirectoryServicesPermissionEntryCollection innerCollection;
        
        /// <include file='doc\DirectoryServicesPermission.uex' path='docs/doc[@for="DirectoryServicesPermission.DirectoryServicesPermission"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public DirectoryServicesPermission() {
            SetNames();
        }                                                                
        
        /// <include file='doc\DirectoryServicesPermission.uex' path='docs/doc[@for="DirectoryServicesPermission.DirectoryServicesPermission1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public DirectoryServicesPermission(PermissionState state) 
        : base(state) {
            SetNames();
        }
        
        /// <include file='doc\DirectoryServicesPermission.uex' path='docs/doc[@for="DirectoryServicesPermission.DirectoryServicesPermission2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public DirectoryServicesPermission(DirectoryServicesPermissionAccess permissionAccess, string path) {            
            SetNames();
            this.AddPermissionAccess(new DirectoryServicesPermissionEntry(permissionAccess, path));              
        }         
         
        /// <include file='doc\DirectoryServicesPermission.uex' path='docs/doc[@for="DirectoryServicesPermission.DirectoryServicesPermission3"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public DirectoryServicesPermission(DirectoryServicesPermissionEntry[] permissionAccessEntries) {            
            if (permissionAccessEntries == null)
                throw new ArgumentNullException("permissionAccessEntries");
                
            SetNames();            
            for (int index = 0; index < permissionAccessEntries.Length; ++index)
                this.AddPermissionAccess(permissionAccessEntries[index]);                          
        }

        /// <include file='doc\DirectoryServicesPermission.uex' path='docs/doc[@for="DirectoryServicesPermission.PermissionEntries"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>                
        public DirectoryServicesPermissionEntryCollection PermissionEntries {
            get {
                if (this.innerCollection == null)                     
                    this.innerCollection = new DirectoryServicesPermissionEntryCollection(this, base.GetPermissionEntries()); 
                                                                           
                return this.innerCollection;                                                               
            }
        }

        /// <include file='doc\DirectoryServicesPermission.uex' path='docs/doc[@for="DirectoryServicesPermission.AddPermissionAccess"]/*' />                
        ///<internalonly/> 
        internal void AddPermissionAccess(DirectoryServicesPermissionEntry entry) {
            base.AddPermissionAccess(entry.GetBaseEntry());
        }
        
        /// <include file='doc\DirectoryServicesPermission.uex' path='docs/doc[@for="DirectoryServicesPermission.Clear"]/*' />                        
        ///<internalonly/> 
        internal new void Clear() {
            base.Clear();
        }

        /// <include file='doc\DirectoryServicesPermission.uex' path='docs/doc[@for="DirectoryServicesPermission.RemovePermissionAccess"]/*' />                                                  
        ///<internalonly/> 
        internal void RemovePermissionAccess(DirectoryServicesPermissionEntry entry) {
            base.RemovePermissionAccess(entry.GetBaseEntry());
        }
        
        private void SetNames() {
            this.PermissionAccessType = typeof(DirectoryServicesPermissionAccess);
            this.TagNames = new string[]{"Path"};
        }                
    }
}  

