// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices;

public static partial class RuntimeHelpers
{
    private static partial class DelegateTypeFactory
    {
        private static readonly DelegateTypeCache s_nonCollectibleCache = new();
        private static readonly ConditionalWeakTable<LoaderAllocator, DelegateTypeCache> s_collectibleCaches = new();

        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Delegate_GetTypeLoaderAllocator")]
        private static unsafe partial void GetTypeLoaderAllocator(nint* signature, int signatureLength, ObjectHandleOnStack loaderAllocator);

        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Delegate_CreateType")]
        private static unsafe partial void CreateType(nint* signature, int signatureLength, ObjectHandleOnStack assembly, ObjectHandleOnStack result);

        internal static unsafe Type GetCustomDelegateType(Type[] typeArgs)
        {
            RuntimeType[] signature = GetSignature(typeArgs, out bool isCollectible);

            if (!isCollectible)
            {
                return s_nonCollectibleCache.GetDelegateType(signature);
            }

            nint[] handles = GetTypeHandles(signature);
            LoaderAllocator? loaderAllocator = null;
            fixed (nint* signaturePtr = handles)
            {
                GetTypeLoaderAllocator(signaturePtr, signature.Length, ObjectHandleOnStack.Create(ref loaderAllocator));
            }

            DelegateTypeCache cache = s_collectibleCaches.GetValue(loaderAllocator!, static _ => new DelegateTypeCache());
            Type result = cache.GetDelegateType(signature, handles);
            GC.KeepAlive(signature);
            return result;
        }

        private static nint[] GetTypeHandles(RuntimeType[] signature)
        {
            nint[] handles = new nint[signature.Length];
            for (int i = 0; i < handles.Length; i++)
            {
                handles[i] = signature[i].TypeHandle.Value;
            }
            return handles;
        }

        private sealed class DelegateTypeCache
        {
            private readonly Dictionary<RuntimeType[], Type> _types = new(SignatureComparer.Instance);
            private RuntimeAssembly? _assembly;

            internal unsafe Type GetDelegateType(RuntimeType[] signature, nint[]? handles = null)
            {
                lock (_types)
                {
                    if (_types.TryGetValue(signature, out Type? result))
                    {
                        return result;
                    }

                    handles ??= GetTypeHandles(signature);
                    RuntimeAssembly? assembly = _assembly;
                    try
                    {
                        fixed (nint* signaturePtr = handles)
                        {
                            CreateType(signaturePtr, signature.Length, ObjectHandleOnStack.Create(ref assembly), ObjectHandleOnStack.Create(ref result));
                        }
                    }
                    finally
                    {
                        _assembly = assembly;
                        GC.KeepAlive(signature);
                    }

                    _types.Add(signature, result!);
                    return result!;
                }
            }
        }

    }
}
