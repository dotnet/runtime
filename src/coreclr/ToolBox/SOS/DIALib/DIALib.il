// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly DIALib
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibVersionAttribute::.ctor(int32,
                                                                                                int32) = ( 01 00 02 00 00 00 00 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 31 30 36 31 37 33 41 30 2D 30 31 37 33   // ..$106173A0-0173
                                                                                                  2D 34 45 35 43 2D 38 34 45 37 2D 45 39 31 35 34   // -4E5C-84E7-E9154
                                                                                                  32 32 42 45 39 39 37 00 00 )                      // 22BE997..
  .custom instance void [mscorlib]System.Runtime.InteropServices.ImportedFromTypeLibAttribute::.ctor(string) = ( 01 00 07 44 69 61 32 4C 69 62 00 00 )             // ...Dia2Lib..
  .hash algorithm 0x00008004
  .ver 2:0:0:0
}
.module DIALib.dll
// MVID: {E5EE27AE-42FF-4EFB-B964-A21EE69EDB47}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x05C30000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface public abstract auto ansi import DIALib.IDiaDataSource
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 37 39 46 31 42 42 35 46 2D 42 36 36 45   // ..$79F1BB5F-B66E
                                                                                                  2D 34 38 45 35 2D 42 36 41 39 2D 31 35 34 35 43   // -48E5-B6A9-1545C
                                                                                                  33 32 33 43 41 33 44 00 00 )                      // 323CA3D..
  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_lastError() runtime managed internalcall
  {
  } // end of method IDiaDataSource::get_lastError

  .method public hidebysig newslot abstract virtual 
          instance void  loadDataFromPdb([in] string  marshal( lpwstr) pdbPath) runtime managed internalcall
  {
  } // end of method IDiaDataSource::loadDataFromPdb

  .method public hidebysig newslot abstract virtual 
          instance void  loadAndValidateDataFromPdb([in] string  marshal( lpwstr) pdbPath,
                                                    [in] valuetype [mscorlib]System.Guid& pcsig70,
                                                    [in] uint32 sig,
                                                    [in] uint32 age) runtime managed internalcall
  {
  } // end of method IDiaDataSource::loadAndValidateDataFromPdb

  .method public hidebysig newslot abstract virtual 
          instance void  loadDataForExe([in] string  marshal( lpwstr) executable,
                                        [in] string  marshal( lpwstr) searchPath,
                                        [in] object  marshal( iunknown ) pCallback) runtime managed internalcall
  {
  } // end of method IDiaDataSource::loadDataForExe

  .method public hidebysig newslot abstract virtual 
          instance void  loadDataFromIStream([in] class DIALib.IStream  marshal( interface ) pIStream) runtime managed internalcall
  {
  } // end of method IDiaDataSource::loadDataFromIStream

  .method public hidebysig newslot abstract virtual 
          instance void  openSession([out] class DIALib.IDiaSession&  marshal( interface ) ppSession) runtime managed internalcall
  {
  } // end of method IDiaDataSource::openSession

  .method public hidebysig newslot abstract virtual 
          instance void  loadDataFromCodeViewInfo([in] string  marshal( lpwstr) executable,
                                                  [in] string  marshal( lpwstr) searchPath,
                                                  [in] uint32 cbCvInfo,
                                                  [in] uint8& pbCvInfo,
                                                  [in] object  marshal( iunknown ) pCallback) runtime managed internalcall
  {
  } // end of method IDiaDataSource::loadDataFromCodeViewInfo

  .method public hidebysig newslot abstract virtual 
          instance void  loadDataFromMiscInfo([in] string  marshal( lpwstr) executable,
                                              [in] string  marshal( lpwstr) searchPath,
                                              [in] uint32 timeStampExe,
                                              [in] uint32 timeStampDbg,
                                              [in] uint32 sizeOfExe,
                                              [in] uint32 cbMiscInfo,
                                              [in] uint8& pbMiscInfo,
                                              [in] object  marshal( iunknown ) pCallback) runtime managed internalcall
  {
  } // end of method IDiaDataSource::loadDataFromMiscInfo

  .property string lastError()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaDataSource::get_lastError()
  } // end of property IDiaDataSource::lastError
} // end of class DIALib.IDiaDataSource

.class interface public abstract auto ansi import DIALib.DiaSource
       implements DIALib.IDiaDataSource
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 37 39 46 31 42 42 35 46 2D 42 36 36 45   // ..$79F1BB5F-B66E
                                                                                                  2D 34 38 45 35 2D 42 36 41 39 2D 31 35 34 35 43   // -48E5-B6A9-1545C
                                                                                                  33 32 33 43 41 33 44 00 00 )                      // 323CA3D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 15 44 49 41 4C 69 62 2E 44 69 61 53 6F 75   // ...DIALib.DiaSou
                                                                                                                          72 63 65 43 6C 61 73 73 00 00 )                   // rceClass..
} // end of class DIALib.DiaSource

.class public auto ansi import DIALib.DiaSourceClass
       extends [mscorlib]System.Object
       implements DIALib.IDiaDataSource,
                  DIALib.DiaSource
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.TypeLibTypeFlags) = ( 01 00 02 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 45 36 37 35 36 31 33 35 2D 31 45 36 35   // ..$E6756135-1E65
                                                                                                  2D 34 44 31 37 2D 38 35 37 36 2D 36 31 30 37 36   // -4D17-8576-61076
                                                                                                  31 33 39 38 43 33 43 00 00 )                      // 1398C3C..
  .custom instance void [mscorlib]System.Runtime.InteropServices.ClassInterfaceAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ClassInterfaceType) = ( 01 00 00 00 00 00 00 00 ) 
  .method public specialname rtspecialname 
          instance void  .ctor() runtime managed internalcall
  {
  } // end of method DiaSourceClass::.ctor

  .method public hidebysig newslot specialname virtual 
          instance string 
          marshal( bstr) 
          get_lastError() runtime managed internalcall
  {
    .override DIALib.IDiaDataSource::get_lastError
  } // end of method DiaSourceClass::get_lastError

  .method public hidebysig newslot virtual 
          instance void  loadDataFromPdb([in] string  marshal( lpwstr) pdbPath) runtime managed internalcall
  {
    .override DIALib.IDiaDataSource::loadDataFromPdb
  } // end of method DiaSourceClass::loadDataFromPdb

  .method public hidebysig newslot virtual 
          instance void  loadAndValidateDataFromPdb([in] string  marshal( lpwstr) pdbPath,
                                                    [in] valuetype [mscorlib]System.Guid& pcsig70,
                                                    [in] uint32 sig,
                                                    [in] uint32 age) runtime managed internalcall
  {
    .override DIALib.IDiaDataSource::loadAndValidateDataFromPdb
  } // end of method DiaSourceClass::loadAndValidateDataFromPdb

  .method public hidebysig newslot virtual 
          instance void  loadDataForExe([in] string  marshal( lpwstr) executable,
                                        [in] string  marshal( lpwstr) searchPath,
                                        [in] object  marshal( iunknown ) pCallback) runtime managed internalcall
  {
    .override DIALib.IDiaDataSource::loadDataForExe
  } // end of method DiaSourceClass::loadDataForExe

  .method public hidebysig newslot virtual 
          instance void  loadDataFromIStream([in] class DIALib.IStream  marshal( interface ) pIStream) runtime managed internalcall
  {
    .override DIALib.IDiaDataSource::loadDataFromIStream
  } // end of method DiaSourceClass::loadDataFromIStream

  .method public hidebysig newslot virtual 
          instance void  openSession([out] class DIALib.IDiaSession&  marshal( interface ) ppSession) runtime managed internalcall
  {
    .override DIALib.IDiaDataSource::openSession
  } // end of method DiaSourceClass::openSession

  .method public hidebysig newslot virtual 
          instance void  loadDataFromCodeViewInfo([in] string  marshal( lpwstr) executable,
                                                  [in] string  marshal( lpwstr) searchPath,
                                                  [in] uint32 cbCvInfo,
                                                  [in] uint8& pbCvInfo,
                                                  [in] object  marshal( iunknown ) pCallback) runtime managed internalcall
  {
    .override DIALib.IDiaDataSource::loadDataFromCodeViewInfo
  } // end of method DiaSourceClass::loadDataFromCodeViewInfo

  .method public hidebysig newslot virtual 
          instance void  loadDataFromMiscInfo([in] string  marshal( lpwstr) executable,
                                              [in] string  marshal( lpwstr) searchPath,
                                              [in] uint32 timeStampExe,
                                              [in] uint32 timeStampDbg,
                                              [in] uint32 sizeOfExe,
                                              [in] uint32 cbMiscInfo,
                                              [in] uint8& pbMiscInfo,
                                              [in] object  marshal( iunknown ) pCallback) runtime managed internalcall
  {
    .override DIALib.IDiaDataSource::loadDataFromMiscInfo
  } // end of method DiaSourceClass::loadDataFromMiscInfo

  .property string lastError()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance string DIALib.DiaSourceClass::get_lastError()
  } // end of property DiaSourceClass::lastError
} // end of class DIALib.DiaSourceClass

.class interface public abstract auto ansi import DIALib.DiaSourceAlt
       implements DIALib.IDiaDataSource
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 18 44 49 41 4C 69 62 2E 44 69 61 53 6F 75   // ...DIALib.DiaSou
                                                                                                                          72 63 65 41 6C 74 43 6C 61 73 73 00 00 )          // rceAltClass..
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 37 39 46 31 42 42 35 46 2D 42 36 36 45   // ..$79F1BB5F-B66E
                                                                                                  2D 34 38 45 35 2D 42 36 41 39 2D 31 35 34 35 43   // -48E5-B6A9-1545C
                                                                                                  33 32 33 43 41 33 44 00 00 )                      // 323CA3D..
} // end of class DIALib.DiaSourceAlt

.class public auto ansi import DIALib.DiaSourceAltClass
       extends [mscorlib]System.Object
       implements DIALib.IDiaDataSource,
                  DIALib.DiaSourceAlt
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.TypeLibTypeFlags) = ( 01 00 02 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ClassInterfaceAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ClassInterfaceType) = ( 01 00 00 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 39 31 39 30 34 38 33 31 2D 34 39 43 41   // ..$91904831-49CA
                                                                                                  2D 34 37 36 36 2D 42 39 35 43 2D 32 35 33 39 37   // -4766-B95C-25397
                                                                                                  45 32 44 44 36 44 43 00 00 )                      // E2DD6DC..
  .method public specialname rtspecialname 
          instance void  .ctor() runtime managed internalcall
  {
  } // end of method DiaSourceAltClass::.ctor

  .method public hidebysig newslot specialname virtual 
          instance string 
          marshal( bstr) 
          get_lastError() runtime managed internalcall
  {
    .override DIALib.IDiaDataSource::get_lastError
  } // end of method DiaSourceAltClass::get_lastError

  .method public hidebysig newslot virtual 
          instance void  loadDataFromPdb([in] string  marshal( lpwstr) pdbPath) runtime managed internalcall
  {
    .override DIALib.IDiaDataSource::loadDataFromPdb
  } // end of method DiaSourceAltClass::loadDataFromPdb

  .method public hidebysig newslot virtual 
          instance void  loadAndValidateDataFromPdb([in] string  marshal( lpwstr) pdbPath,
                                                    [in] valuetype [mscorlib]System.Guid& pcsig70,
                                                    [in] uint32 sig,
                                                    [in] uint32 age) runtime managed internalcall
  {
    .override DIALib.IDiaDataSource::loadAndValidateDataFromPdb
  } // end of method DiaSourceAltClass::loadAndValidateDataFromPdb

  .method public hidebysig newslot virtual 
          instance void  loadDataForExe([in] string  marshal( lpwstr) executable,
                                        [in] string  marshal( lpwstr) searchPath,
                                        [in] object  marshal( iunknown ) pCallback) runtime managed internalcall
  {
    .override DIALib.IDiaDataSource::loadDataForExe
  } // end of method DiaSourceAltClass::loadDataForExe

  .method public hidebysig newslot virtual 
          instance void  loadDataFromIStream([in] class DIALib.IStream  marshal( interface ) pIStream) runtime managed internalcall
  {
    .override DIALib.IDiaDataSource::loadDataFromIStream
  } // end of method DiaSourceAltClass::loadDataFromIStream

  .method public hidebysig newslot virtual 
          instance void  openSession([out] class DIALib.IDiaSession&  marshal( interface ) ppSession) runtime managed internalcall
  {
    .override DIALib.IDiaDataSource::openSession
  } // end of method DiaSourceAltClass::openSession

  .method public hidebysig newslot virtual 
          instance void  loadDataFromCodeViewInfo([in] string  marshal( lpwstr) executable,
                                                  [in] string  marshal( lpwstr) searchPath,
                                                  [in] uint32 cbCvInfo,
                                                  [in] uint8& pbCvInfo,
                                                  [in] object  marshal( iunknown ) pCallback) runtime managed internalcall
  {
    .override DIALib.IDiaDataSource::loadDataFromCodeViewInfo
  } // end of method DiaSourceAltClass::loadDataFromCodeViewInfo

  .method public hidebysig newslot virtual 
          instance void  loadDataFromMiscInfo([in] string  marshal( lpwstr) executable,
                                              [in] string  marshal( lpwstr) searchPath,
                                              [in] uint32 timeStampExe,
                                              [in] uint32 timeStampDbg,
                                              [in] uint32 sizeOfExe,
                                              [in] uint32 cbMiscInfo,
                                              [in] uint8& pbMiscInfo,
                                              [in] object  marshal( iunknown ) pCallback) runtime managed internalcall
  {
    .override DIALib.IDiaDataSource::loadDataFromMiscInfo
  } // end of method DiaSourceAltClass::loadDataFromMiscInfo

  .property string lastError()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance string DIALib.DiaSourceAltClass::get_lastError()
  } // end of property DiaSourceAltClass::lastError
} // end of class DIALib.DiaSourceAltClass

.class interface public abstract auto ansi import DIALib.IDiaStackWalker
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 35 34 38 35 32 31 36 42 2D 41 35 34 43   // ..$5485216B-A54C
                                                                                                  2D 34 36 39 46 2D 39 36 37 30 2D 35 32 42 32 34   // -469F-9670-52B24
                                                                                                  44 35 32 32 39 42 42 00 00 )                      // D5229BB..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  getEnumFrames([in] class DIALib.IDiaStackWalkHelper  marshal( interface ) pHelper,
                                       [out] class DIALib.IDiaEnumStackFrames&  marshal( interface ) ppenum) runtime managed internalcall
  {
  } // end of method IDiaStackWalker::getEnumFrames

  .method public hidebysig newslot abstract virtual 
          instance void  getEnumFrames2([in] valuetype DIALib.CV_CPU_TYPE_e cpuid,
                                        [in] class DIALib.IDiaStackWalkHelper  marshal( interface ) pHelper,
                                        [out] class DIALib.IDiaEnumStackFrames&  marshal( interface ) ppenum) runtime managed internalcall
  {
  } // end of method IDiaStackWalker::getEnumFrames2

} // end of class DIALib.IDiaStackWalker

.class interface public abstract auto ansi import DIALib.DiaStackWalker
       implements DIALib.IDiaStackWalker
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 1A 44 49 41 4C 69 62 2E 44 69 61 53 74 61   // ...DIALib.DiaSta
                                                                                                                          63 6B 57 61 6C 6B 65 72 43 6C 61 73 73 00 00 )    // ckWalkerClass..
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 35 34 38 35 32 31 36 42 2D 41 35 34 43   // ..$5485216B-A54C
                                                                                                  2D 34 36 39 46 2D 39 36 37 30 2D 35 32 42 32 34   // -469F-9670-52B24
                                                                                                  44 35 32 32 39 42 42 00 00 )                      // D5229BB..
} // end of class DIALib.DiaStackWalker

.class public auto ansi import DIALib.DiaStackWalkerClass
       extends [mscorlib]System.Object
       implements DIALib.IDiaStackWalker,
                  DIALib.DiaStackWalker
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 45 34 41 38 35 44 42 2D 35 37 36 38   // ..$CE4A85DB-5768
                                                                                                  2D 34 37 35 42 2D 41 34 45 31 2D 43 30 42 43 41   // -475B-A4E1-C0BCA
                                                                                                  32 31 31 32 41 36 42 00 00 )                      // 2112A6B..
  .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.TypeLibTypeFlags) = ( 01 00 02 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ClassInterfaceAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ClassInterfaceType) = ( 01 00 00 00 00 00 00 00 ) 
  .method public specialname rtspecialname 
          instance void  .ctor() runtime managed internalcall
  {
  } // end of method DiaStackWalkerClass::.ctor

  .method public hidebysig newslot virtual 
          instance void  getEnumFrames([in] class DIALib.IDiaStackWalkHelper  marshal( interface ) pHelper,
                                       [out] class DIALib.IDiaEnumStackFrames&  marshal( interface ) ppenum) runtime managed internalcall
  {
    .override DIALib.IDiaStackWalker::getEnumFrames
  } // end of method DiaStackWalkerClass::getEnumFrames

  .method public hidebysig newslot virtual 
          instance void  getEnumFrames2([in] valuetype DIALib.CV_CPU_TYPE_e cpuid,
                                        [in] class DIALib.IDiaStackWalkHelper  marshal( interface ) pHelper,
                                        [out] class DIALib.IDiaEnumStackFrames&  marshal( interface ) ppenum) runtime managed internalcall
  {
    .override DIALib.IDiaStackWalker::getEnumFrames2
  } // end of method DiaStackWalkerClass::getEnumFrames2

} // end of class DIALib.DiaStackWalkerClass

.class interface public abstract auto ansi import DIALib.ISequentialStream
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 43 37 33 33 41 33 30 2D 32 41 31 43   // ..$0C733A30-2A1C
                                                                                                  2D 31 31 43 45 2D 41 44 45 35 2D 30 30 41 41 30   // -11CE-ADE5-00AA0
                                                                                                  30 34 34 37 37 33 44 00 00 )                      // 044773D..
  .method public hidebysig newslot abstract virtual 
          instance void  RemoteRead([out] uint8& pv,
                                    [in] uint32 cb,
                                    [out] uint32& pcbRead) runtime managed internalcall
  {
  } // end of method ISequentialStream::RemoteRead

  .method public hidebysig newslot abstract virtual 
          instance void  RemoteWrite([in] uint8& pv,
                                     [in] uint32 cb,
                                     [out] uint32& pcbWritten) runtime managed internalcall
  {
  } // end of method ISequentialStream::RemoteWrite

} // end of class DIALib.ISequentialStream

.class interface public abstract auto ansi import DIALib.IStream
       implements DIALib.ISequentialStream
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 30 30 30 30 30 30 43 2D 30 30 30 30   // ..$0000000C-0000
                                                                                                  2D 30 30 30 30 2D 43 30 30 30 2D 30 30 30 30 30   // -0000-C000-00000
                                                                                                  30 30 30 30 30 34 36 00 00 )                      // 0000046..
  .method public hidebysig newslot abstract virtual 
          instance void  RemoteRead([out] uint8& pv,
                                    [in] uint32 cb,
                                    [out] uint32& pcbRead) runtime managed internalcall
  {
  } // end of method IStream::RemoteRead

  .method public hidebysig newslot abstract virtual 
          instance void  RemoteWrite([in] uint8& pv,
                                     [in] uint32 cb,
                                     [out] uint32& pcbWritten) runtime managed internalcall
  {
  } // end of method IStream::RemoteWrite

  .method public hidebysig newslot abstract virtual 
          instance void  RemoteSeek([in] valuetype DIALib._LARGE_INTEGER dlibMove,
                                    [in] uint32 dwOrigin,
                                    [out] valuetype DIALib._ULARGE_INTEGER& plibNewPosition) runtime managed internalcall
  {
  } // end of method IStream::RemoteSeek

  .method public hidebysig newslot abstract virtual 
          instance void  SetSize([in] valuetype DIALib._ULARGE_INTEGER libNewSize) runtime managed internalcall
  {
  } // end of method IStream::SetSize

  .method public hidebysig newslot abstract virtual 
          instance void  RemoteCopyTo([in] class DIALib.IStream  marshal( interface ) pstm,
                                      [in] valuetype DIALib._ULARGE_INTEGER cb,
                                      [out] valuetype DIALib._ULARGE_INTEGER& pcbRead,
                                      [out] valuetype DIALib._ULARGE_INTEGER& pcbWritten) runtime managed internalcall
  {
  } // end of method IStream::RemoteCopyTo

  .method public hidebysig newslot abstract virtual 
          instance void  Commit([in] uint32 grfCommitFlags) runtime managed internalcall
  {
  } // end of method IStream::Commit

  .method public hidebysig newslot abstract virtual 
          instance void  Revert() runtime managed internalcall
  {
  } // end of method IStream::Revert

  .method public hidebysig newslot abstract virtual 
          instance void  LockRegion([in] valuetype DIALib._ULARGE_INTEGER libOffset,
                                    [in] valuetype DIALib._ULARGE_INTEGER cb,
                                    [in] uint32 dwLockType) runtime managed internalcall
  {
  } // end of method IStream::LockRegion

  .method public hidebysig newslot abstract virtual 
          instance void  UnlockRegion([in] valuetype DIALib._ULARGE_INTEGER libOffset,
                                      [in] valuetype DIALib._ULARGE_INTEGER cb,
                                      [in] uint32 dwLockType) runtime managed internalcall
  {
  } // end of method IStream::UnlockRegion

  .method public hidebysig newslot abstract virtual 
          instance void  Stat([out] valuetype DIALib.tagSTATSTG& pstatstg,
                              [in] uint32 grfStatFlag) runtime managed internalcall
  {
  } // end of method IStream::Stat

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class DIALib.IStream&  marshal( interface ) ppstm) runtime managed internalcall
  {
  } // end of method IStream::Clone

} // end of class DIALib.IStream

.class interface public abstract auto ansi import DIALib.IDiaSession
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 32 46 36 30 39 45 45 31 2D 44 31 43 38   // ..$2F609EE1-D1C8
                                                                                                  2D 34 45 32 34 2D 38 32 38 38 2D 33 33 32 36 42   // -4E24-8288-3326B
                                                                                                  41 44 43 44 32 31 31 00 00 )                      // ADCD211..
  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_loadAddress() runtime managed internalcall
  {
  } // end of method IDiaSession::get_loadAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_loadAddress([in] uint64 pRetVal) runtime managed internalcall
  {
  } // end of method IDiaSession::set_loadAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_globalScope() runtime managed internalcall
  {
  } // end of method IDiaSession::get_globalScope

  .method public hidebysig newslot abstract virtual 
          instance void  getEnumTables([out] class DIALib.IDiaEnumTables&  marshal( interface ) ppEnumTables) runtime managed internalcall
  {
  } // end of method IDiaSession::getEnumTables

  .method public hidebysig newslot abstract virtual 
          instance void  getSymbolsByAddr([out] class DIALib.IDiaEnumSymbolsByAddr&  marshal( interface ) ppEnumbyAddr) runtime managed internalcall
  {
  } // end of method IDiaSession::getSymbolsByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findChildren([in] class DIALib.IDiaSymbol  marshal( interface ) parent,
                                      [in] valuetype DIALib.SymTagEnum symTag,
                                      [in] string  marshal( lpwstr) name,
                                      [in] uint32 compareFlags,
                                      [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findChildren

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenEx([in] class DIALib.IDiaSymbol  marshal( interface ) parent,
                                        [in] valuetype DIALib.SymTagEnum symTag,
                                        [in] string  marshal( lpwstr) name,
                                        [in] uint32 compareFlags,
                                        [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findChildrenEx

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByAddr([in] class DIALib.IDiaSymbol  marshal( interface ) parent,
                                              [in] valuetype DIALib.SymTagEnum symTag,
                                              [in] string  marshal( lpwstr) name,
                                              [in] uint32 compareFlags,
                                              [in] uint32 isect,
                                              [in] uint32 offset,
                                              [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findChildrenExByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByVA([in] class DIALib.IDiaSymbol  marshal( interface ) parent,
                                            [in] valuetype DIALib.SymTagEnum symTag,
                                            [in] string  marshal( lpwstr) name,
                                            [in] uint32 compareFlags,
                                            [in] uint64 va,
                                            [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findChildrenExByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByRVA([in] class DIALib.IDiaSymbol  marshal( interface ) parent,
                                             [in] valuetype DIALib.SymTagEnum symTag,
                                             [in] string  marshal( lpwstr) name,
                                             [in] uint32 compareFlags,
                                             [in] uint32 rva,
                                             [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findChildrenExByRVA

  .method public hidebysig newslot abstract virtual 
          instance void  findSymbolByAddr([in] uint32 isect,
                                          [in] uint32 offset,
                                          [in] valuetype DIALib.SymTagEnum symTag,
                                          [out] class DIALib.IDiaSymbol&  marshal( interface ) ppSymbol) runtime managed internalcall
  {
  } // end of method IDiaSession::findSymbolByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findSymbolByRVA([in] uint32 rva,
                                         [in] valuetype DIALib.SymTagEnum symTag,
                                         [out] class DIALib.IDiaSymbol&  marshal( interface ) ppSymbol) runtime managed internalcall
  {
  } // end of method IDiaSession::findSymbolByRVA

  .method public hidebysig newslot abstract virtual 
          instance void  findSymbolByVA([in] uint64 va,
                                        [in] valuetype DIALib.SymTagEnum symTag,
                                        [out] class DIALib.IDiaSymbol&  marshal( interface ) ppSymbol) runtime managed internalcall
  {
  } // end of method IDiaSession::findSymbolByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findSymbolByToken([in] uint32 token,
                                           [in] valuetype DIALib.SymTagEnum symTag,
                                           [out] class DIALib.IDiaSymbol&  marshal( interface ) ppSymbol) runtime managed internalcall
  {
  } // end of method IDiaSession::findSymbolByToken

  .method public hidebysig newslot abstract virtual 
          instance void  symsAreEquiv([in] class DIALib.IDiaSymbol  marshal( interface ) symbolA,
                                      [in] class DIALib.IDiaSymbol  marshal( interface ) symbolB) runtime managed internalcall
  {
  } // end of method IDiaSession::symsAreEquiv

  .method public hidebysig newslot abstract virtual 
          instance void  symbolById([in] uint32 id,
                                    [out] class DIALib.IDiaSymbol&  marshal( interface ) ppSymbol) runtime managed internalcall
  {
  } // end of method IDiaSession::symbolById

  .method public hidebysig newslot abstract virtual 
          instance void  findSymbolByRVAEx([in] uint32 rva,
                                           [in] valuetype DIALib.SymTagEnum symTag,
                                           [out] class DIALib.IDiaSymbol&  marshal( interface ) ppSymbol,
                                           [out] int32& displacement) runtime managed internalcall
  {
  } // end of method IDiaSession::findSymbolByRVAEx

  .method public hidebysig newslot abstract virtual 
          instance void  findSymbolByVAEx([in] uint64 va,
                                          [in] valuetype DIALib.SymTagEnum symTag,
                                          [out] class DIALib.IDiaSymbol&  marshal( interface ) ppSymbol,
                                          [out] int32& displacement) runtime managed internalcall
  {
  } // end of method IDiaSession::findSymbolByVAEx

  .method public hidebysig newslot abstract virtual 
          instance void  findFile([in] class DIALib.IDiaSymbol  marshal( interface ) pCompiland,
                                  [in] string  marshal( lpwstr) name,
                                  [in] uint32 compareFlags,
                                  [out] class DIALib.IDiaEnumSourceFiles&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findFile

  .method public hidebysig newslot abstract virtual 
          instance void  findFileById([in] uint32 uniqueId,
                                      [out] class DIALib.IDiaSourceFile&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findFileById

  .method public hidebysig newslot abstract virtual 
          instance void  findLines([in] class DIALib.IDiaSymbol  marshal( interface ) compiland,
                                   [in] class DIALib.IDiaSourceFile  marshal( interface ) file,
                                   [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findLines

  .method public hidebysig newslot abstract virtual 
          instance void  findLinesByAddr([in] uint32 seg,
                                         [in] uint32 offset,
                                         [in] uint32 length,
                                         [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findLinesByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findLinesByRVA([in] uint32 rva,
                                        [in] uint32 length,
                                        [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findLinesByRVA

  .method public hidebysig newslot abstract virtual 
          instance void  findLinesByVA([in] uint64 va,
                                       [in] uint32 length,
                                       [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findLinesByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findLinesByLinenum([in] class DIALib.IDiaSymbol  marshal( interface ) compiland,
                                            [in] class DIALib.IDiaSourceFile  marshal( interface ) file,
                                            [in] uint32 linenum,
                                            [in] uint32 column,
                                            [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findLinesByLinenum

  .method public hidebysig newslot abstract virtual 
          instance void  findInjectedSource([in] string  marshal( lpwstr) srcFile,
                                            [out] class DIALib.IDiaEnumInjectedSources&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findInjectedSource

  .method public hidebysig newslot abstract virtual 
          instance void  getEnumDebugStreams([out] class DIALib.IDiaEnumDebugStreams&  marshal( interface ) ppEnumDebugStreams) runtime managed internalcall
  {
  } // end of method IDiaSession::getEnumDebugStreams

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByAddr([in] class DIALib.IDiaSymbol  marshal( interface ) parent,
                                                [in] uint32 isect,
                                                [in] uint32 offset,
                                                [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findInlineFramesByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByRVA([in] class DIALib.IDiaSymbol  marshal( interface ) parent,
                                               [in] uint32 rva,
                                               [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findInlineFramesByRVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByVA([in] class DIALib.IDiaSymbol  marshal( interface ) parent,
                                              [in] uint64 va,
                                              [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findInlineFramesByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLines([in] class DIALib.IDiaSymbol  marshal( interface ) parent,
                                          [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findInlineeLines

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByAddr([in] class DIALib.IDiaSymbol  marshal( interface ) parent,
                                                [in] uint32 isect,
                                                [in] uint32 offset,
                                                [in] uint32 length,
                                                [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findInlineeLinesByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByRVA([in] class DIALib.IDiaSymbol  marshal( interface ) parent,
                                               [in] uint32 rva,
                                               [in] uint32 length,
                                               [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findInlineeLinesByRVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByVA([in] class DIALib.IDiaSymbol  marshal( interface ) parent,
                                              [in] uint64 va,
                                              [in] uint32 length,
                                              [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findInlineeLinesByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByLinenum([in] class DIALib.IDiaSymbol  marshal( interface ) compiland,
                                                   [in] class DIALib.IDiaSourceFile  marshal( interface ) file,
                                                   [in] uint32 linenum,
                                                   [in] uint32 column,
                                                   [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findInlineeLinesByLinenum

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineesByName([in] string  marshal( lpwstr) name,
                                            [in] uint32 option,
                                            [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findInlineesByName

  .method public hidebysig newslot abstract virtual 
          instance void  findAcceleratorInlineeLinesByLinenum([in] class DIALib.IDiaSymbol  marshal( interface ) parent,
                                                              [in] class DIALib.IDiaSourceFile  marshal( interface ) file,
                                                              [in] uint32 linenum,
                                                              [in] uint32 column,
                                                              [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findAcceleratorInlineeLinesByLinenum

  .method public hidebysig newslot abstract virtual 
          instance void  findSymbolsForAcceleratorPointerTag([in] class DIALib.IDiaSymbol  marshal( interface ) parent,
                                                             [in] uint32 tagValue,
                                                             [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findSymbolsForAcceleratorPointerTag

  .method public hidebysig newslot abstract virtual 
          instance void  findSymbolsByRVAForAcceleratorPointerTag([in] class DIALib.IDiaSymbol  marshal( interface ) parent,
                                                                  [in] uint32 tagValue,
                                                                  [in] uint32 rva,
                                                                  [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findSymbolsByRVAForAcceleratorPointerTag

  .method public hidebysig newslot abstract virtual 
          instance void  findAcceleratorInlineesByName([in] string  marshal( lpwstr) name,
                                                       [in] uint32 option,
                                                       [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findAcceleratorInlineesByName

  .method public hidebysig newslot abstract virtual 
          instance void  addressForVA([in] uint64 va,
                                      [out] uint32& pISect,
                                      [out] uint32& pOffset) runtime managed internalcall
  {
  } // end of method IDiaSession::addressForVA

  .method public hidebysig newslot abstract virtual 
          instance void  addressForRVA([in] uint32 rva,
                                       [out] uint32& pISect,
                                       [out] uint32& pOffset) runtime managed internalcall
  {
  } // end of method IDiaSession::addressForRVA

  .method public hidebysig newslot abstract virtual 
          instance void  findILOffsetsByAddr([in] uint32 isect,
                                             [in] uint32 offset,
                                             [in] uint32 length,
                                             [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findILOffsetsByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findILOffsetsByRVA([in] uint32 rva,
                                            [in] uint32 length,
                                            [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findILOffsetsByRVA

  .method public hidebysig newslot abstract virtual 
          instance void  findILOffsetsByVA([in] uint64 va,
                                           [in] uint32 length,
                                           [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findILOffsetsByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInputAssemblyFiles([out] class DIALib.IDiaEnumInputAssemblyFiles&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findInputAssemblyFiles

  .method public hidebysig newslot abstract virtual 
          instance void  findInputAssembly([in] uint32 index,
                                           [out] class DIALib.IDiaInputAssemblyFile&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findInputAssembly

  .method public hidebysig newslot abstract virtual 
          instance void  findInputAssemblyById([in] uint32 uniqueId,
                                               [out] class DIALib.IDiaInputAssemblyFile&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findInputAssemblyById

  .method public hidebysig newslot abstract virtual 
          instance void  getFuncMDTokenMapSize([out] uint32& pcb) runtime managed internalcall
  {
  } // end of method IDiaSession::getFuncMDTokenMapSize

  .method public hidebysig newslot abstract virtual 
          instance void  getFuncMDTokenMap([in] uint32 cb,
                                           [out] uint32& pcb,
                                           [out] uint8& pb) runtime managed internalcall
  {
  } // end of method IDiaSession::getFuncMDTokenMap

  .method public hidebysig newslot abstract virtual 
          instance void  getTypeMDTokenMapSize([out] uint32& pcb) runtime managed internalcall
  {
  } // end of method IDiaSession::getTypeMDTokenMapSize

  .method public hidebysig newslot abstract virtual 
          instance void  getTypeMDTokenMap([in] uint32 cb,
                                           [out] uint32& pcb,
                                           [out] uint8& pb) runtime managed internalcall
  {
  } // end of method IDiaSession::getTypeMDTokenMap

  .method public hidebysig newslot abstract virtual 
          instance void  getNumberOfFunctionFragments_VA([in] uint64 vaFunc,
                                                         [in] uint32 cbFunc,
                                                         [out] uint32& pNumFragments) runtime managed internalcall
  {
  } // end of method IDiaSession::getNumberOfFunctionFragments_VA

  .method public hidebysig newslot abstract virtual 
          instance void  getNumberOfFunctionFragments_RVA([in] uint32 rvaFunc,
                                                          [in] uint32 cbFunc,
                                                          [out] uint32& pNumFragments) runtime managed internalcall
  {
  } // end of method IDiaSession::getNumberOfFunctionFragments_RVA

  .method public hidebysig newslot abstract virtual 
          instance void  getFunctionFragments_VA([in] uint64 vaFunc,
                                                 [in] uint32 cbFunc,
                                                 [in] uint32 cFragments,
                                                 [out] uint64& pVaFragment,
                                                 [out] uint32& pLenFragment) runtime managed internalcall
  {
  } // end of method IDiaSession::getFunctionFragments_VA

  .method public hidebysig newslot abstract virtual 
          instance void  getFunctionFragments_RVA([in] uint32 rvaFunc,
                                                  [in] uint32 cbFunc,
                                                  [in] uint32 cFragments,
                                                  [out] uint32& pRvaFragment,
                                                  [out] uint32& pLenFragment) runtime managed internalcall
  {
  } // end of method IDiaSession::getFunctionFragments_RVA

  .method public hidebysig newslot abstract virtual 
          instance void  getExports([out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::getExports

  .method public hidebysig newslot abstract virtual 
          instance void  getHeapAllocationSites([out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::getHeapAllocationSites

  .method public hidebysig newslot abstract virtual 
          instance void  findInputAssemblyFile([in] class DIALib.IDiaSymbol  marshal( interface ) pSymbol,
                                               [out] class DIALib.IDiaInputAssemblyFile&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSession::findInputAssemblyFile

  .property uint64 loadAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .set instance void DIALib.IDiaSession::set_loadAddress(uint64)
    .get instance uint64 DIALib.IDiaSession::get_loadAddress()
  } // end of property IDiaSession::loadAddress
  .property class DIALib.IDiaSymbol globalScope()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSession::get_globalScope()
  } // end of property IDiaSession::globalScope
} // end of class DIALib.IDiaSession

.class interface public abstract auto ansi import DIALib.IDiaStackWalkHelper
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 32 31 46 38 31 42 31 42 2D 43 35 42 42   // ..$21F81B1B-C5BB
                                                                                                  2D 34 32 41 33 2D 42 43 34 46 2D 43 43 42 41 41   // -42A3-BC4F-CCBAA
                                                                                                  37 35 42 39 46 31 39 00 00 )                      // 75B9F19..
  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_registerValue([in] uint32 index) runtime managed internalcall
  {
  } // end of method IDiaStackWalkHelper::get_registerValue

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_registerValue([in] uint32 index,
                                           [in] uint64 pRetVal) runtime managed internalcall
  {
  } // end of method IDiaStackWalkHelper::set_registerValue

  .method public hidebysig newslot abstract virtual 
          instance void  readMemory([in] valuetype DIALib.MemoryTypeEnum 'type',
                                    [in] uint64 va,
                                    [in] uint32 cbData,
                                    [out] uint32& pcbData,
                                    [out] uint8& pbData) runtime managed internalcall
  {
  } // end of method IDiaStackWalkHelper::readMemory

  .method public hidebysig newslot abstract virtual 
          instance void  searchForReturnAddress([in] class DIALib.IDiaFrameData  marshal( interface ) frame,
                                                [out] uint64& returnAddress) runtime managed internalcall
  {
  } // end of method IDiaStackWalkHelper::searchForReturnAddress

  .method public hidebysig newslot abstract virtual 
          instance void  searchForReturnAddressStart([in] class DIALib.IDiaFrameData  marshal( interface ) frame,
                                                     [in] uint64 startAddress,
                                                     [out] uint64& returnAddress) runtime managed internalcall
  {
  } // end of method IDiaStackWalkHelper::searchForReturnAddressStart

  .method public hidebysig newslot abstract virtual 
          instance void  frameForVA([in] uint64 va,
                                    [out] class DIALib.IDiaFrameData&  marshal( interface ) ppFrame) runtime managed internalcall
  {
  } // end of method IDiaStackWalkHelper::frameForVA

  .method public hidebysig newslot abstract virtual 
          instance void  symbolForVA([in] uint64 va,
                                     [out] class DIALib.IDiaSymbol&  marshal( interface ) ppSymbol) runtime managed internalcall
  {
  } // end of method IDiaStackWalkHelper::symbolForVA

  .method public hidebysig newslot abstract virtual 
          instance void  pdataForVA([in] uint64 va,
                                    [in] uint32 cbData,
                                    [out] uint32& pcbData,
                                    [out] uint8& pbData) runtime managed internalcall
  {
  } // end of method IDiaStackWalkHelper::pdataForVA

  .method public hidebysig newslot abstract virtual 
          instance void  imageForVA([in] uint64 vaContext,
                                    [out] uint64& pvaImageStart) runtime managed internalcall
  {
  } // end of method IDiaStackWalkHelper::imageForVA

  .method public hidebysig newslot abstract virtual 
          instance void  addressForVA([in] uint64 va,
                                      [out] uint32& pISect,
                                      [out] uint32& pOffset) runtime managed internalcall
  {
  } // end of method IDiaStackWalkHelper::addressForVA

  .method public hidebysig newslot abstract virtual 
          instance void  numberOfFunctionFragmentsForVA([in] uint64 vaFunc,
                                                        [in] uint32 cbFunc,
                                                        [out] uint32& pNumFragments) runtime managed internalcall
  {
  } // end of method IDiaStackWalkHelper::numberOfFunctionFragmentsForVA

  .method public hidebysig newslot abstract virtual 
          instance void  functionFragmentsForVA([in] uint64 vaFunc,
                                                [in] uint32 cbFunc,
                                                [in] uint32 cFragments,
                                                [out] uint64& pVaFragment,
                                                [out] uint32& pLenFragment) runtime managed internalcall
  {
  } // end of method IDiaStackWalkHelper::functionFragmentsForVA

  .property uint64 registerValue(uint32)
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaStackWalkHelper::get_registerValue(uint32)
    .set instance void DIALib.IDiaStackWalkHelper::set_registerValue(uint32,
                                                                     uint64)
  } // end of property IDiaStackWalkHelper::registerValue
} // end of class DIALib.IDiaStackWalkHelper

.class interface public abstract auto ansi import DIALib.IDiaEnumStackFrames
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 45 43 39 44 34 36 31 44 2D 43 45 37 34   // ..$EC9D461D-CE74
                                                                                                  2D 34 37 31 31 2D 41 30 32 30 2D 37 44 38 46 39   // -4711-A020-7D8F9
                                                                                                  41 31 44 44 32 35 35 00 00 )                      // A1DD255..
  .method public hidebysig newslot abstract virtual 
          instance void  Next([in] uint32 celt,
                              [out] class DIALib.IDiaStackFrame&  marshal( interface ) rgelt,
                              [out] uint32& pceltFetched) runtime managed internalcall
  {
  } // end of method IDiaEnumStackFrames::Next

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method IDiaEnumStackFrames::Reset

} // end of class DIALib.IDiaEnumStackFrames

.class public auto ansi sealed DIALib.CV_CPU_TYPE_e
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_8080 = int32(0x00000000)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_8086 = int32(0x00000001)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_80286 = int32(0x00000002)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_80386 = int32(0x00000003)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_80486 = int32(0x00000004)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_PENTIUM = int32(0x00000005)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_PENTIUMII = int32(0x00000006)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_PENTIUMPRO = int32(0x00000006)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_PENTIUMIII = int32(0x00000007)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_MIPS = int32(0x00000010)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_MIPSR4000 = int32(0x00000010)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_MIPS16 = int32(0x00000011)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_MIPS32 = int32(0x00000012)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_MIPS64 = int32(0x00000013)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_MIPSI = int32(0x00000014)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_MIPSII = int32(0x00000015)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_MIPSIII = int32(0x00000016)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_MIPSIV = int32(0x00000017)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_MIPSV = int32(0x00000018)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_M68000 = int32(0x00000020)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_M68010 = int32(0x00000021)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_M68020 = int32(0x00000022)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_M68030 = int32(0x00000023)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_M68040 = int32(0x00000024)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_ALPHA = int32(0x00000030)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_ALPHA_21064 = int32(0x00000030)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_ALPHA_21164 = int32(0x00000031)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_ALPHA_21164A = int32(0x00000032)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_ALPHA_21264 = int32(0x00000033)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_ALPHA_21364 = int32(0x00000034)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_PPC601 = int32(0x00000040)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_PPC603 = int32(0x00000041)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_PPC604 = int32(0x00000042)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_PPC620 = int32(0x00000043)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_PPCFP = int32(0x00000044)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_PPCBE = int32(0x00000045)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_SH3 = int32(0x00000050)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_SH3E = int32(0x00000051)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_SH3DSP = int32(0x00000052)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_SH4 = int32(0x00000053)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_SHMEDIA = int32(0x00000054)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_ARM3 = int32(0x00000060)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_ARM4 = int32(0x00000061)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_ARM4T = int32(0x00000062)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_ARM5 = int32(0x00000063)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_ARM5T = int32(0x00000064)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_ARM6 = int32(0x00000065)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_ARM_XMAC = int32(0x00000066)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_ARM_WMMX = int32(0x00000067)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_ARM7 = int32(0x00000068)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_OMNI = int32(0x00000070)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_IA64 = int32(0x00000080)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_IA64_1 = int32(0x00000080)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_IA64_2 = int32(0x00000081)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_CEE = int32(0x00000090)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_AM33 = int32(0x000000A0)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_M32R = int32(0x000000B0)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_TRICORE = int32(0x000000C0)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_X64 = int32(0x000000D0)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_AMD64 = int32(0x000000D0)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_EBC = int32(0x000000E0)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_THUMB = int32(0x000000F0)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_ARMNT = int32(0x000000F4)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_ARM64 = int32(0x000000F6)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_HYBRID_X86_ARM64 = int32(0x000000F7)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_ARM64EC = int32(0x000000F8)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_ARM64X = int32(0x000000F9)
  .field public static literal valuetype DIALib.CV_CPU_TYPE_e CV_CFL_D3D11_SHADER = int32(0x00000100)
} // end of class DIALib.CV_CPU_TYPE_e

.class public sequential ansi sealed beforefieldinit DIALib._LARGE_INTEGER
       extends [mscorlib]System.ValueType
{
  .pack 8
  .size 0
  .field public int64 QuadPart
} // end of class DIALib._LARGE_INTEGER

.class public sequential ansi sealed beforefieldinit DIALib._ULARGE_INTEGER
       extends [mscorlib]System.ValueType
{
  .pack 8
  .size 0
  .field public uint64 QuadPart
} // end of class DIALib._ULARGE_INTEGER

.class public sequential ansi sealed beforefieldinit DIALib.tagSTATSTG
       extends [mscorlib]System.ValueType
{
  .pack 8
  .size 0
  .field public  marshal( lpwstr) string pwcsName
  .field public uint32 'type'
  .field public valuetype DIALib._ULARGE_INTEGER cbSize
  .field public valuetype DIALib._FILETIME mtime
  .field public valuetype DIALib._FILETIME ctime
  .field public valuetype DIALib._FILETIME atime
  .field public uint32 grfMode
  .field public uint32 grfLocksSupported
  .field public valuetype [mscorlib]System.Guid 'clsid'
  .field public uint32 grfStateBits
  .field public uint32 reserved
} // end of class DIALib.tagSTATSTG

.class public sequential ansi sealed beforefieldinit DIALib._FILETIME
       extends [mscorlib]System.ValueType
{
  .pack 4
  .size 0
  .field public uint32 dwLowDateTime
  .field public uint32 dwHighDateTime
} // end of class DIALib._FILETIME

.class interface public abstract auto ansi import DIALib.IDiaSymbol
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 0A 73 79 6D 49 6E 64 65 78 49 64 00 00 )    // ...symIndexId..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 42 37 38 37 42 32 46 2D 42 44 36 43   // ..$CB787B2F-BD6C
                                                                                                  2D 34 36 33 35 2D 42 41 35 32 2D 39 33 33 31 32   // -4635-BA52-93312
                                                                                                  36 42 44 32 44 43 44 00 00 )                      // 6BD2DCD..
  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_symIndexId() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_symIndexId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_symTag() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_symTag

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_name() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_name

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_lexicalParent() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_lexicalParent

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_classParent() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_classParent

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_type() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_type

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_dataKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_dataKind

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_locationType() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_locationType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_addressSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_addressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_addressOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_addressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_relativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_relativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_virtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_virtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_registerId() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_registerId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_offset() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_offset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_length() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_length

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_slot() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_slot

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_volatileType() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_volatileType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_constType() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_constType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_unalignedType() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_unalignedType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_access() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_access

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_libraryName() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_libraryName

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_platform() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_platform

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_language() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_language

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_editAndContinueEnabled() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_editAndContinueEnabled

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndMajor() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_frontEndMajor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndMinor() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_frontEndMinor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndBuild() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_frontEndBuild

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndMajor() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_backEndMajor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndMinor() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_backEndMinor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndBuild() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_backEndBuild

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_sourceFileName() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_sourceFileName

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_unused() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_unused

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_thunkOrdinal() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_thunkOrdinal

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_thisAdjust() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_thisAdjust

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_virtualBaseOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_virtualBaseOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_virtual() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_virtual

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_intro() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_intro

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_pure() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_pure

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_callingConvention() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_callingConvention

  .method public hidebysig newslot specialname abstract virtual 
          instance object 
          marshal( struct) 
          get_value() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_value

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseType() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_baseType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_token() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_token

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_timeStamp() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_timeStamp

  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype [mscorlib]System.Guid 
          get_guid() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_guid

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_symbolsFileName() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_symbolsFileName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_reference() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_reference

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_count() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_count

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bitPosition() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_bitPosition

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_arrayIndexType() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_arrayIndexType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_packed() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_packed

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_constructor() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_constructor

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_overloadedOperator() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_overloadedOperator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_nested() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_nested

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasNestedTypes() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_hasNestedTypes

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasAssignmentOperator() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_hasAssignmentOperator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasCastOperator() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_hasCastOperator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_scoped() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_scoped

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_virtualBaseClass() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_virtualBaseClass

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_indirectVirtualBaseClass() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_indirectVirtualBaseClass

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_virtualBasePointerOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_virtualBasePointerOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_virtualTableShape() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_virtualTableShape

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lexicalParentId() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_lexicalParentId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_classParentId() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_classParentId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_typeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_typeId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_arrayIndexTypeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_arrayIndexTypeId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_virtualTableShapeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_virtualTableShapeId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_code() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_code

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_function() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_function

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_managed() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_managed

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_msil() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_msil

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_virtualBaseDispIndex() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_virtualBaseDispIndex

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_undecoratedName() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_undecoratedName

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_age() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_age

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_signature() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_signature

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_compilerGenerated() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_compilerGenerated

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_addressTaken() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_addressTaken

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_rank() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_rank

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_lowerBound() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_lowerBound

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_upperBound() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_upperBound

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lowerBoundId() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_lowerBoundId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_upperBoundId() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_upperBoundId

  .method public hidebysig newslot abstract virtual 
          instance void  get_dataBytes([in] uint32 cbData,
                                       [out] uint32& pcbData,
                                       [out] uint8& pbData) runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_dataBytes

  .method public hidebysig newslot abstract virtual 
          instance void  findChildren([in] valuetype DIALib.SymTagEnum symTag,
                                      [in] string  marshal( lpwstr) name,
                                      [in] uint32 compareFlags,
                                      [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol::findChildren

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenEx([in] valuetype DIALib.SymTagEnum symTag,
                                        [in] string  marshal( lpwstr) name,
                                        [in] uint32 compareFlags,
                                        [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol::findChildrenEx

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByAddr([in] valuetype DIALib.SymTagEnum symTag,
                                              [in] string  marshal( lpwstr) name,
                                              [in] uint32 compareFlags,
                                              [in] uint32 isect,
                                              [in] uint32 offset,
                                              [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol::findChildrenExByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByVA([in] valuetype DIALib.SymTagEnum symTag,
                                            [in] string  marshal( lpwstr) name,
                                            [in] uint32 compareFlags,
                                            [in] uint64 va,
                                            [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol::findChildrenExByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByRVA([in] valuetype DIALib.SymTagEnum symTag,
                                             [in] string  marshal( lpwstr) name,
                                             [in] uint32 compareFlags,
                                             [in] uint32 rva,
                                             [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol::findChildrenExByRVA

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_targetSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_targetSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_targetOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_targetOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_targetRelativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_targetRelativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_targetVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_targetVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_machineType() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_machineType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_oemId() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_oemId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_oemSymbolId() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_oemSymbolId

  .method public hidebysig newslot abstract virtual 
          instance void  get_types([in] uint32 cTypes,
                                   [out] uint32& pcTypes,
                                   [out] class DIALib.IDiaSymbol&  marshal( interface ) pTypes) runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_types

  .method public hidebysig newslot abstract virtual 
          instance void  get_typeIds([in] uint32 cTypeIds,
                                     [out] uint32& pcTypeIds,
                                     [out] uint32& pdwTypeIds) runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_typeIds

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_objectPointerType() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_objectPointerType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_udtKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_udtKind

  .method public hidebysig newslot abstract virtual 
          instance void  get_undecoratedNameEx([in] uint32 undecorateOptions,
                                               [out] string&  marshal( bstr) name) runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_undecoratedNameEx

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noReturn() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_noReturn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_customCallingConvention() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_customCallingConvention

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noInline() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_noInline

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_optimizedCodeDebugInfo() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_optimizedCodeDebugInfo

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_notReached() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_notReached

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_interruptReturn() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_interruptReturn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_farReturn() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_farReturn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isStatic() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isStatic

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasDebugInfo() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_hasDebugInfo

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isLTCG() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isLTCG

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isDataAligned() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isDataAligned

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasSecurityChecks() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_hasSecurityChecks

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_compilerName() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_compilerName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasAlloca() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_hasAlloca

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasSetJump() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_hasSetJump

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasLongJump() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_hasLongJump

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasInlAsm() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_hasInlAsm

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasEH() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_hasEH

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasSEH() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_hasSEH

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasEHa() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_hasEHa

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isNaked() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isNaked

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAggregated() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isAggregated

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSplitted() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isSplitted

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_container() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_container

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_inlSpec() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_inlSpec

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noStackOrdering() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_noStackOrdering

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_virtualBaseTableType() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_virtualBaseTableType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasManagedCode() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_hasManagedCode

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isHotpatchable() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isHotpatchable

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isCVTCIL() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isCVTCIL

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isMSILNetmodule() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isMSILNetmodule

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isCTypes() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isCTypes

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isStripped() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isStripped

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndQFE() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_frontEndQFE

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndQFE() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_backEndQFE

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_wasInlined() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_wasInlined

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_strictGSCheck() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_strictGSCheck

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isCxxReturnUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isCxxReturnUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isConstructorVirtualBase() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isConstructorVirtualBase

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_RValueReference() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_RValueReference

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_unmodifiedType() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_unmodifiedType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_framePointerPresent() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_framePointerPresent

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSafeBuffers() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isSafeBuffers

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_intrinsic() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_intrinsic

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_sealed() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_sealed

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hfaFloat() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_hfaFloat

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hfaDouble() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_hfaDouble

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_liveRangeStartAddressSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_liveRangeStartAddressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_liveRangeStartAddressOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_liveRangeStartAddressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_liveRangeStartRelativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_liveRangeStartRelativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_countLiveRanges() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_countLiveRanges

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_liveRangeLength() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_liveRangeLength

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_offsetInUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_offsetInUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_paramBasePointerRegisterId() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_paramBasePointerRegisterId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_localBasePointerRegisterId() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_localBasePointerRegisterId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isLocationControlFlowDependent() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isLocationControlFlowDependent

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_stride() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_stride

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfRows() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_numberOfRows

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfColumns() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_numberOfColumns

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isMatrixRowMajor() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isMatrixRowMajor

  .method public hidebysig newslot abstract virtual 
          instance void  get_numericProperties([in] uint32 cnt,
                                               [out] uint32& pcnt,
                                               [out] uint32& pProperties) runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_numericProperties

  .method public hidebysig newslot abstract virtual 
          instance void  get_modifierValues([in] uint32 cnt,
                                            [out] uint32& pcnt,
                                            [out] uint16& pModifiers) runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_modifierValues

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isReturnValue() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isReturnValue

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isOptimizedAway() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isOptimizedAway

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_builtInKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_builtInKind

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_registerType() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_registerType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseDataSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_baseDataSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseDataOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_baseDataOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_textureSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_textureSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_samplerSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_samplerSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_uavSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_uavSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_sizeInUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_sizeInUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_memorySpaceKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_memorySpaceKind

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_unmodifiedTypeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_unmodifiedTypeId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_subTypeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_subTypeId

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_subType() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_subType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfModifiers() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_numberOfModifiers

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfRegisterIndices() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_numberOfRegisterIndices

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isHLSLData() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isHLSLData

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPointerToDataMember() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isPointerToDataMember

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPointerToMemberFunction() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isPointerToMemberFunction

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSingleInheritance() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isSingleInheritance

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isMultipleInheritance() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isMultipleInheritance

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isVirtualInheritance() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isVirtualInheritance

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_restrictedType() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_restrictedType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPointerBasedOnSymbolValue() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isPointerBasedOnSymbolValue

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_baseSymbol() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_baseSymbol

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseSymbolId() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_baseSymbolId

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_objectFileName() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_objectFileName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAcceleratorGroupSharedLocal() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isAcceleratorGroupSharedLocal

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAcceleratorPointerTagLiveRange() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isAcceleratorPointerTagLiveRange

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAcceleratorStubFunction() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isAcceleratorStubFunction

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfAcceleratorPointerTags() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_numberOfAcceleratorPointerTags

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSdl() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isSdl

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isWinRTPointer() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isWinRTPointer

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isRefUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isRefUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isValueUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isValueUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isInterfaceUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isInterfaceUdt

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByAddr([in] uint32 isect,
                                                [in] uint32 offset,
                                                [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol::findInlineFramesByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByRVA([in] uint32 rva,
                                               [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol::findInlineFramesByRVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByVA([in] uint64 va,
                                              [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol::findInlineFramesByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLines([out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol::findInlineeLines

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByAddr([in] uint32 isect,
                                                [in] uint32 offset,
                                                [in] uint32 length,
                                                [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol::findInlineeLinesByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByRVA([in] uint32 rva,
                                               [in] uint32 length,
                                               [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol::findInlineeLinesByRVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByVA([in] uint64 va,
                                              [in] uint32 length,
                                              [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol::findInlineeLinesByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findSymbolsForAcceleratorPointerTag([in] uint32 tagValue,
                                                             [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol::findSymbolsForAcceleratorPointerTag

  .method public hidebysig newslot abstract virtual 
          instance void  findSymbolsByRVAForAcceleratorPointerTag([in] uint32 tagValue,
                                                                  [in] uint32 rva,
                                                                  [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol::findSymbolsByRVAForAcceleratorPointerTag

  .method public hidebysig newslot abstract virtual 
          instance void  get_acceleratorPointerTags([in] uint32 cnt,
                                                    [out] uint32& pcnt,
                                                    [out] uint32& pPointerTags) runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_acceleratorPointerTags

  .method public hidebysig newslot abstract virtual 
          instance void  getSrcLineOnTypeDefn([out] class DIALib.IDiaLineNumber&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol::getSrcLineOnTypeDefn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPGO() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isPGO

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasValidPGOCounts() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_hasValidPGOCounts

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isOptimizedForSpeed() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_isOptimizedForSpeed

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_PGOEntryCount() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_PGOEntryCount

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_PGOEdgeCount() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_PGOEdgeCount

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_PGODynamicInstructionCount() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_PGODynamicInstructionCount

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_staticSize() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_staticSize

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_finalLiveStaticSize() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_finalLiveStaticSize

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_phaseName() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_phaseName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasControlFlowCheck() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_hasControlFlowCheck

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_constantExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_constantExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_dataExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_dataExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_privateExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_privateExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noNameExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_noNameExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_exportHasExplicitlyAssignedOrdinal() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_exportHasExplicitlyAssignedOrdinal

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_exportIsForwarder() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_exportIsForwarder

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_ordinal() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_ordinal

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frameSize() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_frameSize

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_exceptionHandlerAddressSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_exceptionHandlerAddressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_exceptionHandlerAddressOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_exceptionHandlerAddressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_exceptionHandlerRelativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_exceptionHandlerRelativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_exceptionHandlerVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_exceptionHandlerVirtualAddress

  .method public hidebysig newslot abstract virtual 
          instance void  findInputAssemblyFile([out] class DIALib.IDiaInputAssemblyFile&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol::findInputAssemblyFile

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_characteristics() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_characteristics

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_coffGroup() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_coffGroup

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bindID() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_bindID

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bindSpace() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_bindSpace

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bindSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol::get_bindSlot

  .property uint32 symIndexId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_symIndexId()
  } // end of property IDiaSymbol::symIndexId
  .property uint32 symTag()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_symTag()
  } // end of property IDiaSymbol::symTag
  .property string name()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol::get_name()
  } // end of property IDiaSymbol::name
  .property class DIALib.IDiaSymbol lexicalParent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol::get_lexicalParent()
  } // end of property IDiaSymbol::lexicalParent
  .property class DIALib.IDiaSymbol classParent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 04 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol::get_classParent()
  } // end of property IDiaSymbol::classParent
  .property class DIALib.IDiaSymbol 'type'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 05 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol::get_type()
  } // end of property IDiaSymbol::'type'
  .property uint32 dataKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 06 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_dataKind()
  } // end of property IDiaSymbol::dataKind
  .property uint32 locationType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 07 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_locationType()
  } // end of property IDiaSymbol::locationType
  .property uint32 addressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_addressSection()
  } // end of property IDiaSymbol::addressSection
  .property uint32 addressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 09 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_addressOffset()
  } // end of property IDiaSymbol::addressOffset
  .property uint32 relativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0A 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_relativeVirtualAddress()
  } // end of property IDiaSymbol::relativeVirtualAddress
  .property uint64 virtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0B 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol::get_virtualAddress()
  } // end of property IDiaSymbol::virtualAddress
  .property uint32 registerId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_registerId()
  } // end of property IDiaSymbol::registerId
  .property int32 offset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0D 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_offset()
  } // end of property IDiaSymbol::offset
  .property uint64 length()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0E 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol::get_length()
  } // end of property IDiaSymbol::length
  .property uint32 slot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_slot()
  } // end of property IDiaSymbol::slot
  .property int32 volatileType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 10 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_volatileType()
  } // end of property IDiaSymbol::volatileType
  .property int32 constType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 11 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_constType()
  } // end of property IDiaSymbol::constType
  .property int32 unalignedType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 12 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_unalignedType()
  } // end of property IDiaSymbol::unalignedType
  .property uint32 access()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 13 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_access()
  } // end of property IDiaSymbol::access
  .property string libraryName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 14 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol::get_libraryName()
  } // end of property IDiaSymbol::libraryName
  .property uint32 platform()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 15 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_platform()
  } // end of property IDiaSymbol::platform
  .property uint32 language()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 16 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_language()
  } // end of property IDiaSymbol::language
  .property int32 editAndContinueEnabled()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 17 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_editAndContinueEnabled()
  } // end of property IDiaSymbol::editAndContinueEnabled
  .property uint32 frontEndMajor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 18 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_frontEndMajor()
  } // end of property IDiaSymbol::frontEndMajor
  .property uint32 frontEndMinor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 19 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_frontEndMinor()
  } // end of property IDiaSymbol::frontEndMinor
  .property uint32 frontEndBuild()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1A 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_frontEndBuild()
  } // end of property IDiaSymbol::frontEndBuild
  .property uint32 backEndMajor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1B 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_backEndMajor()
  } // end of property IDiaSymbol::backEndMajor
  .property uint32 backEndMinor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_backEndMinor()
  } // end of property IDiaSymbol::backEndMinor
  .property uint32 backEndBuild()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1D 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_backEndBuild()
  } // end of property IDiaSymbol::backEndBuild
  .property string sourceFileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1E 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol::get_sourceFileName()
  } // end of property IDiaSymbol::sourceFileName
  .property string 'unused'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1F 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol::get_unused()
  } // end of property IDiaSymbol::'unused'
  .property uint32 thunkOrdinal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 20 00 00 00 00 00 )                         // .. .....
    .get instance uint32 DIALib.IDiaSymbol::get_thunkOrdinal()
  } // end of property IDiaSymbol::thunkOrdinal
  .property int32 thisAdjust()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 21 00 00 00 00 00 )                         // ..!.....
    .get instance int32 DIALib.IDiaSymbol::get_thisAdjust()
  } // end of property IDiaSymbol::thisAdjust
  .property uint32 virtualBaseOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 22 00 00 00 00 00 )                         // ..".....
    .get instance uint32 DIALib.IDiaSymbol::get_virtualBaseOffset()
  } // end of property IDiaSymbol::virtualBaseOffset
  .property int32 'virtual'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 23 00 00 00 00 00 )                         // ..#.....
    .get instance int32 DIALib.IDiaSymbol::get_virtual()
  } // end of property IDiaSymbol::'virtual'
  .property int32 intro()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 24 00 00 00 00 00 )                         // ..$.....
    .get instance int32 DIALib.IDiaSymbol::get_intro()
  } // end of property IDiaSymbol::intro
  .property int32 pure()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 25 00 00 00 00 00 )                         // ..%.....
    .get instance int32 DIALib.IDiaSymbol::get_pure()
  } // end of property IDiaSymbol::pure
  .property uint32 callingConvention()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 26 00 00 00 00 00 )                         // ..&.....
    .get instance uint32 DIALib.IDiaSymbol::get_callingConvention()
  } // end of property IDiaSymbol::callingConvention
  .property object 'value'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 27 00 00 00 00 00 )                         // ..'.....
    .get instance object DIALib.IDiaSymbol::get_value()
  } // end of property IDiaSymbol::'value'
  .property uint32 baseType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 28 00 00 00 00 00 )                         // ..(.....
    .get instance uint32 DIALib.IDiaSymbol::get_baseType()
  } // end of property IDiaSymbol::baseType
  .property uint32 token()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 29 00 00 00 00 00 )                         // ..).....
    .get instance uint32 DIALib.IDiaSymbol::get_token()
  } // end of property IDiaSymbol::token
  .property uint32 timeStamp()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2A 00 00 00 00 00 )                         // ..*.....
    .get instance uint32 DIALib.IDiaSymbol::get_timeStamp()
  } // end of property IDiaSymbol::timeStamp
  .property valuetype [mscorlib]System.Guid
          guid()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2B 00 00 00 00 00 )                         // ..+.....
    .get instance valuetype [mscorlib]System.Guid DIALib.IDiaSymbol::get_guid()
  } // end of property IDiaSymbol::guid
  .property string symbolsFileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2C 00 00 00 00 00 )                         // ..,.....
    .get instance string DIALib.IDiaSymbol::get_symbolsFileName()
  } // end of property IDiaSymbol::symbolsFileName
  .property int32 reference()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2E 00 00 00 00 00 )                         // ........
    .get instance int32 DIALib.IDiaSymbol::get_reference()
  } // end of property IDiaSymbol::reference
  .property uint32 count()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2F 00 00 00 00 00 )                         // ../.....
    .get instance uint32 DIALib.IDiaSymbol::get_count()
  } // end of property IDiaSymbol::count
  .property uint32 bitPosition()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 31 00 00 00 00 00 )                         // ..1.....
    .get instance uint32 DIALib.IDiaSymbol::get_bitPosition()
  } // end of property IDiaSymbol::bitPosition
  .property class DIALib.IDiaSymbol arrayIndexType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 32 00 00 00 00 00 )                         // ..2.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol::get_arrayIndexType()
  } // end of property IDiaSymbol::arrayIndexType
  .property int32 packed()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 33 00 00 00 00 00 )                         // ..3.....
    .get instance int32 DIALib.IDiaSymbol::get_packed()
  } // end of property IDiaSymbol::packed
  .property int32 constructor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 34 00 00 00 00 00 )                         // ..4.....
    .get instance int32 DIALib.IDiaSymbol::get_constructor()
  } // end of property IDiaSymbol::constructor
  .property int32 overloadedOperator()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 35 00 00 00 00 00 )                         // ..5.....
    .get instance int32 DIALib.IDiaSymbol::get_overloadedOperator()
  } // end of property IDiaSymbol::overloadedOperator
  .property int32 'nested'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 36 00 00 00 00 00 )                         // ..6.....
    .get instance int32 DIALib.IDiaSymbol::get_nested()
  } // end of property IDiaSymbol::'nested'
  .property int32 hasNestedTypes()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 37 00 00 00 00 00 )                         // ..7.....
    .get instance int32 DIALib.IDiaSymbol::get_hasNestedTypes()
  } // end of property IDiaSymbol::hasNestedTypes
  .property int32 hasAssignmentOperator()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 38 00 00 00 00 00 )                         // ..8.....
    .get instance int32 DIALib.IDiaSymbol::get_hasAssignmentOperator()
  } // end of property IDiaSymbol::hasAssignmentOperator
  .property int32 hasCastOperator()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 39 00 00 00 00 00 )                         // ..9.....
    .get instance int32 DIALib.IDiaSymbol::get_hasCastOperator()
  } // end of property IDiaSymbol::hasCastOperator
  .property int32 scoped()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3A 00 00 00 00 00 )                         // ..:.....
    .get instance int32 DIALib.IDiaSymbol::get_scoped()
  } // end of property IDiaSymbol::scoped
  .property int32 virtualBaseClass()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3B 00 00 00 00 00 )                         // ..;.....
    .get instance int32 DIALib.IDiaSymbol::get_virtualBaseClass()
  } // end of property IDiaSymbol::virtualBaseClass
  .property int32 indirectVirtualBaseClass()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3C 00 00 00 00 00 )                         // ..<.....
    .get instance int32 DIALib.IDiaSymbol::get_indirectVirtualBaseClass()
  } // end of property IDiaSymbol::indirectVirtualBaseClass
  .property int32 virtualBasePointerOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3D 00 00 00 00 00 )                         // ..=.....
    .get instance int32 DIALib.IDiaSymbol::get_virtualBasePointerOffset()
  } // end of property IDiaSymbol::virtualBasePointerOffset
  .property class DIALib.IDiaSymbol virtualTableShape()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3E 00 00 00 00 00 )                         // ..>.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol::get_virtualTableShape()
  } // end of property IDiaSymbol::virtualTableShape
  .property uint32 lexicalParentId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 40 00 00 00 00 00 )                         // ..@.....
    .get instance uint32 DIALib.IDiaSymbol::get_lexicalParentId()
  } // end of property IDiaSymbol::lexicalParentId
  .property uint32 classParentId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 41 00 00 00 00 00 )                         // ..A.....
    .get instance uint32 DIALib.IDiaSymbol::get_classParentId()
  } // end of property IDiaSymbol::classParentId
  .property uint32 typeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 42 00 00 00 00 00 )                         // ..B.....
    .get instance uint32 DIALib.IDiaSymbol::get_typeId()
  } // end of property IDiaSymbol::typeId
  .property uint32 arrayIndexTypeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 43 00 00 00 00 00 )                         // ..C.....
    .get instance uint32 DIALib.IDiaSymbol::get_arrayIndexTypeId()
  } // end of property IDiaSymbol::arrayIndexTypeId
  .property uint32 virtualTableShapeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 44 00 00 00 00 00 )                         // ..D.....
    .get instance uint32 DIALib.IDiaSymbol::get_virtualTableShapeId()
  } // end of property IDiaSymbol::virtualTableShapeId
  .property int32 code()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 45 00 00 00 00 00 )                         // ..E.....
    .get instance int32 DIALib.IDiaSymbol::get_code()
  } // end of property IDiaSymbol::code
  .property int32 function()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 46 00 00 00 00 00 )                         // ..F.....
    .get instance int32 DIALib.IDiaSymbol::get_function()
  } // end of property IDiaSymbol::function
  .property int32 'managed'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 47 00 00 00 00 00 )                         // ..G.....
    .get instance int32 DIALib.IDiaSymbol::get_managed()
  } // end of property IDiaSymbol::'managed'
  .property int32 msil()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 48 00 00 00 00 00 )                         // ..H.....
    .get instance int32 DIALib.IDiaSymbol::get_msil()
  } // end of property IDiaSymbol::msil
  .property uint32 virtualBaseDispIndex()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 49 00 00 00 00 00 )                         // ..I.....
    .get instance uint32 DIALib.IDiaSymbol::get_virtualBaseDispIndex()
  } // end of property IDiaSymbol::virtualBaseDispIndex
  .property string undecoratedName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4A 00 00 00 00 00 )                         // ..J.....
    .get instance string DIALib.IDiaSymbol::get_undecoratedName()
  } // end of property IDiaSymbol::undecoratedName
  .property uint32 age()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4B 00 00 00 00 00 )                         // ..K.....
    .get instance uint32 DIALib.IDiaSymbol::get_age()
  } // end of property IDiaSymbol::age
  .property uint32 signature()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4C 00 00 00 00 00 )                         // ..L.....
    .get instance uint32 DIALib.IDiaSymbol::get_signature()
  } // end of property IDiaSymbol::signature
  .property int32 compilerGenerated()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4D 00 00 00 00 00 )                         // ..M.....
    .get instance int32 DIALib.IDiaSymbol::get_compilerGenerated()
  } // end of property IDiaSymbol::compilerGenerated
  .property int32 addressTaken()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4E 00 00 00 00 00 )                         // ..N.....
    .get instance int32 DIALib.IDiaSymbol::get_addressTaken()
  } // end of property IDiaSymbol::addressTaken
  .property uint32 rank()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4F 00 00 00 00 00 )                         // ..O.....
    .get instance uint32 DIALib.IDiaSymbol::get_rank()
  } // end of property IDiaSymbol::rank
  .property class DIALib.IDiaSymbol lowerBound()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 50 00 00 00 00 00 )                         // ..P.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol::get_lowerBound()
  } // end of property IDiaSymbol::lowerBound
  .property class DIALib.IDiaSymbol upperBound()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 51 00 00 00 00 00 )                         // ..Q.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol::get_upperBound()
  } // end of property IDiaSymbol::upperBound
  .property uint32 lowerBoundId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 52 00 00 00 00 00 )                         // ..R.....
    .get instance uint32 DIALib.IDiaSymbol::get_lowerBoundId()
  } // end of property IDiaSymbol::lowerBoundId
  .property uint32 upperBoundId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 53 00 00 00 00 00 )                         // ..S.....
    .get instance uint32 DIALib.IDiaSymbol::get_upperBoundId()
  } // end of property IDiaSymbol::upperBoundId
  .property uint32 targetSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 54 00 00 00 00 00 )                         // ..T.....
    .get instance uint32 DIALib.IDiaSymbol::get_targetSection()
  } // end of property IDiaSymbol::targetSection
  .property uint32 targetOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 55 00 00 00 00 00 )                         // ..U.....
    .get instance uint32 DIALib.IDiaSymbol::get_targetOffset()
  } // end of property IDiaSymbol::targetOffset
  .property uint32 targetRelativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 56 00 00 00 00 00 )                         // ..V.....
    .get instance uint32 DIALib.IDiaSymbol::get_targetRelativeVirtualAddress()
  } // end of property IDiaSymbol::targetRelativeVirtualAddress
  .property uint64 targetVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 57 00 00 00 00 00 )                         // ..W.....
    .get instance uint64 DIALib.IDiaSymbol::get_targetVirtualAddress()
  } // end of property IDiaSymbol::targetVirtualAddress
  .property uint32 machineType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 58 00 00 00 00 00 )                         // ..X.....
    .get instance uint32 DIALib.IDiaSymbol::get_machineType()
  } // end of property IDiaSymbol::machineType
  .property uint32 oemId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 59 00 00 00 00 00 )                         // ..Y.....
    .get instance uint32 DIALib.IDiaSymbol::get_oemId()
  } // end of property IDiaSymbol::oemId
  .property uint32 oemSymbolId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5A 00 00 00 00 00 )                         // ..Z.....
    .get instance uint32 DIALib.IDiaSymbol::get_oemSymbolId()
  } // end of property IDiaSymbol::oemSymbolId
  .property class DIALib.IDiaSymbol objectPointerType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5B 00 00 00 00 00 )                         // ..[.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol::get_objectPointerType()
  } // end of property IDiaSymbol::objectPointerType
  .property uint32 udtKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5C 00 00 00 00 00 )                         // ..\.....
    .get instance uint32 DIALib.IDiaSymbol::get_udtKind()
  } // end of property IDiaSymbol::udtKind
  .property int32 noReturn()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5D 00 00 00 00 00 )                         // ..].....
    .get instance int32 DIALib.IDiaSymbol::get_noReturn()
  } // end of property IDiaSymbol::noReturn
  .property int32 customCallingConvention()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5E 00 00 00 00 00 )                         // ..^.....
    .get instance int32 DIALib.IDiaSymbol::get_customCallingConvention()
  } // end of property IDiaSymbol::customCallingConvention
  .property int32 noInline()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5F 00 00 00 00 00 )                         // .._.....
    .get instance int32 DIALib.IDiaSymbol::get_noInline()
  } // end of property IDiaSymbol::noInline
  .property int32 optimizedCodeDebugInfo()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 60 00 00 00 00 00 )                         // ..`.....
    .get instance int32 DIALib.IDiaSymbol::get_optimizedCodeDebugInfo()
  } // end of property IDiaSymbol::optimizedCodeDebugInfo
  .property int32 notReached()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 61 00 00 00 00 00 )                         // ..a.....
    .get instance int32 DIALib.IDiaSymbol::get_notReached()
  } // end of property IDiaSymbol::notReached
  .property int32 interruptReturn()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 62 00 00 00 00 00 )                         // ..b.....
    .get instance int32 DIALib.IDiaSymbol::get_interruptReturn()
  } // end of property IDiaSymbol::interruptReturn
  .property int32 farReturn()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 63 00 00 00 00 00 )                         // ..c.....
    .get instance int32 DIALib.IDiaSymbol::get_farReturn()
  } // end of property IDiaSymbol::farReturn
  .property int32 isStatic()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 64 00 00 00 00 00 )                         // ..d.....
    .get instance int32 DIALib.IDiaSymbol::get_isStatic()
  } // end of property IDiaSymbol::isStatic
  .property int32 hasDebugInfo()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 65 00 00 00 00 00 )                         // ..e.....
    .get instance int32 DIALib.IDiaSymbol::get_hasDebugInfo()
  } // end of property IDiaSymbol::hasDebugInfo
  .property int32 isLTCG()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 66 00 00 00 00 00 )                         // ..f.....
    .get instance int32 DIALib.IDiaSymbol::get_isLTCG()
  } // end of property IDiaSymbol::isLTCG
  .property int32 isDataAligned()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 67 00 00 00 00 00 )                         // ..g.....
    .get instance int32 DIALib.IDiaSymbol::get_isDataAligned()
  } // end of property IDiaSymbol::isDataAligned
  .property int32 hasSecurityChecks()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 68 00 00 00 00 00 )                         // ..h.....
    .get instance int32 DIALib.IDiaSymbol::get_hasSecurityChecks()
  } // end of property IDiaSymbol::hasSecurityChecks
  .property string compilerName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 69 00 00 00 00 00 )                         // ..i.....
    .get instance string DIALib.IDiaSymbol::get_compilerName()
  } // end of property IDiaSymbol::compilerName
  .property int32 hasAlloca()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6A 00 00 00 00 00 )                         // ..j.....
    .get instance int32 DIALib.IDiaSymbol::get_hasAlloca()
  } // end of property IDiaSymbol::hasAlloca
  .property int32 hasSetJump()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6B 00 00 00 00 00 )                         // ..k.....
    .get instance int32 DIALib.IDiaSymbol::get_hasSetJump()
  } // end of property IDiaSymbol::hasSetJump
  .property int32 hasLongJump()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6C 00 00 00 00 00 )                         // ..l.....
    .get instance int32 DIALib.IDiaSymbol::get_hasLongJump()
  } // end of property IDiaSymbol::hasLongJump
  .property int32 hasInlAsm()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6D 00 00 00 00 00 )                         // ..m.....
    .get instance int32 DIALib.IDiaSymbol::get_hasInlAsm()
  } // end of property IDiaSymbol::hasInlAsm
  .property int32 hasEH()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6E 00 00 00 00 00 )                         // ..n.....
    .get instance int32 DIALib.IDiaSymbol::get_hasEH()
  } // end of property IDiaSymbol::hasEH
  .property int32 hasSEH()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6F 00 00 00 00 00 )                         // ..o.....
    .get instance int32 DIALib.IDiaSymbol::get_hasSEH()
  } // end of property IDiaSymbol::hasSEH
  .property int32 hasEHa()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 70 00 00 00 00 00 )                         // ..p.....
    .get instance int32 DIALib.IDiaSymbol::get_hasEHa()
  } // end of property IDiaSymbol::hasEHa
  .property int32 isNaked()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 71 00 00 00 00 00 )                         // ..q.....
    .get instance int32 DIALib.IDiaSymbol::get_isNaked()
  } // end of property IDiaSymbol::isNaked
  .property int32 isAggregated()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 72 00 00 00 00 00 )                         // ..r.....
    .get instance int32 DIALib.IDiaSymbol::get_isAggregated()
  } // end of property IDiaSymbol::isAggregated
  .property int32 isSplitted()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 73 00 00 00 00 00 )                         // ..s.....
    .get instance int32 DIALib.IDiaSymbol::get_isSplitted()
  } // end of property IDiaSymbol::isSplitted
  .property class DIALib.IDiaSymbol container()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 74 00 00 00 00 00 )                         // ..t.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol::get_container()
  } // end of property IDiaSymbol::container
  .property int32 inlSpec()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 75 00 00 00 00 00 )                         // ..u.....
    .get instance int32 DIALib.IDiaSymbol::get_inlSpec()
  } // end of property IDiaSymbol::inlSpec
  .property int32 noStackOrdering()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 76 00 00 00 00 00 )                         // ..v.....
    .get instance int32 DIALib.IDiaSymbol::get_noStackOrdering()
  } // end of property IDiaSymbol::noStackOrdering
  .property class DIALib.IDiaSymbol virtualBaseTableType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 77 00 00 00 00 00 )                         // ..w.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol::get_virtualBaseTableType()
  } // end of property IDiaSymbol::virtualBaseTableType
  .property int32 hasManagedCode()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 78 00 00 00 00 00 )                         // ..x.....
    .get instance int32 DIALib.IDiaSymbol::get_hasManagedCode()
  } // end of property IDiaSymbol::hasManagedCode
  .property int32 isHotpatchable()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 79 00 00 00 00 00 )                         // ..y.....
    .get instance int32 DIALib.IDiaSymbol::get_isHotpatchable()
  } // end of property IDiaSymbol::isHotpatchable
  .property int32 isCVTCIL()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7A 00 00 00 00 00 )                         // ..z.....
    .get instance int32 DIALib.IDiaSymbol::get_isCVTCIL()
  } // end of property IDiaSymbol::isCVTCIL
  .property int32 isMSILNetmodule()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7B 00 00 00 00 00 )                         // ..{.....
    .get instance int32 DIALib.IDiaSymbol::get_isMSILNetmodule()
  } // end of property IDiaSymbol::isMSILNetmodule
  .property int32 isCTypes()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7C 00 00 00 00 00 )                         // ..|.....
    .get instance int32 DIALib.IDiaSymbol::get_isCTypes()
  } // end of property IDiaSymbol::isCTypes
  .property int32 isStripped()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7D 00 00 00 00 00 )                         // ..}.....
    .get instance int32 DIALib.IDiaSymbol::get_isStripped()
  } // end of property IDiaSymbol::isStripped
  .property uint32 frontEndQFE()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7E 00 00 00 00 00 )                         // ..~.....
    .get instance uint32 DIALib.IDiaSymbol::get_frontEndQFE()
  } // end of property IDiaSymbol::frontEndQFE
  .property uint32 backEndQFE()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_backEndQFE()
  } // end of property IDiaSymbol::backEndQFE
  .property int32 wasInlined()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 80 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_wasInlined()
  } // end of property IDiaSymbol::wasInlined
  .property int32 strictGSCheck()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 81 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_strictGSCheck()
  } // end of property IDiaSymbol::strictGSCheck
  .property int32 isCxxReturnUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 82 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isCxxReturnUdt()
  } // end of property IDiaSymbol::isCxxReturnUdt
  .property int32 isConstructorVirtualBase()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 83 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isConstructorVirtualBase()
  } // end of property IDiaSymbol::isConstructorVirtualBase
  .property int32 RValueReference()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 84 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_RValueReference()
  } // end of property IDiaSymbol::RValueReference
  .property class DIALib.IDiaSymbol unmodifiedType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 85 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol::get_unmodifiedType()
  } // end of property IDiaSymbol::unmodifiedType
  .property int32 framePointerPresent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 86 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_framePointerPresent()
  } // end of property IDiaSymbol::framePointerPresent
  .property int32 isSafeBuffers()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 87 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isSafeBuffers()
  } // end of property IDiaSymbol::isSafeBuffers
  .property int32 intrinsic()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 88 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_intrinsic()
  } // end of property IDiaSymbol::intrinsic
  .property int32 'sealed'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 89 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_sealed()
  } // end of property IDiaSymbol::'sealed'
  .property int32 hfaFloat()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8A 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_hfaFloat()
  } // end of property IDiaSymbol::hfaFloat
  .property int32 hfaDouble()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8B 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_hfaDouble()
  } // end of property IDiaSymbol::hfaDouble
  .property uint32 liveRangeStartAddressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_liveRangeStartAddressSection()
  } // end of property IDiaSymbol::liveRangeStartAddressSection
  .property uint32 liveRangeStartAddressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8D 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_liveRangeStartAddressOffset()
  } // end of property IDiaSymbol::liveRangeStartAddressOffset
  .property uint32 liveRangeStartRelativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8E 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_liveRangeStartRelativeVirtualAddress()
  } // end of property IDiaSymbol::liveRangeStartRelativeVirtualAddress
  .property uint32 countLiveRanges()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_countLiveRanges()
  } // end of property IDiaSymbol::countLiveRanges
  .property uint64 liveRangeLength()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 90 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol::get_liveRangeLength()
  } // end of property IDiaSymbol::liveRangeLength
  .property uint32 offsetInUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 91 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_offsetInUdt()
  } // end of property IDiaSymbol::offsetInUdt
  .property uint32 paramBasePointerRegisterId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 92 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_paramBasePointerRegisterId()
  } // end of property IDiaSymbol::paramBasePointerRegisterId
  .property uint32 localBasePointerRegisterId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 93 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_localBasePointerRegisterId()
  } // end of property IDiaSymbol::localBasePointerRegisterId
  .property int32 isLocationControlFlowDependent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 94 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isLocationControlFlowDependent()
  } // end of property IDiaSymbol::isLocationControlFlowDependent
  .property uint32 stride()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 95 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_stride()
  } // end of property IDiaSymbol::stride
  .property uint32 numberOfRows()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 96 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_numberOfRows()
  } // end of property IDiaSymbol::numberOfRows
  .property uint32 numberOfColumns()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 97 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_numberOfColumns()
  } // end of property IDiaSymbol::numberOfColumns
  .property int32 isMatrixRowMajor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 98 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isMatrixRowMajor()
  } // end of property IDiaSymbol::isMatrixRowMajor
  .property int32 isReturnValue()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 99 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isReturnValue()
  } // end of property IDiaSymbol::isReturnValue
  .property int32 isOptimizedAway()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9A 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isOptimizedAway()
  } // end of property IDiaSymbol::isOptimizedAway
  .property uint32 builtInKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9B 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_builtInKind()
  } // end of property IDiaSymbol::builtInKind
  .property uint32 registerType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_registerType()
  } // end of property IDiaSymbol::registerType
  .property uint32 baseDataSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9D 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_baseDataSlot()
  } // end of property IDiaSymbol::baseDataSlot
  .property uint32 baseDataOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9E 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_baseDataOffset()
  } // end of property IDiaSymbol::baseDataOffset
  .property uint32 textureSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_textureSlot()
  } // end of property IDiaSymbol::textureSlot
  .property uint32 samplerSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A0 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_samplerSlot()
  } // end of property IDiaSymbol::samplerSlot
  .property uint32 uavSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A1 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_uavSlot()
  } // end of property IDiaSymbol::uavSlot
  .property uint32 sizeInUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A2 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_sizeInUdt()
  } // end of property IDiaSymbol::sizeInUdt
  .property uint32 memorySpaceKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A3 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_memorySpaceKind()
  } // end of property IDiaSymbol::memorySpaceKind
  .property uint32 unmodifiedTypeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A4 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_unmodifiedTypeId()
  } // end of property IDiaSymbol::unmodifiedTypeId
  .property uint32 subTypeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A5 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_subTypeId()
  } // end of property IDiaSymbol::subTypeId
  .property class DIALib.IDiaSymbol subType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A6 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol::get_subType()
  } // end of property IDiaSymbol::subType
  .property uint32 numberOfModifiers()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A7 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_numberOfModifiers()
  } // end of property IDiaSymbol::numberOfModifiers
  .property uint32 numberOfRegisterIndices()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A8 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_numberOfRegisterIndices()
  } // end of property IDiaSymbol::numberOfRegisterIndices
  .property int32 isHLSLData()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isHLSLData()
  } // end of property IDiaSymbol::isHLSLData
  .property int32 isPointerToDataMember()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isPointerToDataMember()
  } // end of property IDiaSymbol::isPointerToDataMember
  .property int32 isPointerToMemberFunction()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AB 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isPointerToMemberFunction()
  } // end of property IDiaSymbol::isPointerToMemberFunction
  .property int32 isSingleInheritance()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AC 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isSingleInheritance()
  } // end of property IDiaSymbol::isSingleInheritance
  .property int32 isMultipleInheritance()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AD 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isMultipleInheritance()
  } // end of property IDiaSymbol::isMultipleInheritance
  .property int32 isVirtualInheritance()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AE 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isVirtualInheritance()
  } // end of property IDiaSymbol::isVirtualInheritance
  .property int32 restrictedType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AF 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_restrictedType()
  } // end of property IDiaSymbol::restrictedType
  .property int32 isPointerBasedOnSymbolValue()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B0 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isPointerBasedOnSymbolValue()
  } // end of property IDiaSymbol::isPointerBasedOnSymbolValue
  .property class DIALib.IDiaSymbol baseSymbol()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B1 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol::get_baseSymbol()
  } // end of property IDiaSymbol::baseSymbol
  .property uint32 baseSymbolId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B2 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_baseSymbolId()
  } // end of property IDiaSymbol::baseSymbolId
  .property string objectFileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B3 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol::get_objectFileName()
  } // end of property IDiaSymbol::objectFileName
  .property int32 isAcceleratorGroupSharedLocal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B4 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isAcceleratorGroupSharedLocal()
  } // end of property IDiaSymbol::isAcceleratorGroupSharedLocal
  .property int32 isAcceleratorPointerTagLiveRange()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B5 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isAcceleratorPointerTagLiveRange()
  } // end of property IDiaSymbol::isAcceleratorPointerTagLiveRange
  .property int32 isAcceleratorStubFunction()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B6 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isAcceleratorStubFunction()
  } // end of property IDiaSymbol::isAcceleratorStubFunction
  .property uint32 numberOfAcceleratorPointerTags()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B7 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_numberOfAcceleratorPointerTags()
  } // end of property IDiaSymbol::numberOfAcceleratorPointerTags
  .property int32 isSdl()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B8 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isSdl()
  } // end of property IDiaSymbol::isSdl
  .property int32 isWinRTPointer()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isWinRTPointer()
  } // end of property IDiaSymbol::isWinRTPointer
  .property int32 isRefUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isRefUdt()
  } // end of property IDiaSymbol::isRefUdt
  .property int32 isValueUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BB 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isValueUdt()
  } // end of property IDiaSymbol::isValueUdt
  .property int32 isInterfaceUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BC 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isInterfaceUdt()
  } // end of property IDiaSymbol::isInterfaceUdt
  .property int32 isPGO()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BD 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isPGO()
  } // end of property IDiaSymbol::isPGO
  .property int32 hasValidPGOCounts()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BE 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_hasValidPGOCounts()
  } // end of property IDiaSymbol::hasValidPGOCounts
  .property int32 isOptimizedForSpeed()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BF 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_isOptimizedForSpeed()
  } // end of property IDiaSymbol::isOptimizedForSpeed
  .property uint32 PGOEntryCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C0 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_PGOEntryCount()
  } // end of property IDiaSymbol::PGOEntryCount
  .property uint32 PGOEdgeCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C1 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_PGOEdgeCount()
  } // end of property IDiaSymbol::PGOEdgeCount
  .property uint64 PGODynamicInstructionCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C2 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol::get_PGODynamicInstructionCount()
  } // end of property IDiaSymbol::PGODynamicInstructionCount
  .property uint32 staticSize()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C3 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_staticSize()
  } // end of property IDiaSymbol::staticSize
  .property uint32 finalLiveStaticSize()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C4 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_finalLiveStaticSize()
  } // end of property IDiaSymbol::finalLiveStaticSize
  .property string phaseName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C5 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol::get_phaseName()
  } // end of property IDiaSymbol::phaseName
  .property int32 hasControlFlowCheck()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C6 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_hasControlFlowCheck()
  } // end of property IDiaSymbol::hasControlFlowCheck
  .property int32 constantExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C7 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_constantExport()
  } // end of property IDiaSymbol::constantExport
  .property int32 dataExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C8 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_dataExport()
  } // end of property IDiaSymbol::dataExport
  .property int32 privateExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_privateExport()
  } // end of property IDiaSymbol::privateExport
  .property int32 noNameExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_noNameExport()
  } // end of property IDiaSymbol::noNameExport
  .property int32 exportHasExplicitlyAssignedOrdinal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CB 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_exportHasExplicitlyAssignedOrdinal()
  } // end of property IDiaSymbol::exportHasExplicitlyAssignedOrdinal
  .property int32 exportIsForwarder()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CC 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol::get_exportIsForwarder()
  } // end of property IDiaSymbol::exportIsForwarder
  .property uint32 ordinal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CD 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_ordinal()
  } // end of property IDiaSymbol::ordinal
  .property uint32 frameSize()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CE 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_frameSize()
  } // end of property IDiaSymbol::frameSize
  .property uint32 exceptionHandlerAddressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CF 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_exceptionHandlerAddressSection()
  } // end of property IDiaSymbol::exceptionHandlerAddressSection
  .property uint32 exceptionHandlerAddressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D0 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_exceptionHandlerAddressOffset()
  } // end of property IDiaSymbol::exceptionHandlerAddressOffset
  .property uint32 exceptionHandlerRelativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D1 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_exceptionHandlerRelativeVirtualAddress()
  } // end of property IDiaSymbol::exceptionHandlerRelativeVirtualAddress
  .property uint64 exceptionHandlerVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D2 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol::get_exceptionHandlerVirtualAddress()
  } // end of property IDiaSymbol::exceptionHandlerVirtualAddress
  .property uint32 characteristics()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D3 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_characteristics()
  } // end of property IDiaSymbol::characteristics
  .property class DIALib.IDiaSymbol coffGroup()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D4 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol::get_coffGroup()
  } // end of property IDiaSymbol::coffGroup
  .property uint32 bindID()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D5 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_bindID()
  } // end of property IDiaSymbol::bindID
  .property uint32 bindSpace()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D6 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_bindSpace()
  } // end of property IDiaSymbol::bindSpace
  .property uint32 bindSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D7 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol::get_bindSlot()
  } // end of property IDiaSymbol::bindSlot
} // end of class DIALib.IDiaSymbol

.class interface public abstract auto ansi import DIALib.IDiaEnumTables
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 36 35 43 32 42 30 41 2D 31 31 35 30   // ..$C65C2B0A-1150
                                                                                                  2D 34 44 37 41 2D 41 46 43 43 2D 45 30 35 42 46   // -4D7A-AFCC-E05BF
                                                                                                  33 44 45 45 38 31 45 00 00 )                      // 3DEE81E..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )                      // ...Item..
  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Collections.IEnumerator 
          marshal( custom ("System.Runtime.InteropServices.CustomMarshalers.EnumeratorToEnumVariantMarshaler, CustomMarshalers, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a","")) 
          GetEnumerator() runtime managed internalcall
  {
  } // end of method IDiaEnumTables::GetEnumerator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_count() runtime managed internalcall
  {
  } // end of method IDiaEnumTables::get_count

  .method public hidebysig newslot abstract virtual 
          instance class DIALib.IDiaTable 
          marshal( interface ) 
          Item([in] object  marshal( struct) index) runtime managed internalcall
  {
  } // end of method IDiaEnumTables::Item

  .method public hidebysig newslot abstract virtual 
          instance void  Next(uint32 celt,
                              class DIALib.IDiaTable&  marshal( interface ) rgelt,
                              uint32& pceltFetched) runtime managed internalcall
  {
  } // end of method IDiaEnumTables::Next

  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method IDiaEnumTables::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method IDiaEnumTables::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class DIALib.IDiaEnumTables&  marshal( interface ) ppenum) runtime managed internalcall
  {
  } // end of method IDiaEnumTables::Clone

  .property int32 count()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaEnumTables::get_count()
  } // end of property IDiaEnumTables::count
} // end of class DIALib.IDiaEnumTables

.class interface public abstract auto ansi import DIALib.IDiaEnumSymbolsByAddr
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 36 32 34 42 37 44 39 43 2D 32 34 45 41   // ..$624B7D9C-24EA
                                                                                                  2D 34 34 32 31 2D 39 44 30 36 2D 33 42 35 37 37   // -4421-9D06-3B577
                                                                                                  34 37 31 43 31 46 41 00 00 )                      // 471C1FA..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          symbolByAddr([in] uint32 isect,
                       [in] uint32 offset) runtime managed internalcall
  {
  } // end of method IDiaEnumSymbolsByAddr::symbolByAddr

  .method public hidebysig newslot abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          symbolByRVA([in] uint32 relativeVirtualAddress) runtime managed internalcall
  {
  } // end of method IDiaEnumSymbolsByAddr::symbolByRVA

  .method public hidebysig newslot abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          symbolByVA([in] uint64 virtualAddress) runtime managed internalcall
  {
  } // end of method IDiaEnumSymbolsByAddr::symbolByVA

  .method public hidebysig newslot abstract virtual 
          instance void  Next([in] uint32 celt,
                              [out] class DIALib.IDiaSymbol&  marshal( interface ) rgelt,
                              [out] uint32& pceltFetched) runtime managed internalcall
  {
  } // end of method IDiaEnumSymbolsByAddr::Next

  .method public hidebysig newslot abstract virtual 
          instance void  Prev([in] uint32 celt,
                              [out] class DIALib.IDiaSymbol&  marshal( interface ) rgelt,
                              [out] uint32& pceltFetched) runtime managed internalcall
  {
  } // end of method IDiaEnumSymbolsByAddr::Prev

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class DIALib.IDiaEnumSymbolsByAddr&  marshal( interface ) ppenum) runtime managed internalcall
  {
  } // end of method IDiaEnumSymbolsByAddr::Clone

} // end of class DIALib.IDiaEnumSymbolsByAddr

.class public auto ansi sealed DIALib.SymTagEnum
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype DIALib.SymTagEnum SymTagNull = int32(0x00000000)
  .field public static literal valuetype DIALib.SymTagEnum SymTagExe = int32(0x00000001)
  .field public static literal valuetype DIALib.SymTagEnum SymTagCompiland = int32(0x00000002)
  .field public static literal valuetype DIALib.SymTagEnum SymTagCompilandDetails = int32(0x00000003)
  .field public static literal valuetype DIALib.SymTagEnum SymTagCompilandEnv = int32(0x00000004)
  .field public static literal valuetype DIALib.SymTagEnum SymTagFunction = int32(0x00000005)
  .field public static literal valuetype DIALib.SymTagEnum SymTagBlock = int32(0x00000006)
  .field public static literal valuetype DIALib.SymTagEnum SymTagData = int32(0x00000007)
  .field public static literal valuetype DIALib.SymTagEnum SymTagAnnotation = int32(0x00000008)
  .field public static literal valuetype DIALib.SymTagEnum SymTagLabel = int32(0x00000009)
  .field public static literal valuetype DIALib.SymTagEnum SymTagPublicSymbol = int32(0x0000000A)
  .field public static literal valuetype DIALib.SymTagEnum SymTagUDT = int32(0x0000000B)
  .field public static literal valuetype DIALib.SymTagEnum SymTagEnum = int32(0x0000000C)
  .field public static literal valuetype DIALib.SymTagEnum SymTagFunctionType = int32(0x0000000D)
  .field public static literal valuetype DIALib.SymTagEnum SymTagPointerType = int32(0x0000000E)
  .field public static literal valuetype DIALib.SymTagEnum SymTagArrayType = int32(0x0000000F)
  .field public static literal valuetype DIALib.SymTagEnum SymTagBaseType = int32(0x00000010)
  .field public static literal valuetype DIALib.SymTagEnum SymTagTypedef = int32(0x00000011)
  .field public static literal valuetype DIALib.SymTagEnum SymTagBaseClass = int32(0x00000012)
  .field public static literal valuetype DIALib.SymTagEnum SymTagFriend = int32(0x00000013)
  .field public static literal valuetype DIALib.SymTagEnum SymTagFunctionArgType = int32(0x00000014)
  .field public static literal valuetype DIALib.SymTagEnum SymTagFuncDebugStart = int32(0x00000015)
  .field public static literal valuetype DIALib.SymTagEnum SymTagFuncDebugEnd = int32(0x00000016)
  .field public static literal valuetype DIALib.SymTagEnum SymTagUsingNamespace = int32(0x00000017)
  .field public static literal valuetype DIALib.SymTagEnum SymTagVTableShape = int32(0x00000018)
  .field public static literal valuetype DIALib.SymTagEnum SymTagVTable = int32(0x00000019)
  .field public static literal valuetype DIALib.SymTagEnum SymTagCustom = int32(0x0000001A)
  .field public static literal valuetype DIALib.SymTagEnum SymTagThunk = int32(0x0000001B)
  .field public static literal valuetype DIALib.SymTagEnum SymTagCustomType = int32(0x0000001C)
  .field public static literal valuetype DIALib.SymTagEnum SymTagManagedType = int32(0x0000001D)
  .field public static literal valuetype DIALib.SymTagEnum SymTagDimension = int32(0x0000001E)
  .field public static literal valuetype DIALib.SymTagEnum SymTagCallSite = int32(0x0000001F)
  .field public static literal valuetype DIALib.SymTagEnum SymTagInlineSite = int32(0x00000020)
  .field public static literal valuetype DIALib.SymTagEnum SymTagBaseInterface = int32(0x00000021)
  .field public static literal valuetype DIALib.SymTagEnum SymTagVectorType = int32(0x00000022)
  .field public static literal valuetype DIALib.SymTagEnum SymTagMatrixType = int32(0x00000023)
  .field public static literal valuetype DIALib.SymTagEnum SymTagHLSLType = int32(0x00000024)
  .field public static literal valuetype DIALib.SymTagEnum SymTagCaller = int32(0x00000025)
  .field public static literal valuetype DIALib.SymTagEnum SymTagCallee = int32(0x00000026)
  .field public static literal valuetype DIALib.SymTagEnum SymTagExport = int32(0x00000027)
  .field public static literal valuetype DIALib.SymTagEnum SymTagHeapAllocationSite = int32(0x00000028)
  .field public static literal valuetype DIALib.SymTagEnum SymTagCoffGroup = int32(0x00000029)
  .field public static literal valuetype DIALib.SymTagEnum SymTagInlinee = int32(0x0000002A)
  .field public static literal valuetype DIALib.SymTagEnum SymTagMax = int32(0x0000002B)
} // end of class DIALib.SymTagEnum

.class interface public abstract auto ansi import DIALib.IDiaEnumSymbols
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )                      // ...Item..
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 41 42 37 32 43 34 38 2D 34 34 33 42   // ..$CAB72C48-443B
                                                                                                  2D 34 38 46 35 2D 39 42 30 42 2D 34 32 46 30 38   // -48F5-9B0B-42F08
                                                                                                  32 30 41 42 32 39 41 00 00 )                      // 20AB29A..
  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Collections.IEnumerator 
          marshal( custom ("System.Runtime.InteropServices.CustomMarshalers.EnumeratorToEnumVariantMarshaler, CustomMarshalers, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a","")) 
          GetEnumerator() runtime managed internalcall
  {
  } // end of method IDiaEnumSymbols::GetEnumerator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_count() runtime managed internalcall
  {
  } // end of method IDiaEnumSymbols::get_count

  .method public hidebysig newslot abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          Item([in] uint32 index) runtime managed internalcall
  {
  } // end of method IDiaEnumSymbols::Item

  .method public hidebysig newslot abstract virtual 
          instance void  Next([in] uint32 celt,
                              [out] class DIALib.IDiaSymbol&  marshal( interface ) rgelt,
                              [out] uint32& pceltFetched) runtime managed internalcall
  {
  } // end of method IDiaEnumSymbols::Next

  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method IDiaEnumSymbols::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method IDiaEnumSymbols::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppenum) runtime managed internalcall
  {
  } // end of method IDiaEnumSymbols::Clone

  .property int32 count()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaEnumSymbols::get_count()
  } // end of property IDiaEnumSymbols::count
} // end of class DIALib.IDiaEnumSymbols

.class interface public abstract auto ansi import DIALib.IDiaEnumSourceFiles
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )                      // ...Item..
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 31 30 46 33 44 42 44 39 2D 36 36 34 46   // ..$10F3DBD9-664F
                                                                                                  2D 34 34 36 39 2D 42 38 30 38 2D 39 34 37 31 43   // -4469-B808-9471C
                                                                                                  37 41 35 30 35 33 38 00 00 )                      // 7A50538..
  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Collections.IEnumerator 
          marshal( custom ("System.Runtime.InteropServices.CustomMarshalers.EnumeratorToEnumVariantMarshaler, CustomMarshalers, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a","")) 
          GetEnumerator() runtime managed internalcall
  {
  } // end of method IDiaEnumSourceFiles::GetEnumerator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_count() runtime managed internalcall
  {
  } // end of method IDiaEnumSourceFiles::get_count

  .method public hidebysig newslot abstract virtual 
          instance class DIALib.IDiaSourceFile 
          marshal( interface ) 
          Item([in] uint32 index) runtime managed internalcall
  {
  } // end of method IDiaEnumSourceFiles::Item

  .method public hidebysig newslot abstract virtual 
          instance void  Next([in] uint32 celt,
                              [out] class DIALib.IDiaSourceFile&  marshal( interface ) rgelt,
                              [out] uint32& pceltFetched) runtime managed internalcall
  {
  } // end of method IDiaEnumSourceFiles::Next

  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method IDiaEnumSourceFiles::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method IDiaEnumSourceFiles::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class DIALib.IDiaEnumSourceFiles&  marshal( interface ) ppenum) runtime managed internalcall
  {
  } // end of method IDiaEnumSourceFiles::Clone

  .property int32 count()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaEnumSourceFiles::get_count()
  } // end of property IDiaEnumSourceFiles::count
} // end of class DIALib.IDiaEnumSourceFiles

.class interface public abstract auto ansi import DIALib.IDiaSourceFile
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 41 32 45 46 35 33 35 33 2D 46 35 41 38   // ..$A2EF5353-F5A8
                                                                                                  2D 34 45 42 33 2D 39 30 44 32 2D 43 42 35 32 36   // -4EB3-90D2-CB526
                                                                                                  41 43 42 33 43 44 44 00 00 )                      // ACB3CDD..
  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_uniqueId() runtime managed internalcall
  {
  } // end of method IDiaSourceFile::get_uniqueId

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_fileName() runtime managed internalcall
  {
  } // end of method IDiaSourceFile::get_fileName

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_checksumType() runtime managed internalcall
  {
  } // end of method IDiaSourceFile::get_checksumType

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaEnumSymbols 
          marshal( interface ) 
          get_compilands() runtime managed internalcall
  {
  } // end of method IDiaSourceFile::get_compilands

  .method public hidebysig newslot abstract virtual 
          instance void  get_checksum([in] uint32 cbData,
                                      [out] uint32& pcbData,
                                      [out] uint8& pbData) runtime managed internalcall
  {
  } // end of method IDiaSourceFile::get_checksum

  .property uint32 uniqueId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSourceFile::get_uniqueId()
  } // end of property IDiaSourceFile::uniqueId
  .property string fileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSourceFile::get_fileName()
  } // end of property IDiaSourceFile::fileName
  .property uint32 checksumType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 04 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSourceFile::get_checksumType()
  } // end of property IDiaSourceFile::checksumType
  .property class DIALib.IDiaEnumSymbols compilands()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 05 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaEnumSymbols DIALib.IDiaSourceFile::get_compilands()
  } // end of property IDiaSourceFile::compilands
} // end of class DIALib.IDiaSourceFile

.class interface public abstract auto ansi import DIALib.IDiaEnumLineNumbers
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )                      // ...Item..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 46 45 33 30 45 38 37 38 2D 35 34 41 43   // ..$FE30E878-54AC
                                                                                                  2D 34 34 46 31 2D 38 31 42 41 2D 33 39 44 45 39   // -44F1-81BA-39DE9
                                                                                                  34 30 46 36 30 35 32 00 00 )                      // 40F6052..
  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Collections.IEnumerator 
          marshal( custom ("System.Runtime.InteropServices.CustomMarshalers.EnumeratorToEnumVariantMarshaler, CustomMarshalers, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a","")) 
          GetEnumerator() runtime managed internalcall
  {
  } // end of method IDiaEnumLineNumbers::GetEnumerator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_count() runtime managed internalcall
  {
  } // end of method IDiaEnumLineNumbers::get_count

  .method public hidebysig newslot abstract virtual 
          instance class DIALib.IDiaLineNumber 
          marshal( interface ) 
          Item([in] uint32 index) runtime managed internalcall
  {
  } // end of method IDiaEnumLineNumbers::Item

  .method public hidebysig newslot abstract virtual 
          instance void  Next([in] uint32 celt,
                              [out] class DIALib.IDiaLineNumber&  marshal( interface ) rgelt,
                              [out] uint32& pceltFetched) runtime managed internalcall
  {
  } // end of method IDiaEnumLineNumbers::Next

  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method IDiaEnumLineNumbers::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method IDiaEnumLineNumbers::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppenum) runtime managed internalcall
  {
  } // end of method IDiaEnumLineNumbers::Clone

  .property int32 count()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaEnumLineNumbers::get_count()
  } // end of property IDiaEnumLineNumbers::count
} // end of class DIALib.IDiaEnumLineNumbers

.class interface public abstract auto ansi import DIALib.IDiaEnumInjectedSources
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )                      // ...Item..
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 44 35 36 31 32 35 37 33 2D 36 39 32 35   // ..$D5612573-6925
                                                                                                  2D 34 34 36 38 2D 38 38 38 33 2D 39 38 43 44 45   // -4468-8883-98CDE
                                                                                                  43 38 43 33 38 34 41 00 00 )                      // C8C384A..
  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Collections.IEnumerator 
          marshal( custom ("System.Runtime.InteropServices.CustomMarshalers.EnumeratorToEnumVariantMarshaler, CustomMarshalers, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a","")) 
          GetEnumerator() runtime managed internalcall
  {
  } // end of method IDiaEnumInjectedSources::GetEnumerator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_count() runtime managed internalcall
  {
  } // end of method IDiaEnumInjectedSources::get_count

  .method public hidebysig newslot abstract virtual 
          instance class DIALib.IDiaInjectedSource 
          marshal( interface ) 
          Item([in] uint32 index) runtime managed internalcall
  {
  } // end of method IDiaEnumInjectedSources::Item

  .method public hidebysig newslot abstract virtual 
          instance void  Next([in] uint32 celt,
                              [out] class DIALib.IDiaInjectedSource&  marshal( interface ) rgelt,
                              [out] uint32& pceltFetched) runtime managed internalcall
  {
  } // end of method IDiaEnumInjectedSources::Next

  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method IDiaEnumInjectedSources::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method IDiaEnumInjectedSources::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class DIALib.IDiaEnumInjectedSources&  marshal( interface ) ppenum) runtime managed internalcall
  {
  } // end of method IDiaEnumInjectedSources::Clone

  .property int32 count()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaEnumInjectedSources::get_count()
  } // end of property IDiaEnumInjectedSources::count
} // end of class DIALib.IDiaEnumInjectedSources

.class interface public abstract auto ansi import DIALib.IDiaEnumDebugStreams
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 38 43 42 42 34 31 45 2D 34 37 41 36   // ..$08CBB41E-47A6
                                                                                                  2D 34 46 38 37 2D 39 32 46 31 2D 31 43 39 43 38   // -4F87-92F1-1C9C8
                                                                                                  37 43 45 44 30 34 34 00 00 )                      // 7CED044..
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )                      // ...Item..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Collections.IEnumerator 
          marshal( custom ("System.Runtime.InteropServices.CustomMarshalers.EnumeratorToEnumVariantMarshaler, CustomMarshalers, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a","")) 
          GetEnumerator() runtime managed internalcall
  {
  } // end of method IDiaEnumDebugStreams::GetEnumerator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_count() runtime managed internalcall
  {
  } // end of method IDiaEnumDebugStreams::get_count

  .method public hidebysig newslot abstract virtual 
          instance class DIALib.IDiaEnumDebugStreamData 
          marshal( interface ) 
          Item([in] object  marshal( struct) index) runtime managed internalcall
  {
  } // end of method IDiaEnumDebugStreams::Item

  .method public hidebysig newslot abstract virtual 
          instance void  Next([in] uint32 celt,
                              [out] class DIALib.IDiaEnumDebugStreamData&  marshal( interface ) rgelt,
                              [out] uint32& pceltFetched) runtime managed internalcall
  {
  } // end of method IDiaEnumDebugStreams::Next

  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method IDiaEnumDebugStreams::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method IDiaEnumDebugStreams::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class DIALib.IDiaEnumDebugStreams&  marshal( interface ) ppenum) runtime managed internalcall
  {
  } // end of method IDiaEnumDebugStreams::Clone

  .property int32 count()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaEnumDebugStreams::get_count()
  } // end of property IDiaEnumDebugStreams::count
} // end of class DIALib.IDiaEnumDebugStreams

.class interface public abstract auto ansi import DIALib.IDiaEnumInputAssemblyFiles
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )                      // ...Item..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 31 43 37 46 46 36 35 33 2D 35 31 46 37   // ..$1C7FF653-51F7
                                                                                                  2D 34 35 37 45 2D 38 34 31 39 2D 42 32 30 46 35   // -457E-8419-B20F5
                                                                                                  37 45 46 37 45 34 44 00 00 )                      // 7EF7E4D..
  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Collections.IEnumerator 
          marshal( custom ("System.Runtime.InteropServices.CustomMarshalers.EnumeratorToEnumVariantMarshaler, CustomMarshalers, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a","")) 
          GetEnumerator() runtime managed internalcall
  {
  } // end of method IDiaEnumInputAssemblyFiles::GetEnumerator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_count() runtime managed internalcall
  {
  } // end of method IDiaEnumInputAssemblyFiles::get_count

  .method public hidebysig newslot abstract virtual 
          instance class DIALib.IDiaInputAssemblyFile 
          marshal( interface ) 
          Item([in] uint32 index) runtime managed internalcall
  {
  } // end of method IDiaEnumInputAssemblyFiles::Item

  .method public hidebysig newslot abstract virtual 
          instance void  Next([in] uint32 celt,
                              [out] class DIALib.IDiaInputAssemblyFile&  marshal( interface ) rgelt,
                              [out] uint32& pceltFetched) runtime managed internalcall
  {
  } // end of method IDiaEnumInputAssemblyFiles::Next

  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method IDiaEnumInputAssemblyFiles::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method IDiaEnumInputAssemblyFiles::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class DIALib.IDiaEnumInputAssemblyFiles&  marshal( interface ) ppenum) runtime managed internalcall
  {
  } // end of method IDiaEnumInputAssemblyFiles::Clone

  .property int32 count()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaEnumInputAssemblyFiles::get_count()
  } // end of property IDiaEnumInputAssemblyFiles::count
} // end of class DIALib.IDiaEnumInputAssemblyFiles

.class interface public abstract auto ansi import DIALib.IDiaInputAssemblyFile
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 33 42 46 45 35 36 42 30 2D 33 39 30 43   // ..$3BFE56B0-390C
                                                                                                  2D 34 38 36 33 2D 39 34 33 30 2D 31 46 33 44 30   // -4863-9430-1F3D0
                                                                                                  38 33 42 37 36 38 34 00 00 )                      // 83B7684..
  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_uniqueId() runtime managed internalcall
  {
  } // end of method IDiaInputAssemblyFile::get_uniqueId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_index() runtime managed internalcall
  {
  } // end of method IDiaInputAssemblyFile::get_index

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_timeStamp() runtime managed internalcall
  {
  } // end of method IDiaInputAssemblyFile::get_timeStamp

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_pdbAvailableAtILMerge() runtime managed internalcall
  {
  } // end of method IDiaInputAssemblyFile::get_pdbAvailableAtILMerge

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_fileName() runtime managed internalcall
  {
  } // end of method IDiaInputAssemblyFile::get_fileName

  .method public hidebysig newslot abstract virtual 
          instance void  get_version([in] uint32 cbData,
                                     [out] uint32& pcbData,
                                     [out] uint8& pbData) runtime managed internalcall
  {
  } // end of method IDiaInputAssemblyFile::get_version

  .property uint32 uniqueId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaInputAssemblyFile::get_uniqueId()
  } // end of property IDiaInputAssemblyFile::uniqueId
  .property uint32 index()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaInputAssemblyFile::get_index()
  } // end of property IDiaInputAssemblyFile::index
  .property uint32 timeStamp()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaInputAssemblyFile::get_timeStamp()
  } // end of property IDiaInputAssemblyFile::timeStamp
  .property int32 pdbAvailableAtILMerge()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 04 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaInputAssemblyFile::get_pdbAvailableAtILMerge()
  } // end of property IDiaInputAssemblyFile::pdbAvailableAtILMerge
  .property string fileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 05 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaInputAssemblyFile::get_fileName()
  } // end of property IDiaInputAssemblyFile::fileName
} // end of class DIALib.IDiaInputAssemblyFile

.class interface public abstract auto ansi import DIALib.IDiaLineNumber
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 42 33 38 38 45 42 31 34 2D 42 45 34 44   // ..$B388EB14-BE4D
                                                                                                  2D 34 32 31 44 2D 41 38 41 31 2D 36 43 46 37 41   // -421D-A8A1-6CF7A
                                                                                                  42 30 35 37 30 38 36 00 00 )                      // B057086..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_compiland() runtime managed internalcall
  {
  } // end of method IDiaLineNumber::get_compiland

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSourceFile 
          marshal( interface ) 
          get_sourceFile() runtime managed internalcall
  {
  } // end of method IDiaLineNumber::get_sourceFile

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lineNumber() runtime managed internalcall
  {
  } // end of method IDiaLineNumber::get_lineNumber

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lineNumberEnd() runtime managed internalcall
  {
  } // end of method IDiaLineNumber::get_lineNumberEnd

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_columnNumber() runtime managed internalcall
  {
  } // end of method IDiaLineNumber::get_columnNumber

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_columnNumberEnd() runtime managed internalcall
  {
  } // end of method IDiaLineNumber::get_columnNumberEnd

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_addressSection() runtime managed internalcall
  {
  } // end of method IDiaLineNumber::get_addressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_addressOffset() runtime managed internalcall
  {
  } // end of method IDiaLineNumber::get_addressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_relativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaLineNumber::get_relativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_virtualAddress() runtime managed internalcall
  {
  } // end of method IDiaLineNumber::get_virtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_length() runtime managed internalcall
  {
  } // end of method IDiaLineNumber::get_length

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_sourceFileId() runtime managed internalcall
  {
  } // end of method IDiaLineNumber::get_sourceFileId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_statement() runtime managed internalcall
  {
  } // end of method IDiaLineNumber::get_statement

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_compilandId() runtime managed internalcall
  {
  } // end of method IDiaLineNumber::get_compilandId

  .property class DIALib.IDiaSymbol compiland()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaLineNumber::get_compiland()
  } // end of property IDiaLineNumber::compiland
  .property class DIALib.IDiaSourceFile sourceFile()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSourceFile DIALib.IDiaLineNumber::get_sourceFile()
  } // end of property IDiaLineNumber::sourceFile
  .property uint32 lineNumber()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaLineNumber::get_lineNumber()
  } // end of property IDiaLineNumber::lineNumber
  .property uint32 lineNumberEnd()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 04 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaLineNumber::get_lineNumberEnd()
  } // end of property IDiaLineNumber::lineNumberEnd
  .property uint32 columnNumber()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 05 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaLineNumber::get_columnNumber()
  } // end of property IDiaLineNumber::columnNumber
  .property uint32 columnNumberEnd()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 06 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaLineNumber::get_columnNumberEnd()
  } // end of property IDiaLineNumber::columnNumberEnd
  .property uint32 addressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 07 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaLineNumber::get_addressSection()
  } // end of property IDiaLineNumber::addressSection
  .property uint32 addressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaLineNumber::get_addressOffset()
  } // end of property IDiaLineNumber::addressOffset
  .property uint32 relativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 09 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaLineNumber::get_relativeVirtualAddress()
  } // end of property IDiaLineNumber::relativeVirtualAddress
  .property uint64 virtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0A 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaLineNumber::get_virtualAddress()
  } // end of property IDiaLineNumber::virtualAddress
  .property uint32 length()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0B 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaLineNumber::get_length()
  } // end of property IDiaLineNumber::length
  .property uint32 sourceFileId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaLineNumber::get_sourceFileId()
  } // end of property IDiaLineNumber::sourceFileId
  .property int32 statement()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0D 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaLineNumber::get_statement()
  } // end of property IDiaLineNumber::statement
  .property uint32 compilandId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0E 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaLineNumber::get_compilandId()
  } // end of property IDiaLineNumber::compilandId
} // end of class DIALib.IDiaLineNumber

.class interface public abstract auto ansi import DIALib.IEnumUnknown
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 30 30 30 30 31 30 30 2D 30 30 30 30   // ..$00000100-0000
                                                                                                  2D 30 30 30 30 2D 43 30 30 30 2D 30 30 30 30 30   // -0000-C000-00000
                                                                                                  30 30 30 30 30 34 36 00 00 )                      // 0000046..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  RemoteNext([in] uint32 celt,
                                    [out] object&  marshal( iunknown ) rgelt,
                                    [out] uint32& pceltFetched) runtime managed internalcall
  {
  } // end of method IEnumUnknown::RemoteNext

  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method IEnumUnknown::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method IEnumUnknown::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class DIALib.IEnumUnknown&  marshal( interface ) ppenum) runtime managed internalcall
  {
  } // end of method IEnumUnknown::Clone

} // end of class DIALib.IEnumUnknown

.class interface public abstract auto ansi import DIALib.IDiaTable
       implements DIALib.IEnumUnknown
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 34 41 35 39 46 42 37 37 2D 41 42 41 43   // ..$4A59FB77-ABAC
                                                                                                  2D 34 36 39 42 2D 41 33 30 42 2D 39 45 43 43 38   // -469B-A30B-9ECC8
                                                                                                  35 42 46 45 46 31 34 00 00 )                      // 5BFEF14..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )                      // ...Item..
  .method public hidebysig newslot abstract virtual 
          instance void  RemoteNext([in] uint32 celt,
                                    [out] object&  marshal( iunknown ) rgelt,
                                    [out] uint32& pceltFetched) runtime managed internalcall
  {
  } // end of method IDiaTable::RemoteNext

  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method IDiaTable::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method IDiaTable::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class DIALib.IEnumUnknown&  marshal( interface ) ppenum) runtime managed internalcall
  {
  } // end of method IDiaTable::Clone

  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Collections.IEnumerator 
          marshal( custom ("System.Runtime.InteropServices.CustomMarshalers.EnumeratorToEnumVariantMarshaler, CustomMarshalers, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a","")) 
          GetEnumerator() runtime managed internalcall
  {
  } // end of method IDiaTable::GetEnumerator

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_name() runtime managed internalcall
  {
  } // end of method IDiaTable::get_name

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_count() runtime managed internalcall
  {
  } // end of method IDiaTable::get_count

  .method public hidebysig newslot abstract virtual 
          instance object 
          marshal( iunknown ) 
          Item([in] uint32 index) runtime managed internalcall
  {
  } // end of method IDiaTable::Item

  .property string name()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaTable::get_name()
  } // end of property IDiaTable::name
  .property int32 count()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaTable::get_count()
  } // end of property IDiaTable::count
} // end of class DIALib.IDiaTable

.class interface public abstract auto ansi import DIALib.IDiaInjectedSource
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 41 45 36 30 35 43 44 43 2D 38 31 30 35   // ..$AE605CDC-8105
                                                                                                  2D 34 41 32 33 2D 42 37 31 30 2D 33 32 35 39 46   // -4A23-B710-3259F
                                                                                                  31 45 32 36 31 31 32 00 00 )                      // 1E26112..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_crc() runtime managed internalcall
  {
  } // end of method IDiaInjectedSource::get_crc

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_length() runtime managed internalcall
  {
  } // end of method IDiaInjectedSource::get_length

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_fileName() runtime managed internalcall
  {
  } // end of method IDiaInjectedSource::get_fileName

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_objectFileName() runtime managed internalcall
  {
  } // end of method IDiaInjectedSource::get_objectFileName

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_virtualFilename() runtime managed internalcall
  {
  } // end of method IDiaInjectedSource::get_virtualFilename

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_sourceCompression() runtime managed internalcall
  {
  } // end of method IDiaInjectedSource::get_sourceCompression

  .method public hidebysig newslot abstract virtual 
          instance void  get_source([in] uint32 cbData,
                                    [out] uint32& pcbData,
                                    [out] uint8& pbData) runtime managed internalcall
  {
  } // end of method IDiaInjectedSource::get_source

  .property uint32 crc()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaInjectedSource::get_crc()
  } // end of property IDiaInjectedSource::crc
  .property uint64 length()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaInjectedSource::get_length()
  } // end of property IDiaInjectedSource::length
  .property string fileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaInjectedSource::get_fileName()
  } // end of property IDiaInjectedSource::fileName
  .property string objectFileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 04 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaInjectedSource::get_objectFileName()
  } // end of property IDiaInjectedSource::objectFileName
  .property string virtualFilename()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 05 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaInjectedSource::get_virtualFilename()
  } // end of property IDiaInjectedSource::virtualFilename
  .property uint32 sourceCompression()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 06 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaInjectedSource::get_sourceCompression()
  } // end of property IDiaInjectedSource::sourceCompression
} // end of class DIALib.IDiaInjectedSource

.class interface public abstract auto ansi import DIALib.IDiaEnumDebugStreamData
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 34 38 36 39 34 33 45 38 2D 44 31 38 37   // ..$486943E8-D187
                                                                                                  2D 34 41 36 42 2D 41 33 43 34 2D 32 39 31 32 35   // -4A6B-A3C4-29125
                                                                                                  39 46 46 46 36 30 44 00 00 )                      // 9FFF60D..
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )                      // ...Item..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Collections.IEnumerator 
          marshal( custom ("System.Runtime.InteropServices.CustomMarshalers.EnumeratorToEnumVariantMarshaler, CustomMarshalers, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a","")) 
          GetEnumerator() runtime managed internalcall
  {
  } // end of method IDiaEnumDebugStreamData::GetEnumerator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_count() runtime managed internalcall
  {
  } // end of method IDiaEnumDebugStreamData::get_count

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_name() runtime managed internalcall
  {
  } // end of method IDiaEnumDebugStreamData::get_name

  .method public hidebysig newslot abstract virtual 
          instance void  Item([in] uint32 index,
                              [in] uint32 cbData,
                              [out] uint32& pcbData,
                              [out] uint8& pbData) runtime managed internalcall
  {
  } // end of method IDiaEnumDebugStreamData::Item

  .method public hidebysig newslot abstract virtual 
          instance void  Next([in] uint32 celt,
                              [in] uint32 cbData,
                              [out] uint32& pcbData,
                              [out] uint8& pbData,
                              [out] uint32& pceltFetched) runtime managed internalcall
  {
  } // end of method IDiaEnumDebugStreamData::Next

  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method IDiaEnumDebugStreamData::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method IDiaEnumDebugStreamData::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class DIALib.IDiaEnumDebugStreamData&  marshal( interface ) ppenum) runtime managed internalcall
  {
  } // end of method IDiaEnumDebugStreamData::Clone

  .property int32 count()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaEnumDebugStreamData::get_count()
  } // end of property IDiaEnumDebugStreamData::count
  .property string name()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaEnumDebugStreamData::get_name()
  } // end of property IDiaEnumDebugStreamData::name
} // end of class DIALib.IDiaEnumDebugStreamData

.class public auto ansi sealed DIALib.MemoryTypeEnum
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype DIALib.MemoryTypeEnum MemTypeCode = int32(0x00000000)
  .field public static literal valuetype DIALib.MemoryTypeEnum MemTypeData = int32(0x00000001)
  .field public static literal valuetype DIALib.MemoryTypeEnum MemTypeStack = int32(0x00000002)
  .field public static literal valuetype DIALib.MemoryTypeEnum MemTypeCodeOnHeap = int32(0x00000003)
  .field public static literal valuetype DIALib.MemoryTypeEnum MemTypeAny = int32(0xFFFFFFFF)
} // end of class DIALib.MemoryTypeEnum

.class interface public abstract auto ansi import DIALib.IDiaFrameData
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 41 33 39 31 38 34 42 37 2D 36 41 33 36   // ..$A39184B7-6A36
                                                                                                  2D 34 32 44 45 2D 38 45 45 43 2D 37 44 46 39 46   // -42DE-8EEC-7DF9F
                                                                                                  33 46 35 39 46 33 33 00 00 )                      // 3F59F33..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_addressSection() runtime managed internalcall
  {
  } // end of method IDiaFrameData::get_addressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_addressOffset() runtime managed internalcall
  {
  } // end of method IDiaFrameData::get_addressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_relativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaFrameData::get_relativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_virtualAddress() runtime managed internalcall
  {
  } // end of method IDiaFrameData::get_virtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lengthBlock() runtime managed internalcall
  {
  } // end of method IDiaFrameData::get_lengthBlock

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lengthLocals() runtime managed internalcall
  {
  } // end of method IDiaFrameData::get_lengthLocals

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lengthParams() runtime managed internalcall
  {
  } // end of method IDiaFrameData::get_lengthParams

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_maxStack() runtime managed internalcall
  {
  } // end of method IDiaFrameData::get_maxStack

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lengthProlog() runtime managed internalcall
  {
  } // end of method IDiaFrameData::get_lengthProlog

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lengthSavedRegisters() runtime managed internalcall
  {
  } // end of method IDiaFrameData::get_lengthSavedRegisters

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_program() runtime managed internalcall
  {
  } // end of method IDiaFrameData::get_program

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_systemExceptionHandling() runtime managed internalcall
  {
  } // end of method IDiaFrameData::get_systemExceptionHandling

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_cplusplusExceptionHandling() runtime managed internalcall
  {
  } // end of method IDiaFrameData::get_cplusplusExceptionHandling

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_functionStart() runtime managed internalcall
  {
  } // end of method IDiaFrameData::get_functionStart

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_allocatesBasePointer() runtime managed internalcall
  {
  } // end of method IDiaFrameData::get_allocatesBasePointer

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_type() runtime managed internalcall
  {
  } // end of method IDiaFrameData::get_type

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaFrameData 
          marshal( interface ) 
          get_functionParent() runtime managed internalcall
  {
  } // end of method IDiaFrameData::get_functionParent

  .method public hidebysig newslot abstract virtual 
          instance void  execute(class DIALib.IDiaStackWalkFrame  marshal( interface ) frame) runtime managed internalcall
  {
  } // end of method IDiaFrameData::execute

  .property uint32 addressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaFrameData::get_addressSection()
  } // end of property IDiaFrameData::addressSection
  .property uint32 addressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaFrameData::get_addressOffset()
  } // end of property IDiaFrameData::addressOffset
  .property uint32 relativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 04 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaFrameData::get_relativeVirtualAddress()
  } // end of property IDiaFrameData::relativeVirtualAddress
  .property uint64 virtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 05 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaFrameData::get_virtualAddress()
  } // end of property IDiaFrameData::virtualAddress
  .property uint32 lengthBlock()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 06 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaFrameData::get_lengthBlock()
  } // end of property IDiaFrameData::lengthBlock
  .property uint32 lengthLocals()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 07 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaFrameData::get_lengthLocals()
  } // end of property IDiaFrameData::lengthLocals
  .property uint32 lengthParams()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaFrameData::get_lengthParams()
  } // end of property IDiaFrameData::lengthParams
  .property uint32 maxStack()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 09 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaFrameData::get_maxStack()
  } // end of property IDiaFrameData::maxStack
  .property uint32 lengthProlog()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0A 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaFrameData::get_lengthProlog()
  } // end of property IDiaFrameData::lengthProlog
  .property uint32 lengthSavedRegisters()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0B 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaFrameData::get_lengthSavedRegisters()
  } // end of property IDiaFrameData::lengthSavedRegisters
  .property string program()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0C 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaFrameData::get_program()
  } // end of property IDiaFrameData::program
  .property int32 systemExceptionHandling()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0D 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaFrameData::get_systemExceptionHandling()
  } // end of property IDiaFrameData::systemExceptionHandling
  .property int32 cplusplusExceptionHandling()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0E 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaFrameData::get_cplusplusExceptionHandling()
  } // end of property IDiaFrameData::cplusplusExceptionHandling
  .property int32 functionStart()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0F 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaFrameData::get_functionStart()
  } // end of property IDiaFrameData::functionStart
  .property int32 allocatesBasePointer()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 10 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaFrameData::get_allocatesBasePointer()
  } // end of property IDiaFrameData::allocatesBasePointer
  .property uint32 'type'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 11 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaFrameData::get_type()
  } // end of property IDiaFrameData::'type'
  .property class DIALib.IDiaFrameData functionParent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 12 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaFrameData DIALib.IDiaFrameData::get_functionParent()
  } // end of property IDiaFrameData::functionParent
} // end of class DIALib.IDiaFrameData

.class interface public abstract auto ansi import DIALib.IDiaStackWalkFrame
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 37 43 35 39 30 43 31 2D 34 33 38 44   // ..$07C590C1-438D
                                                                                                  2D 34 46 34 37 2D 42 44 43 44 2D 34 33 39 37 42   // -4F47-BDCD-4397B
                                                                                                  43 38 31 41 44 37 35 00 00 )                      // C81AD75..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_registerValue([in] uint32 index) runtime managed internalcall
  {
  } // end of method IDiaStackWalkFrame::get_registerValue

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_registerValue([in] uint32 index,
                                           [in] uint64 pRetVal) runtime managed internalcall
  {
  } // end of method IDiaStackWalkFrame::set_registerValue

  .method public hidebysig newslot abstract virtual 
          instance void  readMemory([in] valuetype DIALib.MemoryTypeEnum 'type',
                                    [in] uint64 va,
                                    [in] uint32 cbData,
                                    [out] uint32& pcbData,
                                    [out] uint8& pbData) runtime managed internalcall
  {
  } // end of method IDiaStackWalkFrame::readMemory

  .method public hidebysig newslot abstract virtual 
          instance void  searchForReturnAddress([in] class DIALib.IDiaFrameData  marshal( interface ) frame,
                                                [out] uint64& returnAddress) runtime managed internalcall
  {
  } // end of method IDiaStackWalkFrame::searchForReturnAddress

  .method public hidebysig newslot abstract virtual 
          instance void  searchForReturnAddressStart([in] class DIALib.IDiaFrameData  marshal( interface ) frame,
                                                     [in] uint64 startAddress,
                                                     [out] uint64& returnAddress) runtime managed internalcall
  {
  } // end of method IDiaStackWalkFrame::searchForReturnAddressStart

  .property uint64 registerValue(uint32)
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .set instance void DIALib.IDiaStackWalkFrame::set_registerValue(uint32,
                                                                    uint64)
    .get instance uint64 DIALib.IDiaStackWalkFrame::get_registerValue(uint32)
  } // end of property IDiaStackWalkFrame::registerValue
} // end of class DIALib.IDiaStackWalkFrame

.class interface public abstract auto ansi import DIALib.IDiaStackFrame
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 35 45 44 42 43 39 36 44 2D 43 44 44 36   // ..$5EDBC96D-CDD6
                                                                                                  2D 34 37 39 32 2D 41 46 42 45 2D 43 43 38 39 30   // -4792-AFBE-CC890
                                                                                                  30 37 44 39 36 31 30 00 00 )                      // 07D9610..
  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_type() runtime managed internalcall
  {
  } // end of method IDiaStackFrame::get_type

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_base() runtime managed internalcall
  {
  } // end of method IDiaStackFrame::get_base

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_size() runtime managed internalcall
  {
  } // end of method IDiaStackFrame::get_size

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_returnAddress() runtime managed internalcall
  {
  } // end of method IDiaStackFrame::get_returnAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_localsBase() runtime managed internalcall
  {
  } // end of method IDiaStackFrame::get_localsBase

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lengthLocals() runtime managed internalcall
  {
  } // end of method IDiaStackFrame::get_lengthLocals

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lengthParams() runtime managed internalcall
  {
  } // end of method IDiaStackFrame::get_lengthParams

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lengthProlog() runtime managed internalcall
  {
  } // end of method IDiaStackFrame::get_lengthProlog

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lengthSavedRegisters() runtime managed internalcall
  {
  } // end of method IDiaStackFrame::get_lengthSavedRegisters

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_systemExceptionHandling() runtime managed internalcall
  {
  } // end of method IDiaStackFrame::get_systemExceptionHandling

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_cplusplusExceptionHandling() runtime managed internalcall
  {
  } // end of method IDiaStackFrame::get_cplusplusExceptionHandling

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_functionStart() runtime managed internalcall
  {
  } // end of method IDiaStackFrame::get_functionStart

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_allocatesBasePointer() runtime managed internalcall
  {
  } // end of method IDiaStackFrame::get_allocatesBasePointer

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_maxStack() runtime managed internalcall
  {
  } // end of method IDiaStackFrame::get_maxStack

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_registerValue([in] uint32 index) runtime managed internalcall
  {
  } // end of method IDiaStackFrame::get_registerValue

  .property uint32 'type'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaStackFrame::get_type()
  } // end of property IDiaStackFrame::'type'
  .property uint64 base()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaStackFrame::get_base()
  } // end of property IDiaStackFrame::base
  .property uint32 size()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaStackFrame::get_size()
  } // end of property IDiaStackFrame::size
  .property uint64 returnAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 04 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaStackFrame::get_returnAddress()
  } // end of property IDiaStackFrame::returnAddress
  .property uint64 localsBase()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 05 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaStackFrame::get_localsBase()
  } // end of property IDiaStackFrame::localsBase
  .property uint32 lengthLocals()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 06 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaStackFrame::get_lengthLocals()
  } // end of property IDiaStackFrame::lengthLocals
  .property uint32 lengthParams()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 07 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaStackFrame::get_lengthParams()
  } // end of property IDiaStackFrame::lengthParams
  .property uint32 lengthProlog()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaStackFrame::get_lengthProlog()
  } // end of property IDiaStackFrame::lengthProlog
  .property uint32 lengthSavedRegisters()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 09 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaStackFrame::get_lengthSavedRegisters()
  } // end of property IDiaStackFrame::lengthSavedRegisters
  .property int32 systemExceptionHandling()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0A 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaStackFrame::get_systemExceptionHandling()
  } // end of property IDiaStackFrame::systemExceptionHandling
  .property int32 cplusplusExceptionHandling()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0B 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaStackFrame::get_cplusplusExceptionHandling()
  } // end of property IDiaStackFrame::cplusplusExceptionHandling
  .property int32 functionStart()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0C 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaStackFrame::get_functionStart()
  } // end of property IDiaStackFrame::functionStart
  .property int32 allocatesBasePointer()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0D 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaStackFrame::get_allocatesBasePointer()
  } // end of property IDiaStackFrame::allocatesBasePointer
  .property uint32 maxStack()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0E 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaStackFrame::get_maxStack()
  } // end of property IDiaStackFrame::maxStack
  .property uint64 registerValue(uint32)
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0F 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaStackFrame::get_registerValue(uint32)
  } // end of property IDiaStackFrame::registerValue
} // end of class DIALib.IDiaStackFrame

.class interface public abstract auto ansi import DIALib.IDiaSectionContrib
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 43 46 34 42 36 30 45 2D 33 35 42 31   // ..$0CF4B60E-35B1
                                                                                                  2D 34 43 36 43 2D 42 44 44 38 2D 38 35 34 42 39   // -4C6C-BDD8-854B9
                                                                                                  43 38 45 33 38 35 37 00 00 )                      // C8E3857..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_compiland() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_compiland

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_addressSection() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_addressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_addressOffset() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_addressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_relativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_relativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_virtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_virtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_length() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_length

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_notPaged() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_notPaged

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_code() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_code

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_initializedData() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_initializedData

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_uninitializedData() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_uninitializedData

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_remove() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_remove

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_comdat() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_comdat

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_discardable() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_discardable

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_notCached() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_notCached

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_share() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_share

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_execute() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_execute

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_read() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_read

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_write() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_write

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_dataCrc() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_dataCrc

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_relocationsCrc() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_relocationsCrc

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_compilandId() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_compilandId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_code16bit() runtime managed internalcall
  {
  } // end of method IDiaSectionContrib::get_code16bit

  .property class DIALib.IDiaSymbol compiland()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSectionContrib::get_compiland()
  } // end of property IDiaSectionContrib::compiland
  .property uint32 addressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSectionContrib::get_addressSection()
  } // end of property IDiaSectionContrib::addressSection
  .property uint32 addressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSectionContrib::get_addressOffset()
  } // end of property IDiaSectionContrib::addressOffset
  .property uint32 relativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 04 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSectionContrib::get_relativeVirtualAddress()
  } // end of property IDiaSectionContrib::relativeVirtualAddress
  .property uint64 virtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 05 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSectionContrib::get_virtualAddress()
  } // end of property IDiaSectionContrib::virtualAddress
  .property uint32 length()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 06 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSectionContrib::get_length()
  } // end of property IDiaSectionContrib::length
  .property int32 notPaged()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSectionContrib::get_notPaged()
  } // end of property IDiaSectionContrib::notPaged
  .property int32 code()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 09 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSectionContrib::get_code()
  } // end of property IDiaSectionContrib::code
  .property int32 initializedData()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0A 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSectionContrib::get_initializedData()
  } // end of property IDiaSectionContrib::initializedData
  .property int32 uninitializedData()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0B 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSectionContrib::get_uninitializedData()
  } // end of property IDiaSectionContrib::uninitializedData
  .property int32 remove()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0C 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSectionContrib::get_remove()
  } // end of property IDiaSectionContrib::remove
  .property int32 comdat()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0D 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSectionContrib::get_comdat()
  } // end of property IDiaSectionContrib::comdat
  .property int32 discardable()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0E 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSectionContrib::get_discardable()
  } // end of property IDiaSectionContrib::discardable
  .property int32 notCached()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0F 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSectionContrib::get_notCached()
  } // end of property IDiaSectionContrib::notCached
  .property int32 share()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 10 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSectionContrib::get_share()
  } // end of property IDiaSectionContrib::share
  .property int32 execute()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 11 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSectionContrib::get_execute()
  } // end of property IDiaSectionContrib::execute
  .property int32 read()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 12 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSectionContrib::get_read()
  } // end of property IDiaSectionContrib::read
  .property int32 write()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 13 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSectionContrib::get_write()
  } // end of property IDiaSectionContrib::write
  .property uint32 dataCrc()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 14 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSectionContrib::get_dataCrc()
  } // end of property IDiaSectionContrib::dataCrc
  .property uint32 relocationsCrc()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 15 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSectionContrib::get_relocationsCrc()
  } // end of property IDiaSectionContrib::relocationsCrc
  .property uint32 compilandId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 16 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSectionContrib::get_compilandId()
  } // end of property IDiaSectionContrib::compilandId
  .property int32 code16bit()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 17 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSectionContrib::get_code16bit()
  } // end of property IDiaSectionContrib::code16bit
} // end of class DIALib.IDiaSectionContrib

.class interface public abstract auto ansi import DIALib.IDiaEnumSectionContribs
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )                      // ...Item..
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 31 39 39 34 44 45 42 32 2D 32 43 38 32   // ..$1994DEB2-2C82
                                                                                                  2D 34 42 31 44 2D 41 35 37 46 2D 41 46 46 34 32   // -4B1D-A57F-AFF42
                                                                                                  34 44 35 34 41 36 38 00 00 )                      // 4D54A68..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Collections.IEnumerator 
          marshal( custom ("System.Runtime.InteropServices.CustomMarshalers.EnumeratorToEnumVariantMarshaler, CustomMarshalers, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a","")) 
          GetEnumerator() runtime managed internalcall
  {
  } // end of method IDiaEnumSectionContribs::GetEnumerator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_count() runtime managed internalcall
  {
  } // end of method IDiaEnumSectionContribs::get_count

  .method public hidebysig newslot abstract virtual 
          instance class DIALib.IDiaSectionContrib 
          marshal( interface ) 
          Item([in] uint32 index) runtime managed internalcall
  {
  } // end of method IDiaEnumSectionContribs::Item

  .method public hidebysig newslot abstract virtual 
          instance void  Next([in] uint32 celt,
                              [out] class DIALib.IDiaSectionContrib&  marshal( interface ) rgelt,
                              [out] uint32& pceltFetched) runtime managed internalcall
  {
  } // end of method IDiaEnumSectionContribs::Next

  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method IDiaEnumSectionContribs::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method IDiaEnumSectionContribs::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class DIALib.IDiaEnumSectionContribs&  marshal( interface ) ppenum) runtime managed internalcall
  {
  } // end of method IDiaEnumSectionContribs::Clone

  .property int32 count()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaEnumSectionContribs::get_count()
  } // end of property IDiaEnumSectionContribs::count
} // end of class DIALib.IDiaEnumSectionContribs

.class interface public abstract auto ansi import DIALib.IDiaSymbol2
       implements DIALib.IDiaSymbol
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 0A 73 79 6D 49 6E 64 65 78 49 64 00 00 )    // ...symIndexId..
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 36 31 31 45 38 36 43 44 2D 42 37 44 31   // ..$611E86CD-B7D1
                                                                                                  2D 34 35 34 36 2D 38 41 31 35 2D 30 37 30 45 32   // -4546-8A15-070E2
                                                                                                  42 30 37 41 34 32 37 00 00 )                      // B07A427..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_symIndexId() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_symIndexId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_symTag() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_symTag

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_name() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_name

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_lexicalParent() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_lexicalParent

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_classParent() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_classParent

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_type() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_type

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_dataKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_dataKind

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_locationType() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_locationType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_addressSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_addressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_addressOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_addressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_relativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_relativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_virtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_virtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_registerId() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_registerId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_offset() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_offset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_length() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_length

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_slot() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_slot

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_volatileType() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_volatileType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_constType() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_constType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_unalignedType() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_unalignedType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_access() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_access

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_libraryName() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_libraryName

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_platform() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_platform

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_language() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_language

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_editAndContinueEnabled() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_editAndContinueEnabled

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndMajor() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_frontEndMajor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndMinor() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_frontEndMinor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndBuild() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_frontEndBuild

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndMajor() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_backEndMajor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndMinor() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_backEndMinor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndBuild() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_backEndBuild

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_sourceFileName() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_sourceFileName

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_unused() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_unused

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_thunkOrdinal() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_thunkOrdinal

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_thisAdjust() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_thisAdjust

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_virtualBaseOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_virtualBaseOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_virtual() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_virtual

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_intro() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_intro

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_pure() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_pure

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_callingConvention() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_callingConvention

  .method public hidebysig newslot specialname abstract virtual 
          instance object 
          marshal( struct) 
          get_value() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_value

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseType() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_baseType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_token() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_token

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_timeStamp() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_timeStamp

  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype [mscorlib]System.Guid 
          get_guid() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_guid

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_symbolsFileName() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_symbolsFileName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_reference() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_reference

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_count() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_count

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bitPosition() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_bitPosition

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_arrayIndexType() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_arrayIndexType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_packed() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_packed

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_constructor() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_constructor

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_overloadedOperator() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_overloadedOperator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_nested() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_nested

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasNestedTypes() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_hasNestedTypes

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasAssignmentOperator() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_hasAssignmentOperator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasCastOperator() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_hasCastOperator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_scoped() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_scoped

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_virtualBaseClass() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_virtualBaseClass

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_indirectVirtualBaseClass() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_indirectVirtualBaseClass

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_virtualBasePointerOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_virtualBasePointerOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_virtualTableShape() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_virtualTableShape

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lexicalParentId() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_lexicalParentId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_classParentId() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_classParentId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_typeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_typeId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_arrayIndexTypeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_arrayIndexTypeId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_virtualTableShapeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_virtualTableShapeId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_code() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_code

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_function() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_function

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_managed() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_managed

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_msil() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_msil

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_virtualBaseDispIndex() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_virtualBaseDispIndex

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_undecoratedName() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_undecoratedName

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_age() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_age

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_signature() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_signature

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_compilerGenerated() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_compilerGenerated

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_addressTaken() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_addressTaken

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_rank() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_rank

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_lowerBound() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_lowerBound

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_upperBound() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_upperBound

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lowerBoundId() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_lowerBoundId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_upperBoundId() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_upperBoundId

  .method public hidebysig newslot abstract virtual 
          instance void  get_dataBytes([in] uint32 cbData,
                                       [out] uint32& pcbData,
                                       [out] uint8& pbData) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_dataBytes

  .method public hidebysig newslot abstract virtual 
          instance void  findChildren([in] valuetype DIALib.SymTagEnum symTag,
                                      [in] string  marshal( lpwstr) name,
                                      [in] uint32 compareFlags,
                                      [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::findChildren

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenEx([in] valuetype DIALib.SymTagEnum symTag,
                                        [in] string  marshal( lpwstr) name,
                                        [in] uint32 compareFlags,
                                        [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::findChildrenEx

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByAddr([in] valuetype DIALib.SymTagEnum symTag,
                                              [in] string  marshal( lpwstr) name,
                                              [in] uint32 compareFlags,
                                              [in] uint32 isect,
                                              [in] uint32 offset,
                                              [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::findChildrenExByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByVA([in] valuetype DIALib.SymTagEnum symTag,
                                            [in] string  marshal( lpwstr) name,
                                            [in] uint32 compareFlags,
                                            [in] uint64 va,
                                            [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::findChildrenExByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByRVA([in] valuetype DIALib.SymTagEnum symTag,
                                             [in] string  marshal( lpwstr) name,
                                             [in] uint32 compareFlags,
                                             [in] uint32 rva,
                                             [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::findChildrenExByRVA

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_targetSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_targetSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_targetOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_targetOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_targetRelativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_targetRelativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_targetVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_targetVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_machineType() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_machineType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_oemId() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_oemId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_oemSymbolId() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_oemSymbolId

  .method public hidebysig newslot abstract virtual 
          instance void  get_types([in] uint32 cTypes,
                                   [out] uint32& pcTypes,
                                   [out] class DIALib.IDiaSymbol&  marshal( interface ) pTypes) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_types

  .method public hidebysig newslot abstract virtual 
          instance void  get_typeIds([in] uint32 cTypeIds,
                                     [out] uint32& pcTypeIds,
                                     [out] uint32& pdwTypeIds) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_typeIds

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_objectPointerType() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_objectPointerType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_udtKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_udtKind

  .method public hidebysig newslot abstract virtual 
          instance void  get_undecoratedNameEx([in] uint32 undecorateOptions,
                                               [out] string&  marshal( bstr) name) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_undecoratedNameEx

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noReturn() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_noReturn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_customCallingConvention() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_customCallingConvention

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noInline() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_noInline

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_optimizedCodeDebugInfo() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_optimizedCodeDebugInfo

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_notReached() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_notReached

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_interruptReturn() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_interruptReturn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_farReturn() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_farReturn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isStatic() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isStatic

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasDebugInfo() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_hasDebugInfo

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isLTCG() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isLTCG

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isDataAligned() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isDataAligned

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasSecurityChecks() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_hasSecurityChecks

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_compilerName() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_compilerName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasAlloca() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_hasAlloca

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasSetJump() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_hasSetJump

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasLongJump() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_hasLongJump

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasInlAsm() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_hasInlAsm

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasEH() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_hasEH

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasSEH() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_hasSEH

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasEHa() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_hasEHa

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isNaked() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isNaked

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAggregated() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isAggregated

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSplitted() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isSplitted

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_container() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_container

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_inlSpec() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_inlSpec

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noStackOrdering() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_noStackOrdering

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_virtualBaseTableType() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_virtualBaseTableType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasManagedCode() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_hasManagedCode

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isHotpatchable() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isHotpatchable

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isCVTCIL() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isCVTCIL

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isMSILNetmodule() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isMSILNetmodule

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isCTypes() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isCTypes

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isStripped() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isStripped

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndQFE() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_frontEndQFE

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndQFE() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_backEndQFE

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_wasInlined() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_wasInlined

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_strictGSCheck() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_strictGSCheck

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isCxxReturnUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isCxxReturnUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isConstructorVirtualBase() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isConstructorVirtualBase

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_RValueReference() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_RValueReference

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_unmodifiedType() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_unmodifiedType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_framePointerPresent() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_framePointerPresent

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSafeBuffers() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isSafeBuffers

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_intrinsic() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_intrinsic

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_sealed() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_sealed

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hfaFloat() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_hfaFloat

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hfaDouble() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_hfaDouble

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_liveRangeStartAddressSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_liveRangeStartAddressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_liveRangeStartAddressOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_liveRangeStartAddressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_liveRangeStartRelativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_liveRangeStartRelativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_countLiveRanges() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_countLiveRanges

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_liveRangeLength() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_liveRangeLength

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_offsetInUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_offsetInUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_paramBasePointerRegisterId() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_paramBasePointerRegisterId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_localBasePointerRegisterId() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_localBasePointerRegisterId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isLocationControlFlowDependent() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isLocationControlFlowDependent

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_stride() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_stride

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfRows() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_numberOfRows

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfColumns() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_numberOfColumns

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isMatrixRowMajor() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isMatrixRowMajor

  .method public hidebysig newslot abstract virtual 
          instance void  get_numericProperties([in] uint32 cnt,
                                               [out] uint32& pcnt,
                                               [out] uint32& pProperties) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_numericProperties

  .method public hidebysig newslot abstract virtual 
          instance void  get_modifierValues([in] uint32 cnt,
                                            [out] uint32& pcnt,
                                            [out] uint16& pModifiers) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_modifierValues

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isReturnValue() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isReturnValue

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isOptimizedAway() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isOptimizedAway

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_builtInKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_builtInKind

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_registerType() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_registerType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseDataSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_baseDataSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseDataOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_baseDataOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_textureSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_textureSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_samplerSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_samplerSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_uavSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_uavSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_sizeInUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_sizeInUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_memorySpaceKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_memorySpaceKind

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_unmodifiedTypeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_unmodifiedTypeId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_subTypeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_subTypeId

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_subType() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_subType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfModifiers() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_numberOfModifiers

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfRegisterIndices() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_numberOfRegisterIndices

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isHLSLData() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isHLSLData

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPointerToDataMember() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isPointerToDataMember

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPointerToMemberFunction() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isPointerToMemberFunction

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSingleInheritance() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isSingleInheritance

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isMultipleInheritance() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isMultipleInheritance

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isVirtualInheritance() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isVirtualInheritance

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_restrictedType() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_restrictedType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPointerBasedOnSymbolValue() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isPointerBasedOnSymbolValue

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_baseSymbol() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_baseSymbol

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseSymbolId() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_baseSymbolId

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_objectFileName() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_objectFileName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAcceleratorGroupSharedLocal() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isAcceleratorGroupSharedLocal

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAcceleratorPointerTagLiveRange() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isAcceleratorPointerTagLiveRange

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAcceleratorStubFunction() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isAcceleratorStubFunction

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfAcceleratorPointerTags() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_numberOfAcceleratorPointerTags

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSdl() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isSdl

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isWinRTPointer() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isWinRTPointer

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isRefUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isRefUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isValueUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isValueUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isInterfaceUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isInterfaceUdt

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByAddr([in] uint32 isect,
                                                [in] uint32 offset,
                                                [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::findInlineFramesByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByRVA([in] uint32 rva,
                                               [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::findInlineFramesByRVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByVA([in] uint64 va,
                                              [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::findInlineFramesByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLines([out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::findInlineeLines

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByAddr([in] uint32 isect,
                                                [in] uint32 offset,
                                                [in] uint32 length,
                                                [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::findInlineeLinesByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByRVA([in] uint32 rva,
                                               [in] uint32 length,
                                               [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::findInlineeLinesByRVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByVA([in] uint64 va,
                                              [in] uint32 length,
                                              [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::findInlineeLinesByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findSymbolsForAcceleratorPointerTag([in] uint32 tagValue,
                                                             [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::findSymbolsForAcceleratorPointerTag

  .method public hidebysig newslot abstract virtual 
          instance void  findSymbolsByRVAForAcceleratorPointerTag([in] uint32 tagValue,
                                                                  [in] uint32 rva,
                                                                  [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::findSymbolsByRVAForAcceleratorPointerTag

  .method public hidebysig newslot abstract virtual 
          instance void  get_acceleratorPointerTags([in] uint32 cnt,
                                                    [out] uint32& pcnt,
                                                    [out] uint32& pPointerTags) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_acceleratorPointerTags

  .method public hidebysig newslot abstract virtual 
          instance void  getSrcLineOnTypeDefn([out] class DIALib.IDiaLineNumber&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::getSrcLineOnTypeDefn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPGO() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isPGO

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasValidPGOCounts() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_hasValidPGOCounts

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isOptimizedForSpeed() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isOptimizedForSpeed

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_PGOEntryCount() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_PGOEntryCount

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_PGOEdgeCount() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_PGOEdgeCount

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_PGODynamicInstructionCount() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_PGODynamicInstructionCount

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_staticSize() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_staticSize

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_finalLiveStaticSize() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_finalLiveStaticSize

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_phaseName() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_phaseName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasControlFlowCheck() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_hasControlFlowCheck

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_constantExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_constantExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_dataExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_dataExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_privateExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_privateExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noNameExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_noNameExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_exportHasExplicitlyAssignedOrdinal() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_exportHasExplicitlyAssignedOrdinal

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_exportIsForwarder() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_exportIsForwarder

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_ordinal() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_ordinal

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frameSize() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_frameSize

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_exceptionHandlerAddressSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_exceptionHandlerAddressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_exceptionHandlerAddressOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_exceptionHandlerAddressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_exceptionHandlerRelativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_exceptionHandlerRelativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_exceptionHandlerVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_exceptionHandlerVirtualAddress

  .method public hidebysig newslot abstract virtual 
          instance void  findInputAssemblyFile([out] class DIALib.IDiaInputAssemblyFile&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol2::findInputAssemblyFile

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_characteristics() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_characteristics

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_coffGroup() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_coffGroup

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bindID() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_bindID

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bindSpace() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_bindSpace

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bindSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_bindSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isObjCClass() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isObjCClass

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isObjCCategory() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isObjCCategory

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isObjCProtocol() runtime managed internalcall
  {
  } // end of method IDiaSymbol2::get_isObjCProtocol

  .property uint32 symIndexId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_symIndexId()
  } // end of property IDiaSymbol2::symIndexId
  .property uint32 symTag()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_symTag()
  } // end of property IDiaSymbol2::symTag
  .property string name()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol2::get_name()
  } // end of property IDiaSymbol2::name
  .property class DIALib.IDiaSymbol lexicalParent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol2::get_lexicalParent()
  } // end of property IDiaSymbol2::lexicalParent
  .property class DIALib.IDiaSymbol classParent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 04 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol2::get_classParent()
  } // end of property IDiaSymbol2::classParent
  .property class DIALib.IDiaSymbol 'type'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 05 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol2::get_type()
  } // end of property IDiaSymbol2::'type'
  .property uint32 dataKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 06 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_dataKind()
  } // end of property IDiaSymbol2::dataKind
  .property uint32 locationType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 07 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_locationType()
  } // end of property IDiaSymbol2::locationType
  .property uint32 addressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_addressSection()
  } // end of property IDiaSymbol2::addressSection
  .property uint32 addressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 09 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_addressOffset()
  } // end of property IDiaSymbol2::addressOffset
  .property uint32 relativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0A 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_relativeVirtualAddress()
  } // end of property IDiaSymbol2::relativeVirtualAddress
  .property uint64 virtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0B 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol2::get_virtualAddress()
  } // end of property IDiaSymbol2::virtualAddress
  .property uint32 registerId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_registerId()
  } // end of property IDiaSymbol2::registerId
  .property int32 offset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0D 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_offset()
  } // end of property IDiaSymbol2::offset
  .property uint64 length()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0E 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol2::get_length()
  } // end of property IDiaSymbol2::length
  .property uint32 slot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_slot()
  } // end of property IDiaSymbol2::slot
  .property int32 volatileType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 10 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_volatileType()
  } // end of property IDiaSymbol2::volatileType
  .property int32 constType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 11 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_constType()
  } // end of property IDiaSymbol2::constType
  .property int32 unalignedType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 12 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_unalignedType()
  } // end of property IDiaSymbol2::unalignedType
  .property uint32 access()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 13 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_access()
  } // end of property IDiaSymbol2::access
  .property string libraryName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 14 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol2::get_libraryName()
  } // end of property IDiaSymbol2::libraryName
  .property uint32 platform()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 15 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_platform()
  } // end of property IDiaSymbol2::platform
  .property uint32 language()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 16 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_language()
  } // end of property IDiaSymbol2::language
  .property int32 editAndContinueEnabled()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 17 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_editAndContinueEnabled()
  } // end of property IDiaSymbol2::editAndContinueEnabled
  .property uint32 frontEndMajor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 18 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_frontEndMajor()
  } // end of property IDiaSymbol2::frontEndMajor
  .property uint32 frontEndMinor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 19 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_frontEndMinor()
  } // end of property IDiaSymbol2::frontEndMinor
  .property uint32 frontEndBuild()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1A 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_frontEndBuild()
  } // end of property IDiaSymbol2::frontEndBuild
  .property uint32 backEndMajor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1B 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_backEndMajor()
  } // end of property IDiaSymbol2::backEndMajor
  .property uint32 backEndMinor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_backEndMinor()
  } // end of property IDiaSymbol2::backEndMinor
  .property uint32 backEndBuild()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1D 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_backEndBuild()
  } // end of property IDiaSymbol2::backEndBuild
  .property string sourceFileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1E 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol2::get_sourceFileName()
  } // end of property IDiaSymbol2::sourceFileName
  .property string 'unused'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1F 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol2::get_unused()
  } // end of property IDiaSymbol2::'unused'
  .property uint32 thunkOrdinal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 20 00 00 00 00 00 )                         // .. .....
    .get instance uint32 DIALib.IDiaSymbol2::get_thunkOrdinal()
  } // end of property IDiaSymbol2::thunkOrdinal
  .property int32 thisAdjust()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 21 00 00 00 00 00 )                         // ..!.....
    .get instance int32 DIALib.IDiaSymbol2::get_thisAdjust()
  } // end of property IDiaSymbol2::thisAdjust
  .property uint32 virtualBaseOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 22 00 00 00 00 00 )                         // ..".....
    .get instance uint32 DIALib.IDiaSymbol2::get_virtualBaseOffset()
  } // end of property IDiaSymbol2::virtualBaseOffset
  .property int32 'virtual'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 23 00 00 00 00 00 )                         // ..#.....
    .get instance int32 DIALib.IDiaSymbol2::get_virtual()
  } // end of property IDiaSymbol2::'virtual'
  .property int32 intro()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 24 00 00 00 00 00 )                         // ..$.....
    .get instance int32 DIALib.IDiaSymbol2::get_intro()
  } // end of property IDiaSymbol2::intro
  .property int32 pure()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 25 00 00 00 00 00 )                         // ..%.....
    .get instance int32 DIALib.IDiaSymbol2::get_pure()
  } // end of property IDiaSymbol2::pure
  .property uint32 callingConvention()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 26 00 00 00 00 00 )                         // ..&.....
    .get instance uint32 DIALib.IDiaSymbol2::get_callingConvention()
  } // end of property IDiaSymbol2::callingConvention
  .property object 'value'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 27 00 00 00 00 00 )                         // ..'.....
    .get instance object DIALib.IDiaSymbol2::get_value()
  } // end of property IDiaSymbol2::'value'
  .property uint32 baseType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 28 00 00 00 00 00 )                         // ..(.....
    .get instance uint32 DIALib.IDiaSymbol2::get_baseType()
  } // end of property IDiaSymbol2::baseType
  .property uint32 token()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 29 00 00 00 00 00 )                         // ..).....
    .get instance uint32 DIALib.IDiaSymbol2::get_token()
  } // end of property IDiaSymbol2::token
  .property uint32 timeStamp()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2A 00 00 00 00 00 )                         // ..*.....
    .get instance uint32 DIALib.IDiaSymbol2::get_timeStamp()
  } // end of property IDiaSymbol2::timeStamp
  .property valuetype [mscorlib]System.Guid
          guid()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2B 00 00 00 00 00 )                         // ..+.....
    .get instance valuetype [mscorlib]System.Guid DIALib.IDiaSymbol2::get_guid()
  } // end of property IDiaSymbol2::guid
  .property string symbolsFileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2C 00 00 00 00 00 )                         // ..,.....
    .get instance string DIALib.IDiaSymbol2::get_symbolsFileName()
  } // end of property IDiaSymbol2::symbolsFileName
  .property int32 reference()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2E 00 00 00 00 00 )                         // ........
    .get instance int32 DIALib.IDiaSymbol2::get_reference()
  } // end of property IDiaSymbol2::reference
  .property uint32 count()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2F 00 00 00 00 00 )                         // ../.....
    .get instance uint32 DIALib.IDiaSymbol2::get_count()
  } // end of property IDiaSymbol2::count
  .property uint32 bitPosition()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 31 00 00 00 00 00 )                         // ..1.....
    .get instance uint32 DIALib.IDiaSymbol2::get_bitPosition()
  } // end of property IDiaSymbol2::bitPosition
  .property class DIALib.IDiaSymbol arrayIndexType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 32 00 00 00 00 00 )                         // ..2.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol2::get_arrayIndexType()
  } // end of property IDiaSymbol2::arrayIndexType
  .property int32 packed()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 33 00 00 00 00 00 )                         // ..3.....
    .get instance int32 DIALib.IDiaSymbol2::get_packed()
  } // end of property IDiaSymbol2::packed
  .property int32 constructor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 34 00 00 00 00 00 )                         // ..4.....
    .get instance int32 DIALib.IDiaSymbol2::get_constructor()
  } // end of property IDiaSymbol2::constructor
  .property int32 overloadedOperator()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 35 00 00 00 00 00 )                         // ..5.....
    .get instance int32 DIALib.IDiaSymbol2::get_overloadedOperator()
  } // end of property IDiaSymbol2::overloadedOperator
  .property int32 'nested'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 36 00 00 00 00 00 )                         // ..6.....
    .get instance int32 DIALib.IDiaSymbol2::get_nested()
  } // end of property IDiaSymbol2::'nested'
  .property int32 hasNestedTypes()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 37 00 00 00 00 00 )                         // ..7.....
    .get instance int32 DIALib.IDiaSymbol2::get_hasNestedTypes()
  } // end of property IDiaSymbol2::hasNestedTypes
  .property int32 hasAssignmentOperator()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 38 00 00 00 00 00 )                         // ..8.....
    .get instance int32 DIALib.IDiaSymbol2::get_hasAssignmentOperator()
  } // end of property IDiaSymbol2::hasAssignmentOperator
  .property int32 hasCastOperator()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 39 00 00 00 00 00 )                         // ..9.....
    .get instance int32 DIALib.IDiaSymbol2::get_hasCastOperator()
  } // end of property IDiaSymbol2::hasCastOperator
  .property int32 scoped()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3A 00 00 00 00 00 )                         // ..:.....
    .get instance int32 DIALib.IDiaSymbol2::get_scoped()
  } // end of property IDiaSymbol2::scoped
  .property int32 virtualBaseClass()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3B 00 00 00 00 00 )                         // ..;.....
    .get instance int32 DIALib.IDiaSymbol2::get_virtualBaseClass()
  } // end of property IDiaSymbol2::virtualBaseClass
  .property int32 indirectVirtualBaseClass()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3C 00 00 00 00 00 )                         // ..<.....
    .get instance int32 DIALib.IDiaSymbol2::get_indirectVirtualBaseClass()
  } // end of property IDiaSymbol2::indirectVirtualBaseClass
  .property int32 virtualBasePointerOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3D 00 00 00 00 00 )                         // ..=.....
    .get instance int32 DIALib.IDiaSymbol2::get_virtualBasePointerOffset()
  } // end of property IDiaSymbol2::virtualBasePointerOffset
  .property class DIALib.IDiaSymbol virtualTableShape()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3E 00 00 00 00 00 )                         // ..>.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol2::get_virtualTableShape()
  } // end of property IDiaSymbol2::virtualTableShape
  .property uint32 lexicalParentId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 40 00 00 00 00 00 )                         // ..@.....
    .get instance uint32 DIALib.IDiaSymbol2::get_lexicalParentId()
  } // end of property IDiaSymbol2::lexicalParentId
  .property uint32 classParentId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 41 00 00 00 00 00 )                         // ..A.....
    .get instance uint32 DIALib.IDiaSymbol2::get_classParentId()
  } // end of property IDiaSymbol2::classParentId
  .property uint32 typeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 42 00 00 00 00 00 )                         // ..B.....
    .get instance uint32 DIALib.IDiaSymbol2::get_typeId()
  } // end of property IDiaSymbol2::typeId
  .property uint32 arrayIndexTypeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 43 00 00 00 00 00 )                         // ..C.....
    .get instance uint32 DIALib.IDiaSymbol2::get_arrayIndexTypeId()
  } // end of property IDiaSymbol2::arrayIndexTypeId
  .property uint32 virtualTableShapeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 44 00 00 00 00 00 )                         // ..D.....
    .get instance uint32 DIALib.IDiaSymbol2::get_virtualTableShapeId()
  } // end of property IDiaSymbol2::virtualTableShapeId
  .property int32 code()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 45 00 00 00 00 00 )                         // ..E.....
    .get instance int32 DIALib.IDiaSymbol2::get_code()
  } // end of property IDiaSymbol2::code
  .property int32 function()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 46 00 00 00 00 00 )                         // ..F.....
    .get instance int32 DIALib.IDiaSymbol2::get_function()
  } // end of property IDiaSymbol2::function
  .property int32 'managed'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 47 00 00 00 00 00 )                         // ..G.....
    .get instance int32 DIALib.IDiaSymbol2::get_managed()
  } // end of property IDiaSymbol2::'managed'
  .property int32 msil()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 48 00 00 00 00 00 )                         // ..H.....
    .get instance int32 DIALib.IDiaSymbol2::get_msil()
  } // end of property IDiaSymbol2::msil
  .property uint32 virtualBaseDispIndex()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 49 00 00 00 00 00 )                         // ..I.....
    .get instance uint32 DIALib.IDiaSymbol2::get_virtualBaseDispIndex()
  } // end of property IDiaSymbol2::virtualBaseDispIndex
  .property string undecoratedName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4A 00 00 00 00 00 )                         // ..J.....
    .get instance string DIALib.IDiaSymbol2::get_undecoratedName()
  } // end of property IDiaSymbol2::undecoratedName
  .property uint32 age()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4B 00 00 00 00 00 )                         // ..K.....
    .get instance uint32 DIALib.IDiaSymbol2::get_age()
  } // end of property IDiaSymbol2::age
  .property uint32 signature()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4C 00 00 00 00 00 )                         // ..L.....
    .get instance uint32 DIALib.IDiaSymbol2::get_signature()
  } // end of property IDiaSymbol2::signature
  .property int32 compilerGenerated()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4D 00 00 00 00 00 )                         // ..M.....
    .get instance int32 DIALib.IDiaSymbol2::get_compilerGenerated()
  } // end of property IDiaSymbol2::compilerGenerated
  .property int32 addressTaken()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4E 00 00 00 00 00 )                         // ..N.....
    .get instance int32 DIALib.IDiaSymbol2::get_addressTaken()
  } // end of property IDiaSymbol2::addressTaken
  .property uint32 rank()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4F 00 00 00 00 00 )                         // ..O.....
    .get instance uint32 DIALib.IDiaSymbol2::get_rank()
  } // end of property IDiaSymbol2::rank
  .property class DIALib.IDiaSymbol lowerBound()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 50 00 00 00 00 00 )                         // ..P.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol2::get_lowerBound()
  } // end of property IDiaSymbol2::lowerBound
  .property class DIALib.IDiaSymbol upperBound()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 51 00 00 00 00 00 )                         // ..Q.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol2::get_upperBound()
  } // end of property IDiaSymbol2::upperBound
  .property uint32 lowerBoundId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 52 00 00 00 00 00 )                         // ..R.....
    .get instance uint32 DIALib.IDiaSymbol2::get_lowerBoundId()
  } // end of property IDiaSymbol2::lowerBoundId
  .property uint32 upperBoundId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 53 00 00 00 00 00 )                         // ..S.....
    .get instance uint32 DIALib.IDiaSymbol2::get_upperBoundId()
  } // end of property IDiaSymbol2::upperBoundId
  .property uint32 targetSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 54 00 00 00 00 00 )                         // ..T.....
    .get instance uint32 DIALib.IDiaSymbol2::get_targetSection()
  } // end of property IDiaSymbol2::targetSection
  .property uint32 targetOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 55 00 00 00 00 00 )                         // ..U.....
    .get instance uint32 DIALib.IDiaSymbol2::get_targetOffset()
  } // end of property IDiaSymbol2::targetOffset
  .property uint32 targetRelativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 56 00 00 00 00 00 )                         // ..V.....
    .get instance uint32 DIALib.IDiaSymbol2::get_targetRelativeVirtualAddress()
  } // end of property IDiaSymbol2::targetRelativeVirtualAddress
  .property uint64 targetVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 57 00 00 00 00 00 )                         // ..W.....
    .get instance uint64 DIALib.IDiaSymbol2::get_targetVirtualAddress()
  } // end of property IDiaSymbol2::targetVirtualAddress
  .property uint32 machineType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 58 00 00 00 00 00 )                         // ..X.....
    .get instance uint32 DIALib.IDiaSymbol2::get_machineType()
  } // end of property IDiaSymbol2::machineType
  .property uint32 oemId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 59 00 00 00 00 00 )                         // ..Y.....
    .get instance uint32 DIALib.IDiaSymbol2::get_oemId()
  } // end of property IDiaSymbol2::oemId
  .property uint32 oemSymbolId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5A 00 00 00 00 00 )                         // ..Z.....
    .get instance uint32 DIALib.IDiaSymbol2::get_oemSymbolId()
  } // end of property IDiaSymbol2::oemSymbolId
  .property class DIALib.IDiaSymbol objectPointerType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5B 00 00 00 00 00 )                         // ..[.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol2::get_objectPointerType()
  } // end of property IDiaSymbol2::objectPointerType
  .property uint32 udtKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5C 00 00 00 00 00 )                         // ..\.....
    .get instance uint32 DIALib.IDiaSymbol2::get_udtKind()
  } // end of property IDiaSymbol2::udtKind
  .property int32 noReturn()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5D 00 00 00 00 00 )                         // ..].....
    .get instance int32 DIALib.IDiaSymbol2::get_noReturn()
  } // end of property IDiaSymbol2::noReturn
  .property int32 customCallingConvention()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5E 00 00 00 00 00 )                         // ..^.....
    .get instance int32 DIALib.IDiaSymbol2::get_customCallingConvention()
  } // end of property IDiaSymbol2::customCallingConvention
  .property int32 noInline()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5F 00 00 00 00 00 )                         // .._.....
    .get instance int32 DIALib.IDiaSymbol2::get_noInline()
  } // end of property IDiaSymbol2::noInline
  .property int32 optimizedCodeDebugInfo()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 60 00 00 00 00 00 )                         // ..`.....
    .get instance int32 DIALib.IDiaSymbol2::get_optimizedCodeDebugInfo()
  } // end of property IDiaSymbol2::optimizedCodeDebugInfo
  .property int32 notReached()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 61 00 00 00 00 00 )                         // ..a.....
    .get instance int32 DIALib.IDiaSymbol2::get_notReached()
  } // end of property IDiaSymbol2::notReached
  .property int32 interruptReturn()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 62 00 00 00 00 00 )                         // ..b.....
    .get instance int32 DIALib.IDiaSymbol2::get_interruptReturn()
  } // end of property IDiaSymbol2::interruptReturn
  .property int32 farReturn()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 63 00 00 00 00 00 )                         // ..c.....
    .get instance int32 DIALib.IDiaSymbol2::get_farReturn()
  } // end of property IDiaSymbol2::farReturn
  .property int32 isStatic()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 64 00 00 00 00 00 )                         // ..d.....
    .get instance int32 DIALib.IDiaSymbol2::get_isStatic()
  } // end of property IDiaSymbol2::isStatic
  .property int32 hasDebugInfo()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 65 00 00 00 00 00 )                         // ..e.....
    .get instance int32 DIALib.IDiaSymbol2::get_hasDebugInfo()
  } // end of property IDiaSymbol2::hasDebugInfo
  .property int32 isLTCG()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 66 00 00 00 00 00 )                         // ..f.....
    .get instance int32 DIALib.IDiaSymbol2::get_isLTCG()
  } // end of property IDiaSymbol2::isLTCG
  .property int32 isDataAligned()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 67 00 00 00 00 00 )                         // ..g.....
    .get instance int32 DIALib.IDiaSymbol2::get_isDataAligned()
  } // end of property IDiaSymbol2::isDataAligned
  .property int32 hasSecurityChecks()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 68 00 00 00 00 00 )                         // ..h.....
    .get instance int32 DIALib.IDiaSymbol2::get_hasSecurityChecks()
  } // end of property IDiaSymbol2::hasSecurityChecks
  .property string compilerName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 69 00 00 00 00 00 )                         // ..i.....
    .get instance string DIALib.IDiaSymbol2::get_compilerName()
  } // end of property IDiaSymbol2::compilerName
  .property int32 hasAlloca()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6A 00 00 00 00 00 )                         // ..j.....
    .get instance int32 DIALib.IDiaSymbol2::get_hasAlloca()
  } // end of property IDiaSymbol2::hasAlloca
  .property int32 hasSetJump()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6B 00 00 00 00 00 )                         // ..k.....
    .get instance int32 DIALib.IDiaSymbol2::get_hasSetJump()
  } // end of property IDiaSymbol2::hasSetJump
  .property int32 hasLongJump()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6C 00 00 00 00 00 )                         // ..l.....
    .get instance int32 DIALib.IDiaSymbol2::get_hasLongJump()
  } // end of property IDiaSymbol2::hasLongJump
  .property int32 hasInlAsm()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6D 00 00 00 00 00 )                         // ..m.....
    .get instance int32 DIALib.IDiaSymbol2::get_hasInlAsm()
  } // end of property IDiaSymbol2::hasInlAsm
  .property int32 hasEH()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6E 00 00 00 00 00 )                         // ..n.....
    .get instance int32 DIALib.IDiaSymbol2::get_hasEH()
  } // end of property IDiaSymbol2::hasEH
  .property int32 hasSEH()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6F 00 00 00 00 00 )                         // ..o.....
    .get instance int32 DIALib.IDiaSymbol2::get_hasSEH()
  } // end of property IDiaSymbol2::hasSEH
  .property int32 hasEHa()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 70 00 00 00 00 00 )                         // ..p.....
    .get instance int32 DIALib.IDiaSymbol2::get_hasEHa()
  } // end of property IDiaSymbol2::hasEHa
  .property int32 isNaked()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 71 00 00 00 00 00 )                         // ..q.....
    .get instance int32 DIALib.IDiaSymbol2::get_isNaked()
  } // end of property IDiaSymbol2::isNaked
  .property int32 isAggregated()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 72 00 00 00 00 00 )                         // ..r.....
    .get instance int32 DIALib.IDiaSymbol2::get_isAggregated()
  } // end of property IDiaSymbol2::isAggregated
  .property int32 isSplitted()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 73 00 00 00 00 00 )                         // ..s.....
    .get instance int32 DIALib.IDiaSymbol2::get_isSplitted()
  } // end of property IDiaSymbol2::isSplitted
  .property class DIALib.IDiaSymbol container()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 74 00 00 00 00 00 )                         // ..t.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol2::get_container()
  } // end of property IDiaSymbol2::container
  .property int32 inlSpec()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 75 00 00 00 00 00 )                         // ..u.....
    .get instance int32 DIALib.IDiaSymbol2::get_inlSpec()
  } // end of property IDiaSymbol2::inlSpec
  .property int32 noStackOrdering()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 76 00 00 00 00 00 )                         // ..v.....
    .get instance int32 DIALib.IDiaSymbol2::get_noStackOrdering()
  } // end of property IDiaSymbol2::noStackOrdering
  .property class DIALib.IDiaSymbol virtualBaseTableType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 77 00 00 00 00 00 )                         // ..w.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol2::get_virtualBaseTableType()
  } // end of property IDiaSymbol2::virtualBaseTableType
  .property int32 hasManagedCode()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 78 00 00 00 00 00 )                         // ..x.....
    .get instance int32 DIALib.IDiaSymbol2::get_hasManagedCode()
  } // end of property IDiaSymbol2::hasManagedCode
  .property int32 isHotpatchable()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 79 00 00 00 00 00 )                         // ..y.....
    .get instance int32 DIALib.IDiaSymbol2::get_isHotpatchable()
  } // end of property IDiaSymbol2::isHotpatchable
  .property int32 isCVTCIL()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7A 00 00 00 00 00 )                         // ..z.....
    .get instance int32 DIALib.IDiaSymbol2::get_isCVTCIL()
  } // end of property IDiaSymbol2::isCVTCIL
  .property int32 isMSILNetmodule()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7B 00 00 00 00 00 )                         // ..{.....
    .get instance int32 DIALib.IDiaSymbol2::get_isMSILNetmodule()
  } // end of property IDiaSymbol2::isMSILNetmodule
  .property int32 isCTypes()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7C 00 00 00 00 00 )                         // ..|.....
    .get instance int32 DIALib.IDiaSymbol2::get_isCTypes()
  } // end of property IDiaSymbol2::isCTypes
  .property int32 isStripped()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7D 00 00 00 00 00 )                         // ..}.....
    .get instance int32 DIALib.IDiaSymbol2::get_isStripped()
  } // end of property IDiaSymbol2::isStripped
  .property uint32 frontEndQFE()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7E 00 00 00 00 00 )                         // ..~.....
    .get instance uint32 DIALib.IDiaSymbol2::get_frontEndQFE()
  } // end of property IDiaSymbol2::frontEndQFE
  .property uint32 backEndQFE()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_backEndQFE()
  } // end of property IDiaSymbol2::backEndQFE
  .property int32 wasInlined()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 80 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_wasInlined()
  } // end of property IDiaSymbol2::wasInlined
  .property int32 strictGSCheck()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 81 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_strictGSCheck()
  } // end of property IDiaSymbol2::strictGSCheck
  .property int32 isCxxReturnUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 82 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isCxxReturnUdt()
  } // end of property IDiaSymbol2::isCxxReturnUdt
  .property int32 isConstructorVirtualBase()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 83 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isConstructorVirtualBase()
  } // end of property IDiaSymbol2::isConstructorVirtualBase
  .property int32 RValueReference()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 84 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_RValueReference()
  } // end of property IDiaSymbol2::RValueReference
  .property class DIALib.IDiaSymbol unmodifiedType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 85 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol2::get_unmodifiedType()
  } // end of property IDiaSymbol2::unmodifiedType
  .property int32 framePointerPresent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 86 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_framePointerPresent()
  } // end of property IDiaSymbol2::framePointerPresent
  .property int32 isSafeBuffers()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 87 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isSafeBuffers()
  } // end of property IDiaSymbol2::isSafeBuffers
  .property int32 intrinsic()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 88 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_intrinsic()
  } // end of property IDiaSymbol2::intrinsic
  .property int32 'sealed'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 89 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_sealed()
  } // end of property IDiaSymbol2::'sealed'
  .property int32 hfaFloat()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8A 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_hfaFloat()
  } // end of property IDiaSymbol2::hfaFloat
  .property int32 hfaDouble()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8B 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_hfaDouble()
  } // end of property IDiaSymbol2::hfaDouble
  .property uint32 liveRangeStartAddressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_liveRangeStartAddressSection()
  } // end of property IDiaSymbol2::liveRangeStartAddressSection
  .property uint32 liveRangeStartAddressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8D 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_liveRangeStartAddressOffset()
  } // end of property IDiaSymbol2::liveRangeStartAddressOffset
  .property uint32 liveRangeStartRelativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8E 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_liveRangeStartRelativeVirtualAddress()
  } // end of property IDiaSymbol2::liveRangeStartRelativeVirtualAddress
  .property uint32 countLiveRanges()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_countLiveRanges()
  } // end of property IDiaSymbol2::countLiveRanges
  .property uint64 liveRangeLength()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 90 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol2::get_liveRangeLength()
  } // end of property IDiaSymbol2::liveRangeLength
  .property uint32 offsetInUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 91 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_offsetInUdt()
  } // end of property IDiaSymbol2::offsetInUdt
  .property uint32 paramBasePointerRegisterId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 92 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_paramBasePointerRegisterId()
  } // end of property IDiaSymbol2::paramBasePointerRegisterId
  .property uint32 localBasePointerRegisterId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 93 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_localBasePointerRegisterId()
  } // end of property IDiaSymbol2::localBasePointerRegisterId
  .property int32 isLocationControlFlowDependent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 94 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isLocationControlFlowDependent()
  } // end of property IDiaSymbol2::isLocationControlFlowDependent
  .property uint32 stride()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 95 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_stride()
  } // end of property IDiaSymbol2::stride
  .property uint32 numberOfRows()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 96 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_numberOfRows()
  } // end of property IDiaSymbol2::numberOfRows
  .property uint32 numberOfColumns()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 97 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_numberOfColumns()
  } // end of property IDiaSymbol2::numberOfColumns
  .property int32 isMatrixRowMajor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 98 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isMatrixRowMajor()
  } // end of property IDiaSymbol2::isMatrixRowMajor
  .property int32 isReturnValue()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 99 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isReturnValue()
  } // end of property IDiaSymbol2::isReturnValue
  .property int32 isOptimizedAway()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9A 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isOptimizedAway()
  } // end of property IDiaSymbol2::isOptimizedAway
  .property uint32 builtInKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9B 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_builtInKind()
  } // end of property IDiaSymbol2::builtInKind
  .property uint32 registerType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_registerType()
  } // end of property IDiaSymbol2::registerType
  .property uint32 baseDataSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9D 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_baseDataSlot()
  } // end of property IDiaSymbol2::baseDataSlot
  .property uint32 baseDataOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9E 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_baseDataOffset()
  } // end of property IDiaSymbol2::baseDataOffset
  .property uint32 textureSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_textureSlot()
  } // end of property IDiaSymbol2::textureSlot
  .property uint32 samplerSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A0 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_samplerSlot()
  } // end of property IDiaSymbol2::samplerSlot
  .property uint32 uavSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A1 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_uavSlot()
  } // end of property IDiaSymbol2::uavSlot
  .property uint32 sizeInUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A2 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_sizeInUdt()
  } // end of property IDiaSymbol2::sizeInUdt
  .property uint32 memorySpaceKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A3 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_memorySpaceKind()
  } // end of property IDiaSymbol2::memorySpaceKind
  .property uint32 unmodifiedTypeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A4 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_unmodifiedTypeId()
  } // end of property IDiaSymbol2::unmodifiedTypeId
  .property uint32 subTypeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A5 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_subTypeId()
  } // end of property IDiaSymbol2::subTypeId
  .property class DIALib.IDiaSymbol subType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A6 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol2::get_subType()
  } // end of property IDiaSymbol2::subType
  .property uint32 numberOfModifiers()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A7 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_numberOfModifiers()
  } // end of property IDiaSymbol2::numberOfModifiers
  .property uint32 numberOfRegisterIndices()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A8 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_numberOfRegisterIndices()
  } // end of property IDiaSymbol2::numberOfRegisterIndices
  .property int32 isHLSLData()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isHLSLData()
  } // end of property IDiaSymbol2::isHLSLData
  .property int32 isPointerToDataMember()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isPointerToDataMember()
  } // end of property IDiaSymbol2::isPointerToDataMember
  .property int32 isPointerToMemberFunction()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AB 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isPointerToMemberFunction()
  } // end of property IDiaSymbol2::isPointerToMemberFunction
  .property int32 isSingleInheritance()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AC 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isSingleInheritance()
  } // end of property IDiaSymbol2::isSingleInheritance
  .property int32 isMultipleInheritance()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AD 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isMultipleInheritance()
  } // end of property IDiaSymbol2::isMultipleInheritance
  .property int32 isVirtualInheritance()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AE 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isVirtualInheritance()
  } // end of property IDiaSymbol2::isVirtualInheritance
  .property int32 restrictedType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AF 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_restrictedType()
  } // end of property IDiaSymbol2::restrictedType
  .property int32 isPointerBasedOnSymbolValue()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B0 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isPointerBasedOnSymbolValue()
  } // end of property IDiaSymbol2::isPointerBasedOnSymbolValue
  .property class DIALib.IDiaSymbol baseSymbol()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B1 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol2::get_baseSymbol()
  } // end of property IDiaSymbol2::baseSymbol
  .property uint32 baseSymbolId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B2 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_baseSymbolId()
  } // end of property IDiaSymbol2::baseSymbolId
  .property string objectFileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B3 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol2::get_objectFileName()
  } // end of property IDiaSymbol2::objectFileName
  .property int32 isAcceleratorGroupSharedLocal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B4 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isAcceleratorGroupSharedLocal()
  } // end of property IDiaSymbol2::isAcceleratorGroupSharedLocal
  .property int32 isAcceleratorPointerTagLiveRange()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B5 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isAcceleratorPointerTagLiveRange()
  } // end of property IDiaSymbol2::isAcceleratorPointerTagLiveRange
  .property int32 isAcceleratorStubFunction()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B6 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isAcceleratorStubFunction()
  } // end of property IDiaSymbol2::isAcceleratorStubFunction
  .property uint32 numberOfAcceleratorPointerTags()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B7 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_numberOfAcceleratorPointerTags()
  } // end of property IDiaSymbol2::numberOfAcceleratorPointerTags
  .property int32 isSdl()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B8 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isSdl()
  } // end of property IDiaSymbol2::isSdl
  .property int32 isWinRTPointer()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isWinRTPointer()
  } // end of property IDiaSymbol2::isWinRTPointer
  .property int32 isRefUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isRefUdt()
  } // end of property IDiaSymbol2::isRefUdt
  .property int32 isValueUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BB 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isValueUdt()
  } // end of property IDiaSymbol2::isValueUdt
  .property int32 isInterfaceUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BC 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isInterfaceUdt()
  } // end of property IDiaSymbol2::isInterfaceUdt
  .property int32 isPGO()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BD 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isPGO()
  } // end of property IDiaSymbol2::isPGO
  .property int32 hasValidPGOCounts()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BE 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_hasValidPGOCounts()
  } // end of property IDiaSymbol2::hasValidPGOCounts
  .property int32 isOptimizedForSpeed()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BF 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isOptimizedForSpeed()
  } // end of property IDiaSymbol2::isOptimizedForSpeed
  .property uint32 PGOEntryCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C0 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_PGOEntryCount()
  } // end of property IDiaSymbol2::PGOEntryCount
  .property uint32 PGOEdgeCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C1 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_PGOEdgeCount()
  } // end of property IDiaSymbol2::PGOEdgeCount
  .property uint64 PGODynamicInstructionCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C2 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol2::get_PGODynamicInstructionCount()
  } // end of property IDiaSymbol2::PGODynamicInstructionCount
  .property uint32 staticSize()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C3 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_staticSize()
  } // end of property IDiaSymbol2::staticSize
  .property uint32 finalLiveStaticSize()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C4 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_finalLiveStaticSize()
  } // end of property IDiaSymbol2::finalLiveStaticSize
  .property string phaseName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C5 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol2::get_phaseName()
  } // end of property IDiaSymbol2::phaseName
  .property int32 hasControlFlowCheck()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C6 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_hasControlFlowCheck()
  } // end of property IDiaSymbol2::hasControlFlowCheck
  .property int32 constantExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C7 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_constantExport()
  } // end of property IDiaSymbol2::constantExport
  .property int32 dataExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C8 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_dataExport()
  } // end of property IDiaSymbol2::dataExport
  .property int32 privateExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_privateExport()
  } // end of property IDiaSymbol2::privateExport
  .property int32 noNameExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_noNameExport()
  } // end of property IDiaSymbol2::noNameExport
  .property int32 exportHasExplicitlyAssignedOrdinal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CB 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_exportHasExplicitlyAssignedOrdinal()
  } // end of property IDiaSymbol2::exportHasExplicitlyAssignedOrdinal
  .property int32 exportIsForwarder()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CC 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_exportIsForwarder()
  } // end of property IDiaSymbol2::exportIsForwarder
  .property uint32 ordinal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CD 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_ordinal()
  } // end of property IDiaSymbol2::ordinal
  .property uint32 frameSize()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CE 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_frameSize()
  } // end of property IDiaSymbol2::frameSize
  .property uint32 exceptionHandlerAddressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CF 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_exceptionHandlerAddressSection()
  } // end of property IDiaSymbol2::exceptionHandlerAddressSection
  .property uint32 exceptionHandlerAddressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D0 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_exceptionHandlerAddressOffset()
  } // end of property IDiaSymbol2::exceptionHandlerAddressOffset
  .property uint32 exceptionHandlerRelativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D1 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_exceptionHandlerRelativeVirtualAddress()
  } // end of property IDiaSymbol2::exceptionHandlerRelativeVirtualAddress
  .property uint64 exceptionHandlerVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D2 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol2::get_exceptionHandlerVirtualAddress()
  } // end of property IDiaSymbol2::exceptionHandlerVirtualAddress
  .property uint32 characteristics()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D3 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_characteristics()
  } // end of property IDiaSymbol2::characteristics
  .property class DIALib.IDiaSymbol coffGroup()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D4 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol2::get_coffGroup()
  } // end of property IDiaSymbol2::coffGroup
  .property uint32 bindID()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D5 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_bindID()
  } // end of property IDiaSymbol2::bindID
  .property uint32 bindSpace()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D6 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_bindSpace()
  } // end of property IDiaSymbol2::bindSpace
  .property uint32 bindSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D7 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol2::get_bindSlot()
  } // end of property IDiaSymbol2::bindSlot
  .property int32 isObjCClass()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D8 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isObjCClass()
  } // end of property IDiaSymbol2::isObjCClass
  .property int32 isObjCCategory()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isObjCCategory()
  } // end of property IDiaSymbol2::isObjCCategory
  .property int32 isObjCProtocol()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 DA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol2::get_isObjCProtocol()
  } // end of property IDiaSymbol2::isObjCProtocol
} // end of class DIALib.IDiaSymbol2

.class interface public abstract auto ansi import DIALib.IDiaSymbol3
       implements DIALib.IDiaSymbol2
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 39 39 42 36 36 35 46 37 2D 43 31 42 32   // ..$99B665F7-C1B2
                                                                                                  2D 34 39 44 33 2D 38 39 42 32 2D 41 33 38 34 33   // -49D3-89B2-A3843
                                                                                                  36 31 41 43 41 42 35 00 00 )                      // 61ACAB5..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 0A 73 79 6D 49 6E 64 65 78 49 64 00 00 )    // ...symIndexId..
  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_symIndexId() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_symIndexId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_symTag() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_symTag

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_name() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_name

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_lexicalParent() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_lexicalParent

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_classParent() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_classParent

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_type() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_type

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_dataKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_dataKind

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_locationType() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_locationType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_addressSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_addressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_addressOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_addressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_relativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_relativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_virtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_virtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_registerId() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_registerId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_offset() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_offset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_length() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_length

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_slot() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_slot

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_volatileType() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_volatileType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_constType() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_constType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_unalignedType() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_unalignedType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_access() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_access

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_libraryName() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_libraryName

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_platform() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_platform

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_language() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_language

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_editAndContinueEnabled() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_editAndContinueEnabled

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndMajor() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_frontEndMajor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndMinor() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_frontEndMinor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndBuild() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_frontEndBuild

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndMajor() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_backEndMajor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndMinor() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_backEndMinor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndBuild() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_backEndBuild

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_sourceFileName() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_sourceFileName

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_unused() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_unused

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_thunkOrdinal() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_thunkOrdinal

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_thisAdjust() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_thisAdjust

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_virtualBaseOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_virtualBaseOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_virtual() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_virtual

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_intro() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_intro

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_pure() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_pure

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_callingConvention() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_callingConvention

  .method public hidebysig newslot specialname abstract virtual 
          instance object 
          marshal( struct) 
          get_value() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_value

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseType() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_baseType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_token() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_token

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_timeStamp() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_timeStamp

  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype [mscorlib]System.Guid 
          get_guid() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_guid

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_symbolsFileName() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_symbolsFileName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_reference() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_reference

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_count() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_count

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bitPosition() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_bitPosition

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_arrayIndexType() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_arrayIndexType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_packed() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_packed

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_constructor() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_constructor

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_overloadedOperator() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_overloadedOperator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_nested() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_nested

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasNestedTypes() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_hasNestedTypes

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasAssignmentOperator() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_hasAssignmentOperator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasCastOperator() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_hasCastOperator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_scoped() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_scoped

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_virtualBaseClass() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_virtualBaseClass

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_indirectVirtualBaseClass() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_indirectVirtualBaseClass

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_virtualBasePointerOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_virtualBasePointerOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_virtualTableShape() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_virtualTableShape

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lexicalParentId() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_lexicalParentId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_classParentId() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_classParentId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_typeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_typeId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_arrayIndexTypeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_arrayIndexTypeId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_virtualTableShapeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_virtualTableShapeId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_code() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_code

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_function() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_function

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_managed() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_managed

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_msil() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_msil

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_virtualBaseDispIndex() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_virtualBaseDispIndex

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_undecoratedName() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_undecoratedName

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_age() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_age

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_signature() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_signature

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_compilerGenerated() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_compilerGenerated

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_addressTaken() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_addressTaken

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_rank() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_rank

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_lowerBound() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_lowerBound

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_upperBound() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_upperBound

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lowerBoundId() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_lowerBoundId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_upperBoundId() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_upperBoundId

  .method public hidebysig newslot abstract virtual 
          instance void  get_dataBytes([in] uint32 cbData,
                                       [out] uint32& pcbData,
                                       [out] uint8& pbData) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_dataBytes

  .method public hidebysig newslot abstract virtual 
          instance void  findChildren([in] valuetype DIALib.SymTagEnum symTag,
                                      [in] string  marshal( lpwstr) name,
                                      [in] uint32 compareFlags,
                                      [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::findChildren

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenEx([in] valuetype DIALib.SymTagEnum symTag,
                                        [in] string  marshal( lpwstr) name,
                                        [in] uint32 compareFlags,
                                        [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::findChildrenEx

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByAddr([in] valuetype DIALib.SymTagEnum symTag,
                                              [in] string  marshal( lpwstr) name,
                                              [in] uint32 compareFlags,
                                              [in] uint32 isect,
                                              [in] uint32 offset,
                                              [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::findChildrenExByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByVA([in] valuetype DIALib.SymTagEnum symTag,
                                            [in] string  marshal( lpwstr) name,
                                            [in] uint32 compareFlags,
                                            [in] uint64 va,
                                            [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::findChildrenExByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByRVA([in] valuetype DIALib.SymTagEnum symTag,
                                             [in] string  marshal( lpwstr) name,
                                             [in] uint32 compareFlags,
                                             [in] uint32 rva,
                                             [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::findChildrenExByRVA

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_targetSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_targetSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_targetOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_targetOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_targetRelativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_targetRelativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_targetVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_targetVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_machineType() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_machineType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_oemId() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_oemId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_oemSymbolId() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_oemSymbolId

  .method public hidebysig newslot abstract virtual 
          instance void  get_types([in] uint32 cTypes,
                                   [out] uint32& pcTypes,
                                   [out] class DIALib.IDiaSymbol&  marshal( interface ) pTypes) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_types

  .method public hidebysig newslot abstract virtual 
          instance void  get_typeIds([in] uint32 cTypeIds,
                                     [out] uint32& pcTypeIds,
                                     [out] uint32& pdwTypeIds) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_typeIds

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_objectPointerType() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_objectPointerType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_udtKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_udtKind

  .method public hidebysig newslot abstract virtual 
          instance void  get_undecoratedNameEx([in] uint32 undecorateOptions,
                                               [out] string&  marshal( bstr) name) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_undecoratedNameEx

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noReturn() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_noReturn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_customCallingConvention() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_customCallingConvention

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noInline() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_noInline

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_optimizedCodeDebugInfo() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_optimizedCodeDebugInfo

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_notReached() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_notReached

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_interruptReturn() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_interruptReturn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_farReturn() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_farReturn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isStatic() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isStatic

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasDebugInfo() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_hasDebugInfo

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isLTCG() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isLTCG

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isDataAligned() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isDataAligned

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasSecurityChecks() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_hasSecurityChecks

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_compilerName() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_compilerName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasAlloca() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_hasAlloca

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasSetJump() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_hasSetJump

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasLongJump() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_hasLongJump

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasInlAsm() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_hasInlAsm

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasEH() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_hasEH

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasSEH() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_hasSEH

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasEHa() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_hasEHa

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isNaked() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isNaked

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAggregated() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isAggregated

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSplitted() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isSplitted

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_container() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_container

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_inlSpec() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_inlSpec

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noStackOrdering() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_noStackOrdering

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_virtualBaseTableType() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_virtualBaseTableType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasManagedCode() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_hasManagedCode

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isHotpatchable() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isHotpatchable

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isCVTCIL() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isCVTCIL

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isMSILNetmodule() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isMSILNetmodule

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isCTypes() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isCTypes

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isStripped() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isStripped

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndQFE() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_frontEndQFE

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndQFE() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_backEndQFE

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_wasInlined() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_wasInlined

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_strictGSCheck() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_strictGSCheck

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isCxxReturnUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isCxxReturnUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isConstructorVirtualBase() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isConstructorVirtualBase

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_RValueReference() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_RValueReference

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_unmodifiedType() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_unmodifiedType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_framePointerPresent() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_framePointerPresent

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSafeBuffers() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isSafeBuffers

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_intrinsic() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_intrinsic

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_sealed() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_sealed

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hfaFloat() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_hfaFloat

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hfaDouble() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_hfaDouble

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_liveRangeStartAddressSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_liveRangeStartAddressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_liveRangeStartAddressOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_liveRangeStartAddressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_liveRangeStartRelativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_liveRangeStartRelativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_countLiveRanges() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_countLiveRanges

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_liveRangeLength() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_liveRangeLength

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_offsetInUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_offsetInUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_paramBasePointerRegisterId() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_paramBasePointerRegisterId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_localBasePointerRegisterId() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_localBasePointerRegisterId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isLocationControlFlowDependent() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isLocationControlFlowDependent

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_stride() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_stride

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfRows() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_numberOfRows

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfColumns() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_numberOfColumns

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isMatrixRowMajor() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isMatrixRowMajor

  .method public hidebysig newslot abstract virtual 
          instance void  get_numericProperties([in] uint32 cnt,
                                               [out] uint32& pcnt,
                                               [out] uint32& pProperties) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_numericProperties

  .method public hidebysig newslot abstract virtual 
          instance void  get_modifierValues([in] uint32 cnt,
                                            [out] uint32& pcnt,
                                            [out] uint16& pModifiers) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_modifierValues

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isReturnValue() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isReturnValue

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isOptimizedAway() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isOptimizedAway

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_builtInKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_builtInKind

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_registerType() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_registerType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseDataSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_baseDataSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseDataOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_baseDataOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_textureSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_textureSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_samplerSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_samplerSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_uavSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_uavSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_sizeInUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_sizeInUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_memorySpaceKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_memorySpaceKind

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_unmodifiedTypeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_unmodifiedTypeId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_subTypeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_subTypeId

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_subType() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_subType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfModifiers() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_numberOfModifiers

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfRegisterIndices() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_numberOfRegisterIndices

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isHLSLData() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isHLSLData

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPointerToDataMember() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isPointerToDataMember

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPointerToMemberFunction() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isPointerToMemberFunction

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSingleInheritance() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isSingleInheritance

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isMultipleInheritance() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isMultipleInheritance

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isVirtualInheritance() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isVirtualInheritance

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_restrictedType() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_restrictedType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPointerBasedOnSymbolValue() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isPointerBasedOnSymbolValue

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_baseSymbol() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_baseSymbol

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseSymbolId() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_baseSymbolId

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_objectFileName() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_objectFileName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAcceleratorGroupSharedLocal() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isAcceleratorGroupSharedLocal

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAcceleratorPointerTagLiveRange() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isAcceleratorPointerTagLiveRange

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAcceleratorStubFunction() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isAcceleratorStubFunction

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfAcceleratorPointerTags() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_numberOfAcceleratorPointerTags

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSdl() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isSdl

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isWinRTPointer() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isWinRTPointer

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isRefUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isRefUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isValueUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isValueUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isInterfaceUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isInterfaceUdt

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByAddr([in] uint32 isect,
                                                [in] uint32 offset,
                                                [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::findInlineFramesByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByRVA([in] uint32 rva,
                                               [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::findInlineFramesByRVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByVA([in] uint64 va,
                                              [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::findInlineFramesByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLines([out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::findInlineeLines

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByAddr([in] uint32 isect,
                                                [in] uint32 offset,
                                                [in] uint32 length,
                                                [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::findInlineeLinesByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByRVA([in] uint32 rva,
                                               [in] uint32 length,
                                               [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::findInlineeLinesByRVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByVA([in] uint64 va,
                                              [in] uint32 length,
                                              [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::findInlineeLinesByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findSymbolsForAcceleratorPointerTag([in] uint32 tagValue,
                                                             [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::findSymbolsForAcceleratorPointerTag

  .method public hidebysig newslot abstract virtual 
          instance void  findSymbolsByRVAForAcceleratorPointerTag([in] uint32 tagValue,
                                                                  [in] uint32 rva,
                                                                  [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::findSymbolsByRVAForAcceleratorPointerTag

  .method public hidebysig newslot abstract virtual 
          instance void  get_acceleratorPointerTags([in] uint32 cnt,
                                                    [out] uint32& pcnt,
                                                    [out] uint32& pPointerTags) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_acceleratorPointerTags

  .method public hidebysig newslot abstract virtual 
          instance void  getSrcLineOnTypeDefn([out] class DIALib.IDiaLineNumber&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::getSrcLineOnTypeDefn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPGO() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isPGO

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasValidPGOCounts() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_hasValidPGOCounts

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isOptimizedForSpeed() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isOptimizedForSpeed

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_PGOEntryCount() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_PGOEntryCount

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_PGOEdgeCount() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_PGOEdgeCount

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_PGODynamicInstructionCount() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_PGODynamicInstructionCount

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_staticSize() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_staticSize

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_finalLiveStaticSize() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_finalLiveStaticSize

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_phaseName() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_phaseName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasControlFlowCheck() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_hasControlFlowCheck

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_constantExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_constantExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_dataExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_dataExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_privateExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_privateExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noNameExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_noNameExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_exportHasExplicitlyAssignedOrdinal() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_exportHasExplicitlyAssignedOrdinal

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_exportIsForwarder() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_exportIsForwarder

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_ordinal() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_ordinal

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frameSize() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_frameSize

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_exceptionHandlerAddressSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_exceptionHandlerAddressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_exceptionHandlerAddressOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_exceptionHandlerAddressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_exceptionHandlerRelativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_exceptionHandlerRelativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_exceptionHandlerVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_exceptionHandlerVirtualAddress

  .method public hidebysig newslot abstract virtual 
          instance void  findInputAssemblyFile([out] class DIALib.IDiaInputAssemblyFile&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol3::findInputAssemblyFile

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_characteristics() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_characteristics

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_coffGroup() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_coffGroup

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bindID() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_bindID

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bindSpace() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_bindSpace

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bindSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_bindSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isObjCClass() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isObjCClass

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isObjCCategory() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isObjCCategory

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isObjCProtocol() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_isObjCProtocol

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_inlinee() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_inlinee

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_inlineeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol3::get_inlineeId

  .property uint32 symIndexId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_symIndexId()
  } // end of property IDiaSymbol3::symIndexId
  .property uint32 symTag()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_symTag()
  } // end of property IDiaSymbol3::symTag
  .property string name()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol3::get_name()
  } // end of property IDiaSymbol3::name
  .property class DIALib.IDiaSymbol lexicalParent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol3::get_lexicalParent()
  } // end of property IDiaSymbol3::lexicalParent
  .property class DIALib.IDiaSymbol classParent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 04 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol3::get_classParent()
  } // end of property IDiaSymbol3::classParent
  .property class DIALib.IDiaSymbol 'type'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 05 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol3::get_type()
  } // end of property IDiaSymbol3::'type'
  .property uint32 dataKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 06 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_dataKind()
  } // end of property IDiaSymbol3::dataKind
  .property uint32 locationType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 07 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_locationType()
  } // end of property IDiaSymbol3::locationType
  .property uint32 addressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_addressSection()
  } // end of property IDiaSymbol3::addressSection
  .property uint32 addressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 09 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_addressOffset()
  } // end of property IDiaSymbol3::addressOffset
  .property uint32 relativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0A 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_relativeVirtualAddress()
  } // end of property IDiaSymbol3::relativeVirtualAddress
  .property uint64 virtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0B 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol3::get_virtualAddress()
  } // end of property IDiaSymbol3::virtualAddress
  .property uint32 registerId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_registerId()
  } // end of property IDiaSymbol3::registerId
  .property int32 offset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0D 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_offset()
  } // end of property IDiaSymbol3::offset
  .property uint64 length()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0E 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol3::get_length()
  } // end of property IDiaSymbol3::length
  .property uint32 slot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_slot()
  } // end of property IDiaSymbol3::slot
  .property int32 volatileType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 10 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_volatileType()
  } // end of property IDiaSymbol3::volatileType
  .property int32 constType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 11 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_constType()
  } // end of property IDiaSymbol3::constType
  .property int32 unalignedType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 12 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_unalignedType()
  } // end of property IDiaSymbol3::unalignedType
  .property uint32 access()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 13 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_access()
  } // end of property IDiaSymbol3::access
  .property string libraryName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 14 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol3::get_libraryName()
  } // end of property IDiaSymbol3::libraryName
  .property uint32 platform()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 15 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_platform()
  } // end of property IDiaSymbol3::platform
  .property uint32 language()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 16 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_language()
  } // end of property IDiaSymbol3::language
  .property int32 editAndContinueEnabled()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 17 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_editAndContinueEnabled()
  } // end of property IDiaSymbol3::editAndContinueEnabled
  .property uint32 frontEndMajor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 18 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_frontEndMajor()
  } // end of property IDiaSymbol3::frontEndMajor
  .property uint32 frontEndMinor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 19 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_frontEndMinor()
  } // end of property IDiaSymbol3::frontEndMinor
  .property uint32 frontEndBuild()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1A 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_frontEndBuild()
  } // end of property IDiaSymbol3::frontEndBuild
  .property uint32 backEndMajor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1B 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_backEndMajor()
  } // end of property IDiaSymbol3::backEndMajor
  .property uint32 backEndMinor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_backEndMinor()
  } // end of property IDiaSymbol3::backEndMinor
  .property uint32 backEndBuild()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1D 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_backEndBuild()
  } // end of property IDiaSymbol3::backEndBuild
  .property string sourceFileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1E 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol3::get_sourceFileName()
  } // end of property IDiaSymbol3::sourceFileName
  .property string 'unused'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1F 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol3::get_unused()
  } // end of property IDiaSymbol3::'unused'
  .property uint32 thunkOrdinal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 20 00 00 00 00 00 )                         // .. .....
    .get instance uint32 DIALib.IDiaSymbol3::get_thunkOrdinal()
  } // end of property IDiaSymbol3::thunkOrdinal
  .property int32 thisAdjust()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 21 00 00 00 00 00 )                         // ..!.....
    .get instance int32 DIALib.IDiaSymbol3::get_thisAdjust()
  } // end of property IDiaSymbol3::thisAdjust
  .property uint32 virtualBaseOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 22 00 00 00 00 00 )                         // ..".....
    .get instance uint32 DIALib.IDiaSymbol3::get_virtualBaseOffset()
  } // end of property IDiaSymbol3::virtualBaseOffset
  .property int32 'virtual'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 23 00 00 00 00 00 )                         // ..#.....
    .get instance int32 DIALib.IDiaSymbol3::get_virtual()
  } // end of property IDiaSymbol3::'virtual'
  .property int32 intro()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 24 00 00 00 00 00 )                         // ..$.....
    .get instance int32 DIALib.IDiaSymbol3::get_intro()
  } // end of property IDiaSymbol3::intro
  .property int32 pure()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 25 00 00 00 00 00 )                         // ..%.....
    .get instance int32 DIALib.IDiaSymbol3::get_pure()
  } // end of property IDiaSymbol3::pure
  .property uint32 callingConvention()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 26 00 00 00 00 00 )                         // ..&.....
    .get instance uint32 DIALib.IDiaSymbol3::get_callingConvention()
  } // end of property IDiaSymbol3::callingConvention
  .property object 'value'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 27 00 00 00 00 00 )                         // ..'.....
    .get instance object DIALib.IDiaSymbol3::get_value()
  } // end of property IDiaSymbol3::'value'
  .property uint32 baseType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 28 00 00 00 00 00 )                         // ..(.....
    .get instance uint32 DIALib.IDiaSymbol3::get_baseType()
  } // end of property IDiaSymbol3::baseType
  .property uint32 token()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 29 00 00 00 00 00 )                         // ..).....
    .get instance uint32 DIALib.IDiaSymbol3::get_token()
  } // end of property IDiaSymbol3::token
  .property uint32 timeStamp()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2A 00 00 00 00 00 )                         // ..*.....
    .get instance uint32 DIALib.IDiaSymbol3::get_timeStamp()
  } // end of property IDiaSymbol3::timeStamp
  .property valuetype [mscorlib]System.Guid
          guid()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2B 00 00 00 00 00 )                         // ..+.....
    .get instance valuetype [mscorlib]System.Guid DIALib.IDiaSymbol3::get_guid()
  } // end of property IDiaSymbol3::guid
  .property string symbolsFileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2C 00 00 00 00 00 )                         // ..,.....
    .get instance string DIALib.IDiaSymbol3::get_symbolsFileName()
  } // end of property IDiaSymbol3::symbolsFileName
  .property int32 reference()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2E 00 00 00 00 00 )                         // ........
    .get instance int32 DIALib.IDiaSymbol3::get_reference()
  } // end of property IDiaSymbol3::reference
  .property uint32 count()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2F 00 00 00 00 00 )                         // ../.....
    .get instance uint32 DIALib.IDiaSymbol3::get_count()
  } // end of property IDiaSymbol3::count
  .property uint32 bitPosition()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 31 00 00 00 00 00 )                         // ..1.....
    .get instance uint32 DIALib.IDiaSymbol3::get_bitPosition()
  } // end of property IDiaSymbol3::bitPosition
  .property class DIALib.IDiaSymbol arrayIndexType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 32 00 00 00 00 00 )                         // ..2.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol3::get_arrayIndexType()
  } // end of property IDiaSymbol3::arrayIndexType
  .property int32 packed()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 33 00 00 00 00 00 )                         // ..3.....
    .get instance int32 DIALib.IDiaSymbol3::get_packed()
  } // end of property IDiaSymbol3::packed
  .property int32 constructor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 34 00 00 00 00 00 )                         // ..4.....
    .get instance int32 DIALib.IDiaSymbol3::get_constructor()
  } // end of property IDiaSymbol3::constructor
  .property int32 overloadedOperator()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 35 00 00 00 00 00 )                         // ..5.....
    .get instance int32 DIALib.IDiaSymbol3::get_overloadedOperator()
  } // end of property IDiaSymbol3::overloadedOperator
  .property int32 'nested'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 36 00 00 00 00 00 )                         // ..6.....
    .get instance int32 DIALib.IDiaSymbol3::get_nested()
  } // end of property IDiaSymbol3::'nested'
  .property int32 hasNestedTypes()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 37 00 00 00 00 00 )                         // ..7.....
    .get instance int32 DIALib.IDiaSymbol3::get_hasNestedTypes()
  } // end of property IDiaSymbol3::hasNestedTypes
  .property int32 hasAssignmentOperator()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 38 00 00 00 00 00 )                         // ..8.....
    .get instance int32 DIALib.IDiaSymbol3::get_hasAssignmentOperator()
  } // end of property IDiaSymbol3::hasAssignmentOperator
  .property int32 hasCastOperator()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 39 00 00 00 00 00 )                         // ..9.....
    .get instance int32 DIALib.IDiaSymbol3::get_hasCastOperator()
  } // end of property IDiaSymbol3::hasCastOperator
  .property int32 scoped()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3A 00 00 00 00 00 )                         // ..:.....
    .get instance int32 DIALib.IDiaSymbol3::get_scoped()
  } // end of property IDiaSymbol3::scoped
  .property int32 virtualBaseClass()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3B 00 00 00 00 00 )                         // ..;.....
    .get instance int32 DIALib.IDiaSymbol3::get_virtualBaseClass()
  } // end of property IDiaSymbol3::virtualBaseClass
  .property int32 indirectVirtualBaseClass()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3C 00 00 00 00 00 )                         // ..<.....
    .get instance int32 DIALib.IDiaSymbol3::get_indirectVirtualBaseClass()
  } // end of property IDiaSymbol3::indirectVirtualBaseClass
  .property int32 virtualBasePointerOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3D 00 00 00 00 00 )                         // ..=.....
    .get instance int32 DIALib.IDiaSymbol3::get_virtualBasePointerOffset()
  } // end of property IDiaSymbol3::virtualBasePointerOffset
  .property class DIALib.IDiaSymbol virtualTableShape()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3E 00 00 00 00 00 )                         // ..>.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol3::get_virtualTableShape()
  } // end of property IDiaSymbol3::virtualTableShape
  .property uint32 lexicalParentId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 40 00 00 00 00 00 )                         // ..@.....
    .get instance uint32 DIALib.IDiaSymbol3::get_lexicalParentId()
  } // end of property IDiaSymbol3::lexicalParentId
  .property uint32 classParentId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 41 00 00 00 00 00 )                         // ..A.....
    .get instance uint32 DIALib.IDiaSymbol3::get_classParentId()
  } // end of property IDiaSymbol3::classParentId
  .property uint32 typeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 42 00 00 00 00 00 )                         // ..B.....
    .get instance uint32 DIALib.IDiaSymbol3::get_typeId()
  } // end of property IDiaSymbol3::typeId
  .property uint32 arrayIndexTypeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 43 00 00 00 00 00 )                         // ..C.....
    .get instance uint32 DIALib.IDiaSymbol3::get_arrayIndexTypeId()
  } // end of property IDiaSymbol3::arrayIndexTypeId
  .property uint32 virtualTableShapeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 44 00 00 00 00 00 )                         // ..D.....
    .get instance uint32 DIALib.IDiaSymbol3::get_virtualTableShapeId()
  } // end of property IDiaSymbol3::virtualTableShapeId
  .property int32 code()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 45 00 00 00 00 00 )                         // ..E.....
    .get instance int32 DIALib.IDiaSymbol3::get_code()
  } // end of property IDiaSymbol3::code
  .property int32 function()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 46 00 00 00 00 00 )                         // ..F.....
    .get instance int32 DIALib.IDiaSymbol3::get_function()
  } // end of property IDiaSymbol3::function
  .property int32 'managed'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 47 00 00 00 00 00 )                         // ..G.....
    .get instance int32 DIALib.IDiaSymbol3::get_managed()
  } // end of property IDiaSymbol3::'managed'
  .property int32 msil()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 48 00 00 00 00 00 )                         // ..H.....
    .get instance int32 DIALib.IDiaSymbol3::get_msil()
  } // end of property IDiaSymbol3::msil
  .property uint32 virtualBaseDispIndex()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 49 00 00 00 00 00 )                         // ..I.....
    .get instance uint32 DIALib.IDiaSymbol3::get_virtualBaseDispIndex()
  } // end of property IDiaSymbol3::virtualBaseDispIndex
  .property string undecoratedName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4A 00 00 00 00 00 )                         // ..J.....
    .get instance string DIALib.IDiaSymbol3::get_undecoratedName()
  } // end of property IDiaSymbol3::undecoratedName
  .property uint32 age()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4B 00 00 00 00 00 )                         // ..K.....
    .get instance uint32 DIALib.IDiaSymbol3::get_age()
  } // end of property IDiaSymbol3::age
  .property uint32 signature()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4C 00 00 00 00 00 )                         // ..L.....
    .get instance uint32 DIALib.IDiaSymbol3::get_signature()
  } // end of property IDiaSymbol3::signature
  .property int32 compilerGenerated()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4D 00 00 00 00 00 )                         // ..M.....
    .get instance int32 DIALib.IDiaSymbol3::get_compilerGenerated()
  } // end of property IDiaSymbol3::compilerGenerated
  .property int32 addressTaken()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4E 00 00 00 00 00 )                         // ..N.....
    .get instance int32 DIALib.IDiaSymbol3::get_addressTaken()
  } // end of property IDiaSymbol3::addressTaken
  .property uint32 rank()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4F 00 00 00 00 00 )                         // ..O.....
    .get instance uint32 DIALib.IDiaSymbol3::get_rank()
  } // end of property IDiaSymbol3::rank
  .property class DIALib.IDiaSymbol lowerBound()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 50 00 00 00 00 00 )                         // ..P.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol3::get_lowerBound()
  } // end of property IDiaSymbol3::lowerBound
  .property class DIALib.IDiaSymbol upperBound()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 51 00 00 00 00 00 )                         // ..Q.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol3::get_upperBound()
  } // end of property IDiaSymbol3::upperBound
  .property uint32 lowerBoundId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 52 00 00 00 00 00 )                         // ..R.....
    .get instance uint32 DIALib.IDiaSymbol3::get_lowerBoundId()
  } // end of property IDiaSymbol3::lowerBoundId
  .property uint32 upperBoundId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 53 00 00 00 00 00 )                         // ..S.....
    .get instance uint32 DIALib.IDiaSymbol3::get_upperBoundId()
  } // end of property IDiaSymbol3::upperBoundId
  .property uint32 targetSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 54 00 00 00 00 00 )                         // ..T.....
    .get instance uint32 DIALib.IDiaSymbol3::get_targetSection()
  } // end of property IDiaSymbol3::targetSection
  .property uint32 targetOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 55 00 00 00 00 00 )                         // ..U.....
    .get instance uint32 DIALib.IDiaSymbol3::get_targetOffset()
  } // end of property IDiaSymbol3::targetOffset
  .property uint32 targetRelativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 56 00 00 00 00 00 )                         // ..V.....
    .get instance uint32 DIALib.IDiaSymbol3::get_targetRelativeVirtualAddress()
  } // end of property IDiaSymbol3::targetRelativeVirtualAddress
  .property uint64 targetVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 57 00 00 00 00 00 )                         // ..W.....
    .get instance uint64 DIALib.IDiaSymbol3::get_targetVirtualAddress()
  } // end of property IDiaSymbol3::targetVirtualAddress
  .property uint32 machineType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 58 00 00 00 00 00 )                         // ..X.....
    .get instance uint32 DIALib.IDiaSymbol3::get_machineType()
  } // end of property IDiaSymbol3::machineType
  .property uint32 oemId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 59 00 00 00 00 00 )                         // ..Y.....
    .get instance uint32 DIALib.IDiaSymbol3::get_oemId()
  } // end of property IDiaSymbol3::oemId
  .property uint32 oemSymbolId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5A 00 00 00 00 00 )                         // ..Z.....
    .get instance uint32 DIALib.IDiaSymbol3::get_oemSymbolId()
  } // end of property IDiaSymbol3::oemSymbolId
  .property class DIALib.IDiaSymbol objectPointerType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5B 00 00 00 00 00 )                         // ..[.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol3::get_objectPointerType()
  } // end of property IDiaSymbol3::objectPointerType
  .property uint32 udtKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5C 00 00 00 00 00 )                         // ..\.....
    .get instance uint32 DIALib.IDiaSymbol3::get_udtKind()
  } // end of property IDiaSymbol3::udtKind
  .property int32 noReturn()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5D 00 00 00 00 00 )                         // ..].....
    .get instance int32 DIALib.IDiaSymbol3::get_noReturn()
  } // end of property IDiaSymbol3::noReturn
  .property int32 customCallingConvention()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5E 00 00 00 00 00 )                         // ..^.....
    .get instance int32 DIALib.IDiaSymbol3::get_customCallingConvention()
  } // end of property IDiaSymbol3::customCallingConvention
  .property int32 noInline()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5F 00 00 00 00 00 )                         // .._.....
    .get instance int32 DIALib.IDiaSymbol3::get_noInline()
  } // end of property IDiaSymbol3::noInline
  .property int32 optimizedCodeDebugInfo()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 60 00 00 00 00 00 )                         // ..`.....
    .get instance int32 DIALib.IDiaSymbol3::get_optimizedCodeDebugInfo()
  } // end of property IDiaSymbol3::optimizedCodeDebugInfo
  .property int32 notReached()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 61 00 00 00 00 00 )                         // ..a.....
    .get instance int32 DIALib.IDiaSymbol3::get_notReached()
  } // end of property IDiaSymbol3::notReached
  .property int32 interruptReturn()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 62 00 00 00 00 00 )                         // ..b.....
    .get instance int32 DIALib.IDiaSymbol3::get_interruptReturn()
  } // end of property IDiaSymbol3::interruptReturn
  .property int32 farReturn()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 63 00 00 00 00 00 )                         // ..c.....
    .get instance int32 DIALib.IDiaSymbol3::get_farReturn()
  } // end of property IDiaSymbol3::farReturn
  .property int32 isStatic()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 64 00 00 00 00 00 )                         // ..d.....
    .get instance int32 DIALib.IDiaSymbol3::get_isStatic()
  } // end of property IDiaSymbol3::isStatic
  .property int32 hasDebugInfo()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 65 00 00 00 00 00 )                         // ..e.....
    .get instance int32 DIALib.IDiaSymbol3::get_hasDebugInfo()
  } // end of property IDiaSymbol3::hasDebugInfo
  .property int32 isLTCG()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 66 00 00 00 00 00 )                         // ..f.....
    .get instance int32 DIALib.IDiaSymbol3::get_isLTCG()
  } // end of property IDiaSymbol3::isLTCG
  .property int32 isDataAligned()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 67 00 00 00 00 00 )                         // ..g.....
    .get instance int32 DIALib.IDiaSymbol3::get_isDataAligned()
  } // end of property IDiaSymbol3::isDataAligned
  .property int32 hasSecurityChecks()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 68 00 00 00 00 00 )                         // ..h.....
    .get instance int32 DIALib.IDiaSymbol3::get_hasSecurityChecks()
  } // end of property IDiaSymbol3::hasSecurityChecks
  .property string compilerName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 69 00 00 00 00 00 )                         // ..i.....
    .get instance string DIALib.IDiaSymbol3::get_compilerName()
  } // end of property IDiaSymbol3::compilerName
  .property int32 hasAlloca()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6A 00 00 00 00 00 )                         // ..j.....
    .get instance int32 DIALib.IDiaSymbol3::get_hasAlloca()
  } // end of property IDiaSymbol3::hasAlloca
  .property int32 hasSetJump()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6B 00 00 00 00 00 )                         // ..k.....
    .get instance int32 DIALib.IDiaSymbol3::get_hasSetJump()
  } // end of property IDiaSymbol3::hasSetJump
  .property int32 hasLongJump()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6C 00 00 00 00 00 )                         // ..l.....
    .get instance int32 DIALib.IDiaSymbol3::get_hasLongJump()
  } // end of property IDiaSymbol3::hasLongJump
  .property int32 hasInlAsm()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6D 00 00 00 00 00 )                         // ..m.....
    .get instance int32 DIALib.IDiaSymbol3::get_hasInlAsm()
  } // end of property IDiaSymbol3::hasInlAsm
  .property int32 hasEH()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6E 00 00 00 00 00 )                         // ..n.....
    .get instance int32 DIALib.IDiaSymbol3::get_hasEH()
  } // end of property IDiaSymbol3::hasEH
  .property int32 hasSEH()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6F 00 00 00 00 00 )                         // ..o.....
    .get instance int32 DIALib.IDiaSymbol3::get_hasSEH()
  } // end of property IDiaSymbol3::hasSEH
  .property int32 hasEHa()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 70 00 00 00 00 00 )                         // ..p.....
    .get instance int32 DIALib.IDiaSymbol3::get_hasEHa()
  } // end of property IDiaSymbol3::hasEHa
  .property int32 isNaked()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 71 00 00 00 00 00 )                         // ..q.....
    .get instance int32 DIALib.IDiaSymbol3::get_isNaked()
  } // end of property IDiaSymbol3::isNaked
  .property int32 isAggregated()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 72 00 00 00 00 00 )                         // ..r.....
    .get instance int32 DIALib.IDiaSymbol3::get_isAggregated()
  } // end of property IDiaSymbol3::isAggregated
  .property int32 isSplitted()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 73 00 00 00 00 00 )                         // ..s.....
    .get instance int32 DIALib.IDiaSymbol3::get_isSplitted()
  } // end of property IDiaSymbol3::isSplitted
  .property class DIALib.IDiaSymbol container()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 74 00 00 00 00 00 )                         // ..t.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol3::get_container()
  } // end of property IDiaSymbol3::container
  .property int32 inlSpec()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 75 00 00 00 00 00 )                         // ..u.....
    .get instance int32 DIALib.IDiaSymbol3::get_inlSpec()
  } // end of property IDiaSymbol3::inlSpec
  .property int32 noStackOrdering()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 76 00 00 00 00 00 )                         // ..v.....
    .get instance int32 DIALib.IDiaSymbol3::get_noStackOrdering()
  } // end of property IDiaSymbol3::noStackOrdering
  .property class DIALib.IDiaSymbol virtualBaseTableType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 77 00 00 00 00 00 )                         // ..w.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol3::get_virtualBaseTableType()
  } // end of property IDiaSymbol3::virtualBaseTableType
  .property int32 hasManagedCode()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 78 00 00 00 00 00 )                         // ..x.....
    .get instance int32 DIALib.IDiaSymbol3::get_hasManagedCode()
  } // end of property IDiaSymbol3::hasManagedCode
  .property int32 isHotpatchable()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 79 00 00 00 00 00 )                         // ..y.....
    .get instance int32 DIALib.IDiaSymbol3::get_isHotpatchable()
  } // end of property IDiaSymbol3::isHotpatchable
  .property int32 isCVTCIL()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7A 00 00 00 00 00 )                         // ..z.....
    .get instance int32 DIALib.IDiaSymbol3::get_isCVTCIL()
  } // end of property IDiaSymbol3::isCVTCIL
  .property int32 isMSILNetmodule()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7B 00 00 00 00 00 )                         // ..{.....
    .get instance int32 DIALib.IDiaSymbol3::get_isMSILNetmodule()
  } // end of property IDiaSymbol3::isMSILNetmodule
  .property int32 isCTypes()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7C 00 00 00 00 00 )                         // ..|.....
    .get instance int32 DIALib.IDiaSymbol3::get_isCTypes()
  } // end of property IDiaSymbol3::isCTypes
  .property int32 isStripped()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7D 00 00 00 00 00 )                         // ..}.....
    .get instance int32 DIALib.IDiaSymbol3::get_isStripped()
  } // end of property IDiaSymbol3::isStripped
  .property uint32 frontEndQFE()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7E 00 00 00 00 00 )                         // ..~.....
    .get instance uint32 DIALib.IDiaSymbol3::get_frontEndQFE()
  } // end of property IDiaSymbol3::frontEndQFE
  .property uint32 backEndQFE()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_backEndQFE()
  } // end of property IDiaSymbol3::backEndQFE
  .property int32 wasInlined()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 80 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_wasInlined()
  } // end of property IDiaSymbol3::wasInlined
  .property int32 strictGSCheck()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 81 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_strictGSCheck()
  } // end of property IDiaSymbol3::strictGSCheck
  .property int32 isCxxReturnUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 82 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isCxxReturnUdt()
  } // end of property IDiaSymbol3::isCxxReturnUdt
  .property int32 isConstructorVirtualBase()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 83 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isConstructorVirtualBase()
  } // end of property IDiaSymbol3::isConstructorVirtualBase
  .property int32 RValueReference()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 84 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_RValueReference()
  } // end of property IDiaSymbol3::RValueReference
  .property class DIALib.IDiaSymbol unmodifiedType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 85 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol3::get_unmodifiedType()
  } // end of property IDiaSymbol3::unmodifiedType
  .property int32 framePointerPresent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 86 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_framePointerPresent()
  } // end of property IDiaSymbol3::framePointerPresent
  .property int32 isSafeBuffers()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 87 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isSafeBuffers()
  } // end of property IDiaSymbol3::isSafeBuffers
  .property int32 intrinsic()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 88 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_intrinsic()
  } // end of property IDiaSymbol3::intrinsic
  .property int32 'sealed'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 89 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_sealed()
  } // end of property IDiaSymbol3::'sealed'
  .property int32 hfaFloat()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8A 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_hfaFloat()
  } // end of property IDiaSymbol3::hfaFloat
  .property int32 hfaDouble()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8B 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_hfaDouble()
  } // end of property IDiaSymbol3::hfaDouble
  .property uint32 liveRangeStartAddressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_liveRangeStartAddressSection()
  } // end of property IDiaSymbol3::liveRangeStartAddressSection
  .property uint32 liveRangeStartAddressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8D 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_liveRangeStartAddressOffset()
  } // end of property IDiaSymbol3::liveRangeStartAddressOffset
  .property uint32 liveRangeStartRelativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8E 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_liveRangeStartRelativeVirtualAddress()
  } // end of property IDiaSymbol3::liveRangeStartRelativeVirtualAddress
  .property uint32 countLiveRanges()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_countLiveRanges()
  } // end of property IDiaSymbol3::countLiveRanges
  .property uint64 liveRangeLength()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 90 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol3::get_liveRangeLength()
  } // end of property IDiaSymbol3::liveRangeLength
  .property uint32 offsetInUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 91 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_offsetInUdt()
  } // end of property IDiaSymbol3::offsetInUdt
  .property uint32 paramBasePointerRegisterId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 92 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_paramBasePointerRegisterId()
  } // end of property IDiaSymbol3::paramBasePointerRegisterId
  .property uint32 localBasePointerRegisterId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 93 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_localBasePointerRegisterId()
  } // end of property IDiaSymbol3::localBasePointerRegisterId
  .property int32 isLocationControlFlowDependent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 94 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isLocationControlFlowDependent()
  } // end of property IDiaSymbol3::isLocationControlFlowDependent
  .property uint32 stride()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 95 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_stride()
  } // end of property IDiaSymbol3::stride
  .property uint32 numberOfRows()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 96 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_numberOfRows()
  } // end of property IDiaSymbol3::numberOfRows
  .property uint32 numberOfColumns()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 97 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_numberOfColumns()
  } // end of property IDiaSymbol3::numberOfColumns
  .property int32 isMatrixRowMajor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 98 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isMatrixRowMajor()
  } // end of property IDiaSymbol3::isMatrixRowMajor
  .property int32 isReturnValue()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 99 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isReturnValue()
  } // end of property IDiaSymbol3::isReturnValue
  .property int32 isOptimizedAway()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9A 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isOptimizedAway()
  } // end of property IDiaSymbol3::isOptimizedAway
  .property uint32 builtInKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9B 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_builtInKind()
  } // end of property IDiaSymbol3::builtInKind
  .property uint32 registerType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_registerType()
  } // end of property IDiaSymbol3::registerType
  .property uint32 baseDataSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9D 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_baseDataSlot()
  } // end of property IDiaSymbol3::baseDataSlot
  .property uint32 baseDataOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9E 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_baseDataOffset()
  } // end of property IDiaSymbol3::baseDataOffset
  .property uint32 textureSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_textureSlot()
  } // end of property IDiaSymbol3::textureSlot
  .property uint32 samplerSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A0 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_samplerSlot()
  } // end of property IDiaSymbol3::samplerSlot
  .property uint32 uavSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A1 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_uavSlot()
  } // end of property IDiaSymbol3::uavSlot
  .property uint32 sizeInUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A2 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_sizeInUdt()
  } // end of property IDiaSymbol3::sizeInUdt
  .property uint32 memorySpaceKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A3 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_memorySpaceKind()
  } // end of property IDiaSymbol3::memorySpaceKind
  .property uint32 unmodifiedTypeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A4 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_unmodifiedTypeId()
  } // end of property IDiaSymbol3::unmodifiedTypeId
  .property uint32 subTypeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A5 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_subTypeId()
  } // end of property IDiaSymbol3::subTypeId
  .property class DIALib.IDiaSymbol subType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A6 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol3::get_subType()
  } // end of property IDiaSymbol3::subType
  .property uint32 numberOfModifiers()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A7 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_numberOfModifiers()
  } // end of property IDiaSymbol3::numberOfModifiers
  .property uint32 numberOfRegisterIndices()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A8 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_numberOfRegisterIndices()
  } // end of property IDiaSymbol3::numberOfRegisterIndices
  .property int32 isHLSLData()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isHLSLData()
  } // end of property IDiaSymbol3::isHLSLData
  .property int32 isPointerToDataMember()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isPointerToDataMember()
  } // end of property IDiaSymbol3::isPointerToDataMember
  .property int32 isPointerToMemberFunction()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AB 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isPointerToMemberFunction()
  } // end of property IDiaSymbol3::isPointerToMemberFunction
  .property int32 isSingleInheritance()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AC 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isSingleInheritance()
  } // end of property IDiaSymbol3::isSingleInheritance
  .property int32 isMultipleInheritance()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AD 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isMultipleInheritance()
  } // end of property IDiaSymbol3::isMultipleInheritance
  .property int32 isVirtualInheritance()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AE 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isVirtualInheritance()
  } // end of property IDiaSymbol3::isVirtualInheritance
  .property int32 restrictedType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AF 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_restrictedType()
  } // end of property IDiaSymbol3::restrictedType
  .property int32 isPointerBasedOnSymbolValue()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B0 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isPointerBasedOnSymbolValue()
  } // end of property IDiaSymbol3::isPointerBasedOnSymbolValue
  .property class DIALib.IDiaSymbol baseSymbol()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B1 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol3::get_baseSymbol()
  } // end of property IDiaSymbol3::baseSymbol
  .property uint32 baseSymbolId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B2 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_baseSymbolId()
  } // end of property IDiaSymbol3::baseSymbolId
  .property string objectFileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B3 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol3::get_objectFileName()
  } // end of property IDiaSymbol3::objectFileName
  .property int32 isAcceleratorGroupSharedLocal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B4 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isAcceleratorGroupSharedLocal()
  } // end of property IDiaSymbol3::isAcceleratorGroupSharedLocal
  .property int32 isAcceleratorPointerTagLiveRange()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B5 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isAcceleratorPointerTagLiveRange()
  } // end of property IDiaSymbol3::isAcceleratorPointerTagLiveRange
  .property int32 isAcceleratorStubFunction()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B6 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isAcceleratorStubFunction()
  } // end of property IDiaSymbol3::isAcceleratorStubFunction
  .property uint32 numberOfAcceleratorPointerTags()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B7 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_numberOfAcceleratorPointerTags()
  } // end of property IDiaSymbol3::numberOfAcceleratorPointerTags
  .property int32 isSdl()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B8 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isSdl()
  } // end of property IDiaSymbol3::isSdl
  .property int32 isWinRTPointer()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isWinRTPointer()
  } // end of property IDiaSymbol3::isWinRTPointer
  .property int32 isRefUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isRefUdt()
  } // end of property IDiaSymbol3::isRefUdt
  .property int32 isValueUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BB 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isValueUdt()
  } // end of property IDiaSymbol3::isValueUdt
  .property int32 isInterfaceUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BC 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isInterfaceUdt()
  } // end of property IDiaSymbol3::isInterfaceUdt
  .property int32 isPGO()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BD 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isPGO()
  } // end of property IDiaSymbol3::isPGO
  .property int32 hasValidPGOCounts()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BE 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_hasValidPGOCounts()
  } // end of property IDiaSymbol3::hasValidPGOCounts
  .property int32 isOptimizedForSpeed()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BF 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isOptimizedForSpeed()
  } // end of property IDiaSymbol3::isOptimizedForSpeed
  .property uint32 PGOEntryCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C0 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_PGOEntryCount()
  } // end of property IDiaSymbol3::PGOEntryCount
  .property uint32 PGOEdgeCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C1 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_PGOEdgeCount()
  } // end of property IDiaSymbol3::PGOEdgeCount
  .property uint64 PGODynamicInstructionCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C2 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol3::get_PGODynamicInstructionCount()
  } // end of property IDiaSymbol3::PGODynamicInstructionCount
  .property uint32 staticSize()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C3 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_staticSize()
  } // end of property IDiaSymbol3::staticSize
  .property uint32 finalLiveStaticSize()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C4 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_finalLiveStaticSize()
  } // end of property IDiaSymbol3::finalLiveStaticSize
  .property string phaseName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C5 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol3::get_phaseName()
  } // end of property IDiaSymbol3::phaseName
  .property int32 hasControlFlowCheck()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C6 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_hasControlFlowCheck()
  } // end of property IDiaSymbol3::hasControlFlowCheck
  .property int32 constantExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C7 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_constantExport()
  } // end of property IDiaSymbol3::constantExport
  .property int32 dataExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C8 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_dataExport()
  } // end of property IDiaSymbol3::dataExport
  .property int32 privateExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_privateExport()
  } // end of property IDiaSymbol3::privateExport
  .property int32 noNameExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_noNameExport()
  } // end of property IDiaSymbol3::noNameExport
  .property int32 exportHasExplicitlyAssignedOrdinal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CB 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_exportHasExplicitlyAssignedOrdinal()
  } // end of property IDiaSymbol3::exportHasExplicitlyAssignedOrdinal
  .property int32 exportIsForwarder()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CC 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_exportIsForwarder()
  } // end of property IDiaSymbol3::exportIsForwarder
  .property uint32 ordinal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CD 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_ordinal()
  } // end of property IDiaSymbol3::ordinal
  .property uint32 frameSize()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CE 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_frameSize()
  } // end of property IDiaSymbol3::frameSize
  .property uint32 exceptionHandlerAddressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CF 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_exceptionHandlerAddressSection()
  } // end of property IDiaSymbol3::exceptionHandlerAddressSection
  .property uint32 exceptionHandlerAddressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D0 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_exceptionHandlerAddressOffset()
  } // end of property IDiaSymbol3::exceptionHandlerAddressOffset
  .property uint32 exceptionHandlerRelativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D1 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_exceptionHandlerRelativeVirtualAddress()
  } // end of property IDiaSymbol3::exceptionHandlerRelativeVirtualAddress
  .property uint64 exceptionHandlerVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D2 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol3::get_exceptionHandlerVirtualAddress()
  } // end of property IDiaSymbol3::exceptionHandlerVirtualAddress
  .property uint32 characteristics()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D3 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_characteristics()
  } // end of property IDiaSymbol3::characteristics
  .property class DIALib.IDiaSymbol coffGroup()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D4 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol3::get_coffGroup()
  } // end of property IDiaSymbol3::coffGroup
  .property uint32 bindID()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D5 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_bindID()
  } // end of property IDiaSymbol3::bindID
  .property uint32 bindSpace()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D6 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_bindSpace()
  } // end of property IDiaSymbol3::bindSpace
  .property uint32 bindSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D7 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_bindSlot()
  } // end of property IDiaSymbol3::bindSlot
  .property int32 isObjCClass()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D8 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isObjCClass()
  } // end of property IDiaSymbol3::isObjCClass
  .property int32 isObjCCategory()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isObjCCategory()
  } // end of property IDiaSymbol3::isObjCCategory
  .property int32 isObjCProtocol()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 DA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol3::get_isObjCProtocol()
  } // end of property IDiaSymbol3::isObjCProtocol
  .property class DIALib.IDiaSymbol inlinee()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 DB 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol3::get_inlinee()
  } // end of property IDiaSymbol3::inlinee
  .property uint32 inlineeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 DC 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol3::get_inlineeId()
  } // end of property IDiaSymbol3::inlineeId
} // end of class DIALib.IDiaSymbol3

.class interface public abstract auto ansi import DIALib.IDiaSymbol4
       implements DIALib.IDiaSymbol3
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 0A 73 79 6D 49 6E 64 65 78 49 64 00 00 )    // ...symIndexId..
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 42 46 36 43 38 38 41 37 2D 45 39 44 36   // ..$BF6C88A7-E9D6
                                                                                                  2D 34 33 34 36 2D 39 39 41 31 2D 44 30 35 33 44   // -4346-99A1-D053D
                                                                                                  45 35 41 37 38 30 38 00 00 )                      // E5A7808..
  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_symIndexId() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_symIndexId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_symTag() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_symTag

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_name() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_name

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_lexicalParent() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_lexicalParent

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_classParent() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_classParent

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_type() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_type

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_dataKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_dataKind

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_locationType() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_locationType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_addressSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_addressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_addressOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_addressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_relativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_relativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_virtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_virtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_registerId() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_registerId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_offset() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_offset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_length() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_length

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_slot() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_slot

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_volatileType() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_volatileType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_constType() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_constType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_unalignedType() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_unalignedType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_access() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_access

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_libraryName() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_libraryName

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_platform() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_platform

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_language() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_language

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_editAndContinueEnabled() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_editAndContinueEnabled

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndMajor() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_frontEndMajor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndMinor() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_frontEndMinor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndBuild() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_frontEndBuild

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndMajor() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_backEndMajor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndMinor() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_backEndMinor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndBuild() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_backEndBuild

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_sourceFileName() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_sourceFileName

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_unused() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_unused

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_thunkOrdinal() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_thunkOrdinal

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_thisAdjust() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_thisAdjust

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_virtualBaseOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_virtualBaseOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_virtual() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_virtual

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_intro() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_intro

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_pure() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_pure

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_callingConvention() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_callingConvention

  .method public hidebysig newslot specialname abstract virtual 
          instance object 
          marshal( struct) 
          get_value() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_value

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseType() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_baseType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_token() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_token

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_timeStamp() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_timeStamp

  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype [mscorlib]System.Guid 
          get_guid() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_guid

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_symbolsFileName() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_symbolsFileName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_reference() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_reference

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_count() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_count

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bitPosition() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_bitPosition

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_arrayIndexType() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_arrayIndexType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_packed() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_packed

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_constructor() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_constructor

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_overloadedOperator() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_overloadedOperator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_nested() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_nested

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasNestedTypes() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_hasNestedTypes

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasAssignmentOperator() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_hasAssignmentOperator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasCastOperator() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_hasCastOperator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_scoped() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_scoped

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_virtualBaseClass() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_virtualBaseClass

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_indirectVirtualBaseClass() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_indirectVirtualBaseClass

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_virtualBasePointerOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_virtualBasePointerOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_virtualTableShape() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_virtualTableShape

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lexicalParentId() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_lexicalParentId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_classParentId() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_classParentId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_typeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_typeId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_arrayIndexTypeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_arrayIndexTypeId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_virtualTableShapeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_virtualTableShapeId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_code() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_code

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_function() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_function

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_managed() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_managed

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_msil() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_msil

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_virtualBaseDispIndex() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_virtualBaseDispIndex

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_undecoratedName() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_undecoratedName

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_age() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_age

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_signature() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_signature

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_compilerGenerated() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_compilerGenerated

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_addressTaken() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_addressTaken

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_rank() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_rank

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_lowerBound() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_lowerBound

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_upperBound() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_upperBound

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lowerBoundId() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_lowerBoundId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_upperBoundId() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_upperBoundId

  .method public hidebysig newslot abstract virtual 
          instance void  get_dataBytes([in] uint32 cbData,
                                       [out] uint32& pcbData,
                                       [out] uint8& pbData) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_dataBytes

  .method public hidebysig newslot abstract virtual 
          instance void  findChildren([in] valuetype DIALib.SymTagEnum symTag,
                                      [in] string  marshal( lpwstr) name,
                                      [in] uint32 compareFlags,
                                      [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::findChildren

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenEx([in] valuetype DIALib.SymTagEnum symTag,
                                        [in] string  marshal( lpwstr) name,
                                        [in] uint32 compareFlags,
                                        [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::findChildrenEx

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByAddr([in] valuetype DIALib.SymTagEnum symTag,
                                              [in] string  marshal( lpwstr) name,
                                              [in] uint32 compareFlags,
                                              [in] uint32 isect,
                                              [in] uint32 offset,
                                              [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::findChildrenExByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByVA([in] valuetype DIALib.SymTagEnum symTag,
                                            [in] string  marshal( lpwstr) name,
                                            [in] uint32 compareFlags,
                                            [in] uint64 va,
                                            [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::findChildrenExByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByRVA([in] valuetype DIALib.SymTagEnum symTag,
                                             [in] string  marshal( lpwstr) name,
                                             [in] uint32 compareFlags,
                                             [in] uint32 rva,
                                             [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::findChildrenExByRVA

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_targetSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_targetSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_targetOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_targetOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_targetRelativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_targetRelativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_targetVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_targetVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_machineType() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_machineType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_oemId() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_oemId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_oemSymbolId() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_oemSymbolId

  .method public hidebysig newslot abstract virtual 
          instance void  get_types([in] uint32 cTypes,
                                   [out] uint32& pcTypes,
                                   [out] class DIALib.IDiaSymbol&  marshal( interface ) pTypes) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_types

  .method public hidebysig newslot abstract virtual 
          instance void  get_typeIds([in] uint32 cTypeIds,
                                     [out] uint32& pcTypeIds,
                                     [out] uint32& pdwTypeIds) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_typeIds

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_objectPointerType() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_objectPointerType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_udtKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_udtKind

  .method public hidebysig newslot abstract virtual 
          instance void  get_undecoratedNameEx([in] uint32 undecorateOptions,
                                               [out] string&  marshal( bstr) name) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_undecoratedNameEx

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noReturn() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_noReturn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_customCallingConvention() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_customCallingConvention

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noInline() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_noInline

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_optimizedCodeDebugInfo() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_optimizedCodeDebugInfo

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_notReached() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_notReached

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_interruptReturn() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_interruptReturn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_farReturn() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_farReturn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isStatic() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isStatic

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasDebugInfo() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_hasDebugInfo

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isLTCG() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isLTCG

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isDataAligned() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isDataAligned

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasSecurityChecks() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_hasSecurityChecks

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_compilerName() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_compilerName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasAlloca() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_hasAlloca

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasSetJump() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_hasSetJump

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasLongJump() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_hasLongJump

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasInlAsm() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_hasInlAsm

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasEH() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_hasEH

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasSEH() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_hasSEH

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasEHa() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_hasEHa

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isNaked() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isNaked

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAggregated() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isAggregated

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSplitted() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isSplitted

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_container() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_container

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_inlSpec() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_inlSpec

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noStackOrdering() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_noStackOrdering

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_virtualBaseTableType() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_virtualBaseTableType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasManagedCode() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_hasManagedCode

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isHotpatchable() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isHotpatchable

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isCVTCIL() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isCVTCIL

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isMSILNetmodule() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isMSILNetmodule

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isCTypes() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isCTypes

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isStripped() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isStripped

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndQFE() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_frontEndQFE

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndQFE() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_backEndQFE

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_wasInlined() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_wasInlined

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_strictGSCheck() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_strictGSCheck

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isCxxReturnUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isCxxReturnUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isConstructorVirtualBase() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isConstructorVirtualBase

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_RValueReference() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_RValueReference

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_unmodifiedType() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_unmodifiedType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_framePointerPresent() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_framePointerPresent

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSafeBuffers() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isSafeBuffers

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_intrinsic() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_intrinsic

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_sealed() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_sealed

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hfaFloat() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_hfaFloat

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hfaDouble() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_hfaDouble

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_liveRangeStartAddressSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_liveRangeStartAddressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_liveRangeStartAddressOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_liveRangeStartAddressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_liveRangeStartRelativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_liveRangeStartRelativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_countLiveRanges() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_countLiveRanges

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_liveRangeLength() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_liveRangeLength

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_offsetInUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_offsetInUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_paramBasePointerRegisterId() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_paramBasePointerRegisterId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_localBasePointerRegisterId() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_localBasePointerRegisterId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isLocationControlFlowDependent() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isLocationControlFlowDependent

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_stride() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_stride

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfRows() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_numberOfRows

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfColumns() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_numberOfColumns

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isMatrixRowMajor() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isMatrixRowMajor

  .method public hidebysig newslot abstract virtual 
          instance void  get_numericProperties([in] uint32 cnt,
                                               [out] uint32& pcnt,
                                               [out] uint32& pProperties) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_numericProperties

  .method public hidebysig newslot abstract virtual 
          instance void  get_modifierValues([in] uint32 cnt,
                                            [out] uint32& pcnt,
                                            [out] uint16& pModifiers) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_modifierValues

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isReturnValue() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isReturnValue

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isOptimizedAway() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isOptimizedAway

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_builtInKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_builtInKind

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_registerType() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_registerType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseDataSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_baseDataSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseDataOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_baseDataOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_textureSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_textureSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_samplerSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_samplerSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_uavSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_uavSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_sizeInUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_sizeInUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_memorySpaceKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_memorySpaceKind

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_unmodifiedTypeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_unmodifiedTypeId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_subTypeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_subTypeId

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_subType() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_subType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfModifiers() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_numberOfModifiers

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfRegisterIndices() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_numberOfRegisterIndices

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isHLSLData() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isHLSLData

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPointerToDataMember() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isPointerToDataMember

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPointerToMemberFunction() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isPointerToMemberFunction

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSingleInheritance() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isSingleInheritance

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isMultipleInheritance() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isMultipleInheritance

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isVirtualInheritance() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isVirtualInheritance

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_restrictedType() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_restrictedType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPointerBasedOnSymbolValue() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isPointerBasedOnSymbolValue

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_baseSymbol() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_baseSymbol

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseSymbolId() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_baseSymbolId

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_objectFileName() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_objectFileName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAcceleratorGroupSharedLocal() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isAcceleratorGroupSharedLocal

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAcceleratorPointerTagLiveRange() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isAcceleratorPointerTagLiveRange

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAcceleratorStubFunction() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isAcceleratorStubFunction

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfAcceleratorPointerTags() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_numberOfAcceleratorPointerTags

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSdl() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isSdl

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isWinRTPointer() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isWinRTPointer

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isRefUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isRefUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isValueUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isValueUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isInterfaceUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isInterfaceUdt

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByAddr([in] uint32 isect,
                                                [in] uint32 offset,
                                                [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::findInlineFramesByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByRVA([in] uint32 rva,
                                               [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::findInlineFramesByRVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByVA([in] uint64 va,
                                              [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::findInlineFramesByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLines([out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::findInlineeLines

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByAddr([in] uint32 isect,
                                                [in] uint32 offset,
                                                [in] uint32 length,
                                                [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::findInlineeLinesByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByRVA([in] uint32 rva,
                                               [in] uint32 length,
                                               [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::findInlineeLinesByRVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByVA([in] uint64 va,
                                              [in] uint32 length,
                                              [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::findInlineeLinesByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findSymbolsForAcceleratorPointerTag([in] uint32 tagValue,
                                                             [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::findSymbolsForAcceleratorPointerTag

  .method public hidebysig newslot abstract virtual 
          instance void  findSymbolsByRVAForAcceleratorPointerTag([in] uint32 tagValue,
                                                                  [in] uint32 rva,
                                                                  [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::findSymbolsByRVAForAcceleratorPointerTag

  .method public hidebysig newslot abstract virtual 
          instance void  get_acceleratorPointerTags([in] uint32 cnt,
                                                    [out] uint32& pcnt,
                                                    [out] uint32& pPointerTags) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_acceleratorPointerTags

  .method public hidebysig newslot abstract virtual 
          instance void  getSrcLineOnTypeDefn([out] class DIALib.IDiaLineNumber&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::getSrcLineOnTypeDefn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPGO() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isPGO

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasValidPGOCounts() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_hasValidPGOCounts

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isOptimizedForSpeed() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isOptimizedForSpeed

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_PGOEntryCount() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_PGOEntryCount

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_PGOEdgeCount() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_PGOEdgeCount

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_PGODynamicInstructionCount() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_PGODynamicInstructionCount

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_staticSize() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_staticSize

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_finalLiveStaticSize() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_finalLiveStaticSize

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_phaseName() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_phaseName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasControlFlowCheck() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_hasControlFlowCheck

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_constantExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_constantExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_dataExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_dataExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_privateExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_privateExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noNameExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_noNameExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_exportHasExplicitlyAssignedOrdinal() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_exportHasExplicitlyAssignedOrdinal

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_exportIsForwarder() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_exportIsForwarder

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_ordinal() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_ordinal

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frameSize() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_frameSize

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_exceptionHandlerAddressSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_exceptionHandlerAddressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_exceptionHandlerAddressOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_exceptionHandlerAddressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_exceptionHandlerRelativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_exceptionHandlerRelativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_exceptionHandlerVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_exceptionHandlerVirtualAddress

  .method public hidebysig newslot abstract virtual 
          instance void  findInputAssemblyFile([out] class DIALib.IDiaInputAssemblyFile&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol4::findInputAssemblyFile

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_characteristics() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_characteristics

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_coffGroup() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_coffGroup

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bindID() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_bindID

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bindSpace() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_bindSpace

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bindSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_bindSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isObjCClass() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isObjCClass

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isObjCCategory() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isObjCCategory

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isObjCProtocol() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_isObjCProtocol

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_inlinee() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_inlinee

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_inlineeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_inlineeId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noexcept() runtime managed internalcall
  {
  } // end of method IDiaSymbol4::get_noexcept

  .property uint32 symIndexId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_symIndexId()
  } // end of property IDiaSymbol4::symIndexId
  .property uint32 symTag()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_symTag()
  } // end of property IDiaSymbol4::symTag
  .property string name()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol4::get_name()
  } // end of property IDiaSymbol4::name
  .property class DIALib.IDiaSymbol lexicalParent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol4::get_lexicalParent()
  } // end of property IDiaSymbol4::lexicalParent
  .property class DIALib.IDiaSymbol classParent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 04 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol4::get_classParent()
  } // end of property IDiaSymbol4::classParent
  .property class DIALib.IDiaSymbol 'type'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 05 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol4::get_type()
  } // end of property IDiaSymbol4::'type'
  .property uint32 dataKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 06 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_dataKind()
  } // end of property IDiaSymbol4::dataKind
  .property uint32 locationType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 07 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_locationType()
  } // end of property IDiaSymbol4::locationType
  .property uint32 addressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_addressSection()
  } // end of property IDiaSymbol4::addressSection
  .property uint32 addressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 09 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_addressOffset()
  } // end of property IDiaSymbol4::addressOffset
  .property uint32 relativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0A 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_relativeVirtualAddress()
  } // end of property IDiaSymbol4::relativeVirtualAddress
  .property uint64 virtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0B 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol4::get_virtualAddress()
  } // end of property IDiaSymbol4::virtualAddress
  .property uint32 registerId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_registerId()
  } // end of property IDiaSymbol4::registerId
  .property int32 offset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0D 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_offset()
  } // end of property IDiaSymbol4::offset
  .property uint64 length()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0E 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol4::get_length()
  } // end of property IDiaSymbol4::length
  .property uint32 slot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_slot()
  } // end of property IDiaSymbol4::slot
  .property int32 volatileType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 10 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_volatileType()
  } // end of property IDiaSymbol4::volatileType
  .property int32 constType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 11 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_constType()
  } // end of property IDiaSymbol4::constType
  .property int32 unalignedType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 12 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_unalignedType()
  } // end of property IDiaSymbol4::unalignedType
  .property uint32 access()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 13 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_access()
  } // end of property IDiaSymbol4::access
  .property string libraryName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 14 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol4::get_libraryName()
  } // end of property IDiaSymbol4::libraryName
  .property uint32 platform()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 15 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_platform()
  } // end of property IDiaSymbol4::platform
  .property uint32 language()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 16 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_language()
  } // end of property IDiaSymbol4::language
  .property int32 editAndContinueEnabled()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 17 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_editAndContinueEnabled()
  } // end of property IDiaSymbol4::editAndContinueEnabled
  .property uint32 frontEndMajor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 18 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_frontEndMajor()
  } // end of property IDiaSymbol4::frontEndMajor
  .property uint32 frontEndMinor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 19 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_frontEndMinor()
  } // end of property IDiaSymbol4::frontEndMinor
  .property uint32 frontEndBuild()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1A 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_frontEndBuild()
  } // end of property IDiaSymbol4::frontEndBuild
  .property uint32 backEndMajor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1B 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_backEndMajor()
  } // end of property IDiaSymbol4::backEndMajor
  .property uint32 backEndMinor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_backEndMinor()
  } // end of property IDiaSymbol4::backEndMinor
  .property uint32 backEndBuild()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1D 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_backEndBuild()
  } // end of property IDiaSymbol4::backEndBuild
  .property string sourceFileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1E 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol4::get_sourceFileName()
  } // end of property IDiaSymbol4::sourceFileName
  .property string 'unused'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1F 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol4::get_unused()
  } // end of property IDiaSymbol4::'unused'
  .property uint32 thunkOrdinal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 20 00 00 00 00 00 )                         // .. .....
    .get instance uint32 DIALib.IDiaSymbol4::get_thunkOrdinal()
  } // end of property IDiaSymbol4::thunkOrdinal
  .property int32 thisAdjust()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 21 00 00 00 00 00 )                         // ..!.....
    .get instance int32 DIALib.IDiaSymbol4::get_thisAdjust()
  } // end of property IDiaSymbol4::thisAdjust
  .property uint32 virtualBaseOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 22 00 00 00 00 00 )                         // ..".....
    .get instance uint32 DIALib.IDiaSymbol4::get_virtualBaseOffset()
  } // end of property IDiaSymbol4::virtualBaseOffset
  .property int32 'virtual'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 23 00 00 00 00 00 )                         // ..#.....
    .get instance int32 DIALib.IDiaSymbol4::get_virtual()
  } // end of property IDiaSymbol4::'virtual'
  .property int32 intro()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 24 00 00 00 00 00 )                         // ..$.....
    .get instance int32 DIALib.IDiaSymbol4::get_intro()
  } // end of property IDiaSymbol4::intro
  .property int32 pure()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 25 00 00 00 00 00 )                         // ..%.....
    .get instance int32 DIALib.IDiaSymbol4::get_pure()
  } // end of property IDiaSymbol4::pure
  .property uint32 callingConvention()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 26 00 00 00 00 00 )                         // ..&.....
    .get instance uint32 DIALib.IDiaSymbol4::get_callingConvention()
  } // end of property IDiaSymbol4::callingConvention
  .property object 'value'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 27 00 00 00 00 00 )                         // ..'.....
    .get instance object DIALib.IDiaSymbol4::get_value()
  } // end of property IDiaSymbol4::'value'
  .property uint32 baseType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 28 00 00 00 00 00 )                         // ..(.....
    .get instance uint32 DIALib.IDiaSymbol4::get_baseType()
  } // end of property IDiaSymbol4::baseType
  .property uint32 token()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 29 00 00 00 00 00 )                         // ..).....
    .get instance uint32 DIALib.IDiaSymbol4::get_token()
  } // end of property IDiaSymbol4::token
  .property uint32 timeStamp()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2A 00 00 00 00 00 )                         // ..*.....
    .get instance uint32 DIALib.IDiaSymbol4::get_timeStamp()
  } // end of property IDiaSymbol4::timeStamp
  .property valuetype [mscorlib]System.Guid
          guid()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2B 00 00 00 00 00 )                         // ..+.....
    .get instance valuetype [mscorlib]System.Guid DIALib.IDiaSymbol4::get_guid()
  } // end of property IDiaSymbol4::guid
  .property string symbolsFileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2C 00 00 00 00 00 )                         // ..,.....
    .get instance string DIALib.IDiaSymbol4::get_symbolsFileName()
  } // end of property IDiaSymbol4::symbolsFileName
  .property int32 reference()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2E 00 00 00 00 00 )                         // ........
    .get instance int32 DIALib.IDiaSymbol4::get_reference()
  } // end of property IDiaSymbol4::reference
  .property uint32 count()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2F 00 00 00 00 00 )                         // ../.....
    .get instance uint32 DIALib.IDiaSymbol4::get_count()
  } // end of property IDiaSymbol4::count
  .property uint32 bitPosition()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 31 00 00 00 00 00 )                         // ..1.....
    .get instance uint32 DIALib.IDiaSymbol4::get_bitPosition()
  } // end of property IDiaSymbol4::bitPosition
  .property class DIALib.IDiaSymbol arrayIndexType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 32 00 00 00 00 00 )                         // ..2.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol4::get_arrayIndexType()
  } // end of property IDiaSymbol4::arrayIndexType
  .property int32 packed()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 33 00 00 00 00 00 )                         // ..3.....
    .get instance int32 DIALib.IDiaSymbol4::get_packed()
  } // end of property IDiaSymbol4::packed
  .property int32 constructor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 34 00 00 00 00 00 )                         // ..4.....
    .get instance int32 DIALib.IDiaSymbol4::get_constructor()
  } // end of property IDiaSymbol4::constructor
  .property int32 overloadedOperator()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 35 00 00 00 00 00 )                         // ..5.....
    .get instance int32 DIALib.IDiaSymbol4::get_overloadedOperator()
  } // end of property IDiaSymbol4::overloadedOperator
  .property int32 'nested'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 36 00 00 00 00 00 )                         // ..6.....
    .get instance int32 DIALib.IDiaSymbol4::get_nested()
  } // end of property IDiaSymbol4::'nested'
  .property int32 hasNestedTypes()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 37 00 00 00 00 00 )                         // ..7.....
    .get instance int32 DIALib.IDiaSymbol4::get_hasNestedTypes()
  } // end of property IDiaSymbol4::hasNestedTypes
  .property int32 hasAssignmentOperator()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 38 00 00 00 00 00 )                         // ..8.....
    .get instance int32 DIALib.IDiaSymbol4::get_hasAssignmentOperator()
  } // end of property IDiaSymbol4::hasAssignmentOperator
  .property int32 hasCastOperator()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 39 00 00 00 00 00 )                         // ..9.....
    .get instance int32 DIALib.IDiaSymbol4::get_hasCastOperator()
  } // end of property IDiaSymbol4::hasCastOperator
  .property int32 scoped()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3A 00 00 00 00 00 )                         // ..:.....
    .get instance int32 DIALib.IDiaSymbol4::get_scoped()
  } // end of property IDiaSymbol4::scoped
  .property int32 virtualBaseClass()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3B 00 00 00 00 00 )                         // ..;.....
    .get instance int32 DIALib.IDiaSymbol4::get_virtualBaseClass()
  } // end of property IDiaSymbol4::virtualBaseClass
  .property int32 indirectVirtualBaseClass()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3C 00 00 00 00 00 )                         // ..<.....
    .get instance int32 DIALib.IDiaSymbol4::get_indirectVirtualBaseClass()
  } // end of property IDiaSymbol4::indirectVirtualBaseClass
  .property int32 virtualBasePointerOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3D 00 00 00 00 00 )                         // ..=.....
    .get instance int32 DIALib.IDiaSymbol4::get_virtualBasePointerOffset()
  } // end of property IDiaSymbol4::virtualBasePointerOffset
  .property class DIALib.IDiaSymbol virtualTableShape()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3E 00 00 00 00 00 )                         // ..>.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol4::get_virtualTableShape()
  } // end of property IDiaSymbol4::virtualTableShape
  .property uint32 lexicalParentId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 40 00 00 00 00 00 )                         // ..@.....
    .get instance uint32 DIALib.IDiaSymbol4::get_lexicalParentId()
  } // end of property IDiaSymbol4::lexicalParentId
  .property uint32 classParentId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 41 00 00 00 00 00 )                         // ..A.....
    .get instance uint32 DIALib.IDiaSymbol4::get_classParentId()
  } // end of property IDiaSymbol4::classParentId
  .property uint32 typeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 42 00 00 00 00 00 )                         // ..B.....
    .get instance uint32 DIALib.IDiaSymbol4::get_typeId()
  } // end of property IDiaSymbol4::typeId
  .property uint32 arrayIndexTypeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 43 00 00 00 00 00 )                         // ..C.....
    .get instance uint32 DIALib.IDiaSymbol4::get_arrayIndexTypeId()
  } // end of property IDiaSymbol4::arrayIndexTypeId
  .property uint32 virtualTableShapeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 44 00 00 00 00 00 )                         // ..D.....
    .get instance uint32 DIALib.IDiaSymbol4::get_virtualTableShapeId()
  } // end of property IDiaSymbol4::virtualTableShapeId
  .property int32 code()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 45 00 00 00 00 00 )                         // ..E.....
    .get instance int32 DIALib.IDiaSymbol4::get_code()
  } // end of property IDiaSymbol4::code
  .property int32 function()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 46 00 00 00 00 00 )                         // ..F.....
    .get instance int32 DIALib.IDiaSymbol4::get_function()
  } // end of property IDiaSymbol4::function
  .property int32 'managed'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 47 00 00 00 00 00 )                         // ..G.....
    .get instance int32 DIALib.IDiaSymbol4::get_managed()
  } // end of property IDiaSymbol4::'managed'
  .property int32 msil()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 48 00 00 00 00 00 )                         // ..H.....
    .get instance int32 DIALib.IDiaSymbol4::get_msil()
  } // end of property IDiaSymbol4::msil
  .property uint32 virtualBaseDispIndex()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 49 00 00 00 00 00 )                         // ..I.....
    .get instance uint32 DIALib.IDiaSymbol4::get_virtualBaseDispIndex()
  } // end of property IDiaSymbol4::virtualBaseDispIndex
  .property string undecoratedName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4A 00 00 00 00 00 )                         // ..J.....
    .get instance string DIALib.IDiaSymbol4::get_undecoratedName()
  } // end of property IDiaSymbol4::undecoratedName
  .property uint32 age()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4B 00 00 00 00 00 )                         // ..K.....
    .get instance uint32 DIALib.IDiaSymbol4::get_age()
  } // end of property IDiaSymbol4::age
  .property uint32 signature()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4C 00 00 00 00 00 )                         // ..L.....
    .get instance uint32 DIALib.IDiaSymbol4::get_signature()
  } // end of property IDiaSymbol4::signature
  .property int32 compilerGenerated()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4D 00 00 00 00 00 )                         // ..M.....
    .get instance int32 DIALib.IDiaSymbol4::get_compilerGenerated()
  } // end of property IDiaSymbol4::compilerGenerated
  .property int32 addressTaken()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4E 00 00 00 00 00 )                         // ..N.....
    .get instance int32 DIALib.IDiaSymbol4::get_addressTaken()
  } // end of property IDiaSymbol4::addressTaken
  .property uint32 rank()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4F 00 00 00 00 00 )                         // ..O.....
    .get instance uint32 DIALib.IDiaSymbol4::get_rank()
  } // end of property IDiaSymbol4::rank
  .property class DIALib.IDiaSymbol lowerBound()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 50 00 00 00 00 00 )                         // ..P.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol4::get_lowerBound()
  } // end of property IDiaSymbol4::lowerBound
  .property class DIALib.IDiaSymbol upperBound()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 51 00 00 00 00 00 )                         // ..Q.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol4::get_upperBound()
  } // end of property IDiaSymbol4::upperBound
  .property uint32 lowerBoundId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 52 00 00 00 00 00 )                         // ..R.....
    .get instance uint32 DIALib.IDiaSymbol4::get_lowerBoundId()
  } // end of property IDiaSymbol4::lowerBoundId
  .property uint32 upperBoundId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 53 00 00 00 00 00 )                         // ..S.....
    .get instance uint32 DIALib.IDiaSymbol4::get_upperBoundId()
  } // end of property IDiaSymbol4::upperBoundId
  .property uint32 targetSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 54 00 00 00 00 00 )                         // ..T.....
    .get instance uint32 DIALib.IDiaSymbol4::get_targetSection()
  } // end of property IDiaSymbol4::targetSection
  .property uint32 targetOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 55 00 00 00 00 00 )                         // ..U.....
    .get instance uint32 DIALib.IDiaSymbol4::get_targetOffset()
  } // end of property IDiaSymbol4::targetOffset
  .property uint32 targetRelativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 56 00 00 00 00 00 )                         // ..V.....
    .get instance uint32 DIALib.IDiaSymbol4::get_targetRelativeVirtualAddress()
  } // end of property IDiaSymbol4::targetRelativeVirtualAddress
  .property uint64 targetVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 57 00 00 00 00 00 )                         // ..W.....
    .get instance uint64 DIALib.IDiaSymbol4::get_targetVirtualAddress()
  } // end of property IDiaSymbol4::targetVirtualAddress
  .property uint32 machineType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 58 00 00 00 00 00 )                         // ..X.....
    .get instance uint32 DIALib.IDiaSymbol4::get_machineType()
  } // end of property IDiaSymbol4::machineType
  .property uint32 oemId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 59 00 00 00 00 00 )                         // ..Y.....
    .get instance uint32 DIALib.IDiaSymbol4::get_oemId()
  } // end of property IDiaSymbol4::oemId
  .property uint32 oemSymbolId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5A 00 00 00 00 00 )                         // ..Z.....
    .get instance uint32 DIALib.IDiaSymbol4::get_oemSymbolId()
  } // end of property IDiaSymbol4::oemSymbolId
  .property class DIALib.IDiaSymbol objectPointerType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5B 00 00 00 00 00 )                         // ..[.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol4::get_objectPointerType()
  } // end of property IDiaSymbol4::objectPointerType
  .property uint32 udtKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5C 00 00 00 00 00 )                         // ..\.....
    .get instance uint32 DIALib.IDiaSymbol4::get_udtKind()
  } // end of property IDiaSymbol4::udtKind
  .property int32 noReturn()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5D 00 00 00 00 00 )                         // ..].....
    .get instance int32 DIALib.IDiaSymbol4::get_noReturn()
  } // end of property IDiaSymbol4::noReturn
  .property int32 customCallingConvention()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5E 00 00 00 00 00 )                         // ..^.....
    .get instance int32 DIALib.IDiaSymbol4::get_customCallingConvention()
  } // end of property IDiaSymbol4::customCallingConvention
  .property int32 noInline()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5F 00 00 00 00 00 )                         // .._.....
    .get instance int32 DIALib.IDiaSymbol4::get_noInline()
  } // end of property IDiaSymbol4::noInline
  .property int32 optimizedCodeDebugInfo()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 60 00 00 00 00 00 )                         // ..`.....
    .get instance int32 DIALib.IDiaSymbol4::get_optimizedCodeDebugInfo()
  } // end of property IDiaSymbol4::optimizedCodeDebugInfo
  .property int32 notReached()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 61 00 00 00 00 00 )                         // ..a.....
    .get instance int32 DIALib.IDiaSymbol4::get_notReached()
  } // end of property IDiaSymbol4::notReached
  .property int32 interruptReturn()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 62 00 00 00 00 00 )                         // ..b.....
    .get instance int32 DIALib.IDiaSymbol4::get_interruptReturn()
  } // end of property IDiaSymbol4::interruptReturn
  .property int32 farReturn()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 63 00 00 00 00 00 )                         // ..c.....
    .get instance int32 DIALib.IDiaSymbol4::get_farReturn()
  } // end of property IDiaSymbol4::farReturn
  .property int32 isStatic()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 64 00 00 00 00 00 )                         // ..d.....
    .get instance int32 DIALib.IDiaSymbol4::get_isStatic()
  } // end of property IDiaSymbol4::isStatic
  .property int32 hasDebugInfo()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 65 00 00 00 00 00 )                         // ..e.....
    .get instance int32 DIALib.IDiaSymbol4::get_hasDebugInfo()
  } // end of property IDiaSymbol4::hasDebugInfo
  .property int32 isLTCG()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 66 00 00 00 00 00 )                         // ..f.....
    .get instance int32 DIALib.IDiaSymbol4::get_isLTCG()
  } // end of property IDiaSymbol4::isLTCG
  .property int32 isDataAligned()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 67 00 00 00 00 00 )                         // ..g.....
    .get instance int32 DIALib.IDiaSymbol4::get_isDataAligned()
  } // end of property IDiaSymbol4::isDataAligned
  .property int32 hasSecurityChecks()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 68 00 00 00 00 00 )                         // ..h.....
    .get instance int32 DIALib.IDiaSymbol4::get_hasSecurityChecks()
  } // end of property IDiaSymbol4::hasSecurityChecks
  .property string compilerName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 69 00 00 00 00 00 )                         // ..i.....
    .get instance string DIALib.IDiaSymbol4::get_compilerName()
  } // end of property IDiaSymbol4::compilerName
  .property int32 hasAlloca()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6A 00 00 00 00 00 )                         // ..j.....
    .get instance int32 DIALib.IDiaSymbol4::get_hasAlloca()
  } // end of property IDiaSymbol4::hasAlloca
  .property int32 hasSetJump()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6B 00 00 00 00 00 )                         // ..k.....
    .get instance int32 DIALib.IDiaSymbol4::get_hasSetJump()
  } // end of property IDiaSymbol4::hasSetJump
  .property int32 hasLongJump()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6C 00 00 00 00 00 )                         // ..l.....
    .get instance int32 DIALib.IDiaSymbol4::get_hasLongJump()
  } // end of property IDiaSymbol4::hasLongJump
  .property int32 hasInlAsm()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6D 00 00 00 00 00 )                         // ..m.....
    .get instance int32 DIALib.IDiaSymbol4::get_hasInlAsm()
  } // end of property IDiaSymbol4::hasInlAsm
  .property int32 hasEH()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6E 00 00 00 00 00 )                         // ..n.....
    .get instance int32 DIALib.IDiaSymbol4::get_hasEH()
  } // end of property IDiaSymbol4::hasEH
  .property int32 hasSEH()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6F 00 00 00 00 00 )                         // ..o.....
    .get instance int32 DIALib.IDiaSymbol4::get_hasSEH()
  } // end of property IDiaSymbol4::hasSEH
  .property int32 hasEHa()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 70 00 00 00 00 00 )                         // ..p.....
    .get instance int32 DIALib.IDiaSymbol4::get_hasEHa()
  } // end of property IDiaSymbol4::hasEHa
  .property int32 isNaked()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 71 00 00 00 00 00 )                         // ..q.....
    .get instance int32 DIALib.IDiaSymbol4::get_isNaked()
  } // end of property IDiaSymbol4::isNaked
  .property int32 isAggregated()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 72 00 00 00 00 00 )                         // ..r.....
    .get instance int32 DIALib.IDiaSymbol4::get_isAggregated()
  } // end of property IDiaSymbol4::isAggregated
  .property int32 isSplitted()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 73 00 00 00 00 00 )                         // ..s.....
    .get instance int32 DIALib.IDiaSymbol4::get_isSplitted()
  } // end of property IDiaSymbol4::isSplitted
  .property class DIALib.IDiaSymbol container()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 74 00 00 00 00 00 )                         // ..t.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol4::get_container()
  } // end of property IDiaSymbol4::container
  .property int32 inlSpec()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 75 00 00 00 00 00 )                         // ..u.....
    .get instance int32 DIALib.IDiaSymbol4::get_inlSpec()
  } // end of property IDiaSymbol4::inlSpec
  .property int32 noStackOrdering()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 76 00 00 00 00 00 )                         // ..v.....
    .get instance int32 DIALib.IDiaSymbol4::get_noStackOrdering()
  } // end of property IDiaSymbol4::noStackOrdering
  .property class DIALib.IDiaSymbol virtualBaseTableType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 77 00 00 00 00 00 )                         // ..w.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol4::get_virtualBaseTableType()
  } // end of property IDiaSymbol4::virtualBaseTableType
  .property int32 hasManagedCode()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 78 00 00 00 00 00 )                         // ..x.....
    .get instance int32 DIALib.IDiaSymbol4::get_hasManagedCode()
  } // end of property IDiaSymbol4::hasManagedCode
  .property int32 isHotpatchable()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 79 00 00 00 00 00 )                         // ..y.....
    .get instance int32 DIALib.IDiaSymbol4::get_isHotpatchable()
  } // end of property IDiaSymbol4::isHotpatchable
  .property int32 isCVTCIL()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7A 00 00 00 00 00 )                         // ..z.....
    .get instance int32 DIALib.IDiaSymbol4::get_isCVTCIL()
  } // end of property IDiaSymbol4::isCVTCIL
  .property int32 isMSILNetmodule()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7B 00 00 00 00 00 )                         // ..{.....
    .get instance int32 DIALib.IDiaSymbol4::get_isMSILNetmodule()
  } // end of property IDiaSymbol4::isMSILNetmodule
  .property int32 isCTypes()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7C 00 00 00 00 00 )                         // ..|.....
    .get instance int32 DIALib.IDiaSymbol4::get_isCTypes()
  } // end of property IDiaSymbol4::isCTypes
  .property int32 isStripped()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7D 00 00 00 00 00 )                         // ..}.....
    .get instance int32 DIALib.IDiaSymbol4::get_isStripped()
  } // end of property IDiaSymbol4::isStripped
  .property uint32 frontEndQFE()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7E 00 00 00 00 00 )                         // ..~.....
    .get instance uint32 DIALib.IDiaSymbol4::get_frontEndQFE()
  } // end of property IDiaSymbol4::frontEndQFE
  .property uint32 backEndQFE()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_backEndQFE()
  } // end of property IDiaSymbol4::backEndQFE
  .property int32 wasInlined()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 80 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_wasInlined()
  } // end of property IDiaSymbol4::wasInlined
  .property int32 strictGSCheck()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 81 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_strictGSCheck()
  } // end of property IDiaSymbol4::strictGSCheck
  .property int32 isCxxReturnUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 82 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isCxxReturnUdt()
  } // end of property IDiaSymbol4::isCxxReturnUdt
  .property int32 isConstructorVirtualBase()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 83 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isConstructorVirtualBase()
  } // end of property IDiaSymbol4::isConstructorVirtualBase
  .property int32 RValueReference()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 84 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_RValueReference()
  } // end of property IDiaSymbol4::RValueReference
  .property class DIALib.IDiaSymbol unmodifiedType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 85 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol4::get_unmodifiedType()
  } // end of property IDiaSymbol4::unmodifiedType
  .property int32 framePointerPresent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 86 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_framePointerPresent()
  } // end of property IDiaSymbol4::framePointerPresent
  .property int32 isSafeBuffers()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 87 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isSafeBuffers()
  } // end of property IDiaSymbol4::isSafeBuffers
  .property int32 intrinsic()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 88 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_intrinsic()
  } // end of property IDiaSymbol4::intrinsic
  .property int32 'sealed'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 89 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_sealed()
  } // end of property IDiaSymbol4::'sealed'
  .property int32 hfaFloat()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8A 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_hfaFloat()
  } // end of property IDiaSymbol4::hfaFloat
  .property int32 hfaDouble()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8B 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_hfaDouble()
  } // end of property IDiaSymbol4::hfaDouble
  .property uint32 liveRangeStartAddressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_liveRangeStartAddressSection()
  } // end of property IDiaSymbol4::liveRangeStartAddressSection
  .property uint32 liveRangeStartAddressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8D 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_liveRangeStartAddressOffset()
  } // end of property IDiaSymbol4::liveRangeStartAddressOffset
  .property uint32 liveRangeStartRelativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8E 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_liveRangeStartRelativeVirtualAddress()
  } // end of property IDiaSymbol4::liveRangeStartRelativeVirtualAddress
  .property uint32 countLiveRanges()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_countLiveRanges()
  } // end of property IDiaSymbol4::countLiveRanges
  .property uint64 liveRangeLength()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 90 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol4::get_liveRangeLength()
  } // end of property IDiaSymbol4::liveRangeLength
  .property uint32 offsetInUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 91 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_offsetInUdt()
  } // end of property IDiaSymbol4::offsetInUdt
  .property uint32 paramBasePointerRegisterId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 92 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_paramBasePointerRegisterId()
  } // end of property IDiaSymbol4::paramBasePointerRegisterId
  .property uint32 localBasePointerRegisterId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 93 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_localBasePointerRegisterId()
  } // end of property IDiaSymbol4::localBasePointerRegisterId
  .property int32 isLocationControlFlowDependent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 94 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isLocationControlFlowDependent()
  } // end of property IDiaSymbol4::isLocationControlFlowDependent
  .property uint32 stride()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 95 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_stride()
  } // end of property IDiaSymbol4::stride
  .property uint32 numberOfRows()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 96 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_numberOfRows()
  } // end of property IDiaSymbol4::numberOfRows
  .property uint32 numberOfColumns()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 97 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_numberOfColumns()
  } // end of property IDiaSymbol4::numberOfColumns
  .property int32 isMatrixRowMajor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 98 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isMatrixRowMajor()
  } // end of property IDiaSymbol4::isMatrixRowMajor
  .property int32 isReturnValue()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 99 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isReturnValue()
  } // end of property IDiaSymbol4::isReturnValue
  .property int32 isOptimizedAway()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9A 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isOptimizedAway()
  } // end of property IDiaSymbol4::isOptimizedAway
  .property uint32 builtInKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9B 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_builtInKind()
  } // end of property IDiaSymbol4::builtInKind
  .property uint32 registerType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_registerType()
  } // end of property IDiaSymbol4::registerType
  .property uint32 baseDataSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9D 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_baseDataSlot()
  } // end of property IDiaSymbol4::baseDataSlot
  .property uint32 baseDataOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9E 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_baseDataOffset()
  } // end of property IDiaSymbol4::baseDataOffset
  .property uint32 textureSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_textureSlot()
  } // end of property IDiaSymbol4::textureSlot
  .property uint32 samplerSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A0 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_samplerSlot()
  } // end of property IDiaSymbol4::samplerSlot
  .property uint32 uavSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A1 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_uavSlot()
  } // end of property IDiaSymbol4::uavSlot
  .property uint32 sizeInUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A2 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_sizeInUdt()
  } // end of property IDiaSymbol4::sizeInUdt
  .property uint32 memorySpaceKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A3 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_memorySpaceKind()
  } // end of property IDiaSymbol4::memorySpaceKind
  .property uint32 unmodifiedTypeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A4 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_unmodifiedTypeId()
  } // end of property IDiaSymbol4::unmodifiedTypeId
  .property uint32 subTypeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A5 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_subTypeId()
  } // end of property IDiaSymbol4::subTypeId
  .property class DIALib.IDiaSymbol subType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A6 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol4::get_subType()
  } // end of property IDiaSymbol4::subType
  .property uint32 numberOfModifiers()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A7 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_numberOfModifiers()
  } // end of property IDiaSymbol4::numberOfModifiers
  .property uint32 numberOfRegisterIndices()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A8 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_numberOfRegisterIndices()
  } // end of property IDiaSymbol4::numberOfRegisterIndices
  .property int32 isHLSLData()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isHLSLData()
  } // end of property IDiaSymbol4::isHLSLData
  .property int32 isPointerToDataMember()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isPointerToDataMember()
  } // end of property IDiaSymbol4::isPointerToDataMember
  .property int32 isPointerToMemberFunction()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AB 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isPointerToMemberFunction()
  } // end of property IDiaSymbol4::isPointerToMemberFunction
  .property int32 isSingleInheritance()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AC 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isSingleInheritance()
  } // end of property IDiaSymbol4::isSingleInheritance
  .property int32 isMultipleInheritance()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AD 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isMultipleInheritance()
  } // end of property IDiaSymbol4::isMultipleInheritance
  .property int32 isVirtualInheritance()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AE 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isVirtualInheritance()
  } // end of property IDiaSymbol4::isVirtualInheritance
  .property int32 restrictedType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AF 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_restrictedType()
  } // end of property IDiaSymbol4::restrictedType
  .property int32 isPointerBasedOnSymbolValue()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B0 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isPointerBasedOnSymbolValue()
  } // end of property IDiaSymbol4::isPointerBasedOnSymbolValue
  .property class DIALib.IDiaSymbol baseSymbol()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B1 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol4::get_baseSymbol()
  } // end of property IDiaSymbol4::baseSymbol
  .property uint32 baseSymbolId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B2 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_baseSymbolId()
  } // end of property IDiaSymbol4::baseSymbolId
  .property string objectFileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B3 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol4::get_objectFileName()
  } // end of property IDiaSymbol4::objectFileName
  .property int32 isAcceleratorGroupSharedLocal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B4 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isAcceleratorGroupSharedLocal()
  } // end of property IDiaSymbol4::isAcceleratorGroupSharedLocal
  .property int32 isAcceleratorPointerTagLiveRange()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B5 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isAcceleratorPointerTagLiveRange()
  } // end of property IDiaSymbol4::isAcceleratorPointerTagLiveRange
  .property int32 isAcceleratorStubFunction()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B6 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isAcceleratorStubFunction()
  } // end of property IDiaSymbol4::isAcceleratorStubFunction
  .property uint32 numberOfAcceleratorPointerTags()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B7 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_numberOfAcceleratorPointerTags()
  } // end of property IDiaSymbol4::numberOfAcceleratorPointerTags
  .property int32 isSdl()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B8 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isSdl()
  } // end of property IDiaSymbol4::isSdl
  .property int32 isWinRTPointer()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isWinRTPointer()
  } // end of property IDiaSymbol4::isWinRTPointer
  .property int32 isRefUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isRefUdt()
  } // end of property IDiaSymbol4::isRefUdt
  .property int32 isValueUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BB 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isValueUdt()
  } // end of property IDiaSymbol4::isValueUdt
  .property int32 isInterfaceUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BC 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isInterfaceUdt()
  } // end of property IDiaSymbol4::isInterfaceUdt
  .property int32 isPGO()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BD 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isPGO()
  } // end of property IDiaSymbol4::isPGO
  .property int32 hasValidPGOCounts()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BE 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_hasValidPGOCounts()
  } // end of property IDiaSymbol4::hasValidPGOCounts
  .property int32 isOptimizedForSpeed()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BF 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isOptimizedForSpeed()
  } // end of property IDiaSymbol4::isOptimizedForSpeed
  .property uint32 PGOEntryCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C0 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_PGOEntryCount()
  } // end of property IDiaSymbol4::PGOEntryCount
  .property uint32 PGOEdgeCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C1 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_PGOEdgeCount()
  } // end of property IDiaSymbol4::PGOEdgeCount
  .property uint64 PGODynamicInstructionCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C2 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol4::get_PGODynamicInstructionCount()
  } // end of property IDiaSymbol4::PGODynamicInstructionCount
  .property uint32 staticSize()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C3 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_staticSize()
  } // end of property IDiaSymbol4::staticSize
  .property uint32 finalLiveStaticSize()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C4 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_finalLiveStaticSize()
  } // end of property IDiaSymbol4::finalLiveStaticSize
  .property string phaseName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C5 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol4::get_phaseName()
  } // end of property IDiaSymbol4::phaseName
  .property int32 hasControlFlowCheck()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C6 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_hasControlFlowCheck()
  } // end of property IDiaSymbol4::hasControlFlowCheck
  .property int32 constantExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C7 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_constantExport()
  } // end of property IDiaSymbol4::constantExport
  .property int32 dataExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C8 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_dataExport()
  } // end of property IDiaSymbol4::dataExport
  .property int32 privateExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_privateExport()
  } // end of property IDiaSymbol4::privateExport
  .property int32 noNameExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_noNameExport()
  } // end of property IDiaSymbol4::noNameExport
  .property int32 exportHasExplicitlyAssignedOrdinal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CB 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_exportHasExplicitlyAssignedOrdinal()
  } // end of property IDiaSymbol4::exportHasExplicitlyAssignedOrdinal
  .property int32 exportIsForwarder()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CC 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_exportIsForwarder()
  } // end of property IDiaSymbol4::exportIsForwarder
  .property uint32 ordinal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CD 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_ordinal()
  } // end of property IDiaSymbol4::ordinal
  .property uint32 frameSize()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CE 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_frameSize()
  } // end of property IDiaSymbol4::frameSize
  .property uint32 exceptionHandlerAddressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CF 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_exceptionHandlerAddressSection()
  } // end of property IDiaSymbol4::exceptionHandlerAddressSection
  .property uint32 exceptionHandlerAddressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D0 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_exceptionHandlerAddressOffset()
  } // end of property IDiaSymbol4::exceptionHandlerAddressOffset
  .property uint32 exceptionHandlerRelativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D1 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_exceptionHandlerRelativeVirtualAddress()
  } // end of property IDiaSymbol4::exceptionHandlerRelativeVirtualAddress
  .property uint64 exceptionHandlerVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D2 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol4::get_exceptionHandlerVirtualAddress()
  } // end of property IDiaSymbol4::exceptionHandlerVirtualAddress
  .property uint32 characteristics()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D3 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_characteristics()
  } // end of property IDiaSymbol4::characteristics
  .property class DIALib.IDiaSymbol coffGroup()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D4 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol4::get_coffGroup()
  } // end of property IDiaSymbol4::coffGroup
  .property uint32 bindID()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D5 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_bindID()
  } // end of property IDiaSymbol4::bindID
  .property uint32 bindSpace()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D6 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_bindSpace()
  } // end of property IDiaSymbol4::bindSpace
  .property uint32 bindSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D7 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_bindSlot()
  } // end of property IDiaSymbol4::bindSlot
  .property int32 isObjCClass()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D8 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isObjCClass()
  } // end of property IDiaSymbol4::isObjCClass
  .property int32 isObjCCategory()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isObjCCategory()
  } // end of property IDiaSymbol4::isObjCCategory
  .property int32 isObjCProtocol()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 DA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_isObjCProtocol()
  } // end of property IDiaSymbol4::isObjCProtocol
  .property class DIALib.IDiaSymbol inlinee()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 DB 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol4::get_inlinee()
  } // end of property IDiaSymbol4::inlinee
  .property uint32 inlineeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 DC 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol4::get_inlineeId()
  } // end of property IDiaSymbol4::inlineeId
  .property int32 noexcept()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 DD 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol4::get_noexcept()
  } // end of property IDiaSymbol4::noexcept
} // end of class DIALib.IDiaSymbol4

.class interface public abstract auto ansi import DIALib.IDiaSymbol5
       implements DIALib.IDiaSymbol4
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 41 42 45 32 44 45 30 30 2D 44 43 32 44   // ..$ABE2DE00-DC2D
                                                                                                  2D 34 37 39 33 2D 41 46 39 41 2D 45 46 31 44 39   // -4793-AF9A-EF1D9
                                                                                                  30 38 33 32 36 34 34 00 00 )                      // 0832644..
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 0A 73 79 6D 49 6E 64 65 78 49 64 00 00 )    // ...symIndexId..
  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_symIndexId() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_symIndexId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_symTag() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_symTag

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_name() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_name

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_lexicalParent() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_lexicalParent

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_classParent() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_classParent

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_type() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_type

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_dataKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_dataKind

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_locationType() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_locationType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_addressSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_addressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_addressOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_addressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_relativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_relativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_virtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_virtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_registerId() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_registerId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_offset() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_offset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_length() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_length

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_slot() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_slot

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_volatileType() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_volatileType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_constType() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_constType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_unalignedType() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_unalignedType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_access() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_access

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_libraryName() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_libraryName

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_platform() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_platform

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_language() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_language

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_editAndContinueEnabled() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_editAndContinueEnabled

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndMajor() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_frontEndMajor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndMinor() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_frontEndMinor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndBuild() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_frontEndBuild

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndMajor() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_backEndMajor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndMinor() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_backEndMinor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndBuild() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_backEndBuild

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_sourceFileName() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_sourceFileName

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_unused() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_unused

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_thunkOrdinal() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_thunkOrdinal

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_thisAdjust() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_thisAdjust

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_virtualBaseOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_virtualBaseOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_virtual() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_virtual

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_intro() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_intro

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_pure() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_pure

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_callingConvention() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_callingConvention

  .method public hidebysig newslot specialname abstract virtual 
          instance object 
          marshal( struct) 
          get_value() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_value

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseType() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_baseType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_token() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_token

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_timeStamp() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_timeStamp

  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype [mscorlib]System.Guid 
          get_guid() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_guid

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_symbolsFileName() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_symbolsFileName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_reference() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_reference

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_count() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_count

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bitPosition() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_bitPosition

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_arrayIndexType() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_arrayIndexType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_packed() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_packed

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_constructor() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_constructor

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_overloadedOperator() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_overloadedOperator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_nested() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_nested

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasNestedTypes() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_hasNestedTypes

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasAssignmentOperator() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_hasAssignmentOperator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasCastOperator() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_hasCastOperator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_scoped() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_scoped

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_virtualBaseClass() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_virtualBaseClass

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_indirectVirtualBaseClass() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_indirectVirtualBaseClass

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_virtualBasePointerOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_virtualBasePointerOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_virtualTableShape() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_virtualTableShape

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lexicalParentId() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_lexicalParentId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_classParentId() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_classParentId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_typeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_typeId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_arrayIndexTypeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_arrayIndexTypeId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_virtualTableShapeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_virtualTableShapeId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_code() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_code

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_function() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_function

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_managed() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_managed

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_msil() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_msil

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_virtualBaseDispIndex() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_virtualBaseDispIndex

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_undecoratedName() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_undecoratedName

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_age() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_age

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_signature() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_signature

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_compilerGenerated() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_compilerGenerated

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_addressTaken() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_addressTaken

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_rank() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_rank

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_lowerBound() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_lowerBound

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_upperBound() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_upperBound

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lowerBoundId() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_lowerBoundId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_upperBoundId() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_upperBoundId

  .method public hidebysig newslot abstract virtual 
          instance void  get_dataBytes([in] uint32 cbData,
                                       [out] uint32& pcbData,
                                       [out] uint8& pbData) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_dataBytes

  .method public hidebysig newslot abstract virtual 
          instance void  findChildren([in] valuetype DIALib.SymTagEnum symTag,
                                      [in] string  marshal( lpwstr) name,
                                      [in] uint32 compareFlags,
                                      [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::findChildren

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenEx([in] valuetype DIALib.SymTagEnum symTag,
                                        [in] string  marshal( lpwstr) name,
                                        [in] uint32 compareFlags,
                                        [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::findChildrenEx

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByAddr([in] valuetype DIALib.SymTagEnum symTag,
                                              [in] string  marshal( lpwstr) name,
                                              [in] uint32 compareFlags,
                                              [in] uint32 isect,
                                              [in] uint32 offset,
                                              [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::findChildrenExByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByVA([in] valuetype DIALib.SymTagEnum symTag,
                                            [in] string  marshal( lpwstr) name,
                                            [in] uint32 compareFlags,
                                            [in] uint64 va,
                                            [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::findChildrenExByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByRVA([in] valuetype DIALib.SymTagEnum symTag,
                                             [in] string  marshal( lpwstr) name,
                                             [in] uint32 compareFlags,
                                             [in] uint32 rva,
                                             [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::findChildrenExByRVA

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_targetSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_targetSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_targetOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_targetOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_targetRelativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_targetRelativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_targetVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_targetVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_machineType() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_machineType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_oemId() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_oemId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_oemSymbolId() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_oemSymbolId

  .method public hidebysig newslot abstract virtual 
          instance void  get_types([in] uint32 cTypes,
                                   [out] uint32& pcTypes,
                                   [out] class DIALib.IDiaSymbol&  marshal( interface ) pTypes) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_types

  .method public hidebysig newslot abstract virtual 
          instance void  get_typeIds([in] uint32 cTypeIds,
                                     [out] uint32& pcTypeIds,
                                     [out] uint32& pdwTypeIds) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_typeIds

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_objectPointerType() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_objectPointerType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_udtKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_udtKind

  .method public hidebysig newslot abstract virtual 
          instance void  get_undecoratedNameEx([in] uint32 undecorateOptions,
                                               [out] string&  marshal( bstr) name) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_undecoratedNameEx

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noReturn() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_noReturn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_customCallingConvention() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_customCallingConvention

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noInline() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_noInline

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_optimizedCodeDebugInfo() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_optimizedCodeDebugInfo

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_notReached() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_notReached

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_interruptReturn() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_interruptReturn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_farReturn() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_farReturn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isStatic() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isStatic

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasDebugInfo() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_hasDebugInfo

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isLTCG() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isLTCG

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isDataAligned() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isDataAligned

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasSecurityChecks() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_hasSecurityChecks

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_compilerName() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_compilerName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasAlloca() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_hasAlloca

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasSetJump() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_hasSetJump

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasLongJump() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_hasLongJump

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasInlAsm() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_hasInlAsm

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasEH() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_hasEH

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasSEH() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_hasSEH

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasEHa() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_hasEHa

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isNaked() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isNaked

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAggregated() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isAggregated

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSplitted() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isSplitted

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_container() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_container

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_inlSpec() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_inlSpec

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noStackOrdering() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_noStackOrdering

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_virtualBaseTableType() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_virtualBaseTableType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasManagedCode() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_hasManagedCode

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isHotpatchable() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isHotpatchable

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isCVTCIL() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isCVTCIL

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isMSILNetmodule() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isMSILNetmodule

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isCTypes() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isCTypes

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isStripped() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isStripped

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndQFE() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_frontEndQFE

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndQFE() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_backEndQFE

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_wasInlined() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_wasInlined

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_strictGSCheck() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_strictGSCheck

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isCxxReturnUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isCxxReturnUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isConstructorVirtualBase() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isConstructorVirtualBase

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_RValueReference() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_RValueReference

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_unmodifiedType() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_unmodifiedType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_framePointerPresent() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_framePointerPresent

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSafeBuffers() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isSafeBuffers

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_intrinsic() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_intrinsic

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_sealed() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_sealed

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hfaFloat() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_hfaFloat

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hfaDouble() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_hfaDouble

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_liveRangeStartAddressSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_liveRangeStartAddressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_liveRangeStartAddressOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_liveRangeStartAddressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_liveRangeStartRelativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_liveRangeStartRelativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_countLiveRanges() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_countLiveRanges

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_liveRangeLength() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_liveRangeLength

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_offsetInUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_offsetInUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_paramBasePointerRegisterId() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_paramBasePointerRegisterId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_localBasePointerRegisterId() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_localBasePointerRegisterId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isLocationControlFlowDependent() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isLocationControlFlowDependent

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_stride() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_stride

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfRows() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_numberOfRows

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfColumns() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_numberOfColumns

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isMatrixRowMajor() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isMatrixRowMajor

  .method public hidebysig newslot abstract virtual 
          instance void  get_numericProperties([in] uint32 cnt,
                                               [out] uint32& pcnt,
                                               [out] uint32& pProperties) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_numericProperties

  .method public hidebysig newslot abstract virtual 
          instance void  get_modifierValues([in] uint32 cnt,
                                            [out] uint32& pcnt,
                                            [out] uint16& pModifiers) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_modifierValues

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isReturnValue() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isReturnValue

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isOptimizedAway() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isOptimizedAway

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_builtInKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_builtInKind

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_registerType() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_registerType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseDataSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_baseDataSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseDataOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_baseDataOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_textureSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_textureSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_samplerSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_samplerSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_uavSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_uavSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_sizeInUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_sizeInUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_memorySpaceKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_memorySpaceKind

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_unmodifiedTypeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_unmodifiedTypeId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_subTypeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_subTypeId

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_subType() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_subType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfModifiers() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_numberOfModifiers

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfRegisterIndices() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_numberOfRegisterIndices

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isHLSLData() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isHLSLData

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPointerToDataMember() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isPointerToDataMember

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPointerToMemberFunction() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isPointerToMemberFunction

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSingleInheritance() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isSingleInheritance

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isMultipleInheritance() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isMultipleInheritance

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isVirtualInheritance() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isVirtualInheritance

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_restrictedType() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_restrictedType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPointerBasedOnSymbolValue() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isPointerBasedOnSymbolValue

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_baseSymbol() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_baseSymbol

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseSymbolId() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_baseSymbolId

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_objectFileName() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_objectFileName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAcceleratorGroupSharedLocal() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isAcceleratorGroupSharedLocal

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAcceleratorPointerTagLiveRange() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isAcceleratorPointerTagLiveRange

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAcceleratorStubFunction() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isAcceleratorStubFunction

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfAcceleratorPointerTags() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_numberOfAcceleratorPointerTags

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSdl() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isSdl

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isWinRTPointer() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isWinRTPointer

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isRefUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isRefUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isValueUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isValueUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isInterfaceUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isInterfaceUdt

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByAddr([in] uint32 isect,
                                                [in] uint32 offset,
                                                [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::findInlineFramesByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByRVA([in] uint32 rva,
                                               [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::findInlineFramesByRVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByVA([in] uint64 va,
                                              [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::findInlineFramesByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLines([out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::findInlineeLines

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByAddr([in] uint32 isect,
                                                [in] uint32 offset,
                                                [in] uint32 length,
                                                [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::findInlineeLinesByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByRVA([in] uint32 rva,
                                               [in] uint32 length,
                                               [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::findInlineeLinesByRVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByVA([in] uint64 va,
                                              [in] uint32 length,
                                              [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::findInlineeLinesByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findSymbolsForAcceleratorPointerTag([in] uint32 tagValue,
                                                             [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::findSymbolsForAcceleratorPointerTag

  .method public hidebysig newslot abstract virtual 
          instance void  findSymbolsByRVAForAcceleratorPointerTag([in] uint32 tagValue,
                                                                  [in] uint32 rva,
                                                                  [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::findSymbolsByRVAForAcceleratorPointerTag

  .method public hidebysig newslot abstract virtual 
          instance void  get_acceleratorPointerTags([in] uint32 cnt,
                                                    [out] uint32& pcnt,
                                                    [out] uint32& pPointerTags) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_acceleratorPointerTags

  .method public hidebysig newslot abstract virtual 
          instance void  getSrcLineOnTypeDefn([out] class DIALib.IDiaLineNumber&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::getSrcLineOnTypeDefn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPGO() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isPGO

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasValidPGOCounts() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_hasValidPGOCounts

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isOptimizedForSpeed() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isOptimizedForSpeed

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_PGOEntryCount() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_PGOEntryCount

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_PGOEdgeCount() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_PGOEdgeCount

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_PGODynamicInstructionCount() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_PGODynamicInstructionCount

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_staticSize() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_staticSize

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_finalLiveStaticSize() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_finalLiveStaticSize

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_phaseName() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_phaseName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasControlFlowCheck() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_hasControlFlowCheck

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_constantExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_constantExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_dataExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_dataExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_privateExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_privateExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noNameExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_noNameExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_exportHasExplicitlyAssignedOrdinal() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_exportHasExplicitlyAssignedOrdinal

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_exportIsForwarder() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_exportIsForwarder

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_ordinal() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_ordinal

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frameSize() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_frameSize

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_exceptionHandlerAddressSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_exceptionHandlerAddressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_exceptionHandlerAddressOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_exceptionHandlerAddressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_exceptionHandlerRelativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_exceptionHandlerRelativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_exceptionHandlerVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_exceptionHandlerVirtualAddress

  .method public hidebysig newslot abstract virtual 
          instance void  findInputAssemblyFile([out] class DIALib.IDiaInputAssemblyFile&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol5::findInputAssemblyFile

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_characteristics() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_characteristics

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_coffGroup() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_coffGroup

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bindID() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_bindID

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bindSpace() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_bindSpace

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bindSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_bindSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isObjCClass() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isObjCClass

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isObjCCategory() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isObjCCategory

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isObjCProtocol() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_isObjCProtocol

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_inlinee() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_inlinee

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_inlineeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_inlineeId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noexcept() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_noexcept

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasAbsoluteAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol5::get_hasAbsoluteAddress

  .property uint32 symIndexId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_symIndexId()
  } // end of property IDiaSymbol5::symIndexId
  .property uint32 symTag()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_symTag()
  } // end of property IDiaSymbol5::symTag
  .property string name()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol5::get_name()
  } // end of property IDiaSymbol5::name
  .property class DIALib.IDiaSymbol lexicalParent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol5::get_lexicalParent()
  } // end of property IDiaSymbol5::lexicalParent
  .property class DIALib.IDiaSymbol classParent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 04 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol5::get_classParent()
  } // end of property IDiaSymbol5::classParent
  .property class DIALib.IDiaSymbol 'type'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 05 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol5::get_type()
  } // end of property IDiaSymbol5::'type'
  .property uint32 dataKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 06 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_dataKind()
  } // end of property IDiaSymbol5::dataKind
  .property uint32 locationType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 07 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_locationType()
  } // end of property IDiaSymbol5::locationType
  .property uint32 addressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_addressSection()
  } // end of property IDiaSymbol5::addressSection
  .property uint32 addressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 09 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_addressOffset()
  } // end of property IDiaSymbol5::addressOffset
  .property uint32 relativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0A 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_relativeVirtualAddress()
  } // end of property IDiaSymbol5::relativeVirtualAddress
  .property uint64 virtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0B 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol5::get_virtualAddress()
  } // end of property IDiaSymbol5::virtualAddress
  .property uint32 registerId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_registerId()
  } // end of property IDiaSymbol5::registerId
  .property int32 offset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0D 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_offset()
  } // end of property IDiaSymbol5::offset
  .property uint64 length()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0E 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol5::get_length()
  } // end of property IDiaSymbol5::length
  .property uint32 slot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_slot()
  } // end of property IDiaSymbol5::slot
  .property int32 volatileType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 10 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_volatileType()
  } // end of property IDiaSymbol5::volatileType
  .property int32 constType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 11 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_constType()
  } // end of property IDiaSymbol5::constType
  .property int32 unalignedType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 12 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_unalignedType()
  } // end of property IDiaSymbol5::unalignedType
  .property uint32 access()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 13 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_access()
  } // end of property IDiaSymbol5::access
  .property string libraryName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 14 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol5::get_libraryName()
  } // end of property IDiaSymbol5::libraryName
  .property uint32 platform()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 15 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_platform()
  } // end of property IDiaSymbol5::platform
  .property uint32 language()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 16 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_language()
  } // end of property IDiaSymbol5::language
  .property int32 editAndContinueEnabled()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 17 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_editAndContinueEnabled()
  } // end of property IDiaSymbol5::editAndContinueEnabled
  .property uint32 frontEndMajor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 18 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_frontEndMajor()
  } // end of property IDiaSymbol5::frontEndMajor
  .property uint32 frontEndMinor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 19 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_frontEndMinor()
  } // end of property IDiaSymbol5::frontEndMinor
  .property uint32 frontEndBuild()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1A 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_frontEndBuild()
  } // end of property IDiaSymbol5::frontEndBuild
  .property uint32 backEndMajor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1B 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_backEndMajor()
  } // end of property IDiaSymbol5::backEndMajor
  .property uint32 backEndMinor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_backEndMinor()
  } // end of property IDiaSymbol5::backEndMinor
  .property uint32 backEndBuild()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1D 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_backEndBuild()
  } // end of property IDiaSymbol5::backEndBuild
  .property string sourceFileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1E 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol5::get_sourceFileName()
  } // end of property IDiaSymbol5::sourceFileName
  .property string 'unused'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1F 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol5::get_unused()
  } // end of property IDiaSymbol5::'unused'
  .property uint32 thunkOrdinal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 20 00 00 00 00 00 )                         // .. .....
    .get instance uint32 DIALib.IDiaSymbol5::get_thunkOrdinal()
  } // end of property IDiaSymbol5::thunkOrdinal
  .property int32 thisAdjust()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 21 00 00 00 00 00 )                         // ..!.....
    .get instance int32 DIALib.IDiaSymbol5::get_thisAdjust()
  } // end of property IDiaSymbol5::thisAdjust
  .property uint32 virtualBaseOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 22 00 00 00 00 00 )                         // ..".....
    .get instance uint32 DIALib.IDiaSymbol5::get_virtualBaseOffset()
  } // end of property IDiaSymbol5::virtualBaseOffset
  .property int32 'virtual'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 23 00 00 00 00 00 )                         // ..#.....
    .get instance int32 DIALib.IDiaSymbol5::get_virtual()
  } // end of property IDiaSymbol5::'virtual'
  .property int32 intro()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 24 00 00 00 00 00 )                         // ..$.....
    .get instance int32 DIALib.IDiaSymbol5::get_intro()
  } // end of property IDiaSymbol5::intro
  .property int32 pure()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 25 00 00 00 00 00 )                         // ..%.....
    .get instance int32 DIALib.IDiaSymbol5::get_pure()
  } // end of property IDiaSymbol5::pure
  .property uint32 callingConvention()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 26 00 00 00 00 00 )                         // ..&.....
    .get instance uint32 DIALib.IDiaSymbol5::get_callingConvention()
  } // end of property IDiaSymbol5::callingConvention
  .property object 'value'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 27 00 00 00 00 00 )                         // ..'.....
    .get instance object DIALib.IDiaSymbol5::get_value()
  } // end of property IDiaSymbol5::'value'
  .property uint32 baseType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 28 00 00 00 00 00 )                         // ..(.....
    .get instance uint32 DIALib.IDiaSymbol5::get_baseType()
  } // end of property IDiaSymbol5::baseType
  .property uint32 token()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 29 00 00 00 00 00 )                         // ..).....
    .get instance uint32 DIALib.IDiaSymbol5::get_token()
  } // end of property IDiaSymbol5::token
  .property uint32 timeStamp()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2A 00 00 00 00 00 )                         // ..*.....
    .get instance uint32 DIALib.IDiaSymbol5::get_timeStamp()
  } // end of property IDiaSymbol5::timeStamp
  .property valuetype [mscorlib]System.Guid
          guid()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2B 00 00 00 00 00 )                         // ..+.....
    .get instance valuetype [mscorlib]System.Guid DIALib.IDiaSymbol5::get_guid()
  } // end of property IDiaSymbol5::guid
  .property string symbolsFileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2C 00 00 00 00 00 )                         // ..,.....
    .get instance string DIALib.IDiaSymbol5::get_symbolsFileName()
  } // end of property IDiaSymbol5::symbolsFileName
  .property int32 reference()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2E 00 00 00 00 00 )                         // ........
    .get instance int32 DIALib.IDiaSymbol5::get_reference()
  } // end of property IDiaSymbol5::reference
  .property uint32 count()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2F 00 00 00 00 00 )                         // ../.....
    .get instance uint32 DIALib.IDiaSymbol5::get_count()
  } // end of property IDiaSymbol5::count
  .property uint32 bitPosition()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 31 00 00 00 00 00 )                         // ..1.....
    .get instance uint32 DIALib.IDiaSymbol5::get_bitPosition()
  } // end of property IDiaSymbol5::bitPosition
  .property class DIALib.IDiaSymbol arrayIndexType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 32 00 00 00 00 00 )                         // ..2.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol5::get_arrayIndexType()
  } // end of property IDiaSymbol5::arrayIndexType
  .property int32 packed()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 33 00 00 00 00 00 )                         // ..3.....
    .get instance int32 DIALib.IDiaSymbol5::get_packed()
  } // end of property IDiaSymbol5::packed
  .property int32 constructor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 34 00 00 00 00 00 )                         // ..4.....
    .get instance int32 DIALib.IDiaSymbol5::get_constructor()
  } // end of property IDiaSymbol5::constructor
  .property int32 overloadedOperator()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 35 00 00 00 00 00 )                         // ..5.....
    .get instance int32 DIALib.IDiaSymbol5::get_overloadedOperator()
  } // end of property IDiaSymbol5::overloadedOperator
  .property int32 'nested'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 36 00 00 00 00 00 )                         // ..6.....
    .get instance int32 DIALib.IDiaSymbol5::get_nested()
  } // end of property IDiaSymbol5::'nested'
  .property int32 hasNestedTypes()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 37 00 00 00 00 00 )                         // ..7.....
    .get instance int32 DIALib.IDiaSymbol5::get_hasNestedTypes()
  } // end of property IDiaSymbol5::hasNestedTypes
  .property int32 hasAssignmentOperator()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 38 00 00 00 00 00 )                         // ..8.....
    .get instance int32 DIALib.IDiaSymbol5::get_hasAssignmentOperator()
  } // end of property IDiaSymbol5::hasAssignmentOperator
  .property int32 hasCastOperator()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 39 00 00 00 00 00 )                         // ..9.....
    .get instance int32 DIALib.IDiaSymbol5::get_hasCastOperator()
  } // end of property IDiaSymbol5::hasCastOperator
  .property int32 scoped()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3A 00 00 00 00 00 )                         // ..:.....
    .get instance int32 DIALib.IDiaSymbol5::get_scoped()
  } // end of property IDiaSymbol5::scoped
  .property int32 virtualBaseClass()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3B 00 00 00 00 00 )                         // ..;.....
    .get instance int32 DIALib.IDiaSymbol5::get_virtualBaseClass()
  } // end of property IDiaSymbol5::virtualBaseClass
  .property int32 indirectVirtualBaseClass()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3C 00 00 00 00 00 )                         // ..<.....
    .get instance int32 DIALib.IDiaSymbol5::get_indirectVirtualBaseClass()
  } // end of property IDiaSymbol5::indirectVirtualBaseClass
  .property int32 virtualBasePointerOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3D 00 00 00 00 00 )                         // ..=.....
    .get instance int32 DIALib.IDiaSymbol5::get_virtualBasePointerOffset()
  } // end of property IDiaSymbol5::virtualBasePointerOffset
  .property class DIALib.IDiaSymbol virtualTableShape()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3E 00 00 00 00 00 )                         // ..>.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol5::get_virtualTableShape()
  } // end of property IDiaSymbol5::virtualTableShape
  .property uint32 lexicalParentId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 40 00 00 00 00 00 )                         // ..@.....
    .get instance uint32 DIALib.IDiaSymbol5::get_lexicalParentId()
  } // end of property IDiaSymbol5::lexicalParentId
  .property uint32 classParentId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 41 00 00 00 00 00 )                         // ..A.....
    .get instance uint32 DIALib.IDiaSymbol5::get_classParentId()
  } // end of property IDiaSymbol5::classParentId
  .property uint32 typeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 42 00 00 00 00 00 )                         // ..B.....
    .get instance uint32 DIALib.IDiaSymbol5::get_typeId()
  } // end of property IDiaSymbol5::typeId
  .property uint32 arrayIndexTypeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 43 00 00 00 00 00 )                         // ..C.....
    .get instance uint32 DIALib.IDiaSymbol5::get_arrayIndexTypeId()
  } // end of property IDiaSymbol5::arrayIndexTypeId
  .property uint32 virtualTableShapeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 44 00 00 00 00 00 )                         // ..D.....
    .get instance uint32 DIALib.IDiaSymbol5::get_virtualTableShapeId()
  } // end of property IDiaSymbol5::virtualTableShapeId
  .property int32 code()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 45 00 00 00 00 00 )                         // ..E.....
    .get instance int32 DIALib.IDiaSymbol5::get_code()
  } // end of property IDiaSymbol5::code
  .property int32 function()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 46 00 00 00 00 00 )                         // ..F.....
    .get instance int32 DIALib.IDiaSymbol5::get_function()
  } // end of property IDiaSymbol5::function
  .property int32 'managed'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 47 00 00 00 00 00 )                         // ..G.....
    .get instance int32 DIALib.IDiaSymbol5::get_managed()
  } // end of property IDiaSymbol5::'managed'
  .property int32 msil()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 48 00 00 00 00 00 )                         // ..H.....
    .get instance int32 DIALib.IDiaSymbol5::get_msil()
  } // end of property IDiaSymbol5::msil
  .property uint32 virtualBaseDispIndex()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 49 00 00 00 00 00 )                         // ..I.....
    .get instance uint32 DIALib.IDiaSymbol5::get_virtualBaseDispIndex()
  } // end of property IDiaSymbol5::virtualBaseDispIndex
  .property string undecoratedName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4A 00 00 00 00 00 )                         // ..J.....
    .get instance string DIALib.IDiaSymbol5::get_undecoratedName()
  } // end of property IDiaSymbol5::undecoratedName
  .property uint32 age()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4B 00 00 00 00 00 )                         // ..K.....
    .get instance uint32 DIALib.IDiaSymbol5::get_age()
  } // end of property IDiaSymbol5::age
  .property uint32 signature()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4C 00 00 00 00 00 )                         // ..L.....
    .get instance uint32 DIALib.IDiaSymbol5::get_signature()
  } // end of property IDiaSymbol5::signature
  .property int32 compilerGenerated()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4D 00 00 00 00 00 )                         // ..M.....
    .get instance int32 DIALib.IDiaSymbol5::get_compilerGenerated()
  } // end of property IDiaSymbol5::compilerGenerated
  .property int32 addressTaken()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4E 00 00 00 00 00 )                         // ..N.....
    .get instance int32 DIALib.IDiaSymbol5::get_addressTaken()
  } // end of property IDiaSymbol5::addressTaken
  .property uint32 rank()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4F 00 00 00 00 00 )                         // ..O.....
    .get instance uint32 DIALib.IDiaSymbol5::get_rank()
  } // end of property IDiaSymbol5::rank
  .property class DIALib.IDiaSymbol lowerBound()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 50 00 00 00 00 00 )                         // ..P.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol5::get_lowerBound()
  } // end of property IDiaSymbol5::lowerBound
  .property class DIALib.IDiaSymbol upperBound()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 51 00 00 00 00 00 )                         // ..Q.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol5::get_upperBound()
  } // end of property IDiaSymbol5::upperBound
  .property uint32 lowerBoundId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 52 00 00 00 00 00 )                         // ..R.....
    .get instance uint32 DIALib.IDiaSymbol5::get_lowerBoundId()
  } // end of property IDiaSymbol5::lowerBoundId
  .property uint32 upperBoundId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 53 00 00 00 00 00 )                         // ..S.....
    .get instance uint32 DIALib.IDiaSymbol5::get_upperBoundId()
  } // end of property IDiaSymbol5::upperBoundId
  .property uint32 targetSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 54 00 00 00 00 00 )                         // ..T.....
    .get instance uint32 DIALib.IDiaSymbol5::get_targetSection()
  } // end of property IDiaSymbol5::targetSection
  .property uint32 targetOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 55 00 00 00 00 00 )                         // ..U.....
    .get instance uint32 DIALib.IDiaSymbol5::get_targetOffset()
  } // end of property IDiaSymbol5::targetOffset
  .property uint32 targetRelativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 56 00 00 00 00 00 )                         // ..V.....
    .get instance uint32 DIALib.IDiaSymbol5::get_targetRelativeVirtualAddress()
  } // end of property IDiaSymbol5::targetRelativeVirtualAddress
  .property uint64 targetVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 57 00 00 00 00 00 )                         // ..W.....
    .get instance uint64 DIALib.IDiaSymbol5::get_targetVirtualAddress()
  } // end of property IDiaSymbol5::targetVirtualAddress
  .property uint32 machineType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 58 00 00 00 00 00 )                         // ..X.....
    .get instance uint32 DIALib.IDiaSymbol5::get_machineType()
  } // end of property IDiaSymbol5::machineType
  .property uint32 oemId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 59 00 00 00 00 00 )                         // ..Y.....
    .get instance uint32 DIALib.IDiaSymbol5::get_oemId()
  } // end of property IDiaSymbol5::oemId
  .property uint32 oemSymbolId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5A 00 00 00 00 00 )                         // ..Z.....
    .get instance uint32 DIALib.IDiaSymbol5::get_oemSymbolId()
  } // end of property IDiaSymbol5::oemSymbolId
  .property class DIALib.IDiaSymbol objectPointerType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5B 00 00 00 00 00 )                         // ..[.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol5::get_objectPointerType()
  } // end of property IDiaSymbol5::objectPointerType
  .property uint32 udtKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5C 00 00 00 00 00 )                         // ..\.....
    .get instance uint32 DIALib.IDiaSymbol5::get_udtKind()
  } // end of property IDiaSymbol5::udtKind
  .property int32 noReturn()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5D 00 00 00 00 00 )                         // ..].....
    .get instance int32 DIALib.IDiaSymbol5::get_noReturn()
  } // end of property IDiaSymbol5::noReturn
  .property int32 customCallingConvention()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5E 00 00 00 00 00 )                         // ..^.....
    .get instance int32 DIALib.IDiaSymbol5::get_customCallingConvention()
  } // end of property IDiaSymbol5::customCallingConvention
  .property int32 noInline()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5F 00 00 00 00 00 )                         // .._.....
    .get instance int32 DIALib.IDiaSymbol5::get_noInline()
  } // end of property IDiaSymbol5::noInline
  .property int32 optimizedCodeDebugInfo()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 60 00 00 00 00 00 )                         // ..`.....
    .get instance int32 DIALib.IDiaSymbol5::get_optimizedCodeDebugInfo()
  } // end of property IDiaSymbol5::optimizedCodeDebugInfo
  .property int32 notReached()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 61 00 00 00 00 00 )                         // ..a.....
    .get instance int32 DIALib.IDiaSymbol5::get_notReached()
  } // end of property IDiaSymbol5::notReached
  .property int32 interruptReturn()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 62 00 00 00 00 00 )                         // ..b.....
    .get instance int32 DIALib.IDiaSymbol5::get_interruptReturn()
  } // end of property IDiaSymbol5::interruptReturn
  .property int32 farReturn()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 63 00 00 00 00 00 )                         // ..c.....
    .get instance int32 DIALib.IDiaSymbol5::get_farReturn()
  } // end of property IDiaSymbol5::farReturn
  .property int32 isStatic()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 64 00 00 00 00 00 )                         // ..d.....
    .get instance int32 DIALib.IDiaSymbol5::get_isStatic()
  } // end of property IDiaSymbol5::isStatic
  .property int32 hasDebugInfo()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 65 00 00 00 00 00 )                         // ..e.....
    .get instance int32 DIALib.IDiaSymbol5::get_hasDebugInfo()
  } // end of property IDiaSymbol5::hasDebugInfo
  .property int32 isLTCG()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 66 00 00 00 00 00 )                         // ..f.....
    .get instance int32 DIALib.IDiaSymbol5::get_isLTCG()
  } // end of property IDiaSymbol5::isLTCG
  .property int32 isDataAligned()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 67 00 00 00 00 00 )                         // ..g.....
    .get instance int32 DIALib.IDiaSymbol5::get_isDataAligned()
  } // end of property IDiaSymbol5::isDataAligned
  .property int32 hasSecurityChecks()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 68 00 00 00 00 00 )                         // ..h.....
    .get instance int32 DIALib.IDiaSymbol5::get_hasSecurityChecks()
  } // end of property IDiaSymbol5::hasSecurityChecks
  .property string compilerName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 69 00 00 00 00 00 )                         // ..i.....
    .get instance string DIALib.IDiaSymbol5::get_compilerName()
  } // end of property IDiaSymbol5::compilerName
  .property int32 hasAlloca()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6A 00 00 00 00 00 )                         // ..j.....
    .get instance int32 DIALib.IDiaSymbol5::get_hasAlloca()
  } // end of property IDiaSymbol5::hasAlloca
  .property int32 hasSetJump()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6B 00 00 00 00 00 )                         // ..k.....
    .get instance int32 DIALib.IDiaSymbol5::get_hasSetJump()
  } // end of property IDiaSymbol5::hasSetJump
  .property int32 hasLongJump()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6C 00 00 00 00 00 )                         // ..l.....
    .get instance int32 DIALib.IDiaSymbol5::get_hasLongJump()
  } // end of property IDiaSymbol5::hasLongJump
  .property int32 hasInlAsm()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6D 00 00 00 00 00 )                         // ..m.....
    .get instance int32 DIALib.IDiaSymbol5::get_hasInlAsm()
  } // end of property IDiaSymbol5::hasInlAsm
  .property int32 hasEH()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6E 00 00 00 00 00 )                         // ..n.....
    .get instance int32 DIALib.IDiaSymbol5::get_hasEH()
  } // end of property IDiaSymbol5::hasEH
  .property int32 hasSEH()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6F 00 00 00 00 00 )                         // ..o.....
    .get instance int32 DIALib.IDiaSymbol5::get_hasSEH()
  } // end of property IDiaSymbol5::hasSEH
  .property int32 hasEHa()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 70 00 00 00 00 00 )                         // ..p.....
    .get instance int32 DIALib.IDiaSymbol5::get_hasEHa()
  } // end of property IDiaSymbol5::hasEHa
  .property int32 isNaked()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 71 00 00 00 00 00 )                         // ..q.....
    .get instance int32 DIALib.IDiaSymbol5::get_isNaked()
  } // end of property IDiaSymbol5::isNaked
  .property int32 isAggregated()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 72 00 00 00 00 00 )                         // ..r.....
    .get instance int32 DIALib.IDiaSymbol5::get_isAggregated()
  } // end of property IDiaSymbol5::isAggregated
  .property int32 isSplitted()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 73 00 00 00 00 00 )                         // ..s.....
    .get instance int32 DIALib.IDiaSymbol5::get_isSplitted()
  } // end of property IDiaSymbol5::isSplitted
  .property class DIALib.IDiaSymbol container()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 74 00 00 00 00 00 )                         // ..t.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol5::get_container()
  } // end of property IDiaSymbol5::container
  .property int32 inlSpec()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 75 00 00 00 00 00 )                         // ..u.....
    .get instance int32 DIALib.IDiaSymbol5::get_inlSpec()
  } // end of property IDiaSymbol5::inlSpec
  .property int32 noStackOrdering()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 76 00 00 00 00 00 )                         // ..v.....
    .get instance int32 DIALib.IDiaSymbol5::get_noStackOrdering()
  } // end of property IDiaSymbol5::noStackOrdering
  .property class DIALib.IDiaSymbol virtualBaseTableType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 77 00 00 00 00 00 )                         // ..w.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol5::get_virtualBaseTableType()
  } // end of property IDiaSymbol5::virtualBaseTableType
  .property int32 hasManagedCode()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 78 00 00 00 00 00 )                         // ..x.....
    .get instance int32 DIALib.IDiaSymbol5::get_hasManagedCode()
  } // end of property IDiaSymbol5::hasManagedCode
  .property int32 isHotpatchable()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 79 00 00 00 00 00 )                         // ..y.....
    .get instance int32 DIALib.IDiaSymbol5::get_isHotpatchable()
  } // end of property IDiaSymbol5::isHotpatchable
  .property int32 isCVTCIL()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7A 00 00 00 00 00 )                         // ..z.....
    .get instance int32 DIALib.IDiaSymbol5::get_isCVTCIL()
  } // end of property IDiaSymbol5::isCVTCIL
  .property int32 isMSILNetmodule()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7B 00 00 00 00 00 )                         // ..{.....
    .get instance int32 DIALib.IDiaSymbol5::get_isMSILNetmodule()
  } // end of property IDiaSymbol5::isMSILNetmodule
  .property int32 isCTypes()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7C 00 00 00 00 00 )                         // ..|.....
    .get instance int32 DIALib.IDiaSymbol5::get_isCTypes()
  } // end of property IDiaSymbol5::isCTypes
  .property int32 isStripped()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7D 00 00 00 00 00 )                         // ..}.....
    .get instance int32 DIALib.IDiaSymbol5::get_isStripped()
  } // end of property IDiaSymbol5::isStripped
  .property uint32 frontEndQFE()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7E 00 00 00 00 00 )                         // ..~.....
    .get instance uint32 DIALib.IDiaSymbol5::get_frontEndQFE()
  } // end of property IDiaSymbol5::frontEndQFE
  .property uint32 backEndQFE()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_backEndQFE()
  } // end of property IDiaSymbol5::backEndQFE
  .property int32 wasInlined()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 80 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_wasInlined()
  } // end of property IDiaSymbol5::wasInlined
  .property int32 strictGSCheck()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 81 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_strictGSCheck()
  } // end of property IDiaSymbol5::strictGSCheck
  .property int32 isCxxReturnUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 82 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isCxxReturnUdt()
  } // end of property IDiaSymbol5::isCxxReturnUdt
  .property int32 isConstructorVirtualBase()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 83 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isConstructorVirtualBase()
  } // end of property IDiaSymbol5::isConstructorVirtualBase
  .property int32 RValueReference()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 84 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_RValueReference()
  } // end of property IDiaSymbol5::RValueReference
  .property class DIALib.IDiaSymbol unmodifiedType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 85 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol5::get_unmodifiedType()
  } // end of property IDiaSymbol5::unmodifiedType
  .property int32 framePointerPresent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 86 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_framePointerPresent()
  } // end of property IDiaSymbol5::framePointerPresent
  .property int32 isSafeBuffers()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 87 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isSafeBuffers()
  } // end of property IDiaSymbol5::isSafeBuffers
  .property int32 intrinsic()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 88 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_intrinsic()
  } // end of property IDiaSymbol5::intrinsic
  .property int32 'sealed'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 89 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_sealed()
  } // end of property IDiaSymbol5::'sealed'
  .property int32 hfaFloat()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8A 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_hfaFloat()
  } // end of property IDiaSymbol5::hfaFloat
  .property int32 hfaDouble()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8B 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_hfaDouble()
  } // end of property IDiaSymbol5::hfaDouble
  .property uint32 liveRangeStartAddressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_liveRangeStartAddressSection()
  } // end of property IDiaSymbol5::liveRangeStartAddressSection
  .property uint32 liveRangeStartAddressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8D 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_liveRangeStartAddressOffset()
  } // end of property IDiaSymbol5::liveRangeStartAddressOffset
  .property uint32 liveRangeStartRelativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8E 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_liveRangeStartRelativeVirtualAddress()
  } // end of property IDiaSymbol5::liveRangeStartRelativeVirtualAddress
  .property uint32 countLiveRanges()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_countLiveRanges()
  } // end of property IDiaSymbol5::countLiveRanges
  .property uint64 liveRangeLength()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 90 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol5::get_liveRangeLength()
  } // end of property IDiaSymbol5::liveRangeLength
  .property uint32 offsetInUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 91 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_offsetInUdt()
  } // end of property IDiaSymbol5::offsetInUdt
  .property uint32 paramBasePointerRegisterId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 92 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_paramBasePointerRegisterId()
  } // end of property IDiaSymbol5::paramBasePointerRegisterId
  .property uint32 localBasePointerRegisterId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 93 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_localBasePointerRegisterId()
  } // end of property IDiaSymbol5::localBasePointerRegisterId
  .property int32 isLocationControlFlowDependent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 94 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isLocationControlFlowDependent()
  } // end of property IDiaSymbol5::isLocationControlFlowDependent
  .property uint32 stride()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 95 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_stride()
  } // end of property IDiaSymbol5::stride
  .property uint32 numberOfRows()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 96 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_numberOfRows()
  } // end of property IDiaSymbol5::numberOfRows
  .property uint32 numberOfColumns()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 97 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_numberOfColumns()
  } // end of property IDiaSymbol5::numberOfColumns
  .property int32 isMatrixRowMajor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 98 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isMatrixRowMajor()
  } // end of property IDiaSymbol5::isMatrixRowMajor
  .property int32 isReturnValue()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 99 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isReturnValue()
  } // end of property IDiaSymbol5::isReturnValue
  .property int32 isOptimizedAway()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9A 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isOptimizedAway()
  } // end of property IDiaSymbol5::isOptimizedAway
  .property uint32 builtInKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9B 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_builtInKind()
  } // end of property IDiaSymbol5::builtInKind
  .property uint32 registerType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_registerType()
  } // end of property IDiaSymbol5::registerType
  .property uint32 baseDataSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9D 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_baseDataSlot()
  } // end of property IDiaSymbol5::baseDataSlot
  .property uint32 baseDataOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9E 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_baseDataOffset()
  } // end of property IDiaSymbol5::baseDataOffset
  .property uint32 textureSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_textureSlot()
  } // end of property IDiaSymbol5::textureSlot
  .property uint32 samplerSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A0 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_samplerSlot()
  } // end of property IDiaSymbol5::samplerSlot
  .property uint32 uavSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A1 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_uavSlot()
  } // end of property IDiaSymbol5::uavSlot
  .property uint32 sizeInUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A2 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_sizeInUdt()
  } // end of property IDiaSymbol5::sizeInUdt
  .property uint32 memorySpaceKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A3 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_memorySpaceKind()
  } // end of property IDiaSymbol5::memorySpaceKind
  .property uint32 unmodifiedTypeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A4 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_unmodifiedTypeId()
  } // end of property IDiaSymbol5::unmodifiedTypeId
  .property uint32 subTypeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A5 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_subTypeId()
  } // end of property IDiaSymbol5::subTypeId
  .property class DIALib.IDiaSymbol subType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A6 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol5::get_subType()
  } // end of property IDiaSymbol5::subType
  .property uint32 numberOfModifiers()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A7 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_numberOfModifiers()
  } // end of property IDiaSymbol5::numberOfModifiers
  .property uint32 numberOfRegisterIndices()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A8 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_numberOfRegisterIndices()
  } // end of property IDiaSymbol5::numberOfRegisterIndices
  .property int32 isHLSLData()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isHLSLData()
  } // end of property IDiaSymbol5::isHLSLData
  .property int32 isPointerToDataMember()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isPointerToDataMember()
  } // end of property IDiaSymbol5::isPointerToDataMember
  .property int32 isPointerToMemberFunction()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AB 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isPointerToMemberFunction()
  } // end of property IDiaSymbol5::isPointerToMemberFunction
  .property int32 isSingleInheritance()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AC 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isSingleInheritance()
  } // end of property IDiaSymbol5::isSingleInheritance
  .property int32 isMultipleInheritance()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AD 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isMultipleInheritance()
  } // end of property IDiaSymbol5::isMultipleInheritance
  .property int32 isVirtualInheritance()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AE 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isVirtualInheritance()
  } // end of property IDiaSymbol5::isVirtualInheritance
  .property int32 restrictedType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AF 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_restrictedType()
  } // end of property IDiaSymbol5::restrictedType
  .property int32 isPointerBasedOnSymbolValue()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B0 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isPointerBasedOnSymbolValue()
  } // end of property IDiaSymbol5::isPointerBasedOnSymbolValue
  .property class DIALib.IDiaSymbol baseSymbol()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B1 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol5::get_baseSymbol()
  } // end of property IDiaSymbol5::baseSymbol
  .property uint32 baseSymbolId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B2 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_baseSymbolId()
  } // end of property IDiaSymbol5::baseSymbolId
  .property string objectFileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B3 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol5::get_objectFileName()
  } // end of property IDiaSymbol5::objectFileName
  .property int32 isAcceleratorGroupSharedLocal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B4 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isAcceleratorGroupSharedLocal()
  } // end of property IDiaSymbol5::isAcceleratorGroupSharedLocal
  .property int32 isAcceleratorPointerTagLiveRange()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B5 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isAcceleratorPointerTagLiveRange()
  } // end of property IDiaSymbol5::isAcceleratorPointerTagLiveRange
  .property int32 isAcceleratorStubFunction()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B6 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isAcceleratorStubFunction()
  } // end of property IDiaSymbol5::isAcceleratorStubFunction
  .property uint32 numberOfAcceleratorPointerTags()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B7 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_numberOfAcceleratorPointerTags()
  } // end of property IDiaSymbol5::numberOfAcceleratorPointerTags
  .property int32 isSdl()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B8 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isSdl()
  } // end of property IDiaSymbol5::isSdl
  .property int32 isWinRTPointer()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isWinRTPointer()
  } // end of property IDiaSymbol5::isWinRTPointer
  .property int32 isRefUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isRefUdt()
  } // end of property IDiaSymbol5::isRefUdt
  .property int32 isValueUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BB 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isValueUdt()
  } // end of property IDiaSymbol5::isValueUdt
  .property int32 isInterfaceUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BC 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isInterfaceUdt()
  } // end of property IDiaSymbol5::isInterfaceUdt
  .property int32 isPGO()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BD 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isPGO()
  } // end of property IDiaSymbol5::isPGO
  .property int32 hasValidPGOCounts()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BE 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_hasValidPGOCounts()
  } // end of property IDiaSymbol5::hasValidPGOCounts
  .property int32 isOptimizedForSpeed()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BF 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isOptimizedForSpeed()
  } // end of property IDiaSymbol5::isOptimizedForSpeed
  .property uint32 PGOEntryCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C0 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_PGOEntryCount()
  } // end of property IDiaSymbol5::PGOEntryCount
  .property uint32 PGOEdgeCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C1 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_PGOEdgeCount()
  } // end of property IDiaSymbol5::PGOEdgeCount
  .property uint64 PGODynamicInstructionCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C2 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol5::get_PGODynamicInstructionCount()
  } // end of property IDiaSymbol5::PGODynamicInstructionCount
  .property uint32 staticSize()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C3 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_staticSize()
  } // end of property IDiaSymbol5::staticSize
  .property uint32 finalLiveStaticSize()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C4 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_finalLiveStaticSize()
  } // end of property IDiaSymbol5::finalLiveStaticSize
  .property string phaseName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C5 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol5::get_phaseName()
  } // end of property IDiaSymbol5::phaseName
  .property int32 hasControlFlowCheck()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C6 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_hasControlFlowCheck()
  } // end of property IDiaSymbol5::hasControlFlowCheck
  .property int32 constantExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C7 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_constantExport()
  } // end of property IDiaSymbol5::constantExport
  .property int32 dataExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C8 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_dataExport()
  } // end of property IDiaSymbol5::dataExport
  .property int32 privateExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_privateExport()
  } // end of property IDiaSymbol5::privateExport
  .property int32 noNameExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_noNameExport()
  } // end of property IDiaSymbol5::noNameExport
  .property int32 exportHasExplicitlyAssignedOrdinal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CB 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_exportHasExplicitlyAssignedOrdinal()
  } // end of property IDiaSymbol5::exportHasExplicitlyAssignedOrdinal
  .property int32 exportIsForwarder()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CC 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_exportIsForwarder()
  } // end of property IDiaSymbol5::exportIsForwarder
  .property uint32 ordinal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CD 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_ordinal()
  } // end of property IDiaSymbol5::ordinal
  .property uint32 frameSize()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CE 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_frameSize()
  } // end of property IDiaSymbol5::frameSize
  .property uint32 exceptionHandlerAddressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CF 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_exceptionHandlerAddressSection()
  } // end of property IDiaSymbol5::exceptionHandlerAddressSection
  .property uint32 exceptionHandlerAddressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D0 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_exceptionHandlerAddressOffset()
  } // end of property IDiaSymbol5::exceptionHandlerAddressOffset
  .property uint32 exceptionHandlerRelativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D1 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_exceptionHandlerRelativeVirtualAddress()
  } // end of property IDiaSymbol5::exceptionHandlerRelativeVirtualAddress
  .property uint64 exceptionHandlerVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D2 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol5::get_exceptionHandlerVirtualAddress()
  } // end of property IDiaSymbol5::exceptionHandlerVirtualAddress
  .property uint32 characteristics()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D3 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_characteristics()
  } // end of property IDiaSymbol5::characteristics
  .property class DIALib.IDiaSymbol coffGroup()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D4 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol5::get_coffGroup()
  } // end of property IDiaSymbol5::coffGroup
  .property uint32 bindID()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D5 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_bindID()
  } // end of property IDiaSymbol5::bindID
  .property uint32 bindSpace()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D6 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_bindSpace()
  } // end of property IDiaSymbol5::bindSpace
  .property uint32 bindSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D7 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_bindSlot()
  } // end of property IDiaSymbol5::bindSlot
  .property int32 isObjCClass()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D8 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isObjCClass()
  } // end of property IDiaSymbol5::isObjCClass
  .property int32 isObjCCategory()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isObjCCategory()
  } // end of property IDiaSymbol5::isObjCCategory
  .property int32 isObjCProtocol()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 DA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_isObjCProtocol()
  } // end of property IDiaSymbol5::isObjCProtocol
  .property class DIALib.IDiaSymbol inlinee()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 DB 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol5::get_inlinee()
  } // end of property IDiaSymbol5::inlinee
  .property uint32 inlineeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 DC 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol5::get_inlineeId()
  } // end of property IDiaSymbol5::inlineeId
  .property int32 noexcept()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 DD 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_noexcept()
  } // end of property IDiaSymbol5::noexcept
  .property int32 hasAbsoluteAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 DE 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol5::get_hasAbsoluteAddress()
  } // end of property IDiaSymbol5::hasAbsoluteAddress
} // end of class DIALib.IDiaSymbol5

.class interface public abstract auto ansi import DIALib.IDiaSymbol6
       implements DIALib.IDiaSymbol5
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.ComInterfaceType) = ( 01 00 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 38 31 33 33 44 41 44 33 2D 37 35 46 45   // ..$8133DAD3-75FE
                                                                                                  2D 34 32 33 34 2D 41 43 37 45 2D 46 38 45 37 41   // -4234-AC7E-F8E7A
                                                                                                  31 44 33 43 42 42 33 00 00 )                      // 1D3CBB3..
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 0A 73 79 6D 49 6E 64 65 78 49 64 00 00 )    // ...symIndexId..
  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_symIndexId() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_symIndexId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_symTag() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_symTag

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_name() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_name

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_lexicalParent() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_lexicalParent

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_classParent() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_classParent

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_type() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_type

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_dataKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_dataKind

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_locationType() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_locationType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_addressSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_addressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_addressOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_addressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_relativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_relativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_virtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_virtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_registerId() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_registerId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_offset() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_offset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_length() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_length

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_slot() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_slot

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_volatileType() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_volatileType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_constType() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_constType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_unalignedType() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_unalignedType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_access() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_access

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_libraryName() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_libraryName

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_platform() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_platform

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_language() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_language

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_editAndContinueEnabled() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_editAndContinueEnabled

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndMajor() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_frontEndMajor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndMinor() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_frontEndMinor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndBuild() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_frontEndBuild

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndMajor() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_backEndMajor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndMinor() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_backEndMinor

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndBuild() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_backEndBuild

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_sourceFileName() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_sourceFileName

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_unused() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_unused

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_thunkOrdinal() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_thunkOrdinal

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_thisAdjust() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_thisAdjust

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_virtualBaseOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_virtualBaseOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_virtual() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_virtual

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_intro() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_intro

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_pure() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_pure

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_callingConvention() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_callingConvention

  .method public hidebysig newslot specialname abstract virtual 
          instance object 
          marshal( struct) 
          get_value() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_value

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseType() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_baseType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_token() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_token

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_timeStamp() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_timeStamp

  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype [mscorlib]System.Guid 
          get_guid() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_guid

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_symbolsFileName() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_symbolsFileName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_reference() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_reference

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_count() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_count

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bitPosition() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_bitPosition

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_arrayIndexType() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_arrayIndexType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_packed() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_packed

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_constructor() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_constructor

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_overloadedOperator() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_overloadedOperator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_nested() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_nested

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasNestedTypes() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_hasNestedTypes

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasAssignmentOperator() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_hasAssignmentOperator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasCastOperator() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_hasCastOperator

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_scoped() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_scoped

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_virtualBaseClass() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_virtualBaseClass

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_indirectVirtualBaseClass() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_indirectVirtualBaseClass

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_virtualBasePointerOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_virtualBasePointerOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_virtualTableShape() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_virtualTableShape

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lexicalParentId() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_lexicalParentId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_classParentId() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_classParentId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_typeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_typeId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_arrayIndexTypeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_arrayIndexTypeId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_virtualTableShapeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_virtualTableShapeId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_code() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_code

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_function() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_function

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_managed() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_managed

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_msil() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_msil

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_virtualBaseDispIndex() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_virtualBaseDispIndex

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_undecoratedName() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_undecoratedName

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_age() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_age

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_signature() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_signature

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_compilerGenerated() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_compilerGenerated

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_addressTaken() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_addressTaken

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_rank() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_rank

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_lowerBound() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_lowerBound

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_upperBound() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_upperBound

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_lowerBoundId() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_lowerBoundId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_upperBoundId() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_upperBoundId

  .method public hidebysig newslot abstract virtual 
          instance void  get_dataBytes([in] uint32 cbData,
                                       [out] uint32& pcbData,
                                       [out] uint8& pbData) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_dataBytes

  .method public hidebysig newslot abstract virtual 
          instance void  findChildren([in] valuetype DIALib.SymTagEnum symTag,
                                      [in] string  marshal( lpwstr) name,
                                      [in] uint32 compareFlags,
                                      [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::findChildren

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenEx([in] valuetype DIALib.SymTagEnum symTag,
                                        [in] string  marshal( lpwstr) name,
                                        [in] uint32 compareFlags,
                                        [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::findChildrenEx

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByAddr([in] valuetype DIALib.SymTagEnum symTag,
                                              [in] string  marshal( lpwstr) name,
                                              [in] uint32 compareFlags,
                                              [in] uint32 isect,
                                              [in] uint32 offset,
                                              [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::findChildrenExByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByVA([in] valuetype DIALib.SymTagEnum symTag,
                                            [in] string  marshal( lpwstr) name,
                                            [in] uint32 compareFlags,
                                            [in] uint64 va,
                                            [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::findChildrenExByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findChildrenExByRVA([in] valuetype DIALib.SymTagEnum symTag,
                                             [in] string  marshal( lpwstr) name,
                                             [in] uint32 compareFlags,
                                             [in] uint32 rva,
                                             [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::findChildrenExByRVA

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_targetSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_targetSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_targetOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_targetOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_targetRelativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_targetRelativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_targetVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_targetVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_machineType() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_machineType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_oemId() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_oemId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_oemSymbolId() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_oemSymbolId

  .method public hidebysig newslot abstract virtual 
          instance void  get_types([in] uint32 cTypes,
                                   [out] uint32& pcTypes,
                                   [out] class DIALib.IDiaSymbol&  marshal( interface ) pTypes) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_types

  .method public hidebysig newslot abstract virtual 
          instance void  get_typeIds([in] uint32 cTypeIds,
                                     [out] uint32& pcTypeIds,
                                     [out] uint32& pdwTypeIds) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_typeIds

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_objectPointerType() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_objectPointerType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_udtKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_udtKind

  .method public hidebysig newslot abstract virtual 
          instance void  get_undecoratedNameEx([in] uint32 undecorateOptions,
                                               [out] string&  marshal( bstr) name) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_undecoratedNameEx

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noReturn() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_noReturn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_customCallingConvention() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_customCallingConvention

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noInline() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_noInline

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_optimizedCodeDebugInfo() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_optimizedCodeDebugInfo

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_notReached() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_notReached

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_interruptReturn() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_interruptReturn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_farReturn() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_farReturn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isStatic() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isStatic

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasDebugInfo() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_hasDebugInfo

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isLTCG() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isLTCG

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isDataAligned() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isDataAligned

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasSecurityChecks() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_hasSecurityChecks

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_compilerName() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_compilerName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasAlloca() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_hasAlloca

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasSetJump() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_hasSetJump

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasLongJump() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_hasLongJump

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasInlAsm() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_hasInlAsm

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasEH() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_hasEH

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasSEH() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_hasSEH

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasEHa() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_hasEHa

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isNaked() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isNaked

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAggregated() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isAggregated

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSplitted() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isSplitted

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_container() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_container

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_inlSpec() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_inlSpec

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noStackOrdering() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_noStackOrdering

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_virtualBaseTableType() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_virtualBaseTableType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasManagedCode() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_hasManagedCode

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isHotpatchable() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isHotpatchable

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isCVTCIL() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isCVTCIL

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isMSILNetmodule() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isMSILNetmodule

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isCTypes() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isCTypes

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isStripped() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isStripped

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frontEndQFE() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_frontEndQFE

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_backEndQFE() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_backEndQFE

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_wasInlined() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_wasInlined

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_strictGSCheck() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_strictGSCheck

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isCxxReturnUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isCxxReturnUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isConstructorVirtualBase() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isConstructorVirtualBase

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_RValueReference() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_RValueReference

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_unmodifiedType() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_unmodifiedType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_framePointerPresent() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_framePointerPresent

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSafeBuffers() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isSafeBuffers

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_intrinsic() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_intrinsic

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_sealed() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_sealed

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hfaFloat() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_hfaFloat

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hfaDouble() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_hfaDouble

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_liveRangeStartAddressSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_liveRangeStartAddressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_liveRangeStartAddressOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_liveRangeStartAddressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_liveRangeStartRelativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_liveRangeStartRelativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_countLiveRanges() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_countLiveRanges

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_liveRangeLength() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_liveRangeLength

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_offsetInUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_offsetInUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_paramBasePointerRegisterId() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_paramBasePointerRegisterId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_localBasePointerRegisterId() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_localBasePointerRegisterId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isLocationControlFlowDependent() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isLocationControlFlowDependent

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_stride() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_stride

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfRows() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_numberOfRows

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfColumns() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_numberOfColumns

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isMatrixRowMajor() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isMatrixRowMajor

  .method public hidebysig newslot abstract virtual 
          instance void  get_numericProperties([in] uint32 cnt,
                                               [out] uint32& pcnt,
                                               [out] uint32& pProperties) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_numericProperties

  .method public hidebysig newslot abstract virtual 
          instance void  get_modifierValues([in] uint32 cnt,
                                            [out] uint32& pcnt,
                                            [out] uint16& pModifiers) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_modifierValues

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isReturnValue() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isReturnValue

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isOptimizedAway() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isOptimizedAway

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_builtInKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_builtInKind

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_registerType() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_registerType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseDataSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_baseDataSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseDataOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_baseDataOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_textureSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_textureSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_samplerSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_samplerSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_uavSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_uavSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_sizeInUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_sizeInUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_memorySpaceKind() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_memorySpaceKind

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_unmodifiedTypeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_unmodifiedTypeId

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_subTypeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_subTypeId

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_subType() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_subType

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfModifiers() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_numberOfModifiers

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfRegisterIndices() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_numberOfRegisterIndices

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isHLSLData() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isHLSLData

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPointerToDataMember() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isPointerToDataMember

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPointerToMemberFunction() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isPointerToMemberFunction

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSingleInheritance() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isSingleInheritance

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isMultipleInheritance() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isMultipleInheritance

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isVirtualInheritance() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isVirtualInheritance

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_restrictedType() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_restrictedType

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPointerBasedOnSymbolValue() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isPointerBasedOnSymbolValue

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_baseSymbol() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_baseSymbol

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_baseSymbolId() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_baseSymbolId

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_objectFileName() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_objectFileName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAcceleratorGroupSharedLocal() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isAcceleratorGroupSharedLocal

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAcceleratorPointerTagLiveRange() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isAcceleratorPointerTagLiveRange

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isAcceleratorStubFunction() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isAcceleratorStubFunction

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_numberOfAcceleratorPointerTags() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_numberOfAcceleratorPointerTags

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isSdl() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isSdl

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isWinRTPointer() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isWinRTPointer

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isRefUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isRefUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isValueUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isValueUdt

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isInterfaceUdt() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isInterfaceUdt

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByAddr([in] uint32 isect,
                                                [in] uint32 offset,
                                                [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::findInlineFramesByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByRVA([in] uint32 rva,
                                               [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::findInlineFramesByRVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineFramesByVA([in] uint64 va,
                                              [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::findInlineFramesByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLines([out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::findInlineeLines

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByAddr([in] uint32 isect,
                                                [in] uint32 offset,
                                                [in] uint32 length,
                                                [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::findInlineeLinesByAddr

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByRVA([in] uint32 rva,
                                               [in] uint32 length,
                                               [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::findInlineeLinesByRVA

  .method public hidebysig newslot abstract virtual 
          instance void  findInlineeLinesByVA([in] uint64 va,
                                              [in] uint32 length,
                                              [out] class DIALib.IDiaEnumLineNumbers&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::findInlineeLinesByVA

  .method public hidebysig newslot abstract virtual 
          instance void  findSymbolsForAcceleratorPointerTag([in] uint32 tagValue,
                                                             [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::findSymbolsForAcceleratorPointerTag

  .method public hidebysig newslot abstract virtual 
          instance void  findSymbolsByRVAForAcceleratorPointerTag([in] uint32 tagValue,
                                                                  [in] uint32 rva,
                                                                  [out] class DIALib.IDiaEnumSymbols&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::findSymbolsByRVAForAcceleratorPointerTag

  .method public hidebysig newslot abstract virtual 
          instance void  get_acceleratorPointerTags([in] uint32 cnt,
                                                    [out] uint32& pcnt,
                                                    [out] uint32& pPointerTags) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_acceleratorPointerTags

  .method public hidebysig newslot abstract virtual 
          instance void  getSrcLineOnTypeDefn([out] class DIALib.IDiaLineNumber&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::getSrcLineOnTypeDefn

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isPGO() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isPGO

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasValidPGOCounts() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_hasValidPGOCounts

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isOptimizedForSpeed() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isOptimizedForSpeed

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_PGOEntryCount() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_PGOEntryCount

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_PGOEdgeCount() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_PGOEdgeCount

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_PGODynamicInstructionCount() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_PGODynamicInstructionCount

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_staticSize() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_staticSize

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_finalLiveStaticSize() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_finalLiveStaticSize

  .method public hidebysig newslot specialname abstract virtual 
          instance string 
          marshal( bstr) 
          get_phaseName() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_phaseName

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasControlFlowCheck() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_hasControlFlowCheck

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_constantExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_constantExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_dataExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_dataExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_privateExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_privateExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noNameExport() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_noNameExport

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_exportHasExplicitlyAssignedOrdinal() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_exportHasExplicitlyAssignedOrdinal

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_exportIsForwarder() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_exportIsForwarder

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_ordinal() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_ordinal

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_frameSize() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_frameSize

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_exceptionHandlerAddressSection() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_exceptionHandlerAddressSection

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_exceptionHandlerAddressOffset() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_exceptionHandlerAddressOffset

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_exceptionHandlerRelativeVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_exceptionHandlerRelativeVirtualAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance uint64  get_exceptionHandlerVirtualAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_exceptionHandlerVirtualAddress

  .method public hidebysig newslot abstract virtual 
          instance void  findInputAssemblyFile([out] class DIALib.IDiaInputAssemblyFile&  marshal( interface ) ppResult) runtime managed internalcall
  {
  } // end of method IDiaSymbol6::findInputAssemblyFile

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_characteristics() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_characteristics

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_coffGroup() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_coffGroup

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bindID() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_bindID

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bindSpace() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_bindSpace

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_bindSlot() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_bindSlot

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isObjCClass() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isObjCClass

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isObjCCategory() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isObjCCategory

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isObjCProtocol() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isObjCProtocol

  .method public hidebysig newslot specialname abstract virtual 
          instance class DIALib.IDiaSymbol 
          marshal( interface ) 
          get_inlinee() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_inlinee

  .method public hidebysig newslot specialname abstract virtual 
          instance uint32  get_inlineeId() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_inlineeId

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_noexcept() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_noexcept

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_hasAbsoluteAddress() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_hasAbsoluteAddress

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_isStaticMemberFunc() runtime managed internalcall
  {
  } // end of method IDiaSymbol6::get_isStaticMemberFunc

  .property uint32 symIndexId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_symIndexId()
  } // end of property IDiaSymbol6::symIndexId
  .property uint32 symTag()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 01 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_symTag()
  } // end of property IDiaSymbol6::symTag
  .property string name()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 02 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol6::get_name()
  } // end of property IDiaSymbol6::name
  .property class DIALib.IDiaSymbol lexicalParent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 03 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol6::get_lexicalParent()
  } // end of property IDiaSymbol6::lexicalParent
  .property class DIALib.IDiaSymbol classParent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 04 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol6::get_classParent()
  } // end of property IDiaSymbol6::classParent
  .property class DIALib.IDiaSymbol 'type'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 05 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol6::get_type()
  } // end of property IDiaSymbol6::'type'
  .property uint32 dataKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 06 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_dataKind()
  } // end of property IDiaSymbol6::dataKind
  .property uint32 locationType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 07 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_locationType()
  } // end of property IDiaSymbol6::locationType
  .property uint32 addressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_addressSection()
  } // end of property IDiaSymbol6::addressSection
  .property uint32 addressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 09 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_addressOffset()
  } // end of property IDiaSymbol6::addressOffset
  .property uint32 relativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0A 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_relativeVirtualAddress()
  } // end of property IDiaSymbol6::relativeVirtualAddress
  .property uint64 virtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0B 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol6::get_virtualAddress()
  } // end of property IDiaSymbol6::virtualAddress
  .property uint32 registerId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_registerId()
  } // end of property IDiaSymbol6::registerId
  .property int32 offset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0D 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_offset()
  } // end of property IDiaSymbol6::offset
  .property uint64 length()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0E 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol6::get_length()
  } // end of property IDiaSymbol6::length
  .property uint32 slot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 0F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_slot()
  } // end of property IDiaSymbol6::slot
  .property int32 volatileType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 10 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_volatileType()
  } // end of property IDiaSymbol6::volatileType
  .property int32 constType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 11 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_constType()
  } // end of property IDiaSymbol6::constType
  .property int32 unalignedType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 12 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_unalignedType()
  } // end of property IDiaSymbol6::unalignedType
  .property uint32 access()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 13 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_access()
  } // end of property IDiaSymbol6::access
  .property string libraryName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 14 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol6::get_libraryName()
  } // end of property IDiaSymbol6::libraryName
  .property uint32 platform()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 15 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_platform()
  } // end of property IDiaSymbol6::platform
  .property uint32 language()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 16 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_language()
  } // end of property IDiaSymbol6::language
  .property int32 editAndContinueEnabled()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 17 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_editAndContinueEnabled()
  } // end of property IDiaSymbol6::editAndContinueEnabled
  .property uint32 frontEndMajor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 18 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_frontEndMajor()
  } // end of property IDiaSymbol6::frontEndMajor
  .property uint32 frontEndMinor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 19 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_frontEndMinor()
  } // end of property IDiaSymbol6::frontEndMinor
  .property uint32 frontEndBuild()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1A 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_frontEndBuild()
  } // end of property IDiaSymbol6::frontEndBuild
  .property uint32 backEndMajor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1B 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_backEndMajor()
  } // end of property IDiaSymbol6::backEndMajor
  .property uint32 backEndMinor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_backEndMinor()
  } // end of property IDiaSymbol6::backEndMinor
  .property uint32 backEndBuild()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1D 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_backEndBuild()
  } // end of property IDiaSymbol6::backEndBuild
  .property string sourceFileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1E 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol6::get_sourceFileName()
  } // end of property IDiaSymbol6::sourceFileName
  .property string 'unused'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 1F 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol6::get_unused()
  } // end of property IDiaSymbol6::'unused'
  .property uint32 thunkOrdinal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 20 00 00 00 00 00 )                         // .. .....
    .get instance uint32 DIALib.IDiaSymbol6::get_thunkOrdinal()
  } // end of property IDiaSymbol6::thunkOrdinal
  .property int32 thisAdjust()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 21 00 00 00 00 00 )                         // ..!.....
    .get instance int32 DIALib.IDiaSymbol6::get_thisAdjust()
  } // end of property IDiaSymbol6::thisAdjust
  .property uint32 virtualBaseOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 22 00 00 00 00 00 )                         // ..".....
    .get instance uint32 DIALib.IDiaSymbol6::get_virtualBaseOffset()
  } // end of property IDiaSymbol6::virtualBaseOffset
  .property int32 'virtual'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 23 00 00 00 00 00 )                         // ..#.....
    .get instance int32 DIALib.IDiaSymbol6::get_virtual()
  } // end of property IDiaSymbol6::'virtual'
  .property int32 intro()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 24 00 00 00 00 00 )                         // ..$.....
    .get instance int32 DIALib.IDiaSymbol6::get_intro()
  } // end of property IDiaSymbol6::intro
  .property int32 pure()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 25 00 00 00 00 00 )                         // ..%.....
    .get instance int32 DIALib.IDiaSymbol6::get_pure()
  } // end of property IDiaSymbol6::pure
  .property uint32 callingConvention()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 26 00 00 00 00 00 )                         // ..&.....
    .get instance uint32 DIALib.IDiaSymbol6::get_callingConvention()
  } // end of property IDiaSymbol6::callingConvention
  .property object 'value'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 27 00 00 00 00 00 )                         // ..'.....
    .get instance object DIALib.IDiaSymbol6::get_value()
  } // end of property IDiaSymbol6::'value'
  .property uint32 baseType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 28 00 00 00 00 00 )                         // ..(.....
    .get instance uint32 DIALib.IDiaSymbol6::get_baseType()
  } // end of property IDiaSymbol6::baseType
  .property uint32 token()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 29 00 00 00 00 00 )                         // ..).....
    .get instance uint32 DIALib.IDiaSymbol6::get_token()
  } // end of property IDiaSymbol6::token
  .property uint32 timeStamp()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2A 00 00 00 00 00 )                         // ..*.....
    .get instance uint32 DIALib.IDiaSymbol6::get_timeStamp()
  } // end of property IDiaSymbol6::timeStamp
  .property valuetype [mscorlib]System.Guid
          guid()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2B 00 00 00 00 00 )                         // ..+.....
    .get instance valuetype [mscorlib]System.Guid DIALib.IDiaSymbol6::get_guid()
  } // end of property IDiaSymbol6::guid
  .property string symbolsFileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2C 00 00 00 00 00 )                         // ..,.....
    .get instance string DIALib.IDiaSymbol6::get_symbolsFileName()
  } // end of property IDiaSymbol6::symbolsFileName
  .property int32 reference()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2E 00 00 00 00 00 )                         // ........
    .get instance int32 DIALib.IDiaSymbol6::get_reference()
  } // end of property IDiaSymbol6::reference
  .property uint32 count()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 2F 00 00 00 00 00 )                         // ../.....
    .get instance uint32 DIALib.IDiaSymbol6::get_count()
  } // end of property IDiaSymbol6::count
  .property uint32 bitPosition()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 31 00 00 00 00 00 )                         // ..1.....
    .get instance uint32 DIALib.IDiaSymbol6::get_bitPosition()
  } // end of property IDiaSymbol6::bitPosition
  .property class DIALib.IDiaSymbol arrayIndexType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 32 00 00 00 00 00 )                         // ..2.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol6::get_arrayIndexType()
  } // end of property IDiaSymbol6::arrayIndexType
  .property int32 packed()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 33 00 00 00 00 00 )                         // ..3.....
    .get instance int32 DIALib.IDiaSymbol6::get_packed()
  } // end of property IDiaSymbol6::packed
  .property int32 constructor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 34 00 00 00 00 00 )                         // ..4.....
    .get instance int32 DIALib.IDiaSymbol6::get_constructor()
  } // end of property IDiaSymbol6::constructor
  .property int32 overloadedOperator()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 35 00 00 00 00 00 )                         // ..5.....
    .get instance int32 DIALib.IDiaSymbol6::get_overloadedOperator()
  } // end of property IDiaSymbol6::overloadedOperator
  .property int32 'nested'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 36 00 00 00 00 00 )                         // ..6.....
    .get instance int32 DIALib.IDiaSymbol6::get_nested()
  } // end of property IDiaSymbol6::'nested'
  .property int32 hasNestedTypes()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 37 00 00 00 00 00 )                         // ..7.....
    .get instance int32 DIALib.IDiaSymbol6::get_hasNestedTypes()
  } // end of property IDiaSymbol6::hasNestedTypes
  .property int32 hasAssignmentOperator()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 38 00 00 00 00 00 )                         // ..8.....
    .get instance int32 DIALib.IDiaSymbol6::get_hasAssignmentOperator()
  } // end of property IDiaSymbol6::hasAssignmentOperator
  .property int32 hasCastOperator()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 39 00 00 00 00 00 )                         // ..9.....
    .get instance int32 DIALib.IDiaSymbol6::get_hasCastOperator()
  } // end of property IDiaSymbol6::hasCastOperator
  .property int32 scoped()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3A 00 00 00 00 00 )                         // ..:.....
    .get instance int32 DIALib.IDiaSymbol6::get_scoped()
  } // end of property IDiaSymbol6::scoped
  .property int32 virtualBaseClass()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3B 00 00 00 00 00 )                         // ..;.....
    .get instance int32 DIALib.IDiaSymbol6::get_virtualBaseClass()
  } // end of property IDiaSymbol6::virtualBaseClass
  .property int32 indirectVirtualBaseClass()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3C 00 00 00 00 00 )                         // ..<.....
    .get instance int32 DIALib.IDiaSymbol6::get_indirectVirtualBaseClass()
  } // end of property IDiaSymbol6::indirectVirtualBaseClass
  .property int32 virtualBasePointerOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3D 00 00 00 00 00 )                         // ..=.....
    .get instance int32 DIALib.IDiaSymbol6::get_virtualBasePointerOffset()
  } // end of property IDiaSymbol6::virtualBasePointerOffset
  .property class DIALib.IDiaSymbol virtualTableShape()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 3E 00 00 00 00 00 )                         // ..>.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol6::get_virtualTableShape()
  } // end of property IDiaSymbol6::virtualTableShape
  .property uint32 lexicalParentId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 40 00 00 00 00 00 )                         // ..@.....
    .get instance uint32 DIALib.IDiaSymbol6::get_lexicalParentId()
  } // end of property IDiaSymbol6::lexicalParentId
  .property uint32 classParentId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 41 00 00 00 00 00 )                         // ..A.....
    .get instance uint32 DIALib.IDiaSymbol6::get_classParentId()
  } // end of property IDiaSymbol6::classParentId
  .property uint32 typeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 42 00 00 00 00 00 )                         // ..B.....
    .get instance uint32 DIALib.IDiaSymbol6::get_typeId()
  } // end of property IDiaSymbol6::typeId
  .property uint32 arrayIndexTypeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 43 00 00 00 00 00 )                         // ..C.....
    .get instance uint32 DIALib.IDiaSymbol6::get_arrayIndexTypeId()
  } // end of property IDiaSymbol6::arrayIndexTypeId
  .property uint32 virtualTableShapeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 44 00 00 00 00 00 )                         // ..D.....
    .get instance uint32 DIALib.IDiaSymbol6::get_virtualTableShapeId()
  } // end of property IDiaSymbol6::virtualTableShapeId
  .property int32 code()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 45 00 00 00 00 00 )                         // ..E.....
    .get instance int32 DIALib.IDiaSymbol6::get_code()
  } // end of property IDiaSymbol6::code
  .property int32 function()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 46 00 00 00 00 00 )                         // ..F.....
    .get instance int32 DIALib.IDiaSymbol6::get_function()
  } // end of property IDiaSymbol6::function
  .property int32 'managed'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 47 00 00 00 00 00 )                         // ..G.....
    .get instance int32 DIALib.IDiaSymbol6::get_managed()
  } // end of property IDiaSymbol6::'managed'
  .property int32 msil()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 48 00 00 00 00 00 )                         // ..H.....
    .get instance int32 DIALib.IDiaSymbol6::get_msil()
  } // end of property IDiaSymbol6::msil
  .property uint32 virtualBaseDispIndex()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 49 00 00 00 00 00 )                         // ..I.....
    .get instance uint32 DIALib.IDiaSymbol6::get_virtualBaseDispIndex()
  } // end of property IDiaSymbol6::virtualBaseDispIndex
  .property string undecoratedName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4A 00 00 00 00 00 )                         // ..J.....
    .get instance string DIALib.IDiaSymbol6::get_undecoratedName()
  } // end of property IDiaSymbol6::undecoratedName
  .property uint32 age()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4B 00 00 00 00 00 )                         // ..K.....
    .get instance uint32 DIALib.IDiaSymbol6::get_age()
  } // end of property IDiaSymbol6::age
  .property uint32 signature()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4C 00 00 00 00 00 )                         // ..L.....
    .get instance uint32 DIALib.IDiaSymbol6::get_signature()
  } // end of property IDiaSymbol6::signature
  .property int32 compilerGenerated()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4D 00 00 00 00 00 )                         // ..M.....
    .get instance int32 DIALib.IDiaSymbol6::get_compilerGenerated()
  } // end of property IDiaSymbol6::compilerGenerated
  .property int32 addressTaken()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4E 00 00 00 00 00 )                         // ..N.....
    .get instance int32 DIALib.IDiaSymbol6::get_addressTaken()
  } // end of property IDiaSymbol6::addressTaken
  .property uint32 rank()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 4F 00 00 00 00 00 )                         // ..O.....
    .get instance uint32 DIALib.IDiaSymbol6::get_rank()
  } // end of property IDiaSymbol6::rank
  .property class DIALib.IDiaSymbol lowerBound()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 50 00 00 00 00 00 )                         // ..P.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol6::get_lowerBound()
  } // end of property IDiaSymbol6::lowerBound
  .property class DIALib.IDiaSymbol upperBound()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 51 00 00 00 00 00 )                         // ..Q.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol6::get_upperBound()
  } // end of property IDiaSymbol6::upperBound
  .property uint32 lowerBoundId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 52 00 00 00 00 00 )                         // ..R.....
    .get instance uint32 DIALib.IDiaSymbol6::get_lowerBoundId()
  } // end of property IDiaSymbol6::lowerBoundId
  .property uint32 upperBoundId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 53 00 00 00 00 00 )                         // ..S.....
    .get instance uint32 DIALib.IDiaSymbol6::get_upperBoundId()
  } // end of property IDiaSymbol6::upperBoundId
  .property uint32 targetSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 54 00 00 00 00 00 )                         // ..T.....
    .get instance uint32 DIALib.IDiaSymbol6::get_targetSection()
  } // end of property IDiaSymbol6::targetSection
  .property uint32 targetOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 55 00 00 00 00 00 )                         // ..U.....
    .get instance uint32 DIALib.IDiaSymbol6::get_targetOffset()
  } // end of property IDiaSymbol6::targetOffset
  .property uint32 targetRelativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 56 00 00 00 00 00 )                         // ..V.....
    .get instance uint32 DIALib.IDiaSymbol6::get_targetRelativeVirtualAddress()
  } // end of property IDiaSymbol6::targetRelativeVirtualAddress
  .property uint64 targetVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 57 00 00 00 00 00 )                         // ..W.....
    .get instance uint64 DIALib.IDiaSymbol6::get_targetVirtualAddress()
  } // end of property IDiaSymbol6::targetVirtualAddress
  .property uint32 machineType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 58 00 00 00 00 00 )                         // ..X.....
    .get instance uint32 DIALib.IDiaSymbol6::get_machineType()
  } // end of property IDiaSymbol6::machineType
  .property uint32 oemId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 59 00 00 00 00 00 )                         // ..Y.....
    .get instance uint32 DIALib.IDiaSymbol6::get_oemId()
  } // end of property IDiaSymbol6::oemId
  .property uint32 oemSymbolId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5A 00 00 00 00 00 )                         // ..Z.....
    .get instance uint32 DIALib.IDiaSymbol6::get_oemSymbolId()
  } // end of property IDiaSymbol6::oemSymbolId
  .property class DIALib.IDiaSymbol objectPointerType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5B 00 00 00 00 00 )                         // ..[.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol6::get_objectPointerType()
  } // end of property IDiaSymbol6::objectPointerType
  .property uint32 udtKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5C 00 00 00 00 00 )                         // ..\.....
    .get instance uint32 DIALib.IDiaSymbol6::get_udtKind()
  } // end of property IDiaSymbol6::udtKind
  .property int32 noReturn()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5D 00 00 00 00 00 )                         // ..].....
    .get instance int32 DIALib.IDiaSymbol6::get_noReturn()
  } // end of property IDiaSymbol6::noReturn
  .property int32 customCallingConvention()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5E 00 00 00 00 00 )                         // ..^.....
    .get instance int32 DIALib.IDiaSymbol6::get_customCallingConvention()
  } // end of property IDiaSymbol6::customCallingConvention
  .property int32 noInline()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 5F 00 00 00 00 00 )                         // .._.....
    .get instance int32 DIALib.IDiaSymbol6::get_noInline()
  } // end of property IDiaSymbol6::noInline
  .property int32 optimizedCodeDebugInfo()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 60 00 00 00 00 00 )                         // ..`.....
    .get instance int32 DIALib.IDiaSymbol6::get_optimizedCodeDebugInfo()
  } // end of property IDiaSymbol6::optimizedCodeDebugInfo
  .property int32 notReached()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 61 00 00 00 00 00 )                         // ..a.....
    .get instance int32 DIALib.IDiaSymbol6::get_notReached()
  } // end of property IDiaSymbol6::notReached
  .property int32 interruptReturn()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 62 00 00 00 00 00 )                         // ..b.....
    .get instance int32 DIALib.IDiaSymbol6::get_interruptReturn()
  } // end of property IDiaSymbol6::interruptReturn
  .property int32 farReturn()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 63 00 00 00 00 00 )                         // ..c.....
    .get instance int32 DIALib.IDiaSymbol6::get_farReturn()
  } // end of property IDiaSymbol6::farReturn
  .property int32 isStatic()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 64 00 00 00 00 00 )                         // ..d.....
    .get instance int32 DIALib.IDiaSymbol6::get_isStatic()
  } // end of property IDiaSymbol6::isStatic
  .property int32 hasDebugInfo()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 65 00 00 00 00 00 )                         // ..e.....
    .get instance int32 DIALib.IDiaSymbol6::get_hasDebugInfo()
  } // end of property IDiaSymbol6::hasDebugInfo
  .property int32 isLTCG()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 66 00 00 00 00 00 )                         // ..f.....
    .get instance int32 DIALib.IDiaSymbol6::get_isLTCG()
  } // end of property IDiaSymbol6::isLTCG
  .property int32 isDataAligned()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 67 00 00 00 00 00 )                         // ..g.....
    .get instance int32 DIALib.IDiaSymbol6::get_isDataAligned()
  } // end of property IDiaSymbol6::isDataAligned
  .property int32 hasSecurityChecks()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 68 00 00 00 00 00 )                         // ..h.....
    .get instance int32 DIALib.IDiaSymbol6::get_hasSecurityChecks()
  } // end of property IDiaSymbol6::hasSecurityChecks
  .property string compilerName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 69 00 00 00 00 00 )                         // ..i.....
    .get instance string DIALib.IDiaSymbol6::get_compilerName()
  } // end of property IDiaSymbol6::compilerName
  .property int32 hasAlloca()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6A 00 00 00 00 00 )                         // ..j.....
    .get instance int32 DIALib.IDiaSymbol6::get_hasAlloca()
  } // end of property IDiaSymbol6::hasAlloca
  .property int32 hasSetJump()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6B 00 00 00 00 00 )                         // ..k.....
    .get instance int32 DIALib.IDiaSymbol6::get_hasSetJump()
  } // end of property IDiaSymbol6::hasSetJump
  .property int32 hasLongJump()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6C 00 00 00 00 00 )                         // ..l.....
    .get instance int32 DIALib.IDiaSymbol6::get_hasLongJump()
  } // end of property IDiaSymbol6::hasLongJump
  .property int32 hasInlAsm()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6D 00 00 00 00 00 )                         // ..m.....
    .get instance int32 DIALib.IDiaSymbol6::get_hasInlAsm()
  } // end of property IDiaSymbol6::hasInlAsm
  .property int32 hasEH()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6E 00 00 00 00 00 )                         // ..n.....
    .get instance int32 DIALib.IDiaSymbol6::get_hasEH()
  } // end of property IDiaSymbol6::hasEH
  .property int32 hasSEH()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 6F 00 00 00 00 00 )                         // ..o.....
    .get instance int32 DIALib.IDiaSymbol6::get_hasSEH()
  } // end of property IDiaSymbol6::hasSEH
  .property int32 hasEHa()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 70 00 00 00 00 00 )                         // ..p.....
    .get instance int32 DIALib.IDiaSymbol6::get_hasEHa()
  } // end of property IDiaSymbol6::hasEHa
  .property int32 isNaked()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 71 00 00 00 00 00 )                         // ..q.....
    .get instance int32 DIALib.IDiaSymbol6::get_isNaked()
  } // end of property IDiaSymbol6::isNaked
  .property int32 isAggregated()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 72 00 00 00 00 00 )                         // ..r.....
    .get instance int32 DIALib.IDiaSymbol6::get_isAggregated()
  } // end of property IDiaSymbol6::isAggregated
  .property int32 isSplitted()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 73 00 00 00 00 00 )                         // ..s.....
    .get instance int32 DIALib.IDiaSymbol6::get_isSplitted()
  } // end of property IDiaSymbol6::isSplitted
  .property class DIALib.IDiaSymbol container()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 74 00 00 00 00 00 )                         // ..t.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol6::get_container()
  } // end of property IDiaSymbol6::container
  .property int32 inlSpec()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 75 00 00 00 00 00 )                         // ..u.....
    .get instance int32 DIALib.IDiaSymbol6::get_inlSpec()
  } // end of property IDiaSymbol6::inlSpec
  .property int32 noStackOrdering()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 76 00 00 00 00 00 )                         // ..v.....
    .get instance int32 DIALib.IDiaSymbol6::get_noStackOrdering()
  } // end of property IDiaSymbol6::noStackOrdering
  .property class DIALib.IDiaSymbol virtualBaseTableType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 77 00 00 00 00 00 )                         // ..w.....
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol6::get_virtualBaseTableType()
  } // end of property IDiaSymbol6::virtualBaseTableType
  .property int32 hasManagedCode()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 78 00 00 00 00 00 )                         // ..x.....
    .get instance int32 DIALib.IDiaSymbol6::get_hasManagedCode()
  } // end of property IDiaSymbol6::hasManagedCode
  .property int32 isHotpatchable()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 79 00 00 00 00 00 )                         // ..y.....
    .get instance int32 DIALib.IDiaSymbol6::get_isHotpatchable()
  } // end of property IDiaSymbol6::isHotpatchable
  .property int32 isCVTCIL()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7A 00 00 00 00 00 )                         // ..z.....
    .get instance int32 DIALib.IDiaSymbol6::get_isCVTCIL()
  } // end of property IDiaSymbol6::isCVTCIL
  .property int32 isMSILNetmodule()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7B 00 00 00 00 00 )                         // ..{.....
    .get instance int32 DIALib.IDiaSymbol6::get_isMSILNetmodule()
  } // end of property IDiaSymbol6::isMSILNetmodule
  .property int32 isCTypes()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7C 00 00 00 00 00 )                         // ..|.....
    .get instance int32 DIALib.IDiaSymbol6::get_isCTypes()
  } // end of property IDiaSymbol6::isCTypes
  .property int32 isStripped()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7D 00 00 00 00 00 )                         // ..}.....
    .get instance int32 DIALib.IDiaSymbol6::get_isStripped()
  } // end of property IDiaSymbol6::isStripped
  .property uint32 frontEndQFE()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7E 00 00 00 00 00 )                         // ..~.....
    .get instance uint32 DIALib.IDiaSymbol6::get_frontEndQFE()
  } // end of property IDiaSymbol6::frontEndQFE
  .property uint32 backEndQFE()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 7F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_backEndQFE()
  } // end of property IDiaSymbol6::backEndQFE
  .property int32 wasInlined()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 80 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_wasInlined()
  } // end of property IDiaSymbol6::wasInlined
  .property int32 strictGSCheck()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 81 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_strictGSCheck()
  } // end of property IDiaSymbol6::strictGSCheck
  .property int32 isCxxReturnUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 82 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isCxxReturnUdt()
  } // end of property IDiaSymbol6::isCxxReturnUdt
  .property int32 isConstructorVirtualBase()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 83 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isConstructorVirtualBase()
  } // end of property IDiaSymbol6::isConstructorVirtualBase
  .property int32 RValueReference()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 84 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_RValueReference()
  } // end of property IDiaSymbol6::RValueReference
  .property class DIALib.IDiaSymbol unmodifiedType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 85 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol6::get_unmodifiedType()
  } // end of property IDiaSymbol6::unmodifiedType
  .property int32 framePointerPresent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 86 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_framePointerPresent()
  } // end of property IDiaSymbol6::framePointerPresent
  .property int32 isSafeBuffers()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 87 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isSafeBuffers()
  } // end of property IDiaSymbol6::isSafeBuffers
  .property int32 intrinsic()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 88 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_intrinsic()
  } // end of property IDiaSymbol6::intrinsic
  .property int32 'sealed'()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 89 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_sealed()
  } // end of property IDiaSymbol6::'sealed'
  .property int32 hfaFloat()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8A 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_hfaFloat()
  } // end of property IDiaSymbol6::hfaFloat
  .property int32 hfaDouble()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8B 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_hfaDouble()
  } // end of property IDiaSymbol6::hfaDouble
  .property uint32 liveRangeStartAddressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_liveRangeStartAddressSection()
  } // end of property IDiaSymbol6::liveRangeStartAddressSection
  .property uint32 liveRangeStartAddressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8D 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_liveRangeStartAddressOffset()
  } // end of property IDiaSymbol6::liveRangeStartAddressOffset
  .property uint32 liveRangeStartRelativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8E 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_liveRangeStartRelativeVirtualAddress()
  } // end of property IDiaSymbol6::liveRangeStartRelativeVirtualAddress
  .property uint32 countLiveRanges()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 8F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_countLiveRanges()
  } // end of property IDiaSymbol6::countLiveRanges
  .property uint64 liveRangeLength()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 90 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol6::get_liveRangeLength()
  } // end of property IDiaSymbol6::liveRangeLength
  .property uint32 offsetInUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 91 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_offsetInUdt()
  } // end of property IDiaSymbol6::offsetInUdt
  .property uint32 paramBasePointerRegisterId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 92 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_paramBasePointerRegisterId()
  } // end of property IDiaSymbol6::paramBasePointerRegisterId
  .property uint32 localBasePointerRegisterId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 93 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_localBasePointerRegisterId()
  } // end of property IDiaSymbol6::localBasePointerRegisterId
  .property int32 isLocationControlFlowDependent()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 94 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isLocationControlFlowDependent()
  } // end of property IDiaSymbol6::isLocationControlFlowDependent
  .property uint32 stride()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 95 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_stride()
  } // end of property IDiaSymbol6::stride
  .property uint32 numberOfRows()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 96 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_numberOfRows()
  } // end of property IDiaSymbol6::numberOfRows
  .property uint32 numberOfColumns()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 97 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_numberOfColumns()
  } // end of property IDiaSymbol6::numberOfColumns
  .property int32 isMatrixRowMajor()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 98 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isMatrixRowMajor()
  } // end of property IDiaSymbol6::isMatrixRowMajor
  .property int32 isReturnValue()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 99 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isReturnValue()
  } // end of property IDiaSymbol6::isReturnValue
  .property int32 isOptimizedAway()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9A 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isOptimizedAway()
  } // end of property IDiaSymbol6::isOptimizedAway
  .property uint32 builtInKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9B 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_builtInKind()
  } // end of property IDiaSymbol6::builtInKind
  .property uint32 registerType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9C 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_registerType()
  } // end of property IDiaSymbol6::registerType
  .property uint32 baseDataSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9D 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_baseDataSlot()
  } // end of property IDiaSymbol6::baseDataSlot
  .property uint32 baseDataOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9E 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_baseDataOffset()
  } // end of property IDiaSymbol6::baseDataOffset
  .property uint32 textureSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 9F 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_textureSlot()
  } // end of property IDiaSymbol6::textureSlot
  .property uint32 samplerSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A0 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_samplerSlot()
  } // end of property IDiaSymbol6::samplerSlot
  .property uint32 uavSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A1 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_uavSlot()
  } // end of property IDiaSymbol6::uavSlot
  .property uint32 sizeInUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A2 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_sizeInUdt()
  } // end of property IDiaSymbol6::sizeInUdt
  .property uint32 memorySpaceKind()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A3 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_memorySpaceKind()
  } // end of property IDiaSymbol6::memorySpaceKind
  .property uint32 unmodifiedTypeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A4 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_unmodifiedTypeId()
  } // end of property IDiaSymbol6::unmodifiedTypeId
  .property uint32 subTypeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A5 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_subTypeId()
  } // end of property IDiaSymbol6::subTypeId
  .property class DIALib.IDiaSymbol subType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A6 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol6::get_subType()
  } // end of property IDiaSymbol6::subType
  .property uint32 numberOfModifiers()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A7 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_numberOfModifiers()
  } // end of property IDiaSymbol6::numberOfModifiers
  .property uint32 numberOfRegisterIndices()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A8 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_numberOfRegisterIndices()
  } // end of property IDiaSymbol6::numberOfRegisterIndices
  .property int32 isHLSLData()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 A9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isHLSLData()
  } // end of property IDiaSymbol6::isHLSLData
  .property int32 isPointerToDataMember()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isPointerToDataMember()
  } // end of property IDiaSymbol6::isPointerToDataMember
  .property int32 isPointerToMemberFunction()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AB 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isPointerToMemberFunction()
  } // end of property IDiaSymbol6::isPointerToMemberFunction
  .property int32 isSingleInheritance()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AC 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isSingleInheritance()
  } // end of property IDiaSymbol6::isSingleInheritance
  .property int32 isMultipleInheritance()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AD 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isMultipleInheritance()
  } // end of property IDiaSymbol6::isMultipleInheritance
  .property int32 isVirtualInheritance()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AE 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isVirtualInheritance()
  } // end of property IDiaSymbol6::isVirtualInheritance
  .property int32 restrictedType()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 AF 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_restrictedType()
  } // end of property IDiaSymbol6::restrictedType
  .property int32 isPointerBasedOnSymbolValue()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B0 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isPointerBasedOnSymbolValue()
  } // end of property IDiaSymbol6::isPointerBasedOnSymbolValue
  .property class DIALib.IDiaSymbol baseSymbol()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B1 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol6::get_baseSymbol()
  } // end of property IDiaSymbol6::baseSymbol
  .property uint32 baseSymbolId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B2 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_baseSymbolId()
  } // end of property IDiaSymbol6::baseSymbolId
  .property string objectFileName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B3 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol6::get_objectFileName()
  } // end of property IDiaSymbol6::objectFileName
  .property int32 isAcceleratorGroupSharedLocal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B4 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isAcceleratorGroupSharedLocal()
  } // end of property IDiaSymbol6::isAcceleratorGroupSharedLocal
  .property int32 isAcceleratorPointerTagLiveRange()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B5 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isAcceleratorPointerTagLiveRange()
  } // end of property IDiaSymbol6::isAcceleratorPointerTagLiveRange
  .property int32 isAcceleratorStubFunction()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B6 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isAcceleratorStubFunction()
  } // end of property IDiaSymbol6::isAcceleratorStubFunction
  .property uint32 numberOfAcceleratorPointerTags()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B7 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_numberOfAcceleratorPointerTags()
  } // end of property IDiaSymbol6::numberOfAcceleratorPointerTags
  .property int32 isSdl()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B8 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isSdl()
  } // end of property IDiaSymbol6::isSdl
  .property int32 isWinRTPointer()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 B9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isWinRTPointer()
  } // end of property IDiaSymbol6::isWinRTPointer
  .property int32 isRefUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isRefUdt()
  } // end of property IDiaSymbol6::isRefUdt
  .property int32 isValueUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BB 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isValueUdt()
  } // end of property IDiaSymbol6::isValueUdt
  .property int32 isInterfaceUdt()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BC 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isInterfaceUdt()
  } // end of property IDiaSymbol6::isInterfaceUdt
  .property int32 isPGO()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BD 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isPGO()
  } // end of property IDiaSymbol6::isPGO
  .property int32 hasValidPGOCounts()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BE 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_hasValidPGOCounts()
  } // end of property IDiaSymbol6::hasValidPGOCounts
  .property int32 isOptimizedForSpeed()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 BF 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isOptimizedForSpeed()
  } // end of property IDiaSymbol6::isOptimizedForSpeed
  .property uint32 PGOEntryCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C0 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_PGOEntryCount()
  } // end of property IDiaSymbol6::PGOEntryCount
  .property uint32 PGOEdgeCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C1 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_PGOEdgeCount()
  } // end of property IDiaSymbol6::PGOEdgeCount
  .property uint64 PGODynamicInstructionCount()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C2 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol6::get_PGODynamicInstructionCount()
  } // end of property IDiaSymbol6::PGODynamicInstructionCount
  .property uint32 staticSize()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C3 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_staticSize()
  } // end of property IDiaSymbol6::staticSize
  .property uint32 finalLiveStaticSize()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C4 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_finalLiveStaticSize()
  } // end of property IDiaSymbol6::finalLiveStaticSize
  .property string phaseName()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C5 00 00 00 00 00 ) 
    .get instance string DIALib.IDiaSymbol6::get_phaseName()
  } // end of property IDiaSymbol6::phaseName
  .property int32 hasControlFlowCheck()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C6 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_hasControlFlowCheck()
  } // end of property IDiaSymbol6::hasControlFlowCheck
  .property int32 constantExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C7 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_constantExport()
  } // end of property IDiaSymbol6::constantExport
  .property int32 dataExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C8 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_dataExport()
  } // end of property IDiaSymbol6::dataExport
  .property int32 privateExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 C9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_privateExport()
  } // end of property IDiaSymbol6::privateExport
  .property int32 noNameExport()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_noNameExport()
  } // end of property IDiaSymbol6::noNameExport
  .property int32 exportHasExplicitlyAssignedOrdinal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CB 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_exportHasExplicitlyAssignedOrdinal()
  } // end of property IDiaSymbol6::exportHasExplicitlyAssignedOrdinal
  .property int32 exportIsForwarder()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CC 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_exportIsForwarder()
  } // end of property IDiaSymbol6::exportIsForwarder
  .property uint32 ordinal()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CD 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_ordinal()
  } // end of property IDiaSymbol6::ordinal
  .property uint32 frameSize()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CE 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_frameSize()
  } // end of property IDiaSymbol6::frameSize
  .property uint32 exceptionHandlerAddressSection()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 CF 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_exceptionHandlerAddressSection()
  } // end of property IDiaSymbol6::exceptionHandlerAddressSection
  .property uint32 exceptionHandlerAddressOffset()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D0 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_exceptionHandlerAddressOffset()
  } // end of property IDiaSymbol6::exceptionHandlerAddressOffset
  .property uint32 exceptionHandlerRelativeVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D1 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_exceptionHandlerRelativeVirtualAddress()
  } // end of property IDiaSymbol6::exceptionHandlerRelativeVirtualAddress
  .property uint64 exceptionHandlerVirtualAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D2 00 00 00 00 00 ) 
    .get instance uint64 DIALib.IDiaSymbol6::get_exceptionHandlerVirtualAddress()
  } // end of property IDiaSymbol6::exceptionHandlerVirtualAddress
  .property uint32 characteristics()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D3 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_characteristics()
  } // end of property IDiaSymbol6::characteristics
  .property class DIALib.IDiaSymbol coffGroup()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D4 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol6::get_coffGroup()
  } // end of property IDiaSymbol6::coffGroup
  .property uint32 bindID()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D5 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_bindID()
  } // end of property IDiaSymbol6::bindID
  .property uint32 bindSpace()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D6 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_bindSpace()
  } // end of property IDiaSymbol6::bindSpace
  .property uint32 bindSlot()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D7 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_bindSlot()
  } // end of property IDiaSymbol6::bindSlot
  .property int32 isObjCClass()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D8 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isObjCClass()
  } // end of property IDiaSymbol6::isObjCClass
  .property int32 isObjCCategory()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 D9 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isObjCCategory()
  } // end of property IDiaSymbol6::isObjCCategory
  .property int32 isObjCProtocol()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 DA 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isObjCProtocol()
  } // end of property IDiaSymbol6::isObjCProtocol
  .property class DIALib.IDiaSymbol inlinee()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 DB 00 00 00 00 00 ) 
    .get instance class DIALib.IDiaSymbol DIALib.IDiaSymbol6::get_inlinee()
  } // end of property IDiaSymbol6::inlinee
  .property uint32 inlineeId()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 DC 00 00 00 00 00 ) 
    .get instance uint32 DIALib.IDiaSymbol6::get_inlineeId()
  } // end of property IDiaSymbol6::inlineeId
  .property int32 noexcept()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 DD 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_noexcept()
  } // end of property IDiaSymbol6::noexcept
  .property int32 hasAbsoluteAddress()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 DE 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_hasAbsoluteAddress()
  } // end of property IDiaSymbol6::hasAbsoluteAddress
  .property int32 isStaticMemberFunc()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 DF 00 00 00 00 00 ) 
    .get instance int32 DIALib.IDiaSymbol6::get_isStaticMemberFunc()
  } // end of property IDiaSymbol6::isStaticMemberFunc
} // end of class DIALib.IDiaSymbol6


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file DIALib.res
