// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Top level mapping layer between a COM object and TypeDescriptor.
    /// </summary>
    [Obsolete("This interface has been deprecated. Add a TypeDescriptionProvider to handle type TypeDescriptor.ComObjectType instead. https://go.microsoft.com/fwlink/?linkid=14202")]
    public interface IComNativeDescriptorHandler
    {
        AttributeCollection GetAttributes(object component);

        string GetClassName(object component);

        TypeConverter GetConverter(object component);

        EventDescriptor GetDefaultEvent(object component);

        PropertyDescriptor GetDefaultProperty(object component);

        object GetEditor(object component, Type baseEditorType);

        string GetName(object component);

        EventDescriptorCollection GetEvents(object component);

        EventDescriptorCollection GetEvents(object component, Attribute[] attributes);

        PropertyDescriptorCollection GetProperties(object component, Attribute[] attributes);

        object GetPropertyValue(object component, string propertyName, ref bool success);

        object GetPropertyValue(object component, int dispid, ref bool success);
    }
}
