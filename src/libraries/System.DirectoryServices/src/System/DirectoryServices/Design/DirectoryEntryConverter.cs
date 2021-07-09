// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Collections;
using System.Globalization;

namespace System.DirectoryServices.Design
{
    internal sealed class DirectoryEntryConverter : TypeConverter
    {
        private static StandardValuesCollection? s_values;
        private static readonly Hashtable s_componentsCreated = new Hashtable(StringComparer.OrdinalIgnoreCase);

        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
        {
            if (value != null && value is string)
            {
                string text = ((string)value).Trim();

                if (text.Length == 0)
                    return null;

                if (text.CompareTo(SR.DSNotSet) != 0)
                {
                    DirectoryEntry? newEntry = GetFromCache(text);
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

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType != null && destinationType == typeof(string))
            {
                if (value != null)
                    return ((DirectoryEntry)value).Path;
                else
                    return SR.DSNotSet;
            }

            return base.ConvertTo(context, culture, value, destinationType!);
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
        {
            if (s_values == null)
            {
                s_values = new StandardValuesCollection(new object?[] { null });
            }
            return s_values;
        }

        internal static DirectoryEntry? GetFromCache(string path)
        {
            if (s_componentsCreated.ContainsKey(path))
            {
                DirectoryEntry existingComponent = (DirectoryEntry)s_componentsCreated[path]!;
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

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => false;

        public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;
    }
}
