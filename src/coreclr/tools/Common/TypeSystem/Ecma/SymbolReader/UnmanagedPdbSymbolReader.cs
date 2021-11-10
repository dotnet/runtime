// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
        public static PdbSymbolReader TryOpenSymbolReaderForMetadataFile(string metadataFileName, string searchPath)
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
        [DllImport("mscoree.dll")]
        private static extern int CLRCreateInstance([In] ref Guid clsid, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.Interface)] out ICLRMetaHost ppInterface);

        [Guid("d332db9e-b9b3-4125-8207-a14884f53216")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        interface ICLRMetaHost
        {
            [PreserveSig]
            int GetRuntime([In, MarshalAs(UnmanagedType.LPWStr)] String pwzVersion, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.Interface)] out ICLRRuntimeInfo ppRuntime);

            // Don't need any other methods.
        }

        [Guid("bd39d1d2-ba2f-486a-89b0-b4b0cb466891")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        interface ICLRRuntimeInfo
        {
            void GetVersionString_Placeholder();
            void GetRuntimeDirectory_Placeholder();
            void IsLoaded_Placeholder();
            void LoadErrorString_Placeholder();
            void LoadLibrary_Placeholder();
            void GetProcAddress_Placeholder();

            [PreserveSig]
            int GetInterface([In] ref Guid rclsid, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.IUnknown)] out Object ppUnk);

            void IsLoadable_Placeholder();
            void SetDefaultStartupFlags_Placeholder();
            void GetDefaultStartupFlags_Placeholder();

            [PreserveSig]
            int BindAsLegacyV2Runtime();

            // Don't need any other methods.
        }

        [Guid("809c652e-7396-11d2-9771-00a0c9b4d50c")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        private interface IMetaDataDispenser
        {
            void DefineScope_Placeholder();

            [PreserveSig]
            int OpenScope([In, MarshalAs(UnmanagedType.LPWStr)] String szScope, [In] Int32 dwOpenFlags, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.IUnknown)] out Object punk);

            // Don't need any other methods.
        }

        [DllImport("ole32.dll")]
        private static extern int CoCreateInstance(ref Guid rclsid, IntPtr pUnkOuter,
                                           Int32 dwClsContext,
                                           ref Guid riid,
                                           [MarshalAs(UnmanagedType.Interface)] out object ppv);

        private void ThrowExceptionForHR(int hr)
        {
            Marshal.ThrowExceptionForHR(hr, new IntPtr(-1));
        }

        static UnmanagedPdbSymbolReader()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    Guid IID_IUnknown = new Guid(0x00000000, 0x0000, 0x0000, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);

                    ICLRMetaHost objMetaHost;
                    Guid CLSID_CLRMetaHost = new Guid(0x9280188d, 0x0e8e, 0x4867, 0xb3, 0x0c, 0x7f, 0xa8, 0x38, 0x84, 0xe8, 0xde);
                    Guid IID_CLRMetaHost = new Guid(0xd332db9e, 0xb9b3, 0x4125, 0x82, 0x07, 0xa1, 0x48, 0x84, 0xf5, 0x32, 0x16);
                    if (CLRCreateInstance(ref CLSID_CLRMetaHost, ref IID_CLRMetaHost, out objMetaHost) < 0)
                        return;

                    ICLRRuntimeInfo objRuntime;
                    Guid IID_CLRRuntimeInfo = new Guid(0xbd39d1d2, 0xba2f, 0x486a, 0x89, 0xb0, 0xb4, 0xb0, 0xcb, 0x46, 0x68, 0x91);
                    if (objMetaHost.GetRuntime("v4.0.30319", ref IID_CLRRuntimeInfo, out objRuntime) < 0)
                        return;

                    // To get everything from the v4 runtime
                    objRuntime.BindAsLegacyV2Runtime();

                    // Create a COM Metadata dispenser
                    object objDispenser;
                    Guid CLSID_CorMetaDataDispenser = new Guid(0xe5cb7a31, 0x7512, 0x11d2, 0x89, 0xce, 0x00, 0x80, 0xc7, 0x92, 0xe5, 0xd8);
                    if (objRuntime.GetInterface(ref CLSID_CorMetaDataDispenser, ref IID_IUnknown, out objDispenser) < 0)
                        return;
                    s_metadataDispenser = (IMetaDataDispenser)objDispenser;

                    // Create a SymBinder
                    object objBinder;
                    Guid CLSID_CorSymBinder = new Guid(0x0a29ff9e, 0x7f9c, 0x4437, 0x8b, 0x11, 0xf4, 0x24, 0x49, 0x1e, 0x39, 0x31);
                    if (CoCreateInstance(ref CLSID_CorSymBinder,
                                         IntPtr.Zero, // pUnkOuter
                                         1, // CLSCTX_INPROC_SERVER
                                         ref IID_IUnknown,
                                         out objBinder) < 0)
                        return;
                    s_symBinder = (ISymUnmanagedBinder)objBinder;
                }
                catch
                {
                }
            }
        }

        private static IMetaDataDispenser s_metadataDispenser;
        private static ISymUnmanagedBinder s_symBinder;

        public static PdbSymbolReader TryOpenSymbolReaderForMetadataFile(string metadataFileName, string searchPath)
        {
            try
            {
                if (s_metadataDispenser == null || s_symBinder == null)
                    return null;

                Guid IID_IMetaDataImport = new Guid(0x7dac8207, 0xd3ae, 0x4c75, 0x9b, 0x67, 0x92, 0x80, 0x1a, 0x49, 0x7d, 0x44);

                // Open an metadata importer on the given filename. We'll end up passing this importer straight
                // through to the Binder.
                object objImporter;
                if (s_metadataDispenser.OpenScope(metadataFileName, 0x00000010 /* read only */, ref IID_IMetaDataImport, out objImporter) < 0)
                    return null;

                ISymUnmanagedReader reader;
                if (s_symBinder.GetReaderForFile(objImporter, metadataFileName, searchPath, out reader) < 0)
                    return null;

                return new UnmanagedPdbSymbolReader(reader);
            }
            catch
            {
                return null;
            }
        }

        private ISymUnmanagedReader _symUnmanagedReader;

        private UnmanagedPdbSymbolReader(ISymUnmanagedReader symUnmanagedReader)
        {
            _symUnmanagedReader = symUnmanagedReader;
        }

        public override void Dispose()
        {
            Marshal.ReleaseComObject(_symUnmanagedReader);
        }

        private Dictionary<ISymUnmanagedDocument, string> _urlCache;

        private string GetUrl(ISymUnmanagedDocument doc)
        {
            lock (this)
            {
                if (_urlCache == null)
                    _urlCache = new Dictionary<ISymUnmanagedDocument, string>();

                string url;
                if (_urlCache.TryGetValue(doc, out url))
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
            ISymUnmanagedMethod symbolMethod;
            if (_symUnmanagedReader.GetMethod(methodToken, out symbolMethod) < 0)
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
        public override IEnumerable<ILLocalVariable> GetLocalVariableNamesForMethod(int methodToken)
        {
            ISymUnmanagedMethod symbolMethod;
            if (_symUnmanagedReader.GetMethod(methodToken, out symbolMethod) < 0)
                return null;

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
