// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>
    ///       Indicates whether the parent property is notified
    ///       if a child namespace property is modified.
    ///    </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class NotifyParentPropertyAttribute : Attribute
    {
        /// <summary>
        ///    <para>
        ///       Specifies that the parent property should be notified on changes to the child class property. This field is read-only.
        ///    </para>
        /// </summary>
        public static readonly NotifyParentPropertyAttribute Yes = new NotifyParentPropertyAttribute(true);

        /// <summary>
        ///    <para>Specifies that the parent property should not be notified of changes to the child class property. This field is read-only.</para>
        /// </summary>
        public static readonly NotifyParentPropertyAttribute No = new NotifyParentPropertyAttribute(false);

        /// <summary>
        ///    <para>Specifies the default attribute state, that the parent property should not be notified of changes to the child class property.
        ///       This field is read-only.</para>
        /// </summary>
        public static readonly NotifyParentPropertyAttribute Default = No;

        /// <summary>
        /// <para>Initializes a new instance of the NotifyPropertyAttribute class 
        ///    that uses the specified value
        ///    to indicate whether the parent property should be notified when a child namespace property is modified.</para>
        /// </summary>
        public NotifyParentPropertyAttribute(bool notifyParent) => NotifyParent = notifyParent;


        /// <summary>
        ///    <para>
        ///       Gets or sets whether the parent property should be notified
        ///       on changes to a child namespace property.
        ///    </para>
        /// </summary>
        public bool NotifyParent { get; }

        /// <summary>
        ///    <para>
        ///       Tests whether the specified object is the same as the current object.
        ///    </para>
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            NotifyParentPropertyAttribute other = obj as NotifyParentPropertyAttribute;
            return other?.NotifyParent == NotifyParent;
        }

        public override int GetHashCode() => base.GetHashCode();

        public override bool IsDefaultAttribute() => Equals(Default);
    }
}
