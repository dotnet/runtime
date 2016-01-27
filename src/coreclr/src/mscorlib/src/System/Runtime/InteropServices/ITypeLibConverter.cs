// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Methods used to convert a TypeLib to metadata and vice versa.
**
**
=============================================================================*/

// ***************************************************************************
// *** Note: The following definitions must remain synchronized with the IDL
// ***       in src/inc/TlbImpExp.idl.
// ***************************************************************************

namespace System.Runtime.InteropServices {
    
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

[Serializable]
[Flags()]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum TypeLibImporterFlags 
    {
        None                            = 0x00000000,
        PrimaryInteropAssembly          = 0x00000001,
        UnsafeInterfaces                = 0x00000002,
        SafeArrayAsSystemArray          = 0x00000004,
        TransformDispRetVals            = 0x00000008,
        PreventClassMembers             = 0x00000010,
        SerializableValueClasses        = 0x00000020,
        ImportAsX86                     = 0x00000100,
        ImportAsX64                     = 0x00000200,
        ImportAsItanium                 = 0x00000400,
        ImportAsAgnostic                = 0x00000800,
        ReflectionOnlyLoading           = 0x00001000,
        NoDefineVersionResource         = 0x00002000,
        ImportAsArm                     = 0x00004000,
    }

[Serializable]
[Flags()]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum TypeLibExporterFlags 
    {
        None                            = 0x00000000,
        OnlyReferenceRegistered         = 0x00000001,
        CallerResolvedReferences        = 0x00000002,
        OldNames                        = 0x00000004,
        ExportAs32Bit                   = 0x00000010,
        ExportAs64Bit                   = 0x00000020,        
    }

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum ImporterEventKind
    {
        NOTIF_TYPECONVERTED = 0,
        NOTIF_CONVERTWARNING = 1,
        ERROR_REFTOINVALIDTYPELIB = 2,
    }

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum ExporterEventKind
    {
        NOTIF_TYPECONVERTED = 0,
        NOTIF_CONVERTWARNING = 1,
        ERROR_REFTOINVALIDASSEMBLY = 2
    }

    [GuidAttribute("F1C3BF76-C3E4-11d3-88E7-00902754C43A")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface ITypeLibImporterNotifySink
    {
        void ReportEvent(
                ImporterEventKind eventKind, 
                int eventCode,
                String eventMsg);
        Assembly ResolveRef(
                [MarshalAs(UnmanagedType.Interface)] Object typeLib);
    }

    [GuidAttribute("F1C3BF77-C3E4-11d3-88E7-00902754C43A")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface ITypeLibExporterNotifySink 
    {
        void ReportEvent(
                ExporterEventKind eventKind, 
                int eventCode,
                String eventMsg);

        [return : MarshalAs(UnmanagedType.Interface)]
        Object ResolveRef(
                Assembly assembly);
    }

    [GuidAttribute("F1C3BF78-C3E4-11d3-88E7-00902754C43A")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface ITypeLibConverter
    {
        AssemblyBuilder ConvertTypeLibToAssembly(
                [MarshalAs(UnmanagedType.Interface)] Object typeLib, 
                String asmFileName,
                TypeLibImporterFlags flags, 
                ITypeLibImporterNotifySink notifySink,
                byte[] publicKey,
                StrongNameKeyPair keyPair,
                String asmNamespace,
                Version asmVersion);

        [return : MarshalAs(UnmanagedType.Interface)] 
        Object ConvertAssemblyToTypeLib(
                Assembly assembly, 
                String typeLibName,
                TypeLibExporterFlags flags, 
                ITypeLibExporterNotifySink notifySink);

        bool GetPrimaryInteropAssembly(Guid g, Int32 major, Int32 minor, Int32 lcid, out String asmName, out String asmCodeBase);

        AssemblyBuilder ConvertTypeLibToAssembly([MarshalAs(UnmanagedType.Interface)] Object typeLib, 
                                                String asmFileName,
                                                int flags,
                                                ITypeLibImporterNotifySink notifySink,
                                                byte[] publicKey,
                                                StrongNameKeyPair keyPair,
                                                bool unsafeInterfaces);
    }

    [GuidAttribute("FA1F3615-ACB9-486d-9EAC-1BEF87E36B09")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface ITypeLibExporterNameProvider
    {
        [return : MarshalAs(UnmanagedType.SafeArray, SafeArraySubType=VarEnum.VT_BSTR)] 
        String[] GetNames(); 
    }
}
