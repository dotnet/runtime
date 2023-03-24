// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public abstract partial class ParameterizedType : TypeDesc
    {
        private TypeDesc _parameterType;

        internal ParameterizedType(TypeDesc parameterType)
        {
            _parameterType = parameterType;
        }

        public TypeDesc ParameterType
        {
            get
            {
                return _parameterType;
            }
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _parameterType.Context;
            }
        }
    }
}
