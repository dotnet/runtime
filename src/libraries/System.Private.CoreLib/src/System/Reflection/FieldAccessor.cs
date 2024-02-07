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
        private readonly IntPtr _addressOrOffset;
        private readonly RtFieldInfo _fieldInfo;
        private readonly unsafe MethodTable* _methodTable;
        private readonly FieldAccessorType _fieldAccessType;

        internal FieldAccessor(FieldInfo fieldInfo)
        {
            _fieldInfo = (RtFieldInfo)fieldInfo;

            InitializeClass();

            // Cached the method table for performance.
            unsafe
            {
                _methodTable = (MethodTable*)_fieldInfo.FieldType.TypeHandle.Value;
            }

            // Initialize for the type of field.
            Debug.Assert(_fieldInfo.m_declaringType != null);
            if (_fieldInfo.m_declaringType.ContainsGenericParameters)
            {
                _fieldAccessType = FieldAccessorType.NoInvoke;
            }
            else if (_fieldInfo.m_declaringType.IsNullableOfT)
            {
                _fieldAccessType = FieldAccessorType.NoInvoke;
            }
            else if (!RuntimeFieldHandle.IsFastPathSupported(_fieldInfo))
            {
                // Currently this is true for [ThreadStatic] cases, for fields added from EnC, and for fields on unloadable types.
                _fieldAccessType = FieldAccessorType.SlowPath;
            }
            else
            {
                Type fieldType = _fieldInfo.FieldType;

                if (fieldInfo.IsStatic)
                {
                    _addressOrOffset = RuntimeFieldHandle.GetStaticFieldAddress(_fieldInfo, out bool isBoxed);

                    if (fieldType.IsValueType)
                    {
                        // The runtime stores non-primitive value types as a boxed value.
                        if (isBoxed)
                        {
                            _fieldAccessType = FieldAccessorType.StaticValueTypeBoxed;
                        }
                        else
                        {
                            _fieldAccessType = GetPrimitiveAccessorTypeForStatic(fieldType);
                        }
                    }
                    else if (fieldType.IsPointer)
                    {
                        Debug.Assert(!isBoxed);
                        _fieldAccessType = FieldAccessorType.StaticPointerType;
                    }
                    else if (fieldType.IsFunctionPointer)
                    {
                        Debug.Assert(!isBoxed);
                        _fieldAccessType = GetFunctionPointerAccessorTypeForStatic();
                        unsafe
                        {
                            _methodTable = (MethodTable*)typeof(IntPtr).TypeHandle.Value;
                        }
                    }
                    else
                    {
                        Debug.Assert(!isBoxed);
                        _fieldAccessType = FieldAccessorType.StaticReferenceType;
                    }
                }
                else
                {
                    _addressOrOffset = RuntimeFieldHandle.GetInstanceFieldOffset(_fieldInfo);

                    if (fieldType.IsValueType)
                    {
                        _fieldAccessType = GetPrimitiveAccessorTypeForInstance(fieldType);
                    }
                    else if (fieldType.IsPointer)
                    {
                        _fieldAccessType = FieldAccessorType.InstancePointerType;
                    }
                    else if (fieldType.IsFunctionPointer)
                    {
                        _fieldAccessType = GetFunctionPointerAccessorTypeForInstance();
                        unsafe
                        {
                            _methodTable = (MethodTable*)typeof(IntPtr).TypeHandle.Value;
                        }
                    }
                    else
                    {
                        _fieldAccessType = FieldAccessorType.InstanceReferenceType;
                    }
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
                        if (!_fieldInfo.m_declaringType.DomainInitialized)
                        {
                            return SlowPath();
                        }
                        return Volatile.Read(ref Unsafe.As<byte, object>(ref Unsafe.AddByteOffset(ref obj.GetRawData(), _addressOrOffset)));

                    case FieldAccessorType.InstanceValueType:
                    case FieldAccessorType.InstanceValueTypeSize1:
                    case FieldAccessorType.InstanceValueTypeSize2:
                    case FieldAccessorType.InstanceValueTypeSize4:
                    case FieldAccessorType.InstanceValueTypeSize8:
                        VerifyTarget(obj);
                        Debug.Assert(obj != null);
                        if (!_fieldInfo.m_declaringType.DomainInitialized)
                        {
                            return SlowPath();
                        }
                        return RuntimeHelpers.Box(
                            _methodTable,
                            ref Unsafe.AddByteOffset(ref obj.GetRawData(), _addressOrOffset));

                    case FieldAccessorType.InstancePointerType:
                        VerifyTarget(obj);
                        Debug.Assert(obj != null);
                        if (!_fieldInfo.m_declaringType.DomainInitialized)
                        {
                            return SlowPath();
                        }
                        return Pointer.Box(
                            (void*)Unsafe.As<byte, IntPtr>(ref Unsafe.AddByteOffset(ref obj.GetRawData(), _addressOrOffset)),
                            _fieldInfo.FieldType);

                    case FieldAccessorType.StaticReferenceType:
                        if (!_fieldInfo.m_declaringType.DomainInitialized)
                        {
                            return SlowPath();
                        }
                        return Volatile.Read(ref Unsafe.As<IntPtr, object>(ref *(IntPtr*)_addressOrOffset));

                    case FieldAccessorType.StaticValueType:
                    case FieldAccessorType.StaticValueTypeSize1:
                    case FieldAccessorType.StaticValueTypeSize2:
                    case FieldAccessorType.StaticValueTypeSize4:
                    case FieldAccessorType.StaticValueTypeSize8:
                        if (!_fieldInfo.m_declaringType.DomainInitialized)
                        {
                            return SlowPath();
                        }
                        return RuntimeHelpers.Box(_methodTable, ref Unsafe.AsRef<byte>(_addressOrOffset.ToPointer()));

                    case FieldAccessorType.StaticValueTypeBoxed:
                        if (!_fieldInfo.m_declaringType.DomainInitialized)
                        {
                            return SlowPath();
                        }
                        // Re-box the value.
                        return RuntimeHelpers.Box(
                            _methodTable,
                            ref Unsafe.As<IntPtr, object>(ref *(IntPtr*)_addressOrOffset).GetRawData());

                    case FieldAccessorType.StaticPointerType:
                        if (!_fieldInfo.m_declaringType.DomainInitialized)
                        {
                            return SlowPath();
                        }
                        return Pointer.Box((void*)Unsafe.As<byte, IntPtr>(
                            ref Unsafe.AsRef<byte>(_addressOrOffset.ToPointer())), _fieldInfo.FieldType);

                    case FieldAccessorType.NoInvoke:
                        if (_fieldInfo.DeclaringType is not null && _fieldInfo.DeclaringType.ContainsGenericParameters)
                            throw new InvalidOperationException(SR.Arg_UnboundGenField);

                        if (_fieldInfo.DeclaringType is not null && ((RuntimeType)_fieldInfo.FieldType).IsNullableOfT)
                            throw new NotSupportedException();

                        throw new FieldAccessException();
                }

                // Slow path
                if (!IsStatic())
                {
                    VerifyTarget(obj);
                }

                return SlowPath();

                object? SlowPath()
                {
                    object? ret;
                    RuntimeType declaringType = _fieldInfo.m_declaringType;
                    bool domainInitialized = declaringType.DomainInitialized;
                    ret = RuntimeFieldHandle.GetValue(_fieldInfo, obj, (RuntimeType)_fieldInfo.FieldType, declaringType, ref domainInitialized);
                    declaringType.DomainInitialized = domainInitialized;
                    return ret;
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

                    case FieldAccessorType.StaticReferenceType:
                        if (!VerifyStaticField(ref value, invokeAttr, binder, culture))
                        {
                            SlowPath();
                        }
                        else
                        {
                            Volatile.Write(ref Unsafe.As<IntPtr, object?>(ref *(IntPtr*)_addressOrOffset), value);
                        }
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

                    case FieldAccessorType.StaticValueTypeSize1:
                        if (!VerifyStaticField(ref value, invokeAttr, binder, culture))
                        {
                            SlowPath();
                        }
                        else
                        {
                            Volatile.Write(
                                ref Unsafe.AsRef<byte>(_addressOrOffset.ToPointer()),
                                value!.GetRawData());
                        }
                        return;

                    case FieldAccessorType.StaticValueTypeSize2:
                        if (!VerifyStaticField(ref value, invokeAttr, binder, culture))
                        {
                            SlowPath();
                        }
                        else
                        {
                            Volatile.Write(
                                ref Unsafe.AsRef<short>(_addressOrOffset.ToPointer()),
                                Unsafe.As<byte, short>(ref value!.GetRawData()));
                        }
                        return;

                    case FieldAccessorType.StaticValueTypeSize4:
                        if (!VerifyStaticField(ref value, invokeAttr, binder, culture))
                        {
                            SlowPath();
                        }
                        else
                        {
                            Volatile.Write(
                                ref Unsafe.AsRef<int>(_addressOrOffset.ToPointer()),
                                Unsafe.As<byte, int>(ref value!.GetRawData()));
                        }
                        return;

                    case FieldAccessorType.StaticValueTypeSize8:
                        if (!VerifyStaticField(ref value, invokeAttr, binder, culture))
                        {
                            SlowPath();
                        }
                        else
                        {
                            Volatile.Write(
                                ref Unsafe.AsRef<long>(_addressOrOffset.ToPointer()),
                                Unsafe.As<byte, long>(ref value!.GetRawData()));
                        }
                        return;

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

            SlowPath();

            void SlowPath()
            {
                RuntimeType declaringType = _fieldInfo.m_declaringType;
                bool domainInitialized = declaringType.DomainInitialized;
                RuntimeFieldHandle.SetValue(_fieldInfo, obj, value, (RuntimeType)_fieldInfo.FieldType, declaringType, ref domainInitialized);
                declaringType.DomainInitialized = domainInitialized;
            }
        }

        private void InitializeClass()
        {
            // Call the class constructor if not already.
            if (_fieldInfo.DeclaringType is null)
            {
                RunModuleConstructor(_fieldInfo.Module);
            }
            else
            {
                RunClassConstructor(_fieldInfo);
            }
        }

        private bool IsStatic() => (_fieldInfo.Attributes & FieldAttributes.Static) == FieldAttributes.Static;

        private bool VerifyStaticField(ref object? value, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture)
        {
            VerifyInitOnly();
            CheckValue(ref value, invokeAttr, binder, culture);

            return _fieldInfo.m_declaringType.DomainInitialized;
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

            if ((_fieldInfo.Attributes & FieldAttributes.InitOnly) == FieldAttributes.InitOnly && _fieldInfo.m_declaringType.DomainInitialized)
            {
                ThrowHelperFieldAccessException(_fieldInfo.Name, _fieldInfo.DeclaringType?.FullName);
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2059:RunClassConstructor",
            Justification = "This represents the static constructor, so if this object was created, the static constructor exists.")]
        private static void RunClassConstructor(FieldInfo fieldInfo)
        {
            RuntimeHelpers.RunClassConstructor(fieldInfo.DeclaringType!.TypeHandle);
        }

        private static void RunModuleConstructor(Module module)
        {
            RuntimeHelpers.RunModuleConstructor(module.ModuleHandle);
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

            return accessorType;
        }

        private static FieldAccessorType GetFunctionPointerAccessorTypeForInstance()
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

        private static FieldAccessorType GetFunctionPointerAccessorTypeForStatic()
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
            SlowPath,
        }
    }
}
