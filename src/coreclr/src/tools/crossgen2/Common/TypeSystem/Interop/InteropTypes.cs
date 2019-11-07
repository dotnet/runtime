// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public static MetadataType GetMissingMemberException(TypeSystemContext context)
        {
            return context.SystemModule.GetKnownType("System", "MissingMemberException");
        }

        public static MetadataType GetPInvokeMarshal(TypeSystemContext context)
        {
            return context.SystemModule.GetKnownType("System.Runtime.InteropServices", "PInvokeMarshal");
        }

        public static MetadataType GetMarshal(TypeSystemContext context)
        {
            return context.SystemModule.GetKnownType("System.Runtime.InteropServices", "Marshal");
        }

        public static MetadataType GetStubHelpers(TypeSystemContext context)
        {
            return context.SystemModule.GetKnownType("System.StubHelpers", "StubHelpers");
        }

        public static MetadataType GetNativeFunctionPointerWrapper(TypeSystemContext context)
        {
            return context.SystemModule.GetKnownType("System.Runtime.InteropServices", "NativeFunctionPointerWrapper");
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

        public static bool IsSystemGuid(TypeSystemContext context, TypeDesc type)
        {
            return IsCoreNamedType(context, type, "System", "Guid");
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
