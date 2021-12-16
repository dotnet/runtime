// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using global::System;
using global::System.Reflection;
using global::System.Collections.Generic;

using global::Internal.Runtime.Augments;

using global::Internal.Reflection.Core;
using global::Internal.Reflection.Core.Execution;
using global::Internal.Reflection.Execution.MethodInvokers;

using global::System.Runtime.CompilerServices;
using global::System.Runtime.InteropServices;

using global::Internal.Runtime;

using Debug = System.Diagnostics.Debug;

namespace Internal.Reflection.Execution
{
    internal sealed partial class ExecutionEnvironmentImplementation : ExecutionEnvironment
    {
        private struct DynamicInvokeMapEntry
        {
            public const uint IsImportMethodFlag = 0x40000000;
            public const uint InstantiationDetailIndexMask = 0x3FFFFFFF;
        }

        private struct VirtualInvokeTableEntry
        {
            public const int GenericVirtualMethod = 1;
            public const int FlagsMask = 1;
        }

        private static class FieldAccessFlags
        {
            public const int RemoteStaticFieldRVA = unchecked((int)0x80000000);
        }

        /// <summary>
        /// This structure describes one static field in an external module. It is represented
        /// by an indirection cell pointer and an offset within the cell - the final address
        /// of the static field is essentially *IndirectionCell + Offset.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct RemoteStaticFieldDescriptor
        {
            public unsafe IntPtr* IndirectionCell;
            public int Offset;
        }
    }
}
