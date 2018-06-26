// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.ComponentModel.DataAnnotations.Schema
{
    /// <summary>
    ///     Specifies the database column that a property is mapped to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ColumnAttribute : Attribute
    {
        private int _order = -1;
        private string _typeName;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ColumnAttribute" /> class.
        /// </summary>
        public ColumnAttribute()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ColumnAttribute" /> class.
        /// </summary>
        /// <param name="name">The name of the column the property is mapped to.</param>
        public ColumnAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(SR.Format(SR.ArgumentIsNullOrWhitespace, nameof(name)), nameof(name));
            }

            Name = name;
        }

        /// <summary>
        ///     The name of the column the property is mapped to.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     The zero-based order of the column the property is mapped to.
        /// </summary>
        public int Order
        {
            get => _order;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _order = value;
            }
        }

        /// <summary>
        ///     The database provider specific data type of the column the property is mapped to.
        /// </summary>
        public string TypeName
        {
            get => _typeName;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException(SR.Format(SR.ArgumentIsNullOrWhitespace, nameof(value)), nameof(value));
                }

                _typeName = value;
            }
        }
    }
}
