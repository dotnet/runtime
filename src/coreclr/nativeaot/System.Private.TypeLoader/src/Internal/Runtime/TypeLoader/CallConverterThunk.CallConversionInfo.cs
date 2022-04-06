// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;
using Internal.NativeFormat;
using Internal.TypeSystem;
using Internal.Runtime.CallConverter;

using ThunkKind = Internal.Runtime.TypeLoader.CallConverterThunk.ThunkKind;

namespace Internal.Runtime.TypeLoader
{
    internal enum CallConversionInfoRegistrationKind
    {
        UsesMethodSignatureAndGenericArgs,
        UsesArgIteratorData
    }

    internal class CallConversionInfo : IEquatable<CallConversionInfo>
    {
        private CallConversionInfo() { }

        private static int s_callConvertersCount;
        private static volatile CallConversionInfo[] s_callConverters = new CallConversionInfo[512];

        private static Lock s_callConvertersCacheLock = new Lock();
        private static LowLevelDictionary<CallConversionInfo, int> s_callConvertersCache = new LowLevelDictionary<CallConversionInfo, int>();

        private CallConversionInfoRegistrationKind _registrationKind;

        //
        // Thunk data
        //
        private ThunkKind _thunkKind;
        private IntPtr _targetFunctionPointer;
        private IntPtr _instantiatingArg;
        private ArgIteratorData _argIteratorData;
        private bool[] _paramsByRefForced;

        //
        // Method signature and generic context info. Signatures are parsed lazily when they are really needed
        //
        private RuntimeSignature _methodSignature;
        private volatile bool _signatureParsed;
        private RuntimeTypeHandle[] _typeArgs;
        private RuntimeTypeHandle[] _methodArgs;

        private int? _hashCode;

#if CCCONVERTER_TRACE
        internal string ThunkKindString()
        {
            switch (_thunkKind)
            {
                case ThunkKind.StandardToStandardInstantiating: return "StandardToStandardInstantiating";
                case ThunkKind.StandardToGenericInstantiating: return "StandardToGenericInstantiating";
                case ThunkKind.StandardToGenericInstantiatingIfNotHasThis: return "StandardToGenericInstantiatingIfNotHasThis";
                case ThunkKind.StandardToGenericPassthruInstantiating: return "StandardToGenericPassthruInstantiating";
                case ThunkKind.StandardToGenericPassthruInstantiatingIfNotHasThis: return "StandardToGenericPassthruInstantiatingIfNotHasThis";
                case ThunkKind.StandardToGeneric: return "StandardToGeneric";
                case ThunkKind.GenericToStandard: return "GenericToStandard";
                case ThunkKind.StandardUnboxing: return "StandardUnboxing";
                case ThunkKind.StandardUnboxingAndInstantiatingGeneric: return "StandardUnboxingAndInstantiatingGeneric";
                case ThunkKind.GenericToStandardWithTargetPointerArg: return "GenericToStandardWithTargetPointerArg";
                case ThunkKind.GenericToStandardWithTargetPointerArgAndParamArg: return "GenericToStandardWithTargetPointerArgAndParamArg";
                case ThunkKind.GenericToStandardWithTargetPointerArgAndMaybeParamArg: return "GenericToStandardWithTargetPointerArgAndMaybeParamArg";
                case ThunkKind.DelegateInvokeOpenStaticThunk: return "DelegateInvokeOpenStaticThunk";
                case ThunkKind.DelegateInvokeClosedStaticThunk: return "DelegateInvokeClosedStaticThunk";
                case ThunkKind.DelegateInvokeOpenInstanceThunk: return "DelegateInvokeOpenInstanceThunk";
                case ThunkKind.DelegateInvokeInstanceClosedOverGenericMethodThunk: return "DelegateInvokeInstanceClosedOverGenericMethodThunk";
                case ThunkKind.DelegateMulticastThunk: return "DelegateMulticastThunk";
                case ThunkKind.DelegateObjectArrayThunk: return "DelegateObjectArrayThunk";
                case ThunkKind.DelegateDynamicInvokeThunk: return "DelegateDynamicInvokeThunk";
                case ThunkKind.ReflectionDynamicInvokeThunk: return "ReflectionDynamicInvokeThunk";
            }
            return ((int)_thunkKind).LowLevelToString();
        }
#endif


        public override bool Equals(object obj)
        {
            if (this == obj) return true;

            CallConversionInfo other = obj as CallConversionInfo;
            if (other == null) return false;

            return Equals(other);
        }

        public bool Equals(CallConversionInfo other)
        {
            if (_registrationKind != other._registrationKind) return false;
            if (_thunkKind != other._thunkKind) return false;
            if (_targetFunctionPointer != other._targetFunctionPointer) return false;
            if (_instantiatingArg != other._instantiatingArg) return false;

            switch (_registrationKind)
            {
                case CallConversionInfoRegistrationKind.UsesMethodSignatureAndGenericArgs:
                    {
                        return (_methodSignature.StructuralEquals(other._methodSignature) &&
                                ArraysAreEqual(_typeArgs, other._typeArgs) &&
                                ArraysAreEqual(_methodArgs, other._methodArgs));
                    }

                case CallConversionInfoRegistrationKind.UsesArgIteratorData:
                    {
                        return _argIteratorData.Equals(other._argIteratorData) &&
                               ArraysAreEqual(_paramsByRefForced, other._paramsByRefForced);
                    }
            }

            Debug.Fail("UNREACHABLE");
            return false;
        }

        private static bool ArraysAreEqual<T>(T[] array1, T[] array2)
        {
            if (array1 == null)
                return array2 == null;

            if (array2 == null || array1.Length != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
                if (!array1[i].Equals(array2[i]))
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int hashCode = 79 + 971 * (int)_thunkKind + 83 * (_targetFunctionPointer.GetHashCode() >> 7) + 13 * (_instantiatingArg.GetHashCode() >> 3);
                switch (_registrationKind)
                {
                    case CallConversionInfoRegistrationKind.UsesMethodSignatureAndGenericArgs:
                        hashCode ^= _methodSignature.GetHashCode();
                        hashCode = _typeArgs == null ? hashCode : TypeHashingAlgorithms.ComputeGenericInstanceHashCode(hashCode, _typeArgs);
                        hashCode = _methodArgs == null ? hashCode : TypeHashingAlgorithms.ComputeGenericInstanceHashCode(hashCode, _methodArgs);
                        break;
                    case CallConversionInfoRegistrationKind.UsesArgIteratorData:
                        hashCode ^= _argIteratorData.GetHashCode();
                        break;
                }
                _hashCode = hashCode;
            }

            return _hashCode.Value;
        }

        #region Construction
        //
        // Lazily parse the method signature, and construct the call converter data
        //
        private void EnsureCallConversionInfoLoaded()
        {
            if (_signatureParsed)
                return;

            lock (this)
            {
                // Check if race was won by another thread and the signature got parsed
                if (_signatureParsed) return;

                TypeSystemContext context = TypeSystemContextFactory.Create();
                {
                    Instantiation typeInstantiation = Instantiation.Empty;
                    Instantiation methodInstantiation = Instantiation.Empty;

                    if (_typeArgs != null && _typeArgs.Length > 0)
                        typeInstantiation = context.ResolveRuntimeTypeHandles(_typeArgs);
                    if (_methodArgs != null && _methodArgs.Length > 0)
                        methodInstantiation = context.ResolveRuntimeTypeHandles(_methodArgs);

                    bool hasThis;
                    TypeDesc[] parameters;
                    bool[] paramsByRefForced;
                    if (!TypeLoaderEnvironment.Instance.GetCallingConverterDataFromMethodSignature(context, _methodSignature, typeInstantiation, methodInstantiation, out hasThis, out parameters, out paramsByRefForced))
                    {
                        Debug.Assert(false);
                        Environment.FailFast("Failed to get type handles for parameters in method signature");
                    }
                    Debug.Assert(parameters != null && parameters.Length >= 1);

                    bool[] byRefParameters = new bool[parameters.Length];
                    RuntimeTypeHandle[] parameterHandles = new RuntimeTypeHandle[parameters.Length];

                    for (int j = 0; j < parameters.Length; j++)
                    {
                        ByRefType parameterAsByRefType = parameters[j] as ByRefType;
                        if (parameterAsByRefType != null)
                        {
                            parameterAsByRefType.ParameterType.RetrieveRuntimeTypeHandleIfPossible();
                            parameterHandles[j] = parameterAsByRefType.ParameterType.RuntimeTypeHandle;
                            byRefParameters[j] = true;
                        }
                        else
                        {
                            parameters[j].RetrieveRuntimeTypeHandleIfPossible();
                            parameterHandles[j] = parameters[j].RuntimeTypeHandle;
                            byRefParameters[j] = false;
                        }

                        Debug.Assert(!parameterHandles[j].IsNull());
                    }

                    // Build thunk data
                    TypeHandle thReturnType = new TypeHandle(CallConverterThunk.GetByRefIndicatorAtIndex(0, byRefParameters), parameterHandles[0]);
                    TypeHandle[] thParameters = null;
                    if (parameters.Length > 1)
                    {
                        thParameters = new TypeHandle[parameters.Length - 1];
                        for (int i = 1; i < parameters.Length; i++)
                        {
                            thParameters[i - 1] = new TypeHandle(CallConverterThunk.GetByRefIndicatorAtIndex(i, byRefParameters), parameterHandles[i]);
                        }
                    }

                    _argIteratorData = new ArgIteratorData(hasThis, false, thParameters, thReturnType);

                    // StandardToStandard thunks don't actually need any parameters to change their ABI
                    // so don't force any params to be adjusted
                    if (!StandardToStandardThunk)
                    {
                        _paramsByRefForced = paramsByRefForced;
                    }
                }
                TypeSystemContextFactory.Recycle(context);

                _signatureParsed = true;
            }
        }

        public static int RegisterCallConversionInfo(ThunkKind thunkKind, IntPtr targetPointer, RuntimeSignature methodSignature, IntPtr instantiatingArg, RuntimeTypeHandle[] typeArgs, RuntimeTypeHandle[] methodArgs)
        {
            CallConversionInfo newConversionInfo = new CallConversionInfo();
            newConversionInfo._registrationKind = CallConversionInfoRegistrationKind.UsesMethodSignatureAndGenericArgs;
            newConversionInfo._thunkKind = thunkKind;
            newConversionInfo._targetFunctionPointer = targetPointer;
            newConversionInfo._methodSignature = methodSignature;
            newConversionInfo._instantiatingArg = instantiatingArg;
            newConversionInfo._typeArgs = typeArgs;
            newConversionInfo._methodArgs = methodArgs;
            newConversionInfo._signatureParsed = false;

            return AddConverter(newConversionInfo);
        }

        public static int RegisterCallConversionInfo(ThunkKind thunkKind,
                                                     IntPtr targetPointer,
                                                     IntPtr instantiatingArg,
                                                     bool hasThis,
                                                     TypeHandle returnType,
                                                     TypeHandle[] parameterTypes,
                                                     bool[] paramsByRefForced)
        {
            CallConversionInfo newConversionInfo = new CallConversionInfo();
            newConversionInfo._registrationKind = CallConversionInfoRegistrationKind.UsesArgIteratorData;
            newConversionInfo._thunkKind = thunkKind;
            newConversionInfo._targetFunctionPointer = targetPointer;
            newConversionInfo._instantiatingArg = instantiatingArg;
            newConversionInfo._argIteratorData = new ArgIteratorData(hasThis, false, parameterTypes, returnType);
            newConversionInfo._paramsByRefForced = paramsByRefForced;
            newConversionInfo._signatureParsed = true;

            return AddConverter(newConversionInfo);
        }

        public static int RegisterCallConversionInfo(ThunkKind thunkKind,
                                                     IntPtr targetPointer,
                                                     IntPtr instantiatingArg,
                                                     ArgIteratorData argIteratorData,
                                                     bool[] paramsByRefForced)
        {
            Debug.Assert(argIteratorData != null);

            CallConversionInfo newConversionInfo = new CallConversionInfo();
            newConversionInfo._registrationKind = CallConversionInfoRegistrationKind.UsesArgIteratorData;
            newConversionInfo._thunkKind = thunkKind;
            newConversionInfo._targetFunctionPointer = targetPointer;
            newConversionInfo._instantiatingArg = instantiatingArg;
            newConversionInfo._argIteratorData = argIteratorData;
            newConversionInfo._paramsByRefForced = paramsByRefForced;
            newConversionInfo._signatureParsed = true;

            return AddConverter(newConversionInfo);
        }

        private static int AddConverter(CallConversionInfo newConversionInfo)
        {
            using (LockHolder.Hold(s_callConvertersCacheLock))
            {
                int converterId;
                if (s_callConvertersCache.TryGetValue(newConversionInfo, out converterId))
                {
                    Debug.Assert((uint)converterId < (uint)s_callConvertersCount && s_callConverters[converterId].Equals(newConversionInfo));
                    return converterId;
                }

                if (s_callConvertersCount >= s_callConverters.Length)
                {
                    CallConversionInfo[] newArray = new CallConversionInfo[s_callConverters.Length * 2];
                    Array.Copy(s_callConverters, newArray, s_callConvertersCount);
                    s_callConverters = newArray;
                }

                s_callConverters[s_callConvertersCount++] = newConversionInfo;
                s_callConvertersCache[newConversionInfo] = s_callConvertersCount - 1;
                return s_callConvertersCount - 1;
            }
        }
        #endregion

        public static CallConversionInfo GetConverter(int id)
        {
            return s_callConverters[id];
        }

        #region Conversion Properties
        public IntPtr TargetFunctionPointer
        {
            get
            {
                return _targetFunctionPointer;
            }
        }

        public bool StandardToStandardThunk
        {
            get
            {
                switch (_thunkKind)
                {
                    case ThunkKind.StandardToStandardInstantiating:
                        return true;

                    default:
                        return false;
                }
            }
        }

        public bool CallerHasExtraParameterWhichIsFunctionTarget
        {
            get
            {
                switch (_thunkKind)
                {
                    case ThunkKind.GenericToStandardWithTargetPointerArg:
                    case ThunkKind.GenericToStandardWithTargetPointerArgAndParamArg:
                    case ThunkKind.GenericToStandardWithTargetPointerArgAndMaybeParamArg:
                        return true;
                }

                return false;
            }
        }

        public bool CalleeMayHaveParamType
        {
            get
            {
                return _thunkKind == ThunkKind.GenericToStandardWithTargetPointerArgAndMaybeParamArg;
            }
        }

        public bool IsUnboxingThunk
        {
            get
            {
                switch (_thunkKind)
                {
                    case ThunkKind.StandardUnboxing:
                    case ThunkKind.StandardUnboxingAndInstantiatingGeneric:
                        return true;
                }
                return false;
            }
        }

        public bool IsDelegateThunk
        {
            get
            {
                switch (_thunkKind)
                {
                    case ThunkKind.DelegateInvokeOpenStaticThunk:
                    case ThunkKind.DelegateInvokeClosedStaticThunk:
                    case ThunkKind.DelegateInvokeOpenInstanceThunk:
                    case ThunkKind.DelegateInvokeInstanceClosedOverGenericMethodThunk:
                    case ThunkKind.DelegateMulticastThunk:
                    case ThunkKind.DelegateObjectArrayThunk:
                    case ThunkKind.DelegateDynamicInvokeThunk:
                        return true;
                }
                return false;
            }
        }

        public bool TargetDelegateFunctionIsExtraFunctionPointerOrDataField
        {
            get
            {
                switch (_thunkKind)
                {
                    case ThunkKind.DelegateInvokeOpenStaticThunk:
                    case ThunkKind.DelegateInvokeClosedStaticThunk:
                    case ThunkKind.DelegateInvokeOpenInstanceThunk:
                    case ThunkKind.DelegateInvokeInstanceClosedOverGenericMethodThunk:
                        return true;
                }
                return false;
            }
        }

        public bool IsOpenInstanceDelegateThunk { get { return _thunkKind == ThunkKind.DelegateInvokeOpenInstanceThunk; } }

        public bool IsClosedStaticDelegate { get { return _thunkKind == ThunkKind.DelegateInvokeClosedStaticThunk; } }

        public bool IsMulticastDelegate { get { return _thunkKind == ThunkKind.DelegateMulticastThunk; } }

        public bool IsObjectArrayDelegateThunk { get { return _thunkKind == ThunkKind.DelegateObjectArrayThunk; } }

        public bool IsDelegateDynamicInvokeThunk { get { return _thunkKind == ThunkKind.DelegateDynamicInvokeThunk; } }

        public bool IsReflectionDynamicInvokerThunk { get { return _thunkKind == ThunkKind.ReflectionDynamicInvokeThunk; } }

        public bool IsAnyDynamicInvokerThunk
        {
            get
            {
                switch (_thunkKind)
                {
                    case ThunkKind.DelegateDynamicInvokeThunk:
                    case ThunkKind.ReflectionDynamicInvokeThunk:
                        return true;
                }
                return false;
            }
        }

        public bool IsStaticDelegateThunk
        {
            get
            {
                switch (_thunkKind)
                {
                    case ThunkKind.DelegateInvokeOpenStaticThunk:
                    case ThunkKind.DelegateInvokeClosedStaticThunk:
                        return true;
                }
                return false;
            }
        }

        public bool IsThisPointerInDelegateData
        {
            get
            {
                switch (_thunkKind)
                {
                    case ThunkKind.DelegateInvokeOpenInstanceThunk:
                    case ThunkKind.DelegateInvokeInstanceClosedOverGenericMethodThunk:
                    case ThunkKind.DelegateMulticastThunk:
                    case ThunkKind.DelegateDynamicInvokeThunk:
                        return true;
                }
                return false;
            }
        }

        public IntPtr InstantiatingStubArgument
        {
            get
            {
                return _instantiatingArg;
            }
        }

        public ArgIteratorData ArgIteratorData
        {
            get
            {
                EnsureCallConversionInfoLoaded();
                return _argIteratorData;
            }
        }

        public bool HasKnownTargetPointerAndInstantiatingArgument
        {
            get
            {
                // If the target method pointer and/or dictionary are passed as arguments to the converter, they are
                // considered unknown.
                // Similarly, delegate thunks and reflection DynamicInvoke thunks do not have any target pointer or
                // dictionary pointers stored in their CallConversionInfo structures.
                if (CallerHasExtraParameterWhichIsFunctionTarget || IsDelegateThunk || IsAnyDynamicInvokerThunk || _targetFunctionPointer == IntPtr.Zero)
                    return false;

                if (_instantiatingArg != IntPtr.Zero)
                    return true;

                // Null instantiating arguments are considered known values for non-instantiating stubs.
                return _thunkKind == ThunkKind.StandardToGeneric || _thunkKind == ThunkKind.GenericToStandard || _thunkKind == ThunkKind.StandardUnboxing;
            }
        }

        public bool CalleeHasParamType
        {
            get
            {
                switch (_thunkKind)
                {
                    case ThunkKind.StandardUnboxingAndInstantiatingGeneric:
                    case ThunkKind.GenericToStandardWithTargetPointerArgAndParamArg:
                    case ThunkKind.StandardToGenericInstantiating:
                    case ThunkKind.StandardToStandardInstantiating:
                    case ThunkKind.StandardToGenericPassthruInstantiating:
                        return true;

                    case ThunkKind.StandardToGenericPassthruInstantiatingIfNotHasThis:
                    case ThunkKind.StandardToGenericInstantiatingIfNotHasThis:
                        EnsureCallConversionInfoLoaded();
                        return !_argIteratorData.HasThis();
                }

                return false;
            }
        }

        public bool CallerHasParamType
        {
            get
            {
                switch (_thunkKind)
                {
                    case ThunkKind.GenericToStandardWithTargetPointerArgAndParamArg:
                    case ThunkKind.StandardToGenericPassthruInstantiating:
                        return true;

                    case ThunkKind.StandardToGenericPassthruInstantiatingIfNotHasThis:
                        return CalleeHasParamType;
                }
                return false;
            }
        }

        private bool ForcedByRefParametersAreCaller
        {
            get
            {
                switch (_thunkKind)
                {
                    case ThunkKind.GenericToStandard:
                    case ThunkKind.GenericToStandardWithTargetPointerArg:
                    case ThunkKind.GenericToStandardWithTargetPointerArgAndParamArg:
                    case ThunkKind.GenericToStandardWithTargetPointerArgAndMaybeParamArg:
                        return true;
                }
                return false;
            }
        }

        public bool[] CallerForcedByRefData
        {
            get
            {
                EnsureCallConversionInfoLoaded();

                if (ForcedByRefParametersAreCaller && !IsDelegateThunk)
                {
                    return _paramsByRefForced;
                }
                else
                {
                    return null;
                }
            }
        }

        public bool[] CalleeForcedByRefData
        {
            get
            {
                EnsureCallConversionInfoLoaded();

                if (!ForcedByRefParametersAreCaller && !IsDelegateThunk)
                {
                    return _paramsByRefForced;
                }
                else
                {
                    return null;
                }
            }
        }
        #endregion
    }
}
