// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies whether a property is appropriate to bind data to.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class BindableAttribute : Attribute
    {
        /// <summary>
        /// Specifies that a property is appropriate to bind data to. This
        /// <see langword='static '/>field is read-only.
        /// </summary>
        public static readonly BindableAttribute Yes = new BindableAttribute(true);

        /// <summary>
        /// Specifies that a property is not appropriate to bind data to.
        /// This <see langword='static '/>field is read-only.
        /// </summary>
        public static readonly BindableAttribute No = new BindableAttribute(false);

        /// <summary>
        /// Specifies the default value for the <see cref='System.ComponentModel.BindableAttribute'/>, which
        /// is <see cref='System.ComponentModel.BindableAttribute.No'/>. This <see langword='static '/>field is
        /// read-only.
        /// </summary>
        public static readonly BindableAttribute Default = No;

        private readonly bool _isDefault;

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.BindableAttribute'/> class.
        /// </summary>
        public BindableAttribute(bool bindable) : this(bindable, BindingDirection.OneWay)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.BindableAttribute'/> class.
        /// </summary>
        public BindableAttribute(bool bindable, BindingDirection direction)
        {
            Bindable = bindable;
            Direction = direction;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.BindableAttribute'/> class.
        /// </summary>
        public BindableAttribute(BindableSupport flags) : this(flags, BindingDirection.OneWay)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.BindableAttribute'/> class.
        /// </summary>
        public BindableAttribute(BindableSupport flags, BindingDirection direction)
        {
            Bindable = (flags != BindableSupport.No);
            _isDefault = (flags == BindableSupport.Default);
            Direction = direction;
        }

        /// <summary>
        /// Gets a value indicating whether a property is appropriate to bind data to.
        /// </summary>
        public bool Bindable { get; }

        /// <summary>
        /// Gets a value indicating the direction(s) this property be bound to data.
        /// </summary>
        public BindingDirection Direction { get; }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == this)
            {
                return true;
            }

            return obj is BindableAttribute other && other.Bindable == Bindable;
        }

        public override int GetHashCode() => Bindable.GetHashCode();

        public override bool IsDefaultAttribute() => Equals(Default) || _isDefault;
    }
}
