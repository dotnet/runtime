// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Marks instances of objects that are inherited from their base class. This
    /// class cannot be inherited.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event)]
    public sealed class InheritanceAttribute : Attribute
    {
        /// <summary>
        /// 
        /// Specifies that the component is inherited. This field is
        /// read-only.
        /// 
        /// </summary>
        public static readonly InheritanceAttribute Inherited = new InheritanceAttribute(InheritanceLevel.Inherited);

        /// <summary>
        /// 
        /// Specifies that
        /// the component is inherited and is read-only. This field is
        /// read-only.
        /// 
        /// </summary>
        public static readonly InheritanceAttribute InheritedReadOnly = new InheritanceAttribute(InheritanceLevel.InheritedReadOnly);

        /// <summary>
        /// 
        /// Specifies that the component is not inherited. This field is
        /// read-only.
        /// 
        /// </summary>
        public static readonly InheritanceAttribute NotInherited = new InheritanceAttribute(InheritanceLevel.NotInherited);

        /// <summary>
        /// 
        /// Specifies the default value for
        /// the InheritanceAttribute as NotInherited.
        /// 
        /// </summary>
        public static readonly InheritanceAttribute Default = NotInherited;

        /// <summary>
        /// Initializes a new instance of the System.ComponentModel.Design.InheritanceAttribute 
        /// class.
        /// </summary>
        public InheritanceAttribute()
        {
            InheritanceLevel = Default.InheritanceLevel;
        }

        /// <summary>
        /// Initializes a new instance of the System.ComponentModel.Design.InheritanceAttribute class 
        /// with the specified inheritance
        /// level.
        /// </summary>
        public InheritanceAttribute(InheritanceLevel inheritanceLevel)
        {
            InheritanceLevel = inheritanceLevel;
        }

        /// <summary>
        /// 
        /// Gets or sets
        /// the current inheritance level stored in this attribute.
        /// 
        /// </summary>
        public InheritanceLevel InheritanceLevel { get; }

        /// <summary>
        /// 
        /// Override to test for equality.
        /// 
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
        /// Returns the hashcode for this object.
        /// </summary>
        public override int GetHashCode() => base.GetHashCode();

        /// <summary>
        /// Gets whether this attribute is the default.
        /// </summary>
        public override bool IsDefaultAttribute() => Equals(Default);

        /// <summary>
        /// Converts this attribute to a string.
        /// </summary>
        public override string ToString() => TypeDescriptor.GetConverter(typeof(InheritanceLevel)).ConvertToString(InheritanceLevel);
    }
}
