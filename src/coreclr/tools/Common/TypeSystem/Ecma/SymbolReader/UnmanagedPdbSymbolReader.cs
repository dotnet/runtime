// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.CompilerServices;

#if !DISABLE_UNMANAGED_PDB_SYMBOLS
using Microsoft.DiaSymReader;
#endif

using Internal.IL;

namespace Internal.TypeSystem.Ecma
{
#if DISABLE_UNMANAGED_PDB_SYMBOLS
    /// <summary>
    ///  Provides PdbSymbolReader via unmanaged SymBinder from .NET Framework
    /// </summary>
    public abstract class UnmanagedPdbSymbolReader : PdbSymbolReader
    {
        public static PdbSymbolReader? TryOpenSymbolReaderForMetadataFile(string metadataFileName, string searchPath)
        {
            return null;
        }
    }
#else
    /// <summary>
    ///  Provides PdbSymbolReader via unmanaged SymBinder from .NET Framework
    /// </summary>
    public sealed class UnmanagedPdbSymbolReader : PdbSymbolReader
    {
        private static int CLRCreateInstance(ref Guid clsid, ref Guid riid, out ClrMetaHostWrapperCache.ClrMetaHostRcw? ppInterface)
        {
            int hr = CLRCreateInstance(ref clsid, ref riid, out IntPtr ptr);
            ppInterface = hr == 0
                ? (ClrMetaHostWrapperCache.ClrMetaHostRcw)ClrMetaHostWrapperCache.Instance.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.UniqueInstance)
                : null;
            return hr;

            [DllImport("mscoree.dll")]
            static extern int CLRCreateInstance(ref Guid clsid, ref Guid riid, out IntPtr ptr);
        }

        interface ICLRMetaHost
        {
            public static readonly Guid IID = new Guid("d332db9e-b9b3-4125-8207-a14884f53216");

            [PreserveSig]
            int GetRuntime(string pwzVersion, ref Guid riid, out CLRRuntimeInfoWrapperCache.ClrRuntimeInfoRcw? ppRuntime);

            // Don't need any other methods.
        }

        private sealed class ClrMetaHostWrapperCache : ComWrappers
        {
            public static readonly ClrMetaHostWrapperCache Instance = new ClrMetaHostWrapperCache();
            private ClrMetaHostWrapperCache() { }

            protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count) => throw new NotImplementedException();
            protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
            {
                Debug.Assert(flags == CreateObjectFlags.UniqueInstance);

                var iid = ICLRMetaHost.IID;
                if (Marshal.QueryInterface(externalComObject, ref iid, out IntPtr hostPtr) != 0)
                {
                    throw new ArgumentException("Expected ICLRMetaHost COM interface");
                }
                return new ClrMetaHostRcw(hostPtr);
            }

            protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();

            public unsafe class ClrMetaHostRcw : ICLRMetaHost, IDisposable
            {
                private bool _disposed;
                private readonly IntPtr _inst;

                public ClrMetaHostRcw(IntPtr inst)
                {
                    _inst = inst;
                }
                public int GetRuntime(string pwzVersion, ref Guid riid, out CLRRuntimeInfoWrapperCache.ClrRuntimeInfoRcw? ppRuntime)
                {
                    // ICLRMetaHost::GetRuntime() is 4th slot (0-based)
                    var func = (delegate* unmanaged<IntPtr, char*, Guid*, IntPtr*, int>)(*(*(void***)_inst + 3));
                    int hr;
                    IntPtr runtimeInfoPtr;
                    fixed (char* versionPtr = pwzVersion)
                    fixed (Guid* riidPtr = &riid)
                    {
                        hr = func(_inst, versionPtr, riidPtr, &runtimeInfoPtr);
                    }
                    ppRuntime = hr == 0
                        ? (CLRRuntimeInfoWrapperCache.ClrRuntimeInfoRcw)CLRRuntimeInfoWrapperCache.Instance.GetOrCreateObjectForComInstance(runtimeInfoPtr, CreateObjectFlags.UniqueInstance)
                        : null;
                    return hr;
                }

                public void Dispose()
                {
                    GC.SuppressFinalize(this);
                    DisposeInternal();
                }

                private void DisposeInternal()
                {
                    if (_disposed)
                        return;
                    Marshal.Release(_inst);
                    _disposed = true;
                }

                ~ClrMetaHostRcw()
                {
                    DisposeInternal();
                }
            }
        }

        interface ICLRRuntimeInfo
        {
            int GetInterface(ref Guid rclsid, ref Guid riid, out MetaDataDispenserWrapperCache.MetaDataDispenserRcw? ppUnk);

            int BindAsLegacyV2Runtime();

            // Don't need any other methods.
        }

        private class CLRRuntimeInfoWrapperCache : ComWrappers
        {
            public static readonly CLRRuntimeInfoWrapperCache Instance = new CLRRuntimeInfoWrapperCache();
            private CLRRuntimeInfoWrapperCache() { }

            protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count) => throw new NotImplementedException();
            protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
            {
                Debug.Assert(flags == CreateObjectFlags.UniqueInstance);

                return new ClrRuntimeInfoRcw(externalComObject);
            }
            protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();

            public unsafe sealed record ClrRuntimeInfoRcw(IntPtr Inst) : ICLRRuntimeInfo, IDisposable
            {
                /// <summary>
                /// List of offsets of methods in the vtable (0-based). First three are from IUnknown.
                /// </summary>
                private enum VtableOffset
                {
                    GetInterface = 9,
                    BindAsLegacyV2Runtime = 13
                }

                private bool _disposed = false;

                public int GetInterface(ref Guid rclsid, ref Guid riid, out MetaDataDispenserWrapperCache.MetaDataDispenserRcw? ppUnk)
                {
                    var func = (delegate* unmanaged<IntPtr, Guid*, Guid*, IntPtr*, int>)(*(*(void***)Inst + (int)VtableOffset.GetInterface));
                    IntPtr outPtr;
                    int hr;
                    fixed (Guid* rclsidPtr = &rclsid)
                    fixed (Guid* riidPtr = &riid)
                    {
                        hr = func(Inst, rclsidPtr, riidPtr, &outPtr);
                    }
                    ppUnk = hr == 0
                        ? (MetaDataDispenserWrapperCache.MetaDataDispenserRcw)MetaDataDispenserWrapperCache.Instance.GetOrCreateObjectForComInstance(outPtr, CreateObjectFlags.UniqueInstance)
                        : null;
                    return hr;
                }

                [PreserveSig]
                public int BindAsLegacyV2Runtime()
                {
                    var func = (delegate* unmanaged<IntPtr, int>)(*(*(void***)Inst + (int)VtableOffset.BindAsLegacyV2Runtime));
                    return func(Inst);
                }

                public void Dispose()
                {
                    DisposeInternal();
                    GC.SuppressFinalize(this);
                }

                private void DisposeInternal()
                {
                    if (_disposed)
                    {
                        return;
                    }
                    _disposed = true;
                    Marshal.Release(Inst);
                }

                ~ClrRuntimeInfoRcw()
                {
                    DisposeInternal();
                }
            }
        }

        private interface IMetaDataDispenser
        {
            int OpenScope(string szScope, int dwOpenFlags, ref Guid riid, out MetadataImportRcw? punk);

            // Don't need any other methods.
        }

        private sealed class MetaDataDispenserWrapperCache : ComWrappers
        {
            public static readonly MetaDataDispenserWrapperCache Instance = new MetaDataDispenserWrapperCache();
            private MetaDataDispenserWrapperCache() { }

            protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count) => throw new NotImplementedException();
            protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
            {
                Debug.Assert(flags == CreateObjectFlags.UniqueInstance);

                return new MetaDataDispenserRcw(externalComObject);
            }
            protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();

            public sealed unsafe record MetaDataDispenserRcw(IntPtr Inst) : IMetaDataDispenser, IDisposable
            {
                private bool _disposed = false;

                /// <remarks>
                /// <paramref="punk" /> is simply a boxed IntPtr, because we don't need an RCW.
                /// </remarks>
                public int OpenScope(string szScope, int dwOpenFlags, ref Guid riid, out MetadataImportRcw? pUnk)
                {
                    // IMetaDataDispenserRcw::OpenScope is slot 5 (0-based)
                    var func = (delegate* unmanaged<IntPtr, char*, int, Guid*, IntPtr*, int>)(*(*(void***)Inst + 4));
                    IntPtr outPtr;
                    int hr;
                    fixed (char* szScopePtr = szScope)
                    fixed (Guid* riidPtr = &riid)
                    {
                        hr = func(Inst, szScopePtr, dwOpenFlags, riidPtr, &outPtr);
                    }
                    pUnk = hr == 0 ? new MetadataImportRcw(outPtr) : null;
                    return hr;
                }

                public void Dispose()
                {
                    DisposeInternal();
                    GC.SuppressFinalize(this);
                }

                private void DisposeInternal()
                {
                    if (_disposed)
                    {
                        return;
                    }
                    _disposed = true;
                    Marshal.Release(Inst);
                }

                ~MetaDataDispenserRcw()
                {
                    DisposeInternal();
                }
            }
        }


        private sealed class CoCreateWrapperCache : ComWrappers
        {
            public static readonly CoCreateWrapperCache Instance = new CoCreateWrapperCache();
            private CoCreateWrapperCache() { }

            protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count) => throw new NotImplementedException();
            protected override SymUnmanagedBinderRcw? CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
            {
                Debug.Assert(flags == CreateObjectFlags.UniqueInstance);

                var iid = new Guid("AA544D42-28CB-11d3-BD22-0000F80849BD");
                if (Marshal.QueryInterface(externalComObject, ref iid, out IntPtr ppv) != 0)
                {
                    return null;
                }
                return new SymUnmanagedBinderRcw(ppv);
            }
            protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();

            public unsafe record SymUnmanagedBinderRcw(IntPtr Inst) : ISymUnmanagedBinder
            {
                private bool _disposed = false;

                public int GetReaderForFile(MetadataImportRcw metadataImporter, string fileName, string searchPath, out SymUnmanagedReaderWrapperCache.SymUnmanagedReaderRcw? reader)
                {
                    // ISymUnmanagedBinder::GetReaderForFile is slot 4 (0-based)
                    var func = (delegate* unmanaged<IntPtr, IntPtr, char*, char*, IntPtr*, int>)(*(*(void***)Inst + 3));
                    int hr;
                    IntPtr readerPtr;
                    fixed (char* fileNamePtr = fileName)
                    fixed (char* searchPathPtr = searchPath)
                    {
                        hr = func(Inst, metadataImporter.Ptr, fileNamePtr, searchPathPtr, &readerPtr);
                    }
                    reader = hr == 0
                        ? (SymUnmanagedReaderWrapperCache.SymUnmanagedReaderRcw)SymUnmanagedReaderWrapperCache.Instance.GetOrCreateObjectForComInstance(readerPtr, CreateObjectFlags.UniqueInstance)
                        : null;
                    return hr;
                }
                public int GetReaderFromStream(object metadataImporter, object stream, out ISymUnmanagedReader reader) => throw new NotImplementedException();

                public void Dispose()
                {
                    DisposeInternal();
                    GC.SuppressFinalize(this);
                }

                private void DisposeInternal()
                {
                    if (_disposed)
                    {
                        return;
                    }
                    _disposed = true;
                    Marshal.Release(Inst);
                }

                ~SymUnmanagedBinderRcw()
                {
                    DisposeInternal();
                }
            }
        }

        /// <summary>
        /// Wrapper for an IMetaDataImport instance.
        /// </summary>
        private sealed record MetadataImportRcw(IntPtr Ptr) : IDisposable
        {
            private bool _disposed = false;

            public void Dispose()
            {
                DisposeInternal();
                GC.SuppressFinalize(this);
            }
            private void DisposeInternal()
            {
                if (_disposed)
                    return;
                Marshal.Release(Ptr);
                _disposed = true;
            }
            ~MetadataImportRcw()
            {
                DisposeInternal();
            }
        }

        interface ISymUnmanagedReader
        {
            int GetMethod(int methodToken, out ISymUnmanagedMethod? method);
            // No more members are used
        }

        private sealed class SymUnmanagedNamespaceWrapperCache : ComWrappers
        {
            public static readonly SymUnmanagedNamespaceWrapperCache Instance = new SymUnmanagedNamespaceWrapperCache();

            protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count) => throw new NotImplementedException();
            protected override object? CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
            {
                Debug.Assert(flags == CreateObjectFlags.UniqueInstance);
                return new SymUnmanagedNamespaceRcw(externalComObject);
            }
            protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();

            public unsafe sealed record SymUnmanagedNamespaceRcw(IntPtr Inst) : ISymUnmanagedNamespace
            {
                private bool _disposed = false;

                public int GetName(int bufferLength, out int count, char[] name)
                {
                    var func = (delegate* unmanaged<IntPtr, int, int*, char*, int>)(*(*(void***)Inst + 3));
                    fixed (int* countPtr = &count)
                    fixed (char* namePtr = name)
                    {
                        return func(Inst, bufferLength, countPtr, namePtr);
                    }
                }

                public int GetNamespaces(int bufferLength, out int count, ISymUnmanagedNamespace[] namespaces)
                {
                    var func = (delegate* unmanaged<IntPtr, int, int*, IntPtr*, int>)(*(*(void***)Inst + 4));
                    int hr;

                    fixed (int* countPtr = &count)
                    {
                        if (namespaces == null)
                        {
                            hr = func(Inst, bufferLength, countPtr, null);
                        }
                        else
                        {
                            IntPtr[] intermediate = new IntPtr[namespaces.Length];
                            fixed (IntPtr* intermediatePtr = intermediate)
                            {
                                hr = func(Inst, bufferLength, countPtr, intermediatePtr);
                            }

                            if (hr == 0)
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    namespaces[i] = (SymUnmanagedNamespaceWrapperCache.SymUnmanagedNamespaceRcw)SymUnmanagedNamespaceWrapperCache.Instance.GetOrCreateObjectForComInstance(intermediate[i], CreateObjectFlags.UniqueInstance);
                                }
                            }
                        }
                    }

                    return hr;
                }

                public int GetVariables(int bufferLength, out int count, ISymUnmanagedVariable[] variables)
                {
                    var func = (delegate* unmanaged<IntPtr, int, int*, IntPtr*, int>)(*(*(void***)Inst + 5));
                    int hr;

                    fixed (int* countPtr = &count)
                    {
                        if (variables == null)
                        {
                            hr = func(Inst, bufferLength, countPtr, null);
                        }
                        else
                        {
                            IntPtr[] intermediate = new IntPtr[variables.Length];
                            fixed (IntPtr* intermediatePtr = intermediate)
                            {
                                hr = func(Inst, bufferLength, countPtr, intermediatePtr);
                            }

                            if (hr == 0)
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    variables[i] = (SymUnmanagedVariableWrapperCache.SymUnmanagedVariableRcw)SymUnmanagedVariableWrapperCache.Instance.GetOrCreateObjectForComInstance(intermediate[i], CreateObjectFlags.UniqueInstance);
                                }
                            }
                        }
                    }

                    return hr;
                }

                public void Dispose()
                {
                    DisposeInternal();
                    GC.SuppressFinalize(this);
                }

                private void DisposeInternal()
                {
                    if (_disposed)
                    {
                        return;
                    }
                    _disposed = true;
                    Marshal.Release(Inst);
                }

                ~SymUnmanagedNamespaceRcw()
                {
                    DisposeInternal();
                }
            }
        }

        private sealed class SymUnmanagedVariableWrapperCache : ComWrappers
        {
            public static readonly SymUnmanagedVariableWrapperCache Instance = new SymUnmanagedVariableWrapperCache();

            protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count) => throw new NotImplementedException();
            protected override object? CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
            {
                Debug.Assert(flags == CreateObjectFlags.UniqueInstance);
                return new SymUnmanagedVariableRcw(externalComObject);
            }
            protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();

            public unsafe sealed record SymUnmanagedVariableRcw(IntPtr Inst) : ISymUnmanagedVariable
            {
                private bool _disposed = false;

                public int GetName(int bufferLength, out int count, char[] name)
                {
                    var func = (delegate* unmanaged<IntPtr, int, int*, char*, int>)(*(*(void***)Inst + 3));
                    fixed (int* countPtr = &count)
                    fixed (char* namePtr = name)
                    {
                        return func(Inst, bufferLength, countPtr, namePtr);
                    }
                }

                public int GetAttributes(out int attributes) => SingleByRefIntWrapper(4, out attributes);

                public int GetSignature(int bufferLength, out int count, byte[] signature)
                {
                    var func = (delegate* unmanaged<IntPtr, int, int*, byte*, int>)(*(*(void***)Inst + 5));
                    fixed (int* countPtr = &count)
                    fixed (byte* signaturePtr = signature)
                    {
                        return func(Inst, bufferLength, countPtr, signaturePtr);
                    }
                }

                public int GetAddressKind(out int kind) => SingleByRefIntWrapper(6, out kind);

                public int GetAddressField1(out int value) => SingleByRefIntWrapper(7, out value);

                public int GetAddressField2(out int value) => SingleByRefIntWrapper(8, out value);

                public int GetAddressField3(out int value) => SingleByRefIntWrapper(9, out value);

                public int GetStartOffset(out int offset) => SingleByRefIntWrapper(10, out offset);

                public int GetEndOffset(out int offset) => SingleByRefIntWrapper(11, out offset);

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private int SingleByRefIntWrapper(int methodSlot, out int value)
                {
                    var func = (delegate* unmanaged<IntPtr, int*, int>)(*(*(void***)Inst + methodSlot));
                    fixed (int* valuePtr = &value)
                    {
                        return func(Inst, valuePtr);
                    }
                }

                public void Dispose()
                {
                    DisposeInternal();
                    GC.SuppressFinalize(this);
                }

                private void DisposeInternal()
                {
                    if (_disposed)
                    {
                        return;
                    }
                    _disposed = true;
                    Marshal.Release(Inst);
                }

                ~SymUnmanagedVariableRcw()
                {
                    DisposeInternal();
                }
            }
        }

        private sealed class SymUnmanagedScopeWrapperCache : ComWrappers
        {
            public static readonly SymUnmanagedScopeWrapperCache Instance = new SymUnmanagedScopeWrapperCache();

            protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count) => throw new NotImplementedException();
            protected override object? CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
            {
                Debug.Assert(flags == CreateObjectFlags.UniqueInstance);
                return new SymUnmanagedScopeRcw(externalComObject);
            }

            protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();

            public unsafe sealed record SymUnmanagedScopeRcw(IntPtr Inst) : ISymUnmanagedScope
            {
                private bool _disposed = false;

                public int GetMethod(out ISymUnmanagedMethod? method)
                {
                    var func = (delegate* unmanaged<IntPtr, IntPtr*, int>)(*(*(void***)Inst + 3));
                    IntPtr methodPtr;
                    int hr = func(Inst, &methodPtr);
                    method = hr == 0
                        ? (SymUnmanagedMethodWrapperCache.SymUnmanagedMethodRcw)SymUnmanagedMethodWrapperCache.Instance.GetOrCreateObjectForComInstance(methodPtr, CreateObjectFlags.UniqueInstance)
                        : null;
                    return hr;
                }

                public int GetParent(out ISymUnmanagedScope? scope)
                {
                    var func = (delegate* unmanaged<IntPtr, IntPtr*, int>)(*(*(void***)Inst + 4));
                    IntPtr scopePtr;
                    int hr = func(Inst, &scopePtr);
                    scope = hr == 0
                        ? (SymUnmanagedScopeWrapperCache.SymUnmanagedScopeRcw)SymUnmanagedScopeWrapperCache.Instance.GetOrCreateObjectForComInstance(scopePtr, CreateObjectFlags.UniqueInstance)
                        : null;
                    return hr;
                }

                public int GetChildren(int bufferLength, out int count, ISymUnmanagedScope[] children)
                {
                    var func = (delegate* unmanaged<IntPtr, int, int*, IntPtr*, int>)(*(*(void***)Inst + 5));
                    int hr;

                    fixed (int* countPtr = &count)
                    {
                        if (children == null)
                        {
                            hr = func(Inst, bufferLength, countPtr, null);
                        }
                        else
                        {
                            IntPtr[] intermediate = new IntPtr[children.Length];
                            fixed (IntPtr* intermediatePtr = intermediate)
                            {
                                hr = func(Inst, bufferLength, countPtr, intermediatePtr);
                            }

                            if (hr == 0)
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    children[i] = (SymUnmanagedScopeWrapperCache.SymUnmanagedScopeRcw)SymUnmanagedScopeWrapperCache.Instance.GetOrCreateObjectForComInstance(intermediate[i], CreateObjectFlags.UniqueInstance);
                                }
                            }
                        }
                    }

                    return hr;
                }

                public int GetStartOffset(out int offset) => SingleByRefIntWrapper(6, out offset);

                public int GetEndOffset(out int offset) => SingleByRefIntWrapper(7, out offset);

                public int GetLocalCount(out int count) => SingleByRefIntWrapper(8, out count);

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private int SingleByRefIntWrapper(int methodSlot, out int value)
                {
                    var func = (delegate* unmanaged<IntPtr, int*, int>)(*(*(void***)Inst + methodSlot));
                    fixed (int* valuePtr = &value)
                    {
                        return func(Inst, valuePtr);
                    }
                }

                public int GetLocals(int bufferLength, out int count, ISymUnmanagedVariable[] locals)
                {
                    var func = (delegate* unmanaged<IntPtr, int, int*, IntPtr*, int>)(*(*(void***)Inst + 9));
                    int hr;

                    fixed (int* countPtr = &count)
                    {
                        if (locals == null)
                        {
                            hr = func(Inst, bufferLength, countPtr, null);
                        }
                        else
                        {
                            IntPtr[] intermediate = new IntPtr[locals.Length];
                            fixed (IntPtr* intermediatePtr = intermediate)
                            {
                                hr = func(Inst, bufferLength, countPtr, intermediatePtr);
                            }

                            if (hr == 0)
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    locals[i] = (SymUnmanagedVariableWrapperCache.SymUnmanagedVariableRcw)SymUnmanagedVariableWrapperCache.Instance.GetOrCreateObjectForComInstance(intermediate[i], CreateObjectFlags.UniqueInstance);
                                }
                            }
                        }
                    }

                    return hr;
                }

                public int GetNamespaces(int bufferLength, out int count, ISymUnmanagedNamespace[] namespaces)
                {
                    var func = (delegate* unmanaged<IntPtr, int, int*, IntPtr*, int>)(*(*(void***)Inst + 10));
                    int hr;

                    fixed (int* countPtr = &count)
                    {
                        if (namespaces == null)
                        {
                            hr = func(Inst, bufferLength, countPtr, null);
                        }
                        else
                        {
                            IntPtr[] intermediate = new IntPtr[namespaces.Length];
                            fixed (IntPtr* intermediatePtr = intermediate)
                            {
                                hr = func(Inst, bufferLength, countPtr, intermediatePtr);
                            }

                            if (hr == 0)
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    namespaces[i] = (SymUnmanagedNamespaceWrapperCache.SymUnmanagedNamespaceRcw)SymUnmanagedNamespaceWrapperCache.Instance.GetOrCreateObjectForComInstance(intermediate[i], CreateObjectFlags.UniqueInstance);
                                }
                            }
                        }
                    }

                    return hr;
                }

                public void Dispose()
                {
                    DisposeInternal();
                    GC.SuppressFinalize(this);
                }

                private void DisposeInternal()
                {
                    if (_disposed)
                    {
                        return;
                    }
                    _disposed = true;
                    Marshal.Release(Inst);
                }

                ~SymUnmanagedScopeRcw()
                {
                    DisposeInternal();
                }
            }
        }

        private sealed class SymUnmanagedDocumentWrapperCache : ComWrappers
        {
            public static readonly SymUnmanagedDocumentWrapperCache Instance = new SymUnmanagedDocumentWrapperCache();

            protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count) => throw new NotImplementedException();
            protected override object? CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
            {
                Debug.Assert(flags == CreateObjectFlags.UniqueInstance);
                return new SymUnmanagedDocumentRcw(externalComObject);
            }
            protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();

            public unsafe record SymUnmanagedDocumentRcw(IntPtr Inst) : ISymUnmanagedDocument
            {
                private bool _disposed = false;

                public int GetUrl(int bufferLength, out int count, char[] url)
                {
                    var func = (delegate* unmanaged<IntPtr, int, int*, char*, int>)(*(*(void***)Inst + 3));
                    fixed (int* countPtr = &count)
                    fixed (char* urlPtr = url)
                    {
                        return func(Inst, bufferLength, countPtr, urlPtr);
                    }
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private int SingleByRefGuidWrapper(int methodSolt, ref Guid guid)
                {
                    var func = (delegate* unmanaged<IntPtr, Guid*, int>)(*(*(void***)Inst + methodSolt));
                    fixed (Guid* guidPtr = &guid)
                    {
                        return func(Inst, guidPtr);
                    }
                }

                public int GetDocumentType(ref Guid documentType) => SingleByRefGuidWrapper(4, ref documentType);
                public int GetLanguage(ref Guid language) => SingleByRefGuidWrapper(5, ref language);
                public int GetLanguageVendor(ref Guid vendor) => SingleByRefGuidWrapper(6, ref vendor);
                public int GetChecksumAlgorithmId(ref Guid algorithm) => SingleByRefGuidWrapper(7, ref algorithm);

                public int GetChecksum(int bufferLength, out int count, byte[] checksum)
                {
                    var func = (delegate* unmanaged<IntPtr, int, int*, byte*, int>)(*(*(void***)Inst + 8));
                    fixed (int* countPtr = &count)
                    fixed (byte* checksumPtr = checksum)
                    {
                        return func(Inst, bufferLength, countPtr, checksumPtr);
                    }
                }

                public int FindClosestLine(int line, out int closestLine)
                {
                    var func = (delegate* unmanaged<IntPtr, int, int*, int>)(*(*(void***)Inst + 9));
                    fixed (int* closestLinePtr = &closestLine)
                    {
                        return func(Inst, line, closestLinePtr);
                    }
                }

                public int HasEmbeddedSource(out bool value)
                {
                    var func = (delegate* unmanaged<IntPtr, bool*, int>)(*(*(void***)Inst + 10));
                    fixed (bool* valuePtr = &value)
                    {
                        return func(Inst, valuePtr);
                    }
                }

                public int GetSourceLength(out int length)
                {
                    var func = (delegate* unmanaged<IntPtr, int*, int>)(*(*(void***)Inst + 11));
                    fixed (int* lengthPtr = &length)
                    {
                        return func(Inst, lengthPtr);
                    }
                }

                public int GetSourceRange(int startLine, int startColumn, int endLine, int endColumn, int bufferLength, out int count, byte[] source)
                {
                    var func = (delegate* unmanaged<IntPtr, int, int, int, int, int, int*, byte*, int>)(*(*(void***)Inst + 12));
                    fixed (int* countPtr = &count)
                    fixed (byte* sourcePtr = source)
                    {
                        return func(Inst, startLine, startColumn, endLine, endColumn, bufferLength, countPtr, sourcePtr);
                    }
                }

                public void Dispose()
                {
                    DisposeInternal();
                    GC.SuppressFinalize(this);
                }

                private void DisposeInternal()
                {
                    if (_disposed)
                    {
                        return;
                    }
                    _disposed = true;
                    Marshal.Release(Inst);
                }

                ~SymUnmanagedDocumentRcw()
                {
                    DisposeInternal();
                }
            }
        }

        private sealed class SymUnmanagedMethodWrapperCache : ComWrappers
        {
            public static readonly SymUnmanagedMethodWrapperCache Instance = new SymUnmanagedMethodWrapperCache();

            protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count) => throw new NotImplementedException();
            protected override object? CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
            {
                Debug.Assert(flags == CreateObjectFlags.UniqueInstance);
                return new SymUnmanagedMethodRcw(externalComObject);
            }
            protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();

            public unsafe sealed record SymUnmanagedMethodRcw(IntPtr Inst) : ISymUnmanagedMethod
            {
                private bool _disposed = false;

                public int GetToken(out int methodToken)
                {
                    var func = (delegate* unmanaged<IntPtr, int*, int>)(*(*(void***)Inst + 3));
                    fixed (int* methodTokenPtr = &methodToken)
                    {
                        return func(Inst, methodTokenPtr);
                    }
                }

                public int GetSequencePointCount(out int count)
                {
                    var func = (delegate* unmanaged<IntPtr, int*, int>)(*(*(void***)Inst + 4));
                    fixed (int* countPtr = &count)
                    {
                        return func(Inst, countPtr);
                    }
                }

                public int GetRootScope(out ISymUnmanagedScope? scope)
                {
                    var func = (delegate* unmanaged<IntPtr, IntPtr*, int>)(*(*(void***)Inst + 5));
                    IntPtr scopePtr;
                    int hr = func(Inst, &scopePtr);
                    scope = hr == 0
                        ? (SymUnmanagedScopeWrapperCache.SymUnmanagedScopeRcw)SymUnmanagedScopeWrapperCache.Instance.GetOrCreateObjectForComInstance(scopePtr, CreateObjectFlags.UniqueInstance)
                        : null;
                    return hr;
                }

                public int GetScopeFromOffset(int offset, out ISymUnmanagedScope? scope)
                {
                    var func = (delegate* unmanaged<IntPtr, int, IntPtr*, int>)(*(*(void***)Inst + 6));
                    IntPtr scopePtr;
                    int hr = func(Inst, offset, &scopePtr);
                    scope = hr == 0
                        ? (SymUnmanagedScopeWrapperCache.SymUnmanagedScopeRcw)SymUnmanagedScopeWrapperCache.Instance.GetOrCreateObjectForComInstance(scopePtr, CreateObjectFlags.UniqueInstance)
                        : null;
                    return hr;
                }

                public int GetOffset(ISymUnmanagedDocument document, int line, int column, out int offset)
                {
                    var func = (delegate* unmanaged<IntPtr, IntPtr, int, int, int*, int>)(*(*(void***)Inst + 7));
                    var handle = GCHandle.Alloc(document, GCHandleType.Pinned);
                    try
                    {
                        fixed (int* offsetPtr = &offset)
                        {
                            return func(Inst, handle.AddrOfPinnedObject(), line, column, offsetPtr);
                        }
                    }
                    finally
                    {
                        handle.Free();
                    }
                }

                public int GetRanges(ISymUnmanagedDocument document, int line, int column, int bufferLength, out int count, int[] ranges)
                {
                    var func = (delegate* unmanaged<IntPtr, IntPtr, int, int, int, int*, int*, int>)(*(*(void***)Inst + 8));
                    var handle = GCHandle.Alloc(document, GCHandleType.Pinned);
                    try
                    {
                        fixed (int* countPtr = &count)
                        fixed (int* rangesPtr = ranges)
                        {
                            return func(Inst, handle.AddrOfPinnedObject(), line, column, bufferLength, countPtr, rangesPtr);
                        }
                    }
                    finally
                    {
                        handle.Free();
                    }
                }

                public int GetParameters(int bufferLength, out int count, ISymUnmanagedVariable[] parameters)
                {
                    var func = (delegate* unmanaged<IntPtr, int, int*, IntPtr*, int>)(*(*(void***)Inst + 9));
                    int hr;

                    fixed (int* countPtr = &count)
                    {
                        if (parameters == null)
                        {
                            hr = func(Inst, bufferLength, countPtr, null);
                        }
                        else
                        {
                            IntPtr[] intermediate = new IntPtr[parameters.Length];
                            fixed (IntPtr* intermediatePtr = intermediate)
                            {
                                hr = func(Inst, bufferLength, countPtr, intermediatePtr);
                            }

                            if (hr == 0)
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    parameters[i] = (SymUnmanagedVariableWrapperCache.SymUnmanagedVariableRcw)SymUnmanagedVariableWrapperCache.Instance.GetOrCreateObjectForComInstance(intermediate[i], CreateObjectFlags.UniqueInstance);
                                }
                            }
                        }
                    }

                    return hr;
                }

                public int GetNamespace(out ISymUnmanagedNamespace? @namespace)
                {
                    var func = (delegate* unmanaged<IntPtr, IntPtr*, int>)(*(*(void***)Inst + 10));
                    IntPtr namespacePtr;
                    int hr = func(Inst, &namespacePtr);
                    @namespace = hr == 0
                        ? (SymUnmanagedNamespaceWrapperCache.SymUnmanagedNamespaceRcw)SymUnmanagedNamespaceWrapperCache.Instance.GetOrCreateObjectForComInstance(namespacePtr, CreateObjectFlags.UniqueInstance)
                        : null;
                    return hr;
                }

                public int GetSourceStartEnd(ISymUnmanagedDocument[] documents, int[] lines, int[] columns, out bool defined)
                {
                    var func = (delegate* unmanaged<IntPtr, IntPtr*, int*, int*, bool*, int>)(*(*(void***)Inst + 11));
                    var handle = GCHandle.Alloc(documents, GCHandleType.Pinned);
                    try
                    {
                        fixed (int* linesPtr = lines)
                        fixed (int* columnsPtr = columns)
                        fixed (bool* definedPtr = &defined)
                        {
                            return func(Inst, (IntPtr*)handle.AddrOfPinnedObject(), linesPtr, columnsPtr, definedPtr);
                        }
                    }
                    finally
                    {
                        handle.Free();
                    }
                }

                public int GetSequencePoints(int bufferLength, out int count, int[] offsets, ISymUnmanagedDocument[] documents, int[] startLines, int[] startColumns, int[] endLines, int[] endColumns)
                {
                    var func = (delegate* unmanaged<IntPtr, int, int*, int*, IntPtr*, int*, int*, int*, int*, int>)(*(*(void***)Inst + 12));
                    int hr;

                    fixed (int* countPtr = &count)
                    fixed (int* offsetsPtr = offsets)
                    fixed (int* startLinesPtr = startLines)
                    fixed (int* endLinesPtr = endLines)
                    fixed (int* startColumnsPtr = startColumns)
                    fixed (int* endColumnsPtr = endColumns)
                    {
                        if (documents == null)
                        {
                            hr = func(Inst, bufferLength, countPtr, offsetsPtr, null, startLinesPtr, startColumnsPtr, endLinesPtr, endColumnsPtr);
                        }
                        else
                        {
                            IntPtr[] intermediate = new IntPtr[documents.Length];
                            fixed (IntPtr* intermediatePtr = intermediate)
                            {
                                hr = func(Inst, bufferLength, countPtr, offsetsPtr, intermediatePtr, startLinesPtr, startColumnsPtr, endLinesPtr, endColumnsPtr);
                            }
                            if (hr == 0)
                            {
                                for (int i = 0; i < documents.Length; i++)
                                {
                                    documents[i] = (SymUnmanagedDocumentWrapperCache.SymUnmanagedDocumentRcw)SymUnmanagedDocumentWrapperCache.Instance.GetOrCreateObjectForComInstance(intermediate[i], CreateObjectFlags.UniqueInstance);
                                }
                            }
                        }
                    }

                    return hr;
                }

                public void Dispose()
                {
                    DisposeInternal();
                    GC.SuppressFinalize(this);
                }

                private void DisposeInternal()
                {
                    if (_disposed)
                    {
                        return;
                    }
                    _disposed = true;
                    Marshal.Release(Inst);
                }

                ~SymUnmanagedMethodRcw()
                {
                    DisposeInternal();
                }
            }
        }

        private sealed class SymUnmanagedReaderWrapperCache : ComWrappers
        {
            public static readonly SymUnmanagedReaderWrapperCache Instance = new SymUnmanagedReaderWrapperCache();
            private SymUnmanagedReaderWrapperCache() { }

            protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count) => throw new NotImplementedException();
            protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
            {
                Debug.Assert(flags == CreateObjectFlags.UniqueInstance);
                return new SymUnmanagedReaderRcw(externalComObject);
            }
            protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();

            /// <summary>
            /// Caveat: implements only the few interface methods currently needed for crossgen2.
            /// </summary>
            public sealed record SymUnmanagedReaderRcw(IntPtr Inst) : ISymUnmanagedReader
            {
                private bool _disposed = false;

                public unsafe int GetMethod(int methodToken, out ISymUnmanagedMethod? method)
                {
                    // ISymUnmanagedReader::GetMethod is slot 7 (0-based)
                    var func = (delegate* unmanaged<IntPtr, int, IntPtr*, int>)(*(*(void***)Inst + 6));
                    IntPtr methodPtr;
                    int hr = func(Inst, methodToken, &methodPtr);
                    method = hr == 0
                        ? (SymUnmanagedMethodWrapperCache.SymUnmanagedMethodRcw)SymUnmanagedMethodWrapperCache.Instance.GetOrCreateObjectForComInstance(methodPtr, CreateObjectFlags.UniqueInstance)
                        : null;
                    return hr;
                }

                public void Dispose()
                {
                    DisposeInternal();
                    GC.SuppressFinalize(this);
                }

                private void DisposeInternal()
                {
                    if (_disposed)
                    {
                        return;
                    }
                    _disposed = true;
                    Marshal.Release(Inst);
                }

                ~SymUnmanagedReaderRcw()
                {
                    DisposeInternal();
                }
            }
        }

        private interface ISymUnmanagedBinder
        {
            int GetReaderForFile(
                MetadataImportRcw importer,
                string filename,
                string searchPath,
                out SymUnmanagedReaderWrapperCache.SymUnmanagedReaderRcw? symReader);
        }

        private static int CoCreateInstance(ref Guid rclsid, IntPtr pUnkOuter,
                                           Int32 dwClsContext,
                                           ref Guid riid,
                                           out CoCreateWrapperCache.SymUnmanagedBinderRcw? ppv)
        {
            Debug.Assert(rclsid == SymBinderIID);
            int hr = CoCreateInstance(ref rclsid, pUnkOuter, dwClsContext, ref riid, out IntPtr ppvPtr);
            ppv = hr == 0
                ? (CoCreateWrapperCache.SymUnmanagedBinderRcw)CoCreateWrapperCache.Instance.GetOrCreateObjectForComInstance(ppvPtr, CreateObjectFlags.UniqueInstance)
                : null;
            return hr;

            [DllImport("ole32.dll")]
            static extern int CoCreateInstance(ref Guid rclsid, IntPtr pUnkOuter,
                                            Int32 dwClsContext,
                                            ref Guid riid,
                                            out IntPtr ppv);
        }

        private void ThrowExceptionForHR(int hr)
        {
            Marshal.ThrowExceptionForHR(hr, new IntPtr(-1));
        }

        private static readonly Guid SymBinderIID = new Guid(0x0a29ff9e, 0x7f9c, 0x4437, 0x8b, 0x11, 0xf4, 0x24, 0x49, 0x1e, 0x39, 0x31);

        static UnmanagedPdbSymbolReader()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    Guid IID_IUnknown = new Guid(0x00000000, 0x0000, 0x0000, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);

                    Guid CLSID_CLRMetaHost = new Guid(0x9280188d, 0x0e8e, 0x4867, 0xb3, 0x0c, 0x7f, 0xa8, 0x38, 0x84, 0xe8, 0xde);
                    Guid IID_CLRMetaHost = new Guid(0xd332db9e, 0xb9b3, 0x4125, 0x82, 0x07, 0xa1, 0x48, 0x84, 0xf5, 0x32, 0x16);
                    if (CLRCreateInstance(ref CLSID_CLRMetaHost, ref IID_CLRMetaHost, out var objMetaHost) < 0)
                        return;
                    Debug.Assert(objMetaHost is not null);
                    using (objMetaHost)
                    {
                        Guid IID_CLRRuntimeInfo = new Guid(0xbd39d1d2, 0xba2f, 0x486a, 0x89, 0xb0, 0xb4, 0xb0, 0xcb, 0x46, 0x68, 0x91);
                        if (objMetaHost.GetRuntime("v4.0.30319", ref IID_CLRRuntimeInfo, out var objRuntime) < 0)
                            return;
                        Debug.Assert(objRuntime is not null);
                        using (objRuntime)
                        {
                            // To get everything from the v4 runtime
                            objRuntime.BindAsLegacyV2Runtime();

                            // Create a COM Metadata dispenser
                            Guid CLSID_CorMetaDataDispenser = new Guid(0xe5cb7a31, 0x7512, 0x11d2, 0x89, 0xce, 0x00, 0x80, 0xc7, 0x92, 0xe5, 0xd8);
                            if (objRuntime.GetInterface(ref CLSID_CorMetaDataDispenser, ref IID_IUnknown, out var objDispenser) < 0)
                                return;
                            Debug.Assert(objDispenser is not null);
                            s_metadataDispenser = objDispenser;

                            // Create a SymBinder
                            Guid CLSID_CorSymBinder = SymBinderIID;
                            if (CoCreateInstance(ref CLSID_CorSymBinder,
                                                 IntPtr.Zero, // pUnkOuter
                                                 1, // CLSCTX_INPROC_SERVER
                                                 ref IID_IUnknown,
                                                 out var objBinder) < 0)
                                return;
                            Debug.Assert(objBinder is not null);
                            s_symBinder = objBinder;
                        }
                    }
                }
                catch
                {
                }
            }
        }

        private readonly static MetaDataDispenserWrapperCache.MetaDataDispenserRcw? s_metadataDispenser;
        private readonly static CoCreateWrapperCache.SymUnmanagedBinderRcw? s_symBinder;

        public static PdbSymbolReader? TryOpenSymbolReaderForMetadataFile(string metadataFileName, string searchPath)
        {
            try
            {
                if (s_metadataDispenser == null || s_symBinder == null)
                    return null;

                Guid IID_IMetaDataImport = new Guid(0x7dac8207, 0xd3ae, 0x4c75, 0x9b, 0x67, 0x92, 0x80, 0x1a, 0x49, 0x7d, 0x44);

                // Open an metadata importer on the given filename. We'll end up passing this importer straight
                // through to the Binder.
                if (s_metadataDispenser.OpenScope(metadataFileName, 0x00000010 /* read only */, ref IID_IMetaDataImport, out var objImporter) < 0)
                    return null;
                Debug.Assert(objImporter is not null);
                using (objImporter)
                {
                    if (s_symBinder.GetReaderForFile(objImporter, metadataFileName, searchPath, out var reader) < 0)
                        return null;
                    Debug.Assert(reader is not null);

                    return new UnmanagedPdbSymbolReader(reader);
                }
            }
            catch
            {
                return null;
            }
        }

        private readonly SymUnmanagedReaderWrapperCache.SymUnmanagedReaderRcw _symUnmanagedReader;

        private UnmanagedPdbSymbolReader(SymUnmanagedReaderWrapperCache.SymUnmanagedReaderRcw symUnmanagedReader)
        {
            _symUnmanagedReader = symUnmanagedReader;
        }

        public override void Dispose()
        {
            _symUnmanagedReader.Dispose();
        }

        private Dictionary<ISymUnmanagedDocument, string>? _urlCache;

        private string GetUrl(ISymUnmanagedDocument doc)
        {
            lock (this)
            {
                if (_urlCache == null)
                    _urlCache = new Dictionary<ISymUnmanagedDocument, string>();

                if (_urlCache.TryGetValue(doc, out var url))
                    return url;

                int urlLength;
                ThrowExceptionForHR(doc.GetUrl(0, out urlLength, null));

                // urlLength includes terminating '\0'
                char[] urlBuffer = new char[urlLength];
                ThrowExceptionForHR(doc.GetUrl(urlLength, out urlLength, urlBuffer));

                url = new string(urlBuffer, 0, urlLength - 1);
                _urlCache.Add(doc, url);
                return url;
            }
        }

        public override IEnumerable<ILSequencePoint> GetSequencePointsForMethod(int methodToken)
        {
            ISymUnmanagedMethod? symbolMethod;
            if (_symUnmanagedReader.GetMethod(methodToken, out symbolMethod) < 0 || symbolMethod is null)
                yield break;

            int count;
            ThrowExceptionForHR(symbolMethod.GetSequencePointCount(out count));

            ISymUnmanagedDocument[] docs = new ISymUnmanagedDocument[count];
            int[] lineNumbers = new int[count];
            int[] ilOffsets = new int[count];

            ThrowExceptionForHR(symbolMethod.GetSequencePoints(count, out count, ilOffsets, docs, lineNumbers, null, null, null));

            for (int i = 0; i < count; i++)
            {
                if (lineNumbers[i] == 0xFEEFEE)
                    continue;

                yield return new ILSequencePoint(ilOffsets[i], GetUrl(docs[i]), lineNumbers[i]);
            }
        }

        //
        // Gather the local details in a scope and then recurse to child scopes
        //
        private void ProbeScopeForLocals(List<ILLocalVariable> variables, ISymUnmanagedScope scope)
        {
            int localCount;
            ThrowExceptionForHR(scope.GetLocalCount(out localCount));

            ISymUnmanagedVariable[] locals = new ISymUnmanagedVariable[localCount];
            ThrowExceptionForHR(scope.GetLocals(localCount, out localCount, locals));

            for (int i = 0; i < localCount; i++)
            {
                var local = locals[i];

                int slot;
                ThrowExceptionForHR(local.GetAddressField1(out slot));

                int nameLength;
                ThrowExceptionForHR(local.GetName(0, out nameLength, null));

                // nameLength includes terminating '\0'
                char[] nameBuffer = new char[nameLength];
                ThrowExceptionForHR(local.GetName(nameLength, out nameLength, nameBuffer));

                int attributes;
                ThrowExceptionForHR(local.GetAttributes(out attributes));

                variables.Add(new ILLocalVariable(slot, new String(nameBuffer, 0, nameLength - 1), (attributes & 0x1) != 0));
            }

            int childrenCount;
            ThrowExceptionForHR(scope.GetChildren(0, out childrenCount, null));

            ISymUnmanagedScope[] children = new ISymUnmanagedScope[childrenCount];
            ThrowExceptionForHR(scope.GetChildren(childrenCount, out childrenCount, children));

            for (int i = 0; i < childrenCount; i++)
            {
                ProbeScopeForLocals(variables, children[i]);
            }
        }

        //
        // Recursively scan the scopes for a method stored in a PDB and gather the local slots
        // and names for all of them.  This assumes a CSC-like compiler that doesn't re-use
        // local slots in the same method across scopes.
        //
        public override IEnumerable<ILLocalVariable>? GetLocalVariableNamesForMethod(int methodToken)
        {
            ISymUnmanagedMethod? symbolMethod;
            if (_symUnmanagedReader.GetMethod(methodToken, out symbolMethod) < 0 || symbolMethod is null)
                return null;
            Debug.Assert(symbolMethod is not null);

            ISymUnmanagedScope rootScope;
            ThrowExceptionForHR(symbolMethod.GetRootScope(out rootScope));

            var variables = new List<ILLocalVariable>();
            ProbeScopeForLocals(variables, rootScope);
            return variables;
        }

        public override int GetStateMachineKickoffMethod(int methodToken)
        {
            return 0;
        }
    }
#endif
}
