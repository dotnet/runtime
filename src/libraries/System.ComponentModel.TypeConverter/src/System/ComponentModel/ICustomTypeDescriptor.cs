// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides an interface that provides custom type information for an object.
    /// </summary>
    public interface ICustomTypeDescriptor
    {
        /// <summary>
        /// Gets a collection of type <see cref='System.Attribute'/> with the attributes
        /// for this object.
        /// </summary>
        AttributeCollection GetAttributes();

        /// <summary>
        /// Gets the class name of this object.
        /// </summary>
        string? GetClassName();

        /// <summary>
        /// Gets the name of this object.
        /// </summary>
        string? GetComponentName();

        /// <summary>
        /// Gets a type converter for this object.
        /// </summary>
        [RequiresUnreferencedCode(TypeConverter.RequiresUnreferencedCodeMessage)]
        TypeConverter? GetConverter();

        /// <summary>
        /// Gets the default event for this object.
        /// </summary>
        [RequiresUnreferencedCode(EventDescriptor.RequiresUnreferencedCodeMessage)]
        EventDescriptor? GetDefaultEvent();

        /// <summary>
        /// Gets the default property for this object.
        /// </summary>
        [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage)]
        PropertyDescriptor? GetDefaultProperty();

        /// <summary>
        /// Gets an editor of the specified type for this object.
        /// </summary>
        [RequiresUnreferencedCode(TypeDescriptor.EditorRequiresUnreferencedCode)]
        object? GetEditor(Type editorBaseType);

        /// <summary>
        /// Gets the events for this instance of a component.
        /// </summary>
        EventDescriptorCollection GetEvents();

        /// <summary>
        /// Gets the events for this instance of a component using the attribute array as a
        /// filter.
        /// </summary>
        [RequiresUnreferencedCode(AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        EventDescriptorCollection GetEvents(Attribute[]? attributes);

        /// <summary>
        /// Gets the properties for this instance of a component.
        /// </summary>
        [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage)]
        PropertyDescriptorCollection GetProperties();

        /// <summary>
        /// Gets the properties for this instance of a component using the attribute array as a filter.
        /// </summary>
        [RequiresUnreferencedCode(PropertyDescriptor.PropertyDescriptorPropertyTypeMessage + " " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        PropertyDescriptorCollection GetProperties(Attribute[]? attributes);

        /// <summary>
        /// Gets the object that directly depends on this value being edited.
        /// </summary>
        object? GetPropertyOwner(PropertyDescriptor? pd);
    }
}
