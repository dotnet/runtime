// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Internal.Reflection.Augments;
using Internal.Runtime;
using Internal.Runtime.CompilerServices;

namespace System
{
    [CLSCompliant(false)]
    [StructLayout(LayoutKind.Sequential)]
    public ref struct TypedReference
    {
        // Do not change the ordering of these fields. The JIT has a dependency on this layout.
        private readonly ref byte _value;
        private readonly RuntimeTypeHandle _typeHandle;

        private TypedReference(object target, int offset, RuntimeTypeHandle typeHandle)
        {
            _value = ref Unsafe.Add<byte>(ref target.GetRawData(), offset);
            _typeHandle = typeHandle;
        }

        public static TypedReference MakeTypedReference(object target, FieldInfo[] flds)
        {
            Type type;
            int offset;
            ReflectionAugments.ReflectionCoreCallbacks.MakeTypedReference(target, flds, out type, out offset);
            return new TypedReference(target, offset, type.TypeHandle);
        }

        public static Type? GetTargetType(TypedReference value) => Type.GetTypeFromHandle(value._typeHandle);

        public static RuntimeTypeHandle TargetTypeToken(TypedReference value)
        {
            if (value._typeHandle.IsNull)
                throw new NullReferenceException(); // For compatibility;
            return value._typeHandle;
        }

        internal static RuntimeTypeHandle RawTargetTypeToken(TypedReference value)
        {
            return value._typeHandle;
        }

        public static unsafe object ToObject(TypedReference value)
        {
            RuntimeTypeHandle typeHandle = value._typeHandle;
            if (typeHandle.IsNull)
                ThrowHelper.ThrowArgumentException_ArgumentNull_TypedRefType();

            MethodTable* eeType = typeHandle.ToMethodTable();
            if (eeType->IsValueType)
            {
                return RuntimeImports.RhBox(eeType, ref value.Value);
            }
            else if (eeType->IsPointer || eeType->IsFunctionPointer)
            {
                return RuntimeImports.RhBox(MethodTable.Of<UIntPtr>(), ref value.Value);
            }
            else
            {
                return Unsafe.As<byte, object>(ref value.Value);
            }
        }

        public static void SetTypedReference(TypedReference target, object? value) { throw new NotSupportedException(); }

        public override bool Equals(object? o) { throw new NotSupportedException(SR.NotSupported_NYI); }
        public override int GetHashCode() => _typeHandle.IsNull ? 0 : _typeHandle.GetHashCode();

        internal bool IsNull => _typeHandle.IsNull;

        internal ref byte Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return ref _value;
            }
        }
    }
}
