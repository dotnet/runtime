// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;

using Internal.TypeSystem.Ecma;

namespace Internal.TypeSystem
{
    internal static unsafe class CallConvHelpers
    {
        /// <summary>
        /// Gets a value indicating whether the method has the SuppressGCTransition attribute
        /// </summary>
        public static bool HasSuppressGCTransitionAttribute(this MethodDesc method)
        {
            Debug.Assert(method.IsPInvoke);

            if (method is Internal.IL.Stubs.PInvokeTargetNativeMethod rawPinvoke)
                method = rawPinvoke.Target;

            // Check SuppressGCTransition attribute
            return method.HasCustomAttribute("System.Runtime.InteropServices", "SuppressGCTransitionAttribute");
        }

        /// <summary>
        /// Gets a value indicating whether GC transition should be suppressed on the given p/invoke.
        /// </summary>
        public static bool IsSuppressGCTransition(this MethodDesc method)
        {
            Debug.Assert(method.IsPInvoke);

            // Check SuppressGCTransition attribute
            if (method.HasSuppressGCTransitionAttribute())
                return true;

            MethodSignatureFlags unmanagedCallConv = method.GetPInvokeMethodMetadata().Flags.UnmanagedCallingConvention;
            if (unmanagedCallConv != MethodSignatureFlags.None)
                return false;

            if (!(method is Internal.TypeSystem.Ecma.EcmaMethod ecmaMethod))
                return false;

            // Check UnmanagedCallConv attribute
            System.Reflection.Metadata.CustomAttributeValue<TypeDesc>? unmanagedCallConvAttribute = ecmaMethod.GetDecodedCustomAttribute("System.Runtime.InteropServices", "UnmanagedCallConvAttribute");
            if (unmanagedCallConvAttribute == null)
                return false;

            foreach (DefType defType in unmanagedCallConvAttribute.Value.EnumerateCallConvsFromAttribute())
            {
                if (defType.Name == "CallConvSuppressGCTransition")
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<DefType> EnumerateCallConvsFromAttribute(this CustomAttributeValue<TypeDesc> attributeWithCallConvsArray)
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
