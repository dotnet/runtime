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
    public partial interface IToolboxItemProvider
    {
        System.Drawing.Design.ToolboxItemCollection Items { get; }
    }
    public partial interface IToolboxService
    {
        System.Drawing.Design.CategoryNameCollection CategoryNames { get; }
        string SelectedCategory { get; set; }
        void AddCreator(System.Drawing.Design.ToolboxItemCreatorCallback creator, string format);
        void AddCreator(System.Drawing.Design.ToolboxItemCreatorCallback creator, string format, System.ComponentModel.Design.IDesignerHost host);
        void AddLinkedToolboxItem(System.Drawing.Design.ToolboxItem toolboxItem, System.ComponentModel.Design.IDesignerHost host);
        void AddLinkedToolboxItem(System.Drawing.Design.ToolboxItem toolboxItem, string category, System.ComponentModel.Design.IDesignerHost host);
        void AddToolboxItem(System.Drawing.Design.ToolboxItem toolboxItem);
        void AddToolboxItem(System.Drawing.Design.ToolboxItem toolboxItem, string category);
        System.Drawing.Design.ToolboxItem DeserializeToolboxItem(object serializedObject);
        System.Drawing.Design.ToolboxItem DeserializeToolboxItem(object serializedObject, System.ComponentModel.Design.IDesignerHost host);
        System.Drawing.Design.ToolboxItem GetSelectedToolboxItem();
        System.Drawing.Design.ToolboxItem GetSelectedToolboxItem(System.ComponentModel.Design.IDesignerHost host);
        System.Drawing.Design.ToolboxItemCollection GetToolboxItems();
        System.Drawing.Design.ToolboxItemCollection GetToolboxItems(System.ComponentModel.Design.IDesignerHost host);
        System.Drawing.Design.ToolboxItemCollection GetToolboxItems(string category);
        System.Drawing.Design.ToolboxItemCollection GetToolboxItems(string category, System.ComponentModel.Design.IDesignerHost host);
        bool IsSupported(object serializedObject, System.Collections.ICollection filterAttributes);
        bool IsSupported(object serializedObject, System.ComponentModel.Design.IDesignerHost host);
        bool IsToolboxItem(object serializedObject);
        bool IsToolboxItem(object serializedObject, System.ComponentModel.Design.IDesignerHost host);
        void Refresh();
        void RemoveCreator(string format);
        void RemoveCreator(string format, System.ComponentModel.Design.IDesignerHost host);
        void RemoveToolboxItem(System.Drawing.Design.ToolboxItem toolboxItem);
        void RemoveToolboxItem(System.Drawing.Design.ToolboxItem toolboxItem, string category);
        void SelectedToolboxItemUsed();
        object SerializeToolboxItem(System.Drawing.Design.ToolboxItem toolboxItem);
        bool SetCursor();
        void SetSelectedToolboxItem(System.Drawing.Design.ToolboxItem toolboxItem);
    }
    public partial interface IToolboxUser
    {
        bool GetToolSupported(System.Drawing.Design.ToolboxItem tool);
        void ToolPicked(System.Drawing.Design.ToolboxItem tool);
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
    public partial class ToolboxComponentsCreatedEventArgs : System.EventArgs
    {
        public ToolboxComponentsCreatedEventArgs(System.ComponentModel.IComponent[] components) { }
        public System.ComponentModel.IComponent[] Components { get { throw null; } }
    }
    public delegate void ToolboxComponentsCreatedEventHandler(object sender, System.Drawing.Design.ToolboxComponentsCreatedEventArgs e);
    public partial class ToolboxComponentsCreatingEventArgs : System.EventArgs
    {
        public ToolboxComponentsCreatingEventArgs(System.ComponentModel.Design.IDesignerHost host) { }
        public System.ComponentModel.Design.IDesignerHost DesignerHost { get { throw null; } }
    }
    public delegate void ToolboxComponentsCreatingEventHandler(object sender, System.Drawing.Design.ToolboxComponentsCreatingEventArgs e);
    public partial class ToolboxItem : System.Runtime.Serialization.ISerializable
    {
        public ToolboxItem() { }
        protected ToolboxItem(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ToolboxItem(System.Type toolType) { }
        public System.Reflection.AssemblyName AssemblyName { get { throw null; } set { } }
        public System.Drawing.Bitmap Bitmap { get { throw null; } set { } }
        public string Company { get { throw null; } set { } }
        public virtual string ComponentType { get { throw null; } }
        public System.Reflection.AssemblyName[] DependentAssemblies { get { throw null; } set { } }
        public string Description { get { throw null; } set { } }
        public string DisplayName { get { throw null; } set { } }
        public System.Collections.ICollection Filter { get { throw null; } set { } }
        public bool IsTransient { get { throw null; } set { } }
        public virtual bool Locked { get { throw null; } }
        public System.Drawing.Bitmap OriginalBitmap { get { throw null; } set { } }
        public System.Collections.IDictionary Properties { get { throw null; } }
        public string TypeName { get { throw null; } set { } }
        public virtual string Version { get { throw null; } }
        public event System.Drawing.Design.ToolboxComponentsCreatedEventHandler ComponentsCreated { add { } remove { } }
        public event System.Drawing.Design.ToolboxComponentsCreatingEventHandler ComponentsCreating { add { } remove { } }
        protected void CheckUnlocked() { }
        public System.ComponentModel.IComponent[] CreateComponents() { throw null; }
        public System.ComponentModel.IComponent[] CreateComponents(System.ComponentModel.Design.IDesignerHost host) { throw null; }
        public System.ComponentModel.IComponent[] CreateComponents(System.ComponentModel.Design.IDesignerHost host, System.Collections.IDictionary defaultValues) { throw null; }
        protected virtual System.ComponentModel.IComponent[] CreateComponentsCore(System.ComponentModel.Design.IDesignerHost host) { throw null; }
        protected virtual System.ComponentModel.IComponent[] CreateComponentsCore(System.ComponentModel.Design.IDesignerHost host, System.Collections.IDictionary defaultValues) { throw null; }
        public override bool Equals(object obj) { throw null; }
        protected virtual object FilterPropertyValue(string propertyName, object value) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.Type GetType(System.ComponentModel.Design.IDesignerHost host) { throw null; }
        protected virtual System.Type GetType(System.ComponentModel.Design.IDesignerHost host, System.Reflection.AssemblyName assemblyName, string typeName, bool reference) { throw null; }
        public virtual void Initialize(System.Type type) { }
        public virtual void Lock() { }
        protected virtual void OnComponentsCreated(System.Drawing.Design.ToolboxComponentsCreatedEventArgs args) { }
        protected virtual void OnComponentsCreating(System.Drawing.Design.ToolboxComponentsCreatingEventArgs args) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { throw null; }
        protected void ValidatePropertyType(string propertyName, object value, System.Type expectedType, bool allowNull) { }
        protected virtual object ValidatePropertyValue(string propertyName, object value) { throw null; }
    }
    public sealed partial class ToolboxItemCollection : System.Collections.ReadOnlyCollectionBase
    {
        public ToolboxItemCollection(System.Drawing.Design.ToolboxItemCollection value) { }
        public ToolboxItemCollection(System.Drawing.Design.ToolboxItem[] value) { }
        public System.Drawing.Design.ToolboxItem this[int index] { get { throw null; } }
        public bool Contains(System.Drawing.Design.ToolboxItem value) { throw null; }
        public void CopyTo(System.Drawing.Design.ToolboxItem[] array, int index) { }
        public int IndexOf(System.Drawing.Design.ToolboxItem value) { throw null; }
    }
    public delegate System.Drawing.Design.ToolboxItem ToolboxItemCreatorCallback(object serializedObject, string format);
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
