// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.Composition
{
    /// <summary>
    ///     Specifies that a constructor should be used when constructing an attributed part.
    /// </summary>
    /// <remarks>
    ///     By default, only a default parameter-less constructor, if available, is used to 
    ///     construct an attributed part. Use this attribute to indicate that a specific constructor 
    ///     should be used.
    /// </remarks>
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public class ImportingConstructorAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ImportingConstructorAttribute"/> class.
        /// </summary>
        public ImportingConstructorAttribute()
        {
        }
    }
}
