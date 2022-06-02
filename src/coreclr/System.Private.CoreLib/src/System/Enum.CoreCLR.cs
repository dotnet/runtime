// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public abstract partial class Enum
    {
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Enum_GetValuesAndNames")]
        private static partial void GetEnumValuesAndNames(QCallTypeHandle enumType, ObjectHandleOnStack values, ObjectHandleOnStack names, Interop.BOOL getNames);

        private static unsafe object InternalBoxEnum(RuntimeType enumType, long value)
        {
            // RuntimeType is an instance of ReflectClassBaseObject, which inherits from Object.
            // Its type layout is as follows (following the method table inherited from Object);
            //
            //   OBJECTREF m_keepalive;
            //   OBJECTREF m_cache;
            //   TypeHandle m_typeHandle;
            //
            // For more details, see src\vm\object.h.
            // We need to get the type handle, so first we get a data reference, which gives us a
            // reference to the start of the first data field (so m_keepalive), then shift
            // by sizeof(void*) * 2. This gets us to the start of the m_typeHandle field.
            ref byte dataRef = ref Unsafe.As<RawData>(enumType).Data;
            ref byte typeHandleRef = ref Unsafe.AddByteOffset(ref dataRef, (nuint)(uint)sizeof(void*) * 2);
            TypeHandle typeHandle = Unsafe.As<byte, TypeHandle>(ref typeHandleRef);

            // The type here will never be a type desc, so we can just reinterpret
            MethodTable* pMethodTable = typeHandle.AsMethodTable();

            uint numInstanceFieldBytes = pMethodTable->GetNumInstanceFieldBytes();
            byte* rawValue = ArgSlot.EndianessFixup(&value, numInstanceFieldBytes);

            object result = RuntimeHelpers.Box(pMethodTable, ref *rawValue)!;

            GC.KeepAlive(enumType);

            return result;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern CorElementType InternalGetCorElementType();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeType InternalGetUnderlyingType(RuntimeType enumType);

        private static EnumInfo GetEnumInfo(RuntimeType enumType, bool getNames = true)
        {
            EnumInfo? entry = enumType.GenericCache as EnumInfo;

            if (entry == null || (getNames && entry.Names == null))
            {
                ulong[]? values = null;
                string[]? names = null;
                RuntimeTypeHandle enumTypeHandle = enumType.TypeHandle;
                GetEnumValuesAndNames(
                    new QCallTypeHandle(ref enumTypeHandle),
                    ObjectHandleOnStack.Create(ref values),
                    ObjectHandleOnStack.Create(ref names),
                    getNames ? Interop.BOOL.TRUE : Interop.BOOL.FALSE);
                bool hasFlagsAttribute = enumType.IsDefined(typeof(FlagsAttribute), inherit: false);

                entry = new EnumInfo(hasFlagsAttribute, values!, names!);
                enumType.GenericCache = entry;
            }

            return entry;
        }
    }
}
