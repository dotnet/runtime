// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

    internal sealed partial class CustomAttributeEncodedArgument
    {
        private static CustomAttributeEncodedArgument ParseCustomAttributeValue(
            ref CustomAttributeDataParser parser,
            CustomAttributeType type,
            RuntimeModule module)
        {
            CustomAttributeType attributeType = type.EncodedType == CustomAttributeEncoding.Object
                ? ParseCustomAttributeType(ref parser, module)
                : type;

            CustomAttributeEncodedArgument arg = new(attributeType);

            CustomAttributeEncoding underlyingType = attributeType.EncodedType == CustomAttributeEncoding.Enum
                ? attributeType.EncodedEnumType
                : attributeType.EncodedType;

            switch (underlyingType)
            {
                case CustomAttributeEncoding.Boolean:
                case CustomAttributeEncoding.Byte:
                case CustomAttributeEncoding.SByte:
                    arg.PrimitiveValue = new PrimitiveValue() { Byte4 = parser.GetU1() };
                    break;
                case CustomAttributeEncoding.Char:
                case CustomAttributeEncoding.Int16:
                case CustomAttributeEncoding.UInt16:
                    arg.PrimitiveValue = new PrimitiveValue() { Byte4 = parser.GetU2() };
                    break;
                case CustomAttributeEncoding.Int32:
                case CustomAttributeEncoding.UInt32:
                    arg.PrimitiveValue = new PrimitiveValue() { Byte4 = parser.GetI4() };
                    break;
                case CustomAttributeEncoding.Int64:
                case CustomAttributeEncoding.UInt64:
                    arg.PrimitiveValue = new PrimitiveValue() { Byte8 = parser.GetI8() };
                    break;
                case CustomAttributeEncoding.Float:
                    arg.PrimitiveValue = new PrimitiveValue() { Byte4 = BitConverter.SingleToInt32Bits(parser.GetR4()) };
                    break;
                case CustomAttributeEncoding.Double:
                    arg.PrimitiveValue = new PrimitiveValue() { Byte8 = BitConverter.DoubleToInt64Bits(parser.GetR8()) };
                    break;
                case CustomAttributeEncoding.String:
                case CustomAttributeEncoding.Type:
                    arg.StringValue = parser.GetString();
                    break;
                case CustomAttributeEncoding.Array:
                {
                    arg.ArrayValue = null;
                    int len = parser.GetI4();
                    if (len != -1) // indicates array is null - ECMA-335 II.23.3.
                    {
                        attributeType = new CustomAttributeType(
                            attributeType.EncodedArrayType,
                            CustomAttributeEncoding.Undefined, // Array type
                            attributeType.EncodedEnumType,
                            attributeType.EnumType);
                        arg.ArrayValue = new CustomAttributeEncodedArgument[len];
                        for (int i = 0; i < len; ++i)
                        {
                            arg.ArrayValue[i] = ParseCustomAttributeValue(ref parser, attributeType, module);
                        }
                    }
                    break;
                }
                default:
                    throw new BadImageFormatException();
            }

            return arg;
        }

        private static CustomAttributeType ParseNamedArgumentTarget(
            ref CustomAttributeDataParser parser, RuntimeModule module, out string? argumentName)
        {
            // Determine if a field or property.
            CustomAttributeEncoding namedArgFieldOrProperty = parser.GetTag();
            if (namedArgFieldOrProperty is not CustomAttributeEncoding.Field
                && namedArgFieldOrProperty is not CustomAttributeEncoding.Property)
            {
                throw new BadImageFormatException(SR.Arg_CustomAttributeFormatException);
            }

            CustomAttributeType argumentType = ParseCustomAttributeType(ref parser, module);
            argumentName = parser.GetString();
            return argumentType;
        }

        private static CustomAttributeType ParseCustomAttributeType(ref CustomAttributeDataParser parser, RuntimeModule module)
        {
            CustomAttributeEncoding arrayTag = CustomAttributeEncoding.Undefined;
            CustomAttributeEncoding enumTag = CustomAttributeEncoding.Undefined;
            Type? enumType = null;

            CustomAttributeEncoding tag = parser.GetTag();
            if (tag is CustomAttributeEncoding.Array)
            {
                arrayTag = parser.GetTag();
            }

            // Load the enum type if needed.
            if (tag is CustomAttributeEncoding.Enum
                || (tag is CustomAttributeEncoding.Array
                    && arrayTag is CustomAttributeEncoding.Enum))
            {
                // We cannot determine the underlying type without loading the enum.
                string enumTypeMaybe = parser.GetString() ?? throw new BadImageFormatException();
                enumType = TypeNameResolver.GetTypeReferencedByCustomAttribute(enumTypeMaybe, module);
                if (!enumType.IsEnum)
                {
                    throw new BadImageFormatException();
                }

                enumTag = RuntimeCustomAttributeData.TypeToCustomAttributeEncoding((RuntimeType)enumType.GetEnumUnderlyingType());
            }
            return new CustomAttributeType(tag, arrayTag, enumTag, enumType);
        }

        /// <summary>
        /// Used to parse CustomAttribute data. See ECMA-335 II.23.3.
        /// </summary>
        private ref struct CustomAttributeDataParser
        {
            private int _curr;
            private ReadOnlySpan<byte> _blob;

            public CustomAttributeDataParser(ConstArray attributeBlob)
            {
                unsafe
                {
                    _blob = new ReadOnlySpan<byte>((void*)attributeBlob.Signature, attributeBlob.Length);
                }
                _curr = 0;
            }

            private ReadOnlySpan<byte> PeekData(int size) => _blob.Slice(_curr, size);

            private ReadOnlySpan<byte> ReadData(int size)
            {
                ReadOnlySpan<byte> tmp = PeekData(size);
                Debug.Assert(size <= (_blob.Length - _curr));
                _curr += size;
                return tmp;
            }

            public byte GetU1()
            {
                ReadOnlySpan<byte> tmp = ReadData(sizeof(byte));
                return tmp[0];
            }

            public sbyte GetI1() => (sbyte)GetU1();

            public ushort GetU2()
            {
                ReadOnlySpan<byte> tmp = ReadData(sizeof(ushort));
                return BinaryPrimitives.ReadUInt16LittleEndian(tmp);
            }

            public short GetI2() => (short)GetU2();

            public uint GetU4()
            {
                ReadOnlySpan<byte> tmp = ReadData(sizeof(uint));
                return BinaryPrimitives.ReadUInt32LittleEndian(tmp);
            }

            public int GetI4() => (int)GetU4();

            public ulong GetU8()
            {
                ReadOnlySpan<byte> tmp = ReadData(sizeof(ulong));
                return BinaryPrimitives.ReadUInt64LittleEndian(tmp);
            }

            public long GetI8() => (long)GetU8();

            public float GetR4()
            {
                ReadOnlySpan<byte> tmp = ReadData(sizeof(float));
                return BinaryPrimitives.ReadSingleLittleEndian(tmp);
            }

            public CustomAttributeEncoding GetTag()
            {
                return (CustomAttributeEncoding)GetI1();
            }

            public double GetR8()
            {
                ReadOnlySpan<byte> tmp = ReadData(sizeof(double));
                return BinaryPrimitives.ReadDoubleLittleEndian(tmp);
            }

            public ushort GetProlog() => GetU2();

            public bool ValidateProlog()
            {
                ushort val = GetProlog();
                return val == 0x0001;
            }

            public int GetNamedArgumentCount() => GetI2();

            public string? GetString()
            {
                byte packedLengthBegin = PeekData(sizeof(byte))[0];

                // Check if the embedded string indicates a 'null' string (0xff).
                if (packedLengthBegin == 0xff) // ECMA 335- II.23.3
                {
                    // Consume the indicator.
                    ReadData(1);
                    return null;
                }

                // Not a null string, return a non-null string value.
                // The embedded string a UTF-8 prefixed by an ECMA-335 packed integer.
                int length = GetPackedLength(packedLengthBegin);
                if (length == 0)
                {
                    return string.Empty;
                }

                ReadOnlySpan<byte> utf8ByteSpan = ReadData(length);
                return Encoding.UTF8.GetString(utf8ByteSpan);
            }

            private int GetPackedLength(byte firstByte)
            {
                if ((firstByte & 0x80) == 0)
                {
                    // Consume one byte.
                    ReadData(1);
                    return firstByte & 0x7f;
                }

                int len;
                ReadOnlySpan<byte> data;
                if ((firstByte & 0xC0) == 0x80)
                {
                    // Consume the bytes.
                    data = ReadData(2);
                    len = (data[0] & 0x3f) << 8;
                    return len + data[1];
                }

                if ((firstByte & 0xE0) == 0xC0)
                {
                    // Consume the bytes.
                    data = ReadData(4);
                    len = (data[0] & 0x1f) << 24;
                    len += data[1] << 16;
                    len += data[2] << 8;
                    return len + data[3];
                }

                throw new OverflowException();
            }
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
                        int data = Unsafe.ReadUnaligned<int>((void*)blobStart);
                        if (!BitConverter.IsLittleEndian)
                        {
                            // Metadata is always written in little-endian format. Must account for this on
                            // big-endian platforms.
                            data = BinaryPrimitives.ReverseEndianness(data);
                        }

                        const int CustomAttributeVersion = 0x0001;
                        if ((data & 0xffff) != CustomAttributeVersion)
                        {
                            throw new CustomAttributeFormatException();
                        }
                        cNamedArgs = data >> 16;

                        blobStart = (IntPtr)((byte*)blobStart + 4); // skip version and namedArgs count
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

        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "CustomAttribute_CreateCustomAttributeInstance")]
        private static partial void CreateCustomAttributeInstance(
            QCallModule pModule,
            ObjectHandleOnStack type,
            ObjectHandleOnStack pCtor,
            ref IntPtr ppBlob,
            IntPtr pEndBlob,
            out int pcNamedArgs,
            ObjectHandleOnStack instance);

        private static object CreateCustomAttributeInstance(RuntimeModule module, RuntimeType type, IRuntimeMethodInfo ctor, ref IntPtr blob, IntPtr blobEnd, out int namedArgs)
        {
            if (module is null)
            {
                throw new ArgumentNullException(null, SR.Arg_InvalidHandle);
            }

            object? result = null;
            CreateCustomAttributeInstance(
                new QCallModule(ref module),
                ObjectHandleOnStack.Create(ref type),
                ObjectHandleOnStack.Create(ref ctor),
                ref blob,
                blobEnd,
                out namedArgs,
                ObjectHandleOnStack.Create(ref result));
            return result!;
        }

        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "CustomAttribute_CreatePropertyOrFieldData", StringMarshalling = StringMarshalling.Utf16)]
        private static partial void CreatePropertyOrFieldData(
            QCallModule pModule,
            ref IntPtr ppBlobStart,
            IntPtr pBlobEnd,
            StringHandleOnStack name,
            [MarshalAs(UnmanagedType.Bool)] out bool bIsProperty,
            ObjectHandleOnStack type,
            ObjectHandleOnStack value);

        private static void GetPropertyOrFieldData(
            RuntimeModule module, ref IntPtr blobStart, IntPtr blobEnd, out string name, out bool isProperty, out RuntimeType? type, out object? value)
        {
            if (module is null)
            {
                throw new ArgumentNullException(null, SR.Arg_InvalidHandle);
            }

            string? nameLocal = null;
            RuntimeType? typeLocal = null;
            object? valueLocal = null;
            CreatePropertyOrFieldData(
                new QCallModule(ref module),
                ref blobStart,
                blobEnd,
                new StringHandleOnStack(ref nameLocal),
                out isProperty,
                ObjectHandleOnStack.Create(ref typeLocal),
                ObjectHandleOnStack.Create(ref valueLocal));
            name = nameLocal!;
            type = typeLocal;
            value = valueLocal;
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
