// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies the ambient value for a property. The ambient value is the value you
    /// can set into a property to make it inherit its ambient.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments")]
    [AttributeUsage(AttributeTargets.All)]
    public sealed class AmbientValueAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/> class, converting the
        /// specified value to the specified type, and using the U.S. English culture as the
        /// translation context.
        /// </summary>
        public AmbientValueAttribute(Type type, string value)
        {
            // The try/catch here is because attributes should never throw exceptions. We would fail to
            // load an otherwise normal class.
            try
            {
                Value = TypeDescriptor.GetConverter(type).ConvertFromInvariantString(value);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/> class using a Unicode
        /// character.
        /// </summary>
        public AmbientValueAttribute(char value)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/> class using an 8-bit unsigned
        /// integer.
        /// </summary>
        public AmbientValueAttribute(byte value)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/> class using a 16-bit signed
        /// integer.
        /// </summary>
        public AmbientValueAttribute(short value)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/> class using a 32-bit signed
        /// integer.
        /// </summary>
        public AmbientValueAttribute(int value)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/> class using a 64-bit signed
        /// integer.
        /// </summary>
        public AmbientValueAttribute(long value)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/> class using a
        /// single-precision floating point number.
        /// </summary>
        public AmbientValueAttribute(float value)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/> class using a
        /// double-precision floating point number.
        /// </summary>
        public AmbientValueAttribute(double value)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/> class using a <see cref='System.Boolean'/>
        /// value.
        /// </summary>
        public AmbientValueAttribute(bool value)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/> class using a <see cref='System.String'/>.
        /// </summary>
        public AmbientValueAttribute(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.AmbientValueAttribute'/>
        /// class.
        /// </summary>
        public AmbientValueAttribute(object value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the ambient value of the property this attribute is bound to.
        /// </summary>
        public object Value { get; }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (obj is AmbientValueAttribute other)
            {
                return Value != null ? Value.Equals(other.Value) : other.Value == null;
            }

            return false;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
