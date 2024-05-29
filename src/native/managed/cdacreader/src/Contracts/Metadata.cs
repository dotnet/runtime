// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

// FIXME: Do we want some other name for this concept? "MethodTable" is CoreCLR specific (in mono this would be a MonoClass)
internal struct MethodTable
{
    internal Data.MethodTable? _data;
    internal bool _isFreeObjectMT;
    internal MethodTable(Data.MethodTable? data, bool isFreeObjectMT)
    {
        _data = data;
        _isFreeObjectMT = isFreeObjectMT;
    }
    public System.Reflection.Metadata.TypeDefinitionHandle Token => System.Reflection.Metadata.Ecma335.MetadataTokens.TypeDefinitionHandle((int)GetTypeDefRid()); // TokenFromRid(GetTypeDefRid(), mdtTypeDef);
    internal int GetTypeDefRid() => _data != null ? (int)(_data.DwFlags2 >> 8) : 0;

}

internal interface IMetadata : IContract
{
    static string IContract.Name => nameof(Metadata);
    static IContract IContract.Create(Target target, int version)
    {
        return version switch
        {
            1 => new Metadata_1(target),
            _ => default(Metadata),
        };
    }

    public virtual MethodTable GetMethodTableData(TargetPointer targetPointer) => throw new NotImplementedException();
}

internal struct Metadata : IMetadata
{
    // Everything throws NotImplementedException
}


internal struct Metadata_1 : IMetadata
{
    private readonly Target _target;

    internal Metadata_1(Target target)
    {
        _target = target;
    }

    private TargetPointer FreeObjectMethodTablePointer => throw new NotImplementedException();

    public MethodTable GetMethodTableData(TargetPointer methodTablePointer)
    {
        if (!ValidateMethodTablePointer(methodTablePointer, out bool isFreeObjectMT))
        {
            throw new ArgumentException("Invalid method table pointer");
        }

        Data.MethodTable? methodTableData;
        if (!_target.ProcessedData.TryGet(methodTablePointer, out methodTableData))
        {

            // Still okay if processed data is already registered by someone else.
            _ = _target.ProcessedData.TryRegister(methodTablePointer, methodTableData);
        }

        return new MethodTable(methodTableData, isFreeObjectMT);
    }


    private bool ValidateMethodTablePointer(TargetPointer methodTablePointer, out bool isFree)
    {
        isFree = false;
        // FIXME: is methodTablePointer properly sign-extended from 32-bit targets?
        if (methodTablePointer == TargetPointer.Null || methodTablePointer == TargetPointer.MinusOne)
        {
            return false;
        }
        try
        {
            if (methodTablePointer == FreeObjectMethodTablePointer)
            {
                isFree = true;
                return true;
            }
            else
            {
                return true; // pointer is valid
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool ValidateWithPossibleAV(ref readonly MethodTable methodTable)
    {
        //PTR_EEClass pEEClass = this->GetClassWithPossibleAV();
        //return ((pEEClass && (this == pEEClass->GetMethodTableWithPossibleAV())) ||
        //    ((HasInstantiation() || IsArray()) &&
        //    (pEEClass && (pEEClass->GetMethodTableWithPossibleAV()->GetClassWithPossibleAV() == pEEClass))));
        throw new NotImplementedException();
    }

    internal bool ValidateMethodTable(ref readonly MethodTable methodTable)
    {
        try
        {
            if (!ValidateWithPossibleAV(in methodTable))
            {
                return false;
            }
        }
        catch (Exception)
        {
            return false;
        }

        System.Reflection.Metadata.Handle tk;
        try
        {
            tk = methodTable.Token;
        }
        catch (ArgumentException)
        {
            return false;
        }
        if (!tk.IsNil && tk.Kind != System.Reflection.Metadata.HandleKind.TypeDefinition)
        {
            return false;
        }
        if (!methodTable.IsInterface && !methodTable.IsString)
        {
            if (methodTable.BaseSize == 0 || !_target.IsAlignedToPointerSize(methodTable.BaseSize))
            {
                return false;
            }
        }
        return true;
    }
}
