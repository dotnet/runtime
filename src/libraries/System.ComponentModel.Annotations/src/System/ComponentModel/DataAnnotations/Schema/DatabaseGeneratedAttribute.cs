// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.DataAnnotations.Schema
{
    /// <summary>
    ///     Specifies how the database generates values for a property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class DatabaseGeneratedAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DatabaseGeneratedAttribute" /> class.
        /// </summary>
        /// <param name="databaseGeneratedOption">The pattern used to generate values for the property in the database.</param>
        public DatabaseGeneratedAttribute(DatabaseGeneratedOption databaseGeneratedOption)
        {
            if (!(Enum.IsDefined(typeof(DatabaseGeneratedOption), databaseGeneratedOption)))
            {
                throw new ArgumentOutOfRangeException(nameof(databaseGeneratedOption));
            }

            DatabaseGeneratedOption = databaseGeneratedOption;
        }

        /// <summary>
        ///     The pattern used to generate values for the property in the database.
        /// </summary>
        public DatabaseGeneratedOption DatabaseGeneratedOption { get; }
    }
}
