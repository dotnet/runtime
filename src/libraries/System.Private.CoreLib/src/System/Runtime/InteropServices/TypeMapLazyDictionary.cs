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
                TypeNameUtf8 targetTypeName = new()
                {
                    Utf8TypeName = arg->Utf8String2,
                    Utf8TypeNameLen = arg->StringLen2
                };
                context->ExternalTypeMap.Add(externalTypeName, targetTypeName, context->CurrentAssembly);
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

                TypeNameUtf8 sourceTypeName = new()
                {
                    Utf8TypeName = arg->Utf8String1,
                    Utf8TypeNameLen = arg->StringLen1
                };
                TypeNameUtf8 proxyTypeName = new()
                {
                    Utf8TypeName = arg->Utf8String2,
                    Utf8TypeNameLen = arg->StringLen2
                };
                context->ProxyTypeMap.Add(parsedSource, sourceTypeName, proxyTypeName, context->CurrentAssembly);
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

        private unsafe struct TypeNameUtf8
        {
            public required void* Utf8TypeName { get; init; }
            public required int Utf8TypeNameLen { get; init; }

            public bool Equals(TypeNameUtf8 other)
            {
                if (Utf8TypeNameLen != other.Utf8TypeNameLen)
                    return false;
                byte* thisChars = (byte*)Utf8TypeName;
                byte* otherChars = (byte*)Utf8TypeName;
                for (int i = 0; i < Utf8TypeNameLen; i++)
                {
                    if (thisChars[i] != otherChars[i])
                        return false;
                }
                return true;
            }
        }

        [RequiresUnreferencedCode("Lazy TypeMap isn't supported for Trimmer scenarios")]
        private sealed class DelayedType
        {
            private TypeNameUtf8 _typeNameUtf8;
            private RuntimeAssembly _fallbackAssembly;

            private Type? _type;

            public DelayedType(TypeNameUtf8 typeNameUtf8, RuntimeAssembly fallbackAssembly)
            {
                _typeNameUtf8 = typeNameUtf8;
                _fallbackAssembly = fallbackAssembly;
                _type = null;
            }

            public TypeNameUtf8 TypeName => _typeNameUtf8;

            public unsafe Type GetOrLoadType()
            {
                if (_type is null)
                {
                    Utf16SharedBuffer typeNameBuffer = new();
                    try
                    {
                        ConvertUtf8ToUtf16(new ReadOnlySpan<byte>(_typeNameUtf8.Utf8TypeName, _typeNameUtf8.Utf8TypeNameLen), out typeNameBuffer);
                        _type = TypeNameResolver.GetTypeHelper(
                            typeNameBuffer.Buffer,
                            _fallbackAssembly,
                            throwOnError: true,
                            requireAssemblyQualifiedName: false)!;
                    }
                    finally
                    {
                        typeNameBuffer.Dispose();
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

            public void Add(string key, TypeNameUtf8 targetType, RuntimeAssembly fallbackAssembly)
            {
                int hash = ComputeHashCode(key);
                // Allow duplicates that have the same string -> mapping. They may have different trimTargets.
                // Warn if the mapping conflicts with an existing mapping.
                if (_lazyData.TryGetValue(hash, out DelayedType? existing) && existing.TypeName.Equals(targetType))
                {
                    ThrowHelper.ThrowAddingDuplicateWithKeyArgumentException(key);
                }

                _lazyData.Add(hash, new DelayedType(targetType, fallbackAssembly));
            }
        }

        [RequiresUnreferencedCode("Lazy TypeMap isn't supported for Trimmer scenarios")]
        private sealed class LazyProxyTypeDictionary : LazyTypeLoadDictionary<Type>
        {
            private static int ComputeHashCode(RuntimeType key)
                => Internal.VersionResilientHashCode.TypeHashCode(key);

            private static int ComputeHashCode(TypeName key)
                => Internal.VersionResilientHashCode.TypeHashCode(key);

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
                if (key is not RuntimeType rtType)
                {
                    throw new ArgumentException(SR.Argument_MustBeRuntimeType, nameof(key));
                }

                int hash = ComputeHashCode(rtType);

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

            public void Add(
                TypeName parsedSourceTypeName,
                TypeNameUtf8 sourceTypeName,
                TypeNameUtf8 proxyTypeName,
                RuntimeAssembly fallbackAssembly)
            {
                int hash = ComputeHashCode(parsedSourceTypeName);

                SourceProxyPair newEntryMaybe = new()
                {
                    Source = new DelayedType(sourceTypeName, fallbackAssembly),
                    Proxy = new DelayedType(proxyTypeName, fallbackAssembly)
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
