// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    using System;
    using System.Security.Permissions;

    /// <summary>
    ///    <para>
    ///       DesignTimeVisibileAttribute marks a component's visibility. If
    ///       DesignTimeVisibileAttribute.Yes is present, a visual designer can show
    ///       this component on a designer.
    ///    </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class DesignTimeVisibleAttribute : Attribute
    {
        /// <summary>
        ///     Creates a new DesignTimeVisibleAttribute with the visible
        ///     property set to the given value.
        /// </summary>
        public DesignTimeVisibleAttribute(bool visible)
        {
            Visible = visible;
        }

        /// <summary>
        ///     Creates a new DesignTimeVisibleAttribute set to the default
        ///     value of true.
        /// </summary>
        public DesignTimeVisibleAttribute()
        {
        }

        /// <summary>
        ///     True if this component should be shown at design time, or false
        ///     if it shouldn't.
        /// </summary>
        public bool Visible { get; }

        /// <summary>
        ///     Marks a component as visible in a visual designer.
        /// </summary>
        public static readonly DesignTimeVisibleAttribute Yes = new DesignTimeVisibleAttribute(true);

        /// <summary>
        ///     Marks a component as not visible in a visual designer.
        /// </summary>
        public static readonly DesignTimeVisibleAttribute No = new DesignTimeVisibleAttribute(false);

        /// <summary>
        ///     The default visibility. (equal to Yes.)
        /// </summary>
        public static readonly DesignTimeVisibleAttribute Default = Yes;

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            DesignTimeVisibleAttribute other = obj as DesignTimeVisibleAttribute;
            return other != null && other.Visible == Visible;
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public override int GetHashCode()
        {
            return typeof(DesignTimeVisibleAttribute).GetHashCode() ^ (Visible ? -1 : 0);
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public override bool IsDefaultAttribute()
        {
            return (Visible == Default.Visible);
        }
    }
}
