// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Threading;

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies the default value for a property.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments")]
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
    [AttributeUsage(AttributeTargets.All)]
    public class DefaultValueAttribute : Attribute
    {
        /// <summary>
        /// This is the default value.
        /// </summary>
        private object? _value;

        // Delegate ad hoc created 'TypeDescriptor.ConvertFromInvariantString' reflection object cache
        private static object? s_convertFromInvariantString;

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DefaultValueAttribute'/>
        /// class, converting the specified value to the specified type, and using the U.S. English
        /// culture as the translation context.
        /// </summary>
        public DefaultValueAttribute(Type type, string? value)
        {
            // The null check and try/catch here are because attributes should never throw exceptions.
            // We would fail to load an otherwise normal class.

            if (type == null)
            {
                return;
            }

            try
            {
                if (TryConvertFromInvariantString(type, value, out object? convertedValue))
                {
                    _value = convertedValue;
                }
                else if (type.IsSubclassOf(typeof(Enum)) && value != null)
                {
                    _value = Enum.Parse(type, value, true);
                }
                else if (type == typeof(TimeSpan) && value != null)
                {
                    _value = TimeSpan.Parse(value);
                }
                else
                {
                    _value = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
                }

                // Looking for ad hoc created TypeDescriptor.ConvertFromInvariantString(Type, string)
                bool TryConvertFromInvariantString(Type? typeToConvert, string? stringValue, out object? conversionResult)
                {
                    conversionResult = null;

                    // lazy init reflection objects
                    if (s_convertFromInvariantString == null)
                    {
                        Type? typeDescriptorType = Type.GetType("System.ComponentModel.TypeDescriptor, System.ComponentModel.TypeConverter", throwOnError: false);
                        MethodInfo? mi = typeDescriptorType?.GetMethod("ConvertFromInvariantString", BindingFlags.NonPublic | BindingFlags.Static);
                        Volatile.Write(ref s_convertFromInvariantString, mi == null ? new object() : mi.CreateDelegate(typeof(Func<Type, string, object>)));
                    }

                    if (!(s_convertFromInvariantString is Func<Type?, string?, object> convertFromInvariantString))
                        return false;

                    try
                    {
                        conversionResult = convertFromInvariantString(typeToConvert, stringValue);
                    }
                    catch
                    {
                        return false;
                    }

                    return true;
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DefaultValueAttribute'/>
        /// class using a Unicode character.
        /// </summary>
        public DefaultValueAttribute(char value)
        {
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DefaultValueAttribute'/>
        /// class using an 8-bit unsigned integer.
        /// </summary>
        public DefaultValueAttribute(byte value)
        {
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DefaultValueAttribute'/>
        /// class using a 16-bit signed integer.
        /// </summary>
        public DefaultValueAttribute(short value)
        {
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DefaultValueAttribute'/>
        /// class using a 32-bit signed integer.
        /// </summary>
        public DefaultValueAttribute(int value)
        {
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DefaultValueAttribute'/>
        /// class using a 64-bit signed integer.
        /// </summary>
        public DefaultValueAttribute(long value)
        {
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DefaultValueAttribute'/>
        /// class using a single-precision floating point number.
        /// </summary>
        public DefaultValueAttribute(float value)
        {
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DefaultValueAttribute'/>
        /// class using a double-precision floating point number.
        /// </summary>
        public DefaultValueAttribute(double value)
        {
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DefaultValueAttribute'/>
        /// class using a <see cref='System.Boolean'/> value.
        /// </summary>
        public DefaultValueAttribute(bool value)
        {
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DefaultValueAttribute'/>
        /// class using a <see cref='System.String'/>.
        /// </summary>
        public DefaultValueAttribute(string? value)
        {
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DefaultValueAttribute'/>
        /// class.
        /// </summary>
        public DefaultValueAttribute(object? value)
        {
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DefaultValueAttribute'/>
        /// class using a <see cref='System.SByte'/> value.
        /// </summary>
        [CLSCompliant(false)]
        public DefaultValueAttribute(sbyte value)
        {
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DefaultValueAttribute'/>
        /// class using a <see cref='System.UInt16'/> value.
        /// </summary>
        [CLSCompliant(false)]
        public DefaultValueAttribute(ushort value)
        {
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DefaultValueAttribute'/>
        /// class using a <see cref='System.UInt32'/> value.
        /// </summary>
        [CLSCompliant(false)]
        public DefaultValueAttribute(uint value)
        {
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DefaultValueAttribute'/>
        /// class using a <see cref='System.UInt64'/> value.
        /// </summary>
        [CLSCompliant(false)]
        public DefaultValueAttribute(ulong value)
        {
            _value = value;
        }

        /// <summary>
        /// Gets the default value of the property this attribute is bound to.
        /// </summary>
        public virtual object? Value => _value;

        public override bool Equals(object? obj)
        {
            if (obj == this)
            {
                return true;
            }
            if (!(obj is DefaultValueAttribute other))
            {
                return false;
            }
            
            if (Value == null)
            {
                return other.Value == null;
            }

            return Value.Equals(other.Value);
        }

        public override int GetHashCode() => base.GetHashCode();

        protected void SetValue(object? value) => _value = value;
    }
}
