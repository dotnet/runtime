// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace System.ComponentModel
{
    /// <internalonly/>
    /// <summary>
    ///    <para>
    ///       Top level mapping layer between a COM object and TypeDescriptor.
    ///    </para>
    /// </summary>
    [Obsolete("This interface has been deprecated. Add a TypeDescriptionProvider to handle type TypeDescriptor.ComObjectType instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
    public interface IComNativeDescriptorHandler
    {
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        AttributeCollection GetAttributes(object component);
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        string GetClassName(object component);
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        TypeConverter GetConverter(object component);
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        EventDescriptor GetDefaultEvent(object component);
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        PropertyDescriptor GetDefaultProperty(object component);
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        object GetEditor(object component, Type baseEditorType);
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        string GetName(object component);
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        EventDescriptorCollection GetEvents(object component);
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        EventDescriptorCollection GetEvents(object component, Attribute[] attributes);
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        PropertyDescriptorCollection GetProperties(object component, Attribute[] attributes);
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        object GetPropertyValue(object component, string propertyName, ref bool success);
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        object GetPropertyValue(object component, int dispid, ref bool success);
    }
}
