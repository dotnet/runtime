// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///     If this attribute is placed on a property or a type, its text representation in a property window
    ///     will appear as dots or asterisks to indicate a password field.  This indication in no way
    ///     represents any type of encryption or security.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class PasswordPropertyTextAttribute : Attribute
    {
        /// <summary>
        ///     Sets the System.ComponentModel.Design.PasswordPropertyText
        ///     attribute by default to true.
        /// </summary>
        public static readonly PasswordPropertyTextAttribute Yes = new PasswordPropertyTextAttribute(true);

        /// <summary>
        ///     Sets the System.ComponentModel.Design.PasswordPropertyText
        ///     attribute by default to false.
        /// </summary>
        public static readonly PasswordPropertyTextAttribute No = new PasswordPropertyTextAttribute(false);


        /// <summary>
        ///     Sets the System.ComponentModel.Design.PasswordPropertyText
        ///     attribute by default to false.
        /// </summary>
        public static readonly PasswordPropertyTextAttribute Default = No;

        /// <summary>
        ///    Creates a default PasswordPropertyTextAttribute.
        /// </summary>
        public PasswordPropertyTextAttribute() : this(false)
        {
        }

        /// <summary>
        ///    Creates a PasswordPropertyTextAttribute with the given password value.
        /// </summary>
        public PasswordPropertyTextAttribute(bool password)
        {
            Password = password;
        }

        /// <summary>
        ///     Gets a value indicating if the property this attribute is defined for should be shown as password text.
        /// </summary>
        public bool Password { get; }

        /// <summary>
        ///     Overload for object equality
        /// </summary>
        public override bool Equals(object o)
        {
            if (o is PasswordPropertyTextAttribute)
            {
                return ((PasswordPropertyTextAttribute)o).Password == Password;
            }
            return false;
        }

        /// <summary>
        ///     Returns the hashcode for this object.
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        ///     Gets a value indicating whether this attribute is set to true by default.
        /// </summary>
        public override bool IsDefaultAttribute()
        {
            return Equals(Default);
        }
    }
}
