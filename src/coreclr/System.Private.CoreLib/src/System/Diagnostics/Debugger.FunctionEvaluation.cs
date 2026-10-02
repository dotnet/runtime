// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    public static partial class Debugger
    {
        [StackTraceHidden]
        internal sealed unsafe partial class FunctionEvaluation
        {
            internal struct Context
            {
                internal IntPtr Evaluation;
                internal IntPtr Method;
#pragma warning disable CS8500
                internal object?* Objects;
#pragma warning restore CS8500
                internal void** Interiors;
                internal void** Homes;
                internal ulong* Primitives;
                internal void** Storage;
                internal void** ResultByRefs;
                internal void* ResultData;
#pragma warning disable CS8500
                internal object?* ResultObject;
                internal object?* LoaderAllocator;
#pragma warning restore CS8500
                internal uint ArgumentCount;
                internal uint ParameterCount;
                internal EvaluationFlags Flags;
                internal uint ReturnElementType;
            }

            [Flags]
            internal enum EvaluationFlags : uint
            {
                NewObject = 1,
                Static = 2,
                Virtual = 4,
                Interface = 8,
                Shared = 16,
                ExternalResult = 32,
            }

            [Flags]
            private enum ArgumentFlags : uint
            {
                Literal = 1,
                Handle = 2,
                Memory = 4,
                Register = 8,
            }

            private struct NativeArgument
            {
                internal uint ElementType;
                internal ArgumentFlags Flags;

                internal readonly bool IsLiteral => (Flags & ArgumentFlags.Literal) != 0;
                internal readonly bool IsHandle => (Flags & ArgumentFlags.Handle) != 0;
                internal readonly bool HasMemory => (Flags & ArgumentFlags.Memory) != 0;
                internal readonly bool IsInRegister => !HasMemory && !IsLiteral;
                internal readonly CorElementType Type => (CorElementType)ElementType;
            }

            private struct Argument
            {
                internal NativeArgument Input;
                internal RuntimeType Type;
                internal CorElementType SignatureType;
                internal bool IsByRef;
                internal bool IsTypedLocal;
                internal bool IsBoxedTemporary;
                internal bool UsesInterior;
                internal bool UsesDirectHome;
            }

            private readonly Context* _context;
            private readonly MethodBase _method;
            private readonly bool _hasReceiver;
            private readonly Argument[] _arguments;
            private readonly object?[] _objects;
            private readonly object?[] _originalNullables;
            private readonly InvokerEmitUtil.InvokeFunc_Debugger _invoke;

            internal bool IsNewObject() => (_context->Flags & EvaluationFlags.NewObject) != 0;

            private bool UsesExternalResult => (_context->Flags & EvaluationFlags.ExternalResult) != 0;

            [DebuggerHidden]
            internal FunctionEvaluation(Context* context)
            {
                _context = context;
                RuntimeType? declaringType = null;
                RuntimeType? allocationType = null;
                GetMethod(context, (uint)sizeof(Context),
                    ObjectHandleOnStack.Create(ref declaringType), ObjectHandleOnStack.Create(ref allocationType));
                Debug.Assert(declaringType is not null);

                object? newObject = null;
                if (IsNewObject() && !UsesExternalResult)
                {
                    Debug.Assert(allocationType is not null);
                    newObject = AllocateObject(allocationType);
                }

                bool isStatic = (context->Flags & EvaluationFlags.Static) != 0;
                if ((ulong)context->ArgumentCount + (IsNewObject() ? 1UL : 0UL) !=
                    (ulong)context->ParameterCount + (isStatic ? 0UL : 1UL))
                {
                    throw new TargetParameterCountException(SR.Arg_ParmCnt);
                }

                MethodBase? method = RuntimeType.GetMethodBase(declaringType, new RuntimeMethodHandleInternal(context->Method));
                Debug.Assert(method is RuntimeMethodInfo or RuntimeConstructorInfo);
                _method = method;
                _hasReceiver = !isStatic && !IsNewObject();
                int argumentCount = checked((int)context->ArgumentCount);
                _arguments = new Argument[argumentCount];
                _objects = new object?[argumentCount];
                _originalNullables = new object?[argumentCount];
                for (int i = 0; i < argumentCount; i++)
                {
                    GetArgument(context, (uint)i, out _arguments[i].Input);
                }

                if (_hasReceiver)
                {
                    NativeArgument input = _arguments[0].Input;
                    if (!IsObject(input.Type) && input.Type != CorElementType.ELEMENT_TYPE_VALUETYPE)
                    {
                        throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_Enum);
                    }

                    PrepareArgument(0, declaringType, isByRef: false, isReceiver: true);
                    ValidateReceiver(declaringType);
                }
                else if (IsNewObject())
                {
                    if (UsesExternalResult)
                    {
                        Debug.Assert(declaringType.IsByRefLike);
                        SetStorage(0, ref Unsafe.AsRef<byte>(context->ResultData));
                    }
                    else
                    {
                        Debug.Assert(newObject is not null);
                        *context->ResultObject = newObject;
                        if (declaringType.IsValueType)
                        {
                            SetStorage(0, ref newObject.GetRawData());
                        }
                        else
                        {
                            SetStorage(0, ref *context->ResultObject);
                        }
                    }
                }

                RuntimeType? returnType = null;
                GetReturnType(context, ObjectHandleOnStack.Create(ref returnType));
                Debug.Assert(returnType is not null);

                ReadOnlySpan<ParameterInfo> parameters = method.GetParametersAsSpan();
                for (int i = 0; i < parameters.Length; i++)
                {
                    RuntimeType parameterType = (RuntimeType)parameters[i].ParameterType;
                    bool isByRef = parameterType.IsByRef;
                    PrepareArgument(i + (_hasReceiver ? 1 : 0),
                        isByRef ? (RuntimeType)parameterType.GetElementType()! : parameterType, isByRef, isReceiver: false);
                }

                CorElementType returnElementType = (CorElementType)context->ReturnElementType;
                if (returnElementType == CorElementType.ELEMENT_TYPE_TYPEDBYREF)
                {
                    throw new ArgumentException(SR.Argument_CannotCreateTypedReference);
                }

                if (!IsNewObject() && !UsesExternalResult && returnElementType == CorElementType.ELEMENT_TYPE_VALUETYPE)
                {
                    *context->ResultObject = AllocateObject(returnType);
                }

                _invoke = method is RuntimeMethodInfo methodInfo
                    ? methodInfo.Invoker.GetDebuggerInvokeDelegate()
                    : ((RuntimeConstructorInfo)method).GetDebuggerInvokeDelegate();
            }

            [DebuggerHidden]
            internal void Invoke()
            {
                int resultSlot = checked((int)_context->ParameterCount + 1);
                CorElementType returnType = (CorElementType)_context->ReturnElementType;
                if (UsesExternalResult)
                {
                    SetStorage(resultSlot, ref Unsafe.AsRef<byte>(_context->ResultData));
                }
                else if (IsNewObject() || returnType == CorElementType.ELEMENT_TYPE_VALUETYPE)
                {
                    object? result = *_context->ResultObject;
                    Debug.Assert(result is not null);
                    SetStorage(resultSlot, ref result.GetRawData());
                }
                else if (IsObject(returnType))
                {
                    SetStorage(resultSlot, ref *_context->ResultObject);
                }
                else if (returnType == CorElementType.ELEMENT_TYPE_BYREF)
                {
                    SetStorage(resultSlot, ref Unsafe.AsRef<byte>(_context->ResultByRefs));
                }
                else if (returnType != CorElementType.ELEMENT_TYPE_VOID)
                {
                    SetStorage(resultSlot, ref Unsafe.AsRef<byte>(_context->ResultData));
                }

                _invoke(this, _context->Storage);
                GC.KeepAlive(_method);
            }

            private void PrepareArgument(int index, RuntimeType type, bool isByRef, bool isReceiver)
            {
                ref Argument argument = ref _arguments[index];
                NativeArgument input = argument.Input;
                argument.Type = type;
                argument.IsByRef = isByRef;
                argument.SignatureType = type.IsEnum
                    ? ((RuntimeType)type.GetEnumUnderlyingType()).GetCorElementType()
                    : type.GetCorElementType();
                int slot = index + (_hasReceiver ? 0 : 1);

                if (IsObject(input.Type))
                {
                    object? value = ReadObject(index, interior: false);
                    if (type.IsValueType)
                    {
                        if (type.IsNullableOfT)
                        {
                            _originalNullables[index] = value;
                            if (!isReceiver || value is null || value.GetType() != type)
                            {
                                object nullable = AllocateObject(type);
                                MethodTable* nullableType = type.GetNativeTypeHandle().AsMethodTable();
                                if (value is not null)
                                {
                                    MethodTable* valueType = RuntimeHelpers.GetMethodTable(value);
                                    if (valueType != nullableType && !CastHelpers.IsNullableForType(nullableType, valueType))
                                    {
                                        throw new ArgumentException(SR.Argument_BadObjRef);
                                    }
                                }

                                CastHelpers.Unbox_Nullable(ref nullable.GetRawData(), nullableType, value);
                                value = nullable;
                            }
                        }
                        else if (value is null)
                        {
                            throw new ArgumentNullException();
                        }
                        else if (!value.GetType().IsValueType)
                        {
                            throw new ArgumentException(SR.Argument_BadObjRef);
                        }

                        Debug.Assert(value is not null);
                        _objects[index] = value;
                        SetStorage(slot, ref value.GetRawData());
                    }
                    else
                    {
                        _objects[index] = value;
                        if (input.IsHandle)
                        {
#pragma warning disable CS8500
                            SetStorage(slot, ref *(object?*)(nuint)_context->Primitives[index]);
#pragma warning restore CS8500
                        }
                        else
                        {
                            SetStorage(slot, ref _objects[index]);
                        }
                    }

                    return;
                }

                if (input.Type == CorElementType.ELEMENT_TYPE_VALUETYPE)
                {
                    if (!type.IsValueType && (isReceiver || !isByRef))
                    {
                        RuntimeType? actualType = null;
                        GetArgumentType(_context, (uint)index, ObjectHandleOnStack.Create(ref actualType));
                        if (actualType is null || !actualType.IsValueType)
                        {
                            throw new ArgumentException(SR.Argument_BadObjRef);
                        }

                        object? value;
                        if (input.HasMemory)
                        {
                            value = actualType.Box(ref ((ByReference*)(_context->Interiors + index))->Value);
                        }
                        else
                        {
                            if (actualType.IsByRefLike)
                            {
                                throw new NotSupportedException(SR.NotSupported_ByRefLike);
                            }

                            object temporary = AllocateObject(actualType);
                            CopyValueTypeArgument(index, actualType, ref temporary.GetRawData());
                            value = RuntimeMethodHandle.ReboxFromNullable(temporary);
                        }

                        if (isReceiver && value is null)
                        {
                            throw new ArgumentNullException();
                        }

                        _objects[index] = value;
                        SetStorage(slot, ref _objects[index]);
                    }
                    else if (input.HasMemory)
                    {
                        SetStorage(slot, ref ((ByReference*)(_context->Interiors + index))->Value);
                    }
                    else
                    {
                        if (!type.IsValueType || GetValueSize(type) > sizeof(ulong))
                        {
                            throw new ArgumentException(SR.Argument_BadObjRef);
                        }

                        if (type.IsByRefLike)
                        {
                            // The emitted local, not a native byte copy or a heap box, owns its ref fields.
                            argument.IsTypedLocal = true;
                        }
                        else
                        {
                            object temporary = AllocateObject(type);
                            _objects[index] = temporary;
                            CopyValueTypeArgument(index, type, ref temporary.GetRawData());
                            argument.IsBoxedTemporary = true;
                            SetStorage(slot, ref temporary.GetRawData());
                        }
                    }

                    return;
                }

                bool pointerSizedNumeric = IntPtr.Size == sizeof(long)
                    ? input.Type is CorElementType.ELEMENT_TYPE_I8 or CorElementType.ELEMENT_TYPE_U8 or CorElementType.ELEMENT_TYPE_R8
                    : input.Type is CorElementType.ELEMENT_TYPE_I4 or CorElementType.ELEMENT_TYPE_U4 or CorElementType.ELEMENT_TYPE_R4;
                if (isByRef && input.HasMemory && !input.IsLiteral && !input.IsHandle &&
                    input.Type == argument.SignatureType)
                {
                    argument.UsesDirectHome = true;
                    SetStorage(slot, ref ((ByReference*)(_context->Homes + index))->Value);
                }
                else if (pointerSizedNumeric && IsObject(argument.SignatureType))
                {
                    ReadObject(index, interior: true);
                    argument.UsesInterior = true;
                    SetStorage(slot, ref Unsafe.AsRef<byte>(_context->Interiors + index));
                }
                else
                {
                    ulong value = _context->Primitives[index];
                    if (!input.IsInRegister && !input.IsHandle &&
                        input.Type is not (CorElementType.ELEMENT_TYPE_I8 or CorElementType.ELEMENT_TYPE_U8 or CorElementType.ELEMENT_TYPE_R8))
                    {
                        value = ConvertPrimitive(value, argument.SignatureType);
                    }

                    _context->Primitives[index] = value;
                    SetStorage(slot, ref _context->Primitives[index]);
                }
            }

            private void ValidateReceiver(RuntimeType declaringType)
            {
                if (declaringType.IsValueType)
                {
                    return;
                }

                object? receiver = _objects[0];
                bool isVirtual = (_context->Flags & EvaluationFlags.Virtual) != 0;
                if (isVirtual)
                {
                    NativeArgument input = _arguments[0].Input;
                    if (receiver is null || (!input.HasMemory && (input.Flags & ArgumentFlags.Register) == 0))
                    {
                        throw new ArgumentNullException();
                    }

                    if (!declaringType.IsInstanceOfType(receiver))
                    {
                        throw new ArgumentException(SR.Argument_CORDBBadMethod);
                    }
                }

                if (receiver is null)
                {
                    throw new NullReferenceException(SR.NullReference_This);
                }

                if ((_context->Flags & (EvaluationFlags.Interface | EvaluationFlags.Shared)) == 0 &&
                    !receiver.GetType().IsArray && !declaringType.IsInstanceOfType(receiver))
                {
                    throw new ArgumentException(SR.Argument_CORDBBadMethod);
                }
            }

            [DebuggerHidden]
            internal bool InitializeTypedArgument(int slot, ref byte storage)
            {
                if (slot == 0 && IsNewObject())
                {
                    SetStorage(slot, ref storage);
                    return true;
                }

                int index = slot + (_hasReceiver ? 0 : -1);
                if (!_arguments[index].IsTypedLocal)
                {
                    return false;
                }

                CopyValueTypeArgument(index, _arguments[index].Type, ref storage);
                SetStorage(slot, ref storage);
                return true;
            }

            [DebuggerHidden]
            internal void CopyBackTypedArgument(int slot, ref byte storage, bool usesLocal)
            {
                if (slot == 0 && IsNewObject())
                {
                    return;
                }

                if (usesLocal)
                {
                    int index = slot + (_hasReceiver ? 0 : -1);
                    WriteArgument(index, ref storage, GetValueSize(_arguments[index].Type));
                }
                else
                {
                    CopyBackArgument(slot);
                }
            }

            [DebuggerHidden]
            internal void CopyBackArgument(int slot)
            {
                if (slot == 0 && !_hasReceiver)
                {
                    return;
                }

                int index = slot + (_hasReceiver ? 0 : -1);
                ref Argument argument = ref _arguments[index];
                NativeArgument input = argument.Input;
                if (slot == 0)
                {
                    if (!argument.Type.IsValueType || input.Type != CorElementType.ELEMENT_TYPE_VALUETYPE || !input.IsInRegister)
                    {
                        return;
                    }
                }
                else if (!argument.IsByRef || argument.UsesDirectHome)
                {
                    return;
                }

                if (argument.IsBoxedTemporary)
                {
                    WriteArgument(index, ref _objects[index]!.GetRawData(), GetValueSize(argument.Type));
                    return;
                }

                if (IsObject(input.Type))
                {
                    if (argument.Type.IsNullableOfT)
                    {
                        object? original = _originalNullables[index];
                        if (!input.IsLiteral && original is not null && original.GetType() == argument.Type)
                        {
                            object? temporary = _objects[index];
                            Debug.Assert(temporary is not null);
                            CopyValue(argument.Type, ref original.GetRawData(), ref temporary.GetRawData());
                            _objects[index] = original;
                        }
                        else
                        {
                            _objects[index] = RuntimeMethodHandle.ReboxFromNullable(_objects[index]);
                            if (input.IsHandle)
                            {
                                GCHandle.InternalSet((nint)(nuint)_context->Primitives[index], _objects[index]);
                            }
                        }
                    }
                    else
                    {
                        _objects[index] = RuntimeMethodHandle.ReboxFromNullable(_objects[index]);
                    }

                    WriteArgument(index, ref Unsafe.As<object?, byte>(ref _objects[index]), (uint)IntPtr.Size);
                }
                else if (input.Type != CorElementType.ELEMENT_TYPE_VALUETYPE)
                {
                    if (argument.UsesInterior)
                    {
                        WriteArgument(index, ref Unsafe.AsRef<byte>(_context->Interiors + index), (uint)IntPtr.Size);
                    }
                    else
                    {
                        WriteArgument(index, ref Unsafe.AsRef<byte>(_context->Primitives + index), sizeof(ulong));
                    }
                }
            }

            private void SetStorage<T>(int slot, [UnscopedRef] ref T value) =>
                *(ByReference*)(_context->Storage + slot) = ByReference.Create(ref value);

            private object? ReadObject(int index, bool interior)
            {
                object? value = null;
                if (GetObject(_context, (uint)index, interior ? 1 : 0, ObjectHandleOnStack.Create(ref value)) < 0)
                {
                    throw new ArgumentException(SR.Argument_BadObjRef);
                }

                return value;
            }

            private static ulong ConvertPrimitive(ulong value, CorElementType type) => type switch
            {
                CorElementType.ELEMENT_TYPE_BOOLEAN => value == 0 ? 0UL : 1UL,
                _ => GetPrimitiveSize(type) switch
                {
                    1 => (byte)value,
                    2 => (ushort)value,
                    4 => (uint)value,
                    8 => value,
                    _ => throw new ArgumentException(SR.Argument_BadObjRef),
                },
            };

            private static int GetPrimitiveSize(CorElementType type) => type switch
            {
                CorElementType.ELEMENT_TYPE_BOOLEAN or CorElementType.ELEMENT_TYPE_I1 or CorElementType.ELEMENT_TYPE_U1 => 1,
                CorElementType.ELEMENT_TYPE_CHAR or CorElementType.ELEMENT_TYPE_I2 or CorElementType.ELEMENT_TYPE_U2 => 2,
                CorElementType.ELEMENT_TYPE_I4 or CorElementType.ELEMENT_TYPE_U4 or CorElementType.ELEMENT_TYPE_R4 => 4,
                CorElementType.ELEMENT_TYPE_I8 or CorElementType.ELEMENT_TYPE_U8 or CorElementType.ELEMENT_TYPE_R8 => 8,
                CorElementType.ELEMENT_TYPE_I or CorElementType.ELEMENT_TYPE_U or
                CorElementType.ELEMENT_TYPE_PTR or CorElementType.ELEMENT_TYPE_FNPTR => IntPtr.Size,
                _ => 0,
            };

            private static bool IsObject(CorElementType type) => type is
                CorElementType.ELEMENT_TYPE_CLASS or CorElementType.ELEMENT_TYPE_OBJECT or
                CorElementType.ELEMENT_TYPE_STRING or CorElementType.ELEMENT_TYPE_ARRAY or CorElementType.ELEMENT_TYPE_SZARRAY;

            internal static uint GetValueSize(RuntimeType type) =>
                type.GetNativeTypeHandle().AsMethodTable()->GetNumInstanceFieldBytes();

            private static void CopyValue(RuntimeType type, ref byte destination, ref byte source)
            {
                uint size = GetValueSize(type);
                if (type.GetNativeTypeHandle().AsMethodTable()->ContainsGCPointers)
                {
                    Buffer.BulkMoveWithWriteBarrier(ref destination, ref source, size);
                }
                else
                {
                    SpanHelpers.Memmove(ref destination, ref source, size);
                }
            }

            private static object AllocateObject(RuntimeType type)
            {
                object? result = null;
                // InternalAlloc also runs precise cctors; debugger temporary/return allocation must not.
                AllocateObject(new QCallTypeHandle(ref type), ObjectHandleOnStack.Create(ref result));
                Debug.Assert(result is not null);
                return result;
            }

            private void CopyValueTypeArgument(int index, RuntimeType type, ref byte destination)
            {
                if (GetValueSize(type) > sizeof(ulong))
                {
                    throw new ArgumentException(SR.Argument_BadObjRef);
                }

                fixed (byte* address = &destination)
                {
                    CopyValueTypeArgument(_context, (uint)index, new QCallTypeHandle(ref type), address);
                }
            }

            private void WriteArgument(int index, ref byte value, uint size)
            {
                fixed (byte* address = &value)
                {
                    WriteArgument(_context, (uint)index, (uint)_arguments[index].SignatureType, address, size);
                }
            }

            [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_GetFuncEvalMethod")]
            private static partial void GetMethod(Context* context, uint contextSize, ObjectHandleOnStack declaringType, ObjectHandleOnStack allocationType);

            [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_GetFuncEvalReturnType")]
            private static partial void GetReturnType(Context* context, ObjectHandleOnStack returnType);

            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_GetFuncEvalArgument")]
            [SuppressGCTransition]
            private static partial void GetArgument(Context* context, uint index, out NativeArgument argument);

            [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_GetFuncEvalArgumentType")]
            private static partial void GetArgumentType(Context* context, uint index, ObjectHandleOnStack type);

            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_GetFuncEvalObject")]
            [SuppressGCTransition]
            private static partial int GetObject(Context* context, uint index, int interior, ObjectHandleOnStack value);

            [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_AllocateFuncEvalObject")]
            private static partial void AllocateObject(QCallTypeHandle type, ObjectHandleOnStack result);

            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_CopyFuncEvalValueTypeArgument")]
            [SuppressGCTransition]
            private static partial void CopyValueTypeArgument(Context* context, uint index, QCallTypeHandle type, void* destination);

            [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_WriteFuncEvalArgument")]
            private static partial void WriteArgument(Context* context, uint index, uint signatureType, void* value, uint size);
        }
    }
}
