// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public struct PropertySignature
    {
        private TypeDesc[] _parameters;

        public readonly bool IsStatic;

        public readonly TypeDesc ReturnType;

        [System.Runtime.CompilerServices.IndexerName("Parameter")]
        public TypeDesc this[int index]
        {
            get
            {
                return _parameters[index];
            }
        }

        public int Length
        {
            get
            {
                return _parameters.Length;
            }
        }

        public PropertySignature(bool isStatic, TypeDesc[] parameters, TypeDesc returnType)
        {
            IsStatic = isStatic;
            _parameters = parameters;
            ReturnType = returnType;
        }
    }
}
