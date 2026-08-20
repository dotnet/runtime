// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;

namespace Microsoft.Diagnostics.DataContractReader.DumpCollect;

internal sealed class ObjectCollector(
    Target target,
    MemoryRegionEmitter emitter,
    MethodCollector methods)
{
    private const ulong MaxObjectSize = 64 * 1024 * 1024;

    private readonly Target _target = target;
    private readonly MemoryRegionEmitter _emitter = emitter;
    private readonly MethodCollector _methods = methods;
    private readonly Dictionary<TargetPointer, string> _names = [];
    private readonly HashSet<TargetPointer> _visitedObjects = [];

    public IReadOnlyDictionary<TargetPointer, string> Names => _names;

    public void EnumerateObject(TargetPointer objectAddress)
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
        catch (System.Exception ex)
        {
            DumpCollectLogger.LogException($"object 0x{objectAddress.Value:x} enumeration", ex);
        }
    }

    private bool EnumerateTypeHierarchy(ITypeHandle type)
    {
        IRuntimeTypeSystem types = _target.Contracts.RuntimeTypeSystem;
        TargetPointer exceptionMethodTable =
            types.GetWellKnownMethodTable(WellKnownMethodTable.Exception);
        bool isException = false;

        while (type.Address != TargetPointer.Null)
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
        while (types.IsArray(elementType, out _))
            elementType = types.GetTypeParam(elementType);
    }

    private void EnumerateExceptionData(TargetPointer exceptionObject)
    {
        IException exceptions = _target.Contracts.Exception;
        ExceptionData data = exceptions.GetExceptionData(exceptionObject);
        EnumerateObject(data.Message);
        EnumerateObject(data.StackTrace);
        EnumerateObject(data.WatsonBuckets);
        EnumerateObject(data.StackTraceString);
        EnumerateObject(data.RemoteStackTraceString);
        EnumerateObject(data.InnerException);

        foreach (ExceptionStackFrameInfo frame in exceptions.GetExceptionStackFrames(exceptionObject))
            _methods.CaptureMethod(frame.MethodDesc);
    }

    private void CacheMethodTableName(TargetPointer methodTable)
    {
        if (methodTable == TargetPointer.Null || _names.ContainsKey(methodTable))
            return;

        try
        {
            using (_emitter.SuppressTargetReadEmission())
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
        }
        catch (System.Exception ex)
        {
            DumpCollectLogger.LogException(
                $"method table name 0x{methodTable.Value:x} collection",
                ex);
        }
    }
}
