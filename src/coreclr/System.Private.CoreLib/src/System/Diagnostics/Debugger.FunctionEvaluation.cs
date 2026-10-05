// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime;
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
                internal ulong* CapturedArguments;
                internal void** ResultOwner;
                internal void** ResultByRefs;
                internal void* ResultData;
                internal void* ResultHandle;
#pragma warning disable CS8500
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
                internal byte* Literal;
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
                // A ref return can keep this argument temporary alive after the managed entry returns.
                internal ulong Primitive;
                internal CorElementType SignatureType;
                internal bool IsByRef;
                internal bool IsTypedLocal;
                internal bool IsBoxedTemporary;
                internal bool UsesInterior;
                internal bool UsesDirectHome;
            }

            private readonly Context* _context;
            private readonly void** _storage;
            private readonly MethodBase _method;
            private readonly bool _hasReceiver;
            private readonly Argument[] _arguments;
            private readonly object?[] _objects;
            private readonly object?[] _originalNullables;
            private readonly InvokerEmitUtil.InvokeFunc_Debugger _invoke;
            // Holds the new-object/boxed-value-type/object-typed result across the gap between
            // the constructor and Invoke(). An instance field is tracked like any other live
            // managed reference for as long as this FunctionEvaluation is reachable, so it needs
            // no native-side rooting the way a raw Context-pointer slot would.
            private object? _resultObject;

            internal bool IsNewObject() => (_context->Flags & EvaluationFlags.NewObject) != 0;

            private bool UsesExternalResult => (_context->Flags & EvaluationFlags.ExternalResult) != 0;

            [DebuggerHidden]
            internal static void Run(Context* context)
            {
                const int MaxStackArguments = 16;
                int argumentCount = checked((int)context->ArgumentCount);
                int storageCount = checked(argumentCount + 2);
                Span<IntPtr> storage = argumentCount <= MaxStackArguments
                    ? stackalloc IntPtr[MaxStackArguments + 2]
                    : new IntPtr[storageCount];
                storage.Clear();

                fixed (IntPtr* address = storage)
                {
                    GCFrameRegistration registration = new((void**)address, (uint)storageCount, areByRefs: true);
                    try
                    {
                        GCFrameRegistration.RegisterForGCReporting(&registration);
                        new FunctionEvaluation(context, (void**)address).Invoke();
                    }
                    finally
                    {
                        GCFrameRegistration.UnregisterForGCReporting(&registration);
                    }
                }
            }

            [DebuggerHidden]
            private FunctionEvaluation(Context* context, void** storage)
            {
                _context = context;
                _storage = storage;
                int argumentCount = checked((int)context->ArgumentCount);
                _arguments = new Argument[argumentCount];
                CapturePrimitiveArguments();
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
                _objects = new object?[argumentCount];
                _originalNullables = new object?[argumentCount];

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
                        _resultObject = newObject;
                        if (declaringType.IsValueType)
                        {
                            SetStorage(0, ref newObject.GetRawData());
                        }
                        else
                        {
                            SetStorage(0, ref _resultObject);
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
                    _resultObject = AllocateObject(returnType);
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
                    object? result = _resultObject;
                    Debug.Assert(result is not null);
                    SetStorage(resultSlot, ref result.GetRawData());
                }
                else if (IsObject(returnType))
                {
                    SetStorage(resultSlot, ref _resultObject);
                }
                else if (returnType == CorElementType.ELEMENT_TYPE_BYREF)
                {
                    SetStorage(resultSlot, ref Unsafe.AsRef<byte>(_context->ResultByRefs));
                }
                else if (returnType != CorElementType.ELEMENT_TYPE_VOID)
                {
                    SetStorage(resultSlot, ref Unsafe.AsRef<byte>(_context->ResultData));
                }

                _invoke(this, _storage);

                // Matches native's boxing-policy computation (DebuggerEval::m_retValueBoxing) so the
                // completion step in funceval.cpp can tell what, if anything, it should publish.
                bool boxed = !UsesExternalResult &&
                    (IsNewObject() || returnType == CorElementType.ELEMENT_TYPE_VALUETYPE);
                if (boxed || IsObject(returnType))
                {
                    // The strong handle is what crosses back into native code; once allocated it is
                    // a stable, self-rooting reference, so _resultObject needs no further protection.
                    _context->ResultHandle = (void*)GCHandle.ToIntPtr(GCHandle.Alloc(_resultObject));
                }

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
                            SetStorage(slot, ref *(object?*)(nuint)_context->CapturedArguments[index]);
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
                    _objects[index] = ReadObject(index, interior: true);
                    argument.UsesInterior = true;
                    SetStorage(slot, ref _objects[index]);
                }
                else
                {
                    ulong value = argument.Primitive;
                    if (!input.IsInRegister && !input.IsHandle &&
                        input.Type is not (CorElementType.ELEMENT_TYPE_I8 or CorElementType.ELEMENT_TYPE_U8 or CorElementType.ELEMENT_TYPE_R8))
                    {
                        value = ConvertPrimitive(value, argument.SignatureType);
                    }

                    argument.Primitive = value;
                    SetStorage(slot, ref argument.Primitive);
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
                                GCHandle.InternalSet((nint)(nuint)_context->CapturedArguments[index], _objects[index]);
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
                        WriteArgument(index, ref Unsafe.As<object?, byte>(ref _objects[index]), (uint)IntPtr.Size);
                    }
                    else
                    {
                        WriteArgument(index, ref Unsafe.As<ulong, byte>(ref argument.Primitive), sizeof(ulong));
                    }
                }
            }

            private void SetStorage<T>(int slot, [UnscopedRef] ref T value) =>
                *(ByReference*)(_storage + slot) = ByReference.Create(ref value);

            private object? ReadObject(int index, bool interior)
            {
                object? value = null;
                if (GetObject(_context, (uint)index, interior ? 1 : 0, ObjectHandleOnStack.Create(ref value)) < 0)
                {
                    throw new ArgumentException(SR.Argument_BadObjRef);
                }

                return value;
            }

            private void CapturePrimitiveArguments()
            {
                // Resolve the target only after capturing values; resolution can run its class initializer.
                for (int i = 0; i < _arguments.Length; i++)
                {
                    GetArgument(_context, (uint)i, out _arguments[i].Input);
                    NativeArgument input = _arguments[i].Input;
                    if (!IsObject(input.Type) && input.Type != CorElementType.ELEMENT_TYPE_VALUETYPE)
                    {
                        _arguments[i].Primitive = ReadPrimitive((uint)i, input);
                    }
                }
            }

            private ulong ReadPrimitive(uint index, NativeArgument input)
            {
                int size = GetPrimitiveSize(input.Type);
                if (size == 0)
                {
                    throw new ArgumentException(SR.Argument_BadObjRef);
                }

                if (input.IsHandle)
                {
                    return _context->CapturedArguments[index];
                }

                if (input.IsInRegister)
                {
                    if (ReadPrimitiveRegister(_context, index, out ulong value) == 0)
                    {
                        throw new ArgumentNullException();
                    }

                    return value;
                }

                ref byte source = ref (input.IsLiteral
                    ? ref Unsafe.AsRef<byte>(input.Literal)
                    : ref ((ByReference*)(_context->Homes + index))->Value);
                return size switch
                {
                    1 => source,
                    2 => Unsafe.ReadUnaligned<ushort>(ref source),
                    4 => Unsafe.ReadUnaligned<uint>(ref source),
                    8 => Unsafe.ReadUnaligned<ulong>(ref source),
                    _ => throw new ArgumentException(SR.Argument_BadObjRef),
                };
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
                Debug.Assert(!type.IsByRefLike);
                EnsureTypeActive(new QCallTypeHandle(ref type));
                object result = RuntimeTypeHandle.InternalAllocNoChecks(type.GetNativeTypeHandle().AsMethodTable());
                GC.KeepAlive(type);
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
                Debug.Assert(size <= sizeof(ulong));
                ref Argument argument = ref _arguments[index];
                NativeArgument input = argument.Input;
                if (input.IsInRegister)
                {
                    fixed (byte* address = &value)
                    {
                        WriteRegister(_context, (uint)index, address, size);
                    }
                    return;
                }

                if (input.IsHandle || input.Type == CorElementType.ELEMENT_TYPE_VALUETYPE)
                {
                    // These arguments were passed using their original storage.
                    return;
                }

                if (IsObject(input.Type) || argument.UsesInterior)
                {
                    Debug.Assert(size == IntPtr.Size);
                    ref object? source = ref Unsafe.As<byte, object?>(ref value);
                    if (input.IsLiteral)
                    {
                        Unsafe.WriteUnaligned(input.Literal, 0UL);
#pragma warning disable CS8500
                        *(object?*)input.Literal = source;
#pragma warning restore CS8500
                    }
                    else
                    {
                        ref byte home = ref ((ByReference*)(_context->Homes + index))->Value;
                        Unsafe.As<byte, object?>(ref home) = source;
                    }
                    return;
                }

                ulong bits = 0;
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<ulong, byte>(ref bits), ref value, size);
                if (input.Type is not (CorElementType.ELEMENT_TYPE_I8 or CorElementType.ELEMENT_TYPE_U8 or CorElementType.ELEMENT_TYPE_R8))
                {
                    bits = (nuint)bits;
                }

                if (input.IsLiteral)
                {
                    // Update the eval's private buffer, not the debugger's detached value.
                    Unsafe.WriteUnaligned(input.Literal, bits);
                    return;
                }

                bits = ConvertPrimitive(bits, input.Type);
                ref byte destination = ref ((ByReference*)(_context->Homes + index))->Value;
                switch (GetPrimitiveSize(input.Type))
                {
                    case 1:
                        destination = (byte)bits;
                        break;
                    case 2:
                        Unsafe.WriteUnaligned(ref destination, (ushort)bits);
                        break;
                    case 4:
                        Unsafe.WriteUnaligned(ref destination, (uint)bits);
                        break;
                    case 8:
                        Unsafe.WriteUnaligned(ref destination, bits);
                        break;
                    default:
                        throw new ArgumentException(SR.Argument_BadObjRef);
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
            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_EnsureFuncEvalTypeActive")]
            private static partial void EnsureTypeActive(QCallTypeHandle type);

            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_CopyFuncEvalValueTypeArgument")]
            [SuppressGCTransition]
            private static partial void CopyValueTypeArgument(Context* context, uint index, QCallTypeHandle type, void* destination);

            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_ReadFuncEvalPrimitiveRegister")]
            [SuppressGCTransition]
            private static partial int ReadPrimitiveRegister(Context* context, uint index, out ulong value);

            [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_WriteFuncEvalRegister")]
            private static partial void WriteRegister(Context* context, uint index, void* value, uint size);
        }
    }
}
