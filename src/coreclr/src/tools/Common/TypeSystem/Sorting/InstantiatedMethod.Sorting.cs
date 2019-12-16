// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Internal.TypeSystem
{
    // Functionality related to deterministic ordering of types
    partial class InstantiatedMethod
    {
        protected internal override int ClassCode => -873941872;

        protected internal override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var otherMethod = (InstantiatedMethod)other;
            int result = _instantiation.Length - otherMethod._instantiation.Length;
            if (result != 0)
                return result;

            result = comparer.Compare(_methodDef, otherMethod._methodDef);
            if (result != 0)
                return result;

            for (int i = 0; i < _instantiation.Length; i++)
            {
                result = comparer.Compare(_instantiation[i], otherMethod._instantiation[i]);
                if (result != 0)
                    break;
            }

            return result;
        }
    }
}
