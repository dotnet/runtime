// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Drawing.Design
{
    public partial interface IPropertyValueUIService
    {
        event System.EventHandler PropertyUIValueItemsChanged;
        void AddPropertyValueUIHandler(System.Drawing.Design.PropertyValueUIHandler newHandler);
        System.Drawing.Design.PropertyValueUIItem[] GetPropertyUIValueItems(System.ComponentModel.ITypeDescriptorContext context, System.ComponentModel.PropertyDescriptor propDesc);
        void NotifyPropertyValueUIItemsChanged();
        void RemovePropertyValueUIHandler(System.Drawing.Design.PropertyValueUIHandler newHandler);
    }
    public partial class PaintValueEventArgs : System.EventArgs
    {
        public PaintValueEventArgs(System.ComponentModel.ITypeDescriptorContext context, object value, System.Drawing.Graphics graphics, System.Drawing.Rectangle bounds) { }
        public System.Drawing.Rectangle Bounds { get { throw null; } }
        public System.ComponentModel.ITypeDescriptorContext Context { get { throw null; } }
        public System.Drawing.Graphics Graphics { get { throw null; } }
        public object Value { get { throw null; } }
    }
    public delegate void PropertyValueUIHandler(System.ComponentModel.ITypeDescriptorContext context, System.ComponentModel.PropertyDescriptor propDesc, System.Collections.ArrayList valueUIItemList);
    public partial class PropertyValueUIItem
    {
        public PropertyValueUIItem(System.Drawing.Image uiItemImage, System.Drawing.Design.PropertyValueUIItemInvokeHandler handler, string tooltip) { }
        public virtual System.Drawing.Image Image { get { throw null; } }
        public virtual System.Drawing.Design.PropertyValueUIItemInvokeHandler InvokeHandler { get { throw null; } }
        public virtual string ToolTip { get { throw null; } }
        public virtual void Reset() { }
    }
    public delegate void PropertyValueUIItemInvokeHandler(System.ComponentModel.ITypeDescriptorContext context, System.ComponentModel.PropertyDescriptor descriptor, System.Drawing.Design.PropertyValueUIItem invokedItem);
    public partial class UITypeEditor
    {
        public UITypeEditor() { }
        public virtual bool IsDropDownResizable { get { throw null; } }
        public virtual object EditValue(System.ComponentModel.ITypeDescriptorContext context, System.IServiceProvider provider, object value) { throw null; }
        public object EditValue(System.IServiceProvider provider, object value) { throw null; }
        public System.Drawing.Design.UITypeEditorEditStyle GetEditStyle() { throw null; }
        public virtual System.Drawing.Design.UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
        public bool GetPaintValueSupported() { throw null; }
        public virtual bool GetPaintValueSupported(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
        public virtual void PaintValue(System.Drawing.Design.PaintValueEventArgs e) { }
        public void PaintValue(object value, System.Drawing.Graphics canvas, System.Drawing.Rectangle rectangle) { }
    }
    public enum UITypeEditorEditStyle
    {
        DropDown = 3,
        Modal = 2,
        None = 1,
    }
}
