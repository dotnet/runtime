// ------------------------------------------------------------------------------
// <copyright file="ToolboxItemCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright> 
// ------------------------------------------------------------------------------
// 
namespace System.Drawing.Design {

    using System;
    using System.Collections;
    
    /// <include file='doc\ToolboxItemCollection.uex' path='docs/doc[@for="ToolboxItemCollection"]/*' />
    /// <devdoc>
    ///     <para>
    ///       A collection that stores <see cref='System.Drawing.Design.ToolboxItem'/> objects.
    ///    </para>
    /// </devdoc>
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name="FullTrust")]
    public sealed class ToolboxItemCollection : ReadOnlyCollectionBase {
        
        /// <include file='doc\ToolboxItemCollection.uex' path='docs/doc[@for="ToolboxItemCollection.ToolboxItemCollection"]/*' />
        /// <devdoc>
        ///     <para>
        ///       Initializes a new instance of <see cref='System.Drawing.Design.ToolboxItemCollection'/> based on another <see cref='System.Drawing.Design.ToolboxItemCollection'/>.
        ///    </para>
        /// </devdoc>
        public ToolboxItemCollection(ToolboxItemCollection value) {
            InnerList.AddRange(value);
        }
        
        /// <include file='doc\ToolboxItemCollection.uex' path='docs/doc[@for="ToolboxItemCollection.ToolboxItemCollection1"]/*' />
        /// <devdoc>
        ///     <para>
        ///       Initializes a new instance of <see cref='System.Drawing.Design.ToolboxItemCollection'/> containing any array of <see cref='System.Drawing.Design.ToolboxItem'/> objects.
        ///    </para>
        /// </devdoc>
        public ToolboxItemCollection(ToolboxItem[] value) {
            InnerList.AddRange(value);
        }
        
        /// <include file='doc\ToolboxItemCollection.uex' path='docs/doc[@for="ToolboxItemCollection.this"]/*' />
        /// <devdoc>
        /// <para>Represents the entry at the specified index of the <see cref='System.Drawing.Design.ToolboxItem'/>.</para>
        /// </devdoc>
        public ToolboxItem this[int index] {
            get {
                return ((ToolboxItem)(InnerList[index]));
            }
        }
        
        /// <include file='doc\ToolboxItemCollection.uex' path='docs/doc[@for="ToolboxItemCollection.Contains"]/*' />
        /// <devdoc>
        /// <para>Gets a value indicating whether the 
        ///    <see cref='System.Drawing.Design.ToolboxItemCollection'/> contains the specified <see cref='System.Drawing.Design.ToolboxItem'/>.</para>
        /// </devdoc>
        public bool Contains(ToolboxItem value) {
            return InnerList.Contains(value);
        }
        
        /// <include file='doc\ToolboxItemCollection.uex' path='docs/doc[@for="ToolboxItemCollection.CopyTo"]/*' />
        /// <devdoc>
        /// <para>Copies the <see cref='System.Drawing.Design.ToolboxItemCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the 
        ///    specified index.</para>
        /// </devdoc>
        public void CopyTo(ToolboxItem[] array, int index) {
            InnerList.CopyTo(array, index);
        }
        
        /// <include file='doc\ToolboxItemCollection.uex' path='docs/doc[@for="ToolboxItemCollection.IndexOf"]/*' />
        /// <devdoc>
        ///    <para>Returns the index of a <see cref='System.Drawing.Design.ToolboxItem'/> in 
        ///       the <see cref='System.Drawing.Design.ToolboxItemCollection'/> .</para>
        /// </devdoc>
        public int IndexOf(ToolboxItem value) {
            return InnerList.IndexOf(value);
        }
    }
}
