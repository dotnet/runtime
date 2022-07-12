// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;

using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Enumeration that represents a unified view of calling conventions
    /// irrespective of their metadata encoding.
    /// </summary>
    [Flags]
    public enum CallingConventions
    {
        //
        // Calling convention modifiers
        //
        ModifiersMask           = unchecked((int)0xFF000000),
        IsSuppressGcTransition  = 0x01000000,
        IsMemberFunction        = 0x02000000,

        //
        // Calling conventions
        //
        CallingConventionMask   = 0x00FFFFFF,

        // Keep the ones defined in MethodSignatureFlags bitcastable
        Cdecl                   = 0x00000001,
        Stdcall                 = 0x00000002,
        Thiscall                = 0x00000003,
        Fastcall                = 0x00000004,
        Varargs                 = 0x00000005,

        /// <summary>
        /// Platform-default unmanaged calling convention.
        /// </summary>
        Unmanaged               = 0x00000009,

        // The ones higher than 0xF are defined by the type system
        // There are no such calling conventions yet.
    }

    public static class CallingConventionExtensions
    {
        /// <summary>
        /// Gets calling conventions for a standalone ('calli') method signature.
        /// </summary>
        public static CallingConventions GetStandaloneMethodSignatureCallingConventions(this MethodSignature signature)
        {
            // If calling convention is anything but 'unmanaged', or there's no modifiers, we can bitcast to our enum and we're done.
            MethodSignatureFlags unmanagedCallconv = signature.Flags & MethodSignatureFlags.UnmanagedCallingConventionMask;
            if (unmanagedCallconv != MethodSignatureFlags.UnmanagedCallingConvention || !signature.HasEmbeddedSignatureData)
            {
                Debug.Assert((int)MethodSignatureFlags.UnmanagedCallingConventionCdecl == (int)CallingConventions.Cdecl
                    && (int)MethodSignatureFlags.UnmanagedCallingConventionStdCall == (int)CallingConventions.Stdcall
                    && (int)MethodSignatureFlags.UnmanagedCallingConventionThisCall == (int)CallingConventions.Thiscall);
                return (CallingConventions)unmanagedCallconv;
            }

            // If calling convention is 'unmanaged', there might be more metadata in the custom modifiers.
            CallingConventions result = 0;
            foreach (EmbeddedSignatureData data in signature.GetEmbeddedSignatureData())
            {
                if (data.kind != EmbeddedSignatureDataKind.OptionalCustomModifier)
                    continue;

                // We only care about the modifiers for the return type. These will be at the start of
                // the signature, so will be first in the array of embedded signature data.
                if (data.index != MethodSignature.IndexOfCustomModifiersOnReturnType)
                    break;

                if (data.type is not MetadataType mdType)
                    continue;

                result = AccumulateCallingConventions(result, mdType);
            }

            // If we haven't found a calling convention in the modifiers, the calling convention is 'unmanaged'.
            if ((result & CallingConventions.CallingConventionMask) == 0)
            {
                result |= CallingConventions.Unmanaged;
            }

            return result;
        }

        public static CallingConventions GetUnmanagedCallersOnlyMethodCallingConventions(this MethodDesc method)
        {
            Debug.Assert(method.IsUnmanagedCallersOnly);
            CustomAttributeValue<TypeDesc> unmanagedCallersOnlyAttribute = ((EcmaMethod)method).GetDecodedCustomAttribute("System.Runtime.InteropServices", "UnmanagedCallersOnlyAttribute").Value;
            return GetUnmanagedCallingConventionFromAttribute(unmanagedCallersOnlyAttribute) & ~CallingConventions.IsSuppressGcTransition;
        }

        public static CallingConventions GetPInvokeMethodCallingConventions(this MethodDesc method)
        {
            Debug.Assert(method.IsPInvoke);

            CallingConventions result;

            if (method is Internal.IL.Stubs.PInvokeTargetNativeMethod pinvokeTarget)
                method = pinvokeTarget.Target;

            MethodSignatureFlags unmanagedCallConv = method.GetPInvokeMethodMetadata().Flags.UnmanagedCallingConvention;
            if (unmanagedCallConv != MethodSignatureFlags.None)
            {
                Debug.Assert((int)MethodSignatureFlags.UnmanagedCallingConventionCdecl == (int)CallingConventions.Cdecl
                    && (int)MethodSignatureFlags.UnmanagedCallingConventionStdCall == (int)CallingConventions.Stdcall
                    && (int)MethodSignatureFlags.UnmanagedCallingConventionThisCall == (int)CallingConventions.Thiscall);
                result = (CallingConventions)unmanagedCallConv;
            }
            else
            {
                CustomAttributeValue<TypeDesc>? unmanagedCallConvAttribute = ((EcmaMethod)method).GetDecodedCustomAttribute("System.Runtime.InteropServices", "UnmanagedCallConvAttribute");
                if (unmanagedCallConvAttribute != null)
                {
                    result = GetUnmanagedCallingConventionFromAttribute(unmanagedCallConvAttribute.Value);
                }
                else
                {
                    result = CallingConventions.Unmanaged;
                }
            }

            if (method.HasCustomAttribute("System.Runtime.InteropServices", "SuppressGCTransitionAttribute"))
                result |= CallingConventions.IsSuppressGcTransition;

            return result;
        }

        private static CallingConventions GetUnmanagedCallingConventionFromAttribute(CustomAttributeValue<TypeDesc> attributeWithCallConvsArray)
        {
            ImmutableArray<CustomAttributeTypedArgument<TypeDesc>> callConvArray = default;
            foreach (var arg in attributeWithCallConvsArray.NamedArguments)
            {
                if (arg.Name == "CallConvs")
                {
                    callConvArray = (ImmutableArray<CustomAttributeTypedArgument<TypeDesc>>)arg.Value;
                }
            }

            CallingConventions result = 0;

            if (!callConvArray.IsDefault)
            {
                foreach (CustomAttributeTypedArgument<TypeDesc> type in callConvArray)
                {
                    if (type.Value is not MetadataType mdType)
                        continue;

                    result = AccumulateCallingConventions(result, mdType);
                }
            }

            // If we haven't found a calling convention in the attribute, the calling convention is 'unmanaged'.
            if ((result & CallingConventions.CallingConventionMask) == 0)
            {
                result |= CallingConventions.Unmanaged;
            }

            return result;
        }

        private static CallingConventions AccumulateCallingConventions(CallingConventions existing, MetadataType newConvention)
        {
            if (newConvention.Namespace != "System.Runtime.CompilerServices")
                return existing;

            CallingConventions? addedCallConv = newConvention.Name switch
            {
                "CallConvCdecl" => CallingConventions.Cdecl,
                "CallConvStdcall" => CallingConventions.Stdcall,
                "CallConvFastcall" => CallingConventions.Fastcall,
                "CallConvThiscall" => CallingConventions.Thiscall,
                "CallConvSuppressGCTransition" => CallingConventions.IsSuppressGcTransition,
                "CallConvMemberFunction" => CallingConventions.IsMemberFunction,
                _ => null
            };

            if (addedCallConv == null)
                return existing;

            // Do not allow accumulating additional calling conventions - only modifiers are allowed
            if ((addedCallConv.Value & CallingConventions.CallingConventionMask) != 0 && (existing & CallingConventions.CallingConventionMask) != 0)
                ThrowHelper.ThrowInvalidProgramException(ExceptionStringID.InvalidProgramMultipleCallConv);

            return existing | addedCallConv.Value;
        }
    }
}
