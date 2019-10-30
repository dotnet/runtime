// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Internal.TypeSystem
{
    partial class TypeDesc
    {
        /// <summary>
        /// Gets a value indicating whether this is a type that needs to be treated
        /// specially.
        /// </summary>
        public bool IsIntrinsic
        {
            get
            {
                return (GetTypeFlags(TypeFlags.IsIntrinsic | TypeFlags.AttributeCacheComputed) & TypeFlags.IsIntrinsic) != 0;
            }
        }
    }

    partial class InstantiatedType
    {
        partial void AddComputedIntrinsicFlag(ref TypeFlags flags)
        {
            if (_typeDef.IsIntrinsic)
                flags |= TypeFlags.IsIntrinsic;
        }
    }
}
