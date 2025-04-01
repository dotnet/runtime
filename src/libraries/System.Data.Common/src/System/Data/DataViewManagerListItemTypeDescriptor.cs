// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    internal sealed class DataViewManagerListItemTypeDescriptor : ICustomTypeDescriptor
    {
        private readonly DataViewManager _dataViewManager;
        private PropertyDescriptorCollection? _propsCollection;

        internal DataViewManagerListItemTypeDescriptor(DataViewManager dataViewManager)
        {
            _dataViewManager = dataViewManager;
        }

        internal void Reset()
        {
            _propsCollection = null;
        }

        internal DataView GetDataView(DataTable table)
        {
            DataView dataView = new DataView(table);
            dataView.SetDataViewManager(_dataViewManager);
            return dataView;
        }

        /// <summary>
        /// Retrieves an array of member attributes for the given object.
        /// </summary>
        AttributeCollection ICustomTypeDescriptor.GetAttributes() => new AttributeCollection(null);

        /// <summary>
        /// Retrieves the class name for this object.  If null is returned,
        /// the type name is used.
        /// </summary>
        string? ICustomTypeDescriptor.GetClassName() => null;

        /// <summary>
        /// Retrieves the name for this object.  If null is returned,
        /// the default is used.
        /// </summary>
        string? ICustomTypeDescriptor.GetComponentName() => null;

        /// <summary>
        /// Retrieves the type converter for this object.
        /// </summary>
        [RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
        TypeConverter? ICustomTypeDescriptor.GetConverter() => null;

        /// <summary>
        /// Retrieves the default event.
        /// </summary>
        [RequiresUnreferencedCode("The built-in EventDescriptor implementation uses Reflection which requires unreferenced code.")]
        EventDescriptor? ICustomTypeDescriptor.GetDefaultEvent() => null;

        /// <summary>
        /// Retrieves the default property.
        /// </summary>
        [RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
        PropertyDescriptor? ICustomTypeDescriptor.GetDefaultProperty() => null;

        /// <summary>
        /// Retrieves the an editor for this object.
        /// </summary>
        [RequiresUnreferencedCode("Design-time attributes are not preserved when trimming. Types referenced by attributes like EditorAttribute and DesignerAttribute may not be available after trimming.")]
        object? ICustomTypeDescriptor.GetEditor(Type editorBaseType) => null;

        /// <summary>
        /// Retrieves an array of events that the given component instance
        /// provides.  This may differ from the set of events the class
        /// provides.  If the component is sited, the site may add or remove
        /// additional events.
        /// </summary>
        EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => new EventDescriptorCollection(null);

        /// <summary>
        /// Retrieves an array of events that the given component instance
        /// provides.  This may differ from the set of events the class
        /// provides.  If the component is sited, the site may add or remove
        /// additional events.  The returned array of events will be
        /// filtered by the given set of attributes.
        /// </summary>
        [RequiresUnreferencedCode("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[]? attributes) =>
            new EventDescriptorCollection(null);

        /// <summary>
        ///     Retrieves an array of properties that the given component instance
        ///     provides.  This may differ from the set of properties the class
        ///     provides.  If the component is sited, the site may add or remove
        ///     additional properties.
        /// </summary>
        [RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() =>
            GetPropertiesInternal();

        /// <summary>
        ///     Retrieves an array of properties that the given component instance
        ///     provides.  This may differ from the set of properties the class
        ///     provides.  If the component is sited, the site may add or remove
        ///     additional properties.  The returned array of properties will be
        ///     filtered by the given set of attributes.
        /// </summary>
        [RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[]? attributes) =>
            GetPropertiesInternal();

        internal PropertyDescriptorCollection GetPropertiesInternal()
        {
            if (_propsCollection == null)
            {
                PropertyDescriptor[]? props = null;
                DataSet? dataSet = _dataViewManager.DataSet;
                if (dataSet != null)
                {
                    int tableCount = dataSet.Tables.Count;
                    props = new PropertyDescriptor[tableCount];
                    for (int i = 0; i < tableCount; i++)
                    {
                        props[i] = new DataTablePropertyDescriptor(dataSet.Tables[i]);
                    }
                }
                _propsCollection = new PropertyDescriptorCollection(props);
            }
            return _propsCollection;
        }

        /// <summary>
        ///     Retrieves the object that directly depends on this value being edited.  This is
        ///     generally the object that is required for the PropertyDescriptor's GetValue and SetValue
        ///     methods.  If 'null' is passed for the PropertyDescriptor, the ICustomComponent
        ///     descriptor implementation should return the default object, that is the main
        ///     object that exposes the properties and attributes,
        /// </summary>
        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor? pd) => this;
    }
}
