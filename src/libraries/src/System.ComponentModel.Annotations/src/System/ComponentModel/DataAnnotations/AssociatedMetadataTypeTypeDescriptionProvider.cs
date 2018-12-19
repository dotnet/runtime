// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    /// Extends the metadata information for a class by adding attributes and property
    /// information that is defined in an associated class.
    /// </summary>
    public class AssociatedMetadataTypeTypeDescriptionProvider : TypeDescriptionProvider
    {
        private Type _associatedMetadataType;

        /// <summary>
        /// Initializes a new instance of the System.ComponentModel.DataAnnotations.AssociatedMetadataTypeTypeDescriptionProvider
        /// class by using the specified type.
        /// </summary>
        /// <param name="type">The type for which the metadata provider is created.</param>
        public AssociatedMetadataTypeTypeDescriptionProvider(Type type)
            : base(TypeDescriptor.GetProvider(type))
        {
        }

        /// <summary>
        /// Initializes a new instance of the System.ComponentModel.DataAnnotations.AssociatedMetadataTypeTypeDescriptionProvider
        /// class by using the specified metadata provider type and associated type.
        /// </summary>
        /// <param name="type">The type for which the metadata provider is created.</param>
        /// <param name="associatedMetadataType">The associated type that contains the metadata.</param>
        /// <exception cref="System.ArgumentNullException">The value of associatedMetadataType is null.</exception>
        public AssociatedMetadataTypeTypeDescriptionProvider(Type type, Type associatedMetadataType)
            : this(type)
        {
            if (associatedMetadataType == null)
            {
                throw new ArgumentNullException(nameof(associatedMetadataType));
            }

            _associatedMetadataType = associatedMetadataType;
        }

        /// <summary>
        /// Gets a type descriptor for the specified type and object.
        /// </summary>
        /// <param name="objectType">The type of object to retrieve the type descriptor for.</param>
        /// <param name="instance">An instance of the type.</param>
        /// <returns>The descriptor that provides metadata for the type.</returns>
        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            ICustomTypeDescriptor baseDescriptor = base.GetTypeDescriptor(objectType, instance);
            return new AssociatedMetadataTypeTypeDescriptor(baseDescriptor, objectType, _associatedMetadataType);
        }
    }
}
