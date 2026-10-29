// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using ILVerify;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace Internal.IL
{
    enum LifetimeScope : byte
    {
        // Ordered by permitted escape: lower values are narrower and more conservative.
        Unknown = 0,
        Function = 1,
        ReturnOnly = 2,
        Caller = 3,
        Global = 4,
    }

    partial class ILImporter
    {
        sealed class LifetimeContract
        {
            public LifetimeScope[] References;
            public LifetimeScope[] Contents;
            public bool[] Outputs;
            public bool[] ReadOnly;
            public FieldDesc StoredField;
            public bool Valid;
            public bool HasTypedReference;
        }

        StackValue[] _lifetimeMemory;
        bool _lifetimeMemoryIsShared;
        bool _unknownLifetimeCacheIsValid;
        LifetimeScope _unknownLocalLifetime;
        LifetimeScope _unknownLifetime;
        StackValue[] _initialLifetimeMemory;
        StackValue[] _exceptionLifetimeMemory;
        TypeDesc[] _lifetimeCellTypes;
        int _argumentCount;
        int _variableCount;
        bool _hasLifetimeOutputs;
        bool _exceptionLifetimeChanged;
        bool _lifetimeSelfChanged;
        LifetimeContract _lifetimeContract;
        Dictionary<MethodDesc, LifetimeContract> _lifetimeContracts;
        HashSet<(int Offset, VerifierError Error)> _lifetimeErrors;

        static LifetimeScope Narrower(LifetimeScope a, LifetimeScope b) => a < b ? a : b;

        static bool HasLifetimeContents(TypeDesc type)
        {
            return type is not null && (type.IsByRefLike ||
                type is GenericParameterDesc parameter && parameter.HasAllowByRefLikeConstraint);
        }

        static bool HasLifetime(TypeDesc type) => type.IsByRef || HasLifetimeContents(type);

        void CheckLifetimeField(FieldDesc field)
        {
            if (HasLifetime(field.FieldType))
                LifetimeCheck(!field.IsStatic && HasLifetimeContents(field.OwningType));
            if (field.FieldType.IsByRef)
                LifetimeCheck(!HasLifetime(((ByRefType)field.FieldType).ParameterType));
        }

        static bool IsRestrictedLifetimeType(TypeDesc type)
        {
            return type.GetTypeDefinition() is MetadataType metadata && metadata.Namespace == "System"u8 &&
                (metadata.Name == "TypedReference"u8 || metadata.Name == "ArgIterator"u8 ||
                 metadata.Name == "RuntimeArgumentHandle"u8);
        }

        static bool IsTypedReference(TypeDesc type)
        {
            if (type.IsByRef)
                type = ((ByRefType)type).ParameterType;
            return type.GetTypeDefinition() is MetadataType metadata &&
                metadata.Namespace == "System"u8 && metadata.Name == "TypedReference"u8;
        }

        void LifetimeCheck(bool condition, VerifierError error = VerifierError.Unverifiable)
        {
            if (!condition)
                (_lifetimeErrors ??= new()).Add((_currentInstructionOffset, error));
        }

        void ReportLifetimeErrors()
        {
            if (_lifetimeErrors is null)
                return;

            var errors = new List<(int Offset, VerifierError Error)>(_lifetimeErrors);
            errors.Sort((a, b) => a.Offset != b.Offset ? a.Offset.CompareTo(b.Offset) : a.Error.CompareTo(b.Error));
            foreach (var error in errors)
                ReportVerificationError(new[] { new ErrorArgument("Offset", error.Offset) }, error.Error);
        }

        // C# 11 low-level struct improvements: return-only, scoped, and invocation rules.
        // https://github.com/dotnet/csharplang/blob/392aec93f87e4d87f082c4c4fad3587140f36947/proposals/csharp-11.0/low-level-struct-improvements.md
        LifetimeContract GetLifetimeContract(MethodDesc method)
        {
            _lifetimeContracts ??= new();
            if (_lifetimeContracts.TryGetValue(method, out LifetimeContract contract))
                return contract;

            MethodSignature signature = method.Signature;
            EcmaMethod ecma = method.GetTypicalMethodDefinition() as EcmaMethod;
            int firstParameter = signature.IsStatic ? 0 : 1;
            int count = signature.Length + firstParameter;
            contract = new LifetimeContract
            {
                References = new LifetimeScope[count],
                Contents = new LifetimeScope[count],
                Outputs = new bool[count],
                ReadOnly = new bool[count],
                // Array methods have runtime-defined contracts, not method metadata.
                Valid = ecma is not null || method is ArrayMethod,
            };
            _lifetimeContracts.Add(method, contract);

            bool modern = false;
            if (ecma is not null)
            {
                MetadataReader reader = ecma.MetadataReader;
                bool found = false;
                foreach (CustomAttributeHandle rules in reader.GetModuleDefinition().GetCustomAttributes())
                {
                    if (!MetadataExtensions.IsEqualCustomAttributeName(rules, reader,
                        "System.Runtime.CompilerServices", "RefSafetyRulesAttribute"))
                        continue;
                    BlobReader blob = reader.GetBlobReader(reader.GetCustomAttribute(rules).Value);
                    modern = blob.Length == 8 && blob.ReadUInt16() == 1 &&
                        blob.ReadInt32() == 11 && blob.ReadUInt16() == 0;
                    contract.Valid &= modern && !found;
                    found = true;
                }
            }

            for (int i = 0; i < count; i++)
            {
                bool receiver = i < firstParameter;
                TypeDesc type = receiver ? method.OwningType : signature[i - firstParameter];
                contract.HasTypedReference |= IsTypedReference(type);
                bool byRef = receiver ? type.IsValueType : type.IsByRef;
                contract.References[i] = !byRef || receiver ? LifetimeScope.Function :
                    modern ? LifetimeScope.ReturnOnly : LifetimeScope.Caller;
                contract.Contents[i] = receiver && method.IsConstructor && type.IsValueType ?
                    LifetimeScope.ReturnOnly : LifetimeScope.Caller;
                contract.Outputs[i] = receiver && method.IsConstructor && type.IsValueType;
            }

            if (ecma is not null)
            {
                MetadataReader reader = ecma.MetadataReader;
                // C# readonly references and readonly members encode the receiver contract
                // on the value type or method, and in parameters on their metadata row.
                // https://github.com/dotnet/csharplang/blob/392aec93f87e4d87f082c4c4fad3587140f36947/proposals/csharp-7.2/readonly-ref.md
                if (firstParameter != 0 && method.OwningType.IsValueType && !method.IsConstructor)
                {
                    contract.ReadOnly[0] = HasLifetimeMarker(reader,
                        reader.GetTypeDefinition(ecma.OwningType.Handle).GetCustomAttributes(),
                        "System.Runtime.CompilerServices", "IsReadOnlyAttribute", contract);
                    contract.ReadOnly[0] |= HasLifetimeMarker(reader,
                        reader.GetMethodDefinition(ecma.Handle).GetCustomAttributes(),
                        "System.Runtime.CompilerServices", "IsReadOnlyAttribute", contract);
                }
                if (firstParameter != 0 && HasUnscopedReceiver(ecma, contract))
                    contract.References[0] = LifetimeScope.ReturnOnly;

                foreach (ParameterHandle handle in reader.GetMethodDefinition(ecma.Handle).GetParameters())
                {
                    Parameter parameter = reader.GetParameter(handle);
                    int index = parameter.SequenceNumber - 1;
                    if ((uint)index >= (uint)signature.Length)
                        continue;

                    int slot = index + firstParameter;
                    bool byRef = signature[index].IsByRef;
                    bool output = byRef && (parameter.Attributes & (ParameterAttributes.In | ParameterAttributes.Out)) ==
                        ParameterAttributes.Out;
                    contract.ReadOnly[slot] = IsReadOnlyLifetimeParameter(signature, index, reader, parameter, contract);
                    bool scoped = HasLifetimeMarker(reader, parameter.GetCustomAttributes(),
                        "System.Runtime.CompilerServices", "ScopedRefAttribute", contract);
                    bool unscoped = HasLifetimeMarker(reader, parameter.GetCustomAttributes(),
                        "System.Diagnostics.CodeAnalysis", "UnscopedRefAttribute", contract);
                    contract.Valid &= !(scoped && unscoped);
                    contract.Outputs[slot] = output;
                    if (output)
                    {
                        contract.Contents[slot] = LifetimeScope.ReturnOnly;
                        if (modern)
                            contract.References[slot] = LifetimeScope.Function;
                    }
                    if (scoped)
                    {
                        if (byRef)
                            contract.References[slot] = LifetimeScope.Function;
                        else
                            contract.Contents[slot] = LifetimeScope.Function;
                    }
                    if (unscoped)
                        contract.References[slot] = output ? LifetimeScope.ReturnOnly : LifetimeScope.Caller;
                }
                contract.StoredField = GetLifetimeFieldStore(method, ecma);
            }
            return contract;
        }

        // C# 12 uses RequiresLocationAttribute instead of IsReadOnlyAttribute for ref readonly.
        // https://github.com/dotnet/csharplang/blob/392aec93f87e4d87f082c4c4fad3587140f36947/proposals/csharp-12.0/ref-readonly-parameters.md#metadata-encoding
        static bool IsReadOnlyLifetimeParameter(MethodSignature signature, int index, MetadataReader reader,
            Parameter parameter, LifetimeContract contract)
        {
            bool readOnly = HasLifetimeMarker(reader, parameter.GetCustomAttributes(),
                "System.Runtime.CompilerServices", "IsReadOnlyAttribute", contract);
            readOnly |= HasLifetimeMarker(reader, parameter.GetCustomAttributes(),
                "System.Runtime.CompilerServices", "RequiresLocationAttribute", contract);
            return signature[index].IsByRef &&
                (parameter.Attributes & (ParameterAttributes.In | ParameterAttributes.Out)) == ParameterAttributes.In &&
                (readOnly || HasReadOnlyParameterModifier(signature, index));
        }

        static FieldDesc GetLifetimeFieldStore(MethodDesc method, EcmaMethod definition)
        {
            MethodSignature signature = method.Signature;
            if (signature.IsStatic || method.IsConstructor || !method.OwningType.IsValueType ||
                !HasLifetimeContents(method.OwningType) || signature.Length != 1 || !signature.ReturnType.IsVoid)
                return null;

            MethodIL body = EcmaMethodIL.Create(definition);
            if (body is null || body.GetExceptionRegions().Length != 0)
                return null;

            // Only a literal field assignment proves that the call does not read
            // uninitialized receiver contents or write any sibling fields.
            const int FieldStoreLength = 8;
            ReadOnlySpan<byte> il = body.GetILBytes();
            if (il.Length == FieldStoreLength + 1 && il[0] == (byte)ILOpcode.nop)
                il = il.Slice(1);
            if (il.Length != FieldStoreLength || il[0] != (byte)ILOpcode.ldarg_0 ||
                il[1] != (byte)ILOpcode.ldarg_1 || il[2] != (byte)ILOpcode.stfld ||
                il[^1] != (byte)ILOpcode.ret)
                return null;

            int token = BinaryPrimitives.ReadInt32LittleEndian(il.Slice(3));
            if (method != definition)
                body = new InstantiatedMethodIL(method, body);
            return body.GetObject(token) is FieldDesc field && !field.IsStatic &&
                field.OwningType == method.OwningType && field.FieldType == signature[0] ? field : null;
        }

        static bool HasReadOnlyParameterModifier(MethodSignature signature, int index)
        {
            if (!signature.HasEmbeddedSignatureData)
                return false;
            foreach (EmbeddedSignatureData modifier in signature.GetEmbeddedSignatureData())
            {
                if (modifier.kind == EmbeddedSignatureDataKind.RequiredCustomModifier &&
                    modifier.type is MetadataType type && type.Namespace == "System.Runtime.InteropServices"u8 &&
                    type.Name == "InAttribute"u8 &&
                    modifier.index == MethodSignature.GetIndexOfCustomModifierOnTypeByParameterIndex(index + 1))
                    return true;
            }
            return false;
        }

        static bool HasUnscopedReceiver(EcmaMethod method, LifetimeContract contract)
        {
            MetadataReader reader = method.MetadataReader;
            bool unscoped = HasLifetimeMarker(reader,
                reader.GetMethodDefinition(method.Handle).GetCustomAttributes(),
                "System.Diagnostics.CodeAnalysis", "UnscopedRefAttribute", contract);

            foreach (PropertyDefinitionHandle handle in reader.GetTypeDefinition(method.OwningType.Handle).GetProperties())
            {
                PropertyDefinition property = reader.GetPropertyDefinition(handle);
                PropertyAccessors accessors = property.GetAccessors();
                bool associated = accessors.Getter == method.Handle || accessors.Setter == method.Handle;
                foreach (MethodDefinitionHandle other in accessors.Others)
                    associated |= other == method.Handle;
                if (associated)
                    unscoped |= HasLifetimeMarker(reader, property.GetCustomAttributes(),
                        "System.Diagnostics.CodeAnalysis", "UnscopedRefAttribute", contract);
            }
            return unscoped;
        }

        static bool HasLifetimeMarker(MetadataReader reader, CustomAttributeHandleCollection attributes,
            string ns, string name, LifetimeContract contract)
        {
            bool found = false;
            foreach (CustomAttributeHandle handle in attributes)
            {
                if (!MetadataExtensions.IsEqualCustomAttributeName(handle, reader, ns, name))
                    continue;

                BlobReader blob = reader.GetBlobReader(reader.GetCustomAttribute(handle).Value);
                contract.Valid &= !found && blob.Length == 4 && blob.ReadUInt16() == 1 && blob.ReadUInt16() == 0;
                found = true;
            }
            return found;
        }

        static StackValue CreateInitialReferentLifetime(TypeDesc type, LifetimeScope contents, bool output)
        {
            StackValue value = StackValue.CreateFromType(type).WithLifetime(contents);
            if (output && HasLifetimeContents(type))
            {
                bool initialized = GetLifetimeFieldMask(type, null, out _) == 0;
                value.ContentsScope = LifetimeScope.Global;
                value.InitializedLifetimeFields = initialized ? ulong.MaxValue : 0;
                if (initialized)
                    value.EscapeScope = LifetimeScope.Global;
            }
            return value;
        }

        void InitializeLifetimes()
        {
            _argumentCount = _methodSignature.Length + (_thisType is null ? 0 : 1);
            _variableCount = _argumentCount + _locals.Length;
            bool hasLifetimeVariables = false;
            for (int i = 0; i < _variableCount; i++)
                hasLifetimeVariables |= HasLifetime(GetVarType(i < _argumentCount ? i : i - _argumentCount, i < _argumentCount));
            if (!hasLifetimeVariables)
            {
                _initialLifetimeMemory = s_emptyStack;
                _lifetimeCellTypes = Array.Empty<TypeDesc>();
                return;
            }
            _lifetimeContract = GetLifetimeContract(_method);
            LifetimeCheck(_lifetimeContract.Valid);
            _lifetimeCellTypes = new TypeDesc[_variableCount + _argumentCount];
            _initialLifetimeMemory = new StackValue[_lifetimeCellTypes.Length];

            for (int i = 0; i < _variableCount; i++)
            {
                bool argument = i < _argumentCount;
                TypeDesc type = GetVarType(argument ? i : i - _argumentCount, argument);
                _lifetimeCellTypes[i] = type;
                StackValue value = StackValue.CreateFromType(type);
                LifetimeScope scope = argument ? _lifetimeContract.Contents[i] :
                    _initLocals ? LifetimeScope.Global : LifetimeScope.Unknown;
                if (argument && type.IsByRef)
                {
                    TypeDesc referent = ((ByRefType)type).ParameterType;
                    int target = _variableCount + i;
                    _lifetimeCellTypes[target] = referent;
                    LifetimeScope contents = _lifetimeContract.Outputs[i] ?
                        LifetimeScope.Unknown : _lifetimeContract.Contents[i];
                    if (IsRestrictedLifetimeType(referent))
                        contents = LifetimeScope.Unknown;
                    _initialLifetimeMemory[target] = CreateInitialReferentLifetime(referent, contents, _lifetimeContract.Outputs[i]);
                    value = value.WithLifetime(_lifetimeContract.References[i], contents, target + 1, true,
                        _lifetimeContract.Contents[i]);
                }
                else
                {
                    // A zero-initialized byref has no referent. These are neutral bounds
                    // for the method-wide weak joins used in exception regions.
                    value = !argument && type.IsByRef && _initLocals ?
                        value.WithLifetime(scope, LifetimeScope.Global, capacity: LifetimeScope.Function) :
                        value.WithLifetime(IsRestrictedLifetimeType(type) ? LifetimeScope.Unknown : scope);
                }
                _initialLifetimeMemory[i] = value;
            }

            if (_exceptionRegions.Length != 0)
            {
                // A method-wide may-state includes partial writes and finally continuations.
                _exceptionLifetimeMemory = (StackValue[])_initialLifetimeMemory.Clone();
                for (int i = _variableCount; i < _exceptionLifetimeMemory.Length; i++)
                {
                    if (IsLifetimeOutputCell(i))
                    {
                        _hasLifetimeOutputs = true;
                        _exceptionLifetimeMemory[i] = StackValue.CreateFromType(_lifetimeCellTypes[i])
                            .WithLifetime(LifetimeScope.Global, LifetimeScope.Global);
                    }
                }
            }
            if (_exceptionLifetimeMemory is null || _hasLifetimeOutputs)
                _basicBlocks[0].EntryLifetimeMemory = (StackValue[])_initialLifetimeMemory.Clone();
        }

        bool IsLifetimeOutputCell(int index) => index >= _variableCount &&
            _lifetimeContract.Outputs[index - _variableCount] && HasLifetimeContents(_lifetimeCellTypes[index]);

        void StartLifetimeBlock(BasicBlock block)
        {
            _lifetimeSelfChanged = false;
            _lifetimeMemory = _exceptionLifetimeMemory ?? block.EntryLifetimeMemory ?? _initialLifetimeMemory;
            _lifetimeMemoryIsShared = true;
            _unknownLifetimeCacheIsValid = false;
            if (_hasLifetimeOutputs)
            {
                _lifetimeMemory = (StackValue[])_lifetimeMemory.Clone();
                _lifetimeMemoryIsShared = false;
                StackValue[] entry = block.EntryLifetimeMemory ?? _initialLifetimeMemory;
                for (int i = _variableCount; i < _lifetimeMemory.Length; i++)
                {
                    if (IsLifetimeOutputCell(i))
                        // Definite assignment follows normal edges; handler effects can only narrow it.
                        _lifetimeMemory[i] = entry[i].MergeLifetime(_exceptionLifetimeMemory[i]);
                }
            }
        }

        void EndLifetimeBlock(BasicBlock block)
        {
            if (_lifetimeSelfChanged)
            {
                block.State = BasicBlock.ImportState.Unmarked;
                MarkBasicBlock(block);
            }
            if (!_exceptionLifetimeChanged)
                return;

            _exceptionLifetimeChanged = false;
            foreach (BasicBlock candidate in _basicBlocks)
            {
                if (candidate is not null && candidate.State == BasicBlock.ImportState.WasVerified)
                {
                    candidate.State = BasicBlock.ImportState.Unmarked;
                    MarkBasicBlock(candidate);
                }
            }
        }

        void PropagateLifetimeMemory(BasicBlock next)
        {
            if (_exceptionLifetimeMemory is not null && !_hasLifetimeOutputs || _lifetimeMemory.Length == 0)
                return;

            bool changed = false;
            if (next.EntryLifetimeMemory is null)
                next.EntryLifetimeMemory = (StackValue[])_lifetimeMemory.Clone();
            else
            {
                for (int i = 0; i < _lifetimeMemory.Length; i++)
                {
                    if (_exceptionLifetimeMemory is not null && !IsLifetimeOutputCell(i))
                        continue;
                    StackValue merged = next.EntryLifetimeMemory[i].MergeLifetime(_lifetimeMemory[i]);
                    if (!merged.HasSameLifetime(next.EntryLifetimeMemory[i]))
                    {
                        next.EntryLifetimeMemory[i] = merged;
                        changed = true;
                    }
                }
            }
            if (changed)
                RevisitLifetimeBlock(next);
        }

        void RevisitLifetimeBlock(BasicBlock next)
        {
            if (next == _currentBasicBlock)
                _lifetimeSelfChanged = true;
            else if (next.State == BasicBlock.ImportState.WasVerified)
                next.State = BasicBlock.ImportState.Unmarked;
        }

        int VariableCell(int index, bool argument) => argument ? index : _argumentCount + index;

        LifetimeScope ValueLifetime(StackValue value)
        {
            return value.Kind == StackValueKind.ByRef || HasLifetimeContents(value.Type) ?
                value.EscapeScope : LifetimeScope.Global;
        }

        LifetimeScope ReadLifetime(StackValue address)
        {
            if (address.Kind != StackValueKind.ByRef)
                return ValueLifetime(address);

            if (!HasLifetimeContents(address.Type))
                return LifetimeScope.Global;

            if (address.LifetimeTarget > 0)
            {
                StackValue cell = _lifetimeMemory[address.LifetimeTarget - 1];
                if (!address.IsWholeLifetimeTarget && cell.InitializedLifetimeFields != ulong.MaxValue)
                {
                    if (address.InitializedLifetimeFields != ulong.MaxValue &&
                        (cell.InitializedLifetimeFields & address.InitializedLifetimeFields) == address.InitializedLifetimeFields)
                        return cell.ContentsScope;
                    return Narrower(address.ContentsScope, cell.ContentsScope);
                }
                return cell.EscapeScope;
            }
            if (address.LifetimeTarget < 0)
                return address.ContentsScope;

            LifetimeScope scope = address.ContentsScope;
            if (!_unknownLifetimeCacheIsValid)
            {
                _unknownLocalLifetime = LifetimeScope.Global;
                _unknownLifetime = LifetimeScope.Global;
                for (int i = 0; i < _lifetimeMemory.Length; i++)
                {
                    if (HasLifetimeContents(_lifetimeCellTypes[i]))
                    {
                        _unknownLifetime = Narrower(_unknownLifetime, _lifetimeMemory[i].EscapeScope);
                        if (i < _variableCount)
                            _unknownLocalLifetime = Narrower(_unknownLocalLifetime, _lifetimeMemory[i].EscapeScope);
                    }
                }
                _unknownLifetimeCacheIsValid = true;
            }
            return Narrower(scope, address.LifetimeCapacity > LifetimeScope.Function ? _unknownLifetime : _unknownLocalLifetime);
        }

        void StoreLifetimeCell(int index, StackValue value, bool replace)
        {
            if (_lifetimeMemory.Length == 0 || !HasLifetime(_lifetimeCellTypes[index]))
                return;
            bool output = IsLifetimeOutputCell(index);
            if (_exceptionLifetimeMemory is not null && !output)
                replace = false;
            if (_lifetimeMemoryIsShared)
            {
                _lifetimeMemory = (StackValue[])_lifetimeMemory.Clone();
                _lifetimeMemoryIsShared = false;
            }
            _lifetimeMemory[index] = replace ? value : _lifetimeMemory[index].MergeLifetime(value);
            _unknownLifetimeCacheIsValid = false;
            if (_exceptionLifetimeMemory is not null)
            {
                StackValue written = _lifetimeMemory[index];
                if (output)
                {
                    // Unassigned fields are not writes and must not poison the may-write summary.
                    written = value;
                    if (written.InitializedLifetimeFields != ulong.MaxValue)
                        written.EscapeScope = written.ContentsScope;
                    written.InitializedLifetimeFields = ulong.MaxValue;
                }
                StackValue joined = _exceptionLifetimeMemory[index].MergeLifetime(written);
                if (!joined.HasSameLifetime(_exceptionLifetimeMemory[index]))
                {
                    _exceptionLifetimeMemory[index] = joined;
                    _exceptionLifetimeChanged = true;
                }
            }
        }

        static ulong GetLifetimeFieldMask(TypeDesc type, FieldDesc field, out ulong fieldMask)
        {
            fieldMask = 0;
            if (type.GetTypeDefinition() is not EcmaType ecma ||
                ecma.Module.Assembly is not EcmaAssembly assembly ||
                assembly.HasAssemblyCustomAttribute("System.Runtime.CompilerServices", "ReferenceAssemblyAttribute"))
                return ulong.MaxValue;

            ulong required = 0;
            int count = 0;
            foreach (FieldDesc candidate in type.GetFields())
            {
                if (candidate.IsStatic || !HasLifetime(candidate.FieldType))
                    continue;
                if (count == 63)
                    // Reserve all bits set for initialized values and unknown layouts.
                    return ulong.MaxValue;
                ulong bit = 1UL << count++;
                required |= bit;
                if (candidate == field)
                    fieldMask = bit;
            }
            return required;
        }

        void StoreFieldLifetime(StackValue address, StackValue value, FieldDesc field)
        {
            StoreLifetime(address, value, field.FieldType, whole: false);
            int target = address.LifetimeTarget - 1;
            if (!HasLifetime(field.FieldType) || target < 0 || !address.IsWholeLifetimeTarget ||
                _lifetimeMemory[target].InitializedLifetimeFields == ulong.MaxValue ||
                _lifetimeCellTypes[target] != field.OwningType)
                return;

            ulong required = GetLifetimeFieldMask(field.OwningType, field, out ulong bit);
            if (required == ulong.MaxValue || bit == 0)
                return;
            InitializeLifetimeField(target, value, required, bit);
        }

        void InitializeLifetimeField(int target, StackValue value, ulong required, ulong bit)
        {
            StackValue previous = _lifetimeMemory[target];
            if (previous.InitializedLifetimeFields == ulong.MaxValue)
                return;
            previous.InitializedLifetimeFields |= bit;
            previous.ContentsScope = Narrower(previous.ContentsScope, ValueLifetime(value));
            if ((previous.InitializedLifetimeFields & required) == required)
            {
                previous.EscapeScope = previous.ContentsScope;
                previous.InitializedLifetimeFields = ulong.MaxValue;
            }
            StoreLifetimeCell(target, previous, true);
        }

        LifetimeScope ReadFieldLifetime(StackValue address, FieldDesc field)
        {
            int target = address.LifetimeTarget - 1;
            if (target >= 0 && address.IsWholeLifetimeTarget && HasLifetime(field.FieldType) &&
                _lifetimeCellTypes[target] == field.OwningType)
            {
                StackValue cell = _lifetimeMemory[target];
                if (cell.InitializedLifetimeFields != ulong.MaxValue &&
                    GetLifetimeFieldMask(field.OwningType, field, out ulong bit) != ulong.MaxValue &&
                    (cell.InitializedLifetimeFields & bit) != 0)
                    return cell.ContentsScope;
            }
            return ReadLifetime(address);
        }

        void StoreLifetime(StackValue address, StackValue value, TypeDesc type, bool whole = true)
        {
            if (!HasLifetime(type))
                return;

            LifetimeScope scope = ValueLifetime(value);
            LifetimeCheck(scope >= address.LifetimeCapacity);
            LifetimeCheck(scope >= address.LifetimeWriteFloor);
            if (address.LifetimeTarget < 0)
            {
                // Other copies of an untracked address retain this contents snapshot.
                LifetimeCheck(scope >= address.ContentsScope);
                return;
            }
            if (address.LifetimeTarget > 0)
            {
                int target = address.LifetimeTarget - 1;
                StackValue contents = whole && address.IsWholeLifetimeTarget ? value : _lifetimeMemory[target].WithLifetime(scope,
                    Narrower(_lifetimeMemory[target].ContentsScope, scope));
                bool initialize = _lifetimeMemory[target].InitializedLifetimeFields != ulong.MaxValue;
                StoreLifetimeCell(target, contents, whole && address.IsWholeLifetimeTarget &&
                    (target < _variableCount || initialize));
                if (whole && !address.IsWholeLifetimeTarget && initialize && address.InitializedLifetimeFields != ulong.MaxValue)
                {
                    ulong required = GetLifetimeFieldMask(_lifetimeCellTypes[target], null, out _);
                    InitializeLifetimeField(target, value, required, address.InitializedLifetimeFields);
                }
                if (target < _variableCount)
                    return;
            }

            // Incoming byrefs can alias one another; an unknown target can also alias locals.
            for (int i = address.LifetimeTarget > 0 ? _variableCount : 0; i < _lifetimeMemory.Length; i++)
            {
                if (HasLifetimeContents(_lifetimeCellTypes[i]) &&
                    (i < _variableCount || address.LifetimeCapacity > LifetimeScope.Function))
                    StoreLifetimeCell(i, _lifetimeMemory[i].WithLifetime(scope,
                        Narrower(_lifetimeMemory[i].ContentsScope, scope)), false);
            }
        }

        void CheckOutputLifetimes()
        {
            if (_lifetimeContract is null)
                return;
            for (int i = 0; i < _argumentCount; i++)
            {
                int cell = _variableCount + i;
                if (_lifetimeContract.Outputs[i] && HasLifetimeContents(_lifetimeCellTypes[cell]))
                    LifetimeCheck(_lifetimeMemory[cell].EscapeScope >= LifetimeScope.ReturnOnly);
            }
        }

        void InvalidateLifetimeMemory()
        {
            for (int i = 0; i < _lifetimeMemory.Length; i++)
            {
                // A byref variable cannot be addressed by verifiable IL, so a call
                // can mutate its referent but cannot rebind the variable itself.
                if (HasLifetimeContents(_lifetimeCellTypes[i]))
                    StoreLifetimeCell(i, _lifetimeMemory[i].WithLifetime(LifetimeScope.Unknown), false);
            }
        }

        bool NeedsCallLifetimes(MethodDesc method, TypeDesc constrained)
        {
            if (!method.Signature.IsStatic && (HasLifetimeContents(constrained) || HasLifetimeContents(method.OwningType)) ||
                HasLifetime(method.Signature.ReturnType))
                return true;
            foreach (TypeDesc parameter in method.Signature)
            {
                if (parameter.IsByRef && HasLifetimeContents(((ByRefType)parameter).ParameterType) ||
                    HasLifetimeContents(method.OwningType) && HasLifetime(parameter))
                    return true;
            }
            return false;
        }

        static bool HasTypedReferenceArguments(MethodDesc method)
        {
            if (!method.Signature.IsStatic && IsTypedReference(method.OwningType))
                return true;
            foreach (TypeDesc parameter in method.Signature)
            {
                if (IsTypedReference(parameter))
                    return true;
            }
            return false;
        }

        static MethodDesc ResolveLifetimeTarget(MethodDesc method, TypeDesc constrained)
        {
            if (constrained is not MetadataType || !constrained.IsValueType ||
                method.Signature.IsStatic || !method.IsVirtual || method.HasInstantiation)
                return null;

            MethodDesc slot = method;
            if (method.OwningType.IsInterface)
            {
                // Generic interfaces can unify after substitution. A selected
                // implementation's contract must not stand in for another candidate.
                int matches = 0;
                bool openInterface = method.OwningType.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true);
                foreach (DefType candidate in constrained.RuntimeInterfaces)
                {
                    if (candidate.HasSameTypeDefinition(method.OwningType) &&
                        (candidate == method.OwningType || openInterface ||
                         candidate.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true)) &&
                        ++matches > 1)
                        return null;
                }
                slot = constrained.ResolveInterfaceMethodTarget(method);
                if (slot is null || slot.OwningType.IsInterface)
                    return null;
            }
            MethodDesc target = slot.IsVirtual ?
                constrained.FindVirtualFunctionTargetMethodOnObjectType(slot) : slot;
            return target?.OwningType.IsValueType == true ? target : null;
        }

        StackValue ApplyCallLifetimes(MethodDesc method, StackValue[] arguments, TypeDesc resultType,
            bool exactTarget, bool needsLifetimeContract)
        {
            StackValue result = StackValue.CreateFromType(resultType);
            if (arguments is null)
                return result;

            LifetimeContract contract = GetLifetimeContract(method);
            if (needsLifetimeContract)
                LifetimeCheck(contract.Valid && !method.OwningType.IsDelegate &&
                    (!method.IsVirtual || method.IsFinal || exactTarget));
            if (exactTarget && contract.Valid && contract.StoredField is FieldDesc field)
            {
                LifetimeCheck(!contract.ReadOnly[0] && !arguments[0].IsReadOnly, VerifierError.ReadOnlyIllegalWrite);
                StoreFieldLifetime(arguments[0], arguments[1], field);
                return result;
            }
            LifetimeScope returned = contract.Valid && !contract.HasTypedReference ? LifetimeScope.Global : LifetimeScope.Unknown;
            LifetimeScope returnedContents = returned;
            int target = 0;
            bool hasTarget = false;
            bool wholeTarget = true;
            LifetimeScope capacity = LifetimeScope.Function;
            LifetimeScope writeFloor = LifetimeScope.Unknown;
            for (int i = 0; i < arguments.Length; i++)
            {
                StackValue argument = arguments[i];
                if (!contract.Outputs[i] && argument.Kind == StackValueKind.ByRef && argument.LifetimeTarget > 0 &&
                    _lifetimeMemory[argument.LifetimeTarget - 1].InitializedLifetimeFields != ulong.MaxValue)
                    LifetimeCheck(ReadLifetime(argument) != LifetimeScope.Unknown);
                if (!contract.ReadOnly[i] && argument.IsReadOnly && HasLifetimeContents(argument.Type))
                    LifetimeCheck(false, VerifierError.ReadOnlyIllegalWrite);
                if (!contract.Outputs[i] && contract.Contents[i] >= LifetimeScope.ReturnOnly)
                    returned = Narrower(returned, ReadLifetime(argument));
                if (argument.Kind == StackValueKind.ByRef && contract.References[i] >= LifetimeScope.ReturnOnly)
                {
                    returned = Narrower(returned, argument.EscapeScope);
                    if (HasLifetimeContents(argument.Type))
                    {
                        returnedContents = Narrower(returnedContents, ReadLifetime(argument));
                        target = !hasTarget ? argument.LifetimeTarget : target == argument.LifetimeTarget ? target : 0;
                        hasTarget = true;
                        wholeTarget &= argument.IsWholeLifetimeTarget;
                        capacity = capacity > argument.LifetimeCapacity ? capacity : argument.LifetimeCapacity;
                        writeFloor = writeFloor > argument.LifetimeWriteFloor ? writeFloor : argument.LifetimeWriteFloor;
                    }
                }
            }

            for (int destination = 0; destination < arguments.Length; destination++)
            {
                StackValue address = arguments[destination];
                if (address.Kind != StackValueKind.ByRef || !HasLifetimeContents(address.Type) ||
                    contract.ReadOnly[destination] || IsRestrictedLifetimeType(address.Type))
                    continue;

                LifetimeScope required = contract.Outputs[destination] ?
                    LifetimeScope.ReturnOnly : LifetimeScope.Caller;
                LifetimeScope written = contract.Outputs[destination] ? LifetimeScope.Global : ReadLifetime(address);
                for (int source = 0; source < arguments.Length; source++)
                {
                    if (!contract.Outputs[source] && contract.Contents[source] >= required)
                        written = Narrower(written, ReadLifetime(arguments[source]));
                    if (arguments[source].Kind == StackValueKind.ByRef && contract.References[source] >= required)
                        written = Narrower(written, arguments[source].EscapeScope);
                }
                if (!contract.Valid)
                    written = LifetimeScope.Unknown;
                StoreLifetime(address, StackValue.CreateFromType(address.Type).WithLifetime(written), address.Type);
            }

            if (contract.HasTypedReference)
                InvalidateLifetimeMemory();

            wholeTarget &= target > 0 && resultType.IsByRef &&
                _lifetimeCellTypes[target - 1] == ((ByRefType)resultType).ParameterType;
            return result.WithLifetime(returned, returnedContents, target < 0 ? 0 : target,
                wholeTarget, hasTarget ? capacity : LifetimeScope.Caller, writeFloor);
        }
    }
}
