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
        [RequiresUnreferencedCode(TypeDescriptor.DesignTimeAttributeTrimmed)]
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

        /// <summary>
        /// Gets a type converter for this object that may be registered.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = TypeDescriptionProvider.ForwardFromRegisteredMessage)]
        TypeConverter? GetConverterFromRegisteredType()
        {
            if (RequireRegisteredTypes is null)
            {
                if (TypeDescriptor.RequireRegisteredTypes)
                {
                    TypeDescriptor.ThrowHelper.ThrowNotImplementedException_CustomTypeProviderMustImplememtMember(nameof(GetConverterFromRegisteredType));
                }
            }
            else if (RequireRegisteredTypes == true)
            {
                TypeDescriptor.ThrowHelper.ThrowNotImplementedException_CustomTypeProviderMustImplememtMember(nameof(GetConverterFromRegisteredType));
            }

            return GetConverter();
        }

        /// <summary>
        /// Gets the events for this instance of a component that may be registered.
        /// </summary>
        EventDescriptorCollection GetEventsFromRegisteredType()
        {
            if (RequireRegisteredTypes is null)
            {
                if (TypeDescriptor.RequireRegisteredTypes)
                {
                    TypeDescriptor.ThrowHelper.ThrowNotImplementedException_CustomTypeProviderMustImplememtMember(nameof(GetEventsFromRegisteredType));
                }
            }
            else if (RequireRegisteredTypes == true)
            {
                TypeDescriptor.ThrowHelper.ThrowNotImplementedException_CustomTypeProviderMustImplememtMember(nameof(GetEventsFromRegisteredType));
            }

            return GetEvents();
        }

        /// <summary>
        /// Gets the properties for this instance of a component that may be registered.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = TypeDescriptionProvider.ForwardFromRegisteredMessage)]
        PropertyDescriptorCollection GetPropertiesFromRegisteredType()
        {
            if (RequireRegisteredTypes is null)
            {
                if (TypeDescriptor.RequireRegisteredTypes)
                {
                    TypeDescriptor.ThrowHelper.ThrowNotImplementedException_CustomTypeProviderMustImplememtMember(nameof(GetPropertiesFromRegisteredType));
                }
            }
            else if (RequireRegisteredTypes == true)
            {
                TypeDescriptor.ThrowHelper.ThrowNotImplementedException_CustomTypeProviderMustImplememtMember(nameof(GetPropertiesFromRegisteredType));
            }

            return GetProperties();
        }

        /// <summary>
        /// Whether types are required to be registered through <see cref="TypeDescriptionProvider.RegisterType{T}"/>.
        /// </summary>
        /// <remarks>
        /// The default value is <see langword="null"/> which means that the type descriptor has not declared whether or not it is compatible registered types.
        /// A type descriptor needs to implement this to return either <see langword="true"/> or <see langword="false"/> if the feature switch
        /// 'System.ComponentModel.TypeDescriptor.RequireRegisteredTypes' is enabled.
        /// If <see langword="true"/> is returned, then the type descriptor must also implement
        /// <see cref="ICustomTypeDescriptor.GetConverterFromRegisteredType()"/>,
        /// <see cref="ICustomTypeDescriptor.GetEventsFromRegisteredType()"/>, and
        /// <see cref="ICustomTypeDescriptor.GetPropertiesFromRegisteredType()"/>.
        /// <br/>
        /// </remarks>
        bool? RequireRegisteredTypes => null;
    }
}
