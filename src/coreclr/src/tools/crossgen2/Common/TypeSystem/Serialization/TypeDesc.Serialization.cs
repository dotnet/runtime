// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Internal.TypeSystem
{
    partial class TypeDesc
    {
        /// <summary>
        /// Gets a value indicating whether this type is serializable.
        /// </summary>
        public virtual bool IsSerializable
        {
            get
            {
                return false;
            }
        }
    }

    partial class InstantiatedType
    {
        public override bool IsSerializable
        {
            get
            {
                return _typeDef.IsSerializable;
            }
        }
    }
}
