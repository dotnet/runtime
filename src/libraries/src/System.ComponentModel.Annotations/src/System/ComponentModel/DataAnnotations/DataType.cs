// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Enumeration of logical data types that may appear in <see cref="DataTypeAttribute" />
    /// </summary>
    public enum DataType
    {
        /// <summary>
        ///     Custom data type, not one of the static data types we know
        /// </summary>
        Custom = 0,

        /// <summary>
        ///     DateTime data type
        /// </summary>
        DateTime = 1,

        /// <summary>
        ///     Date data type
        /// </summary>
        Date = 2,

        /// <summary>
        ///     Time data type
        /// </summary>
        Time = 3,

        /// <summary>
        ///     Duration data type
        /// </summary>
        Duration = 4,

        /// <summary>
        ///     Phone number data type
        /// </summary>
        PhoneNumber = 5,

        /// <summary>
        ///     Currency data type
        /// </summary>
        Currency = 6,

        /// <summary>
        ///     Plain text data type
        /// </summary>
        Text = 7,

        /// <summary>
        ///     Html data type
        /// </summary>
        Html = 8,

        /// <summary>
        ///     Multiline text data type
        /// </summary>
        MultilineText = 9,

        /// <summary>
        ///     Email address data type
        /// </summary>
        EmailAddress = 10,

        /// <summary>
        ///     Password data type -- do not echo in UI
        /// </summary>
        Password = 11,

        /// <summary>
        ///     URL data type
        /// </summary>
        Url = 12,

        /// <summary>
        ///     URL to an Image -- to be displayed as an image instead of text
        /// </summary>
        ImageUrl = 13,

        /// <summary>
        ///     Credit card data type
        /// </summary>
        CreditCard = 14,

        /// <summary>
        ///     Postal code data type
        /// </summary>
        PostalCode = 15,

        /// <summary>
        ///     File upload data type
        /// </summary>
        Upload = 16
    }
}
