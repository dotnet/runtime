// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataModule : ICustomQueryInterface, IXCLRDataModule, IXCLRDataModule2
{
    private readonly TargetPointer _address;
    private readonly Target _target;

    internal TargetPointer Address => _address;

    private bool _extentsSet;
    private CLRDataModuleExtent[] _extents = new CLRDataModuleExtent[2];

#if DEBUG
    private ulong legacyExtentHandle;
#endif


    private readonly IXCLRDataModule? _legacyModule;
    private readonly IXCLRDataModule2? _legacyModule2;

    // This is an IUnknown pointer for the legacy implementation
    private readonly nint _legacyModulePointer;

    private MetaDataImportImpl? _metaDataImportImpl;

    public ClrDataModule(TargetPointer address, Target target, IXCLRDataModule? legacyImpl)
    {
        _address = address;
        _target = target;
        _legacyModule = legacyImpl;
        _legacyModule2 = legacyImpl as IXCLRDataModule2;
        if (legacyImpl is not null && System.Runtime.InteropServices.ComWrappers.TryGetComInstance(legacyImpl, out _legacyModulePointer))
        {
            // Release the AddRef from TryGetComInstance. We rely on the ref-count from holding on to IXCLRDataModule.
            Marshal.Release(_legacyModulePointer);
        }
    }

    private const uint CORDEBUG_JIT_DEFAULT = 0x1;
    private const uint CORDEBUG_JIT_DISABLE_OPTIMIZATION = 0x3;

    CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out nint ppv)
    {
        ppv = default;

        // Legacy DAC implementation of IXCLRDataModule handles QIs for IMetaDataImport by creating and
        // passing out an implementation of IMetaDataImport. Note that it does not do COM aggregation.
        // It simply returns a completely separate object. See ClrDataModule::QueryInterface in task.cpp
        // The returned MetaDataImportImpl also implements IMetaDataImport2 and IMetaDataAssemblyImport,
        // so consumers can QI the returned object for those interfaces as well.
        //
        // IMPORTANT: Some consumers (e.g. ClrMD) QI for IMetaDataImport but then access IMetaDataImport2
        // vtable slots beyond the IMetaDataImport vtable boundary. This works with native C++ COM objects
        // (where the vtable for IMetaDataImport and IMetaDataImport2 is unified) but breaks with managed
        // [GeneratedComInterface] CCWs which create separate vtables per interface. To handle this, we
        // always return the IMetaDataImport2 vtable pointer when asked for IMetaDataImport. Since
        // IMetaDataImport2 inherits from IMetaDataImport, the first slots are identical.
        if (iid == typeof(IMetaDataImport).GUID)
        {
            MetaDataImportImpl? wrapper = _metaDataImportImpl;
            if (wrapper is null)
            {
                MetadataReader? reader = null;
                IMetaDataImport? legacyImport = null;

                try
                {
                    ILoader loader = _target.Contracts.Loader;
                    Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(_address);
                    reader = _target.Contracts.EcmaMetadata.GetMetadata(moduleHandle);
                }
                catch
                {
                }

                try
                {
                    Guid iidMetaDataImport = typeof(IMetaDataImport).GUID;
                    if (_legacyModulePointer != 0 && Marshal.QueryInterface(_legacyModulePointer, iidMetaDataImport, out nint ppMdi) >= 0)
                    {
                        legacyImport = ComInterfaceMarshaller<IMetaDataImport>.ConvertToManaged((void*)ppMdi);
                        Marshal.Release(ppMdi);
                    }
                }
                catch
                {
                }

                if (reader is null)
                    return CustomQueryInterfaceResult.NotHandled;

                wrapper = new MetaDataImportImpl(reader, legacyImport);
                _metaDataImportImpl ??= wrapper;
                wrapper = _metaDataImportImpl;
            }

            nint pUnk = (nint)ComInterfaceMarshaller<IMetaDataImport2>.ConvertToUnmanaged(wrapper);

            // ConvertToUnmanaged returns a COM pointer for IMetaDataImport2.
            // We return this directly as ppv so that consumers (e.g. ClrMD) that QI for
            // IMetaDataImport but access IMetaDataImport2 vtable slots get the full vtable.
            ppv = pUnk;
            return CustomQueryInterfaceResult.Handled;
        }

        return CustomQueryInterfaceResult.NotHandled;
    }

    internal sealed class EnumMethodDefinitions : IEnum<uint>
    {
        private readonly uint _flags;
        private readonly MetadataReader _reader;
        private TypeDefinitionHandle? _typeHandle;
        private string? _methodName;
        public IEnumerator<uint> Enumerator { get; set; } = Enumerable.Empty<uint>().GetEnumerator();
        public TargetPointer LegacyHandle { get; set; } = TargetPointer.Null;

        public EnumMethodDefinitions(MetadataReader reader, uint flags, TargetPointer legacyHandle)
        {
            _reader = reader;
            _flags = flags;
            LegacyHandle = legacyHandle;
        }

        public void Start(string fullName)
        {
            // start: find the type.
            int parenIndex = fullName.IndexOf('(');
            if (parenIndex >= 0)
                fullName = fullName[..parenIndex];

            int lastNestingSep = Math.Max(fullName.LastIndexOf('+'), fullName.LastIndexOf('/'));
            int searchFrom = fullName.Length - 1;

            while (searchFrom > 0)
            {
                int dotPos = fullName.LastIndexOf('.', searchFrom);
                if (dotPos <= 0)
                    break;

                while (dotPos > 0 && fullName[dotPos - 1] == '.')
                    dotPos--;

                if (dotPos <= lastNestingSep || dotPos <= 0)
                    break;

                string typePortion = fullName[..dotPos];
                string methodName = fullName[Math.Min(dotPos + 1, fullName.Length - 1)..];

                _typeHandle = ResolveType(_reader, typePortion);
                if (_typeHandle != null)
                {
                    _methodName = methodName;
                    break;
                }

                searchFrom = dotPos - 1;
            }

            if (_typeHandle == null)
                throw new ArgumentException();
            Enumerator = IterateMethodDefinitions().GetEnumerator();
        }

        private bool StringEquals(string a, string b)
        {
            StringComparison comparison = (_flags & (uint)CLRDataByNameFlag.CLRDATA_BYNAME_CASE_INSENSITIVE) != 0 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return string.Equals(a, b, comparison);
        }

        private IEnumerable<uint> IterateMethodDefinitions()
        {
            foreach (MethodDefinitionHandle mh in _reader.GetTypeDefinition(_typeHandle!.Value).GetMethods())
            {
                if (StringEquals(_reader.GetString(_reader.GetMethodDefinition(mh).Name), _methodName!))
                    yield return (uint)MetadataTokens.GetToken(mh);
            }
            yield break;
        }

        private TypeDefinitionHandle? ResolveType(MetadataReader reader, string typeFullName)
        {
            string[] nestingParts = typeFullName.Split('+', '/');

            string firstPart = nestingParts[0];
            int lastDot = firstPart.LastIndexOf('.');

            string ns = lastDot >= 0 ? firstPart[..lastDot] : "";
            string outerName = lastDot >= 0 ? firstPart[(lastDot + 1)..] : firstPart;

            TypeDefinitionHandle? current = FindTopLevelType(reader, ns, outerName);

            for (int i = 1; i < nestingParts.Length && current != null; i++)
                current = FindNestedType(reader, current.Value, nestingParts[i]);

            return current;
        }

        private TypeDefinitionHandle? FindTopLevelType(MetadataReader reader, string @namespace, string name)
        {
            foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
            {
                TypeDefinition td = reader.GetTypeDefinition(handle);
                if (td.IsNested)
                    continue;

                if ((string.IsNullOrEmpty(@namespace) || StringEquals(reader.GetString(td.Namespace), @namespace)) &&
                    StringEquals(reader.GetString(td.Name), name))
                    return handle;
            }
            return null;
        }

        private TypeDefinitionHandle? FindNestedType(MetadataReader reader, TypeDefinitionHandle enclosing, string name)
        {
            foreach (TypeDefinitionHandle nh in reader.GetTypeDefinition(enclosing).GetNestedTypes())
            {
                if (StringEquals(reader.GetString(reader.GetTypeDefinition(nh).Name), name))
                    return nh;
            }
            return null;
        }
    }

    int IXCLRDataModule.StartEnumAssemblies(ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.StartEnumAssemblies(handle) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EnumAssembly(ulong* handle, DacComNullableByRef<IXCLRDataAssembly> assembly)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.EnumAssembly(handle, assembly) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EndEnumAssemblies(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.EndEnumAssemblies(handle) : HResults.E_NOTIMPL;

    int IXCLRDataModule.StartEnumTypeDefinitions(ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.StartEnumTypeDefinitions(handle) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EnumTypeDefinition(ulong* handle, DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinition)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.EnumTypeDefinition(handle, typeDefinition) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EndEnumTypeDefinitions(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.EndEnumTypeDefinitions(handle) : HResults.E_NOTIMPL;

    int IXCLRDataModule.StartEnumTypeInstances(IXCLRDataAppDomain? appDomain, ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.StartEnumTypeInstances(appDomain, handle) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EnumTypeInstance(ulong* handle, DacComNullableByRef<IXCLRDataTypeInstance> typeInstance)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.EnumTypeInstance(handle, typeInstance) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EndEnumTypeInstances(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.EndEnumTypeInstances(handle) : HResults.E_NOTIMPL;

    int IXCLRDataModule.StartEnumTypeDefinitionsByName(char* name, uint flags, ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.StartEnumTypeDefinitionsByName(name, flags, handle) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EnumTypeDefinitionByName(ulong* handle, DacComNullableByRef<IXCLRDataTypeDefinition> type)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.EnumTypeDefinitionByName(handle, type) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EndEnumTypeDefinitionsByName(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.EndEnumTypeDefinitionsByName(handle) : HResults.E_NOTIMPL;

    int IXCLRDataModule.StartEnumTypeInstancesByName(char* name, uint flags, IXCLRDataAppDomain? appDomain, ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.StartEnumTypeInstancesByName(name, flags, appDomain, handle) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EnumTypeInstanceByName(ulong* handle, DacComNullableByRef<IXCLRDataTypeInstance> type)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.EnumTypeInstanceByName(handle, type) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EndEnumTypeInstancesByName(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.EndEnumTypeInstancesByName(handle) : HResults.E_NOTIMPL;

    int IXCLRDataModule.GetTypeDefinitionByToken(/*mdTypeDef*/ uint token, DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinition)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.GetTypeDefinitionByToken(token, typeDefinition) : HResults.E_NOTIMPL;

    int IXCLRDataModule.StartEnumMethodDefinitionsByName(char* name, uint flags, ulong* handle)
    {
        int hr = HResults.S_OK;
        *handle = 0;

        // Start the legacy enumeration to keep it in sync with the cDAC enumeration.
        ulong handleLocal = default;
        int hrLocal = default;
        try
        {
            if (_legacyModule is not null)
            {
                hrLocal = _legacyModule.StartEnumMethodDefinitionsByName(name, flags, &handleLocal);
            }
            if (name == null || *name == '\0')
                throw new ArgumentException();
            if ((flags & ~((uint)CLRDataByNameFlag.CLRDATA_BYNAME_CASE_SENSITIVE | (uint)CLRDataByNameFlag.CLRDATA_BYNAME_CASE_INSENSITIVE)) != 0)
                throw new ArgumentException();
            string fullName = new string(name);

            // start: find the type.
            ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(_address);
            MetadataReader reader = _target.Contracts.EcmaMetadata.GetMetadata(moduleHandle)!;

            EnumMethodDefinitions emd = new(reader, flags, handleLocal);
            emd.Start(fullName);
            *handle = (ulong)((IEnum<uint>)emd).GetHandle();
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyModule is not null)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif
        return hr;
    }

    int IXCLRDataModule.EnumMethodDefinitionByName(ulong* handle, DacComNullableByRef<IXCLRDataMethodDefinition> method)
    {
        int hr = HResults.S_OK;
        EnumMethodDefinitions emd;
        try
        {
            if (method.IsNullRef)
                throw new NullReferenceException();
            GCHandle gcHandle = GCHandle.FromIntPtr((IntPtr)(*handle));
            if (gcHandle.Target is not EnumMethodDefinitions emdLocal)
                throw new ArgumentException();

            emd = emdLocal;
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }

        // Advance the legacy enumeration to keep it in sync with the cDAC enumeration.
        IXCLRDataMethodDefinition? legacyMethod = null;
        int hrLocal = HResults.S_OK;
        if (_legacyModule is not null)
        {
            ulong legacyHandle = emd.LegacyHandle;
            DacComNullableByRef<IXCLRDataMethodDefinition> legacyMethodOut = new(isNullRef: false);
            hrLocal = _legacyModule.EnumMethodDefinitionByName(&legacyHandle, legacyMethodOut);
            legacyMethod = legacyMethodOut.Interface;
            emd.LegacyHandle = legacyHandle;
        }

        try
        {
            if (emd.Enumerator.MoveNext())
            {
                uint token = emd.Enumerator.Current;
                method.Interface = new ClrDataMethodDefinition(_target, _address, token, legacyMethod);
            }
            else
            {
                hr = HResults.S_FALSE;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyModule is not null)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif

        return hr;
    }
    int IXCLRDataModule.EndEnumMethodDefinitionsByName(ulong handle)
    {
        int hr = HResults.S_OK;
        EnumMethodDefinitions emd;
        try
        {
            GCHandle gcHandle = GCHandle.FromIntPtr((IntPtr)handle);
            if (gcHandle.Target is not EnumMethodDefinitions emdLocal)
                throw new ArgumentException();
            emd = emdLocal;
            ((IEnum<uint>)emd).Dispose();
            gcHandle.Free();
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }

        if (_legacyModule != null && emd.LegacyHandle != TargetPointer.Null)
        {
            int hrLocal = _legacyModule.EndEnumMethodDefinitionsByName(emd.LegacyHandle);
            if (hrLocal < 0)
                return hrLocal;
        }

        return hr;
    }
    int IXCLRDataModule.StartEnumMethodInstancesByName(char* name, uint flags, IXCLRDataAppDomain? appDomain, ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.StartEnumMethodInstancesByName(name, flags, appDomain, handle) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EnumMethodInstanceByName(ulong* handle, DacComNullableByRef<IXCLRDataMethodInstance> method)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.EnumMethodInstanceByName(handle, method) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EndEnumMethodInstancesByName(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.EndEnumMethodInstancesByName(handle) : HResults.E_NOTIMPL;

    int IXCLRDataModule.GetMethodDefinitionByToken(/*mdMethodDef*/ uint token, DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinition)
    {
        int hr = HResults.S_OK;
        int hrLocal = HResults.S_OK;
        IXCLRDataMethodDefinition? legacyMethod = null;
        try
        {
            if (_legacyModule is not null)
            {
                DacComNullableByRef<IXCLRDataMethodDefinition> legacyMethodOut = new(isNullRef: false);
                hrLocal = _legacyModule.GetMethodDefinitionByToken(token, legacyMethodOut);
                legacyMethod = legacyMethodOut.Interface;
            }

            if ((EcmaMetadataUtils.TokenType)(token & EcmaMetadataUtils.TokenTypeMask) != EcmaMetadataUtils.TokenType.mdtMethodDef)
                throw new ArgumentException();

            methodDefinition.Interface = new ClrDataMethodDefinition(_target, _address, token, legacyMethod);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyModule is not null)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif

        return hr;
    }

    int IXCLRDataModule.StartEnumDataByName(char* name, uint flags, IXCLRDataAppDomain? appDomain, IXCLRDataTask? tlsTask, ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.StartEnumDataByName(name, flags, appDomain, tlsTask, handle) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EnumDataByName(ulong* handle, DacComNullableByRef<IXCLRDataValue> value)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.EnumDataByName(handle, value) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EndEnumDataByName(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.EndEnumDataByName(handle) : HResults.E_NOTIMPL;

    int IXCLRDataModule.GetName(uint bufLen, uint* nameLen, char* name)
    {
        int hr = HResults.S_OK;
        int E_INSUFFICIENT_BUFFER = unchecked((int)0x8007007A);
        try
        {
            if (nameLen != null)
                *nameLen = 0;
            Contracts.ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = loader.GetModuleHandleFromModulePtr(_address);
            if (!loader.TryGetSimpleName(handle, out string result))
                throw new ArgumentException("Module does not have a simple name");

            uint nameLenLocal = 0;
            OutputBufferHelpers.CopyStringToBuffer(name, bufLen, &nameLenLocal, result);
            if (nameLen != null)
                *nameLen = nameLenLocal;
            // throw on insufficient buffer
            if (nameLenLocal > bufLen)
                throw Marshal.GetExceptionForHR(E_INSUFFICIENT_BUFFER)!;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyModule is not null)
        {
            char[] nameLocal = new char[bufLen];
            uint nameLenLocal;
            int hrLocal;
            fixed (char* ptr = nameLocal)
            {
                hrLocal = _legacyModule.GetName(bufLen, &nameLenLocal, ptr);
            }
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(nameLen == null || *nameLen == nameLenLocal);
                Debug.Assert(name == null || new ReadOnlySpan<char>(nameLocal, 0, (int)nameLenLocal - 1).SequenceEqual(new string(name)));
            }
        }
#endif
        return hr;
    }
    int IXCLRDataModule.GetFileName(uint bufLen, uint* nameLen, char* name)
    {
        try
        {
            Contracts.ILoader contract = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(_address);
            string result = string.Empty;
            try
            {
                result = contract.GetPath(handle);
            }
            catch (VirtualReadException)
            {
                // The memory for the path may not be enumerated - for example, in triage dumps
                // In this case, GetPath will throw VirtualReadException
            }

            if (string.IsNullOrEmpty(result))
            {
                result = contract.GetFileName(handle);
            }

            if (string.IsNullOrEmpty(result))
                return HResults.E_FAIL;

            OutputBufferHelpers.CopyStringToBuffer(name, bufLen, nameLen, result);
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }

#if DEBUG
        if (_legacyModule is not null)
        {
            char[] nameLocal = new char[bufLen];
            uint nameLenLocal;
            int hrLocal;
            fixed (char* ptr = nameLocal)
            {
                hrLocal = _legacyModule.GetFileName(bufLen, &nameLenLocal, ptr);
            }
            Debug.Assert(hrLocal == HResults.S_OK);
            Debug.Assert(nameLen == null || *nameLen == nameLenLocal);
            Debug.Assert(name == null || new ReadOnlySpan<char>(nameLocal, 0, (int)nameLenLocal - 1).SequenceEqual(new string(name)));
        }
#endif
        return HResults.S_OK;
    }

    int IXCLRDataModule.GetFlags(uint* flags)
    {
        *flags = 0;
        try
        {
            Contracts.ILoader contract = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(_address);

            ModuleFlags moduleFlags = contract.GetFlags(handle);
            if ((moduleFlags & ModuleFlags.ReflectionEmit) != 0)
            {
                *flags |= 0x1; // CLRDATA_MODULE_IS_DYNAMIC
            }

            if (contract.GetAssembly(handle) == contract.GetRootAssembly())
            {
                *flags |= 0x4; // CLRDATA_MODULE_FLAGS_ROOT_ASSEMBLY
            }
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }

#if DEBUG
        if (_legacyModule is not null)
        {
            uint flagsLocal;
            int hrLocal = _legacyModule.GetFlags(&flagsLocal);
            Debug.Assert(hrLocal == HResults.S_OK, $"cDAC: {HResults.S_OK}, DAC: {hrLocal}");
            Debug.Assert(flagsLocal == *flags, $"cDAC: {*flags}, DAC: {flagsLocal}");
        }
#endif

        return HResults.S_OK;
    }

    int IXCLRDataModule.IsSameObject(IXCLRDataModule* mod)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.IsSameObject(mod) : HResults.E_NOTIMPL;

    int IXCLRDataModule.StartEnumExtents(ulong* handle)
    {
        int hr = HResults.S_OK;
        try
        {
            if (!_extentsSet)
            {
                Contracts.ILoader contract = _target.Contracts.Loader;
                Contracts.ModuleHandle moduleHandle = contract.GetModuleHandleFromModulePtr(_address);

                TargetPointer peAssembly = contract.GetPEAssembly(moduleHandle);
                if (peAssembly == 0)
                {
                    *handle = 0;
                    hr = HResults.E_INVALIDARG;
                }
                else
                {
                    if (contract.TryGetLoadedImageContents(moduleHandle, out TargetPointer baseAddress, out uint size, out _))
                    {
                        _extents[0].baseAddress = baseAddress.ToClrDataAddress(_target);
                        _extents[0].length = size;
                        _extents[0].type = 0x0; // CLRDATA_MODULE_PE_FILE
                    }
                    _extentsSet = true;
                }
            }

            *handle = 1;
            hr = _extents[0].baseAddress != 0 ? HResults.S_OK : HResults.S_FALSE;
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }

#if DEBUG
        if (_legacyModule is not null)
        {
            ulong handleLocal = 0;
            int hrLocal = _legacyModule.StartEnumExtents(&handleLocal);
            legacyExtentHandle = handleLocal;
            Debug.Assert(hrLocal == HResults.S_OK, $"cDAC: {HResults.S_OK}, DAC: {hrLocal}");
        }
#endif

        return hr;
    }
    int IXCLRDataModule.EnumExtent(ulong* handle, /*CLRDATA_MODULE_EXTENT*/ void* extent)
    {
        int hr = HResults.S_OK;
        try
        {
            Contracts.ILoader contract = _target.Contracts.Loader;
            Contracts.ModuleHandle moduleHandle = contract.GetModuleHandleFromModulePtr(_address);

            if (!_extentsSet)
            {
                hr = HResults.E_INVALIDARG;
            }
            else if (*handle == 1)
            {
                *handle += 1;
                CLRDataModuleExtent* dataModuleExtent = (CLRDataModuleExtent*)extent;
                dataModuleExtent->baseAddress = _extents[0].baseAddress;
                dataModuleExtent->length = _extents[0].length;
                dataModuleExtent->type = _extents[0].type;
            }
            else
            {
                hr = HResults.S_FALSE;
            }
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }

#if DEBUG
        if (_legacyModule is not null)
        {
            ulong handleLocal = legacyExtentHandle;
            CLRDataModuleExtent dataModuleExtentLocal = default;
            int hrLocal = _legacyModule.EnumExtent(&handleLocal, &dataModuleExtentLocal);
            legacyExtentHandle = handleLocal;
            Debug.Assert(hr == hrLocal, $"cDAC: {hr}, DAC: {hrLocal}");
            if (hr == HResults.S_OK)
            {
                CLRDataModuleExtent* dataModuleExtent = (CLRDataModuleExtent*)extent;
                Debug.Assert(dataModuleExtent->baseAddress == dataModuleExtentLocal.baseAddress, $"cDAC: {dataModuleExtent->baseAddress}, DAC: {dataModuleExtentLocal.baseAddress}");
                Debug.Assert(dataModuleExtent->length == dataModuleExtentLocal.length, $"cDAC: {dataModuleExtent->length}, DAC: {dataModuleExtentLocal.length}");
                Debug.Assert(dataModuleExtent->type == dataModuleExtentLocal.type, $"cDAC: {dataModuleExtent->type}, DAC: {dataModuleExtentLocal.type}");
            }
        }
#endif

        return hr;
    }
    int IXCLRDataModule.EndEnumExtents(ulong handle)
    {
#if DEBUG
        if (_legacyModule is not null)
        {
            int hrLocal = _legacyModule.EndEnumExtents(handle);
            Debug.Assert(hrLocal == HResults.S_OK, $"cDAC: {HResults.S_OK}, DAC: {hrLocal}");
        }
#endif

        return HResults.S_OK;
    }

    int IXCLRDataModule.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
        int hr = HResults.S_OK;
        try
        {
            if (inBufferSize != 0 || inBuffer is not null || outBuffer is null)
                throw new ArgumentException();

            switch (reqCode)
            {
                case (uint)CLRDataGeneralRequest.CLRDATA_REQUEST_REVISION:
                    if (outBufferSize != sizeof(uint))
                        throw new ArgumentException();
                    *(uint*)outBuffer = 3;
                    break;

                case 0xf0000000 /*DACDATAMODULEPRIV_REQUEST_GET_MODULEPTR*/:
                    if (outBufferSize != sizeof(DacpGetModuleAddress))
                        throw new ArgumentException();
                    ((DacpGetModuleAddress*)outBuffer)->ModulePtr = _address.ToClrDataAddress(_target);
                    break;

                case 0xf0000001 /*DACDATAMODULEPRIV_REQUEST_GET_MODULEDATA*/:
                    if (outBufferSize != sizeof(DacpGetModuleData))
                        throw new ArgumentException();
                    PopulateModuleData((DacpGetModuleData*)outBuffer);
                    break;

                default:
                    throw new ArgumentException();
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyModule is not null)
        {
            byte[] localBuffer = new byte[(int)outBufferSize];
            fixed (byte* localOutBuffer = localBuffer)
            {
                int hrLocal = _legacyModule.Request(reqCode, inBufferSize, inBuffer, outBufferSize, localOutBuffer);
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK)
                    Debug.Assert(new ReadOnlySpan<byte>(outBuffer, (int)outBufferSize).SequenceEqual(localBuffer));
            }
        }
#endif

        return hr;
    }

    private void PopulateModuleData(DacpGetModuleData* getModuleData)
    {
        *getModuleData = default;

        Contracts.ILoader contract = _target.Contracts.Loader;
        Contracts.ModuleHandle moduleHandle = contract.GetModuleHandleFromModulePtr(_address);
        TargetPointer peAssembly = contract.GetPEAssembly(moduleHandle);

        bool isReflectionEmit = (contract.GetFlags(moduleHandle) & ModuleFlags.ReflectionEmit) != 0;

        getModuleData->PEAssembly = _address.ToClrDataAddress(_target);
        getModuleData->IsDynamic = isReflectionEmit ? 1u : 0u;

        if (peAssembly != TargetPointer.Null)
        {
            // If isReflectionEmit or ProbeExtension is valid, the path is not valid.
            if (isReflectionEmit || contract.IsProbeExtensionResultValid(moduleHandle))
            {
                getModuleData->IsInMemory = 1u;
            }
            else
            {
                getModuleData->IsInMemory = contract.GetPath(moduleHandle).Length == 0 ? 1u : 0u;
            }

            contract.TryGetLoadedImageContents(moduleHandle, out TargetPointer baseAddress, out uint size, out uint flags);
            getModuleData->LoadedPEAddress = baseAddress.ToClrDataAddress(_target);
            getModuleData->LoadedPESize = size;

            // Can not get the assembly layout for a dynamic module
            if (getModuleData->IsDynamic == 0u)
            {
                getModuleData->IsFileLayout = ((flags & /*FLAG_CONTENTS*/0x2) != 0) && !((flags & /*FLAG_MAPPED*/0x1) != 0) ? 1u : 0u; // HasContents && !IsMapped
            }
        }

        if (contract.TryGetSymbolStream(moduleHandle, out TargetPointer symbolBuffer, out uint symbolBufferSize))
        {
            getModuleData->InMemoryPdbAddress = symbolBuffer.ToClrDataAddress(_target);
            getModuleData->InMemoryPdbSize = symbolBufferSize;
        }
    }

    int IXCLRDataModule.StartEnumAppDomains(ulong* handle)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.StartEnumAppDomains(handle) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EnumAppDomain(ulong* handle, /*IXCLRDataAppDomain*/ void** appDomain)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.EnumAppDomain(handle, appDomain) : HResults.E_NOTIMPL;
    int IXCLRDataModule.EndEnumAppDomains(ulong handle)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.EndEnumAppDomains(handle) : HResults.E_NOTIMPL;

    int IXCLRDataModule.GetVersionId(Guid* vid)
        => LegacyFallbackHelper.CanFallback() && _legacyModule is not null ? _legacyModule.GetVersionId(vid) : HResults.E_NOTIMPL;

    int IXCLRDataModule2.SetJITCompilerFlags(uint flags)
    {
        int hr = HResults.S_OK;
        try
        {
            if ((flags != CORDEBUG_JIT_DEFAULT) && (flags != CORDEBUG_JIT_DISABLE_OPTIMIZATION))
                throw new ArgumentException();

            Contracts.ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = loader.GetModuleHandleFromModulePtr(_address);

            bool allowJitOpts = (flags & CORDEBUG_JIT_DISABLE_OPTIMIZATION) != CORDEBUG_JIT_DISABLE_OPTIMIZATION;
            DebuggerAssemblyControlFlags bits = loader.GetDebuggerInfoBits(handle)
                & ~(DebuggerAssemblyControlFlags.DACF_ALLOW_JIT_OPTS | DebuggerAssemblyControlFlags.DACF_ENC_ENABLED);
            bits &= DebuggerAssemblyControlFlags.DACF_CONTROL_FLAGS_MASK;

            if (allowJitOpts)
            {
                bits |= DebuggerAssemblyControlFlags.DACF_ALLOW_JIT_OPTS;
            }

            loader.SetDebuggerInfoBits(handle, bits);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyModule2 is not null)
        {
            int hrLocal = _legacyModule2.SetJITCompilerFlags(flags);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif

        return hr;
    }
}
