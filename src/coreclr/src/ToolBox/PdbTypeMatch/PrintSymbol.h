// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <string>

inline int myDebugBreak( int ){
    DebugBreak();
    return 0;
}
#define MAXELEMS(x)     (sizeof(x)/sizeof(x[0]))
#define SafeDRef(a, i)  ((i < MAXELEMS(a)) ? a[i] : a[myDebugBreak(i)])

#define MAX_TYPE_IN_DETAIL 5
#define MAX_RVA_LINES_BYTES_RANGE 0x100

extern const wchar_t * const rgBaseType[];
extern const wchar_t * const rgTags[];
extern const wchar_t * const rgFloatPackageStrings[];
extern const wchar_t * const rgProcessorStrings[];
extern const wchar_t * const rgDataKind[];
extern const wchar_t * const rgUdtKind[];
extern const wchar_t * const rgAccess[];
extern const wchar_t * const rgCallingConvention[];
extern const wchar_t * const rgLanguage[];
extern const wchar_t * const rgLocationTypeString[];

void PrintPublicSymbol( IDiaSymbol* );
void PrintGlobalSymbol( IDiaSymbol* );
void PrintSymbol( IDiaSymbol* , DWORD );
void GetSymbolName(std::wstring& symbolName, IDiaSymbol *pSymbol);
void PrintSymTag( DWORD );
void PrintName( IDiaSymbol* );
void PrintUndName( IDiaSymbol* );
void PrintThunk( IDiaSymbol* );
void PrintCompilandDetails( IDiaSymbol* );
void PrintCompilandEnv( IDiaSymbol* );
void PrintLocation( IDiaSymbol* );
void PrintConst( IDiaSymbol* );
void PrintUDT( IDiaSymbol* );
void PrintSymbolType( IDiaSymbol* );
void PrintType( IDiaSymbol* );
void PrintBound( IDiaSymbol* );
void PrintData( IDiaSymbol* , DWORD );
void PrintVariant( VARIANT );
void PrintUdtKind( IDiaSymbol* );
void PrintTypeInDetail( IDiaSymbol* , DWORD );
void PrintFunctionType( IDiaSymbol* );
void PrintSourceFile( IDiaSourceFile* );
void PrintLines( IDiaSession* , IDiaSymbol* );
void PrintLines( IDiaEnumLineNumbers* );
void PrintSource( IDiaSourceFile* );
void PrintSecContribs( IDiaSectionContrib* );
void PrintStreamData( IDiaEnumDebugStreamData* );
void PrintFrameData( IDiaFrameData* );

void PrintPropertyStorage( IDiaPropertyStorage* );

template<class T> void PrintGeneric( T t ){
  IDiaPropertyStorage* pPropertyStorage;
  
  if(t->QueryInterface( __uuidof(IDiaPropertyStorage), (void **)&pPropertyStorage ) == S_OK){
    PrintPropertyStorage(pPropertyStorage);
    pPropertyStorage->Release();
  }
}
