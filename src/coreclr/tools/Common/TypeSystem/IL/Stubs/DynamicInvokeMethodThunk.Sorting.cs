// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    // Functionality related to determinstic ordering of types
    partial class DynamicInvokeMethodThunk
    {
        protected override int ClassCode => -1980933220;

        protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            return CompareTo((DynamicInvokeMethodThunk)other);
        }

        private int CompareTo(DynamicInvokeMethodThunk otherMethod)
        {
            int result = _targetSignature.Length - otherMethod._targetSignature.Length;
            if (result != 0)
                return result;

            DynamicInvokeMethodParameterKind thisReturnType = _targetSignature.ReturnType;
            result = (int)thisReturnType - (int)otherMethod._targetSignature.ReturnType;
            if (result != 0)
                return result;

            result = _targetSignature.GetNumerOfReturnTypePointerIndirections() - otherMethod._targetSignature.GetNumerOfReturnTypePointerIndirections();
            if (result != 0)
                return result;

            for (int i = 0; i < _targetSignature.Length; i++)
            {
                DynamicInvokeMethodParameterKind thisParamType = _targetSignature[i];
                result = (int)thisParamType - (int)otherMethod._targetSignature[i];
                if (result != 0)
                    return result;

                result = _targetSignature.GetNumberOfParameterPointerIndirections(i) - otherMethod._targetSignature.GetNumberOfParameterPointerIndirections(i);
                if (result != 0)
                    return result;
            }

            Debug.Assert(this == otherMethod);
            return 0;
        }

        partial class DynamicInvokeThunkGenericParameter
        {
            protected override int ClassCode => -234393261;

            protected override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
            {
                var otherType = (DynamicInvokeThunkGenericParameter)other;
                int result = Index - otherType.Index;
                if (result != 0)
                    return result;

                return _owningMethod.CompareTo(otherType._owningMethod);
            }
        }
    }
}
