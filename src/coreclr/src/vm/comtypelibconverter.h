// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Header:  COMTypeLibConverter.h
**
**
** Purpose: Definition of the native methods used by the 
**          typelib converter.
**
** 
===========================================================*/

#ifndef _COMTYPELIBCONVERTER_H
#define _COMTYPELIBCONVERTER_H

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP
#ifndef FEATURE_COMINTEROP_TLB_SUPPORT
#error FEATURE_COMINTEROP_TLB_SUPPORT is required for this file
#endif // FEATURE_COMINTEROP

#include "vars.hpp"

struct ITypeLibImporterNotifySink;
class ImpTlbEventInfo;


enum TlbImporterFlags
{
    TlbImporter_PrimaryInteropAssembly          = 0x00000001,   // Generate a PIA.
    TlbImporter_UnsafeInterfaces                = 0x00000002,   // Generate unsafe interfaces.
    TlbImporter_SafeArrayAsSystemArray          = 0x00000004,   // Safe array import control.
    TlbImporter_TransformDispRetVals            = 0x00000008,   // Disp only itf [out, retval] transformation.
    TlbImporter_PreventClassMembers             = 0x00000010,   // Prevent adding members to class.
    TlbImporter_SerializableValueClasses        = 0x00000020,   // Mark value classes as serializable.
    TlbImporter_ImportAsX86                     = 0x00000100,   // Import to a 32-bit assembly
    TlbImporter_ImportAsX64                     = 0x00000200,   // Import to an x64 assembly
    TlbImporter_ImportAsItanium                 = 0x00000400,   // Import to an itanium assembly
    TlbImporter_ImportAsAgnostic                = 0x00000800,   // Import to an agnostic assembly
    TlbImporter_ReflectionOnlyLoading           = 0x00001000,   // Use ReflectionOnly loading.
    TlbImporter_NoDefineVersionResource         = 0x00002000,   // Don't call AssemblyBuilder.DefineVersionResource
    TlbImporter_ImportAsArm                     = 0x00004000,   // Import to an ARM assembly
    TlbImporter_ValidFlags                      = TlbImporter_PrimaryInteropAssembly | 
                                                  TlbImporter_UnsafeInterfaces | 
                                                  TlbImporter_SafeArrayAsSystemArray |
                                                  TlbImporter_TransformDispRetVals |
                                                  TlbImporter_PreventClassMembers |
                                                  TlbImporter_SerializableValueClasses |
                                                  TlbImporter_ImportAsX86 |
                                                  TlbImporter_ImportAsX64 |
                                                  TlbImporter_ImportAsItanium |
                                                  TlbImporter_ImportAsAgnostic |
                                                  TlbImporter_ReflectionOnlyLoading |
                                                  TlbImporter_NoDefineVersionResource |
                                                  TlbImporter_ImportAsArm
};

// Note that the second hex digit is reserved
enum TlbExporterFlags
{
    TlbExporter_OnlyReferenceRegistered         = 0x00000001,   // Only reference an external typelib if it is registered.
    TlbExporter_CallerResolvedReferences        = 0x00000002,   // Always allow caller to resolve typelib references first
    TlbExporter_OldNames                        = 0x00000004,   // Do not ignore non COM visible types when doing name decoration.
//  TlbExporter_Unused                          = 0x00000008,   // This is currently unused - feel free to use this for another switch
    TlbExporter_ExportAs32Bit                   = 0x00000010,   // Export the type library using 32-bit semantics
    TlbExporter_ExportAs64Bit                   = 0x00000020,   // Export the type library using 64-bit semantics
//  TlbExporter_Reserved                        = 0x00000040,   // Do not use this
//  TlbExporter_Reserved                        = 0x00000080,   // Do not use this
    TlbExporter_ValidFlags                      = TlbExporter_OnlyReferenceRegistered | 
                                                  TlbExporter_CallerResolvedReferences | 
                                                  TlbExporter_OldNames |
                                                  TlbExporter_ExportAs32Bit |
                                                  TlbExporter_ExportAs64Bit
};

#define TlbExportAsMask         0x000000F0
#define TlbExportAs32Bit(x)     ((TlbExportAsMask & x) == TlbExporter_ExportAs32Bit)
#define TlbExportAs64Bit(x)     ((TlbExportAsMask & x) == TlbExporter_ExportAs64Bit)
#define TlbExportAsDefault(x)   ((!TlbExportAs32Bit(x)) && (!TlbExportAs64Bit(x)))

class COMTypeLibConverter
{
public:
    static FCDECL4(Object*, ConvertAssemblyToTypeLib, Object* AssemblyUNSAFE, StringObject* TypeLibNameUNSAFE, DWORD Flags, Object* NotifySinkUNSAFE);
    static FCDECL7(void, ConvertTypeLibToMetadata, Object* TypeLibUNSAFE, Object* AsmBldrUNSAFE, Object* ModBldrUNSAFE, StringObject* NamespaceUNSAFE, TlbImporterFlags Flags, Object* NotifySinkUNSAFE, OBJECTREF* pEventItfInfoList);

private:
    static void             Init();
    static void             CreateItfInfoList(OBJECTREF* pEventItfInfoList);
    static void             GetEventItfInfoList(CImportTlb *pImporter, Assembly *pAssembly, OBJECTREF *pEventItfInfoList);
    static OBJECTREF        GetEventItfInfo(Assembly *pAssembly, ImpTlbEventInfo *pImpTlbEventInfo);
    static void             TypeLibImporterWrapper(ITypeLib *pITLB, LPCWSTR szFname, LPCWSTR szNamespace, IMetaDataEmit *pEmit, Assembly *pAssembly, Module *pModule, ITypeLibImporterNotifySink *pNotify, TlbImporterFlags flags, CImportTlb **ppImporter);

    static void             ConvertAssemblyToTypeLibInternal(OBJECTREF* ppAssembly, STRINGREF* ppTypeLibName, DWORD Flags, OBJECTREF* ppNotifySink, OBJECTREF* pRetObj);
    static void             COMTypeLibConverter::LoadType(Module * pModule,
                                                          mdTypeDef cl,
                                                          TlbImporterFlags Flags);
    static void             ConvertTypeLibToMetadataInternal(OBJECTREF* ppTypeLib, OBJECTREF* ppAsmBldr, OBJECTREF* ppModBldr, STRINGREF* ppNamespace, TlbImporterFlags Flags, OBJECTREF* ppNotifySink, OBJECTREF* pEventItfInfoList);

    static BOOL             m_bInitialized;
};

#endif // _COMTYPELIBCONVERTER_H
