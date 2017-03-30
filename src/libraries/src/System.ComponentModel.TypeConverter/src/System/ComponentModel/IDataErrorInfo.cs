// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.ComponentModel
{
    /// <summary>
    /// </summary>
    // suppose that you have some data that can be indexed by use of string:
    // then there are two types of errors:
    // 1. an error for each piece of data that can be indexed
    // 2. an error that is valid on the entire data
    //
    public interface IDataErrorInfo
    {
        /// <summary>
        /// </summary>
        string this[string columnName]
        {
            get;
        }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        string Error
        {
            get;
        }
    }
}
