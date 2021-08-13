// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert object references to and from various
    /// other representations.
    /// </summary>
    public class ReferenceConverter : TypeConverter
    {
        private static readonly string s_none = SR.toStringNone;
        private readonly Type _type;

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.ReferenceConverter'/> class.
        /// </summary>
        public ReferenceConverter(Type type)
        {
            _type = type;
        }

        /// <summary>
        /// Gets a value indicating whether this converter can convert an object in the
        /// given source type to a reference object using the specified context.
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == typeof(string) && context != null)
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Converts the given object to the reference type.
        /// </summary>
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string text)
            {
                text = text.Trim();
                if (!string.Equals(text, s_none) && context != null)
                {
                    // Try the reference service first.
                    if (context.GetService(typeof(IReferenceService)) is IReferenceService refSvc)
                    {
                        object? obj = refSvc.GetReference(text);
                        if (obj != null)
                        {
                            return obj;
                        }
                    }

                    // Now try IContainer
                    IContainer? cont = context.Container;
                    if (cont != null)
                    {
                        object? obj = cont.Components[text];
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

        /// <summary>
        /// Converts the given value object to the reference type using the specified context and arguments.
        /// </summary>
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (value != null)
                {
                    // Try the reference service first.
                    if (context?.GetService(typeof(IReferenceService)) is IReferenceService refSvc)
                    {
                        string? name = refSvc.GetName(value);
                        if (name != null)
                        {
                            return name;
                        }
                    }

                    // Now see if this is an IComponent.
                    if (!Marshal.IsComObject(value) && value is IComponent comp)
                    {
                        ISite? site = comp.Site;
                        string? name = site?.Name;
                        if (name != null)
                        {
                            return name;
                        }
                    }

                    // Couldn't find it.
                    return string.Empty;
                }

                return s_none;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// Gets a collection of standard values for the reference data type.
        /// </summary>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
        {
            List<object?>? components = null;

            if (context != null)
            {
                components = new List<object?> { null };

                // Try the reference service first.
                if (context.GetService(typeof(IReferenceService)) is IReferenceService refSvc)
                {
                    object[] objs = refSvc.GetReferences(_type);
                    if (objs != null)
                    {
                        for (int i = 0; i < objs.Length; i++)
                        {
                            if (IsValueAllowed(context, objs[i]))
                            {
                                components.Add(objs[i]);
                            }
                        }
                    }
                }
                else
                {
                    // Now try IContainer.
                    IContainer cont = context.Container;
                    if (cont != null)
                    {
                        ComponentCollection objs = cont.Components;
                        foreach (IComponent obj in objs)
                        {
                            if (obj != null && _type != null && _type.IsInstanceOfType(obj) && IsValueAllowed(context, obj))
                            {
                                components.Add(obj);
                            }
                        }
                    }
                }

                components.Sort(new ReferenceComparer(this));
            }

            return new StandardValuesCollection(components);
        }

        /// <summary>
        /// Gets a value indicating whether the list of standard values returned from
        /// <see cref='System.ComponentModel.ReferenceConverter.GetStandardValues'/> is an exclusive list.
        /// </summary>
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => true;

        /// <summary>
        /// Gets a value indicating whether this object supports a standard set of values
        /// that can be picked from a list.
        /// </summary>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;

        /// <summary>
        /// Gets a value indicating whether a particular value can be added to
        /// the standard values collection.
        /// </summary>
        protected virtual bool IsValueAllowed(ITypeDescriptorContext context, object value) => true;

        /// <summary>
        /// IComparer object used for sorting references
        /// </summary>
        private struct ReferenceComparer : IComparer<object>
        {
            private readonly ReferenceConverter _converter;

            public ReferenceComparer(ReferenceConverter converter)
            {
                _converter = converter;
            }

            public int Compare(object? item1, object? item2)
            {
                string? itemName1 = _converter.ConvertToString(item1);
                string? itemName2 = _converter.ConvertToString(item2);

                return string.Compare(itemName1, itemName2, StringComparison.InvariantCulture);
            }
        }
    }
}
