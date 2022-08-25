// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.IL;

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem.Interop
{
    public static class InteropTypes
    {
        public static MetadataType GetGC(TypeSystemContext context)
        {
            return context.SystemModule.GetKnownType("System", "GC");
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
            return context.SystemModule.GetKnownType("System.Runtime.InteropServices", "Variant");
        }

        public static bool IsSafeHandle(TypeSystemContext context, TypeDesc type)
        {
            return IsOrDerivesFromType(type, GetSafeHandle(context));
        }

        public static bool IsCriticalHandle(TypeSystemContext context, TypeDesc type)
        {
            return IsOrDerivesFromType(type, GetCriticalHandle(context));
        }

        private static bool IsCoreNamedType(TypeSystemContext context, TypeDesc type, string @namespace, string name)
        {
            return type is MetadataType mdType &&
                mdType.Name == name &&
                mdType.Namespace == @namespace &&
                mdType.Module == context.SystemModule;
        }

        public static bool IsHandleRef(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System.Runtime.InteropServices", "HandleRef");
        }

        public static bool IsSystemDateTime(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System", "DateTime");
        }

        public static bool IsStringBuilder(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System.Text", "StringBuilder");
        }

        public static bool IsSystemDecimal(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System", "Decimal");
        }

        public static bool IsSystemDelegate(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System", "Delegate");
        }

        public static bool IsSystemMulticastDelegate(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System", "MulticastDelegate");
        }

        public static bool IsSystemGuid(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System", "Guid");
        }

        public static bool IsSystemArgIterator(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System", "ArgIterator");
        }

        public static bool IsSystemSpan(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System", "Span`1");
        }

        public static bool IsSystemReadOnlySpan(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System", "ReadOnlySpan`1");
        }

        public static bool IsSystemNullable(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System", "Nullable`1");
        }

        public static bool IsSystemRuntimeIntrinsicsVector64T(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System.Runtime.Intrinsics", "Vector64`1");
        }

        public static bool IsSystemRuntimeIntrinsicsVector128T(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System.Runtime.Intrinsics", "Vector128`1");
        }

        public static bool IsSystemRuntimeIntrinsicsVector256T(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System.Runtime.Intrinsics", "Vector256`1");
        }

        public static bool IsSystemNumericsVectorT(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System.Numerics", "Vector`1");
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
