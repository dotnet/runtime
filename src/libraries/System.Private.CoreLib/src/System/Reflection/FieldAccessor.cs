// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Reflection
{
    internal sealed class FieldAccessor
    {
        private readonly RtFieldInfo _fieldInfo;
        private readonly IntPtr _addressOrOffset;
        private readonly unsafe MethodTable* _methodTable;
        private FieldAccessorType _fieldAccessType;

        internal FieldAccessor(FieldInfo fieldInfo)
        {
            _fieldInfo = (RtFieldInfo)fieldInfo;

            InitializeClass();

            Debug.Assert(_fieldInfo.m_declaringType != null);
            if (_fieldInfo.m_declaringType.ContainsGenericParameters ||
                _fieldInfo.m_declaringType.IsNullableOfT)
            {
                _fieldAccessType = FieldAccessorType.NoInvoke;
            }
            else
            {
                _fieldAccessType = FieldAccessorType.SlowPathUntilClassInitialized;

                if (RuntimeFieldHandle.IsFastPathSupported(_fieldInfo))
                {
                    // Initialize the readonly fields.
                    _addressOrOffset = _fieldInfo.IsStatic ?
                        RuntimeFieldHandle.GetStaticFieldAddress(_fieldInfo) :
                        RuntimeFieldHandle.GetInstanceFieldOffset(_fieldInfo);

                    unsafe
                    {
                        _methodTable = _fieldInfo.FieldType.IsFunctionPointer ?
                            (MethodTable*) typeof(IntPtr).TypeHandle.Value :
                            (MethodTable*) _fieldInfo.FieldType.TypeHandle.Value;
                    }
                }
            }
        }

        private void Initialize()
        {
            RuntimeType fieldType = (RuntimeType)_fieldInfo.FieldType;

            if (!RuntimeFieldHandle.IsFastPathSupported(_fieldInfo))
            {
                // Currently this is true for [ThreadStatic] cases, for fields added from EnC, and for fields on unloadable types.
                _fieldAccessType = FieldAccessorType.SlowPath;
            }
            else if (_fieldInfo.IsStatic)
            {
                if (fieldType.IsValueType)
                {
                    if (fieldType.IsEnum)
                    {
                        _fieldAccessType = GetPrimitiveAccessorTypeForStatic(fieldType.GetEnumUnderlyingType());
                    }
                    else if (RuntimeTypeHandle.GetCorElementType(fieldType) == CorElementType.ELEMENT_TYPE_VALUETYPE)
                    {
                        // The runtime stores non-primitive value types as a boxed value.
                        _fieldAccessType = FieldAccessorType.StaticValueTypeBoxed;
                    }
                    else
                    {
                        _fieldAccessType = GetPrimitiveAccessorTypeForStatic(fieldType);
                    }
                }
                else if (fieldType.IsPointer)
                {
                    _fieldAccessType = FieldAccessorType.StaticPointerType;
                }
                else if (fieldType.IsFunctionPointer)
                {
                    _fieldAccessType = GetIntPtrAccessorTypeForStatic();
                }
                else
                {
                    _fieldAccessType = FieldAccessorType.StaticReferenceType;
                }
            }
            else
            {
                if (fieldType.IsEnum)
                {
                    _fieldAccessType = GetPrimitiveAccessorTypeForInstance(fieldType.GetEnumUnderlyingType());
                }
                else if (fieldType.IsValueType)
                {
                    _fieldAccessType = GetPrimitiveAccessorTypeForInstance(fieldType);
                }
                else if (fieldType.IsPointer)
                {
                    _fieldAccessType = FieldAccessorType.InstancePointerType;
                }
                else if (fieldType.IsFunctionPointer)
                {
                    _fieldAccessType = GetIntPtrAccessorTypeForInstance();
                }
                else
                {
                    _fieldAccessType = FieldAccessorType.InstanceReferenceType;
                }
            }
        }

        public object? GetValue(object? obj)
        {
            unsafe
            {
                switch (_fieldAccessType)
                {
                    case FieldAccessorType.InstanceReferenceType:
                        VerifyTarget(obj);
                        Debug.Assert(obj != null);
                        return Volatile.Read(ref Unsafe.As<byte, object>(ref Unsafe.AddByteOffset(ref obj.GetRawData(), _addressOrOffset)));

                    case FieldAccessorType.InstanceValueType:
                    case FieldAccessorType.InstanceValueTypeSize1:
                    case FieldAccessorType.InstanceValueTypeSize2:
                    case FieldAccessorType.InstanceValueTypeSize4:
                    case FieldAccessorType.InstanceValueTypeSize8:
                        VerifyTarget(obj);
                        Debug.Assert(obj != null);
                        return RuntimeHelpers.Box(
                            _methodTable,
                            ref Unsafe.AddByteOffset(ref obj.GetRawData(), _addressOrOffset));

                    case FieldAccessorType.InstancePointerType:
                        VerifyTarget(obj);
                        Debug.Assert(obj != null);
                        return Pointer.Box(
                            (void*)Unsafe.As<byte, IntPtr>(ref Unsafe.AddByteOffset(ref obj.GetRawData(), _addressOrOffset)),
                            _fieldInfo.FieldType);

                    case FieldAccessorType.StaticReferenceType:
                        return Volatile.Read(ref Unsafe.As<IntPtr, object>(ref *(IntPtr*)_addressOrOffset));

                    case FieldAccessorType.StaticValueType:
                    case FieldAccessorType.StaticValueTypeSize1:
                    case FieldAccessorType.StaticValueTypeSize2:
                    case FieldAccessorType.StaticValueTypeSize4:
                    case FieldAccessorType.StaticValueTypeSize8:
                        return RuntimeHelpers.Box(_methodTable, ref Unsafe.AsRef<byte>(_addressOrOffset.ToPointer()));

                    case FieldAccessorType.StaticValueTypeBoxed:
                        // Re-box the value.
                        return RuntimeHelpers.Box(
                            _methodTable,
                            ref Unsafe.As<IntPtr, object>(ref *(IntPtr*)_addressOrOffset).GetRawData());

                    case FieldAccessorType.StaticPointerType:
                        return Pointer.Box((void*)Unsafe.As<byte, IntPtr>(
                            ref Unsafe.AsRef<byte>(_addressOrOffset.ToPointer())), _fieldInfo.FieldType);

                    case FieldAccessorType.SlowPathUntilClassInitialized:
                        if (!IsStatic())
                        {
                            VerifyTarget(obj);
                        }

                        object? ret = RuntimeFieldHandle.GetValue(_fieldInfo, obj, (RuntimeType)_fieldInfo.FieldType, _fieldInfo.m_declaringType, isInitialized: false);
                        Initialize();
                        return ret;

                    case FieldAccessorType.SlowPath:
                        if (!IsStatic())
                        {
                            VerifyTarget(obj);
                        }

                        return RuntimeFieldHandle.GetValue(_fieldInfo, obj, (RuntimeType)_fieldInfo.FieldType, _fieldInfo.m_declaringType, isInitialized: true);

                    case FieldAccessorType.NoInvoke:
                        if (_fieldInfo.DeclaringType is not null && _fieldInfo.DeclaringType.ContainsGenericParameters)
                            throw new InvalidOperationException(SR.Arg_UnboundGenField);

                        if (_fieldInfo.DeclaringType is not null && ((RuntimeType)_fieldInfo.FieldType).IsNullableOfT)
                            throw new NotSupportedException();

                        throw new FieldAccessException();

                    default:
                        Debug.Assert(false, "Unknown enum value");
                        return null;
                }
            }
        }

        public void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture)
        {
            unsafe
            {
                switch (_fieldAccessType)
                {
                    case FieldAccessorType.InstanceReferenceType:
                        VerifyInstanceField(obj, ref value, invokeAttr, binder, culture);
                        Debug.Assert(obj != null);
                        Volatile.Write(ref Unsafe.As<byte, object?>(ref Unsafe.AddByteOffset(ref obj.GetRawData(), _addressOrOffset)), value);
                        return;

                    case FieldAccessorType.InstanceValueTypeSize1:
                        VerifyInstanceField(obj, ref value, invokeAttr, binder, culture);
                        Debug.Assert(obj != null);
                        Volatile.Write(
                            ref Unsafe.AddByteOffset(ref obj.GetRawData(), _addressOrOffset),
                            value!.GetRawData());
                        return;

                    case FieldAccessorType.InstanceValueTypeSize2:
                        VerifyInstanceField(obj, ref value, invokeAttr, binder, culture);
                        Debug.Assert(obj != null);
                        Volatile.Write(
                            ref Unsafe.As<byte, short>(ref Unsafe.AddByteOffset(ref obj.GetRawData(), _addressOrOffset)),
                            Unsafe.As<byte, short>(ref value!.GetRawData()));
                        return;

                    case FieldAccessorType.InstanceValueTypeSize4:
                        VerifyInstanceField(obj, ref value, invokeAttr, binder, culture);
                        Debug.Assert(obj != null);
                        Volatile.Write(
                            ref Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref obj.GetRawData(), _addressOrOffset)),
                            Unsafe.As<byte, int>(ref value!.GetRawData()));
                        return;

                    case FieldAccessorType.InstanceValueTypeSize8:
                        VerifyInstanceField(obj, ref value, invokeAttr, binder, culture);
                        Debug.Assert(obj != null);
                        Volatile.Write(
                            ref Unsafe.As<byte, long>(ref Unsafe.AddByteOffset(ref obj.GetRawData(), _addressOrOffset)),
                            Unsafe.As<byte, long>(ref value!.GetRawData()));
                        return;

                    case FieldAccessorType.StaticReferenceType:
                        VerifyStaticField(ref value, invokeAttr, binder, culture);
                        Volatile.Write(ref Unsafe.As<IntPtr, object?>(ref *(IntPtr*)_addressOrOffset), value);
                        return;

                    case FieldAccessorType.StaticValueTypeSize1:
                        VerifyStaticField(ref value, invokeAttr, binder, culture);
                        Volatile.Write(
                            ref Unsafe.AsRef<byte>(_addressOrOffset.ToPointer()),
                            value!.GetRawData());
                        return;

                    case FieldAccessorType.StaticValueTypeSize2:
                        VerifyStaticField(ref value, invokeAttr, binder, culture);
                        Volatile.Write(
                            ref Unsafe.AsRef<short>(_addressOrOffset.ToPointer()),
                            Unsafe.As<byte, short>(ref value!.GetRawData()));
                        return;

                    case FieldAccessorType.StaticValueTypeSize4:
                        VerifyStaticField(ref value, invokeAttr, binder, culture);
                        Volatile.Write(
                            ref Unsafe.AsRef<int>(_addressOrOffset.ToPointer()),
                            Unsafe.As<byte, int>(ref value!.GetRawData()));
                        return;

                    case FieldAccessorType.StaticValueTypeSize8:
                        VerifyStaticField(ref value, invokeAttr, binder, culture);
                        Volatile.Write(
                            ref Unsafe.AsRef<long>(_addressOrOffset.ToPointer()),
                            Unsafe.As<byte, long>(ref value!.GetRawData()));
                        return;

                    case FieldAccessorType.SlowPathUntilClassInitialized:
                        {
                            if (IsStatic())
                            {
                                VerifyStaticField(ref value, invokeAttr, binder, culture);
                            }
                            else
                            {
                                VerifyInstanceField(obj, ref value, invokeAttr, binder, culture);
                            }

                            RuntimeFieldHandle.SetValue(_fieldInfo, obj, value, (RuntimeType)_fieldInfo.FieldType, _fieldInfo.m_declaringType, isInitialized: false);
                            Initialize();
                            return;
                        }

                    case FieldAccessorType.NoInvoke:
                        if (_fieldInfo.DeclaringType is not null && _fieldInfo.DeclaringType.ContainsGenericParameters)
                            throw new InvalidOperationException(SR.Arg_UnboundGenField);

                        throw new FieldAccessException();
                }
            }

            // Slow path
            if (IsStatic())
            {
                VerifyStaticField(ref value, invokeAttr, binder, culture);
            }
            else
            {
                VerifyInstanceField(obj, ref value, invokeAttr, binder, culture);
            }

            RuntimeFieldHandle.SetValue(_fieldInfo, obj, value, (RuntimeType)_fieldInfo.FieldType, _fieldInfo.m_declaringType, isInitialized: true);
        }

        private void InitializeClass()
        {
            if (_fieldInfo.DeclaringType is null)
            {
                RunModuleConstructor(_fieldInfo.Module);
            }
            else
            {
                RunClassConstructor(_fieldInfo);
            }

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2059:RunClassConstructor",
                Justification = "This represents the static constructor, so if this object was created, the static constructor exists.")]
            static void RunClassConstructor(FieldInfo fieldInfo)
            {
                RuntimeHelpers.RunClassConstructor(fieldInfo.DeclaringType!.TypeHandle);
            }

            static void RunModuleConstructor(Module module)
            {
                RuntimeHelpers.RunModuleConstructor(module.ModuleHandle);
            }
        }

        private bool IsStatic() => (_fieldInfo.Attributes & FieldAttributes.Static) == FieldAttributes.Static;

        private void VerifyStaticField(ref object? value, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture)
        {
            VerifyInitOnly();
            CheckValue(ref value, invokeAttr, binder, culture);
        }

        private void VerifyInstanceField(object? obj, ref object? value, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture)
        {
            VerifyTarget(obj);
            CheckValue(ref value, invokeAttr, binder, culture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void VerifyTarget(object? target)
        {
            Debug.Assert(!IsStatic());

            if (!_fieldInfo.m_declaringType.IsInstanceOfType(target))
            {
                if (target == null)
                {
                    ThrowHelperTargetException();
                }
                else
                {
                    ThrowHelperArgumentException(target, _fieldInfo);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckValue(ref object? value, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture)
        {
            if (value is null)
            {
                if (((RuntimeType)_fieldInfo.FieldType).IsActualValueType)
                {
                    ((RuntimeType)_fieldInfo.FieldType).CheckValue(ref value, binder, culture, invokeAttr);
                }
            }
            else if (!ReferenceEquals(value.GetType(), _fieldInfo.FieldType))
            {
                ((RuntimeType)_fieldInfo.FieldType).CheckValue(ref value, binder, culture, invokeAttr);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void VerifyInitOnly()
        {
            Debug.Assert(IsStatic());

            if ((_fieldInfo.Attributes & FieldAttributes.InitOnly) == FieldAttributes.InitOnly &&
                _fieldAccessType != FieldAccessorType.SlowPathUntilClassInitialized)
            {
                ThrowHelperFieldAccessException(_fieldInfo.Name, _fieldInfo.DeclaringType?.FullName);
            }
        }

        /// <summary>
        /// Currently we only optimize for primitive types and not all value types. Primitive types support atomic write operations, are
        /// not boxed by the runtime when stored as a static field, and don't need special nullable, GC or alignment checks.
        /// </summary>
        private static FieldAccessorType GetPrimitiveAccessorTypeForInstance(Type fieldType)
        {
            FieldAccessorType accessorType = FieldAccessorType.InstanceValueType;

            if (fieldType == typeof(byte) ||
                fieldType == typeof(sbyte) ||
                fieldType == typeof(bool))
                accessorType = FieldAccessorType.InstanceValueTypeSize1;
            else if (fieldType == typeof(short) ||
                fieldType == typeof(ushort) ||
                fieldType == typeof(char))
                accessorType = FieldAccessorType.InstanceValueTypeSize2;
            else if (fieldType == typeof(int) ||
                fieldType == typeof(uint) ||
                fieldType == typeof(float))
                accessorType = FieldAccessorType.InstanceValueTypeSize4;
            else if (fieldType == typeof(long) ||
                fieldType == typeof(ulong) ||
                fieldType == typeof(double))
                accessorType = FieldAccessorType.InstanceValueTypeSize8;
            else if (fieldType == typeof(IntPtr) ||
                fieldType == typeof(UIntPtr))
                accessorType = GetIntPtrAccessorTypeForInstance();

            return accessorType;
        }

        private static FieldAccessorType GetPrimitiveAccessorTypeForStatic(Type fieldType)
        {
            FieldAccessorType accessorType = FieldAccessorType.StaticValueType;

            if (fieldType == typeof(byte) ||
                fieldType == typeof(sbyte) ||
                fieldType == typeof(bool))
                accessorType = FieldAccessorType.StaticValueTypeSize1;
            else if (fieldType == typeof(short) ||
                fieldType == typeof(ushort) ||
                fieldType == typeof(char))
                accessorType = FieldAccessorType.StaticValueTypeSize2;
            else if (fieldType == typeof(int) ||
                fieldType == typeof(uint) ||
                fieldType == typeof(float))
                accessorType = FieldAccessorType.StaticValueTypeSize4;
            else if (fieldType == typeof(long) ||
                fieldType == typeof(ulong) ||
                fieldType == typeof(double))
                accessorType = FieldAccessorType.StaticValueTypeSize8;
            else if (fieldType == typeof(IntPtr) ||
                fieldType == typeof(UIntPtr))
                accessorType = GetIntPtrAccessorTypeForStatic();

            return accessorType;
        }

        private static FieldAccessorType GetIntPtrAccessorTypeForInstance()
        {
            FieldAccessorType accessorType = FieldAccessorType.InstanceValueType;

            if (IntPtr.Size == 4)
            {
                accessorType = FieldAccessorType.InstanceValueTypeSize4;
            }
            else if (IntPtr.Size == 8)
            {
                accessorType = FieldAccessorType.InstanceValueTypeSize8;
            }

            return accessorType;
        }

        private static FieldAccessorType GetIntPtrAccessorTypeForStatic()
        {
            FieldAccessorType accessorType = FieldAccessorType.StaticValueType;

            if (IntPtr.Size == 4)
            {
                accessorType = FieldAccessorType.StaticValueTypeSize4;
            }
            else if (IntPtr.Size == 8)
            {
                accessorType = FieldAccessorType.StaticValueTypeSize8;
            }

            return accessorType;
        }

        private static void ThrowHelperTargetException() => throw new TargetException(SR.RFLCT_Targ_StatFldReqTarg);

        private static void ThrowHelperArgumentException(object target, FieldInfo fieldInfo) =>
            throw new ArgumentException(SR.Format(SR.Arg_FieldDeclTarget, fieldInfo.Name, fieldInfo.DeclaringType, target.GetType()));

        private static void ThrowHelperFieldAccessException(string fieldName, string? declaringTypeName) =>
            throw new FieldAccessException(SR.Format(SR.RFLCT_CannotSetInitonlyStaticField, fieldName, declaringTypeName));

        private enum FieldAccessorType
        {
            InstanceReferenceType,
            InstanceValueType,
            InstanceValueTypeSize1,
            InstanceValueTypeSize2,
            InstanceValueTypeSize4,
            InstanceValueTypeSize8,
            InstancePointerType,
            StaticReferenceType,
            StaticValueType,
            StaticValueTypeSize1,
            StaticValueTypeSize2,
            StaticValueTypeSize4,
            StaticValueTypeSize8,
            StaticValueTypeBoxed,
            StaticPointerType,
            NoInvoke,
            SlowPathUntilClassInitialized,
            SlowPath,
        }
    }
}
