// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.IL;

namespace Internal.TypeSystem.Interop
{
    public static class InteropTypes
    {
        public static MetadataType GetGC(TypeSystemContext context)
        {
            return context.SystemModule.GetKnownType("System", "GC");
        }

        public static MetadataType GetType(TypeSystemContext context)
        {
            return context.SystemModule.GetKnownType("System", "Type");
        }

        public static MetadataType GetSafeHandle(TypeSystemContext context)
        {
            return context.SystemModule.GetKnownType("System.Runtime.InteropServices", "SafeHandle");
        }

        public static MetadataType GetCriticalHandle(TypeSystemContext context)
        {
            return context.SystemModule.GetKnownType("System.Runtime.InteropServices", "CriticalHandle");
        }

        public static MetadataType GetHandleRef(TypeSystemContext context)
        {
            return context.SystemModule.GetKnownType("System.Runtime.InteropServices", "HandleRef");
        }

        public static MetadataType GetPInvokeMarshal(TypeSystemContext context)
        {
            return context.SystemModule.GetKnownType("System.Runtime.InteropServices", "PInvokeMarshal");
        }

        public static MetadataType GetRuntimeHelpers(TypeSystemContext context)
        {
            return context.SystemModule.GetKnownType("System.Runtime.CompilerServices", "RuntimeHelpers");
        }

        public static MetadataType GetMarshal(TypeSystemContext context)
        {
            return context.SystemModule.GetKnownType("System.Runtime.InteropServices", "Marshal");
        }

        public static MetadataType GetMemoryMarshal(TypeSystemContext context)
        {
            return context.SystemModule.GetKnownType("System.Runtime.InteropServices", "MemoryMarshal");
        }

        public static MetadataType GetNativeFunctionPointerWrapper(TypeSystemContext context)
        {
            return context.SystemModule.GetKnownType("System.Runtime.InteropServices", "NativeFunctionPointerWrapper");
        }

        public static MetadataType GetMarshalDirectiveException(TypeSystemContext context)
        {
            return context.SystemModule.GetKnownType("System.Runtime.InteropServices", "MarshalDirectiveException");
        }

        public static MetadataType GetVariant(TypeSystemContext context)
        {
            return context.SystemModule.GetKnownType("System.Runtime.InteropServices.Marshalling", "ComVariant");
        }

        public static bool IsSafeHandle(TypeSystemContext context, TypeDesc type)
        {
            return IsOrDerivesFromType(type, GetSafeHandle(context));
        }

        public static bool IsCriticalHandle(TypeSystemContext context, TypeDesc type)
        {
            return IsOrDerivesFromType(type, GetCriticalHandle(context));
        }

        private static bool IsCoreNamedType(TypeSystemContext context, TypeDesc type, ReadOnlySpan<byte> @namespace, ReadOnlySpan<byte> name)
        {
            return type is MetadataType mdType &&
                mdType.U8Name.SequenceEqual(name) &&
                mdType.U8Namespace.SequenceEqual(@namespace) &&
                mdType.Module == context.SystemModule;
        }

        public static bool IsHandleRef(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System.Runtime.InteropServices"u8, "HandleRef"u8);
        }

        public static bool IsSystemDateTime(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System"u8, "DateTime"u8);
        }

        public static bool IsStringBuilder(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System.Text"u8, "StringBuilder"u8);
        }

        public static bool IsSystemDecimal(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System"u8, "Decimal"u8);
        }

        public static bool IsSystemDelegate(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System"u8, "Delegate"u8);
        }

        public static bool IsSystemMulticastDelegate(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System"u8, "MulticastDelegate"u8);
        }

        public static bool IsSystemGuid(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System"u8, "Guid"u8);
        }

        public static bool IsSystemArgIterator(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System"u8, "ArgIterator"u8);
        }

        public static bool IsSystemSpan(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System"u8, "Span`1"u8);
        }

        public static bool IsSystemReadOnlySpan(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System"u8, "ReadOnlySpan`1"u8);
        }

        public static bool IsSystemNullable(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System"u8, "Nullable`1"u8);
        }

        public static bool IsSystemRuntimeIntrinsicsVector64T(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System.Runtime.Intrinsics"u8, "Vector64`1"u8);
        }

        public static bool IsSystemRuntimeIntrinsicsVector128T(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System.Runtime.Intrinsics"u8, "Vector128`1"u8);
        }

        public static bool IsSystemRuntimeIntrinsicsVector256T(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System.Runtime.Intrinsics"u8, "Vector256`1"u8);
        }

        public static bool IsSystemRuntimeIntrinsicsVector512T(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System.Runtime.Intrinsics"u8, "Vector512`1"u8);
        }

        public static bool IsSystemNumericsVectorT(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System.Numerics"u8, "Vector`1"u8);
        }

        private static bool IsOrDerivesFromType(TypeDesc type, MetadataType targetType)
        {
            while (type != null)
            {
                if (type == targetType)
                    return true;
                type = type.BaseType;
            }
            return false;
        }
    }
}
