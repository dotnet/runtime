// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                throw new ArgumentOutOfRangeException("databaseGeneratedOption");
            }

            DatabaseGeneratedOption = databaseGeneratedOption;
        }

        /// <summary>
        ///     The pattern used to generate values for the property in the database.
        /// </summary>
        public DatabaseGeneratedOption DatabaseGeneratedOption { get; private set; }
    }
}
