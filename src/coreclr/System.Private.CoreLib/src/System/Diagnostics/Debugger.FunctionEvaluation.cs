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
#pragma warning disable CS8500
                internal object?* Objects;
#pragma warning restore CS8500
                internal void** Interiors;
                internal void** Homes;
                internal ulong* CapturedArguments;
                internal void** ResultOwner;
                internal void** TemporaryResultOwner;
                internal void** ResultByRefs;
                internal void* ResultData;
                internal void* ResultHandle;
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
                ValueTypeResult = 64,
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
            private readonly InvokerEmitUtil.InvokeFunc_Debugger _invoke;
            internal bool IsNewObject() => (_context->Flags & EvaluationFlags.NewObject) != 0;

            private bool UsesExternalResult => (_context->Flags & EvaluationFlags.ExternalResult) != 0;

            private bool UsesValueTypeResult => (_context->Flags & EvaluationFlags.ValueTypeResult) != 0;

            [DebuggerHidden]
            internal static void Run(Context* context)
            {
                const int MaxStackArguments = 14;
                int argumentCount = checked((int)context->ArgumentCount);
                int storageCount = checked(argumentCount + 2);
                InlineArray16<IntPtr> inlineStorage = default;
                Span<IntPtr> storage = argumentCount <= MaxStackArguments
                    ? ((Span<IntPtr>)inlineStorage).Slice(0, storageCount)
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
                IRuntimeMethodInfo? methodOwner = null;
                GetMethod(context,
                    ObjectHandleOnStack.Create(ref declaringType), ObjectHandleOnStack.Create(ref methodOwner));
                Debug.Assert(declaringType is not null);
                Debug.Assert(methodOwner is not null);
                bool isStatic = (context->Flags & EvaluationFlags.Static) != 0;
                if ((ulong)context->ArgumentCount + (IsNewObject() ? 1UL : 0UL) !=
                    (ulong)context->ParameterCount + (isStatic ? 0UL : 1UL))
                {
                    throw new TargetParameterCountException(SR.Arg_ParmCnt);
                }

                MethodBase? method = RuntimeType.GetMethodBase(declaringType, methodOwner);
                Debug.Assert(method is RuntimeMethodInfo or RuntimeConstructorInfo);
                Debug.Assert(!IsNewObject() || method is RuntimeConstructorInfo);
                _method = method;
                _hasReceiver = !isStatic && !IsNewObject();
                _objects = new object?[argumentCount];

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
                PrepareResult(context);

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

                _invoke = method is RuntimeMethodInfo methodInfo
                    ? methodInfo.Invoker.GetDebuggerInvokeDelegate()
                    : ((RuntimeConstructorInfo)method).GetDebuggerInvokeDelegate();
            }

            [DebuggerHidden]
            internal void Invoke()
            {
                int resultSlot = checked((int)_context->ParameterCount + 1);
                CorElementType returnType = (CorElementType)_context->ReturnElementType;
                object? resultObject = null;
                if (UsesExternalResult || UsesValueTypeResult)
                {
                    SetStorage(resultSlot, ref *(byte*)_context->ResultData);
                }
                else if (IsObject(returnType))
                {
                    SetStorage(resultSlot, ref resultObject);
                }
                else if (returnType == CorElementType.ELEMENT_TYPE_BYREF)
                {
                    SetStorage(resultSlot, ref *(byte*)_context->ResultByRefs);
                }
                else if (returnType != CorElementType.ELEMENT_TYPE_VOID)
                {
                    SetStorage(resultSlot, ref *(byte*)_context->ResultData);
                }

                object? newObject = _invoke(this, _storage);
                if (UsesValueTypeResult)
                {
                    RuntimeType type = IsNewObject()
                        ? (RuntimeType)_method.DeclaringType!
                        : (RuntimeType)((RuntimeMethodInfo)_method).ReturnType;
                    ref byte source = ref *(byte*)_context->ResultData;
                    resultObject = CastHelpers.Box(type.GetNativeTypeHandle().AsMethodTable(), ref source);
                }
                else if (IsNewObject() && !UsesExternalResult)
                {
                    resultObject = newObject;
                    Debug.Assert(resultObject is not null);
                }

                // Matches native's boxing-policy computation (DebuggerEval::m_retValueBoxing) so the
                // completion step in funceval.cpp can tell what, if anything, it should publish.
                bool boxed = !UsesExternalResult &&
                    (IsNewObject() || returnType == CorElementType.ELEMENT_TYPE_VALUETYPE);
                if (boxed || IsObject(returnType))
                {
                    // The strong handle is what crosses back into native code; once allocated it is
                    // a stable, self-rooting reference, so resultObject needs no further protection.
                    _context->ResultHandle = (void*)GCHandle.ToIntPtr(GCHandle.Alloc(resultObject));
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

                // input.Type describes the debugger's transport representation, not the signature type.
                // Boxed value types arrive as objects; Nullable<T> specifically arrives as null or boxed T,
                // so materialize a true nullable box when mutable Nullable<T> storage is required.
                // A debugger handle is not a writable nullable home: mutations remain in this temporary
                // and are not copied back to the handle's target.
                if (IsObject(input.Type))
                {
                    object? value = ReadObject(index, interior: false);
                    if (type.IsValueType)
                    {
                        if (type.IsNullableOfT)
                        {
                            if (!isReceiver || value is null || value.GetType() != type)
                            {
                                if (value is not null)
                                {
                                    Type valueType = value.GetType();
                                    if (valueType != type && !valueType.IsEquivalentTo(Nullable.GetUnderlyingType(type)))
                                    {
                                        throw new ArgumentException(SR.Argument_BadObjRef);
                                    }
                                }

                                value = RuntimeMethodHandle.ReboxToNullable(value, type);
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

                            value = actualType.Box(ref *(byte*)(_context->CapturedArguments + index));
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
                        if (type.IsByRefLike && !isByRef && !isReceiver)
                        {
                            // Copy by-value ref structs into an exact managed local so their byrefs
                            // are reported by normal JIT GC info throughout the evaluated call.
                            argument.IsTypedLocal = true;
                        }
                        else
                        {
                            SetStorage(slot, ref ((ByReference*)(_context->Interiors + index))->Value);
                        }
                    }
                    else
                    {
                        if (!type.IsValueType || type.GetNativeTypeHandle().AsMethodTable()->GetNumInstanceFieldBytes() > sizeof(ulong))
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
                            ref byte source = ref *(byte*)(_context->CapturedArguments + index);
                            object temporary;
                            if (type.IsNullableOfT)
                            {
                                temporary = RuntimeMethodHandle.BoxToNullable(ref source, type);
                            }
                            else
                            {
                                temporary = type.Box(ref source)!;
                            }

                            _objects[index] = temporary;
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
                        argument.SignatureType == CorElementType.ELEMENT_TYPE_BOOLEAN)
                    {
                        value = NormalizeBoolValue(value);
                    }

                    argument.Primitive = 0;
                    Span<byte> primitive = MemoryMarshal.AsBytes(
                        MemoryMarshal.CreateSpan(ref argument.Primitive, 1));
                    WritePrimitive(primitive[..GetPrimitiveSize(argument.SignatureType)], value);
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
            internal bool UsesTypedArgument(int slot)
            {
                int index = slot + (_hasReceiver ? 0 : -1);
                return _arguments[index].IsTypedLocal;
            }

            [DebuggerHidden]
            internal void InitializeTypedArgument<T>(int slot, [UnscopedRef] ref T storage)
                where T : allows ref struct
            {
                int index = slot + (_hasReceiver ? 0 : -1);
                ref Argument argument = ref _arguments[index];
                Debug.Assert(argument.IsTypedLocal);
                Debug.Assert(argument.Input.HasMemory || sizeof(T) <= sizeof(ulong));
                ref byte source = ref (argument.Input.HasMemory
                    ? ref ((ByReference*)(_context->Interiors + index))->Value
                    : ref *(byte*)(_context->CapturedArguments + index));
                storage = Unsafe.As<byte, T>(ref source);
                SetStorage(slot, ref storage);
            }

            [DebuggerHidden]
            internal void CopyBackTypedArgument<T>(int slot, ref T storage)
                where T : allows ref struct
            {
                int index = slot + (_hasReceiver ? 0 : -1);
                Debug.Assert(_arguments[index].IsTypedLocal);
                WriteArgument(index, ref Unsafe.As<T, byte>(ref storage), (uint)sizeof(T));
            }

            [DebuggerHidden]
            internal bool NeedsCopyBack(int slot)
            {
                int index = slot + (_hasReceiver ? 0 : -1);
                ref Argument argument = ref _arguments[index];
                NativeArgument input = argument.Input;
                if (slot == 0)
                {
                    return _hasReceiver && argument.Type.IsValueType &&
                        input.Type == CorElementType.ELEMENT_TYPE_VALUETYPE && input.IsInRegister;
                }

                Debug.Assert(argument.IsByRef);
                if (argument.UsesDirectHome)
                {
                    return false;
                }

                if (input.IsHandle)
                {
                    return false;
                }

                return argument.IsBoxedTemporary || IsObject(input.Type) ||
                    input.Type != CorElementType.ELEMENT_TYPE_VALUETYPE;
            }

            [DebuggerHidden]
            internal void CopyBackArgument(int slot)
            {
                Debug.Assert(NeedsCopyBack(slot));
                int index = slot + (_hasReceiver ? 0 : -1);
                ref Argument argument = ref _arguments[index];
                NativeArgument input = argument.Input;
                if (argument.IsBoxedTemporary)
                {
                    WriteArgument(index, ref _objects[index]!.GetRawData(),
                        argument.Type.GetNativeTypeHandle().AsMethodTable()->GetNumInstanceFieldBytes());
                }
                // Convert mutable value-type storage back to the debugger's object representation.
                // This path is an object-reference home, not a debugger handle. Nullable<T> is
                // normalized to null or boxed T before replacing that home.
                else if (IsObject(input.Type))
                {
                    Debug.Assert(!input.IsHandle);
                    _objects[index] = RuntimeMethodHandle.ReboxFromNullable(_objects[index]);
                    WriteObjectArgument(index, ref _objects[index]);
                }
                else
                {
                    Debug.Assert(input.Type != CorElementType.ELEMENT_TYPE_VALUETYPE);
                    if (argument.UsesInterior)
                    {
                        WriteObjectArgument(index, ref _objects[index]);
                    }
                    else
                    {
                        Span<byte> primitive = MemoryMarshal.AsBytes(
                            MemoryMarshal.CreateSpan(ref argument.Primitive, 1));
                        primitive = primitive[..GetPrimitiveSize(argument.SignatureType)];
                        WriteArgument(index, ref MemoryMarshal.GetReference(primitive), (uint)primitive.Length);
                    }
                }
            }

            private void SetStorage<T>(int slot, [UnscopedRef] ref T value)
                where T : allows ref struct =>
                *(ByReference*)(_storage + slot) = new(ref Unsafe.As<T, byte>(ref value));

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
                    ? ref *input.Literal
                    : ref ((ByReference*)(_context->Homes + index))->Value);
                return ReadPrimitive(ref source, size);
            }

            private static ulong ReadPrimitive(ref byte source, int size)
            {
                ReadOnlySpan<byte> bytes = MemoryMarshal.CreateReadOnlySpan(ref source, size);
                return size switch
                {
                    1 => source,
                    2 => BitConverter.ToUInt16(bytes),
                    4 => BitConverter.ToUInt32(bytes),
                    8 => BitConverter.ToUInt64(bytes),
                    _ => throw new ArgumentException(SR.Argument_BadObjRef),
                };
            }

            private static void WritePrimitive(Span<byte> destination, ulong value)
            {
                bool written;
                switch (destination.Length)
                {
                    case 1:
                        destination[0] = (byte)value;
                        return;
                    case 2:
                        written = BitConverter.TryWriteBytes(destination, (ushort)value);
                        break;
                    case 4:
                        written = BitConverter.TryWriteBytes(destination, (uint)value);
                        break;
                    case 8:
                        written = BitConverter.TryWriteBytes(destination, value);
                        break;
                    default:
                        throw new ArgumentException(SR.Argument_BadObjRef);
                }
                Debug.Assert(written);
            }

            private static ulong NormalizeBoolValue(ulong value) => value == 0 ? 0UL : 1UL;

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

                Debug.Assert(!IsObject(input.Type) && !argument.UsesInterior);
                ulong bits = ReadPrimitive(ref value, checked((int)size));
                if (input.Type is not (CorElementType.ELEMENT_TYPE_I8 or CorElementType.ELEMENT_TYPE_U8 or CorElementType.ELEMENT_TYPE_R8))
                {
                    bits = (nuint)bits;
                }

                if (input.IsLiteral)
                {
                    // Update the eval's private buffer, not the debugger's detached value.
                    Span<byte> literal = new(input.Literal, sizeof(ulong));
                    literal.Clear();
                    WritePrimitive(literal[..GetPrimitiveSize(input.Type)], bits);
                    return;
                }

                if (input.Type == CorElementType.ELEMENT_TYPE_BOOLEAN)
                {
                    bits = NormalizeBoolValue(bits);
                }
                ref byte destination = ref ((ByReference*)(_context->Homes + index))->Value;
                WritePrimitive(MemoryMarshal.CreateSpan(ref destination, GetPrimitiveSize(input.Type)), bits);
            }

            private void WriteObjectArgument(int index, ref object? value)
            {
                ref Argument argument = ref _arguments[index];
                NativeArgument input = argument.Input;
                Debug.Assert(IsObject(input.Type) || argument.UsesInterior);
                if (input.IsInRegister)
                {
                    WriteObjectRegister(_context, (uint)index, ObjectHandleOnStack.Create(ref value));
                    return;
                }

                if (input.IsHandle)
                {
                    // The argument used the handle's object-reference slot directly.
                    return;
                }

                if (input.IsLiteral)
                {
                    // Update the eval's private buffer, not the debugger's detached value.
                    Span<byte> literal = new(input.Literal, sizeof(ulong));
                    literal.Clear();
#pragma warning disable CS8500
                    *(object?*)input.Literal = value;
#pragma warning restore CS8500
                    return;
                }

                ref byte destination = ref ((ByReference*)(_context->Homes + index))->Value;
                Unsafe.As<byte, object?>(ref destination) = value;
            }

            [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_GetFuncEvalMethod")]
            private static partial void GetMethod(Context* context, ObjectHandleOnStack declaringType, ObjectHandleOnStack methodOwner);

            [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_PrepareFuncEvalResult")]
            private static partial void PrepareResult(Context* context);

            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_GetFuncEvalArgument")]
            [SuppressGCTransition]
            private static partial void GetArgument(Context* context, uint index, out NativeArgument argument);

            [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_GetFuncEvalArgumentType")]
            private static partial void GetArgumentType(Context* context, uint index, ObjectHandleOnStack type);

            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_GetFuncEvalObject")]
            [SuppressGCTransition]
            private static partial int GetObject(Context* context, uint index, int interior, ObjectHandleOnStack value);

            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_ReadFuncEvalPrimitiveRegister")]
            [SuppressGCTransition]
            private static partial int ReadPrimitiveRegister(Context* context, uint index, out ulong value);

            [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_WriteFuncEvalObjectRegister")]
            private static partial void WriteObjectRegister(Context* context, uint index, ObjectHandleOnStack value);

            [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DebugDebugger_WriteFuncEvalRegister")]
            private static partial void WriteRegister(Context* context, uint index, void* value, uint size);
        }
    }
}
