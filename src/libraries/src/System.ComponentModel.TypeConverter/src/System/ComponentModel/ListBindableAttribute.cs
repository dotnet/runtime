// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    [AttributeUsage(AttributeTargets.All)]
    public sealed class ListBindableAttribute : Attribute
    {
        public static readonly ListBindableAttribute Yes = new ListBindableAttribute(true);

        public static readonly ListBindableAttribute No = new ListBindableAttribute(false);

        public static readonly ListBindableAttribute Default = Yes;

        private readonly bool _isDefault;

        public ListBindableAttribute(bool listBindable)
        {
            ListBindable = listBindable;
        }

        public ListBindableAttribute(BindableSupport flags)
        {
            ListBindable = (flags != BindableSupport.No);
            _isDefault = (flags == BindableSupport.Default);
        }

        public bool ListBindable { get; }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            return obj is ListBindableAttribute other && other.ListBindable == ListBindable;
        }

        /// <summary>
        /// Returns the hashcode for this object.
        /// </summary>
        public override int GetHashCode() => base.GetHashCode();

        public override bool IsDefaultAttribute() => (Equals(Default) || _isDefault);
    }
}
