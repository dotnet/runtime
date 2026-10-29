// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
                    bool output = byRef && (parameter.Attributes & ParameterAttributes.Out) != 0;
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
            }
            return contract;
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
                    value = value.WithLifetime(IsRestrictedLifetimeType(type) ? LifetimeScope.Unknown : scope);
                }
                _initialLifetimeMemory[i] = value;
            }

            if (_exceptionRegions.Length != 0)
                // A method-wide may-state includes partial writes and finally continuations.
                _exceptionLifetimeMemory = (StackValue[])_initialLifetimeMemory.Clone();
            else
                _basicBlocks[0].EntryLifetimeMemory = (StackValue[])_initialLifetimeMemory.Clone();
        }

        void StartLifetimeBlock(BasicBlock block)
        {
            _lifetimeSelfChanged = false;
            _lifetimeMemory = _exceptionLifetimeMemory ?? block.EntryLifetimeMemory ?? _initialLifetimeMemory;
            _lifetimeMemoryIsShared = true;
            _unknownLifetimeCacheIsValid = false;
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
            if (_exceptionLifetimeMemory is not null || _lifetimeMemory.Length == 0)
                return;

            bool changed = false;
            if (next.EntryLifetimeMemory is null)
                next.EntryLifetimeMemory = (StackValue[])_lifetimeMemory.Clone();
            else
            {
                for (int i = 0; i < _lifetimeMemory.Length; i++)
                {
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
                return _lifetimeMemory[address.LifetimeTarget - 1].EscapeScope;
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
            if (_exceptionLifetimeMemory is not null)
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
                StackValue joined = _exceptionLifetimeMemory[index].MergeLifetime(_lifetimeMemory[index]);
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
            int target = address.LifetimeTarget - 1;
            StackValue previous = target >= 0 ? _lifetimeMemory[target] : default;
            StoreLifetime(address, value, field.FieldType, whole: false);
            if (!HasLifetime(field.FieldType) || target < 0 || !address.IsWholeLifetimeTarget ||
                previous.InitializedLifetimeFields == ulong.MaxValue ||
                _lifetimeCellTypes[target] != field.OwningType)
                return;

            ulong required = GetLifetimeFieldMask(field.OwningType, field, out ulong bit);
            if (required == ulong.MaxValue || bit == 0)
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
            if (address.LifetimeTarget < 0)
            {
                // Other copies of an untracked address retain this contents snapshot.
                LifetimeCheck(scope >= address.ContentsScope);
                return;
            }
            if (address.LifetimeTarget > 0)
            {
                int target = address.LifetimeTarget - 1;
                StackValue contents = whole ? value : _lifetimeMemory[target].WithLifetime(scope);
                bool initialize = _lifetimeMemory[target].InitializedLifetimeFields != ulong.MaxValue;
                StoreLifetimeCell(target, contents, whole && address.IsWholeLifetimeTarget &&
                    (target < _variableCount || initialize));
                if (target < _variableCount)
                    return;
            }

            // Incoming byrefs can alias one another; an unknown target can also alias locals.
            for (int i = address.LifetimeTarget > 0 ? _variableCount : 0; i < _lifetimeMemory.Length; i++)
            {
                if (HasLifetimeContents(_lifetimeCellTypes[i]) &&
                    (i < _variableCount || address.LifetimeCapacity > LifetimeScope.Function))
                    StoreLifetimeCell(i, _lifetimeMemory[i].WithLifetime(scope), false);
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
                if (_lifetimeCellTypes[i] is TypeDesc type && HasLifetime(type))
                    StoreLifetimeCell(i, _lifetimeMemory[i].WithLifetime(LifetimeScope.Unknown), false);
            }
        }

        bool NeedsCallLifetimes(MethodDesc method, TypeDesc constrained)
        {
            if (HasLifetimeContents(constrained) || HasLifetime(method.Signature.ReturnType) ||
                method.IsConstructor && HasLifetimeContents(method.OwningType))
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
                if (constrained.HasInstantiation)
                    return null;
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
            LifetimeScope returned = contract.Valid && !contract.HasTypedReference ? LifetimeScope.Global : LifetimeScope.Unknown;
            LifetimeScope returnedContents = returned;
            int target = 0;
            bool hasTarget = false;
            bool wholeTarget = true;
            LifetimeScope capacity = LifetimeScope.Function;
            for (int i = 0; i < arguments.Length; i++)
            {
                StackValue argument = arguments[i];
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
                    }
                }
            }

            for (int destination = 0; destination < arguments.Length; destination++)
            {
                StackValue address = arguments[destination];
                if (address.Kind != StackValueKind.ByRef || !HasLifetimeContents(address.Type) || address.IsReadOnly)
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
                wholeTarget, hasTarget ? capacity : LifetimeScope.Caller);
        }
    }
}
