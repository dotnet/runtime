// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    // Functionality related to determinstic ordering of types
    partial class InstantiatedType
    {
        protected internal override int ClassCode => 1150020412;

        protected internal override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
        {
            var otherType = (InstantiatedType)other;

            int result = comparer.Compare(_typeDef, otherType._typeDef);
            if (result == 0)
            {
                Debug.Assert(_instantiation.Length == otherType._instantiation.Length);
                for (int i = 0; i < _instantiation.Length; i++)
                {
                    result = comparer.Compare(_instantiation[i], otherType._instantiation[i]);
                    if (result != 0)
                        break;
                }
            }

            return result;
        }
    }
}
