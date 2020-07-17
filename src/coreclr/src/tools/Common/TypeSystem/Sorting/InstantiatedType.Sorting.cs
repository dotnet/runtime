// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            // Sort by instantiation before sorting by associated method definition
            // The goal of this is to keep methods which work with the same types near
            // to each other. This is a better heuristic than sorting by method definition
            // then by instantiation.
            //
            // The goal is to sort classes like SomeClass<UserStruct>, 
            // near SomeOtherClass<UserStruct, int>

            int result = 0;
            // Sort instantiations of the same type together
            for (int i = 0; i < _instantiation.Length; i++)
            {
                if (i >= otherType._instantiation.Length)
                    return 1;
                result = comparer.Compare(_instantiation[i], otherType._instantiation[i]);
                if (result != 0)
                    return result;
            }
            if (_instantiation.Length < otherType._instantiation.Length)
                return -1;

            return comparer.Compare(_typeDef, otherType._typeDef);
        }
    }
}
