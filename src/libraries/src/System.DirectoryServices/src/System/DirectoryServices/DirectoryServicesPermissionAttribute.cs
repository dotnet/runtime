
namespace System.DirectoryServices {  
    using System.Security;  
    using System.Security.Permissions;    
    
    /// <include file='doc\DirectoryServicesPermissionAttribute.uex' path='docs/doc[@for="DirectoryServicesPermissionAttribute"]/*' />
    [
    AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly | AttributeTargets.Event, AllowMultiple = true, Inherited = false ),
    Serializable()
    ]     
    public class DirectoryServicesPermissionAttribute : CodeAccessSecurityAttribute {
        private string path;
        private DirectoryServicesPermissionAccess permissionAccess;
                                        
        /// <include file='doc\DirectoryServicesPermissionAttribute.uex' path='docs/doc[@for="DirectoryServicesPermissionAttribute.DirectoryServicesPermissionAttribute"]/*' />
        public DirectoryServicesPermissionAttribute(SecurityAction action)
        : base(action) {
            this.path = "*";
            this.permissionAccess = DirectoryServicesPermissionAccess.Browse;            
        }

        /// <include file='doc\DirectoryServicesPermissionAttribute.uex' path='docs/doc[@for="DirectoryServicesPermissionAttribute.Path"]/*' />
        public string Path {
            get {
                return this.path;
            }
            
            set {
                if (value == null)
                    throw new ArgumentNullException("value");
                
                this.path = value;                    
            }
        }
        
        /// <include file='doc\DirectoryServicesPermissionAttribute.uex' path='docs/doc[@for="DirectoryServicesPermissionAttribute.PermissionAccess"]/*' />
        public DirectoryServicesPermissionAccess PermissionAccess {
            get {
                return this.permissionAccess;
            }
            
            set {
                this.permissionAccess = value;
            }
        }                          
              
        /// <include file='doc\DirectoryServicesPermissionAttribute.uex' path='docs/doc[@for="DirectoryServicesPermissionAttribute.CreatePermission"]/*' />
        public override IPermission CreatePermission() {            
            if (Unrestricted) 
                return new DirectoryServicesPermission(PermissionState.Unrestricted);
                        
            DirectoryServicesPermissionAccess tmpAccess = this.permissionAccess;
            string tmpPath = this.Path;
            return new DirectoryServicesPermission(tmpAccess, tmpPath);
            
        }
    }    
}

