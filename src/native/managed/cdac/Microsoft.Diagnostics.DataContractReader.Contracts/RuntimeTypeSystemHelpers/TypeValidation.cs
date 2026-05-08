// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;

internal sealed class TypeValidation
{
    private readonly Target _target;
    private readonly TargetPointer _continuationMethodTablePointer;

    internal TypeValidation(Target target, TargetPointer continuationMethodTablePointer)
    {
        _target = target;
        _continuationMethodTablePointer = continuationMethodTablePointer;
    }

    // This doesn't need as many properties as MethodTable because we don't want to be operating on
    // a NonValidatedMethodTable for too long
    internal struct NonValidatedMethodTable
    {
        private readonly Target _target;
        private readonly Target.TypeInfo _type;
        internal TargetPointer Address { get; init; }

        private MethodTableFlags_1? _methodTableFlags;

        internal NonValidatedMethodTable(Target target, TargetPointer methodTablePointer)
        {
            _target = target;
            _type = target.GetTypeInfo(DataType.MethodTable);
            Address = methodTablePointer;
            _methodTableFlags = null;
        }

        private MethodTableFlags_1 GetOrCreateFlags()
        {
            if (_methodTableFlags == null)
            {
                // note: may throw if the method table Address is corrupted
                MethodTableFlags_1 flags = new MethodTableFlags_1
                {
                    MTFlags = _target.Read<uint>(Address + (ulong)_type.Fields[nameof(MethodTableFlags_1.MTFlags)].Offset),
                    MTFlags2 = _target.Read<uint>(Address + (ulong)_type.Fields[nameof(MethodTableFlags_1.MTFlags2)].Offset),
                    BaseSize = _target.Read<uint>(Address + (ulong)_type.Fields[nameof(MethodTableFlags_1.BaseSize)].Offset),
                };
                _methodTableFlags = flags;
            }
            return _methodTableFlags.Value;
        }

        internal MethodTableFlags_1 Flags => GetOrCreateFlags();

        internal TargetPointer EEClassOrCanonMT => _target.ReadPointer(Address + (ulong)_type.Fields[nameof(EEClassOrCanonMT)].Offset);
        internal TargetPointer EEClass => MethodTableFlags_1.GetEEClassOrCanonMTBits(EEClassOrCanonMT) == MethodTableFlags_1.EEClassOrCanonMTBits.EEClass ? EEClassOrCanonMT : throw new InvalidOperationException("not an EEClass");
        internal TargetPointer CanonMT
        {
            get
            {
                if (MethodTableFlags_1.GetEEClassOrCanonMTBits(EEClassOrCanonMT) == MethodTableFlags_1.EEClassOrCanonMTBits.CanonMT)
                {
                    return MethodTableFlags_1.UntagEEClassOrCanonMT(EEClassOrCanonMT);
                }
                else
                {
                    throw new InvalidOperationException("not a canonical method table");
                }
            }
        }

        internal readonly bool ValidateReadable() => ValidateDataReadable<MethodTable>(_target, Address);
        internal TargetPointer ParentMethodTable => _target.ReadPointer(Address + (ulong)_type.Fields[nameof(ParentMethodTable)].Offset);
    }

    internal struct NonValidatedEEClass
    {
        public readonly Target _target;
        private readonly Target.TypeInfo _type;

        internal TargetPointer Address { get; init; }

        internal NonValidatedEEClass(Target target, TargetPointer eeClassPointer)
        {
            _target = target;
            Address = eeClassPointer;
            _type = target.GetTypeInfo(DataType.EEClass);
        }

        internal TargetPointer MethodTable => _target.ReadPointer(Address + (ulong)_type.Fields[nameof(MethodTable)].Offset);

        internal readonly bool ValidateReadable() => ValidateDataReadable<EEClass>(_target, Address);
    }

    internal static NonValidatedMethodTable GetMethodTableData(Target target, TargetPointer methodTablePointer)
    {
        return new NonValidatedMethodTable(target, methodTablePointer);
    }

    internal static NonValidatedEEClass GetEEClassData(Target target, TargetPointer eeClassPointer)
    {
        return new NonValidatedEEClass(target, eeClassPointer);
    }


    /// <summary>
    /// Validates that the given address is a valid MethodTable.
    /// </summary>
    ///  <remarks>
    ///  If the target process has memory corruption, we may see pointers that are not valid method tables.
    ///  We validate by looking at the MethodTable -> EEClass -> MethodTable relationship (which may throw if we access invalid memory).
    ///  And then we do some ad-hoc checks on the method table flags.
    private bool ValidateMethodTablePointer(NonValidatedMethodTable umt)
    {
        try
        {
            // Make sure that we can read the method table's data.
            if (!umt.ValidateReadable())
            {
                return false;
            }
            if (!ValidateThrowing(umt))
            {
                return false;
            }
            if (!ValidateMethodTableAdHoc(umt))
            {
                return false;
            }
        }
        catch (System.Exception)
        {
            // TODO(cdac): maybe don't swallow all exceptions? We could consider a richer contract that
            // helps to track down what sort of memory corruption caused the validation to fail.
            // TODO(cdac): we could also consider a more fine-grained exception type so we don't mask
            // programmer mistakes in the cdac.
            return false;
        }
        return true;
    }

    // This portion of validation may throw if we are trying to read an invalid address in the target process
    private bool ValidateThrowing(NonValidatedMethodTable methodTable)
    {
        // For non-generic classes, we can rely on comparing
        //    object->methodtable->class->methodtable
        // to
        //    object->methodtable
        //
        //  However, for generic instantiation this does not work. There we must
        //  compare
        //
        //    object->methodtable->class->methodtable->class
        // to
        //    object->methodtable->class
        TargetPointer eeClassPtr = GetClassThrowing(methodTable);
        if (eeClassPtr != TargetPointer.Null)
        {
            NonValidatedEEClass eeClass = GetEEClassData(_target, eeClassPtr);
            if (!eeClass.ValidateReadable())
            {
                return false;
            }
            TargetPointer methodTablePtrFromClass = eeClass.MethodTable;
            if (methodTable.Address == methodTablePtrFromClass)
            {
                return true;
            }
            if (methodTable.Flags.HasInstantiation || methodTable.Flags.IsArray || IsContinuation(methodTable))
            {
                NonValidatedMethodTable methodTableFromClass = GetMethodTableData(_target, methodTablePtrFromClass);
                if (!methodTableFromClass.ValidateReadable())
                {
                    return false;
                }
                TargetPointer classFromMethodTable = GetClassThrowing(methodTableFromClass);
                return classFromMethodTable == eeClassPtr;
            }
        }
        return false;
    }

    private bool ValidateMethodTableAdHoc(NonValidatedMethodTable methodTable)
    {
        // ad-hoc checks; add more here as needed
        if (!methodTable.Flags.IsInterface && !methodTable.Flags.IsString)
        {
            if (methodTable.Flags.BaseSize == 0 || !_target.IsAlignedToPointerSize(methodTable.Flags.BaseSize))
            {
                return false;
            }
        }
        return true;
    }

    private static bool ValidateDataReadable<T>(Target target, TargetPointer dataAddress) where T : IData<T>
    {
        try
        {
            T dataClass = T.Create(target, dataAddress);
            return true;
        }
        catch (VirtualReadException)
        {
            return false;
        }
    }

    private TargetPointer GetClassThrowing(NonValidatedMethodTable methodTable)
    {
        TargetPointer eeClassOrCanonMT = methodTable.EEClassOrCanonMT;

        if (MethodTableFlags_1.GetEEClassOrCanonMTBits(eeClassOrCanonMT) == MethodTableFlags_1.EEClassOrCanonMTBits.EEClass)
        {
            return methodTable.EEClass;
        }
        else
        {
            TargetPointer canonicalMethodTablePtr = methodTable.CanonMT;
            NonValidatedMethodTable umt = GetMethodTableData(_target, canonicalMethodTablePtr);
            if (!umt.ValidateReadable())
            {
                throw new InvalidOperationException("canon MT is not readable");
            }
            return umt.EEClass;
        }
    }

    // NOTE: The continuation check is duplicated here and in RuntimeTypeSystem_1.IsContinuation.
    // TypeValidation runs before the MethodTable is added to the RuntimeTypeSystem's cache, so we
    // cannot call into RuntimeTypeSystem_1 — the type handle does not exist yet. Instead we
    // duplicate the check using the raw ParentMethodTable read from target memory.
    private bool IsContinuation(NonValidatedMethodTable methodTable)
    {
        return _continuationMethodTablePointer != TargetPointer.Null
            && methodTable.ParentMethodTable == _continuationMethodTablePointer;
    }

    internal bool TryValidateMethodTablePointer(TargetPointer methodTablePointer)
    {
        NonValidatedMethodTable nonvalidatedMethodTable = GetMethodTableData(_target, methodTablePointer);

        return ValidateMethodTablePointer(nonvalidatedMethodTable);
    }
}
