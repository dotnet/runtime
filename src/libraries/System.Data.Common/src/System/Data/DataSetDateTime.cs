// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Data
{
    /// <summary>
    /// Gets the DateTimeMode of a DateTime <see cref='System.Data.DataColumn'/> object.
    /// </summary>
    public enum DataSetDateTime
    {
        /// <summary>
        /// The datetime column in Local DateTimeMode stores datetime in Local. Adjusts Utc/Unspecified to Local. Serializes as Local
        /// </summary>
        Local = 1,
        /// <summary>
        /// The datetime column in Unspecified DateTimeMode stores datetime in Unspecified. Adjusts Local/Utc to Unspecified. Serializes as Unspecified with no offset across timezones
        /// </summary>
        Unspecified = 2,
        /// <summary>
        /// This is the default. The datetime column in UnspecifiedLocal DateTimeMode stores datetime in Unspecified. Adjusts Local/Utc to Unspecified. Serializes as Unspecified but applying offset across timezones
        /// </summary>
        UnspecifiedLocal = 3, //Unspecified while storing and Local when serializing. -> DataSetDateTime.Unspecified | DataSetDateTime.Local
        /// <summary>
        /// The datetime column in Utc DateTimeMode  stores datetime in Utc. Adjusts Local/Unspecified to Utc. Serializes as Utc
        /// </summary>
        Utc = 4,
    }
}
