// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Provides a type converter to convert Version objects to and
    ///       from various other representations.</para>
    /// </summary>
    public class VersionConverter : TypeConverter
    {
        /// <summary>
        ///    <para>Gets a value indicating whether this converter can convert an object in the
        ///       given source type to a Version.</para>
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string) || sourceType == typeof(Version) || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        ///    <para>Gets a value indicating whether this converter can
        ///       convert an object to the given destination type using the context.</para>
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            return destinationType == typeof(Version) || destinationType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        ///    <para>Converts the given object to a Version.</para>
        /// </summary>
        /// <exception cref="FormatException"><paramref name="value"/> is not a valid version string</exception>
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string versionString)
            {
                try
                {
                    return Version.Parse(versionString);
                }
                catch (Exception e)
                {
                    throw new FormatException(SR.Format(SR.ConvertInvalidPrimitive, versionString, nameof(Version)), e);
                }
            }

            if (value is Version version)
            {
                return new Version(version.Major, version.Minor, version.Build, version.Revision); // return new instance
            }

            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        ///    <para>Converts the given value object to
        ///       the specified destination type using the specified context and arguments.</para>
        /// </summary>
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (value is Version version)
            {
                if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo? ctor = typeof(Version).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(int), typeof(int), typeof(int), typeof(int) }, null);
                    Debug.Assert(ctor != null, "Couldn't find constructor");
                    return new InstanceDescriptor(ctor, new object[] { version.Major, version.Minor, version.Build, version.Revision });
                }

                if (destinationType == typeof(string))
                {
                    return version.ToString();
                }

                if (destinationType == typeof(Version))
                {
                    return new Version(version.Major, version.Minor, version.Build, version.Revision);
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool IsValid(ITypeDescriptorContext? context, object? value)
        {
            if (value is string version)
            {
                return Version.TryParse(version, out Version _);
            }
            return value is Version;
        }
    }
}
