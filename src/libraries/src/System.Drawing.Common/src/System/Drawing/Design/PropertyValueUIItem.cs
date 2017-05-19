//------------------------------------------------------------------------------
// <copyright file="PropertyValueUIItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.Drawing.Design {

    using System.Diagnostics;

    using Microsoft.Win32;
    using System.Collections;
    using System.Drawing;
    
    /// <include file='doc\PropertyValueUIItem.uex' path='docs/doc[@for="PropertyValueUIItem"]/*' />
    /// <devdoc>
    ///    <para>Provides information about the property value UI including the invoke 
    ///       handler, tool tip, and the glyph icon to be displayed on the property
    ///       browser.</para>
    /// </devdoc>
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name="FullTrust")]
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name="FullTrust")]
    public class PropertyValueUIItem {
    
      /// <include file='doc\PropertyValueUIItem.uex' path='docs/doc[@for="PropertyValueUIItem.itemImage"]/*' />
      /// <devdoc>
      /// The image to display for this.  Must be 8x8
      /// </devdoc>
      private Image itemImage;
      
      /// <include file='doc\PropertyValueUIItem.uex' path='docs/doc[@for="PropertyValueUIItem.handler"]/*' />
      /// <devdoc>
      /// The handler to fire if this item is double clicked.
      /// </devdoc>
      private PropertyValueUIItemInvokeHandler handler;
      
      /// <include file='doc\PropertyValueUIItem.uex' path='docs/doc[@for="PropertyValueUIItem.tooltip"]/*' />
      /// <devdoc>
      /// The tooltip for this item.
      /// </devdoc>
      private string tooltip;
      
      /// <include file='doc\PropertyValueUIItem.uex' path='docs/doc[@for="PropertyValueUIItem.PropertyValueUIItem"]/*' />
      /// <devdoc>
      /// <para>Initiailzes a new instance of the <see cref='System.Drawing.Design.PropertyValueUIItem'/> class.</para>
      /// </devdoc>
      public PropertyValueUIItem(Image uiItemImage, PropertyValueUIItemInvokeHandler handler, string tooltip){
            this.itemImage = uiItemImage;
            this.handler = handler;
            if (itemImage == null) {
               throw new ArgumentNullException("uiItemImage");
            }
            if (handler == null) {
               throw new ArgumentNullException("handler");
            }
            this.tooltip = tooltip;
      }
      
      /// <include file='doc\PropertyValueUIItem.uex' path='docs/doc[@for="PropertyValueUIItem.Image"]/*' />
      /// <devdoc>
      ///    <para>Gets or sets
      ///       the 8x8 pixel image that will be drawn on the properties window.</para>
      /// </devdoc>
      public virtual Image Image {
          get {
            return itemImage;
          }
      }
      
      
      /// <include file='doc\PropertyValueUIItem.uex' path='docs/doc[@for="PropertyValueUIItem.InvokeHandler"]/*' />
      /// <devdoc>
      ///    <para>Gets or sets the handler that will be raised when this item is double clicked.</para>
      /// </devdoc>
      public virtual PropertyValueUIItemInvokeHandler InvokeHandler {
          get {
             return handler;
          }
      }
      
      /// <include file='doc\PropertyValueUIItem.uex' path='docs/doc[@for="PropertyValueUIItem.ToolTip"]/*' />
      /// <devdoc>
      ///    <para>Gets or sets the
      ///       tool tip to display for this item.</para>
      /// </devdoc>
      public virtual string ToolTip {
          get {
            return tooltip;
          }
      }
      
      /// <include file='doc\PropertyValueUIItem.uex' path='docs/doc[@for="PropertyValueUIItem.Reset"]/*' />
      /// <devdoc>
      ///    <para>Resets the UI item.</para>
      /// </devdoc>
      public virtual void Reset(){
      } 
    }

}
   
