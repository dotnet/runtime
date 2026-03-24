// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Stub for Internal.TypeSystem.Interop.InteropTypes which is not available in ILTrim's TypeSystem.
// ILTrim doesn't need full interop analysis; these stubs allow the shared dataflow code to compile.

using Internal.TypeSystem;

namespace Internal.TypeSystem.Interop
{
    internal static class InteropTypes
    {
        public static bool IsStringBuilder(TypeSystemContext context, TypeDesc type) => false;
        public static bool IsCriticalHandle(TypeSystemContext context, TypeDesc type) => false;
        public static bool IsSafeHandle(TypeSystemContext context, TypeDesc type) => false;
    }
}
