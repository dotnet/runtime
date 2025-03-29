// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;

namespace System.Runtime.InteropServices
{
    [RequiresUnreferencedCode("Lazy TypeMap isn't supported for Trimmer scenarios")]
    internal static partial class TypeMapLazyDictionary
    {
        private ref struct CallbackContext
        {
            private LazyExternalTypeDictionary? _externalTypeMap;
            private LazyProxyTypeDictionary? _proxyTypeMap;

            public LazyExternalTypeDictionary ExternalTypeMap
            {
                [RequiresUnreferencedCode("Lazy TypeMap isn't supported for Trimmer scenarios")]
                get
                {
                    _externalTypeMap ??= new LazyExternalTypeDictionary();
                    return _externalTypeMap;
                }
            }

            public LazyProxyTypeDictionary ProxyTypeMap
            {
                [RequiresUnreferencedCode("Lazy TypeMap isn't supported for Trimmer scenarios")]
                get
                {
                    _proxyTypeMap ??= new LazyProxyTypeDictionary();
                    return _proxyTypeMap;
                }
            }

            public ExceptionDispatchInfo? CreationException { get; set; }
        }

        // See assemblynative.hpp for native version.
        public unsafe struct ProcessAttributesCallbackArg
        {
            public void* Utf8String1;
            public void* Utf8String2;
            public void* Utf8String3;
            public int StringLen1;
            public int StringLen2;
            public int StringLen3;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeMapLazyDictionary_ProcessAttributes")]
        private static unsafe partial void ProcessAttributes(
            QCallAssembly assembly,
            QCallTypeHandle groupType,
            delegate* unmanaged<CallbackContext*, ProcessAttributesCallbackArg*, void> newExternalTypeEntry,
            delegate* unmanaged<CallbackContext*, ProcessAttributesCallbackArg*, void> newProxyTypeEntry,
            CallbackContext* context);

        private static TypeName CreateNewTypeName(string typeName, string assemblyName)
        {
            AssemblyNameInfo asmNameInfo = AssemblyNameInfo.Parse(assemblyName);
            TypeName parsedType = new TypeName(typeName, asmNameInfo);
            Debug.Assert(parsedType.AssemblyName != null);
            return parsedType;
        }

        [UnmanagedCallersOnly]
        private static unsafe void NewExternalTypeEntry(CallbackContext* context, ProcessAttributesCallbackArg* arg)
        {
            Debug.Assert(context != null);
            Debug.Assert(arg != null);
            Debug.Assert(arg->Utf8String1 != null);
            Debug.Assert(arg->Utf8String2 != null);

            try
            {
                string externalTypeName = new((sbyte*)arg->Utf8String1, 0, arg->StringLen1, Encoding.UTF8);
                string targetType = new((sbyte*)arg->Utf8String2, 0, arg->StringLen2, Encoding.UTF8);

                TypeName parsedTarget = TypeNameParser.Parse(targetType, throwOnError: true)!;
                if (parsedTarget.AssemblyName is null)
                {
                    // The assembly name is not included in the type name, so use the fallback assembly name.
                    Debug.Assert(arg->Utf8String3 != null);
                    string fallbackAssemblyName = new((sbyte*)arg->Utf8String3, 0, arg->StringLen3, Encoding.UTF8);
                    parsedTarget = CreateNewTypeName(targetType, fallbackAssemblyName);
                }
                context->ExternalTypeMap.Add(externalTypeName, parsedTarget);
            }
            catch (Exception ex)
            {
                context->CreationException = ExceptionDispatchInfo.Capture(ex);
            }
        }

        [UnmanagedCallersOnly]
        private static unsafe void NewProxyTypeEntry(CallbackContext* context, ProcessAttributesCallbackArg* arg)
        {
            Debug.Assert(context != null);
            Debug.Assert(arg != null);
            Debug.Assert(arg->Utf8String1 != null);
            Debug.Assert(arg->Utf8String2 != null);

            try
            {
                string sourceType = new((sbyte*)arg->Utf8String1, 0, arg->StringLen1, Encoding.UTF8);
                string proxyType = new((sbyte*)arg->Utf8String2, 0, arg->StringLen2, Encoding.UTF8);

                TypeName parsedSource = TypeNameParser.Parse(sourceType, throwOnError: true)!;
                TypeName parsedProxy = TypeNameParser.Parse(proxyType, throwOnError: true)!;
                if (parsedSource.AssemblyName is null || parsedProxy.AssemblyName is null)
                {
                    // The assembly name is not included in the type name, so use the fallback assembly name.
                    Debug.Assert(arg->Utf8String3 != null);
                    string fallbackAssemblyName = new((sbyte*)arg->Utf8String3, 0, arg->StringLen3, Encoding.UTF8);
                    if (parsedSource.AssemblyName is null)
                    {
                        parsedSource = CreateNewTypeName(sourceType, fallbackAssemblyName);
                    }
                    if (parsedProxy.AssemblyName is null)
                    {
                        parsedProxy = CreateNewTypeName(proxyType, fallbackAssemblyName);
                    }
                }
                context->ProxyTypeMap.Add(parsedSource, parsedProxy);
            }
            catch (Exception ex)
            {
                context->CreationException = ExceptionDispatchInfo.Capture(ex);
            }
        }

        private static unsafe CallbackContext CreateMaps(
            RuntimeType groupType,
            delegate* unmanaged<CallbackContext*, ProcessAttributesCallbackArg*, void> newExternalTypeEntry,
            delegate* unmanaged<CallbackContext*, ProcessAttributesCallbackArg*, void> newProxyTypeEntry)
        {
            RuntimeAssembly? startingAssembly = (RuntimeAssembly?)Assembly.GetEntryAssembly();
            if (startingAssembly is null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_TypeMapMissingEntryAssembly);
            }

            CallbackContext context;
            ProcessAttributes(
                new QCallAssembly(ref startingAssembly),
                new QCallTypeHandle(ref groupType),
                newExternalTypeEntry,
                newProxyTypeEntry,
                &context);

            context.CreationException?.Throw();

            return context;
        }

        public static IReadOnlyDictionary<string, Type> CreateExternalTypeMap(RuntimeType groupType)
        {
            unsafe
            {
                return CreateMaps(
                    groupType,
                    &NewExternalTypeEntry,
                    null).ExternalTypeMap;
            }
        }

        public static IReadOnlyDictionary<Type, Type> CreateProxyTypeMap(RuntimeType groupType)
        {
            unsafe
            {
                return CreateMaps(
                    groupType,
                    null,
                    &NewProxyTypeEntry).ProxyTypeMap;
            }
        }

        private abstract class LazyTypeLoadDictionary<TKey> : IReadOnlyDictionary<TKey, Type> where TKey : notnull
        {
            protected abstract bool TryGetOrLoadType(TKey key, [NotNullWhen(true)] out Type? type);

            public Type this[TKey key]
            {
                get
                {
                    if (!TryGetOrLoadType(key, out Type? type))
                    {
                        ThrowHelper.ThrowKeyNotFoundException(key);
                    }

                    return type;
                }
            }

            public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out Type value) => TryGetOrLoadType(key, out value);

            // Not supported to avoid exposing TypeMap entries in a manner that
            // would violate invariants the Trimmer is attempting to enforce.
            public IEnumerable<TKey> Keys => throw new NotSupportedException();
            public IEnumerable<Type> Values => throw new NotSupportedException();
            public int Count => throw new NotSupportedException();
            public bool ContainsKey(TKey key) => throw new NotSupportedException();
            public IEnumerator<KeyValuePair<TKey, Type>> GetEnumerator() => throw new NotSupportedException();
            IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
        }

        [RequiresUnreferencedCode("Lazy TypeMap isn't supported for Trimmer scenarios")]
        private sealed class DelayedType
        {
            private Type? _type;
            public DelayedType(string assemblyName, string typeName)
            {
                AssemblyName = assemblyName;
                TypeName = typeName;
                _type = null;
            }

            public string AssemblyName { get; }
            public string TypeName { get; }

            public Type GetOrLoadType()
            {
                if (_type is null)
                {
                    lock (this)
                    {
                        Assembly targetAssembly = Assembly.Load(AssemblyName);
                        _type = targetAssembly.GetType(TypeName, throwOnError: true)!;
                    }
                }
                return _type;
            }
        }

        [RequiresUnreferencedCode("Lazy TypeMap isn't supported for Trimmer scenarios")]
        private sealed class LazyExternalTypeDictionary : LazyTypeLoadDictionary<string>
        {
            private static int ComputeHashCode(string key) => key.GetHashCode();

            private readonly Dictionary<int, DelayedType> _lazyData = new();

            protected override bool TryGetOrLoadType(string key, [NotNullWhen(true)] out Type? type)
            {
                int hash = ComputeHashCode(key);
                if (!_lazyData.TryGetValue(hash, out DelayedType? value))
                {
                    type = null;
                    return false;
                }

                type = value.GetOrLoadType();
                return true;
            }

            public void Add(string key, TypeName parsedTarget)
            {
                int hash = ComputeHashCode(key);
                if (_lazyData.ContainsKey(hash))
                {
                    ThrowHelper.ThrowAddingDuplicateWithKeyArgumentException(key);
                }

                _lazyData.Add(hash, new DelayedType(parsedTarget.AssemblyName!.FullName, parsedTarget.FullName));
            }
        }

        [RequiresUnreferencedCode("Lazy TypeMap isn't supported for Trimmer scenarios")]
        private sealed class LazyProxyTypeDictionary : LazyTypeLoadDictionary<Type>
        {
            // We don't include the assembly name for the hash code since it is not
            // guaranteed to be the same for the same type due to type forwarding.
            private static int ComputeHashCode(Type key) => key.FullName!.GetHashCode();
            private static int ComputeHashCode(TypeName key) => key.FullName.GetHashCode();

            private struct SourceProxyPair
            {
                public required DelayedType Source { get; init; }
                public required DelayedType Proxy { get; init; }
            }

            private sealed class DelayedTypeCollection
            {
                public required SourceProxyPair First { get; init; }
                public List<SourceProxyPair>? Others { get; private set; }

                public void Add(SourceProxyPair newEntryMaybe)
                {
                    Others ??= new List<SourceProxyPair>();
                    Others.Add(newEntryMaybe);
                }
            }

            private readonly Dictionary<int, DelayedTypeCollection> _lazyData = new();

            protected override bool TryGetOrLoadType(Type key, [NotNullWhen(true)] out Type? type)
            {
                int hash = ComputeHashCode(key);

                if (_lazyData.TryGetValue(hash, out DelayedTypeCollection? value))
                {
                    // The common case, no duplicate mappings.
                    if (value.First.Source.GetOrLoadType() == key)
                    {
                        type = value.First.Proxy.GetOrLoadType();
                        return true;
                    }
                    else if (value.Others != null)
                    {
                        // Common case failed, look at alternate mappings.
                        foreach (SourceProxyPair entry in value.Others)
                        {
                            if (entry.Source.GetOrLoadType() == key)
                            {
                                type = entry.Proxy.GetOrLoadType();
                                return true;
                            }
                        }
                    }
                }
                type = null;
                return false;
            }

            public void Add(TypeName parsedSource, TypeName parsedProxy)
            {
                int hash = ComputeHashCode(parsedSource);

                SourceProxyPair newEntryMaybe = new()
                {
                    Source = new(parsedSource.AssemblyName!.FullName, parsedSource.FullName),
                    Proxy = new(parsedProxy.AssemblyName!.FullName, parsedProxy.FullName)
                };

                if (!_lazyData.TryGetValue(hash, out DelayedTypeCollection? types))
                {
                    types = new DelayedTypeCollection() { First = newEntryMaybe };
                    _lazyData.Add(hash, types);
                }
                else
                {
                    types.Add(newEntryMaybe);
                }
            }
        }
    }
}
