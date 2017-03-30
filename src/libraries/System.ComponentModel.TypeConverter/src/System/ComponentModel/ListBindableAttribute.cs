// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>[To be supplied.]</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class ListBindableAttribute : Attribute
    {
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public static readonly ListBindableAttribute Yes = new ListBindableAttribute(true);

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public static readonly ListBindableAttribute No = new ListBindableAttribute(false);

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public static readonly ListBindableAttribute Default = Yes;

        private bool _isDefault;

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public ListBindableAttribute(bool listBindable)
        {
            ListBindable = listBindable;
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public ListBindableAttribute(BindableSupport flags)
        {
            ListBindable = (flags != BindableSupport.No);
            _isDefault = (flags == BindableSupport.Default);
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public bool ListBindable { get; }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            ListBindableAttribute other = obj as ListBindableAttribute;
            return other != null && other.ListBindable == ListBindable;
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
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public override bool IsDefaultAttribute()
        {
            return (Equals(Default) || _isDefault);
        }
    }
}
