// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using ILCompiler.DependencyAnalysis;
using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.TypeSystem.TypesDebugInfo;

using LibObjectFile.Dwarf;
using static ILCompiler.ObjectWriter.DwarfNative;

namespace ILCompiler.ObjectWriter
{
    internal sealed class DwarfBuilder : ITypesDebugInfoWriter
    {
        private NameMangler _nameMangler;
        private TargetArchitecture _architecture;
        private int _targetPointerSize;
        private int _frameRegister;
        private DwarfFile _dwarfFile;
        private DwarfDIECompileUnit _rootDIE;
        private DwarfLineProgramTable _lineTable;
        private Dictionary<int, DwarfLineSequence> _lineSequences = new();
        private Dictionary<string, DwarfFileName> _emittedFileName;

        private List<DwarfMemberFunctionTypeInfo> _memberFunctionTypeInfos = new();
        private List<DwarfDIESubprogram> _memberFunctions = new();
        private Dictionary<TypeFlags, uint> _primitiveDwarfTypes = new();
        private Dictionary<(uint, uint), uint> _simpleArrayDwarfTypes = new(); // (elementTypeIndex, size) -> arrayTypeIndex
        private List<(DwarfDIEMember, StaticDataFieldDescriptor)> _staticFields = new();

        public DwarfFile DwarfFile => _dwarfFile;

        public DwarfBuilder(NameMangler nameMangler, TargetArchitecture targetArchitecture, bool useDwarf5)
        {
            _nameMangler = nameMangler;
            _architecture = targetArchitecture;

            if (targetArchitecture == TargetArchitecture.ARM64)
            {
                _targetPointerSize = 8;
                _frameRegister = 29; // FP
            }
            else if (targetArchitecture == TargetArchitecture.X64)
            {
                _targetPointerSize = 8;
                _frameRegister = 6; // RBP
            }
            else
            {
                throw new NotSupportedException("Unsupported architecture");
            }

            _dwarfFile = new DwarfFile();

            _lineTable = new DwarfLineProgramTable();
            _lineTable.AddressSize = _targetPointerSize == 8 ? DwarfAddressSize.Bit64 : DwarfAddressSize.Bit32;

            _rootDIE = new DwarfDIECompileUnit
            {
                Producer = "CoreRT", // TODO: Change
                Language = DwarfLanguageKind.CPlusPlus,
                Name = "IL.c",
                CompDir = "/tmp",
                StmtList = _lineTable,
            };

            var compilationUnit = new DwarfCompilationUnit()
            {
                AddressSize = _targetPointerSize == 8 ? DwarfAddressSize.Bit64 : DwarfAddressSize.Bit32,
                Root = _rootDIE,
                Version = (ushort)(useDwarf5 ? 5u : 4u),
            };

            _dwarfFile.InfoSection.AddUnit(compilationUnit);
            _dwarfFile.LineSection.AddLineProgramTable(_lineTable);

            _emittedFileName = new();
        }

        public uint GetPrimitiveTypeIndex(TypeDesc type)
        {
            Debug.Assert(type.IsPrimitive, "it is not a primitive type");
            return GetPrimitiveTypeIndex(type.Category);
        }

        private uint GetPrimitiveTypeIndex(TypeFlags typeFlags)
        {
            if (_primitiveDwarfTypes.TryGetValue(typeFlags, out uint index))
            {
                return index;
            }

            if (typeFlags == TypeFlags.Void)
            {
                _rootDIE.AddChild(new DwarfDIEUnspecifiedType { Name = "void" });
            }
            else
            {
                var (name, encoding, byteSize) = typeFlags switch {
                    TypeFlags.Boolean => ("bool", DW_ATE_boolean, 1),
                    TypeFlags.Char => ("char16_t", DW_ATE_UTF, 2),
                    TypeFlags.SByte => ("sbyte", DW_ATE_signed, 1),
                    TypeFlags.Byte => ("byte", DW_ATE_unsigned, 1),
                    TypeFlags.Int16 => ("short", DW_ATE_signed, 2),
                    TypeFlags.UInt16 => ("ushort", DW_ATE_unsigned, 2),
                    TypeFlags.Int32 => ("int", DW_ATE_signed, 4),
                    TypeFlags.UInt32 => ("uint", DW_ATE_unsigned, 4),
                    TypeFlags.Int64 => ("long", DW_ATE_signed, 8),
                    TypeFlags.UInt64 => ("ulong", DW_ATE_unsigned, 8),
                    TypeFlags.IntPtr => ("nint", DW_ATE_signed, _targetPointerSize),
                    TypeFlags.UIntPtr => ("nuint", DW_ATE_unsigned, _targetPointerSize),
                    TypeFlags.Single => ("float", DW_ATE_float, 4),
                    TypeFlags.Double => ("double", DW_ATE_float, 8),
                    _ => ("", 0, 0),
                };

                _rootDIE.AddChild(new DwarfDIEBaseType { Name = name, Encoding = encoding, ByteSize = byteSize});
            }

            uint typeIndex = (uint)_rootDIE.Children.Count;
            _primitiveDwarfTypes.Add(typeFlags, typeIndex);

            return typeIndex;
        }

        public uint GetPointerTypeIndex(PointerTypeDescriptor pointerDescriptor)
        {
            uint voidTypeIndex = GetPrimitiveTypeIndex(TypeFlags.Void);

            // Creating a pointer to what DWARF considers Void type (DW_TAG_unspecified_type -
            // per http://eagercon.com/dwarf/issues/minutes-001017.htm) leads to unhappines
            // since debuggers don't really know how to handle that. The Clang symbol parser
            // in LLDB only handles DW_TAG_unspecified_type if it's named
            // "nullptr_t" or "decltype(nullptr)".
            //
            // We resort to this kludge to generate the exact same debug info for void* that
            // clang would generate (pointer type with no element type specified).
            if (pointerDescriptor.ElementType == voidTypeIndex)
            {
                _rootDIE.AddChild(new DwarfDIEPointerType());
            }
            else
            {
                if (pointerDescriptor.IsReference == 1)
                {
                    _rootDIE.AddChild(new DwarfDIEReferenceType
                    {
                        Type = _rootDIE.Children[(int)pointerDescriptor.ElementType - 1],
                        ByteSize = pointerDescriptor.Is64Bit == 1 ? 8 : 4,
                    });
                }
                else
                {
                    _rootDIE.AddChild(new DwarfDIEPointerType
                    {
                        Type = _rootDIE.Children[(int)pointerDescriptor.ElementType - 1],
                        ByteSize = pointerDescriptor.Is64Bit == 1 ? 8 : 4,
                    });
                }
            }

            return (uint)_rootDIE.Children.Count;
        }

        private uint GetSimpleArrayTypeIndex(uint elementIndex, uint size)
        {
            if (_simpleArrayDwarfTypes.TryGetValue((elementIndex, size), out uint index))
            {
                return index;
            }

            var arrayType = new DwarfDIEArrayType { Type = _rootDIE.Children[(int)elementIndex - 1] };
            arrayType.AddChild(new DwarfDIESubrangeType { UpperBound = (int)size });
            _rootDIE.AddChild(arrayType);

            uint typeIndex = (uint)_rootDIE.Children.Count;
            _simpleArrayDwarfTypes.Add((elementIndex, size), typeIndex);

            return typeIndex;
        }

        public uint GetArrayTypeIndex(
            ClassTypeDescriptor classDescriptor,
            ArrayTypeDescriptor arrayDescriptor)
        {
            int fieldOffset = _targetPointerSize;
            var classType = new DwarfDIEClassType { Name = classDescriptor.Name };

            if (classDescriptor.BaseClassId != 0)
            {
                classType.AddChild(new DwarfDIEInheritance
                {
                    Type = _rootDIE.Children[(int)classDescriptor.BaseClassId - 1],
                    DataMemberLocation = new DwarfLocation(0),
                });
            }

            classType.AddChild(new DwarfDIEMember
            {
                Name = "m_NumComponents",
                Type = _rootDIE.Children[(int)GetPrimitiveTypeIndex(TypeFlags.Int32) - 1],
                DataMemberLocation = new DwarfLocation(fieldOffset),
            });
            fieldOffset += _targetPointerSize;

            if (arrayDescriptor.IsMultiDimensional == 1)
            {
                classType.AddChild(new DwarfDIEMember
                {
                    Name = "m_Bounds",
                    Type = _rootDIE.Children[(int)GetSimpleArrayTypeIndex(GetPrimitiveTypeIndex(TypeFlags.Int32), arrayDescriptor.Rank) - 1],
                    DataMemberLocation = new DwarfLocation(fieldOffset),
                });
                fieldOffset += 2 * 4 * (int)arrayDescriptor.Rank;
            }

            classType.AddChild(new DwarfDIEMember
            {
                Name = "m_Data",
                Type = _rootDIE.Children[(int)GetSimpleArrayTypeIndex(arrayDescriptor.ElementType, 0) - 1],
                DataMemberLocation = new DwarfLocation(fieldOffset),
            });

            classType.ByteSize = fieldOffset;

            _rootDIE.AddChild(classType);

            return (uint)_rootDIE.Children.Count;
        }

        public uint GetEnumTypeIndex(
            EnumTypeDescriptor typeDescriptor,
            EnumRecordTypeDescriptor[] typeRecords)
        {
            var elementType = _rootDIE.Children[(int)typeDescriptor.ElementType - 1];
            var enumerationType = new DwarfDIEEnumerationType
            {
                Name = typeDescriptor.Name,
                Type = elementType,
                ByteSize = ((DwarfDIEBaseType)elementType).ByteSize,
            };

            foreach (EnumRecordTypeDescriptor typeRecord in typeRecords)
            {
                enumerationType.AddChild(new DwarfDIEEnumerator
                {
                    Name = typeRecord.Name,
                    ConstValue = (int)typeRecord.Value,
                });
            }

            _rootDIE.AddChild(enumerationType);

            return (uint)_rootDIE.Children.Count;
        }

        public uint GetClassTypeIndex(ClassTypeDescriptor classDescriptor)
        {
            _dwarfFile.StringTable.GetOrCreateString(classDescriptor.Name);

            var classType = new DwarfDIEClassType
            {
                Name = classDescriptor.Name,
                Declaration = true,
            };

            if (classDescriptor.BaseClassId != 0)
            {
                classType.AddChild(new DwarfDIEInheritance
                {
                    Type = _rootDIE.Children[(int)classDescriptor.BaseClassId - 1],
                    DataMemberLocation = new DwarfLocation(0),
                });
            }

            // Size = ClassDescriptor.InstanceSize?

            _rootDIE.AddChild(classType);

            return (uint)_rootDIE.Children.Count;
        }

        public uint GetCompleteClassTypeIndex(
            ClassTypeDescriptor classTypeDescriptor,
            ClassFieldsTypeDescriptor classFieldsTypeDescriptor,
            DataFieldDescriptor[] fields,
            StaticDataFieldDescriptor[] statics)
        {
            _dwarfFile.StringTable.GetOrCreateString(classTypeDescriptor.Name);

            var classType = new DwarfDIEClassType
            {
                Name = classTypeDescriptor.Name,
                ByteSize = (int)classTypeDescriptor.InstanceSize,
            };

            if (classTypeDescriptor.BaseClassId != 0)
            {
                classType.AddChild(new DwarfDIEInheritance
                {
                    Type = _rootDIE.Children[(int)classTypeDescriptor.BaseClassId - 1],
                    DataMemberLocation = new DwarfLocation(0),
                });
            }

            int staticIndex = 0;
            foreach (DataFieldDescriptor fieldDescriptor in fields)
            {
                _dwarfFile.StringTable.GetOrCreateString(fieldDescriptor.Name);

                var member = new DwarfDIEMember
                {
                    Name = fieldDescriptor.Name,
                    Type = _rootDIE.Children[(int)fieldDescriptor.FieldTypeIndex - 1],
                };

                if (fieldDescriptor.Offset != 0xFFFFFFFFu)
                {
                    member.DataMemberLocation = new DwarfLocation((int)fieldDescriptor.Offset);
                }
                else
                {
                    member.Declaration = true;
                    member.AddAttribute(new DwarfAttribute { Kind = DwarfAttributeKind.External, ValueAsU64 = 1 });
                    _staticFields.Add((member, statics[staticIndex]));
                    staticIndex++;
                }

                classType.AddChild(member);
            }

            // TODO: static members

            _rootDIE.AddChild(classType);

            return (uint)_rootDIE.Children.Count;
        }

        private sealed class DwarfMemberFunctionTypeInfo : DwarfDIEDeclaration
        {
            public MemberFunctionTypeDescriptor MemberDescriptor { get; init; }
            public uint[] ArgumentTypes { get; init; }
            public bool IsStatic { get; init; }
        };

        public uint GetMemberFunctionTypeIndex(MemberFunctionTypeDescriptor memberDescriptor, uint[] argumentTypes)
        {
            _memberFunctionTypeInfos.Add(new DwarfMemberFunctionTypeInfo
            {
                MemberDescriptor = memberDescriptor,
                ArgumentTypes = argumentTypes,
                IsStatic = memberDescriptor.TypeIndexOfThisPointer == GetPrimitiveTypeIndex(TypeFlags.Void),
            });

            return (uint)_memberFunctionTypeInfos.Count;
        }

        public uint GetMemberFunctionId(MemberFunctionIdTypeDescriptor memberIdDescriptor)
        {
            DwarfMemberFunctionTypeInfo memberFunctionTypeInfo =
                _memberFunctionTypeInfos[(int)memberIdDescriptor.MemberFunction - 1];
            MemberFunctionTypeDescriptor memberDescriptor = memberFunctionTypeInfo.MemberDescriptor;
            DwarfDIEClassType parentClass =
                (DwarfDIEClassType)_rootDIE.Children[(int)memberIdDescriptor.ParentClass - 1];

            _dwarfFile.StringTable.GetOrCreateString(memberIdDescriptor.Name);

            var subprogram = new DwarfDIESubprogram
            {
                Name = memberIdDescriptor.Name,
                LinkageName = memberIdDescriptor.Name,
                Type = _rootDIE.Children[(int)memberDescriptor.ReturnType - 1],
                // decl file, decl line
                External = true,
                Declaration = true,
            };

            if (!memberFunctionTypeInfo.IsStatic)
            {
                subprogram.ObjectPointer = new DwarfDIEFormalParameter
                {
                    Type = _rootDIE.Children[(int)memberDescriptor.TypeIndexOfThisPointer - 1],
                    Artificial = true,
                };
                subprogram.AddChild(subprogram.ObjectPointer);
            }

            foreach (var argType in memberFunctionTypeInfo.ArgumentTypes)
            {
                subprogram.AddChild(new DwarfDIEFormalParameter
                {
                    Type = _rootDIE.Children[(int)argType - 1],
                });
            }

            parentClass.AddChild(subprogram);
            _memberFunctions.Add(subprogram);

            return (uint)_memberFunctions.Count;
        }

        public string GetMangledName(TypeDesc type)
        {
            return _nameMangler.GetMangledTypeName(type);
        }

        private sealed class LexicalScope
        {
            private uint _start;
            private uint _end;
            private bool _isFuncScope;
            private List<(DebugVarInfoMetadata, DwarfDIE)> _vars = new();
            private List<LexicalScope> _innerScopes = new();

            public uint Start => _start;
            public uint End => _end;
            public bool IsFuncScope => _isFuncScope;
            public List<(DebugVarInfoMetadata, DwarfDIE)> Vars => _vars;
            public List<LexicalScope> InnerScopes => _innerScopes;

            public LexicalScope(uint start, uint end, bool isFuncScope)
            {
                _start = start;
                _end = end;
                _isFuncScope = isFuncScope;
            }

            public LexicalScope(DebugVarRangeInfo rangeInfo)
            {
                _start = rangeInfo.StartOffset;
                _end = rangeInfo.EndOffset;
                _isFuncScope = false;
            }

            private bool IsContains(DebugVarRangeInfo rangeInfo)
                => _start <= rangeInfo.StartOffset && _end >= rangeInfo.EndOffset;

            public void AddVar(DebugVarInfoMetadata metadataInfo, DwarfDIE type)
            {
                if (metadataInfo.IsParameter && _isFuncScope)
                {
                    _vars.Add((metadataInfo, type));
                    return;
                }

                foreach (var rangeInfo in metadataInfo.DebugVarInfo.Ranges)
                {
                    if (!IsContains(rangeInfo))
                        return;

                    // Var belongs to inner scope
                    if (rangeInfo.StartOffset != _start || rangeInfo.EndOffset != _end)
                    {
                        // Try to add variable to one the inner scopes
                        foreach (var innerScope in _innerScopes)
                        {
                            if (innerScope.IsContains(rangeInfo))
                            {
                                innerScope.AddVar(metadataInfo, type);
                                return;
                            }
                        }

                        // We need to create new inner scope for this var
                        var newInnerScope = new LexicalScope(rangeInfo);
                        newInnerScope.AddVar(metadataInfo, type);
                        _innerScopes.Add(newInnerScope);
                    }
                    else
                    {
                        _vars.Add((metadataInfo, type));
                    }
                }
            }
        };

        // TODO: Clean-up and share with R2R code
        private enum RegNumAmd64 : int
        {
            REGNUM_RAX,
            REGNUM_RCX,
            REGNUM_RDX,
            REGNUM_RBX,
            REGNUM_RSP,
            REGNUM_RBP,
            REGNUM_RSI,
            REGNUM_RDI,
            REGNUM_R8,
            REGNUM_R9,
            REGNUM_R10,
            REGNUM_R11,
            REGNUM_R12,
            REGNUM_R13,
            REGNUM_R14,
            REGNUM_R15,
            REGNUM_COUNT,
            REGNUM_SP = REGNUM_RSP,
            REGNUM_FP = REGNUM_RBP
        };

        private int DwarfRegNum(int regNum)
        {
            if (_architecture == TargetArchitecture.ARM64)
            {
                // Normal registers are directly mapped
                if (regNum >= 33)
                    regNum = regNum - 33 + 64; // FP
                return regNum;
            }
            else if (_architecture == TargetArchitecture.X64)
            {
                return (RegNumAmd64)regNum switch
                {
                    RegNumAmd64.REGNUM_RAX => 0,
                    RegNumAmd64.REGNUM_RDX => 1,
                    RegNumAmd64.REGNUM_RCX => 2,
                    RegNumAmd64.REGNUM_RBX => 3,
                    RegNumAmd64.REGNUM_RSI => 4,
                    RegNumAmd64.REGNUM_RDI => 5,
                    RegNumAmd64.REGNUM_RBP => 6,
                    RegNumAmd64.REGNUM_RSP => 7,
                    RegNumAmd64.REGNUM_R8 => 8,
                    RegNumAmd64.REGNUM_R9 => 9,
                    RegNumAmd64.REGNUM_R10 => 10,
                    RegNumAmd64.REGNUM_R11 => 11,
                    RegNumAmd64.REGNUM_R12 => 12,
                    RegNumAmd64.REGNUM_R13 => 13,
                    RegNumAmd64.REGNUM_R14 => 14,
                    RegNumAmd64.REGNUM_R15 => 15,
                    _ => regNum - (int)RegNumAmd64.REGNUM_COUNT + 17 // FP registers
                };
            }
            else
            {
                throw new NotSupportedException();
            }
        }


        private DwarfOperation DwarfReg(int regNum)
        {
            int dwarfRegNum = DwarfRegNum(regNum);
            if (dwarfRegNum < 32)
            {
                return new DwarfOperation { Kind = (DwarfOperationKind)((int)DwarfOperationKind.Reg0 + dwarfRegNum) };
            }
            else
            {
                return new DwarfOperation
                {
                    Kind = DwarfOperationKind.Regx,
                    Operand1 = { U64 = (uint)dwarfRegNum },
                };
            }
        }

        private DwarfOperation DwarfBreg(int regNum, int offset = 0)
        {
            int dwarfRegNum = DwarfRegNum(regNum);
            if (dwarfRegNum < 32)
            {
                return new DwarfOperation
                {
                    Kind = (DwarfOperationKind)((int)DwarfOperationKind.Breg0 + dwarfRegNum),
                    Operand2 = { I64 = offset },
                };
            }
            else
            {
                return new DwarfOperation
                {
                    Kind = DwarfOperationKind.Bregx,
                    Operand1 = { U64 = (uint)dwarfRegNum },
                    Operand2 = { I64 = offset },
                };
            }
        }

        private DwarfExpression DwarfVarLocation(VarLoc loc)
        {
            var e = new DwarfExpression();

            switch (loc.LocationType)
            {
                case VarLocType.VLT_REG:
                case VarLocType.VLT_REG_FP:
                    e.AddOperation(DwarfReg(loc.B));
                    break;
                case VarLocType.VLT_REG_BYREF:
                    e.AddOperation(DwarfBreg(loc.B));
                    break;
                case VarLocType.VLT_STK:
                case VarLocType.VLT_STK2:
                case VarLocType.VLT_FPSTK:
                case VarLocType.VLT_STK_BYREF:
                    e.AddOperation(DwarfBreg(loc.B, loc.C));
                    if (loc.LocationType == VarLocType.VLT_STK_BYREF)
                    {
                        e.AddOperation(new DwarfOperation { Kind = DwarfOperationKind.Deref });
                    }
                    break;
                case VarLocType.VLT_REG_REG:
                    e.AddOperation(DwarfReg(loc.C));
                    e.AddOperation(new DwarfOperation { Kind = DwarfOperationKind.Piece });
                    e.AddOperation(DwarfReg(loc.B));
                    e.AddOperation(new DwarfOperation { Kind = DwarfOperationKind.Piece });
                    break;
                case VarLocType.VLT_REG_STK:
                case VarLocType.VLT_STK_REG:
                    if (loc.LocationType == VarLocType.VLT_REG_STK)
                    {
                        e.AddOperation(DwarfReg(loc.B));
                        e.AddOperation(new DwarfOperation { Kind = DwarfOperationKind.Piece });
                        e.AddOperation(DwarfBreg(loc.C, loc.D));
                        e.AddOperation(new DwarfOperation { Kind = DwarfOperationKind.Piece });
                    }
                    else
                    {
                        e.AddOperation(DwarfBreg(loc.C, loc.D));
                        e.AddOperation(new DwarfOperation { Kind = DwarfOperationKind.Piece });
                        e.AddOperation(DwarfReg(loc.B));
                        e.AddOperation(new DwarfOperation { Kind = DwarfOperationKind.Piece });
                    }
                    break;
                default:
                    // Unsupported
                    Debug.Assert(loc.LocationType != VarLocType.VLT_FIXED_VA);
                    return null;
            }

            return e;
        }

        private DwarfDIE EmitVar(DebugVarInfoMetadata metadataInfo, ulong methodPCStart, DwarfDIE type, bool isThis)
        {
            string varName = metadataInfo.Name;
            DwarfLocation? location = null;

            if (metadataInfo.DebugVarInfo.Ranges.Length == 1)
            {
                var e = DwarfVarLocation(metadataInfo.DebugVarInfo.Ranges[0].VarLoc);
                if (e != null)
                {
                    location = new DwarfLocation(e);
                }
                else
                {
                    Console.WriteLine("F: " + metadataInfo.Name);
                }
            }
            else
            {
                var locationList = new DwarfLocationList();
                foreach (var range in metadataInfo.DebugVarInfo.Ranges)
                {
                    var e = DwarfVarLocation(range.VarLoc);
                    if (e != null)
                    {
                        locationList.AddLocationListEntry(new DwarfLocationListEntry
                        {
                            Start = range.StartOffset + methodPCStart,
                            End = range.EndOffset + methodPCStart,
                            Expression = e,
                        });
                    }
                }
                _dwarfFile.LocationSection.AddLocationList(locationList);
                location = locationList;
            }

            if (metadataInfo.IsParameter)
            {
                var formalParameter = new DwarfDIEFormalParameter
                {
                    Name = isThis ? "this" : varName,
                    // decl file, line
                    Type = type,
                };

                if (location != null)
                {
                    formalParameter.Location = location;
                }

                if (isThis)
                {
                    formalParameter.Artificial = true;
                }

                return formalParameter;
            }
            else
            {
                var variable = new DwarfDIEVariable
                {
                    Name = varName,
                    Type = type,
                };

                if (location != null)
                {
                    variable.Location = location;
                }

                return variable;
            }
        }

        private void EmitLexicalBlock(LexicalScope lexicalScope, ulong methodPCStart, bool methodIsStatic, DwarfDIE die)
        {
            if (!lexicalScope.IsFuncScope)
            {
                var lexicalBlock = new DwarfDIELexicalBlock
                {
                    LowPC = methodPCStart + lexicalScope.Start,
                    HighPC = (int)(lexicalScope.End - lexicalScope.Start),
                };

                die.AddChild(lexicalBlock);
                die = lexicalBlock;
            }

            foreach (var (var, type) in lexicalScope.Vars)
            {
                bool isThis = var.IsParameter && var.DebugVarInfo.VarNumber == 0 && !methodIsStatic;
                var dwarfVar = EmitVar(var, methodPCStart, type, isThis);
                die.AddChild(dwarfVar);
                if (isThis)
                {
                    ((DwarfDIESubprogram)die).ObjectPointer = dwarfVar;
                }
            }

            foreach (var innerScope in lexicalScope.InnerScopes)
            {
                EmitLexicalBlock(innerScope, methodPCStart, methodIsStatic, die);
            }
        }

        public void EmitSubprogramInfo(
            string methodName,
            ulong methodPCStart,
            int methodPCLength,
            uint methodTypeIndex,
            IEnumerable<(DebugVarInfoMetadata, uint)> debugVars,
            IEnumerable<DebugEHClauseInfo> debugEHClauseInfos)
        {
            if (methodTypeIndex == 0)
            {
                return;
            }

            var subprogramSpec = _memberFunctions[(int)methodTypeIndex - 1];

            _dwarfFile.StringTable.GetOrCreateString(methodName);
            subprogramSpec.LinkageName = methodName;

            var frameExpression = new DwarfExpression();
            frameExpression.AddOperation(new DwarfOperation { Kind = (DwarfOperationKind)((int)DwarfOperationKind.Reg0 + _frameRegister) });
            var subprogram = new DwarfDIESubprogram
            {
                Specification = subprogramSpec,
                LowPC = methodPCStart,
                HighPC = methodPCLength,
                FrameBase = new DwarfLocation(frameExpression),
            };

            var methodScope = new LexicalScope(0u, (uint)methodPCLength, true);
            foreach (var (debugVar, typeIndex) in debugVars)
            {
                methodScope.AddVar(debugVar, typeIndex == 0 ? null : _rootDIE.Children[(int)typeIndex - 1]);
            }
            EmitLexicalBlock(methodScope, methodPCStart, subprogramSpec.ObjectPointer == null, subprogram);

            foreach (var clause in debugEHClauseInfos)
            {
                subprogram.AddChild(new DwarfDIETryBlock
                {
                    LowPC = methodPCStart + clause.TryOffset,
                    HighPC = (int)clause.TryLength,
                });

                subprogram.AddChild(new DwarfDIECatchBlock
                {
                    LowPC = methodPCStart + clause.HandlerOffset,
                    HighPC = (int)clause.HandlerLength,
                });
            }

            _rootDIE.AddChild(subprogram);
        }

        public void EmitLineInfo(int sectionIndex, ulong methodPCStart, IEnumerable<NativeSequencePoint> sequencePoints)
        {
            DwarfLineSequence lineSequence;

            // Create line sequence for every section so they can get the
            // base address relocated properly.
            if (!_lineSequences.TryGetValue(sectionIndex, out lineSequence))
            {
                lineSequence = new DwarfLineSequence();
                _lineTable.AddLineSequence(lineSequence);
                _lineSequences.Add(sectionIndex, lineSequence);
            }

            foreach (var sequencePoint in sequencePoints)
            {
                DwarfFileName dwarfFileName;

                if (!_emittedFileName.TryGetValue(sequencePoint.FileName, out dwarfFileName))
                {
                    dwarfFileName = new DwarfFileName
                    {
                        Name = Path.GetFileName(sequencePoint.FileName),
                        Directory = Path.GetDirectoryName(sequencePoint.FileName),
                    };
                    _emittedFileName.Add(sequencePoint.FileName, dwarfFileName);
                    _lineTable.FileNames.Add(dwarfFileName);
                }

                lineSequence.Add(new DwarfLine
                {
                    File = dwarfFileName,
                    Address = methodPCStart + (ulong)sequencePoint.NativeOffset,
                    Column = (uint)sequencePoint.ColNumber,
                    Line = (uint)sequencePoint.LineNumber,
                });
            }
        }

        public void EmitStaticVars(Func<string, ulong> resolveNameToAddress)
        {
            foreach (var (memberSpec, staticVarInfo) in _staticFields)
            {
                var staticAddress = resolveNameToAddress(staticVarInfo.StaticDataName);

                if (staticAddress == 0)
                {
                    continue;
                }

                var expression = new DwarfExpression();
                expression.AddOperation(new DwarfOperation
                {
                    Kind = DwarfOperationKind.Addr,
                    Operand1 = { U64 = staticAddress },
                });

                if (staticVarInfo.IsStaticDataInObject == 1)
                {
                    expression.AddOperation(new DwarfOperation { Kind = DwarfOperationKind.Deref });
                    expression.AddOperation(new DwarfOperation { Kind = DwarfOperationKind.Deref });
                }

                if (staticVarInfo.StaticOffset != 0)
                {
                    expression.AddOperation(new DwarfOperation
                    {
                        Kind = DwarfOperationKind.PlusUconst,
                        Operand1 = { U64 = staticVarInfo.StaticOffset },
                    });
                }

                var variable = new DwarfDIEVariable
                {
                    Specification = memberSpec,
                    Location = new DwarfLocation(expression),
                };

                _rootDIE.AddChild(variable);
            }
        }
    }
}
