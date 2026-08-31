// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Implementation for Instantiated type specific interface logic

    public sealed partial class InstantiatedType : MetadataType
    {
        private DefType[] _implementedInterfaces;

        private DefType[] InitializeImplementedInterfaces()
        {
            // TODO Add duplicate detection
            return (_implementedInterfaces = InstantiateTypeArray(_typeDef.ExplicitlyImplementedInterfaces, _instantiation, default(Instantiation)));
        }

        /// <summary>
        /// The interfaces explicitly declared as implemented by this InstantiatedType. Duplicates are not permitted
        /// </summary>
        public override DefType[] ExplicitlyImplementedInterfaces
        {
            get
            {
                if (_implementedInterfaces == null)
                    return InitializeImplementedInterfaces();
                return _implementedInterfaces;
            }
        }
    }
}
