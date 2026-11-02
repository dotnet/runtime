// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Reflection
{
    internal readonly struct QCustomAttributeList(RuntimeModule module, int metadataToken)
    {
        public RuntimeModule Module { get; } = module;
        public int MetadataToken { get; } = metadataToken;

        public QCustomAttributeList(RuntimeType target)
            : this(target.GetRuntimeModule(), target.MetadataToken) { }

        public QCustomAttributeList(RuntimeFieldInfo target)
            : this(target.GetRuntimeModule(), target.MetadataToken) { }

        public QCustomAttributeList(RuntimeMethodInfo target)
            : this(target.GetRuntimeModule(), target.MetadataToken) { }

        public QCustomAttributeList(RuntimeConstructorInfo target)
            : this(target.GetRuntimeModule(), target.MetadataToken) { }

        public QCustomAttributeList(RuntimeEventInfo target)
            : this(target.GetRuntimeModule(), target.MetadataToken) { }

        public QCustomAttributeList(RuntimePropertyInfo target)
            : this(target.GetRuntimeModule(), target.MetadataToken) { }

        public QCustomAttributeList(RuntimeModule target)
            : this(target, target.MetadataToken) { }

        public QCustomAttributeList(RuntimeAssembly target)
            : this((RuntimeModule)target.ManifestModule, RuntimeAssembly.GetToken(target)) { }

        public QCustomAttributeList(RuntimeParameterInfo target)
            : this(target.GetRuntimeModule()!, target.MetadataToken) { }
    }

    internal sealed partial class RuntimeCustomAttributeData : CustomAttributeData
    {
        #region Private Static Methods
        private static IList<CustomAttributeData> GetCustomAttributes(QCustomAttributeList attributeList)
        {
            RuntimeModule module = attributeList.Module;
            CustomAttributeRecord[] records = GetCustomAttributeRecords(module, attributeList.MetadataToken);
            if (records.Length == 0)
            {
                return Array.Empty<CustomAttributeData>();
            }

            CustomAttributeData[] customAttributes = new CustomAttributeData[records.Length];
            for (int i = 0; i < records.Length; i++)
                customAttributes[i] = new RuntimeCustomAttributeData(module, records[i].tkCtor, in records[i].blob);

            return Array.AsReadOnly(customAttributes);
        }
        #endregion

        #region Internal Static Members
        internal static CustomAttributeRecord[] GetCustomAttributeRecords(RuntimeModule module, int targetToken)
        {
            MetadataImport scope = module.MetadataImport;

            scope.EnumCustomAttributes(targetToken, out MetadataEnumResult tkCustomAttributeTokens);

            if (tkCustomAttributeTokens.Length == 0)
            {
                return [];
            }

            CustomAttributeRecord[] records = new CustomAttributeRecord[tkCustomAttributeTokens.Length];

            for (int i = 0; i < records.Length; i++)
            {
                scope.GetCustomAttributeProps(tkCustomAttributeTokens[i],
                    out records[i].tkCtor.Value, out records[i].blob);
            }
            GC.KeepAlive(module);

            return records;
        }

        internal static CustomAttributeTypedArgument Filter(IList<CustomAttributeData> attrs, Type? caType, int parameter)
        {
            for (int i = 0; i < attrs.Count; i++)
            {
                if (attrs[i].Constructor.DeclaringType == caType)
                {
                    return attrs[i].ConstructorArguments[parameter];
                }
            }

            return default;
        }
        #endregion
    }

    public readonly partial struct CustomAttributeTypedArgument
    {
        private static RuntimeType ResolveType(RuntimeModule scope, string typeName)
        {
            RuntimeType type = TypeNameResolver.GetTypeReferencedByCustomAttribute(typeName, scope);
            Debug.Assert(type is not null);
            return type;
        }
    }

    internal struct CustomAttributeRecord
    {
        internal ConstArray blob;
        internal MetadataToken tkCtor;

        public CustomAttributeRecord(int token, ConstArray blob)
        {
            tkCtor = new MetadataToken(token);
            this.blob = blob;
        }
    }

    internal static unsafe partial class RuntimeCustomAttribute
    {
        internal static bool IsAttributeDefined(RuntimeModule decoratedModule, int decoratedMetadataToken, int attributeCtorToken)
        {
            return IsCustomAttributeDefined(new QCustomAttributeList(decoratedModule, decoratedMetadataToken), null, attributeCtorToken: attributeCtorToken);
        }

        internal static bool IsCustomAttributeDefined(
            RuntimeModule decoratedModule, int decoratedMetadataToken, RuntimeType? attributeFilterType)
        {
            return IsCustomAttributeDefined(new QCustomAttributeList(decoratedModule, decoratedMetadataToken), attributeFilterType);
        }

        private static bool IsCustomAttributeDefined(
            QCustomAttributeList customAttributes, RuntimeType? attributeFilterType, bool mustBeInheritable = false, int attributeCtorToken = 0)
        {
            RuntimeModule decoratedModule = customAttributes.Module;
            int decoratedMetadataToken = customAttributes.MetadataToken;
            MetadataImport scope = decoratedModule.MetadataImport;

            scope.EnumCustomAttributes(decoratedMetadataToken, out MetadataEnumResult attributeTokens);

            if (attributeTokens.Length == 0)
            {
                return false;
            }

            CustomAttributeRecord record = default;
            if (attributeFilterType is not null)
            {
                Debug.Assert(attributeCtorToken == 0);

                ListBuilder<object> derivedAttributes = default;

                for (int i = 0; i < attributeTokens.Length; i++)
                {
                    scope.GetCustomAttributeProps(attributeTokens[i],
                        out record.tkCtor.Value, out record.blob);

                    if (FilterCustomAttributeRecord(record.tkCtor, in scope,
                        decoratedModule, decoratedMetadataToken, attributeFilterType, mustBeInheritable, ref derivedAttributes,
                        out _, out _, out _))
                    {
                        return true;
                    }
                }
            }
            else
            {
                Debug.Assert(attributeFilterType is null);
                Debug.Assert(!MetadataToken.IsNullToken(attributeCtorToken));

                for (int i = 0; i < attributeTokens.Length; i++)
                {
                    scope.GetCustomAttributeProps(attributeTokens[i],
                        out record.tkCtor.Value, out record.blob);

                    if (record.tkCtor == attributeCtorToken)
                    {
                        return true;
                    }
                }
            }
            GC.KeepAlive(decoratedModule);

            return false;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:MethodParameterDoesntMeetThisParameterRequirements",
            Justification = "Linker guarantees presence of all the constructor parameters, property setters and fields which are accessed by any " +
                            "attribute instantiation which is present in the code linker has analyzed." +
                            "As such the reflection usage in this method will never fail as those methods/fields will be present.")]
        private static void AddCustomAttributes(
            ref ListBuilder<object> attributes,
            QCustomAttributeList customAttributes,
            RuntimeType? attributeFilterType, bool mustBeInheritable,
            // The derivedAttributes list must be passed by value so that it is not modified with the discovered attributes
            ListBuilder<object> derivedAttributes)
        {
            RuntimeModule decoratedModule = customAttributes.Module;
            int decoratedMetadataToken = customAttributes.MetadataToken;
            CustomAttributeRecord[] car = RuntimeCustomAttributeData.GetCustomAttributeRecords(decoratedModule, decoratedMetadataToken);

            if (attributeFilterType is null && car.Length == 0)
            {
                return;
            }

            MetadataImport scope = decoratedModule.MetadataImport;
            for (int i = 0; i < car.Length; i++)
            {
                ref CustomAttributeRecord caRecord = ref car[i];

                IntPtr blobStart = caRecord.blob.Signature;
                IntPtr blobEnd = (IntPtr)((byte*)blobStart + caRecord.blob.Length);

                if (!FilterCustomAttributeRecord(caRecord.tkCtor, in scope,
                                                 decoratedModule, decoratedMetadataToken, attributeFilterType!, mustBeInheritable,
                                                 ref derivedAttributes,
                                                 out RuntimeType attributeType, out IRuntimeMethodInfo? ctorWithParameters, out bool isVarArg))
                {
                    continue;
                }

                // Leverage RuntimeConstructorInfo standard .ctor verification
                RuntimeConstructorInfo.CheckCanCreateInstance(attributeType, isVarArg);

                // Create custom attribute object
                int cNamedArgs;
                object attribute;
                if (ctorWithParameters is not null)
                {
                    attribute = CreateCustomAttributeInstance(decoratedModule, attributeType, ctorWithParameters, ref blobStart, blobEnd, out cNamedArgs);
                }
                else
                {
                    attribute = attributeType.CreateInstanceDefaultCtor(publicOnly: false, wrapExceptions: false)!;

                    // It is allowed by the ECMA spec to have an empty signature blob
                    int blobLen = (int)((byte*)blobEnd - (byte*)blobStart);
                    if (blobLen == 0)
                    {
                        cNamedArgs = 0;
                    }
                    else
                    {
                        var parser = new CustomAttributeEncodedArgument.CustomAttributeDataParser(GetAttributeBlob(blobStart, blobEnd));
                        if (!parser.ValidateProlog())
                        {
                            throw new CustomAttributeFormatException();
                        }
                        cNamedArgs = parser.GetI2();
                        blobStart += parser.Consumed;
                    }
                }

                for (int j = 0; j < cNamedArgs; j++)
                {
                    GetPropertyOrFieldData(decoratedModule, ref blobStart, blobEnd, out string name, out bool isProperty, out RuntimeType? type, out object? value);

                    try
                    {
                        if (isProperty)
                        {
                            if (type is null && value is not null)
                            {
                                type = (RuntimeType)value.GetType();
                                if (type == typeof(RuntimeType))
                                {
                                    type = (RuntimeType)typeof(Type);
                                }
                            }

                            RuntimePropertyInfo? property = (RuntimePropertyInfo?)(type is null ?
                                attributeType.GetProperty(name) :
                                attributeType.GetProperty(name, type, [])) ??
                                throw new CustomAttributeFormatException(SR.Format(SR.RFLCT_InvalidPropFail, name));
                            RuntimeMethodInfo setMethod = property.GetSetMethod(true)!;

                            // Public properties may have non-public setter methods
                            if (!setMethod.IsPublic)
                            {
                                continue;
                            }

                            setMethod.InvokePropertySetter(attribute, BindingFlags.Default, null, value, null);
                        }
                        else
                        {
                            FieldInfo field = attributeType.GetField(name)!;
                            field.SetValue(attribute, value, BindingFlags.Default, Type.DefaultBinder, null);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new CustomAttributeFormatException(
                            SR.Format(isProperty ? SR.RFLCT_InvalidPropFail : SR.RFLCT_InvalidFieldFail, name), e);
                    }
                }

                if (blobStart != blobEnd)
                {
                    throw new CustomAttributeFormatException();
                }

                attributes.Add(attribute);
            }
            GC.KeepAlive(decoratedModule);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Module.ResolveMethod and Module.ResolveType are marked as RequiresUnreferencedCode because they rely on tokens" +
                            "which are not guaranteed to be stable across trimming. So if somebody hardcodes a token it could break." +
                            "The usage here is not like that as all these tokens come from existing metadata loaded from some IL" +
                            "and so trimming has no effect (the tokens are read AFTER trimming occurred).")]
        private static bool FilterCustomAttributeRecord(
            MetadataToken caCtorToken,
            in MetadataImport scope,
            RuntimeModule decoratedModule,
            MetadataToken decoratedToken,
            RuntimeType attributeFilterType,
            bool mustBeInheritable,
            ref ListBuilder<object> derivedAttributes,
            out RuntimeType attributeType,
            out IRuntimeMethodInfo? ctorWithParameters,
            out bool isVarArg)
        {
            ctorWithParameters = null;
            isVarArg = false;

            // Resolve attribute type from ctor parent token found in decorated decoratedModule scope
            attributeType = (decoratedModule.ResolveType(scope.GetParentToken(caCtorToken), null, null) as RuntimeType)!;

            // Test attribute type against user provided attribute type filter
            if (!MatchesTypeFilter(attributeType, attributeFilterType))
                return false;

            // Ensure if attribute type must be inheritable that it is inheritable
            // Ensure that to consider a duplicate attribute type AllowMultiple is true
            if (!AttributeUsageCheck(attributeType, mustBeInheritable, ref derivedAttributes))
                return false;

            // Resolve the attribute ctor
            ConstArray ctorSig = scope.GetMethodSignature(caCtorToken);
            isVarArg = (ctorSig[0] & 0x05) != 0;
            bool ctorHasParameters = ctorSig[1] != 0;

            if (ctorHasParameters)
            {
                // Resolve method ctor token found in decorated decoratedModule scope
                // See https://github.com/dotnet/runtime/issues/11637 for why we fast-path non-generics here (fewer allocations)
                if (attributeType.IsGenericType)
                {
                    ctorWithParameters = decoratedModule.ResolveMethod(caCtorToken, attributeType.GenericTypeArguments, null)!.MethodHandle.GetMethodInfo();
                }
                else
                {
                    ctorWithParameters = new ModuleHandle(decoratedModule).ResolveMethodHandle(caCtorToken).GetMethodInfo();
                }
            }

            // Visibility checks
            MetadataToken tkParent = default;

            if (decoratedToken.IsParamDef)
            {
                tkParent = new MetadataToken(scope.GetParentToken(decoratedToken));
                tkParent = new MetadataToken(scope.GetParentToken(tkParent));
            }
            else if (decoratedToken.IsMethodDef || decoratedToken.IsProperty || decoratedToken.IsEvent || decoratedToken.IsFieldDef)
            {
                tkParent = new MetadataToken(scope.GetParentToken(decoratedToken));
            }
            else if (decoratedToken.IsTypeDef)
            {
                tkParent = decoratedToken;
            }
            else if (decoratedToken.IsGenericPar)
            {
                tkParent = new MetadataToken(scope.GetParentToken(decoratedToken));

                // decoratedToken is a generic parameter on a method. Get the declaring Type of the method.
                if (tkParent.IsMethodDef)
                    tkParent = new MetadataToken(scope.GetParentToken(tkParent));
            }
            else
            {
                // We need to relax this when we add support for other types of decorated tokens.
                Debug.Assert(decoratedToken.IsModule || decoratedToken.IsAssembly,
                                "The decoratedToken must be either an assembly, a module, a type, or a member.");
            }

            // If the attribute is on a type, member, or parameter we check access against the (declaring) type,
            // otherwise we check access against the module.
            RuntimeTypeHandle parentTypeHandle = tkParent.IsTypeDef ?
                                                    decoratedModule.ModuleHandle.ResolveTypeHandle(tkParent) :
                                                    default;

            RuntimeTypeHandle attributeTypeHandle = attributeType.TypeHandle;

            bool result = RuntimeMethodHandle.IsCAVisibleFromDecoratedType(new QCallTypeHandle(ref attributeTypeHandle),
                                                                    ctorWithParameters is not null ? IRuntimeMethodInfo.GetValue(ctorWithParameters) : RuntimeMethodHandleInternal.EmptyHandle,
                                                                    new QCallTypeHandle(ref parentTypeHandle),
                                                                    new QCallModule(ref decoratedModule)) != Interop.BOOL.FALSE;

            GC.KeepAlive(ctorWithParameters);
            return result;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "CustomAttribute_ParseAttributeUsageAttribute")]
        [SuppressGCTransition]
        private static partial int ParseAttributeUsageAttribute(
            IntPtr pData,
            int cData,
            int* pTargets,
            int* pAllowMultiple,
            int* pInherited);

        private static bool ParseAttributeUsageAttribute(
            ConstArray blob,
            out AttributeTargets attrTargets,
            out bool allowMultiple,
            out bool inherited)
        {
            int attrTargetsLocal = 0;
            int allowMultipleLocal = 0;
            int inheritedLocal = 0;
            int result = ParseAttributeUsageAttribute(blob.Signature, blob.Length, &attrTargetsLocal, &allowMultipleLocal, &inheritedLocal);
            attrTargets = (AttributeTargets)attrTargetsLocal;
            allowMultiple = allowMultipleLocal != 0;
            inherited = inheritedLocal != 0;
            return result != 0;
        }

        private static object CreateCustomAttributeInstance(RuntimeModule module, RuntimeType type, IRuntimeMethodInfo ctor, ref IntPtr blob, IntPtr blobEnd, out int namedArgs)
        {
            // A shared generic constructor handle does not preserve its declaring type's instantiation.
            if (RuntimeType.GetMethodBase(type, ctor) is not RuntimeConstructorInfo constructor)
            {
                throw new CustomAttributeFormatException();
            }

            RuntimeType[] argumentTypes = constructor.ArgumentTypes;
            int argumentCount = argumentTypes.Length;
            var parser = new CustomAttributeEncodedArgument.CustomAttributeDataParser(GetAttributeBlob(blob, blobEnd));
            if (!parser.ValidateProlog())
            {
                throw new CustomAttributeFormatException();
            }

            // Keep fixed primitives unboxed. Only the bounded common case uses stack storage.
            const int MaxStackAttributeArguments = 4;
            InlineArray4<object?> referenceStorage = default;
            InlineArray4<ulong> primitiveStorage = default;
            InlineArray4<IntPtr> byrefStorage = default;
            Span<object?> references = argumentCount <= MaxStackAttributeArguments
                ? ((Span<object?>)referenceStorage).Slice(0, argumentCount)
                : new object?[argumentCount];
            Span<ulong> primitives = argumentCount <= MaxStackAttributeArguments
                ? primitiveStorage
                : new ulong[argumentCount];
            Span<IntPtr> byrefs = argumentCount <= MaxStackAttributeArguments
                ? byrefStorage
                : new IntPtr[argumentCount];

            fixed (IntPtr* argumentStorage = byrefs)
            {
                GCFrameRegistration registration = new((void**)argumentStorage, (uint)argumentCount, areByRefs: true);
                try
                {
                    GCFrameRegistration.RegisterForGCReporting(&registration);
                    for (int i = 0; i < argumentCount; i++)
                    {
                        RuntimeType argumentType = argumentTypes[i];
                        CustomAttributeEncoding encoding = GetPrimitiveEncoding(argumentType);
                        int size = GetPrimitiveSize(encoding);
                        if (size != 0)
                        {
                            Span<byte> value = MemoryMarshal.AsBytes(primitives.Slice(i, 1)).Slice(0, size);
                            ReadPrimitiveData(ref parser, encoding).CopyTo(value);
                            if (!BitConverter.IsLittleEndian)
                            {
                                value.Reverse();
                            }
                            StoreArgumentReference(argumentStorage + i, ref MemoryMarshal.GetReference(value));
                        }
                        else
                        {
                            references[i] = ReadAttributeValue(ref parser, argumentType, module);
                            StoreArgumentReference(argumentStorage + i, ref references[i]);
                        }
                    }

                    namedArgs = parser.Remaining == 0 ? 0 : parser.GetU2();
                    if (namedArgs == 0 && parser.Remaining != 0)
                    {
                        throw new CustomAttributeFormatException();
                    }

                    blob += parser.Consumed;
                    return constructor.InvokeDirectByRef(argumentStorage);
                }
                finally
                {
                    GCFrameRegistration.UnregisterForGCReporting(&registration);
                    GC.KeepAlive(module);
                    GC.KeepAlive(type);
                }
            }
        }

        // The caller registers this native vector as byrefs for the lifetime of the invocation.
        private static void StoreArgumentReference<T>(IntPtr* storage, scoped ref T value) =>
            *storage = (IntPtr)Unsafe.AsPointer(ref value);

        private static void GetPropertyOrFieldData(
            RuntimeModule module, ref IntPtr blobStart, IntPtr blobEnd, out string name, out bool isProperty, out RuntimeType? type, out object? value)
        {
            try
            {
                var parser = new CustomAttributeEncodedArgument.CustomAttributeDataParser(GetAttributeBlob(blobStart, blobEnd));
                CustomAttributeEncoding memberKind = parser.GetTag();
                if (memberKind is not CustomAttributeEncoding.Field and not CustomAttributeEncoding.Property)
                {
                    throw new CustomAttributeFormatException();
                }
                isProperty = memberKind == CustomAttributeEncoding.Property;

                CustomAttributeEncoding encoding = parser.GetTag();
                RuntimeType declaredType = ReadAttributeType(ref parser, encoding, module);
                name = parser.GetString()!;
                value = ReadAttributeValue(ref parser, declaredType, module);

                type = encoding is CustomAttributeEncoding.Object or CustomAttributeEncoding.Enum ? declaredType : null;
                if (value is null && encoding is CustomAttributeEncoding.String or CustomAttributeEncoding.Type or CustomAttributeEncoding.Array)
                {
                    // A null enum array leaves type unspecified for the existing named-member lookup.
                    if (encoding != CustomAttributeEncoding.Array || !declaredType.GetElementType()!.IsEnum)
                    {
                        type = declaredType;
                    }
                }

                blobStart += parser.Consumed;
            }
            finally
            {
                GC.KeepAlive(module);
            }
        }

        private static ReadOnlySpan<byte> GetAttributeBlob(IntPtr start, IntPtr end)
        {
            nuint length = (nuint)end - (nuint)start;
            if (length > int.MaxValue)
            {
                throw new CustomAttributeFormatException();
            }

            return new ReadOnlySpan<byte>((void*)start, (int)length);
        }

        private static CustomAttributeEncoding GetPrimitiveEncoding(RuntimeType type) =>
            RuntimeCustomAttributeData.TypeToCustomAttributeEncoding(type.IsActualEnum
                ? (RuntimeType)type.GetEnumUnderlyingType()
                : type);

        private static int GetPrimitiveSize(CustomAttributeEncoding encoding) => encoding switch
        {
            CustomAttributeEncoding.Boolean or CustomAttributeEncoding.SByte or CustomAttributeEncoding.Byte => 1,
            CustomAttributeEncoding.Char or CustomAttributeEncoding.Int16 or CustomAttributeEncoding.UInt16 => 2,
            CustomAttributeEncoding.Int32 or CustomAttributeEncoding.UInt32 or CustomAttributeEncoding.Float => 4,
            CustomAttributeEncoding.Int64 or CustomAttributeEncoding.UInt64 or CustomAttributeEncoding.Double => 8,
            _ => 0
        };

        private static ReadOnlySpan<byte> ReadPrimitiveData(ref CustomAttributeEncodedArgument.CustomAttributeDataParser parser, CustomAttributeEncoding encoding)
        {
            int size = GetPrimitiveSize(encoding);
            if (size == 0)
            {
                throw new CustomAttributeFormatException();
            }

            return parser.ReadData(size);
        }

        private static object? ReadAttributeValue(ref CustomAttributeEncodedArgument.CustomAttributeDataParser parser, RuntimeType type, RuntimeModule module)
        {
            CustomAttributeEncoding encoding = GetPrimitiveEncoding(type);
            int size = GetPrimitiveSize(encoding);
            if (size != 0)
            {
                ReadOnlySpan<byte> data = ReadPrimitiveData(ref parser, encoding);
                if (BitConverter.IsLittleEndian || size == 1)
                {
                    return RuntimeHelpers.Box(ref MemoryMarshal.GetReference(data), type.TypeHandle);
                }

                InlineArray8<byte> reversedStorage = default;
                Span<byte> reversed = ((Span<byte>)reversedStorage).Slice(0, size);
                data.CopyTo(reversed);
                reversed.Reverse();
                return RuntimeHelpers.Box(ref MemoryMarshal.GetReference(reversed), type.TypeHandle);
            }

            if (type == typeof(string))
            {
                return parser.GetString();
            }

            if (type == typeof(Type))
            {
                return ReadAttributeTypeName(ref parser, module);
            }

            if (type == typeof(object))
            {
                RuntimeType valueType = ReadAttributeType(ref parser, parser.GetTag(), module);
                return ReadAttributeValue(ref parser, valueType, module);
            }

            if (type.IsSZArray)
            {
                return ReadAttributeArray(ref parser, (RuntimeType)type.GetElementType()!, module);
            }

            throw new CustomAttributeFormatException();
        }

        private static RuntimeType ReadAttributeType(
            ref CustomAttributeEncodedArgument.CustomAttributeDataParser parser, CustomAttributeEncoding encoding, RuntimeModule module, bool allowArray = true)
        {
            if (GetPrimitiveSize(encoding) != 0 || encoding is CustomAttributeEncoding.String or CustomAttributeEncoding.Type or CustomAttributeEncoding.Object)
            {
                return (RuntimeType)CustomAttributeTypedArgument.CustomAttributeEncodingToType(encoding);
            }

            if (encoding == CustomAttributeEncoding.Enum)
            {
                RuntimeType? enumType = ReadAttributeTypeName(ref parser, module);
                if (enumType is null || !enumType.IsActualEnum)
                {
                    throw new CustomAttributeFormatException();
                }
                return enumType;
            }

            if (allowArray && encoding == CustomAttributeEncoding.Array)
            {
                RuntimeType elementType = ReadAttributeType(ref parser, parser.GetTag(), module, allowArray: false);
                return (RuntimeType)elementType.MakeArrayType();
            }

            throw new CustomAttributeFormatException();
        }

        private static RuntimeType? ReadAttributeTypeName(ref CustomAttributeEncodedArgument.CustomAttributeDataParser parser, RuntimeModule module)
        {
            ReadOnlySpan<byte> utf8Name = parser.GetStringBytes(out bool isNull);
            if (isNull)
            {
                return null;
            }
            if (utf8Name.IsEmpty)
            {
                throw new CustomAttributeFormatException();
            }

            // Type names in the native metadata path are null-terminated, unlike string values.
            int terminator = utf8Name.IndexOf((byte)0);
            if (terminator >= 0)
            {
                utf8Name = utf8Name.Slice(0, terminator);
            }

            const int StackNameLength = 128;
            char[]? rented = null;
            Span<char> name = utf8Name.Length <= StackNameLength
                ? stackalloc char[StackNameLength]
                : (rented = System.Buffers.ArrayPool<char>.Shared.Rent(utf8Name.Length));
            try
            {
                int written = Encoding.UTF8.GetChars(utf8Name, name);
                return TypeNameResolver.GetTypeHelper(name.Slice(0, written), module.GetRuntimeAssembly(),
                    throwOnError: true, requireAssemblyQualifiedName: false);
            }
            finally
            {
                if (rented is not null)
                {
                    System.Buffers.ArrayPool<char>.Shared.Return(rented);
                }
            }
        }

        private static Array? ReadAttributeArray(ref CustomAttributeEncodedArgument.CustomAttributeDataParser parser, RuntimeType elementType, RuntimeModule module)
        {
            int length = parser.GetI4();
            if (length == -1)
            {
                return null;
            }
            if (length < 0)
            {
                throw new OverflowException();
            }

            CustomAttributeEncoding encoding = GetPrimitiveEncoding(elementType);
            int elementSize = GetPrimitiveSize(encoding);
            if (elementSize != 0)
            {
                if (length > parser.Remaining / elementSize)
                {
                    throw new CustomAttributeFormatException();
                }

                Array array = Array.CreateInstance(elementType, length);
                Span<byte> data = new(ref MemoryMarshal.GetArrayDataReference(array), length * elementSize);
                if (BitConverter.IsLittleEndian || elementSize == 1)
                {
                    parser.ReadData(data.Length).CopyTo(data);
                }
                else
                {
                    for (int i = 0; i < length; i++)
                    {
                        Span<byte> element = data.Slice(i * elementSize, elementSize);
                        ReadPrimitiveData(ref parser, encoding).CopyTo(element);
                        element.Reverse();
                    }
                }
                return array;
            }

            if (elementType != typeof(string) && elementType != typeof(Type) &&
                elementType != typeof(object) && !elementType.IsSZArray)
            {
                throw new CustomAttributeFormatException();
            }

            // Every reference element consumes at least a string/tag/array-length byte.
            if (length > parser.Remaining)
            {
                throw new CustomAttributeFormatException();
            }
            var references = (object?[])Array.CreateInstance(elementType, length);
            for (int i = 0; i < references.Length; i++)
            {
                references[i] = ReadAttributeValue(ref parser, elementType, module);
            }
            return references;
        }
    }

    internal static partial class PseudoCustomAttribute
    {
        [Conditional("DEBUG")]
        private static void VerifyPseudoCustomAttribute(RuntimeType pca)
        {
            // If any of these are invariants are no longer true will have to
            // re-architect the PCA product logic and test cases.
            Debug.Assert(pca.BaseType == typeof(Attribute), "Pseudo CA Error - Incorrect base type");
            AttributeUsageAttribute usage = RuntimeCustomAttribute.GetAttributeUsage(pca);
            Debug.Assert(!usage.Inherited, "Pseudo CA Error - Unexpected Inherited value");
            if (pca == typeof(TypeForwardedToAttribute))
            {
                Debug.Assert(usage.AllowMultiple, "Pseudo CA Error - Unexpected AllowMultiple value");
            }
            else
            {
                Debug.Assert(!usage.AllowMultiple, "Pseudo CA Error - Unexpected AllowMultiple value");
            }
        }

        private static DllImportAttribute? GetDllImportCustomAttribute(RuntimeMethodInfo method)
        {
            if ((method.Attributes & MethodAttributes.PinvokeImpl) == 0)
                return null;

            RuntimeModule module = method.Module.ModuleHandle.GetRuntimeModule();
            MetadataImport scope = module.MetadataImport;
            int token = method.MetadataToken;
            scope.GetPInvokeMap(token, out PInvokeAttributes flags, out string entryPoint, out string dllName);
            GC.KeepAlive(module);

            CharSet charSet = CharSet.None;

            switch (flags & PInvokeAttributes.CharSetMask)
            {
                case PInvokeAttributes.CharSetNotSpec: charSet = CharSet.None; break;
                case PInvokeAttributes.CharSetAnsi: charSet = CharSet.Ansi; break;
                case PInvokeAttributes.CharSetUnicode: charSet = CharSet.Unicode; break;
                case PInvokeAttributes.CharSetAuto: charSet = CharSet.Auto; break;

                // Invalid: default to CharSet.None
                default: break;
            }

            CallingConvention callingConvention = CallingConvention.Cdecl;

            switch (flags & PInvokeAttributes.CallConvMask)
            {
                case PInvokeAttributes.CallConvWinapi: callingConvention = CallingConvention.Winapi; break;
                case PInvokeAttributes.CallConvCdecl: callingConvention = CallingConvention.Cdecl; break;
                case PInvokeAttributes.CallConvStdcall: callingConvention = CallingConvention.StdCall; break;
                case PInvokeAttributes.CallConvThiscall: callingConvention = CallingConvention.ThisCall; break;
                case PInvokeAttributes.CallConvFastcall: callingConvention = CallingConvention.FastCall; break;

                // Invalid: default to CallingConvention.Cdecl
                default: break;
            }

            DllImportAttribute attribute = new DllImportAttribute(dllName);

            attribute.EntryPoint = entryPoint;
            attribute.CharSet = charSet;
            attribute.SetLastError = (flags & PInvokeAttributes.SupportsLastError) != 0;
            attribute.ExactSpelling = (flags & PInvokeAttributes.NoMangle) != 0;
            attribute.PreserveSig = (method.GetMethodImplementationFlags() & MethodImplAttributes.PreserveSig) != 0;
            attribute.CallingConvention = callingConvention;
            attribute.BestFitMapping = (flags & PInvokeAttributes.BestFitMask) == PInvokeAttributes.BestFitEnabled;
            attribute.ThrowOnUnmappableChar = (flags & PInvokeAttributes.ThrowOnUnmappableCharMask) == PInvokeAttributes.ThrowOnUnmappableCharEnabled;

            return attribute;
        }

        private static MarshalAsAttribute? GetMarshalAsCustomAttribute(RuntimeParameterInfo parameter)
        {
            return GetMarshalAsCustomAttribute(parameter.MetadataToken, parameter.GetRuntimeModule()!);
        }

        private static MarshalAsAttribute? GetMarshalAsCustomAttribute(RuntimeFieldInfo field)
        {
            return GetMarshalAsCustomAttribute(field.MetadataToken, field.GetRuntimeModule());
        }

        private static MarshalAsAttribute? GetMarshalAsCustomAttribute(int token, RuntimeModule scope)
        {
            ConstArray nativeType = scope.MetadataImport.GetFieldMarshal(token);

            if (nativeType.Length == 0)
                return null;

            return MetadataImport.GetMarshalAs(nativeType, scope);
        }

        private static FieldOffsetAttribute? GetFieldOffsetCustomAttribute(RuntimeFieldInfo field)
        {
            if (field.DeclaringType is not null)
            {
                RuntimeModule module = field.GetRuntimeModule();
                if (module.MetadataImport.GetFieldOffset(field.DeclaringType.MetadataToken, field.MetadataToken, out int fieldOffset))
                {
                    return new FieldOffsetAttribute(fieldOffset);
                }
                GC.KeepAlive(module);
            }
            return null;
        }

        internal static StructLayoutAttribute? GetStructLayoutCustomAttribute(RuntimeType type)
        {
            if (type.IsActualInterface || type.HasElementType || type.IsGenericParameter)
                return null;

            LayoutKind layoutKind = LayoutKind.Auto;
            switch (type.Attributes & TypeAttributes.LayoutMask)
            {
                case TypeAttributes.ExplicitLayout: layoutKind = LayoutKind.Explicit; break;
                case TypeAttributes.AutoLayout: layoutKind = LayoutKind.Auto; break;
                case TypeAttributes.SequentialLayout: layoutKind = LayoutKind.Sequential; break;
                case TypeAttributes.ExtendedLayout: layoutKind = LayoutKind.Extended; break;
                default: Debug.Fail("Unreachable code"); break;
            }

            CharSet charSet = CharSet.None;
            switch (type.Attributes & TypeAttributes.StringFormatMask)
            {
                case TypeAttributes.AnsiClass: charSet = CharSet.Ansi; break;
                case TypeAttributes.AutoClass: charSet = CharSet.Auto; break;
                case TypeAttributes.UnicodeClass: charSet = CharSet.Unicode; break;
                default: Debug.Fail("Unreachable code"); break;
            }
            RuntimeModule module = type.GetRuntimeModule();
            module.MetadataImport.GetClassLayout(type.MetadataToken, out int pack, out int size);
            GC.KeepAlive(module);

            StructLayoutAttribute attribute = new StructLayoutAttribute(layoutKind);

            attribute.Pack = pack;
            attribute.Size = size;
            attribute.CharSet = charSet;

            return attribute;
        }
    }
}
