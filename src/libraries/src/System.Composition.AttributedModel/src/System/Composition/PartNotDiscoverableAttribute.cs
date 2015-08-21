// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Composition
{
    /// <summary>
    /// Place on a type that should not be discovered as a MEF part.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PartNotDiscoverableAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PartNotDiscoverableAttribute"/> class.
        /// </summary>
        public PartNotDiscoverableAttribute()
        {
        }
    }
}
