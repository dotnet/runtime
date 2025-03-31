// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
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
        // See assemblynative.cpp for native version.
        private ref struct CallbackContext
        {
            private RuntimeAssembly? _currAssembly;
            private LazyExternalTypeDictionary? _externalTypeMap;
            private LazyProxyTypeDictionary? _proxyTypeMap;
            private ExceptionDispatchInfo? _creationException;

            public RuntimeAssembly CurrentAssembly
            {
                get
                {
                    // This field is set by native code.
                    Debug.Assert(_currAssembly != null);
                    return _currAssembly;
                }
            }

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

            public ExceptionDispatchInfo? CreationException
            {
                get => _creationException;
                set => _creationException = value;
            }
        }

        // See assemblynative.hpp for native version.
        public unsafe struct ProcessAttributesCallbackArg
        {
            public void* Utf8String1;
            public void* Utf8String2;
            public int StringLen1;
            public int StringLen2;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeMapLazyDictionary_ProcessAttributes")]
        private static unsafe partial void ProcessAttributes(
            QCallAssembly assembly,
            QCallTypeHandle groupType,
            delegate* unmanaged<CallbackContext*, ProcessAttributesCallbackArg*, Interop.BOOL> newExternalTypeEntry,
            delegate* unmanaged<CallbackContext*, ProcessAttributesCallbackArg*, Interop.BOOL> newProxyTypeEntry,
            CallbackContext* context);

        public ref struct Utf16SharedBuffer
        {
            private char[]? _backingArray;
            public Utf16SharedBuffer()
            {
                _backingArray = null;
                Buffer = default;
            }

            public Utf16SharedBuffer(char[] backingBuffer, int validLength)
            {
                _backingArray = backingBuffer;
                Buffer = new ReadOnlySpan<char>(backingBuffer, 0, validLength);
            }

            public ReadOnlySpan<char> Buffer { get; init; }

            public void Dispose()
            {
                if (_backingArray != null)
                {
                    ArrayPool<char>.Shared.Return(_backingArray);
                }
            }
        }

        private static void ConvertUtf8ToUtf16(ReadOnlySpan<byte> utf8TypeName, out Utf16SharedBuffer utf16Buffer)
        {
            // Use quick conservative estimate for small strings
            int needed = (utf8TypeName.Length < 1024)
                ? Encoding.UTF8.GetMaxCharCount(utf8TypeName.Length)
                : Encoding.UTF8.GetCharCount(utf8TypeName);

            char[] buffer = ArrayPool<char>.Shared.Rent(needed);
            int converted = Encoding.UTF8.GetChars(utf8TypeName, buffer);
            utf16Buffer = new Utf16SharedBuffer(buffer, converted);
        }

        [UnmanagedCallersOnly]
        private static unsafe Interop.BOOL NewExternalTypeEntry(CallbackContext* context, ProcessAttributesCallbackArg* arg)
        {
            Debug.Assert(context != null);
            Debug.Assert(arg != null);
            Debug.Assert(arg->Utf8String1 != null);
            Debug.Assert(arg->Utf8String2 != null);

            try
            {
                string externalTypeName = new((sbyte*)arg->Utf8String1, 0, arg->StringLen1, Encoding.UTF8);
                TypeNameValue targetTypeName = new()
                {
                    Utf8TypeName = arg->Utf8String2,
                    Utf8TypeNameLen = arg->StringLen2,
                    FallbackAssembly = context->CurrentAssembly
                };
                context->ExternalTypeMap.Add(externalTypeName, targetTypeName);
            }
            catch (Exception ex)
            {
                context->CreationException = ExceptionDispatchInfo.Capture(ex);
                return Interop.BOOL.FALSE; // Stop processing.
            }

            return Interop.BOOL.TRUE; // Continue processing.
        }

        [UnmanagedCallersOnly]
        private static unsafe Interop.BOOL NewProxyTypeEntry(CallbackContext* context, ProcessAttributesCallbackArg* arg)
        {
            Debug.Assert(context != null);
            Debug.Assert(arg != null);
            Debug.Assert(arg->Utf8String1 != null);
            Debug.Assert(arg->Utf8String2 != null);

            Utf16SharedBuffer sourceTypeBuffer = new();
            try
            {
                ConvertUtf8ToUtf16(new ReadOnlySpan<byte>(arg->Utf8String1, arg->StringLen1), out sourceTypeBuffer);
                TypeName parsedSource = TypeNameParser.Parse(sourceTypeBuffer.Buffer, throwOnError: true)!;

                // The assembly name is not included in the type name, so use the fallback assembly name.
                string assemblyName = parsedSource.AssemblyName?.FullName ?? context->CurrentAssembly.FullName!;
                Debug.Assert(assemblyName != null);

                TypeNameValue proxyTypeName = new()
                {
                    Utf8TypeName = arg->Utf8String2,
                    Utf8TypeNameLen = arg->StringLen2,
                    FallbackAssembly = context->CurrentAssembly
                };
                context->ProxyTypeMap.Add(parsedSource, assemblyName, proxyTypeName);
            }
            catch (Exception ex)
            {
                context->CreationException = ExceptionDispatchInfo.Capture(ex);
                return Interop.BOOL.FALSE; // Stop processing.
            }
            finally
            {
                sourceTypeBuffer.Dispose();
            }

            return Interop.BOOL.TRUE; // Continue processing.
        }

        private static unsafe CallbackContext CreateMaps(
            RuntimeType groupType,
            delegate* unmanaged<CallbackContext*, ProcessAttributesCallbackArg*, Interop.BOOL> newExternalTypeEntry,
            delegate* unmanaged<CallbackContext*, ProcessAttributesCallbackArg*, Interop.BOOL> newProxyTypeEntry)
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

            // If an exception was thrown during the processing of
            // the attributes, rethrow it.
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

        private unsafe struct TypeNameValue
        {
            public required void* Utf8TypeName { get; init; }
            public required int Utf8TypeNameLen { get; init; }
            public required RuntimeAssembly FallbackAssembly { get; init; }
        }

        [RequiresUnreferencedCode("Lazy TypeMap isn't supported for Trimmer scenarios")]
        private sealed class DelayedType
        {
            private TypeNameValue? _typeNameRaw;
            private string _assemblyName;
            private string _typeName;

            private Type? _type;

            public DelayedType(TypeNameValue typeNameValue)
            {
                _typeNameRaw = typeNameValue;
                _assemblyName = string.Empty;
                _typeName = string.Empty;
                _type = null;
            }

            public DelayedType(string assemblyName, string typeName)
            {
                _typeNameRaw = null;
                _assemblyName = assemblyName;
                _typeName = typeName;
                _type = null;
            }

            public unsafe Type GetOrLoadType()
            {
                if (_type is null)
                {
                    if (!_typeNameRaw.HasValue)
                    {
                        Assembly targetAssembly = Assembly.Load(_assemblyName);
                        _type = targetAssembly.GetType(_typeName, throwOnError: true)!;
                    }
                    else
                    {
                        Utf16SharedBuffer typeNameBuffer = new();
                        try
                        {
                            ConvertUtf8ToUtf16(
                                new ReadOnlySpan<byte>(_typeNameRaw.Value.Utf8TypeName, _typeNameRaw.Value.Utf8TypeNameLen),
                                out typeNameBuffer);
                            _type = TypeNameResolver.GetTypeHelper(
                                typeNameBuffer.Buffer,
                                _typeNameRaw.Value.FallbackAssembly,
                                throwOnError: true,
                                requireAssemblyQualifiedName: false)!;
                        }
                        finally
                        {
                            typeNameBuffer.Dispose();
                        }
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

            public void Add(string key, TypeNameValue targetType)
            {
                int hash = ComputeHashCode(key);
                if (_lazyData.ContainsKey(hash))
                {
                    ThrowHelper.ThrowAddingDuplicateWithKeyArgumentException(key);
                }

                _lazyData.Add(hash, new DelayedType(targetType));
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

            public void Add(TypeName typeName, string assemblyName, TypeNameValue proxyTypeName)
            {
                int hash = ComputeHashCode(typeName);

                SourceProxyPair newEntryMaybe = new()
                {
                    Source = new DelayedType(assemblyName, typeName.FullName),
                    Proxy = new DelayedType(proxyTypeName)
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
