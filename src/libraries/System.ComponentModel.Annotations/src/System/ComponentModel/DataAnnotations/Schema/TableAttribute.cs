// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel.DataAnnotations.Schema
{
    /// <summary>
    ///     Specifies the database table that a class is mapped to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TableAttribute : Attribute
    {
        private string? _schema;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TableAttribute" /> class.
        /// </summary>
        /// <param name="name">The name of the table the class is mapped to.</param>
        public TableAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(SR.Format(SR.ArgumentIsNullOrWhitespace, nameof(name)), nameof(name));
            }

            Name = name;
        }

        /// <summary>
        ///     The name of the table the class is mapped to.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     The schema of the table the class is mapped to.
        /// </summary>
        [DisallowNull]
        public string? Schema
        {
            get => _schema;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException(SR.Format(SR.ArgumentIsNullOrWhitespace, nameof(value)), nameof(value));
                }

                _schema = value;
            }
        }
    }
}
