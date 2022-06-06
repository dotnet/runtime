// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

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

            if (result != 0)
                return result;

            bool thisHasModopts = this.TryGetCallConvModOpts(out EmbeddedSignatureData[] thisModoptData);
            bool otherHasModopts = other.TryGetCallConvModOpts(out EmbeddedSignatureData[] otherModoptData);

            result = System.Collections.Generic.Comparer<bool>.Default.Compare(thisHasModopts, otherHasModopts);
            if (result != 0)
                return result;

            if (!(thisModoptData == null && otherModoptData == null))
            {
                result = thisModoptData.Length - otherModoptData.Length;
                if (result != 0)
                    return result;

                System.Array.Sort(thisModoptData, comparer.Compare);
                System.Array.Sort(otherModoptData, comparer.Compare);
                for (int i = 0; i < thisModoptData.Length; i++)
                {
                    for (int j = 0; j < otherModoptData.Length; j++)
                    {
                        result = comparer.Compare(thisModoptData[i], otherModoptData[j]);
                        if (result != 0)
                        {
                            return result;
                        }
                    }
                }
            }

            return result;
        }
    }

    partial struct EmbeddedSignatureData
    {
        public int CompareTo(EmbeddedSignatureData other, TypeSystemComparer comparer)
        {
            int result = string.Compare(index, other.index, System.StringComparison.InvariantCulture);
            if (result != 0)
                return result;

            result = (int)kind - (int)other.kind;
            if (result != 0)
                return result;

            result = comparer.Compare(type, other.type);
            if (result != 0)
                return result;

            return result;
        }

    }
}
