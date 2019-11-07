// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.TypeLoading
{
    //
    // Passed as an argument to code that parses signatures or typespecs. Specifies the subsitution values for ET_VAR and ET_MVAR elements inside the signature.
    // Both may be null if no generic parameters are expected.
    //
    internal readonly struct TypeContext
    {
        internal TypeContext(RoType[] genericTypeArguments, RoType[] genericMethodArguments)
        {
            GenericTypeArguments = genericTypeArguments;
            GenericMethodArguments = genericMethodArguments;
        }

        public RoType[] GenericTypeArguments { get; }
        public RoType[] GenericMethodArguments { get; }

        public RoType GetGenericTypeArgumentOrNull(int index)
        {
            if (GenericTypeArguments == null || ((uint)index) >= GenericTypeArguments.Length)
                return null;
            return GenericTypeArguments[index];
        }

        public RoType GetGenericMethodArgumentOrNull(int index)
        {
            if (GenericMethodArguments == null || ((uint)index) >= GenericMethodArguments.Length)
                return null;
            return GenericMethodArguments[index];
        }
    }
}
