// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

using Internal.Reflection.Augments;
using Internal.Reflection.Core.NonPortable;
using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

namespace System
{
    public abstract partial class Type : MemberInfo, IReflect
    {
        [Intrinsic]
        public static unsafe Type? GetTypeFromHandle(RuntimeTypeHandle handle) => handle.IsNull ? null : GetTypeFromMethodTable(handle.ToMethodTable());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe RuntimeType GetTypeFromMethodTable(MethodTable* pMT)
        {
            // If we support the writable data section on MethodTables, the runtime type associated with the MethodTable
            // is cached there. If writable data is not supported, we need to do a lookup in the runtime type
            // unifier's hash table.
            if (MethodTable.SupportsWritableData)
            {
                ref RuntimeType? type = ref Unsafe.AsRef<RuntimeType?>(pMT->WritableData);
                return type ?? GetTypeFromMethodTableSlow(pMT);
            }
            else
            {
                return RuntimeTypeUnifier.GetRuntimeTypeForMethodTable(pMT);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe RuntimeType GetTypeFromMethodTableSlow(MethodTable* pMT)
        {
            // TODO: instead of fragmenting the frozen object heap, we should have our own allocator
            // for objects that live forever outside the GC heap.

            RuntimeType? type = null;
            RuntimeImports.RhAllocateNewObject(
                (IntPtr)MethodTable.Of<RuntimeType>(),
                (uint)GC_ALLOC_FLAGS.GC_ALLOC_PINNED_OBJECT_HEAP,
                Unsafe.AsPointer(ref type));

            if (type == null)
                throw new OutOfMemoryException();

            type.DangerousSetUnderlyingEEType(pMT);

            ref RuntimeType? runtimeTypeCache = ref Unsafe.AsRef<RuntimeType?>(pMT->WritableData);
            if (Interlocked.CompareExchange(ref runtimeTypeCache, type, null) == null)
            {
                // Create and leak a GC handle
                UnsafeGCHandle.Alloc(type);
            }

            return runtimeTypeCache;
        }

        //
        // This is a port of the desktop CLR's RuntimeType.FormatTypeName() routine. This routine is used by various Reflection ToString() methods
        // to display the name of a type. Do not use for any other purpose as it inherits some pretty quirky desktop behavior.
        //
        internal string FormatTypeNameForReflection()
        {
            // Legacy: this doesn't make sense, why use only Name for nested types but otherwise
            // ToString() which contains namespace.
            Type rootElementType = this;
            while (rootElementType.HasElementType)
                rootElementType = rootElementType.GetElementType()!;
            if (rootElementType.IsNested)
            {
                return Name!;
            }

            // Legacy: why removing "System"? Is it just because C# has keywords for these types?
            // If so why don't we change it to lower case to match the C# keyword casing?
            string typeName = ToString();
            if (typeName.StartsWith("System."))
            {
                if (rootElementType.IsPrimitive || rootElementType == typeof(void))
                {
                    typeName = typeName.Substring("System.".Length);
                }
            }
            return typeName;
        }

        [Intrinsic]
        [RequiresUnreferencedCode("The type might be removed")]
        public static Type GetType(string typeName) => GetType(typeName, throwOnError: false, ignoreCase: false);
        [Intrinsic]
        [RequiresUnreferencedCode("The type might be removed")]
        public static Type GetType(string typeName, bool throwOnError) => GetType(typeName, throwOnError: throwOnError, ignoreCase: false);
        [Intrinsic]
        [RequiresUnreferencedCode("The type might be removed")]
        public static Type GetType(string typeName, bool throwOnError, bool ignoreCase)
        {
            return TypeNameParser.GetType(typeName, throwOnError: throwOnError, ignoreCase: ignoreCase);
        }

        [Intrinsic]
        [RequiresUnreferencedCode("The type might be removed")]
        public static Type GetType(string typeName, Func<AssemblyName, Assembly?>? assemblyResolver, Func<Assembly?, string, bool, Type?>? typeResolver) => GetType(typeName, assemblyResolver, typeResolver, throwOnError: false, ignoreCase: false);
        [Intrinsic]
        [RequiresUnreferencedCode("The type might be removed")]
        public static Type GetType(string typeName, Func<AssemblyName, Assembly?>? assemblyResolver, Func<Assembly?, string, bool, Type?>? typeResolver, bool throwOnError) => GetType(typeName, assemblyResolver, typeResolver, throwOnError: throwOnError, ignoreCase: false);
        [Intrinsic]
        [RequiresUnreferencedCode("The type might be removed")]
        public static Type GetType(string typeName, Func<AssemblyName, Assembly?>? assemblyResolver, Func<Assembly?, string, bool, Type?>? typeResolver, bool throwOnError, bool ignoreCase)
        {
            return TypeNameParser.GetType(typeName, assemblyResolver, typeResolver, throwOnError: throwOnError, ignoreCase: ignoreCase);
        }
    }
}
