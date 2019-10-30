// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.TypeLoading
{
    // This helper class represents a combined return value info and parameter info array. It is motivated by the fact that
    // the Reflection api surfaces "return types" as a "ParameterInfo at position -1." (and that the Ecma-335 metadata
    // scheme similarly reuses the Parameter table for holding return type custom attributes and stuff.)
    //
    // Behaviorally, it acts like an array whose lower bound is -1 (where the "-1"th element is the "return" ParameterInfo).
    //
    // It also allows getting an array of the parameters (excluding the return value) without copying.
    //
    // The name is motivated by System.Reflection.Metadata using the name MethodSignature<T> for a similar concept and
    // that it's far shorter than "ParametersAndReturnType".
    internal sealed class MethodSig<T>
    {
        public T Return { get; private set; }
        public T[] Parameters { get; }

        public MethodSig(int parameterCount)
        {
            Debug.Assert(parameterCount >= 0);
            Parameters = new T[parameterCount];
        }

        public T this[int position]
        {
            get
            {
                Debug.Assert(position >= -1 && position < Parameters.Length);
                return position == -1 ? Return : Parameters[position];
            }

            set
            {
                Debug.Assert(position >= -1 && position < Parameters.Length);
                if (position == -1)
                {
                    Return = value;
                }
                else
                {
                    Parameters[position] = value;
                }
            }
        }
    }
}
