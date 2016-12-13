
namespace System.DirectoryServices {

    /// <include file='doc\DirectoryServicesPermissionAccess.uex' path='docs/doc[@for="DirectoryServicesPermissionAccess"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [Flags]         
    public enum DirectoryServicesPermissionAccess {
        /// <include file='doc\DirectoryServicesPermissionAccess.uex' path='docs/doc[@for="DirectoryServicesPermissionAccess.None"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        None = 0,
        /// <include file='doc\DirectoryServicesPermissionAccess.uex' path='docs/doc[@for="DirectoryServicesPermissionAccess.Browse"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Browse = 1 << 1,
        /// <include file='doc\DirectoryServicesPermissionAccess.uex' path='docs/doc[@for="DirectoryServicesPermissionAccess.Write"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Write = 1 << 2 | Browse,          
    }    
}  
  

