// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Composition
{
    /// <summary>
    ///     Specifies that an attribute can be used to provide metadata for a type, property, field,
    ///     or method marked with the <see cref="ExportAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class,
                    AllowMultiple = false, Inherited = true)]
    public sealed class MetadataAttributeAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MetadataAttributeAttribute"/> class.
        /// </summary>
        public MetadataAttributeAttribute()
        {
        }
    }
}
