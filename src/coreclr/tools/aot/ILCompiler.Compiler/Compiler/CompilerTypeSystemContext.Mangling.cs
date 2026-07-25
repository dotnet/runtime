// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;

namespace ILCompiler
{
    // Contains functionality related to name mangling
    public partial class CompilerTypeSystemContext
    {
        private partial class BoxedValueType : IPrefixMangledType
        {
            TypeDesc IPrefixMangledType.BaseType
            {
                get
                {
                    return ValueTypeRepresented;
                }
            }

            ReadOnlySpan<byte> IPrefixMangledType.Prefix
            {
                get
                {
                    return "Boxed"u8;
                }
            }
        }

        private partial class GenericUnboxingThunk : IPrefixMangledMethod
        {
            MethodDesc IPrefixMangledMethod.BaseMethod
            {
                get
                {
                    return _targetMethod;
                }
            }

            ReadOnlySpan<byte> IPrefixMangledMethod.Prefix
            {
                get
                {
                    return "unbox"u8;
                }
            }
        }

        private partial class UnboxingThunk : IPrefixMangledMethod
        {
            MethodDesc IPrefixMangledMethod.BaseMethod
            {
                get
                {
                    return _targetMethod;
                }
            }

            ReadOnlySpan<byte> IPrefixMangledMethod.Prefix
            {
                get
                {
                    return "unbox"u8;
                }
            }
        }
    }
}
