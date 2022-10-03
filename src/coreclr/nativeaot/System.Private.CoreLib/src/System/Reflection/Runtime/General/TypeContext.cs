// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;

namespace System.Reflection.Runtime.General
{
    //
    // Passed as an argument to code that parses signatures or typespecs. Specifies the substitution values for ET_VAR and ET_MVAR elements inside the signature.
    // Both may be null if no generic parameters are expected.
    //

    internal struct TypeContext
    {
        internal TypeContext(RuntimeTypeInfo[] genericTypeArguments, RuntimeTypeInfo[] genericMethodArguments)
        {
            _genericTypeArguments = genericTypeArguments;
            _genericMethodArguments = genericMethodArguments;
        }

        internal RuntimeTypeInfo[] GenericTypeArguments
        {
            get
            {
                return _genericTypeArguments;
            }
        }

        internal RuntimeTypeInfo[] GenericMethodArguments
        {
            get
            {
                return _genericMethodArguments;
            }
        }

        private readonly RuntimeTypeInfo[] _genericTypeArguments;
        private readonly RuntimeTypeInfo[] _genericMethodArguments;
    }
}
