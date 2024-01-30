// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Reflection.Metadata;

using Internal.IL;
using Internal.TypeSystem.Ecma;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Enumeration that represents a unified view of calling conventions
    /// irrespective of their metadata encoding.
    /// </summary>
    [Flags]
    public enum UnmanagedCallingConventions
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
        // Unmanaged            = 0x00000009, - this one is always translated to cdecl/stdcall

        // The ones higher than 0xF are defined by the type system
        Swift                   = 0x00000010
    }

    public static class CallingConventionExtensions
    {
        /// <summary>
        /// Gets calling conventions for a standalone ('calli') method signature.
        /// </summary>
        public static UnmanagedCallingConventions GetStandaloneMethodSignatureCallingConventions(this MethodSignature signature)
        {
            // If calling convention is anything but 'unmanaged', or there's no modifiers, we can bitcast to our enum and we're done.
            MethodSignatureFlags unmanagedCallconv = signature.Flags & MethodSignatureFlags.UnmanagedCallingConventionMask;
            if (unmanagedCallconv != MethodSignatureFlags.UnmanagedCallingConvention)
            {
                Debug.Assert((int)MethodSignatureFlags.UnmanagedCallingConventionCdecl == (int)UnmanagedCallingConventions.Cdecl
                    && (int)MethodSignatureFlags.UnmanagedCallingConventionStdCall == (int)UnmanagedCallingConventions.Stdcall
                    && (int)MethodSignatureFlags.UnmanagedCallingConventionThisCall == (int)UnmanagedCallingConventions.Thiscall);
                Debug.Assert(unmanagedCallconv != 0);
                return (UnmanagedCallingConventions)unmanagedCallconv;
            }

            // If calling convention is 'unmanaged', there might be more metadata in the custom modifiers.
            UnmanagedCallingConventions result = 0;

            if (signature.HasEmbeddedSignatureData)
            {
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
            }

            // If we haven't found a calling convention in the modifiers, the calling convention is 'unmanaged'.
            if ((result & UnmanagedCallingConventions.CallingConventionMask) == 0)
            {
                result |= GetPlatformDefaultUnmanagedCallingConvention(signature.Context);
            }

            return result;
        }

        public static UnmanagedCallingConventions GetUnmanagedCallersOnlyMethodCallingConventions(this MethodDesc method)
        {
            Debug.Assert(method.IsUnmanagedCallersOnly);
            CustomAttributeValue<TypeDesc> unmanagedCallersOnlyAttribute = ((EcmaMethod)method).GetDecodedCustomAttribute("System.Runtime.InteropServices", "UnmanagedCallersOnlyAttribute").Value;
            return GetUnmanagedCallingConventionFromAttribute(unmanagedCallersOnlyAttribute, method.Context) & ~UnmanagedCallingConventions.IsSuppressGcTransition;
        }

        public static UnmanagedCallingConventions GetPInvokeMethodCallingConventions(this MethodDesc method)
        {
            Debug.Assert(method.IsPInvoke);

            UnmanagedCallingConventions result;

            if (method is Internal.IL.Stubs.PInvokeTargetNativeMethod pinvokeTarget)
                method = pinvokeTarget.Target;

            MethodSignatureFlags unmanagedCallConv = method.GetPInvokeMethodMetadata().Flags.UnmanagedCallingConvention;
            if (unmanagedCallConv != MethodSignatureFlags.None)
            {
                Debug.Assert((int)MethodSignatureFlags.UnmanagedCallingConventionCdecl == (int)UnmanagedCallingConventions.Cdecl
                    && (int)MethodSignatureFlags.UnmanagedCallingConventionStdCall == (int)UnmanagedCallingConventions.Stdcall
                    && (int)MethodSignatureFlags.UnmanagedCallingConventionThisCall == (int)UnmanagedCallingConventions.Thiscall);
                result = (UnmanagedCallingConventions)unmanagedCallConv;
            }
            else
            {
                CustomAttributeValue<TypeDesc>? unmanagedCallConvAttribute = ((EcmaMethod)method).GetDecodedCustomAttribute("System.Runtime.InteropServices", "UnmanagedCallConvAttribute");
                if (unmanagedCallConvAttribute != null)
                {
                    result = GetUnmanagedCallingConventionFromAttribute(unmanagedCallConvAttribute.Value, method.Context);
                }
                else
                {
                    result = GetPlatformDefaultUnmanagedCallingConvention(method.Context);
                }
            }

            if (method.HasCustomAttribute("System.Runtime.InteropServices", "SuppressGCTransitionAttribute"))
                result |= UnmanagedCallingConventions.IsSuppressGcTransition;

            return result;
        }

        public static UnmanagedCallingConventions GetDelegateCallingConventions(this TypeDesc delegateType)
        {
            Debug.Assert(delegateType.IsDelegate);

            if (delegateType is EcmaType ecmaDelegate)
            {
                MethodSignatureFlags unmanagedCallConv = ecmaDelegate.GetDelegatePInvokeFlags().UnmanagedCallingConvention;
                if (unmanagedCallConv != MethodSignatureFlags.None)
                {
                    Debug.Assert((int)MethodSignatureFlags.UnmanagedCallingConventionCdecl == (int)UnmanagedCallingConventions.Cdecl
                        && (int)MethodSignatureFlags.UnmanagedCallingConventionStdCall == (int)UnmanagedCallingConventions.Stdcall
                        && (int)MethodSignatureFlags.UnmanagedCallingConventionThisCall == (int)UnmanagedCallingConventions.Thiscall);
                    return (UnmanagedCallingConventions)unmanagedCallConv;
                }
            }

            return GetPlatformDefaultUnmanagedCallingConvention(delegateType.Context);
        }

        private static UnmanagedCallingConventions GetUnmanagedCallingConventionFromAttribute(CustomAttributeValue<TypeDesc> attributeWithCallConvsArray, TypeSystemContext context)
        {
            ImmutableArray<CustomAttributeTypedArgument<TypeDesc>> callConvArray = default;
            foreach (var arg in attributeWithCallConvsArray.NamedArguments)
            {
                if (arg.Name == "CallConvs")
                {
                    callConvArray = (ImmutableArray<CustomAttributeTypedArgument<TypeDesc>>)arg.Value;
                }
            }

            UnmanagedCallingConventions result = 0;

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
            if ((result & UnmanagedCallingConventions.CallingConventionMask) == 0)
            {
                result |= GetPlatformDefaultUnmanagedCallingConvention(context);
            }

            return result;
        }

        private static UnmanagedCallingConventions AccumulateCallingConventions(UnmanagedCallingConventions existing, MetadataType newConvention)
        {
            if (newConvention.Namespace != "System.Runtime.CompilerServices")
                return existing;

            UnmanagedCallingConventions? addedCallConv = newConvention.Name switch
            {
                "CallConvCdecl" => UnmanagedCallingConventions.Cdecl,
                "CallConvStdcall" => UnmanagedCallingConventions.Stdcall,
                "CallConvFastcall" => UnmanagedCallingConventions.Fastcall,
                "CallConvThiscall" => UnmanagedCallingConventions.Thiscall,
                "CallConvSuppressGCTransition" => UnmanagedCallingConventions.IsSuppressGcTransition,
                "CallConvMemberFunction" => UnmanagedCallingConventions.IsMemberFunction,
                "CallConvSwift" => UnmanagedCallingConventions.Swift,
                _ => null
            };

            if (addedCallConv == null)
                return existing;

            // Do not allow accumulating additional calling conventions - only modifiers are allowed
            if ((addedCallConv.Value & UnmanagedCallingConventions.CallingConventionMask) != 0 && (existing & UnmanagedCallingConventions.CallingConventionMask) != 0)
                ThrowHelper.ThrowInvalidProgramException(ExceptionStringID.InvalidProgramMultipleCallConv);

            return existing | addedCallConv.Value;
        }

        public static EmbeddedSignatureData[] EncodeAsEmbeddedSignatureData(this UnmanagedCallingConventions callingConventions, TypeSystemContext context)
        {
            UnmanagedCallingConventions convention = (callingConventions & UnmanagedCallingConventions.CallingConventionMask);
            UnmanagedCallingConventions modifiers = (callingConventions & UnmanagedCallingConventions.ModifiersMask);

            UnmanagedCallingConventions platformDefault = GetPlatformDefaultUnmanagedCallingConvention(context);

            int count = ((convention != platformDefault) ? 1 : 0) + BitOperations.PopCount((uint)modifiers);

            if (count == 0)
                return null;

            EmbeddedSignatureData[] ret = new EmbeddedSignatureData[count];

            int index = 0;

            if (convention != platformDefault)
            {
                ret[index++] = CreateCallConvEmbeddedSignatureData(context, convention switch
                {
                    UnmanagedCallingConventions.Cdecl => "CallConvCdecl",
                    UnmanagedCallingConventions.Stdcall => "CallConvStdcall",
                    UnmanagedCallingConventions.Fastcall => "CallConvFastcall",
                    UnmanagedCallingConventions.Thiscall => "CallConvThiscall",
                    UnmanagedCallingConventions.Swift => "CallConvSwift",
                    _ => throw new InvalidProgramException()
                });
            }

            if ((modifiers & UnmanagedCallingConventions.IsMemberFunction) != 0)
                ret[index++] = CreateCallConvEmbeddedSignatureData(context, "CallConvMemberFunction");

            if ((modifiers & UnmanagedCallingConventions.IsSuppressGcTransition) != 0)
                ret[index++] = CreateCallConvEmbeddedSignatureData(context, "CallConvSuppressGCTransition");

            Debug.Assert(index == count);

            return ret;

            static EmbeddedSignatureData CreateCallConvEmbeddedSignatureData(TypeSystemContext context, string name)
                => new()
                {
                    index = MethodSignature.IndexOfCustomModifiersOnReturnType,
                    kind = EmbeddedSignatureDataKind.OptionalCustomModifier,
                    type = context.SystemModule.GetKnownType("System.Runtime.CompilerServices", name)
                };
        }

        private static UnmanagedCallingConventions GetPlatformDefaultUnmanagedCallingConvention(TypeSystemContext context)
            => context.Target.IsWindows ? UnmanagedCallingConventions.Stdcall : UnmanagedCallingConventions.Cdecl;
    }
}
