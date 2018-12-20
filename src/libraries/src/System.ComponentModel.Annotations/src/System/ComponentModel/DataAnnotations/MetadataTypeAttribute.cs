// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    /// Specifies the metadata class to associate with a data model class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class MetadataTypeAttribute : Attribute
    {
        private Type _metadataClassType;

        /// <summary>
        /// Initializes a new instance of the System.ComponentModel.DataAnnotations.MetadataTypeAttribute
        /// class.
        /// </summary>
        /// <param name="metadataClassType">The metadata class to reference.</param>
        /// <exception cref="System.ArgumentNullException">metadataClassType is null.</exception>
        public MetadataTypeAttribute(Type metadataClassType)
        {
            _metadataClassType = metadataClassType;
        }

        /// <summary>
        /// Gets the metadata class that is associated with a data-model partial class.
        /// </summary>
        public Type MetadataClassType
        {
            get
            {
                if (_metadataClassType == null)
                {
                    throw new InvalidOperationException(SR.MetadataTypeAttribute_TypeCannotBeNull);
                }

                return _metadataClassType;
            }
        }
    }
}
