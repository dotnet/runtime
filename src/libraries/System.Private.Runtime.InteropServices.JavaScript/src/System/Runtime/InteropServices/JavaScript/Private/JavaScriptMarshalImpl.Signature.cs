// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript.Private
{
    internal static unsafe partial class JavaScriptMarshalImpl
    {
        internal static JSMarshalerSig exceptionSignature = GetExceptionSignature();
        internal static JSMarshalerSig voidSignature = GetVoidSignature();

        internal static IntPtr boolType = typeof(bool).TypeHandle.Value;
        internal static IntPtr byteType = typeof(byte).TypeHandle.Value;
        internal static IntPtr int16Type = typeof(short).TypeHandle.Value;
        internal static IntPtr int32Type = typeof(int).TypeHandle.Value;
        internal static IntPtr int64Type = typeof(long).TypeHandle.Value;
        internal static IntPtr floatType = typeof(float).TypeHandle.Value;
        internal static IntPtr doubleType = typeof(double).TypeHandle.Value;
        internal static IntPtr intptrType = typeof(IntPtr).TypeHandle.Value;
        internal static IntPtr dateTimeType = typeof(DateTime).TypeHandle.Value;
        internal static IntPtr dateTimeOffsetType = typeof(DateTimeOffset).TypeHandle.Value;

        internal static IntPtr stringType = typeof(string).TypeHandle.Value;
        internal static IntPtr ijsObjectType = typeof(IJSObject).TypeHandle.Value;
        internal static IntPtr objectType = typeof(object).TypeHandle.Value;
        internal static IntPtr exceptionType = typeof(Exception).TypeHandle.Value;
        internal static IntPtr taskType = typeof(Task).TypeHandle.Value;
        internal static IntPtr tcsType = typeof(TaskCompletionSource).TypeHandle.Value;
        internal static IntPtr jsExceptionType = typeof(JSException).TypeHandle.Value;

        private static Dictionary<Type[], JavaScriptMarshalerSignature> methodSignatures = new Dictionary<Type[], JavaScriptMarshalerSignature>(new TypeArrayEqualityComparer());

        internal static JSMarshalerSig GetExceptionSignature()
        {
            return new JSMarshalerSig
            {
                TypeHandle = typeof(Exception).TypeHandle.Value,
                BufferLength = -1,
                BufferOffset = -1,
                UseRoot = 0,
                MarshallerJSHandle = IntPtr.Zero,
            };
        }

        internal static JSMarshalerSig GetVoidSignature()
        {
            return new JSMarshalerSig
            {
                TypeHandle = IntPtr.Zero,
                BufferLength = -1,
                BufferOffset = -1,
                UseRoot = 0,
                MarshallerJSHandle = IntPtr.Zero,
            };
        }

        internal static JSMarshalerSig GetArgumentSignature(JavaScriptMarshalerBase[] customMarshalers, Type argType, ref int extraBufferLength)
        {
            if (argType.IsPrimitive)
            {
                return new JSMarshalerSig
                {
                    TypeHandle = argType.TypeHandle.Value,
                    BufferLength = -1,
                    BufferOffset = -1,
                    UseRoot = 0,
                    MarshallerJSHandle = IntPtr.Zero,
                };
            }
            if (marshalers.TryGetValue(argType, out var m1))
            {
                int argBufferLength = m1.GetFixedBufferLength();
                var sig = new JSMarshalerSig
                {
                    TypeHandle = m1.MarshaledType.TypeHandle.Value,
                    BufferLength = argBufferLength != 0
                        ? argBufferLength
                        : -1,
                    BufferOffset = argBufferLength == 0 ? -1 : extraBufferLength,
                    UseRoot = m1.GetUseRoot() ? 1 : 0,
                    MarshallerJSHandle = m1.MarshallerJSHandle,
                };
                extraBufferLength += argBufferLength;
                return sig;
            }
            if (customMarshalers != null) foreach (var m3 in customMarshalers)
            {
                    if (m3.MarshaledType.IsAssignableFrom(argType))
                    {
                        int argBufferLength = m3.GetFixedBufferLength();
                        var sig = new JSMarshalerSig
                        {
                            TypeHandle = m3.MarshaledType.TypeHandle.Value,
                            BufferLength = argBufferLength != 0
                                ? argBufferLength
                                : -1,
                            BufferOffset = argBufferLength == 0 ? -1 : extraBufferLength,
                            UseRoot = m3.GetUseRoot() ? 1 : 0,
                            MarshallerJSHandle = m3.MarshallerJSHandle,
                        };
                        extraBufferLength += argBufferLength;
                        return sig;
                    }
                }
            foreach (var m2 in marshalersSequence)
            {
                if (m2.MarshaledType.IsAssignableFrom(argType))
                {
                    int argBufferLength = m2.GetFixedBufferLength();
                    var sig = new JSMarshalerSig
                    {
                        TypeHandle = m2.MarshaledType.TypeHandle.Value,
                        BufferLength = argBufferLength != 0
                            ? argBufferLength
                            : -1,
                        BufferOffset = argBufferLength == 0 ? -1 : extraBufferLength,
                        UseRoot = m2.GetUseRoot() ? 1 : 0,
                        MarshallerJSHandle = m2.MarshallerJSHandle,
                    };
                    extraBufferLength += argBufferLength;
                    return sig;
                }
            }
            // fallback System.Object
            return new JSMarshalerSig
            {
                TypeHandle = objectType,
                BufferLength = -1,
                BufferOffset = -1,
                MarshallerJSHandle = IntPtr.Zero,
                UseRoot = 1,
            };
        }
        // res type is first
        internal static JavaScriptMarshalerSignature GetMethodSignature(JavaScriptMarshalerBase[] customMarshalers, params Type[] types)
        {

            lock (methodSignatures)
            {
                // TODO the cache doesn't differentiate customMarshalers, just the types they marshal
                if (methodSignatures.TryGetValue(types, out var signature))
                {
                    // copy as it could have different JSHandle
                    return new JavaScriptMarshalerSignature
                    {
                        Header = signature.Header,
                        Sigs = signature.Sigs,
                        CustomMarshalers = signature.CustomMarshalers,
                        Types = signature.Types,
                    };
                }

                int argsCount = types.Length - 1;
                var size = sizeof(JSMarshalerSignatureHeader) + ((2 + argsCount) * sizeof(JSMarshalerSig));
                // this is allocated outside of GC, so that it doesn't move in memory. We send it to JS side.
                var buffer = Marshal.AllocHGlobal(size);
                var header = (JSMarshalerSignatureHeader*)buffer;
                var args = (JSMarshalerSig*)(buffer + sizeof(JSMarshalerSignatureHeader));
                signature = new JavaScriptMarshalerSignature
                {
                    Header = (JSMarshalerSignatureHeader*)buffer,
                    Sigs = (JSMarshalerSig*)(buffer + sizeof(JSMarshalerSignatureHeader)),
                    CustomMarshalers = customMarshalers,
                    Types = types,
                };

                var extraBufferLength = 0;
                signature.ArgumentCount = argsCount;
                signature.Exception = exceptionSignature;
                var resType = types[0];
                signature.Result = resType == typeof(void)
                    ? voidSignature
                    : GetArgumentSignature(customMarshalers, resType, ref extraBufferLength);
                for (int i = 0; i < argsCount; i++)
                {
                    args[i] = GetArgumentSignature(customMarshalers, types[i+1], ref extraBufferLength);
                }
                signature.ExtraBufferLength = extraBufferLength;
                methodSignatures.Add(types, signature);
                return signature;
            }
        }

        internal class TypeArrayEqualityComparer : IEqualityComparer<Type[]>
        {
            public bool Equals(Type[]? first, Type[]? second)
            {
                if (first == second)
                {
                    return true;
                }
                if (first == null || second == null)
                {
                    return false;
                }
                if (first.Length != second.Length)
                {
                    return false;
                }
                for (int i = 0; i < first.Length; i++)
                {
                    if (first[i] != second[i])
                    {
                        return false;
                    }
                }
                return true;
            }

            public int GetHashCode(Type[] obj)
            {
                var hc = default(HashCode);
                foreach (var type in obj)
                {
                    hc.Add(type);
                }
                return hc.ToHashCode();
            }
        }
    }
}
