// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Functionality related to deterministic ordering of types
    public partial class InstantiatedMethod
    {
        protected internal override int ClassCode => -873941872;

        protected internal override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var otherMethod = (InstantiatedMethod)other;
            // Sort by instantiation before sorting by associated method definition
            // The goal of this is to keep methods which work with the same types near
            // to each other. This is a better heuristic than sorting by method definition
            // then by instantiation.
            //
            // The goal is to sort methods like SomeClass.SomeMethod<UserStruct>,
            // near SomeOtherClass.SomeOtherMethod<UserStruct, int>
            int result;
            // Sort instantiations of the same type together
            for (int i = 0; i < _instantiation.Length; i++)
            {
                if (i >= otherMethod._instantiation.Length)
                    return 1;
                result = comparer.Compare(_instantiation[i], otherMethod._instantiation[i]);
                if (result != 0)
                    return result;
            }
            if (_instantiation.Length < otherMethod._instantiation.Length)
                return -1;

            result = comparer.Compare(_methodDef, otherMethod._methodDef);
            return result;
        }
    }
}
