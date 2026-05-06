// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Functionality related to deterministic ordering of types
    public partial class MethodSignature
    {
        internal int CompareTo(MethodSignature other, TypeSystemComparer comparer)
        {
            int result = _parameters.Length.CompareTo(other._parameters.Length);
            if (result != 0)
                return result;

            result = ((int)_flags).CompareTo((int)other._flags);
            if (result != 0)
                return result;

            result = _genericParameterCount.CompareTo(other._genericParameterCount);
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
                    return result;
            }

            if (_embeddedSignatureData == null || other._embeddedSignatureData == null)
                return (_embeddedSignatureData?.Length ?? 0).CompareTo(other._embeddedSignatureData?.Length ?? 0);

            result = _embeddedSignatureData.Length.CompareTo(other._embeddedSignatureData.Length);
            if (result != 0)
                return result;

            for (int i = 0; i < _embeddedSignatureData.Length; i++)
            {
                ref EmbeddedSignatureData thisData = ref _embeddedSignatureData[i];
                ref EmbeddedSignatureData otherData = ref other._embeddedSignatureData[i];

                result = string.CompareOrdinal(thisData.index, otherData.index);
                if (result != 0)
                    return result;

                result = ((int)thisData.kind).CompareTo((int)otherData.kind);
                if (result != 0)
                    return result;

                result = comparer.Compare(thisData.type, otherData.type);
                if (result != 0)
                    return result;
            }

            return 0;
        }
    }
}
