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
        private readonly PrimitiveFieldSize _primitiveFieldSize;
        internal FieldAccessor(FieldInfo fieldInfo)
        {
            _fieldInfo = (RtFieldInfo)fieldInfo;
            RuntimeType? declaringType = (RuntimeType?)_fieldInfo.DeclaringType;

            // Call the class constructor if not already.
            if (declaringType is null)
            {
                InvokeModuleConstructor(fieldInfo.Module);
            }
            else if (!declaringType.DomainInitialized)
            {
                InvokeClassConstructor();
            }

            // Cached the method table for performance.
            unsafe
            {
                _methodTable = (MethodTable*)_fieldInfo.FieldType.TypeHandle.Value;
            }

            // Initialize for the type of field.
            if (declaringType is null || declaringType.ContainsGenericParameters)
            {
                _addressOrOffset = default;
                _fieldAccessType = FieldAccessorType.NoInvoke;
            }
            else if (!RuntimeFieldHandle.IsFastPathSupported(_fieldInfo))
            {
                // Currently this is only true for newly added fields from EnC.
                _addressOrOffset = default;
                _fieldAccessType = FieldAccessorType.SlowPath;
            }
            else
            {
                Type fieldType = _fieldInfo.FieldType;

                if (fieldInfo.IsStatic)
                {
                    bool isBoxed = false;
                    _addressOrOffset = RuntimeFieldHandle.GetStaticFieldAddress(_fieldInfo, ref isBoxed);

                    if (fieldType.IsValueType)
                    {
                        // The runtime stores non-primitive value types as boxed.
                        _fieldAccessType = isBoxed ? FieldAccessorType.StaticValueTypeBoxed : FieldAccessorType.StaticValueType;
                    }
                    else if (fieldType.IsPointer)
                    {
                        Debug.Assert(isBoxed == false);
                        _fieldAccessType = FieldAccessorType.StaticPointerType;
                    }
                    else if (fieldType.IsFunctionPointer)
                    {
                        Debug.Assert(isBoxed == false);
                        _fieldAccessType = FieldAccessorType.StaticValueType;
                        unsafe
                        {
                            _methodTable = (MethodTable*)typeof(IntPtr).TypeHandle.Value;
                        }
                    }
                    else
                    {
                        Debug.Assert(isBoxed == false);
                        _fieldAccessType = FieldAccessorType.StaticReferenceType;
                    }
                }
                else
                {
                    _addressOrOffset = RuntimeFieldHandle.GetInstanceFieldAddress(_fieldInfo);

                    if (fieldType.IsValueType)
                    {
                        _fieldAccessType = FieldAccessorType.InstanceValueType;
                    }
                    else if (fieldType.IsPointer)
                    {
                        _fieldAccessType = FieldAccessorType.InstancePointerType;
                    }
                    else if (fieldType.IsFunctionPointer)
                    {
                        _fieldAccessType = FieldAccessorType.InstanceValueType;
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

                _primitiveFieldSize = GetPrimitiveFieldSize(fieldType, _fieldAccessType);
            }
        }

        public object? GetValue(object? obj)
        {
            object? ret = null;

            unsafe
            {
                switch (_fieldAccessType)
                {
                    case FieldAccessorType.InstanceReferenceType:
                        VerifyTarget(obj);
                        Debug.Assert(obj != null);
                        ret = Unsafe.As<byte, object>(ref Unsafe.AddByteOffset(ref obj.GetRawData(), _addressOrOffset));
                        break;

                    case FieldAccessorType.InstanceValueType:
                        VerifyTarget(obj);
                        Debug.Assert(obj != null);
                        ret = RuntimeHelpers.Box(
                            _methodTable,
                            ref Unsafe.AddByteOffset(ref obj.GetRawData(), _addressOrOffset));

                        break;

                    case FieldAccessorType.InstancePointerType:
                        VerifyTarget(obj);
                        Debug.Assert(obj != null);
                        ret = Pointer.Box(
                            (void*)Unsafe.As<byte, IntPtr>(ref Unsafe.AddByteOffset(ref obj.GetRawData(), _addressOrOffset)),
                            _fieldInfo.FieldType);

                        break;

                    case FieldAccessorType.StaticReferenceType:
                        ret = Volatile.Read(ref Unsafe.As<IntPtr, object>(ref *(IntPtr*)_addressOrOffset));
                        break;

                    case FieldAccessorType.StaticValueType:
                        ret = RuntimeHelpers.Box(_methodTable, ref Unsafe.AsRef<byte>(_addressOrOffset.ToPointer()));
                        break;

                    case FieldAccessorType.StaticValueTypeBoxed:
                        // Re-box the value.
                        ret = RuntimeHelpers.Box(
                            _methodTable,
                            ref Unsafe.As<IntPtr, object>(ref *(IntPtr*)_addressOrOffset).GetRawData());

                        break;

                    case FieldAccessorType.StaticPointerType:
                        ret = Pointer.Box((void*)Unsafe.As<byte, IntPtr>(
                            ref Unsafe.AsRef<byte>(_addressOrOffset.ToPointer())), _fieldInfo.FieldType);

                        break;

                    case FieldAccessorType.SlowPath:
                        if (!IsStatic())
                        {
                            VerifyTarget(obj);
                        }

                        ret = RuntimeFieldHandle.GetValue(_fieldInfo, obj, (RuntimeType)_fieldInfo.FieldType, (RuntimeType?)_fieldInfo.DeclaringType);
                        break;

                    case FieldAccessorType.NoInvoke:
                        if (_fieldInfo.DeclaringType is not null && _fieldInfo.DeclaringType.ContainsGenericParameters)
                            throw new InvalidOperationException(SR.Arg_UnboundGenField);

                        throw new FieldAccessException();
#if DEBUG
                    default:
                        throw new Exception("Unknown enum value");
#endif
                }
            }

            return ret;
        }

        public void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture)
        {
            RuntimeType fieldType = (RuntimeType)_fieldInfo.FieldType;

            switch (_fieldAccessType)
            {
                case FieldAccessorType.InstanceReferenceType:
                    VerifyTarget(obj);
                    CheckValue();
                    Debug.Assert(obj != null);
                    Unsafe.As<byte, object?>(ref Unsafe.AddByteOffset(ref obj.GetRawData(), _addressOrOffset)) = value;
                    return;

                case FieldAccessorType.StaticReferenceType:
                    VerifyInitOnly();
                    CheckValue();
                    unsafe
                    {
                        Unsafe.As<IntPtr, object?>(ref *(IntPtr*)_addressOrOffset) = value;
                    }
                    return;

                case FieldAccessorType.NoInvoke:
                    if (_fieldInfo.DeclaringType is not null && _fieldInfo.DeclaringType.ContainsGenericParameters)
                        throw new InvalidOperationException(SR.Arg_UnboundGenField);

                    throw new FieldAccessException();

                case FieldAccessorType.InstanceValueType:
                    VerifyTarget(obj);
                    CheckValue();
                    Debug.Assert(obj != null);
                    switch (_primitiveFieldSize)
                    {
                        case PrimitiveFieldSize.Size1:
                            Volatile.Write(
                                ref Unsafe.AddByteOffset(ref obj.GetRawData(), _addressOrOffset),
                                value!.GetRawData());

                            return;

                        case PrimitiveFieldSize.Size2:
                            Volatile.Write(
                                ref Unsafe.As<byte, short>(ref Unsafe.AddByteOffset(ref obj.GetRawData(), _addressOrOffset)),
                                Unsafe.As<byte, short>(ref value!.GetRawData()));

                            return;

                        case PrimitiveFieldSize.Size4:
                            Volatile.Write(
                                ref Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref obj.GetRawData(), _addressOrOffset)),
                                Unsafe.As<byte, int>(ref value!.GetRawData()));

                            return;

                        case PrimitiveFieldSize.Size8:
                            Volatile.Write(
                                ref Unsafe.As<byte, long>(ref Unsafe.AddByteOffset(ref obj.GetRawData(), _addressOrOffset)),
                                Unsafe.As<byte, long>(ref value!.GetRawData()));

                            return;

                        default:
                            RuntimeFieldHandle.SetValue(_fieldInfo, obj, value, fieldType, (RuntimeType?)_fieldInfo.DeclaringType);
                            return;
                    }


                case FieldAccessorType.StaticValueType:
                    VerifyInitOnly();
                    CheckValue();
                    unsafe
                    {
                        switch (_primitiveFieldSize)
                        {
                            case PrimitiveFieldSize.Size1:
                                Volatile.Write(
                                    ref Unsafe.AsRef<byte>(_addressOrOffset.ToPointer()),
                                    value!.GetRawData());

                                break;

                            case PrimitiveFieldSize.Size2:
                                Volatile.Write(
                                    ref Unsafe.AsRef<short>(_addressOrOffset.ToPointer()),
                                    Unsafe.As<byte, short>(ref value!.GetRawData()));

                                break;

                            case PrimitiveFieldSize.Size4:
                                Volatile.Write(
                                    ref Unsafe.AsRef<int>(_addressOrOffset.ToPointer()),
                                    Unsafe.As<byte, int>(ref value!.GetRawData()));

                                break;

                            case PrimitiveFieldSize.Size8:
                                Volatile.Write(
                                    ref Unsafe.AsRef<long>(_addressOrOffset.ToPointer()),
                                    Unsafe.As<byte, long>(ref value!.GetRawData()));
                                break;

                            case PrimitiveFieldSize.NotPrimitive:
                                RuntimeFieldHandle.SetValue(_fieldInfo, obj, value, fieldType, (RuntimeType?)_fieldInfo.DeclaringType);
                                break;
                        }

                        return;
                    }
            }

            if (!IsStatic())
            {
                VerifyTarget(obj);
            }

            CheckValue();
            RuntimeFieldHandle.SetValue(_fieldInfo, obj, value, fieldType, (RuntimeType?)_fieldInfo.DeclaringType);

            void CheckValue()
            {
                if (value is null)
                {
                    if (fieldType.IsActualValueType)
                    {
                        fieldType.CheckValue(ref value, binder, culture, invokeAttr);
                    }
                }
                else if (!ReferenceEquals(value.GetType(), fieldType))
                {
                    fieldType.CheckValue(ref value, binder, culture, invokeAttr);
                }
            }
        }

        private bool IsStatic() => (_fieldInfo.Attributes & FieldAttributes.Static) == FieldAttributes.Static;

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
                    ThrowHelperArgumentException(target);
                }
            }
        }

        private static void ThrowHelperTargetException() => throw new TargetException(SR.RFLCT_Targ_StatFldReqTarg);
        private void ThrowHelperArgumentException(object target) => throw new ArgumentException(
            SR.Format(SR.Arg_FieldDeclTarget, _fieldInfo.Name, _fieldInfo.DeclaringType, target.GetType()));
        private static void ThrowHelperFieldAccessException(string fieldName, string? declaringTypeName) => throw new FieldAccessException(SR.Format(SR.RFLCT_CannotSetInitonlyStaticField, fieldName, declaringTypeName));

        internal void VerifyInitOnly()
        {
            Debug.Assert(IsStatic());
            if ((_fieldInfo.Attributes & FieldAttributes.InitOnly) == FieldAttributes.InitOnly)
            {
                ThrowHelperFieldAccessException(_fieldInfo.Name, _fieldInfo.DeclaringType!.FullName);
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2059:RunClassConstructor",
            Justification = "This represents the static constructor, so if this object was created, the static constructor exists.")]
        private void InvokeClassConstructor()
        {
            RuntimeHelpers.RunClassConstructor(_fieldInfo.DeclaringType!.TypeHandle);
        }

        private static void InvokeModuleConstructor(Module module)
        {
            RuntimeHelpers.RunModuleConstructor(module.ModuleHandle);
        }

        /// <summary>
        /// Currently we only optimize for primitive types; these are simple because they are atomic write operations, are
        /// not boxed by the runtime when stored as a static field, and don't need special nullable, GC or alignment checks.
        /// </summary>
        private static PrimitiveFieldSize GetPrimitiveFieldSize(Type fieldType, FieldAccessorType fieldAccessType)
        {
            PrimitiveFieldSize size = PrimitiveFieldSize.NotPrimitive;

            if (fieldAccessType == FieldAccessorType.InstanceValueType ||
                fieldAccessType == FieldAccessorType.StaticValueType)
            {
                if (fieldType == typeof(byte) ||
                    fieldType == typeof(sbyte) ||
                    fieldType == typeof(bool))
                    size = PrimitiveFieldSize.Size1;
                else if (fieldType == typeof(short) ||
                    fieldType == typeof(ushort) ||
                    fieldType == typeof(char))
                    size = PrimitiveFieldSize.Size2;
                else if (fieldType == typeof(int) ||
                    fieldType == typeof(uint) ||
                    fieldType == typeof(float))
                    size = PrimitiveFieldSize.Size4;
                else if (fieldType == typeof(long) ||
                    fieldType == typeof(ulong) ||
                    fieldType == typeof(double))
                    size = PrimitiveFieldSize.Size8;
                else if (fieldType.IsFunctionPointer)
                {
                    if (IntPtr.Size == 4)
                    {
                        size = PrimitiveFieldSize.Size4;
                    }
                    else if (IntPtr.Size == 8)
                    {
                        size = PrimitiveFieldSize.Size8;
                    }
                }
            }

            return size;
        }

        private enum FieldAccessorType : short
        {
            InstanceReferenceType = 0,
            InstanceValueType = 1,
            InstancePointerType = 2,
            StaticReferenceType = 3,
            StaticValueType = 4,
            StaticValueTypeBoxed = 5,
            StaticPointerType = 6,
            SlowPath = 7,
            NoInvoke = 8,
        }

        private enum PrimitiveFieldSize : short
        {
            Size1,
            Size2,
            Size4,
            Size8,
            NotPrimitive
        }
    }
}
