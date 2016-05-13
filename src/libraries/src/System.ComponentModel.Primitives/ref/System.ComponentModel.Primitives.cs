// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.ComponentModel
{
    public sealed partial class BrowsableAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.BrowsableAttribute Default;
        public static readonly System.ComponentModel.BrowsableAttribute No;
        public static readonly System.ComponentModel.BrowsableAttribute Yes;
        public BrowsableAttribute(bool browsable) { }
        public bool Browsable { get; }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
    }
    public partial class CategoryAttribute : System.Attribute
    {
        public CategoryAttribute() { }
        public CategoryAttribute(string category) { }
        public static System.ComponentModel.CategoryAttribute Action { get; }
        public static System.ComponentModel.CategoryAttribute Appearance { get; }
        public static System.ComponentModel.CategoryAttribute Asynchronous { get; }
        public static System.ComponentModel.CategoryAttribute Behavior { get; }
        public string Category { get; }
        public static System.ComponentModel.CategoryAttribute Data { get; }
        public static System.ComponentModel.CategoryAttribute Default { get; }
        public static System.ComponentModel.CategoryAttribute Design { get; }
        public static System.ComponentModel.CategoryAttribute DragDrop { get; }
        public static System.ComponentModel.CategoryAttribute Focus { get; }
        public static System.ComponentModel.CategoryAttribute Format { get; }
        public static System.ComponentModel.CategoryAttribute Key { get; }
        public static System.ComponentModel.CategoryAttribute Layout { get; }
        public static System.ComponentModel.CategoryAttribute Mouse { get; }
        public static System.ComponentModel.CategoryAttribute WindowStyle { get; }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        protected virtual string GetLocalizedString(string value) { return default(string); }
    }
    public partial class ComponentCollection
    {
        internal ComponentCollection() { }
    }
    public partial class DescriptionAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.DescriptionAttribute Default;
        public DescriptionAttribute() { }
        public DescriptionAttribute(string description) { }
        public virtual string Description { get; }
        protected string DescriptionValue { get; set; }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
    }
    public sealed partial class DesignerCategoryAttribute : Attribute
    {
        public static readonly DesignerCategoryAttribute Component;
        public static readonly DesignerCategoryAttribute Default;
        public static readonly DesignerCategoryAttribute Form;
        public static readonly DesignerCategoryAttribute Generic;
        public DesignerCategoryAttribute() { }
        public DesignerCategoryAttribute(string category) { }
        public string Category { get; }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
    }
    public enum DesignerSerializationVisibility
    {
        Content = 2,
        Hidden = 0,
        Visible = 1,
    }
    public sealed partial class DesignerSerializationVisibilityAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.DesignerSerializationVisibilityAttribute Content;
        public static readonly System.ComponentModel.DesignerSerializationVisibilityAttribute Default;
        public static readonly System.ComponentModel.DesignerSerializationVisibilityAttribute Hidden;
        public static readonly System.ComponentModel.DesignerSerializationVisibilityAttribute Visible;
        public DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility visibility) { }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public System.ComponentModel.DesignerSerializationVisibility Visibility { get; }
    }
    public sealed partial class DesignOnlyAttribute : System.Attribute
    {
        public static readonly DesignOnlyAttribute Default;
        public static readonly DesignOnlyAttribute No;
        public static readonly DesignOnlyAttribute Yes;
        public DesignOnlyAttribute(bool isDesignOnly) { }
        public bool IsDesignOnly { get; }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
    }
    public partial class DisplayNameAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.DisplayNameAttribute Default;
        public DisplayNameAttribute() { }
        public DisplayNameAttribute(string displayName) { }
        public virtual string DisplayName { get; }
        protected string DisplayNameValue { get; set; }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
    }
    public sealed partial class EventHandlerList : System.IDisposable
    {
        public EventHandlerList() { }
        public System.Delegate this[object key] { get { return default(System.Delegate); } set { } }
        public void AddHandler(object key, System.Delegate value) { }
        public void AddHandlers(System.ComponentModel.EventHandlerList listToAddFrom) { }
        public void Dispose() { }
        public void RemoveHandler(object key, System.Delegate value) { }
    }
    public partial interface IComponent : System.IDisposable
    {
        System.ComponentModel.ISite Site { get; set; }
        event System.EventHandler Disposed;
    }
    public partial interface IContainer : System.IDisposable
    {
        System.ComponentModel.ComponentCollection Components { get; }
        void Add(System.ComponentModel.IComponent component);
        void Add(System.ComponentModel.IComponent component, string name);
        void Remove(System.ComponentModel.IComponent component);
    }
    public sealed partial class ImmutableObjectAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.ImmutableObjectAttribute Default;
        public static readonly System.ComponentModel.ImmutableObjectAttribute No;
        public static readonly System.ComponentModel.ImmutableObjectAttribute Yes;
        public ImmutableObjectAttribute(bool immutable) { }
        public bool Immutable { get; }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
    }
    public sealed partial class InitializationEventAttribute : System.Attribute
    {
        public InitializationEventAttribute(string eventName) { }
        public string EventName { get; }
    }
    public partial interface ISite : System.IServiceProvider
    {
        System.ComponentModel.IComponent Component { get; }
        System.ComponentModel.IContainer Container { get; }
        bool DesignMode { get; }
        string Name { get; set; }
    }
    public sealed partial class LocalizableAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.LocalizableAttribute Default;
        public static readonly System.ComponentModel.LocalizableAttribute No;
        public static readonly System.ComponentModel.LocalizableAttribute Yes;
        public LocalizableAttribute(bool isLocalizable) { }
        public bool IsLocalizable { get; }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
    }
    public sealed partial class MergablePropertyAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.MergablePropertyAttribute Default;
        public static readonly System.ComponentModel.MergablePropertyAttribute No;
        public static readonly System.ComponentModel.MergablePropertyAttribute Yes;
        public MergablePropertyAttribute(bool allowMerge) { }
        public bool AllowMerge { get; }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
    }
    public sealed partial class NotifyParentPropertyAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.NotifyParentPropertyAttribute Default;
        public static readonly System.ComponentModel.NotifyParentPropertyAttribute No;
        public static readonly System.ComponentModel.NotifyParentPropertyAttribute Yes;
        public NotifyParentPropertyAttribute(bool notifyParent) { }
        public bool NotifyParent { get; }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
    }
    public sealed partial class ParenthesizePropertyNameAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.ParenthesizePropertyNameAttribute Default;
        public ParenthesizePropertyNameAttribute() { }
        public ParenthesizePropertyNameAttribute(bool needParenthesis) { }
        public bool NeedParenthesis { get; }
        public override bool Equals(object o) { return default(bool); }
        public override int GetHashCode() { return default(int); }
    }
    public sealed partial class ReadOnlyAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.ReadOnlyAttribute Default;
        public static readonly System.ComponentModel.ReadOnlyAttribute No;
        public static readonly System.ComponentModel.ReadOnlyAttribute Yes;
        public ReadOnlyAttribute(bool isReadOnly) { }
        public bool IsReadOnly { get; }
        public override bool Equals(object value) { return default(bool); }
        public override int GetHashCode() { return default(int); }
    }
    public enum RefreshProperties
    {
        All = 1,
        None = 0,
        Repaint = 2,
    }
    public sealed partial class RefreshPropertiesAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.RefreshPropertiesAttribute All;
        public static readonly System.ComponentModel.RefreshPropertiesAttribute Default;
        public static readonly System.ComponentModel.RefreshPropertiesAttribute Repaint;
        public RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties refresh) { }
        public System.ComponentModel.RefreshProperties RefreshProperties { get; }
        public override bool Equals(object value) { return default(bool); }
        public override int GetHashCode() { return default(int); }
    }
}
