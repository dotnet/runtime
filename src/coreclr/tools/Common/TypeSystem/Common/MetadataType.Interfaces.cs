// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Api surface definition for interfaces that all MetadataTypes must implement

    public abstract partial class MetadataType : DefType
    {
        /// <summary>
        /// The interfaces explicitly declared as implemented by this MetadataType in the type's metadata.
        /// These correspond to the InterfaceImpls of a type in metadata
        /// </summary>
        public abstract DefType[] ExplicitlyImplementedInterfaces
        {
            get;
        }
    }
}
