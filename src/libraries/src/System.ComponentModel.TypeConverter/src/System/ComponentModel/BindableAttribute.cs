// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Security.Permissions;

    /// <summary>
    ///    <para>Specifies whether a property is appropriate to bind data
    ///       to.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class BindableAttribute : Attribute
    {
        /// <summary>
        ///    <para>
        ///       Specifies that a property is appropriate to bind data to. This
        ///    <see langword='static '/>field is read-only. 
        ///    </para>
        /// </summary>
        public static readonly BindableAttribute Yes = new BindableAttribute(true);

        /// <summary>
        ///    <para>
        ///       Specifies that a property is not appropriate to bind
        ///       data to. This <see langword='static '/>field is read-only.
        ///    </para>
        /// </summary>
        public static readonly BindableAttribute No = new BindableAttribute(false);

        /// <summary>
        ///    <para>
        ///       Specifies the default value for the <see cref='System.ComponentModel.BindableAttribute'/>,
        ///       which is <see cref='System.ComponentModel.BindableAttribute.No'/>. This <see langword='static '/>field is
        ///       read-only.
        ///    </para>
        /// </summary>
        public static readonly BindableAttribute Default = No;

        private bool _bindable = false;
        private bool _isDefault = false;
        private BindingDirection _direction;

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.BindableAttribute'/> class.
        ///    </para>
        /// </summary>
        public BindableAttribute(bool bindable) : this(bindable, BindingDirection.OneWay)
        {
        }

        /// <summary>
        /// <para>
        /// Initializes a new instance of the <see cref='System.ComponentModel.BindableAttribute'/> class.
        /// </para>
        /// </summary>
        public BindableAttribute(bool bindable, BindingDirection direction)
        {
            _bindable = bindable;
            _direction = direction;
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.BindableAttribute'/> class.
        ///    </para>
        /// </summary>
        public BindableAttribute(BindableSupport flags) : this(flags, BindingDirection.OneWay)
        {
        }

        /// <summary>
        /// <para>
        /// Initializes a new instance of the <see cref='System.ComponentModel.BindableAttribute'/> class.
        /// </para>
        /// </summary>
        public BindableAttribute(BindableSupport flags, BindingDirection direction)
        {
            _bindable = (flags != BindableSupport.No);
            _isDefault = (flags == BindableSupport.Default);
            _direction = direction;
        }

        /// <summary>
        ///    <para>
        ///       Gets a value indicating
        ///       whether a property is appropriate to bind data to.
        ///    </para>
        /// </summary>
        public bool Bindable
        {
            get
            {
                return _bindable;
            }
        }

        /// <summary>
        /// <para>
        /// Gets a value indicating
        /// the direction(s) this property be bound to data.
        /// </para>
        /// </summary>
        public BindingDirection Direction
        {
            get
            {
                return _direction;
            }
        }

        /// <internalonly/>
        /// <summary>
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (obj != null && obj is BindableAttribute)
            {
                return (((BindableAttribute)obj).Bindable == _bindable);
            }

            return false;
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public override int GetHashCode()
        {
            return _bindable.GetHashCode();
        }

        /// <summary>
        /// </summary>
        /// <internalonly/>
        public override bool IsDefaultAttribute()
        {
            return (Equals(Default) || _isDefault);
        }
    }
}
