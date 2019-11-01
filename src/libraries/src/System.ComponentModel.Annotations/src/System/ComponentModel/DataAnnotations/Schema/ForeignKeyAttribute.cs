// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.ComponentModel.DataAnnotations.Schema
{
    /// <summary>
    ///     Denotes a property used as a foreign key in a relationship.
    ///     The annotation may be placed on the foreign key property and specify the associated navigation property name,
    ///     or placed on a navigation property and specify the associated foreign key name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ForeignKeyAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ForeignKeyAttribute" /> class.
        /// </summary>
        /// <param name="name">
        ///     If placed on a foreign key property, the name of the associated navigation property.
        ///     If placed on a navigation property, the name of the associated foreign key(s).
        ///     If a navigation property has multiple foreign keys, a comma separated list should be supplied.
        /// </param>
        public ForeignKeyAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(SR.Format(SR.ArgumentIsNullOrWhitespace, nameof(name)), nameof(name));
            }

            Name = name;
        }

        /// <summary>
        ///     If placed on a foreign key property, the name of the associated navigation property.
        ///     If placed on a navigation property, the name of the associated foreign key(s).
        /// </summary>
        public string Name { get; }
    }
}
