// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Specifies the ambient value for a property.  The ambient value is the value you
    ///    can set into a property to make it inherit its ambient.</para>
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments")]
    [AttributeUsage(AttributeTargets.All)]
    public sealed class AmbientValueAttribute : Attribute
    {
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/> class, converting the
        ///    specified value to the
        ///    specified type, and using the U.S. English culture as the
        ///    translation
        ///    context.</para>
        /// </summary>
        public AmbientValueAttribute(Type type, string value)
        {
            // The try/catch here is because attributes should never throw exceptions.  We would fail to
            // load an otherwise normal class.
            try
            {
                Value = TypeDescriptor.GetConverter(type).ConvertFromInvariantString(value);
            }
            catch
            {
                Debug.Fail($"Ambient value attribute of type {type.FullName} threw converting from the string '{value}'.");
            }
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/> class using a Unicode
        ///    character.</para>
        /// </summary>
        public AmbientValueAttribute(char value)
        {
            Value = value;
        }
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/> class using an 8-bit unsigned
        ///    integer.</para>
        /// </summary>
        public AmbientValueAttribute(byte value)
        {
            Value = value;
        }
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/> class using a 16-bit signed
        ///    integer.</para>
        /// </summary>
        public AmbientValueAttribute(short value)
        {
            Value = value;
        }
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/> class using a 32-bit signed
        ///    integer.</para>
        /// </summary>
        public AmbientValueAttribute(int value)
        {
            Value = value;
        }
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/> class using a 64-bit signed
        ///    integer.</para>
        /// </summary>
        public AmbientValueAttribute(long value)
        {
            Value = value;
        }
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/> class using a
        ///    single-precision floating point
        ///    number.</para>
        /// </summary>
        public AmbientValueAttribute(float value)
        {
            Value = value;
        }
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/> class using a
        ///    double-precision floating point
        ///    number.</para>
        /// </summary>
        public AmbientValueAttribute(double value)
        {
            Value = value;
        }
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/> class using a <see cref='System.Boolean'/>
        /// value.</para>
        /// </summary>
        public AmbientValueAttribute(bool value)
        {
            Value = value;
        }
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/> class using a <see cref='System.String'/>.</para>
        /// </summary>
        public AmbientValueAttribute(string value)
        {
            Value = value;
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/>
        /// class.</para>
        /// </summary>
        public AmbientValueAttribute(object value)
        {
            Value = value;
        }

        /// <summary>
        ///    <para>
        ///       Gets the ambient value of the property this
        ///       attribute is
        ///       bound to.
        ///    </para>
        /// </summary>
        public object Value { get; }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            AmbientValueAttribute other = obj as AmbientValueAttribute;

            if (other != null)
            {
                if (Value != null)
                {
                    return Value.Equals(other.Value);
                }
                else
                {
                    return (other.Value == null);
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}

