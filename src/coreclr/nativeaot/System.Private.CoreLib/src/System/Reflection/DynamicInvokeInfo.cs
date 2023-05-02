// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection
{
    // caches information required for efficient argument validation and type coercion for reflection Invoke.
    [ReflectionBlocked]
    public class DynamicInvokeInfo
    {
        // Public state
        public MethodBase Method { get; }
        public IntPtr InvokeThunk { get; }

        // Private cached information
        private readonly int _argumentCount;
        private readonly bool _isStatic;
        // private readonly bool _isValueTypeInstanceMethod;
        private readonly bool _needsCopyBack;
        private readonly Transform _returnTransform;
        private readonly EETypePtr _returnType;
        private readonly ArgumentInfo[] _arguments;

        // We use negative argument count to signal unsupported invoke signatures
        private const int ArgumentCount_NotSupported = -1;
        private const int ArgumentCount_NotSupported_ByRefLike = -2;

        [Flags]
        private enum Transform
        {
            ByRef = 0x0001,
            Nullable = 0x0002,
            Pointer = 0x0004,
            Reference = 0x0008,
            FunctionPointer = 0x0010,
            AllocateReturnBox = 0x0020,
        }

        private readonly struct ArgumentInfo
        {
            internal ArgumentInfo(Transform transform, EETypePtr type)
            {
                Transform = transform;
                Type = type;
            }

            internal Transform Transform { get; }
            internal EETypePtr Type { get; }
        }

        public DynamicInvokeInfo(MethodBase method, IntPtr invokeThunk)
        {
            Method = method;
            InvokeThunk = invokeThunk;

            _isStatic = method.IsStatic;

            // _isValueTypeInstanceMethod = method.DeclaringType?.IsValueType ?? false;

            ParameterInfo[] parameters = method.GetParametersNoCopy();

            _argumentCount = parameters.Length;

            if (_argumentCount != 0)
            {
                ArgumentInfo[] arguments = new ArgumentInfo[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    Transform transform = default;

                    Type argumentType = parameters[i].ParameterType;
                    if (argumentType.IsByRef)
                    {
                        _needsCopyBack = true;
                        transform |= Transform.ByRef;
                        argumentType = argumentType.GetElementType()!;
                    }
                    Debug.Assert(!argumentType.IsByRef);

                    EETypePtr eeArgumentType = argumentType.GetEEType();

                    if (eeArgumentType.IsValueType)
                    {
                        Debug.Assert(argumentType.IsValueType);

                        if (eeArgumentType.IsByRefLike)
                            _argumentCount = ArgumentCount_NotSupported_ByRefLike;

                        if (eeArgumentType.IsNullable)
                            transform |= Transform.Nullable;
                    }
                    else if (eeArgumentType.IsPointer)
                    {
                        Debug.Assert(argumentType.IsPointer);

                        transform |= Transform.Pointer;
                    }
                    else if (eeArgumentType.IsFunctionPointer)
                    {
                        Debug.Assert(argumentType.IsFunctionPointer);

                        transform |= Transform.FunctionPointer;
                    }
                    else
                    {
                        transform |= Transform.Reference;
                    }

                    arguments[i] = new ArgumentInfo(transform, eeArgumentType);
                }
                _arguments = arguments;
            }

            if (method is MethodInfo methodInfo)
            {
                Transform transform = default;

                Type returnType = methodInfo.ReturnType;
                if (returnType.IsByRef)
                {
                    transform |= Transform.ByRef;
                    returnType = returnType.GetElementType()!;
                }
                Debug.Assert(!returnType.IsByRef);

                EETypePtr eeReturnType = returnType.GetEEType();

                if (eeReturnType.IsValueType)
                {
                    Debug.Assert(returnType.IsValueType);

                    if (returnType != typeof(void))
                    {
                        if (eeReturnType.IsByRefLike)
                            _argumentCount = ArgumentCount_NotSupported_ByRefLike;

                        if ((transform & Transform.ByRef) == 0)
                            transform |= Transform.AllocateReturnBox;

                        if (eeReturnType.IsNullable)
                            transform |= Transform.Nullable;
                    }
                    else
                    {
                        if ((transform & Transform.ByRef) != 0)
                            _argumentCount = ArgumentCount_NotSupported; // ByRef to void return
                    }
                }
                else if (eeReturnType.IsPointer)
                {
                    Debug.Assert(returnType.IsPointer);

                    transform |= Transform.Pointer;
                    if ((transform & Transform.ByRef) == 0)
                        transform |= Transform.AllocateReturnBox;
                }
                else if (eeReturnType.IsFunctionPointer)
                {
                    Debug.Assert(returnType.IsFunctionPointer);

                    transform |= Transform.FunctionPointer;
                    if ((transform & Transform.ByRef) == 0)
                        transform |= Transform.AllocateReturnBox;
                }
                else
                {
                    transform |= Transform.Reference;
                }

                _returnTransform = transform;
                _returnType = eeReturnType;
            }
        }

        public bool IsSupportedSignature => _argumentCount >= 0;

        [DebuggerGuidedStepThroughAttribute]
        public unsafe object? Invoke(
            object? thisPtr,
            IntPtr methodToCall,
            object?[]? parameters,
            BinderBundle? binderBundle,
            bool wrapInTargetInvocationException)
        {
            int argCount = parameters?.Length ?? 0;
            if (argCount != _argumentCount)
            {
                if (_argumentCount < 0)
                {
                    if (_argumentCount == ArgumentCount_NotSupported_ByRefLike)
                        throw new NotSupportedException(SR.NotSupported_ByRefLike);
                    throw new NotSupportedException();
                }

                throw new TargetParameterCountException(SR.Arg_ParmCnt);
            }

            object? returnObject = null;

            scoped ref byte thisArg = ref Unsafe.NullRef<byte>();
            if (!_isStatic)
            {
                // The caller is expected to validate this
                Debug.Assert(thisPtr != null);

                // See TODO comment in DynamicInvokeMethodThunk.NormalizeSignature
                // if (_isValueTypeInstanceMethod)
                // {
                //     // thisArg is a raw data byref for valuetype instance methods
                //     thisArg = ref thisPtr.GetRawData();
                // }
                // else
                {
                    thisArg = ref Unsafe.As<object?, byte>(ref thisPtr);
                }
            }

            scoped ref byte ret = ref Unsafe.As<object?, byte>(ref returnObject);
            if ((_returnTransform & Transform.AllocateReturnBox) != 0)
            {
                returnObject = RuntimeImports.RhNewObject(
                    (_returnTransform & (Transform.Pointer | Transform.FunctionPointer)) != 0 ?
                        EETypePtr.EETypePtrOf<IntPtr>() : _returnType);
                ret = ref returnObject.GetRawData();
            }

            if (argCount == 0)
            {
                try
                {
                    ret = ref RawCalliHelper.Call(InvokeThunk, (void*)methodToCall, ref thisArg, ref ret, null);
                    DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
                }
                catch (Exception e) when (wrapInTargetInvocationException)
                {
                    throw new TargetInvocationException(e);
                }
            }
            else if (argCount > MaxStackAllocArgCount)
            {
                ret = ref InvokeWithManyArguments( methodToCall, ref thisArg, ref ret,
                    parameters, binderBundle, wrapInTargetInvocationException);
            }
            else
            {
                StackAllocedArguments argStorage = default;
                StackAllocatedByRefs byrefStorage = default;

#pragma warning disable 8500
                CheckArguments(ref argStorage._arg0!, (ByReference*)&byrefStorage, parameters, binderBundle);
#pragma warning restore 8500

                try
                {
#pragma warning disable 8500
                    ret = ref RawCalliHelper.Call(InvokeThunk, (void*)methodToCall, ref thisArg, ref ret, &byrefStorage);
#pragma warning restore 8500
                    DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
                }
                catch (Exception e) when (wrapInTargetInvocationException)
                {
                    throw new TargetInvocationException(e);
                }
                finally
                {
                    if (_needsCopyBack)
                        CopyBack(ref argStorage._arg0!, parameters);
                }
            }

            return ((_returnTransform & (Transform.Nullable | Transform.Pointer | Transform.FunctionPointer | Transform.ByRef)) != 0) ?
                ReturnTransform(ref ret, wrapInTargetInvocationException) : returnObject;
        }

        private unsafe ref byte InvokeWithManyArguments(
            IntPtr methodToCall, ref byte thisArg, ref byte ret,
            object?[] parameters, BinderBundle binderBundle, bool wrapInTargetInvocationException)
        {
            int argCount = _argumentCount;

            // We don't check a max stack size since we are invoking a method which
            // naturally requires a stack size that is dependent on the arg count\size.
            IntPtr* pStorage = stackalloc IntPtr[2 * argCount];
            NativeMemory.Clear(pStorage, (nuint)(2 * argCount) * (nuint)sizeof(IntPtr));

#pragma warning disable 8500
            void* pByRefStorage = (ByReference*)(pStorage + argCount);
#pragma warning restore 8500

            RuntimeImports.GCFrameRegistration regArgStorage = new(pStorage, (uint)argCount, areByRefs: false);
            RuntimeImports.GCFrameRegistration regByRefStorage = new(pByRefStorage, (uint)argCount, areByRefs: true);

            try
            {
                RuntimeImports.RhRegisterForGCReporting(&regArgStorage);
                RuntimeImports.RhRegisterForGCReporting(&regByRefStorage);

                CheckArguments(ref Unsafe.As<IntPtr, object>(ref *pStorage), pByRefStorage, parameters, binderBundle);

                try
                {
                    ret = ref RawCalliHelper.Call(InvokeThunk, (void*)methodToCall, ref thisArg, ref ret, pByRefStorage);
                    DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
                }
                catch (Exception e) when (wrapInTargetInvocationException)
                {
                    throw new TargetInvocationException(e);
                }
                finally
                {
                    if (_needsCopyBack)
                        CopyBack(ref Unsafe.As<IntPtr, object>(ref *pStorage), parameters);
                }
            }
            finally
            {
                RuntimeImports.RhUnregisterForGCReporting(&regByRefStorage);
                RuntimeImports.RhUnregisterForGCReporting(&regArgStorage);
            }

            return ref ret;
        }

        private object? GetCoercedDefaultValue(int index, in ArgumentInfo argumentInfo)
        {
            object? defaultValue = Method.GetParametersNoCopy()[index].DefaultValue;
            if (defaultValue == DBNull.Value)
                throw new ArgumentException(SR.Arg_VarMissNull, "parameters");

            if (defaultValue != null && (argumentInfo.Transform & Transform.Nullable) != 0)
            {
                // In case if the parameter is nullable Enum type the ParameterInfo.DefaultValue returns a raw value which
                // needs to be parsed to the Enum type, for more info: https://github.com/dotnet/runtime/issues/12924
                EETypePtr nullableType = argumentInfo.Type.NullableType;
                if (nullableType.IsEnum)
                {
                    defaultValue = Enum.ToObject(Type.GetTypeFromEETypePtr(nullableType), defaultValue);
                }
            }

            return defaultValue;
        }

        private unsafe void CheckArguments(
            ref object copyOfParameters,
            void* byrefParameters,
            object?[] parameters,
            BinderBundle binderBundle)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                object? arg = parameters[i];

                ref readonly ArgumentInfo argumentInfo = ref _arguments[i];

            Again:
                if (arg is null)
                {
                    // null is substituded by zero-initialized value for non-reference type
                    if ((argumentInfo.Transform & Transform.Reference) == 0)
                        arg = RuntimeImports.RhNewObject(
                            (argumentInfo.Transform & (Transform.Pointer | Transform.FunctionPointer)) != 0 ?
                                EETypePtr.EETypePtrOf<IntPtr>() : argumentInfo.Type);
                }
                else
                {
                    // Check for Missing by comparing the type. It will save us from allocating the Missing instance
                    // unless it is needed.
                    if (arg.GetType() == typeof(Missing))
                    {
                        // Missing is substited by metadata default value
                        arg = GetCoercedDefaultValue(i, in argumentInfo);

                        // The metadata default value is written back into the parameters array
                        parameters[i] = arg;
                        if (arg is null)
                            goto Again; // Redo the argument handling to deal with null
                    }

                    EETypePtr srcEEType = arg.GetEETypePtr();
                    EETypePtr dstEEType = argumentInfo.Type;

                    if (!(srcEEType.RawValue == dstEEType.RawValue ||
                        RuntimeImports.AreTypesAssignable(srcEEType, dstEEType) ||
                        (dstEEType.IsInterface && arg is System.Runtime.InteropServices.IDynamicInterfaceCastable castable
                            && castable.IsInterfaceImplemented(new RuntimeTypeHandle(dstEEType), throwIfNotImplemented: false))))
                    {
                        // ByRefs have to be exact match
                        if ((argumentInfo.Transform & Transform.ByRef) != 0)
                            throw InvokeUtils.CreateChangeTypeArgumentException(srcEEType, argumentInfo.Type, destinationIsByRef: true);

                        arg = InvokeUtils.CheckArgumentConversions(arg, argumentInfo.Type, InvokeUtils.CheckArgumentSemantics.DynamicInvoke, binderBundle);
                    }

                    if ((argumentInfo.Transform & Transform.Reference) == 0)
                    {
                        if ((argumentInfo.Transform & (Transform.ByRef | Transform.Nullable)) != 0)
                        {
                            // Rebox the value to avoid mutating the original box. This also takes care of
                            // T -> Nullable<T> transformation as side-effect.
                            object box = RuntimeImports.RhNewObject(argumentInfo.Type);
                            RuntimeImports.RhUnbox(arg, ref box.GetRawData(), argumentInfo.Type);
                            arg = box;
                        }
                    }
                }

                // We need to perform type safety validation against the incoming arguments, but we also need
                // to be resilient against the possibility that some other thread (or even the binder itself!)
                // may mutate the array after we've validated the arguments but before we've properly invoked
                // the method. The solution is to copy the arguments to a different, not-user-visible buffer
                // as we validate them. This separate array is also used to hold default values when 'null'
                // is specified for value types, and also used to hold the results from conversions such as
                // from Int16 to Int32.

                Unsafe.Add(ref copyOfParameters, i) = arg!;

#pragma warning disable 8500, 9094
                ((ByReference*)byrefParameters)[i] = new ByReference(ref (argumentInfo.Transform & Transform.Reference) != 0 ?
                    ref Unsafe.As<object, byte>(ref Unsafe.Add(ref copyOfParameters, i)) : ref arg.GetRawData());
#pragma warning restore 8500, 9094
            }
        }

        private unsafe void CopyBack(ref object copyOfParameters, object?[] parameters)
        {
            ArgumentInfo[] arguments = _arguments;

            for (int i = 0; i < arguments.Length; i++)
            {
                ref readonly ArgumentInfo argumentInfo = ref arguments[i];

                Transform transform = argumentInfo.Transform;

                if ((transform & Transform.ByRef) == 0)
                    continue;

                object obj = Unsafe.Add(ref copyOfParameters, i);

                if ((transform & (Transform.Pointer | Transform.FunctionPointer | Transform.Nullable)) != 0)
                {
                    if ((transform & Transform.Pointer) != 0)
                    {
                        Type type = Type.GetTypeFromEETypePtr(argumentInfo.Type);
                        Debug.Assert(type.IsPointer);
                        obj = Pointer.Box((void*)Unsafe.As<byte, IntPtr>(ref obj.GetRawData()), type);
                    }
                    if ((transform & Transform.FunctionPointer) != 0)
                    {
                        obj = RuntimeImports.RhBox(EETypePtr.EETypePtrOf<IntPtr>(), ref obj.GetRawData());
                    }
                    else
                    {
                        obj = RuntimeImports.RhBox(argumentInfo.Type, ref obj.GetRawData());
                    }
                }

                parameters[i] = obj;
            }
        }

        private unsafe object ReturnTransform(ref byte byref, bool wrapInTargetInvocationException)
        {
            if (Unsafe.IsNullRef(ref byref))
            {
                Debug.Assert((_returnTransform & Transform.ByRef) != 0);
                Exception exception = new NullReferenceException(SR.NullReference_InvokeNullRefReturned);
                if (wrapInTargetInvocationException)
                    exception = new TargetInvocationException(exception);
                throw exception;
            }

            object obj;
            if ((_returnTransform & Transform.Pointer) != 0)
            {
                Type type = Type.GetTypeFromEETypePtr(_returnType);
                Debug.Assert(type.IsPointer);
                obj = Pointer.Box((void*)Unsafe.As<byte, IntPtr>(ref byref), type);
            }
            else if ((_returnTransform & Transform.Reference) != 0)
            {
                Debug.Assert((_returnTransform & Transform.ByRef) != 0);
                obj = Unsafe.As<byte, object>(ref byref);
            }
            else
            {
                Debug.Assert((_returnTransform & (Transform.ByRef | Transform.Nullable)) != 0);
                obj = RuntimeImports.RhBox(_returnType, ref byref);
            }
            return obj;
        }

        private const int MaxStackAllocArgCount = 4;

        // Helper struct to avoid intermediate object[] allocation in calls to the native reflection stack.
        // When argument count <= MaxStackAllocArgCount, define a local of type default(StackAllocatedByRefs)
        // and pass it to CheckArguments().
        // For argument count > MaxStackAllocArgCount, do a stackalloc of void* pointers along with
        // GCReportingRegistration to safely track references.
        [InlineArray(MaxStackAllocArgCount)]
        private ref struct StackAllocedArguments
        {
            internal object? _arg0;
        }

        // Helper struct to avoid intermediate IntPtr[] allocation and RegisterForGCReporting in calls to the native reflection stack.
        [InlineArray(MaxStackAllocArgCount)]
        private ref struct StackAllocatedByRefs
        {
            internal ref byte _arg0;
        }
    }
}
