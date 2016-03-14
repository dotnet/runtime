// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System.Collections;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Serialization.Formatters;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Provides a type converter to convert object references to and from various
    ///       other representations.</para>
    /// </devdoc>
    [HostProtection(SharedState = true)]
    public class ReferenceConverter : TypeConverter
    {
        private static readonly string s_none = SR.GetString(SR.toStringNone);
        private Type _type;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.ReferenceConverter'/> class.
        ///    </para>
        /// </devdoc>
        public ReferenceConverter(Type type)
        {
            _type = type;
        }

        /// <internalonly/>
        /// <devdoc>
        ///    <para>Gets a value indicating whether this converter can convert an object in the
        ///       given source type to a reference object using the specified context.</para>
        /// </devdoc>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string) && context != null)
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        /// <internalonly/>
        /// <devdoc>
        ///    <para>Converts the given object to the reference type.</para>
        /// </devdoc>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                string text = ((string)value).Trim();

                if (!String.Equals(text, s_none) && context != null)
                {
                    // Try the reference service first.
                    //
                    IReferenceService refSvc = (IReferenceService)context.GetService(typeof(IReferenceService));
                    if (refSvc != null)
                    {
                        object obj = refSvc.GetReference(text);
                        if (obj != null)
                        {
                            return obj;
                        }
                    }

                    // Now try IContainer
                    //
                    IContainer cont = context.Container;
                    if (cont != null)
                    {
                        object obj = cont.Components[text];
                        if (obj != null)
                        {
                            return obj;
                        }
                    }
                }
                return null;
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <internalonly/>
        /// <devdoc>
        ///    <para>Converts the given value object to the reference type
        ///       using the specified context and arguments.</para>
        /// </devdoc>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }

            if (destinationType == typeof(string))
            {
                if (value != null)
                {
                    // Try the reference service first.
                    //
                    if (context != null)
                    {
                        IReferenceService refSvc = (IReferenceService)context.GetService(typeof(IReferenceService));
                        if (refSvc != null)
                        {
                            string name = refSvc.GetName(value);
                            if (name != null)
                            {
                                return name;
                            }
                        }
                    }

                    // Now see if this is an IComponent.
                    //
                    if (!Marshal.IsComObject(value) && value is IComponent)
                    {
                        IComponent comp = (IComponent)value;
                        ISite site = comp.Site;
                        if (site != null)
                        {
                            string name = site.Name;
                            if (name != null)
                            {
                                return name;
                            }
                        }
                    }

                    // Couldn't find it.
                    return String.Empty;
                }
                return s_none;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <internalonly/>
        /// <devdoc>
        ///    <para>Gets a collection of standard values for the reference data type.</para>
        /// </devdoc>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            object[] components = null;

            if (context != null)
            {
                ArrayList list = new ArrayList();
                list.Add(null);

                // Try the reference service first.
                //
                IReferenceService refSvc = (IReferenceService)context.GetService(typeof(IReferenceService));
                if (refSvc != null)
                {
                    object[] objs = refSvc.GetReferences(_type);
                    int count = objs.Length;

                    for (int i = 0; i < count; i++)
                    {
                        if (IsValueAllowed(context, objs[i]))
                            list.Add(objs[i]);
                    }
                }
                else
                {
                    // Now try IContainer.
                    //
                    IContainer cont = context.Container;
                    if (cont != null)
                    {
                        ComponentCollection objs = cont.Components;

                        foreach (IComponent obj in objs)
                        {
                            if (obj != null && _type.IsInstanceOfType(obj) &&
                                IsValueAllowed(context, obj))
                            {
                                list.Add(obj);
                            }
                        }
                    }
                }

                components = list.ToArray();
                Array.Sort(components, 0, components.Length, new ReferenceComparer(this));
            }

            return new StandardValuesCollection(components);
        }

        /// <internalonly/>
        /// <devdoc>
        ///    <para>Gets a value indicating whether the list of standard values returned from
        ///    <see cref='System.ComponentModel.ReferenceConverter.GetStandardValues'/> is an exclusive list. </para>
        /// </devdoc>
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <internalonly/>
        /// <devdoc>
        ///    <para>Gets a value indicating whether this object supports a standard set of values
        ///       that can be picked from a list.</para>
        /// </devdoc>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <devdoc>
        ///    <para>Gets a value indicating whether a particular value can be added to
        ///       the standard values collection.</para>
        /// </devdoc>
        protected virtual bool IsValueAllowed(ITypeDescriptorContext context, object value)
        {
            return true;
        }

        /// <devdoc>
        ///      IComparer object used for sorting references
        /// </devdoc>
        private class ReferenceComparer : IComparer
        {
            private ReferenceConverter _converter;

            public ReferenceComparer(ReferenceConverter converter)
            {
                _converter = converter;
            }

            public int Compare(object item1, object item2)
            {
                String itemName1 = _converter.ConvertToString(item1);
                String itemName2 = _converter.ConvertToString(item2);

                return string.Compare(itemName1, itemName2, false, CultureInfo.InvariantCulture);
            }
        }
    }
}

