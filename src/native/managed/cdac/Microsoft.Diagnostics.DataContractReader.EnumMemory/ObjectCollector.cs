// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;

namespace Microsoft.Diagnostics.DataContractReader.EnumMemory;

internal sealed class ObjectCollector(
    Target target,
    MemoryRegionEmitter emitter,
    MethodCollector methods)
{
    private const ulong MaxObjectSize = 64 * 1024 * 1024;
    private const int MaxInnerExceptionCount = 256;
    private const int MaxTypeTraversalDepth = 1_024;

    private readonly Target _target = target;
    private readonly MemoryRegionEmitter _emitter = emitter;
    private readonly MethodCollector _methods = methods;
    private readonly Dictionary<TargetPointer, string> _names = [];
    private readonly Stack<TargetPointer> _pendingObjects = [];
    private readonly HashSet<TargetPointer> _visitedObjects = [];
    private int _remainingInnerExceptions;

    public IReadOnlyDictionary<TargetPointer, string> Names => _names;

    public void EnumerateObject(TargetPointer objectAddress)
    {
        _remainingInnerExceptions = MaxInnerExceptionCount;
        _pendingObjects.Push(objectAddress);
        try
        {
            while (_pendingObjects.TryPop(out TargetPointer pendingObject))
                EnumerateObjectCore(pendingObject);
        }
        finally
        {
            _pendingObjects.Clear();
        }
    }

    private void EnumerateObjectCore(TargetPointer objectAddress)
    {
        if (objectAddress == TargetPointer.Null || !_visitedObjects.Add(objectAddress))
        {
            return;
        }

        try
        {
            IObject objects = _target.Contracts.Object;
            IRuntimeTypeSystem types = _target.Contracts.RuntimeTypeSystem;
            ulong objectSize = objects.GetSize(objectAddress);
            if (objectSize == 0 || objectSize > MaxObjectSize)
                return;

            _emitter.Add(objectAddress.Value, objectSize);
            objects.GetSyncBlockAddress(objectAddress);

            TargetPointer methodTable = objects.GetMethodTableAddress(objectAddress);
            ITypeHandle type = types.GetTypeHandle(methodTable);
            bool isException = EnumerateTypeHierarchy(type);

            if (methodTable == types.GetWellKnownMethodTable(WellKnownMethodTable.String))
            {
                objects.GetStringValue(objectAddress);
            }
            else if (types.IsArray(type, out _))
            {
                EnumerateArray(objects, objectAddress, type);
            }

            if (_target.Contracts.FeatureFlags.IsEnabled(RuntimeFeature.COMInterop))
                objects.GetBuiltInComData(objectAddress, out _, out _, out _);

            if (isException)
                EnumerateExceptionData(objectAddress);

            CacheMethodTableName(methodTable);
        }
        catch (System.Exception ex) when (ex.HResult != HResults.COR_E_OPERATIONCANCELED)
        {
        }
    }

    private bool EnumerateTypeHierarchy(ITypeHandle type)
    {
        IRuntimeTypeSystem types = _target.Contracts.RuntimeTypeSystem;
        TargetPointer exceptionMethodTable =
            types.GetWellKnownMethodTable(WellKnownMethodTable.Exception);
        bool isException = false;

        for (int depth = 0; type.Address != TargetPointer.Null && depth < MaxTypeTraversalDepth; depth++)
        {
            EnumerateMethodTableData(type);
            isException |= type.Address == exceptionMethodTable;

            TargetPointer parentMethodTable = types.GetParentMethodTable(type);
            if (parentMethodTable == TargetPointer.Null)
                break;

            type = types.GetTypeHandle(parentMethodTable);
        }

        return isException;
    }

    private void EnumerateMethodTableData(ITypeHandle type)
    {
        IRuntimeTypeSystem types = _target.Contracts.RuntimeTypeSystem;
        types.GetBaseSize(type);
        types.GetComponentSize(type);
        if (types.IsFreeObjectMethodTable(type))
            return;

        types.GetModule(type);
        types.GetCanonicalMethodTable(type);
        types.GetParentMethodTable(type);
        types.GetNumInterfaces(type);
        types.GetNumMethods(type);
        types.GetTypeDefToken(type);
        types.GetTypeDefTypeAttributes(type);
        types.ContainsGCPointers(type);
        types.IsDynamicStatics(type);
    }

    private void EnumerateArray(IObject objects, TargetPointer objectAddress, ITypeHandle type)
    {
        IRuntimeTypeSystem types = _target.Contracts.RuntimeTypeSystem;
        objects.GetArrayData(objectAddress, out _, out _, out _, out _, out _);
        ITypeHandle elementType = types.GetTypeParam(type);
        types.GetSignatureCorElementType(elementType);
        for (int depth = 0; depth < MaxTypeTraversalDepth && types.IsArray(elementType, out _); depth++)
            elementType = types.GetTypeParam(elementType);
    }

    private void EnumerateExceptionData(TargetPointer exceptionObject)
    {
        IException exceptions = _target.Contracts.Exception;
        ExceptionData data = exceptions.GetExceptionData(exceptionObject);
        if (data.InnerException != TargetPointer.Null && _remainingInnerExceptions > 0)
        {
            _remainingInnerExceptions--;
            _pendingObjects.Push(data.InnerException);
        }
        _pendingObjects.Push(data.RemoteStackTraceString);
        _pendingObjects.Push(data.StackTraceString);
        _pendingObjects.Push(data.WatsonBuckets);
        _pendingObjects.Push(data.StackTrace);
        _pendingObjects.Push(data.Message);

        foreach (ExceptionStackFrameInfo frame in exceptions.GetExceptionStackFrames(exceptionObject))
            _methods.CaptureMethod(frame.MethodDesc);
    }

    private void CacheMethodTableName(TargetPointer methodTable)
    {
        if (methodTable == TargetPointer.Null || _names.ContainsKey(methodTable))
            return;

        try
        {
            IRuntimeTypeSystem types = _target.Contracts.RuntimeTypeSystem;
            ITypeHandle type = types.GetTypeHandle(methodTable);
            if (types.IsFreeObjectMethodTable(type))
                return;

            StringBuilder name = new();
            TypeNameBuilder.AppendType(
                _target,
                name,
                type,
                TypeNameFormat.FormatNamespace | TypeNameFormat.FormatFullInst);
            if (name.Length != 0)
                _names.Add(methodTable, name.ToString());
        }
        catch (System.Exception ex) when (ex.HResult != HResults.COR_E_OPERATIONCANCELED)
        {
        }
    }
}
