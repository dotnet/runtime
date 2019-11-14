// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Internal.TypeSystem
{
    // Functionality related to determinstic ordering of types
    partial class MethodSignature
    {
        internal int CompareTo(MethodSignature other, TypeSystemComparer comparer)
        {
            int result = _parameters.Length - other._parameters.Length;
            if (result != 0)
                return result;

            result = (int)_flags - (int)other._flags;
            if (result != 0)
                return result;

            result = _genericParameterCount - other._genericParameterCount;
            if (result != 0)
                return result;

            // Most expensive checks last

            result = comparer.Compare(_returnType, other._returnType);
            if (result != 0)
                return result;
            
            for (int i = 0; i < _parameters.Length; i++)
            {
                result = comparer.Compare(_parameters[i], other._parameters[i]);
                if (result != 0)
                    break;
            }

            return result;
        }
    }
}
