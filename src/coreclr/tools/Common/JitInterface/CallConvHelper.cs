// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;

using Internal.TypeSystem;

namespace Internal.JitInterface
{
    internal static unsafe class CallConvHelper
    {
        internal static IEnumerable<DefType> EnumerateCallConvsFromAttribute(CustomAttributeValue<TypeDesc> attributeWithCallConvsArray)
        {
            ImmutableArray<CustomAttributeTypedArgument<TypeDesc>> callConvArray = default;
            foreach (var arg in attributeWithCallConvsArray.NamedArguments)
            {
                if (arg.Name == "CallConvs")
                {
                    callConvArray = (ImmutableArray<CustomAttributeTypedArgument<TypeDesc>>)arg.Value;
                }
            }

            // No calling convention was specified in the attribute
            if (callConvArray.IsDefault)
                yield break;

            foreach (CustomAttributeTypedArgument<TypeDesc> type in callConvArray)
            {
                if (!(type.Value is DefType defType))
                    continue;

                if (defType.Namespace != "System.Runtime.CompilerServices")
                    continue;

                yield return defType;
            }
        }
    }
}
