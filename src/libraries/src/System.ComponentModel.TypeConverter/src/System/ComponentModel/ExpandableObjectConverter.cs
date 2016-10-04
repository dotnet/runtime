// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System.Collections;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Provides
    ///       a type converter to convert expandable objects to and from various
    ///       other representations.</para>
    /// </summary>
    public class ExpandableObjectConverter : TypeConverter
    {
        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the System.ComponentModel.ExpandableObjectConverter class.
        ///    </para>
        /// </summary>
        public ExpandableObjectConverter()
        {
        }


        /// <internalonly/>
        /// <summary>
        ///    <para>Gets a collection of properties for the type of object
        ///       specified by the value
        ///       parameter.</para>
        /// </summary>
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            return TypeDescriptor.GetProperties(value, attributes);
        }

        /// <internalonly/>
        /// <summary>
        ///    <para>Gets a value indicating
        ///       whether this object supports properties using the
        ///       specified context.</para>
        /// </summary>
        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
    }
}

