// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Marks instances of objects that are inherited from their base class. This
    ///       class cannot be inherited.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event)]
    public sealed class InheritanceAttribute : Attribute
    {
        /// <summary>
        ///    <para>
        ///       Specifies that the component is inherited. This field is
        ///       read-only.
        ///    </para>
        /// </summary>
        public static readonly InheritanceAttribute Inherited = new InheritanceAttribute(InheritanceLevel.Inherited);

        /// <summary>
        ///    <para>
        ///       Specifies that
        ///       the component is inherited and is read-only. This field is
        ///       read-only.
        ///    </para>
        /// </summary>
        public static readonly InheritanceAttribute InheritedReadOnly = new InheritanceAttribute(InheritanceLevel.InheritedReadOnly);

        /// <summary>
        ///    <para>
        ///       Specifies that the component is not inherited. This field is
        ///       read-only.
        ///    </para>
        /// </summary>
        public static readonly InheritanceAttribute NotInherited = new InheritanceAttribute(InheritanceLevel.NotInherited);

        /// <summary>
        ///    <para>
        ///       Specifies the default value for
        ///       the InheritanceAttribute as NotInherited.
        ///    </para>
        /// </summary>
        public static readonly InheritanceAttribute Default = NotInherited;

        /// <summary>
        /// <para>Initializes a new instance of the System.ComponentModel.Design.InheritanceAttribute 
        /// class.</para>
        /// </summary>
        public InheritanceAttribute()
        {
            InheritanceLevel = Default.InheritanceLevel;
        }

        /// <summary>
        /// <para>Initializes a new instance of the System.ComponentModel.Design.InheritanceAttribute class 
        ///    with the specified inheritance
        ///    level.</para>
        /// </summary>
        public InheritanceAttribute(InheritanceLevel inheritanceLevel)
        {
            InheritanceLevel = inheritanceLevel;
        }

        /// <summary>
        ///    <para>
        ///       Gets or sets
        ///       the current inheritance level stored in this attribute.
        ///    </para>
        /// </summary>
        public InheritanceLevel InheritanceLevel { get; }

        /// <summary>
        ///    <para>
        ///       Override to test for equality.
        ///    </para>
        /// </summary>
        public override bool Equals(object value)
        {
            if (value == this)
            {
                return true;
            }

            if (!(value is InheritanceAttribute))
            {
                return false;
            }

            InheritanceLevel valueLevel = ((InheritanceAttribute)value).InheritanceLevel;
            return (valueLevel == InheritanceLevel);
        }

        /// <summary>
        ///    <para>
        ///       Returns the hashcode for this object.
        ///    </para>
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        ///    <para>
        ///       Gets whether this attribute is the default.
        ///    </para>
        /// </summary>
        public override bool IsDefaultAttribute()
        {
            return (Equals(Default));
        }

        /// <summary>
        ///    <para>
        ///       Converts this attribute to a string.
        ///    </para>
        /// </summary>
        public override string ToString()
        {
            return TypeDescriptor.GetConverter(typeof(InheritanceLevel)).ConvertToString(InheritanceLevel);
        }
    }
}

