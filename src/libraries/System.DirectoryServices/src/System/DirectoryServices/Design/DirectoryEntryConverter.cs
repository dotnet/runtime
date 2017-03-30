// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Design
{
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System;
    using System.Reflection;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Globalization;

    /// <include file='doc\DirectoryEntryConverter.uex' path='docs/doc[@for="DirectoryEntryConverter"]/*' />
    /// <internalonly/>
    internal class DirectoryEntryConverter : TypeConverter
    {
        private static StandardValuesCollection s_values;
        private static Hashtable s_componentsCreated = new Hashtable(StringComparer.OrdinalIgnoreCase);

        /// <include file='doc\DirectoryEntryConverter.uex' path='docs/doc[@for="DirectoryEntryConverter.CanConvertFrom"]/*' />
        /// <internalonly/>                               
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        /// <include file='doc\DirectoryEntryConverter.uex' path='docs/doc[@for="DirectoryEntryConverter.ConvertFrom"]/*' />
        /// <internalonly/>                 
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value != null && value is string)
            {
                string text = ((string)value).Trim();

                if (text.Length == 0)
                    return null;

                if (text.CompareTo(SR.DSNotSet) != 0)
                {
                    DirectoryEntry newEntry = GetFromCache(text);
                    if (newEntry == null)
                    {
                        newEntry = new DirectoryEntry(text);
                        s_componentsCreated[text] = newEntry;
                        if (context != null)
                            context.Container.Add(newEntry);

                        return newEntry;
                    }
                }
            }

            return null;
        }

        /// <include file='doc\DirectoryEntryConverter.uex' path='docs/doc[@for="DirectoryEntryConverter.ConvertTo"]/*' />
        /// <internalonly/>                 
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType != null && destinationType == typeof(string))
            {
                if (value != null)
                    return ((DirectoryEntry)value).Path;
                else
                    return SR.DSNotSet;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <include file='doc\MessageFormatterConverter.uex' path='docs/doc[@for="DirectoryEntryConverter.GetStandardValues"]/*' />
        /// <internalonly/>            
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (s_values == null)
            {
                s_values = new StandardValuesCollection(new object[] { null });
            }
            return s_values;
        }

        internal static DirectoryEntry GetFromCache(string path)
        {
            if (s_componentsCreated.ContainsKey(path))
            {
                DirectoryEntry existingComponent = (DirectoryEntry)s_componentsCreated[path];
                if (existingComponent.Site == null)
                    s_componentsCreated.Remove(path);
                else
                {
                    if (existingComponent.Path == path)
                        return existingComponent;
                    else
                        s_componentsCreated.Remove(path);
                }
            }

            return null;
        }

        /// <include file='doc\MessageFormatterConverter.uex' path='docs/doc[@for="DirectoryEntryConverter.GetStandardValuesExclusive"]/*' />
        /// <internalonly/>                                   
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        /// <include file='doc\MessageFormatterConverter.uex' path='docs/doc[@for="DirectoryEntryConverter.GetStandardValuesSupported"]/*' />
        /// <internalonly/>                        
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
    }
}

