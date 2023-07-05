// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.TypeSystem.TypesDebugInfo;

using ILCompiler.DependencyAnalysis;

namespace ILCompiler
{
    public class UserDefinedTypeDescriptor
    {
        private object _lock = new object();
        private NodeFactory _nodeFactory;

        private NodeFactory NodeFactory => _nodeFactory;

        private bool Is64Bit => NodeFactory.Target.PointerSize == 8;

        private TargetAbi Abi => NodeFactory.Target.Abi;

        public UserDefinedTypeDescriptor(ITypesDebugInfoWriter objectWriter, NodeFactory nodeFactory)
        {
            _objectWriter = objectWriter;
            _nodeFactory = nodeFactory;
        }

        // Get type index for use as a variable/parameter
        public uint GetVariableTypeIndex(TypeDesc type)
        {
            lock (_lock)
            {
                return GetVariableTypeIndex(type, true);
            }
        }

        public uint GetStateMachineThisVariableTypeIndex(TypeDesc type)
        {
            // If the state machine is a valuetype, the this parameter will be a byref
            if (type.IsByRef)
            {
                type = type.GetParameterType();
                Debug.Assert(type.IsValueType);
            }

            type = DebuggerCanonicalize(type);

            lock (_lock)
            {
                if (!_knownStateMachineThisTypes.TryGetValue(type, out uint typeIndex))
                {
                    Debug.Assert(type.IsDefType);

                    MetadataType defType = (MetadataType)type;
                    ClassTypeDescriptor classTypeDescriptor = new ClassTypeDescriptor
                    {
                        IsStruct = 1,
                        Name = $"StateMachineLocals_{System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(((EcmaType)defType.GetTypeDefinition()).Handle):X}",
                        InstanceSize = defType.InstanceByteCount.IsIndeterminate ? 0 : (ulong)defType.InstanceByteCount.AsInt,
                    };

                    var fieldsDescs = default(ArrayBuilder<DataFieldDescriptor>);

                    foreach (var fieldDesc in defType.GetFields())
                    {
                        if (fieldDesc.IsStatic)
                            continue;

                        // We're going to parse the Roslyn-generated backing fields and unmangle them back to usable names.
                        // We'll also skip some infrastructural fields that are not interesting (like the field that
                        // holds the initial value of parameters, the current state, the current thread ID, etc.).
                        // The set of fields we're going to touch is tagged as "parsed by the expression evaluator"
                        // in the Roslyn codebase, so it's somewhat safe to do this.
                        // https://github.com/dotnet/roslyn/blob/afd10305a37c0ffb2cfb2c2d8446154c68cfa87a/src/Compilers/CSharp/Portable/Symbols/Synthesized/GeneratedNameKind.cs#L15-L22
                        string fieldNameEmit = fieldDesc.Name;
                        if (fieldNameEmit.Length > 0 && fieldNameEmit[0] == '<')
                        {
                            if (TryGetGeneratedNameKind(fieldNameEmit, out char kind))
                            {
                                if (kind == '4' /* ThisProxy */)
                                {
                                    fieldNameEmit = "this";
                                }
                                else if (kind == '5' /* HoistedLocalField */)
                                {
                                    fieldNameEmit = fieldNameEmit.Substring(1, fieldNameEmit.IndexOf('>') - 1);
                                }
                                else
                                {
                                    // The rest of fields support the state machine infrastructure
                                    continue;
                                }
                            }

                            static bool TryGetGeneratedNameKind(string name, out char kind)
                            {
                                int endIndex = name.IndexOf('>');
                                if (endIndex > 0 && endIndex + 1 < name.Length)
                                {
                                    kind = name[endIndex + 1];
                                    return true;
                                }
                                kind = default;
                                return false;
                            }
                        }

                        LayoutInt fieldOffset = fieldDesc.Offset;
                        int fieldOffsetEmit = fieldOffset.IsIndeterminate ? 0xBAAD : fieldOffset.AsInt;

                        TypeDesc fieldType = GetFieldDebugType(fieldDesc);

                        uint fieldTypeIndex = GetVariableTypeIndex(fieldType, false);

                        DataFieldDescriptor field = new DataFieldDescriptor
                        {
                            FieldTypeIndex = fieldTypeIndex,
                            Offset = (ulong)fieldOffsetEmit,
                            Name = fieldNameEmit
                        };

                        fieldsDescs.Add(field);
                    }

                    LayoutInt elementSize = defType.GetElementSize();
                    int elementSizeEmit = elementSize.IsIndeterminate ? 0xBAAD : elementSize.AsInt;
                    ClassFieldsTypeDescriptor fieldsDescriptor = new ClassFieldsTypeDescriptor
                    {
                        Size = (ulong)elementSizeEmit,
                        FieldsCount = fieldsDescs.Count,
                    };

                    uint completeTypeIndex = _objectWriter.GetCompleteClassTypeIndex(classTypeDescriptor, fieldsDescriptor, fieldsDescs.ToArray(), Array.Empty<StaticDataFieldDescriptor>());

                    PointerTypeDescriptor descriptor = new PointerTypeDescriptor
                    {
                        ElementType = completeTypeIndex,
                        Is64Bit = Is64Bit ? 1 : 0,
                        IsConst = 0,
                        IsReference = 1,
                    };

                    typeIndex = _objectWriter.GetPointerTypeIndex(descriptor);

                    _knownStateMachineThisTypes.Add(type, typeIndex);
                }

                return typeIndex;
            }
        }

        // Get Type index for this pointer of specified type
        public uint GetThisTypeIndex(TypeDesc type)
        {
            lock (_lock)
            {
                uint typeIndex;

                if (_thisTypes.TryGetValue(type, out typeIndex))
                    return typeIndex;

                PointerTypeDescriptor descriptor = default(PointerTypeDescriptor);
                // Note the use of GetTypeIndex here instead of GetVariableTypeIndex (We need the type exactly, not a reference to the type (as would happen for arrays/classes), and not a primitive value (as would happen for primitives))
                descriptor.ElementType = GetTypeIndex(type, true);
                descriptor.Is64Bit = Is64Bit ? 1 : 0;
                descriptor.IsConst = 1;
                descriptor.IsReference = 0;

                typeIndex = _objectWriter.GetPointerTypeIndex(descriptor);
                _thisTypes.Add(type, typeIndex);
                return typeIndex;
            }
        }

        // Get type index for method
        public uint GetMethodTypeIndex(MethodDesc method)
        {
            lock (_lock)
            {
                uint typeIndex;

                if (_methodIndices.TryGetValue(method, out typeIndex))
                    return typeIndex;

                MemberFunctionTypeDescriptor descriptor = default(MemberFunctionTypeDescriptor);
                MethodSignature signature = method.Signature;

                descriptor.ReturnType = GetVariableTypeIndex(DebuggerCanonicalize(signature.ReturnType));
                descriptor.ThisAdjust = 0;
                descriptor.CallingConvention = 0x4; // Near fastcall
                descriptor.TypeIndexOfThisPointer = signature.IsStatic ?
                    GetPrimitiveTypeIndex(method.OwningType.Context.GetWellKnownType(WellKnownType.Void)) :
                    GetThisTypeIndex(method.OwningType);
                descriptor.ContainingClass = GetTypeIndex(method.OwningType, true);

                try
                {
                    descriptor.NumberOfArguments = checked((ushort)signature.Length);
                }
                catch (OverflowException)
                {
                    return 0;
                }

                uint[] args = new uint[signature.Length];
                for (int i = 0; i < args.Length; i++)
                    args[i] = GetVariableTypeIndex(DebuggerCanonicalize(signature[i]));


                typeIndex = _objectWriter.GetMemberFunctionTypeIndex(descriptor, args);
                _methodIndices.Add(method, typeIndex);
                return typeIndex;
            }
        }

        // Get type index for specific method by name
        public uint GetMethodFunctionIdTypeIndex(MethodDesc method)
        {
            lock (_lock)
            {
                uint typeIndex;

                if (_methodIdIndices.TryGetValue(method, out typeIndex))
                    return typeIndex;

                MemberFunctionIdTypeDescriptor descriptor = default(MemberFunctionIdTypeDescriptor);

                descriptor.MemberFunction = GetMethodTypeIndex(method);
                descriptor.ParentClass = GetTypeIndex(method.OwningType, true);
                descriptor.Name = method.Name;

                typeIndex = _objectWriter.GetMemberFunctionId(descriptor);
                _methodIdIndices.Add(method, typeIndex);
                return typeIndex;
            }
        }

        private static TypeDesc DebuggerCanonicalize(TypeDesc type)
        {
            if (type.IsCanonicalSubtype(CanonicalFormKind.Any))
                return type.ConvertToCanonForm(CanonicalFormKind.Specific);

            return type;
        }

        private uint GetVariableTypeIndex(TypeDesc type, bool needsCompleteIndex)
        {
            uint variableTypeIndex;
            if (type.IsPrimitive)
            {
                variableTypeIndex = GetPrimitiveTypeIndex(type);
            }
            else
            {
                type = DebuggerCanonicalize(type);

                if ((type.IsDefType && !type.IsValueType) || type.IsArray)
                {
                    // The type index of a variable/field of a reference type is wrapped
                    // in a pointer, as these fields are really pointer fields, and the data is on the heap
                    if (_knownReferenceWrappedTypes.TryGetValue(type, out variableTypeIndex))
                    {
                        return variableTypeIndex;
                    }
                    else
                    {
                        uint typeindex = GetTypeIndex(type, false);

                        PointerTypeDescriptor descriptor = default(PointerTypeDescriptor);
                        descriptor.ElementType = typeindex;
                        descriptor.Is64Bit = Is64Bit ? 1 : 0;
                        descriptor.IsConst = 0;
                        descriptor.IsReference = 1;

                        variableTypeIndex = _objectWriter.GetPointerTypeIndex(descriptor);
                        _knownReferenceWrappedTypes[type] = variableTypeIndex;

                        return variableTypeIndex;
                    }
                }
                else if (type.IsEnum)
                {
                    // Enum's use the LF_ENUM record as the variable type index.

                    if (_enumTypes.TryGetValue(type, out variableTypeIndex))
                        return variableTypeIndex;

                    variableTypeIndex = GetEnumTypeIndex(type);

                    _enumTypes[type] = variableTypeIndex;

                    return variableTypeIndex;
                }

                variableTypeIndex = GetTypeIndex(type, needsCompleteIndex);
            }
            return variableTypeIndex;
        }

        /// <summary>
        /// Get type index for type without the type being wrapped as a reference (as a variable or field must be)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="needsCompleteType"></param>
        /// <returns></returns>
        public uint GetTypeIndex(TypeDesc type, bool needsCompleteType)
        {
            uint typeIndex;
            if (needsCompleteType ?
                _completeKnownTypes.TryGetValue(type, out typeIndex)
                : _knownTypes.TryGetValue(type, out typeIndex))
            {
                return typeIndex;
            }
            else
            {
                return GetNewTypeIndex(type, needsCompleteType);
            }
        }

        private uint GetNewTypeIndex(TypeDesc type, bool needsCompleteType)
        {
            if (type.IsArray)
            {
                return GetArrayTypeIndex(type);
            }
            else if (type.IsDefType)
            {
                return GetClassTypeIndex(type, needsCompleteType);
            }
            else if (type.IsPointer)
            {
                return GetPointerTypeIndex(((ParameterizedType)type).ParameterType);
            }
            else if (type.IsByRef)
            {
                return GetByRefTypeIndex(((ParameterizedType)type).ParameterType);
            }
            else if (type.IsFunctionPointer)
            {
                return GetPointerTypeIndex(type.Context.GetWellKnownType(WellKnownType.Void));
            }

            Debug.Fail("Unhandled UserDefinedTypeDescriptor type: {type}");
            return 0;
        }

        private uint GetPointerTypeIndex(TypeDesc pointeeType)
        {
            uint typeIndex;

            if (_pointerTypes.TryGetValue(pointeeType, out typeIndex))
                return typeIndex;

            PointerTypeDescriptor descriptor = default(PointerTypeDescriptor);
            descriptor.ElementType = GetVariableTypeIndex(pointeeType, false);
            descriptor.Is64Bit = Is64Bit ? 1 : 0;
            descriptor.IsConst = 0;
            descriptor.IsReference = 0;

            // Calling GetVariableTypeIndex may have filled in _pointerTypes
            if (_pointerTypes.TryGetValue(pointeeType, out typeIndex))
                return typeIndex;

            typeIndex = _objectWriter.GetPointerTypeIndex(descriptor);
            _pointerTypes.Add(pointeeType, typeIndex);
            return typeIndex;
        }

        private uint GetByRefTypeIndex(TypeDesc pointeeType)
        {
            uint typeIndex;

            if (_byRefTypes.TryGetValue(pointeeType, out typeIndex))
                return typeIndex;

            PointerTypeDescriptor descriptor = default(PointerTypeDescriptor);
            descriptor.ElementType = GetVariableTypeIndex(pointeeType, false);
            descriptor.Is64Bit = Is64Bit ? 1 : 0;
            descriptor.IsConst = 0;
            descriptor.IsReference = 1;

            // Calling GetVariableTypeIndex may have filled in _byRefTypes
            if (_byRefTypes.TryGetValue(pointeeType, out typeIndex))
                return typeIndex;

            typeIndex = _objectWriter.GetPointerTypeIndex(descriptor);
            _byRefTypes.Add(pointeeType, typeIndex);
            return typeIndex;
        }

        private uint GetEnumTypeIndex(TypeDesc type)
        {
            Debug.Assert(type.IsEnum, "GetEnumTypeIndex was called with wrong type");
            DefType defType = type as DefType;
            Debug.Assert(defType != null, "GetEnumTypeIndex was called with non def type");
            List<FieldDesc> fieldsDescriptors = new List<FieldDesc>();
            foreach (var field in defType.GetFields())
            {
                if (field.IsLiteral)
                {
                    fieldsDescriptors.Add(field);
                }
            }
            EnumTypeDescriptor enumTypeDescriptor = new EnumTypeDescriptor
            {
                ElementCount = (ulong)fieldsDescriptors.Count,
                ElementType = GetPrimitiveTypeIndex(defType.UnderlyingType),
                Name = _objectWriter.GetMangledName(type),
            };
            EnumRecordTypeDescriptor[] typeRecords = new EnumRecordTypeDescriptor[enumTypeDescriptor.ElementCount];
            for (int i = 0; i < fieldsDescriptors.Count; ++i)
            {
                FieldDesc field = fieldsDescriptors[i];
                EnumRecordTypeDescriptor recordTypeDescriptor;
                recordTypeDescriptor.Value = GetEnumRecordValue(field);
                recordTypeDescriptor.Name = field.Name;
                typeRecords[i] = recordTypeDescriptor;
            }
            uint typeIndex = _objectWriter.GetEnumTypeIndex(enumTypeDescriptor, typeRecords);
            return typeIndex;
        }

        private uint GetArrayTypeIndex(TypeDesc type)
        {
            Debug.Assert(type.IsArray, "GetArrayTypeIndex was called with wrong type");
            ArrayType arrayType = (ArrayType)type;

            uint elementSize = (uint)type.Context.Target.PointerSize;
            LayoutInt layoutElementSize = arrayType.GetElementSize();
            if (!layoutElementSize.IsIndeterminate)
                elementSize = (uint)layoutElementSize.AsInt;

            ArrayTypeDescriptor arrayTypeDescriptor = new ArrayTypeDescriptor
            {
                Rank = (uint)arrayType.Rank,
                ElementType = GetVariableTypeIndex(arrayType.ElementType, false),
                Size = elementSize,
                IsMultiDimensional = arrayType.IsMdArray ? 1 : 0
            };

            ClassTypeDescriptor classDescriptor = new ClassTypeDescriptor
            {
                IsStruct = 0,
                Name = _objectWriter.GetMangledName(type),
                BaseClassId = GetTypeIndex(arrayType.BaseType, false)
            };

            uint typeIndex = _objectWriter.GetArrayTypeIndex(classDescriptor, arrayTypeDescriptor);
            _knownTypes[type] = typeIndex;
            _completeKnownTypes[type] = typeIndex;
            return typeIndex;
        }

        private static ulong GetEnumRecordValue(FieldDesc field)
        {
            var ecmaField = field as EcmaField;
            if (ecmaField != null)
            {
                MetadataReader reader = ecmaField.MetadataReader;
                FieldDefinition fieldDef = reader.GetFieldDefinition(ecmaField.Handle);
                ConstantHandle defaultValueHandle = fieldDef.GetDefaultValue();
                if (!defaultValueHandle.IsNil)
                {
                    return HandleConstant(ecmaField.Module, defaultValueHandle);
                }
            }
            return 0;
        }

        private static ulong HandleConstant(EcmaModule module, ConstantHandle constantHandle)
        {
            MetadataReader reader = module.MetadataReader;
            Constant constant = reader.GetConstant(constantHandle);
            BlobReader blob = reader.GetBlobReader(constant.Value);
            switch (constant.TypeCode)
            {
                case ConstantTypeCode.Byte:
                    return (ulong)blob.ReadByte();
                case ConstantTypeCode.Int16:
                    return (ulong)blob.ReadInt16();
                case ConstantTypeCode.Int32:
                    return (ulong)blob.ReadInt32();
                case ConstantTypeCode.Int64:
                    return (ulong)blob.ReadInt64();
                case ConstantTypeCode.SByte:
                    return (ulong)blob.ReadSByte();
                case ConstantTypeCode.UInt16:
                    return (ulong)blob.ReadUInt16();
                case ConstantTypeCode.UInt32:
                    return (ulong)blob.ReadUInt32();
                case ConstantTypeCode.UInt64:
                    return (ulong)blob.ReadUInt64();
            }
            Debug.Assert(false);
            return 0;
        }

        private bool ShouldUseCanonicalTypeRecord(TypeDesc type)
        {
            // TODO: check the type's generic complexity
            return GetGenericDepth(type) > NodeFactory.TypeSystemContext.GenericsConfig.MaxGenericDepthOfDebugRecord;

            static int GetGenericDepth(TypeDesc type)
            {
                if (type.HasInstantiation)
                {
                    int maxGenericDepthInInstantiation = 0;
                    foreach (TypeDesc instantiationType in type.Instantiation)
                    {
                        maxGenericDepthInInstantiation = Math.Max(GetGenericDepth(instantiationType), maxGenericDepthInInstantiation);
                    }

                    return maxGenericDepthInInstantiation + 1;
                }

                if (type.IsParameterizedType)
                    return 1 + GetGenericDepth(((ParameterizedType)type).ParameterType);

                return 0;
            }
        }

        private TypeDesc GetDebugType(TypeDesc type)
        {
            // To avoid infinite generic recursion issues, attempt to use canonical form for fields with high generic complexity.
            if (type.IsCanonicalSubtype(CanonicalFormKind.Specific) || ShouldUseCanonicalTypeRecord(type))
            {
                type = type.ConvertToCanonForm(CanonicalFormKind.Specific);

                if (ShouldUseCanonicalTypeRecord(type))
                {
                    type = type.ConvertToCanonForm(CanonicalFormKind.Universal);
                }
            }

            return type;
        }

        private TypeDesc GetFieldDebugType(FieldDesc field)
        {
            return GetDebugType(field.FieldType);
        }

        private uint GetClassTypeIndex(TypeDesc type, bool needsCompleteType)
        {
            TypeDesc debugType = GetDebugType(type);
            DefType defType = debugType as DefType;
            Debug.Assert(defType != null, "GetClassTypeIndex was called with non def type");
            ClassTypeDescriptor classTypeDescriptor = new ClassTypeDescriptor
            {
                IsStruct = type.IsValueType ? 1 : 0,
                Name = _objectWriter.GetMangledName(defType),
                BaseClassId = 0,
                InstanceSize = 0
            };

            uint typeIndex = _objectWriter.GetClassTypeIndex(classTypeDescriptor);
            _knownTypes[type] = typeIndex;

            if (!defType.InstanceByteCount.IsIndeterminate)
            {
                classTypeDescriptor.InstanceSize = (ulong)defType.InstanceByteCount.AsInt;
            }

            if (type.HasBaseType && !type.IsValueType)
            {
                classTypeDescriptor.BaseClassId = GetTypeIndex(defType.BaseType, true);
            }
            else if (type.IsInterface)
            {
                // Allows debuggers to vtcast the types and see the real instance types.
                classTypeDescriptor.BaseClassId = GetTypeIndex(type.Context.GetWellKnownType(WellKnownType.Object), true);
            }

            List<DataFieldDescriptor> fieldsDescs = new List<DataFieldDescriptor>();
            List<DataFieldDescriptor> nonGcStaticFields = new List<DataFieldDescriptor>();
            List<DataFieldDescriptor> gcStaticFields = new List<DataFieldDescriptor>();
            List<DataFieldDescriptor> threadStaticFields = new List<DataFieldDescriptor>();
            List<StaticDataFieldDescriptor> staticsDescs = new List<StaticDataFieldDescriptor>();

            string nonGcStaticDataName = NodeFactory.NameMangler.NodeMangler.NonGCStatics(type);
            string gcStaticDataName = NodeFactory.NameMangler.NodeMangler.GCStatics(type);
            string threadStaticDataName = NodeFactory.NameMangler.NodeMangler.ThreadStatics(type);
            bool isNativeAOT = Abi == TargetAbi.NativeAot;

            bool isCanonical = defType.IsCanonicalSubtype(CanonicalFormKind.Any);

            foreach (var fieldDesc in defType.GetFields())
            {
                if (fieldDesc.HasRva || fieldDesc.IsLiteral)
                    continue;

                if (isCanonical && fieldDesc.IsStatic)
                    continue;

                LayoutInt fieldOffset = fieldDesc.Offset;
                int fieldOffsetEmit = fieldOffset.IsIndeterminate ? 0xBAAD : fieldOffset.AsInt;

                TypeDesc fieldType = GetFieldDebugType(fieldDesc);

                // We're going to drill into this type more deeply and it might be more than what the
                // compiler already looked at. e.g. if this is a reference type instance field that
                // has fields that don't resolve we might hit resolution exceptions in the process:
                //
                // class NeverInstantiated
                // {
                //     private UnresolvableType Foo;
                // }
                //
                // class GeneratingDebugInfoForThis
                // {
                //     // Going to throw here because instance layout of UnresolvableType cannot be computed.
                //     private NeverInstantiated Bar;
                // }
                //
                // If this happens, just treat the field as Object or IntPtr. Better than crashing here.
                //
                // We are limiting this try/catch to when the type is not a valuetype. If it's a valuetype,
                // we would generate a very invalid debug info. This should have been prevented elsewhere.
                uint fieldTypeIndex;
                try
                {
                    fieldTypeIndex = GetVariableTypeIndex(fieldType, false);
                }
                catch (TypeSystemException) when (!fieldType.IsValueType)
                {
                    fieldTypeIndex = fieldType.IsGCPointer ?
                        GetVariableTypeIndex(fieldType.Context.GetWellKnownType(WellKnownType.Object))
                        : GetVariableTypeIndex(fieldType.Context.GetWellKnownType(WellKnownType.IntPtr));
                }

                DataFieldDescriptor field = new DataFieldDescriptor
                {
                    FieldTypeIndex = fieldTypeIndex,
                    Offset = (ulong)fieldOffsetEmit,
                    Name = fieldDesc.Name
                };

                if (fieldDesc.IsStatic)
                {
                    if (NodeFactory.Target.OperatingSystem != TargetOS.Windows)
                    {
                        StaticDataFieldDescriptor staticDesc = new StaticDataFieldDescriptor
                        {
                            StaticOffset = (ulong)fieldOffsetEmit
                        };

                        // Mark field as static
                        field.Offset = 0xFFFFFFFF;

                        if (fieldDesc.IsThreadStatic) {
                            staticDesc.StaticDataName = threadStaticDataName;
                            staticDesc.IsStaticDataInObject = isNativeAOT ? 1 : 0;
                        } else if (fieldDesc.HasGCStaticBase) {
                            staticDesc.StaticDataName = gcStaticDataName;
                            staticDesc.IsStaticDataInObject = isNativeAOT ? 1 : 0;
                        } else {
                            staticDesc.StaticDataName = nonGcStaticDataName;
                            staticDesc.IsStaticDataInObject = 0;
                        }

                        staticsDescs.Add(staticDesc);
                    }

                    if (fieldDesc.IsThreadStatic)
                        threadStaticFields.Add(field);
                    else if (fieldDesc.HasGCStaticBase)
                        gcStaticFields.Add(field);
                    else
                        nonGcStaticFields.Add(field);
                }
                else
                {
                    fieldsDescs.Add(field);
                }
            }

            if (NodeFactory.Target.OperatingSystem == TargetOS.Windows)
            {
                InsertStaticFieldRegionMember(fieldsDescs, defType, nonGcStaticFields, WindowsNodeMangler.NonGCStaticMemberName, false, false);
                InsertStaticFieldRegionMember(fieldsDescs, defType, gcStaticFields, WindowsNodeMangler.GCStaticMemberName, isNativeAOT, false);
                InsertStaticFieldRegionMember(fieldsDescs, defType, threadStaticFields, WindowsNodeMangler.ThreadStaticMemberName, isNativeAOT, true);
            }
            else
            {
                fieldsDescs.AddRange(nonGcStaticFields);
                fieldsDescs.AddRange(gcStaticFields);
                fieldsDescs.AddRange(threadStaticFields);
            }

            DataFieldDescriptor[] fields = new DataFieldDescriptor[fieldsDescs.Count];
            for (int i = 0; i < fieldsDescs.Count; ++i)
            {
                fields[i] = fieldsDescs[i];
            }

            StaticDataFieldDescriptor[] statics = new StaticDataFieldDescriptor[staticsDescs.Count];
            for (int i = 0; i < staticsDescs.Count; ++i)
            {
                statics[i] = staticsDescs[i];
            }

            LayoutInt elementSize = defType.GetElementSize();
            int elementSizeEmit = elementSize.IsIndeterminate ? 0xBAAD : elementSize.AsInt;
            ClassFieldsTypeDescriptor fieldsDescriptor = new ClassFieldsTypeDescriptor
            {
                Size = (ulong)elementSizeEmit,
                FieldsCount = fieldsDescs.Count,
            };

            uint completeTypeIndex = _objectWriter.GetCompleteClassTypeIndex(classTypeDescriptor, fieldsDescriptor, fields, statics);
            _completeKnownTypes[type] = completeTypeIndex;

            if (needsCompleteType)
                return completeTypeIndex;
            else
                return typeIndex;
        }

        private void InsertStaticFieldRegionMember(List<DataFieldDescriptor> fieldDescs, DefType defType, List<DataFieldDescriptor> staticFields, string staticFieldForm,
                                                   bool staticDataInObject, bool isThreadStatic)
        {
            if (staticFields != null && (staticFields.Count > 0))
            {
                // Generate struct symbol for type describing individual fields of the statics region
                ClassFieldsTypeDescriptor fieldsDescriptor = new ClassFieldsTypeDescriptor
                {
                    Size = (ulong)0,
                    FieldsCount = staticFields.Count
                };

                ClassTypeDescriptor classTypeDescriptor = new ClassTypeDescriptor
                {
                    IsStruct = !staticDataInObject ? 1 : 0,
                    Name = $"__type{staticFieldForm}{_objectWriter.GetMangledName(defType)}",
                    BaseClassId = 0
                };

                if (staticDataInObject)
                {
                    classTypeDescriptor.BaseClassId = GetTypeIndex(defType.Context.GetWellKnownType(WellKnownType.Object), true);
                }

                uint staticFieldRegionTypeIndex = _objectWriter.GetCompleteClassTypeIndex(classTypeDescriptor, fieldsDescriptor, staticFields.ToArray(), null);
                uint staticFieldRegionSymbolTypeIndex = staticFieldRegionTypeIndex;

                if (isThreadStatic)
                {
                    // Generate helper struct used by natvis to get to the actual thread static data
                    ClassFieldsTypeDescriptor helperFieldsDescriptor = new ClassFieldsTypeDescriptor
                    {
                        Size = (ulong)NodeFactory.Target.PointerSize * 2ul,
                        FieldsCount = 2
                    };

                    ClassTypeDescriptor helperClassTypeDescriptor = new ClassTypeDescriptor
                    {
                        IsStruct = 1,
                        Name = $"__ThreadStaticHelper<{classTypeDescriptor.Name}>",
                        BaseClassId = 0
                    };
                    var pointerTypeDescriptor = new PointerTypeDescriptor
                    {
                        Is64Bit = Is64Bit ? 1 : 0,
                        IsConst = 0,
                        IsReference = 0,
                        ElementType = GetTypeIndex(defType.Context.SystemModule.GetType("Internal.Runtime.CompilerHelpers", "TypeManagerSlot"), true)
                    };

                    var helperFields = new DataFieldDescriptor[] {
                        new DataFieldDescriptor
                        {
                            FieldTypeIndex = _objectWriter.GetPointerTypeIndex(pointerTypeDescriptor),
                            Offset = 0,
                            Name = "TypeManagerSlot"
                        },
                        new DataFieldDescriptor
                        {
                            FieldTypeIndex = GetVariableTypeIndex(defType.Context.GetWellKnownType(Is64Bit? WellKnownType.Int64 : WellKnownType.Int32), true),
                            Offset = (ulong)NodeFactory.Target.PointerSize,
                            Name = "ClassIndex"
                        }
                    };

                    staticFieldRegionTypeIndex = _objectWriter.GetCompleteClassTypeIndex(helperClassTypeDescriptor, helperFieldsDescriptor, helperFields, null);
                    staticFieldRegionSymbolTypeIndex = staticFieldRegionTypeIndex;
                    staticFieldForm = WindowsNodeMangler.ThreadStaticIndexName;
                }
                else if (staticDataInObject)// This means that access to this static region is done via indirection
                {
                    PointerTypeDescriptor pointerTypeDescriptor = default(PointerTypeDescriptor);
                    pointerTypeDescriptor.Is64Bit = Is64Bit ? 1 : 0;
                    pointerTypeDescriptor.IsConst = 0;
                    pointerTypeDescriptor.IsReference = 0;
                    pointerTypeDescriptor.ElementType = staticFieldRegionTypeIndex;

                    staticFieldRegionSymbolTypeIndex = _objectWriter.GetPointerTypeIndex(pointerTypeDescriptor);
                }

                DataFieldDescriptor staticRegionField = new DataFieldDescriptor
                {
                    FieldTypeIndex = staticFieldRegionSymbolTypeIndex,
                    Offset = 0xFFFFFFFF,
                    Name = staticFieldForm
                };

                fieldDescs.Add(staticRegionField);
            }
        }

        private uint GetPrimitiveTypeIndex(TypeDesc type)
        {
            Debug.Assert(type.IsPrimitive, "it is not a primitive type");

            uint typeIndex;

            if (_primitiveTypes.TryGetValue(type, out typeIndex))
                return typeIndex;

            typeIndex = _objectWriter.GetPrimitiveTypeIndex(type);
            _primitiveTypes[type] = typeIndex;

            return typeIndex;
        }

        private ITypesDebugInfoWriter _objectWriter;
        private Dictionary<TypeDesc, uint> _knownTypes = new Dictionary<TypeDesc, uint>();
        private Dictionary<TypeDesc, uint> _completeKnownTypes = new Dictionary<TypeDesc, uint>();
        private Dictionary<TypeDesc, uint> _knownReferenceWrappedTypes = new Dictionary<TypeDesc, uint>();
        private Dictionary<TypeDesc, uint> _knownStateMachineThisTypes = new Dictionary<TypeDesc, uint>();
        private Dictionary<TypeDesc, uint> _pointerTypes = new Dictionary<TypeDesc, uint>();
        private Dictionary<TypeDesc, uint> _enumTypes = new Dictionary<TypeDesc, uint>();
        private Dictionary<TypeDesc, uint> _byRefTypes = new Dictionary<TypeDesc, uint>();
        private Dictionary<TypeDesc, uint> _thisTypes = new Dictionary<TypeDesc, uint>();
        private Dictionary<TypeDesc, uint> _primitiveTypes = new Dictionary<TypeDesc, uint>();
        private Dictionary<MethodDesc, uint> _methodIndices = new Dictionary<MethodDesc, uint>();
        private Dictionary<MethodDesc, uint> _methodIdIndices = new Dictionary<MethodDesc, uint>();

        public ICollection<KeyValuePair<TypeDesc, uint>> CompleteKnownTypes => _completeKnownTypes;
    }
}
