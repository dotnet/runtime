//----------------------------------------------------
// <copyright file="DirectoryServicesPermissionEntry.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices {
    using System.Security.Permissions;
    
    /// <include file='doc\DirectoryServicesPermissionEntry.uex' path='docs/doc[@for="DirectoryServicesPermissionEntry"]/*' />
    [
        Serializable()
    ] 
    public class DirectoryServicesPermissionEntry {
        private string path;
        private DirectoryServicesPermissionAccess permissionAccess;
            
        /// <include file='doc\DirectoryServicesPermissionEntry.uex' path='docs/doc[@for="DirectoryServicesPermissionEntry.DirectoryServicesPermissionEntry"]/*' />
        public DirectoryServicesPermissionEntry(DirectoryServicesPermissionAccess permissionAccess, string path) {
            if (path == null)
                throw new ArgumentNullException("path");
                    
            this.permissionAccess = permissionAccess;
            this.path = path;
        }  
        
        /// <include file='doc\DirectoryServicesPermissionEntry.uex' path='docs/doc[@for="DirectoryServicesPermissionEntry.DirectoryServicesPermissionEntry1"]/*' />                                                                                                                                 
        ///<internalonly/> 
        internal DirectoryServicesPermissionEntry(ResourcePermissionBaseEntry baseEntry) {
            this.permissionAccess = (DirectoryServicesPermissionAccess)baseEntry.PermissionAccess;
            this.path = baseEntry.PermissionAccessPath[0]; 
        }

        
        /// <include file='doc\DirectoryServicesPermissionEntry.uex' path='docs/doc[@for="DirectoryServicesPermissionEntry.Path"]/*' />
        public string Path {
            get {
                return this.path;
            }                        
        }
        
        /// <include file='doc\DirectoryServicesPermissionEntry.uex' path='docs/doc[@for="DirectoryServicesPermissionEntry.PermissionAccess"]/*' />
        public DirectoryServicesPermissionAccess PermissionAccess {
            get {
                return this.permissionAccess;
            }                        
        }     
        
        /// <include file='doc\DirectoryServicesPermissionEntry.uex' path='docs/doc[@for="DirectoryServicesPermissionEntry.GetBaseEntry"]/*' />                                                                                                                                 
        ///<internalonly/> 
        internal ResourcePermissionBaseEntry GetBaseEntry() {
            ResourcePermissionBaseEntry baseEntry = new ResourcePermissionBaseEntry((int)this.PermissionAccess, new string[] {this.Path});            
            return baseEntry;
        }                        
    }        
}   

