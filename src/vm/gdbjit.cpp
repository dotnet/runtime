// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: gdbjit.cpp
//

//
// NotifyGdb implementation.
//
//*****************************************************************************

#include "common.h"
#include "formattype.h"
#include "gdbjit.h"
#include "gdbjithelpers.h"

TypeInfoBase*
GetTypeInfoFromTypeHandle(TypeHandle typeHandle, NotifyGdb::PTK_TypeInfoMap pTypeMap)
{
    TypeInfoBase *typeInfo = nullptr;
    TypeKey key = typeHandle.GetTypeKey();
    PTR_MethodTable pMT = typeHandle.GetMethodTable();

    if (pTypeMap->Lookup(&key, &typeInfo))
    {
        return typeInfo;
    }

    CorElementType corType = typeHandle.GetSignatureCorElementType();
    switch (corType)
    {
        case ELEMENT_TYPE_VOID:
        case ELEMENT_TYPE_BOOLEAN:
        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R4:
        case ELEMENT_TYPE_R8:
        case ELEMENT_TYPE_U:
        case ELEMENT_TYPE_I:
            typeInfo = new (nothrow) PrimitiveTypeInfo(typeHandle, CorElementTypeToDWEncoding[corType]);
            if (typeInfo == nullptr)
                return nullptr;

            typeInfo->m_type_size = CorTypeInfo::Size(corType);

            break;
        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_CLASS:
        {
            ApproxFieldDescIterator fieldDescIterator(pMT,
                pMT->IsString() ? ApproxFieldDescIterator::INSTANCE_FIELDS : ApproxFieldDescIterator::ALL_FIELDS);
            ULONG cFields = fieldDescIterator.Count();

            typeInfo = new (nothrow) ClassTypeInfo(typeHandle, cFields);

            if (typeInfo == nullptr)
                return nullptr;

            typeInfo->m_type_size = typeHandle.AsMethodTable()->GetClass()->GetSize();

            RefTypeInfo* refTypeInfo = nullptr;
            if (!typeHandle.IsValueType())
            {
                // name the type
                refTypeInfo = new (nothrow) RefTypeInfo(typeHandle, typeInfo);
                if (refTypeInfo == nullptr)
                {
                    return nullptr;
                }
                refTypeInfo->m_type_size = sizeof(TADDR);
                refTypeInfo->m_value_type = typeInfo;
                refTypeInfo->CalculateName();

                pTypeMap->Add(refTypeInfo->GetTypeKey(), refTypeInfo);
            }

            pTypeMap->Add(typeInfo->GetTypeKey(), typeInfo);
            typeInfo->CalculateName();

            //
            // Now fill in the array
            //
            FieldDesc *pField;

            for (ULONG i = 0; i < cFields; i++)
            {
                pField = fieldDescIterator.Next();
                ClassTypeInfo *info = static_cast<ClassTypeInfo*>(typeInfo);

                LPCUTF8 szName = pField->GetName();
                info->members[i].m_member_name = new char[strlen(szName) + 1];
                strcpy(info->members[i].m_member_name, szName);
                if (!pField->IsStatic())
                {
                    info->members[i].m_member_offset = (ULONG)pField->GetOffset();
                    if (!typeHandle.IsValueType())
                        info->members[i].m_member_offset += Object::GetOffsetOfFirstField();
                }
                else
                {
                    PTR_BYTE base = 0;
                    MethodTable* pMT = pField->GetEnclosingMethodTable();
                    base = pField->GetBase();

                    // TODO: add support of generics with static fields
                    if (pField->IsRVA() || !pMT->IsDynamicStatics())
                    {
                        PTR_VOID pAddress = pField->GetStaticAddressHandle((PTR_VOID)dac_cast<TADDR>(base));
                        info->members[i].m_static_member_address = dac_cast<TADDR>(pAddress);
                    }
                }

                info->members[i].m_member_type =
                    GetTypeInfoFromTypeHandle(pField->GetExactFieldType(typeHandle), pTypeMap);

                // handle the System.String case:
                // coerce type of the second field into array type
                if (pMT->IsString() && i == 1)
                {
                    TypeInfoBase* elemTypeInfo = info->members[1].m_member_type;
                    TypeInfoBase* arrayTypeInfo = new (nothrow) ArrayTypeInfo(typeHandle.MakeSZArray(), 0, elemTypeInfo);
                    if (arrayTypeInfo == nullptr)
                        return nullptr;
                    info->members[1].m_member_type = arrayTypeInfo;
                }
            }
            if (refTypeInfo)
                return refTypeInfo;
            else
                return typeInfo;
        }
        case ELEMENT_TYPE_PTR:
        case ELEMENT_TYPE_BYREF:
        {
            TypeInfoBase* valTypeInfo = GetTypeInfoFromTypeHandle(typeHandle.GetTypeParam(), pTypeMap);
            typeInfo = new (nothrow) RefTypeInfo(typeHandle, valTypeInfo);
            if (typeInfo == nullptr)
                return nullptr;
            typeInfo->m_type_size = sizeof(TADDR);
            typeInfo->m_type_offset = valTypeInfo->m_type_offset;
            break;
        }
        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_SZARRAY:
        {
            typeInfo = new (nothrow) ClassTypeInfo(typeHandle, 2);
            if (typeInfo == nullptr)
                return nullptr;
            typeInfo->m_type_size = pMT->GetClass()->GetSize();

            typeInfo->CalculateName();
            RefTypeInfo *refTypeInfo = new (nothrow) RefTypeInfo(typeHandle, typeInfo);
            if (refTypeInfo == nullptr)
            {
                return nullptr;
            }
            refTypeInfo->m_type_size = sizeof(TADDR);
            refTypeInfo->m_value_type = typeInfo;
            refTypeInfo->CalculateName();

            pTypeMap->Add(refTypeInfo->GetTypeKey(), refTypeInfo);

            TypeInfoBase* lengthTypeInfo = GetTypeInfoFromTypeHandle(
                TypeHandle(MscorlibBinder::GetElementType(ELEMENT_TYPE_I4)), pTypeMap);

            TypeInfoBase* valTypeInfo = GetTypeInfoFromTypeHandle(typeHandle.GetTypeParam(), pTypeMap);
            TypeInfoBase* arrayTypeInfo = new (nothrow) ArrayTypeInfo(typeHandle, 0, valTypeInfo);
            if (arrayTypeInfo == nullptr)
                return nullptr;

            ClassTypeInfo *info = static_cast<ClassTypeInfo*>(typeInfo);

            info->members[0].m_member_name = new (nothrow) char[16];
            strcpy(info->members[0].m_member_name, "m_NumComponents");
            info->members[0].m_member_offset = ArrayBase::GetOffsetOfNumComponents();
            info->members[0].m_member_type = lengthTypeInfo;
            info->members[0].m_member_type->m_type_size = sizeof(DWORD);

            info->members[1].m_member_name = new (nothrow) char[7];
            strcpy(info->members[1].m_member_name, "m_Data");
            info->members[1].m_member_offset = ArrayBase::GetDataPtrOffset(pMT);
            info->members[1].m_member_type = arrayTypeInfo;
            info->members[1].m_member_type->m_type_size = sizeof(TADDR);

            return refTypeInfo;
        }
        default:
            ASSERT(0 && "not implemented");
            break;
    }
    // name the type
    if (corType == ELEMENT_TYPE_CHAR)
    {
        typeInfo->m_type_name = new char[9];
        strcpy(typeInfo->m_type_name, "char16_t");
    }
    else
    {
        typeInfo->CalculateName();
    }
    pTypeMap->Add(typeInfo->GetTypeKey(), typeInfo);
    return typeInfo;
}

TypeInfoBase* GetArgTypeInfo(MethodDesc* MethodDescPtr,
                    NotifyGdb::PTK_TypeInfoMap pTypeMap,
                    unsigned ilIndex)
{
    MetaSig sig(MethodDescPtr);
    TypeHandle th;
    if (ilIndex == 0)
    {
        th = sig.GetRetTypeHandleNT();
    }
    else
    {
        while (--ilIndex)
            sig.SkipArg();

        sig.NextArg();
        th = sig.GetLastTypeHandleNT();
    }
    return GetTypeInfoFromTypeHandle(th, pTypeMap);
}

TypeInfoBase* GetLocalTypeInfo(MethodDesc *MethodDescPtr,
                      NotifyGdb::PTK_TypeInfoMap pTypeMap,
                      unsigned ilIndex)
{
    COR_ILMETHOD_DECODER method(MethodDescPtr->GetILHeader());
    if (method.GetLocalVarSigTok())
    {
        DWORD cbSigLen;
        PCCOR_SIGNATURE pComSig;

        if (FAILED(MethodDescPtr->GetMDImport()->GetSigFromToken(method.GetLocalVarSigTok(), &cbSigLen, &pComSig)))
        {
            printf("\nInvalid record");
            return nullptr;
        }

        _ASSERTE(*pComSig == IMAGE_CEE_CS_CALLCONV_LOCAL_SIG);

        SigTypeContext typeContext(MethodDescPtr, TypeHandle());
        MetaSig sig(pComSig, cbSigLen, MethodDescPtr->GetModule(), &typeContext, MetaSig::sigLocalVars);
        if (ilIndex > 0)
        {
            while (ilIndex--)
                sig.SkipArg();
        }
        sig.NextArg();
        TypeHandle th = sig.GetLastTypeHandleNT();
        return GetTypeInfoFromTypeHandle(th, pTypeMap);
    }
    return nullptr;
}

HRESULT GetArgNameByILIndex(MethodDesc* MethodDescPtr, unsigned index, LPSTR &paramName)
{
    IMDInternalImport* mdImport = MethodDescPtr->GetMDImport();
    mdParamDef paramToken;
    USHORT seq;
    DWORD attr;
    HRESULT status;

    // Param indexing is 1-based.
    ULONG32 mdIndex = index + 1;

    MetaSig sig(MethodDescPtr);
    if (sig.HasThis())
    {
        mdIndex--;
    }
    status = mdImport->FindParamOfMethod(MethodDescPtr->GetMemberDef(), mdIndex, &paramToken);
    if (status == S_OK)
    {
        LPCSTR name;
        status = mdImport->GetParamDefProps(paramToken, &seq, &attr, &name);
        paramName = new char[strlen(name) + 1];
        strcpy(paramName, name);
    }
    return status;
}

// Copy-pasted from src/debug/di/module.cpp
HRESULT FindNativeInfoInILVariable(DWORD dwIndex,
                                   SIZE_T ip,
                                   ICorDebugInfo::NativeVarInfo** nativeInfoList,
                                   unsigned int nativeInfoCount,
                                   ICorDebugInfo::NativeVarInfo** ppNativeInfo)
{
    _ASSERTE(ppNativeInfo != NULL);
    *ppNativeInfo = NULL;
    int lastGoodOne = -1;
    for (unsigned int i = 0; i < (unsigned)nativeInfoCount; i++)
    {
        if ((*nativeInfoList)[i].varNumber == dwIndex)
        {
            if ((lastGoodOne == -1) || ((*nativeInfoList)[lastGoodOne].startOffset < (*nativeInfoList)[i].startOffset))
            {
                lastGoodOne = i;
            }

            if (((*nativeInfoList)[i].startOffset <= ip) &&
                ((*nativeInfoList)[i].endOffset > ip))
            {
                *ppNativeInfo = &((*nativeInfoList)[i]);

                return S_OK;
            }
        }
    }

    if ((lastGoodOne > -1) && ((*nativeInfoList)[lastGoodOne].endOffset == ip))
    {
        *ppNativeInfo = &((*nativeInfoList)[lastGoodOne]);
        return S_OK;
    }

    return CORDBG_E_IL_VAR_NOT_AVAILABLE;
}

BYTE* DebugInfoStoreNew(void * pData, size_t cBytes)
{
    return new (nothrow) BYTE[cBytes];
}

/* Get IL to native offsets map */
HRESULT
GetMethodNativeMap(MethodDesc* methodDesc,
                   ULONG32* numMap,
                   DebuggerILToNativeMap** map,
                   ULONG32* pcVars,
                   ICorDebugInfo::NativeVarInfo** ppVars)
{
    // Use the DebugInfoStore to get IL->Native maps.
    // It doesn't matter whether we're jitted, ngenned etc.

    DebugInfoRequest request;
    TADDR nativeCodeStartAddr = PCODEToPINSTR(methodDesc->GetNativeCode());
    request.InitFromStartingAddr(methodDesc, nativeCodeStartAddr);

    // Bounds info.
    ULONG32 countMapCopy;
    NewHolder<ICorDebugInfo::OffsetMapping> mapCopy(NULL);

    BOOL success = DebugInfoManager::GetBoundariesAndVars(request,
                                                          DebugInfoStoreNew,
                                                          NULL, // allocator
                                                          &countMapCopy,
                                                          &mapCopy,
                                                          pcVars,
                                                          ppVars);

    if (!success)
    {
        return E_FAIL;
    }

    // Need to convert map formats.
    *numMap = countMapCopy;

    *map = new (nothrow) DebuggerILToNativeMap[countMapCopy];
    if (!*map)
    {
        return E_OUTOFMEMORY;
    }

    ULONG32 i;
    for (i = 0; i < *numMap; i++)
    {
        (*map)[i].ilOffset = mapCopy[i].ilOffset;
        (*map)[i].nativeStartOffset = mapCopy[i].nativeOffset;
        if (i > 0)
        {
            (*map)[i - 1].nativeEndOffset = (*map)[i].nativeStartOffset;
        }
        (*map)[i].source = mapCopy[i].source;
    }
    if (*numMap >= 1)
    {
        (*map)[i - 1].nativeEndOffset = 0;
    }
    return S_OK;
}

HRESULT FunctionMember::GetLocalsDebugInfo(NotifyGdb::PTK_TypeInfoMap pTypeMap,
                           LocalsInfo& locals,
                           int startNativeOffset)
{

    ICorDebugInfo::NativeVarInfo* nativeVar = NULL;
    int thisOffs = 0;
    if (!md->IsStatic())
    {
        thisOffs = 1;
    }

    int i;
    for (i = 0; i < m_num_args - thisOffs; i++)
    {
        if (FindNativeInfoInILVariable(i + thisOffs, startNativeOffset, &locals.pVars, locals.countVars, &nativeVar) == S_OK)
        {
            vars[i + thisOffs].m_var_type = GetArgTypeInfo(md, pTypeMap, i + 1);
            GetArgNameByILIndex(md, i + thisOffs, vars[i + thisOffs].m_var_name);
            vars[i + thisOffs].m_il_index = i;
            vars[i + thisOffs].m_native_offset = nativeVar->loc.vlStk.vlsOffset;
            vars[i + thisOffs].m_var_abbrev = 6;
        }
    }
    //Add info about 'this' as first argument
    if (thisOffs == 1)
    {
        if (FindNativeInfoInILVariable(0, startNativeOffset, &locals.pVars, locals.countVars, &nativeVar) == S_OK)
        {
            vars[0].m_var_type = GetTypeInfoFromTypeHandle(TypeHandle(md->GetMethodTable()), pTypeMap);
            vars[0].m_var_name = new char[strlen("this") + 1];
            strcpy(vars[0].m_var_name, "this");
            vars[0].m_il_index = 0;
            vars[0].m_native_offset = nativeVar->loc.vlStk.vlsOffset;
            vars[0].m_var_abbrev = 13;
         }
         i++;
    }
    for (; i < m_num_vars; i++)
    {
        if (FindNativeInfoInILVariable(
                i, startNativeOffset, &locals.pVars, locals.countVars, &nativeVar) == S_OK)
        {
            vars[i].m_var_type = GetLocalTypeInfo(md, pTypeMap, i - m_num_args);
            vars[i].m_var_name = new char[strlen(locals.localsName[i - m_num_args]) + 1];
            strcpy(vars[i].m_var_name, locals.localsName[i - m_num_args]);
            vars[i].m_il_index = i - m_num_args;
            vars[i].m_native_offset = nativeVar->loc.vlStk.vlsOffset;
            vars[i].m_var_abbrev = 5;
        }
    }
    return S_OK;
}
/* Get mapping of IL offsets to source line numbers */
HRESULT
GetDebugInfoFromPDB(MethodDesc* MethodDescPtr, SymbolsInfo** symInfo, unsigned int &symInfoLen,  LocalsInfo &locals)
{
    DebuggerILToNativeMap* map = NULL;

    ULONG32 numMap;

    if (!getInfoForMethodDelegate)
        return E_FAIL;
 
    if (GetMethodNativeMap(MethodDescPtr, &numMap, &map, &locals.countVars, &locals.pVars) != S_OK)
        return E_FAIL;

    const Module* mod = MethodDescPtr->GetMethodTable()->GetModule();
    SString modName = mod->GetFile()->GetPath();
    if (modName.IsEmpty())
        return E_FAIL;

    StackScratchBuffer scratch;
    const char* szModName = modName.GetUTF8(scratch);

    MethodDebugInfo methodDebugInfo;

    methodDebugInfo.points = (SequencePointInfo*) CoTaskMemAlloc(sizeof(SequencePointInfo) * numMap);
    if (methodDebugInfo.points == nullptr)
        return E_OUTOFMEMORY;

    methodDebugInfo.size = numMap;

    if (getInfoForMethodDelegate(szModName, MethodDescPtr->GetMemberDef(), methodDebugInfo) == FALSE)
        return E_FAIL;

    symInfoLen = numMap;
    *symInfo = new (nothrow) SymbolsInfo[numMap];
    if (*symInfo == nullptr)
        return E_FAIL;
    locals.size = methodDebugInfo.localsSize;
    locals.localsName = new (nothrow) char *[locals.size];
    if (locals.localsName == nullptr)
        return E_FAIL;

    for (ULONG32 i = 0; i < locals.size; i++)
    {
        size_t sizeRequired = WideCharToMultiByte(CP_UTF8, 0, methodDebugInfo.locals[i], -1, NULL, 0, NULL, NULL);
        locals.localsName[i] = new (nothrow) char[sizeRequired];

        int len = WideCharToMultiByte(
            CP_UTF8, 0, methodDebugInfo.locals[i], -1, locals.localsName[i], sizeRequired, NULL, NULL);
    }

    for (ULONG32 j = 0; j < numMap; j++)
    {
        SymbolsInfo& s = (*symInfo)[j];

        if (j == 0) {
            s.fileName[0] = 0;
            s.lineNumber = 0;
            s.fileIndex = 0;
        } else {
            s = (*symInfo)[j - 1];
        }
        s.nativeOffset = map[j].nativeStartOffset;
        s.ilOffset = map[j].ilOffset;
        s.source = map[j].source;
        s.lineNumber = 0;

        for (ULONG32 i = 0; i < methodDebugInfo.size; i++)
        {
            const SequencePointInfo& sp = methodDebugInfo.points[i];

            if (methodDebugInfo.points[i].ilOffset == map[j].ilOffset)
            {
                s.fileIndex = 0;
                int len = WideCharToMultiByte(CP_UTF8, 0, sp.fileName, -1, s.fileName, sizeof(s.fileName), NULL, NULL);
                s.fileName[len] = 0;
                s.lineNumber = sp.lineNumber;
                break;
            }
        }
    }

    CoTaskMemFree(methodDebugInfo.points);
    return S_OK;
}

/* LEB128 for 32-bit unsigned integer */
int Leb128Encode(uint32_t num, char* buf, int size)
{
    int i = 0;
    
    do
    {
        uint8_t byte = num & 0x7F;
        if (i >= size)
            break;
        num >>= 7;
        if (num != 0)
            byte |= 0x80;
        buf[i++] = byte;
    }
    while (num != 0);
    
    return i;
}

/* LEB128 for 32-bit signed integer */
int Leb128Encode(int32_t num, char* buf, int size)
{
    int i = 0;
    bool hasMore = true, isNegative = num < 0;
    
    while (hasMore && i < size)
    {
        uint8_t byte = num & 0x7F;
        num >>= 7;
        
        if ((num == 0 && (byte & 0x40) == 0) || (num  == -1 && (byte & 0x40) == 0x40))
            hasMore = false;
        else
            byte |= 0x80;
        buf[i++] = byte;
    }
    
    return i;
}

int GetFrameLocation(int nativeOffset, char* bufVarLoc)
{
    char cnvBuf[16] = {0};
    int len = Leb128Encode(static_cast<int32_t>(nativeOffset), cnvBuf, sizeof(cnvBuf));
    bufVarLoc[0] = len + 1;
    bufVarLoc[1] = DW_OP_fbreg;
    for (int j = 0; j < len; j++)
    {
        bufVarLoc[j + 2] = cnvBuf[j];
    }

    return len + 2;  // We add '2' because first 2 bytes contain length of expression and DW_OP_fbreg operation.
}

// GDB JIT interface
typedef enum
{
  JIT_NOACTION = 0,
  JIT_REGISTER_FN,
  JIT_UNREGISTER_FN
} jit_actions_t;

struct jit_code_entry
{
  struct jit_code_entry *next_entry;
  struct jit_code_entry *prev_entry;
  const char *symfile_addr;
  UINT64 symfile_size;
};

struct jit_descriptor
{
  UINT32 version;
  /* This type should be jit_actions_t, but we use uint32_t
     to be explicit about the bitwidth.  */
  UINT32 action_flag;
  struct jit_code_entry *relevant_entry;
  struct jit_code_entry *first_entry;
};
// GDB puts a breakpoint in this function.
// To prevent from inlining we add noinline attribute and inline assembler statement.
extern "C"
void __attribute__((noinline)) __jit_debug_register_code() { __asm__(""); };

/* Make sure to specify the version statically, because the
   debugger may check the version before we can set it.  */
struct jit_descriptor __jit_debug_descriptor = { 1, 0, 0, 0 };

// END of GDB JIT interface

/* Predefined section names */
const char* SectionNames[] = {
    "",
    ".text",
    ".shstrtab",
    ".debug_str",
    ".debug_abbrev",
    ".debug_info",
    ".debug_pubnames",
    ".debug_pubtypes",
    ".debug_line",
    ".symtab",
    ".strtab"
    /* After the last (.strtab) section zero or more .thunk_* sections are generated.

       Each .thunk_* section contains a single .thunk_#.
       These symbols are mapped to methods (or trampolines) called by currently compiled method. */
};

const int SectionNamesCount = sizeof(SectionNames) / sizeof(SectionNames[0]); // Does not include .thunk_* sections

/* Static data for section headers */
struct SectionHeader {
    uint32_t m_type;
    uint64_t m_flags;
} Sections[] = {
    {SHT_NULL, 0},
    {SHT_PROGBITS, SHF_ALLOC | SHF_EXECINSTR},
    {SHT_STRTAB, 0},
    {SHT_PROGBITS, SHF_MERGE | SHF_STRINGS },
    {SHT_PROGBITS, 0},
    {SHT_PROGBITS, 0},
    {SHT_PROGBITS, 0},
    {SHT_PROGBITS, 0},
    {SHT_PROGBITS, 0},
    {SHT_SYMTAB, 0},
    {SHT_STRTAB, 0},
    {SHT_PROGBITS, SHF_ALLOC | SHF_EXECINSTR}
};

/* Static data for .debug_str section */
const char* DebugStrings[] = {
  "CoreCLR", "" /* module name */, "" /* module path */
};

const int DebugStringCount = sizeof(DebugStrings) / sizeof(DebugStrings[0]);

/* Static data for .debug_abbrev */
const unsigned char AbbrevTable[] = {
    1, DW_TAG_compile_unit, DW_CHILDREN_yes,
        DW_AT_producer, DW_FORM_strp, DW_AT_language, DW_FORM_data2, DW_AT_name, DW_FORM_strp,
        DW_AT_stmt_list, DW_FORM_sec_offset, 0, 0,

    2, DW_TAG_base_type, DW_CHILDREN_no,
        DW_AT_name, DW_FORM_strp, DW_AT_encoding, DW_FORM_data1, DW_AT_byte_size, DW_FORM_data1, 0, 0,

    3, DW_TAG_typedef, DW_CHILDREN_no,
        DW_AT_name, DW_FORM_strp, DW_AT_decl_file, DW_FORM_data1, DW_AT_decl_line, DW_FORM_data1,
        DW_AT_type, DW_FORM_ref4, 0, 0,

    4, DW_TAG_subprogram, DW_CHILDREN_yes,
        DW_AT_name, DW_FORM_strp, DW_AT_linkage_name, DW_FORM_strp, DW_AT_decl_file, DW_FORM_data1, DW_AT_decl_line, DW_FORM_data1,
        DW_AT_type, DW_FORM_ref4, DW_AT_external, DW_FORM_flag_present,
        DW_AT_low_pc, DW_FORM_addr, DW_AT_high_pc, DW_FORM_size,
        DW_AT_frame_base, DW_FORM_exprloc, 0, 0,

    5, DW_TAG_variable, DW_CHILDREN_no,
        DW_AT_name, DW_FORM_strp, DW_AT_decl_file, DW_FORM_data1, DW_AT_decl_line, DW_FORM_data1, DW_AT_type,
        DW_FORM_ref4, DW_AT_location, DW_FORM_exprloc, 0, 0,

    6, DW_TAG_formal_parameter, DW_CHILDREN_no,
        DW_AT_name, DW_FORM_strp, DW_AT_decl_file, DW_FORM_data1, DW_AT_decl_line, DW_FORM_data1, DW_AT_type,
        DW_FORM_ref4, DW_AT_location, DW_FORM_exprloc, 0, 0,

    7, DW_TAG_class_type, DW_CHILDREN_yes,
        DW_AT_name, DW_FORM_strp, DW_AT_byte_size, DW_FORM_data1, 0, 0,

    8, DW_TAG_member, DW_CHILDREN_no,
        DW_AT_name, DW_FORM_strp, DW_AT_type, DW_FORM_ref4, DW_AT_data_member_location, DW_FORM_data4, 0, 0,

    9, DW_TAG_pointer_type, DW_CHILDREN_no,
        DW_AT_type, DW_FORM_ref4, DW_AT_byte_size, DW_FORM_data1, 0, 0,

    10, DW_TAG_array_type, DW_CHILDREN_yes,
        DW_AT_type, DW_FORM_ref4, 0, 0,

    11, DW_TAG_subrange_type, DW_CHILDREN_no,
        DW_AT_upper_bound, DW_FORM_exprloc, 0, 0,

    12, DW_TAG_subprogram, DW_CHILDREN_yes,
        DW_AT_name, DW_FORM_strp, DW_AT_linkage_name, DW_FORM_strp, DW_AT_decl_file, DW_FORM_data1, DW_AT_decl_line, DW_FORM_data1,
        DW_AT_type, DW_FORM_ref4, DW_AT_external, DW_FORM_flag_present,
        DW_AT_low_pc, DW_FORM_addr, DW_AT_high_pc, DW_FORM_size,
        DW_AT_frame_base, DW_FORM_exprloc, DW_AT_object_pointer, DW_FORM_ref4, 0, 0,

    13, DW_TAG_formal_parameter, DW_CHILDREN_no,
        DW_AT_name, DW_FORM_strp, DW_AT_decl_file, DW_FORM_data1, DW_AT_decl_line, DW_FORM_data1, DW_AT_type,
        DW_FORM_ref4, DW_AT_location, DW_FORM_exprloc, DW_AT_artificial, DW_FORM_flag_present, 0, 0,

    14, DW_TAG_member, DW_CHILDREN_no,
        DW_AT_name, DW_FORM_strp, DW_AT_type, DW_FORM_ref4, DW_AT_external, DW_FORM_flag_present, 0, 0,

    15, DW_TAG_variable, DW_CHILDREN_no, DW_AT_specification, DW_FORM_ref4, DW_AT_location, DW_FORM_exprloc,
        0, 0,

    16, DW_TAG_try_block, DW_CHILDREN_no,
        DW_AT_low_pc, DW_FORM_addr, DW_AT_high_pc, DW_FORM_size,
        0, 0,

    17, DW_TAG_catch_block, DW_CHILDREN_no,
        DW_AT_low_pc, DW_FORM_addr, DW_AT_high_pc, DW_FORM_size,
        0, 0,

    0
};

const int AbbrevTableSize = sizeof(AbbrevTable);

/* Static data for .debug_line, including header */
#define DWARF_LINE_BASE (-5)
#define DWARF_LINE_RANGE 14
#define DWARF_OPCODE_BASE 13

DwarfLineNumHeader LineNumHeader = {
    0, 2, 0, 1, 1, DWARF_LINE_BASE, DWARF_LINE_RANGE, DWARF_OPCODE_BASE, {0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 1}
};

/* Static data for .debug_info */
struct __attribute__((packed)) DebugInfoCU
{
    uint8_t m_cu_abbrev;
    uint32_t m_prod_off;
    uint16_t m_lang;
    uint32_t m_cu_name;
    uint32_t m_line_num;
} debugInfoCU = {
    1, 0, DW_LANG_C89, 0, 0
};

struct __attribute__((packed)) DebugInfoTryCatchSub
{
    uint8_t m_sub_abbrev;
    uintptr_t m_sub_low_pc, m_sub_high_pc;
};

struct __attribute__((packed)) DebugInfoSub
{
    uint8_t m_sub_abbrev;
    uint32_t m_sub_name;
    uint32_t m_linkage_name;
    uint8_t m_file, m_line;
    uint32_t m_sub_type;
    uintptr_t m_sub_low_pc, m_sub_high_pc;
    uint8_t m_sub_loc[2];
};

struct __attribute__((packed)) DebugInfoSubMember
{
    DebugInfoSub sub;
    uint32_t m_obj_ptr;
};

// Holder for array of pointers to FunctionMember objects
class FunctionMemberPtrArrayHolder : public NewArrayHolder<FunctionMember*>
{
private:
    int m_cElements;

    void DeleteElements()
    {
        for (int i = 0; i < m_cElements; i++)
        {
            delete this->m_value[i];
        }
    }

public:
    FunctionMemberPtrArrayHolder() :
        NewArrayHolder<FunctionMember*>(),
        m_cElements(0)
    {
    }

    bool Alloc(int cElements)
    {
        FunctionMember** value = new (nothrow) FunctionMember*[cElements];
        if (value == nullptr)
            return false;

        for (int i = 0; i < cElements; i++)
        {
            value[i] = nullptr;
        }

        // Clean previous elements
        DeleteElements();

        NewArrayHolder<FunctionMember*>::operator=(value);
        m_cElements = cElements;
        return true;
    }

    int GetCount() const
    {
        return m_cElements;
    }

    ~FunctionMemberPtrArrayHolder()
    {
        DeleteElements();
    }
};

static FunctionMemberPtrArrayHolder method;

struct __attribute__((packed)) DebugInfoType
{
    uint8_t m_type_abbrev;
    uint32_t m_type_name;
    uint8_t m_encoding;
    uint8_t m_byte_size;
};

struct __attribute__((packed)) DebugInfoVar
{
    uint8_t m_var_abbrev;
    uint32_t m_var_name;
    uint8_t m_var_file, m_var_line;
    uint32_t m_var_type;
};

struct __attribute__((packed)) DebugInfoClassType
{
    uint8_t m_type_abbrev;
    uint32_t m_type_name;
    uint8_t m_byte_size;
};

struct __attribute__((packed)) DebugInfoClassMember
{
    uint8_t m_member_abbrev;
    uint32_t m_member_name;
    uint32_t m_member_type;
};

struct __attribute__((packed)) DebugInfoStaticMember
{
    uint8_t m_member_abbrev;
    uint32_t m_member_specification;
};


struct __attribute__((packed)) DebugInfoRefType
{
    uint8_t m_type_abbrev;
    uint32_t m_ref_type;
    uint8_t m_byte_size;
};

struct __attribute__((packed)) DebugInfoArrayType
{
    uint8_t m_abbrev;
    uint32_t m_type;
};

void TypeInfoBase::DumpStrings(char* ptr, int& offset)
{
    if (ptr != nullptr)
    {
        strcpy(ptr + offset, m_type_name);
        m_type_name_offset = offset;
    }
    offset += strlen(m_type_name) + 1;
}

void TypeInfoBase::CalculateName() 
{
    // name the type
    SString sName;
    typeHandle.GetName(sName);
    StackScratchBuffer buffer;
    const UTF8 *utf8 = sName.GetUTF8(buffer);
    m_type_name = new char[strlen(utf8) + 1];
    strcpy(m_type_name, utf8);
}

void TypeInfoBase::SetTypeHandle(TypeHandle handle)
{
    typeHandle = handle;
    typeKey = handle.GetTypeKey();
}

TypeHandle TypeInfoBase::GetTypeHandle()
{
    return typeHandle;
}

TypeKey* TypeInfoBase::GetTypeKey()
{
    return &typeKey;
}

void PrimitiveTypeInfo::DumpDebugInfo(char* ptr, int& offset)
{
    if (m_type_offset != 0)
    {
        return;
    }

    if (ptr != nullptr)
    {
        DebugInfoType bufType;
        bufType.m_type_abbrev = 2;
        bufType.m_type_name = m_type_name_offset;
        bufType.m_encoding = m_type_encoding;
        bufType.m_byte_size = m_type_size;

        memcpy(ptr + offset,
               &bufType,
               sizeof(DebugInfoType));
        m_type_offset = offset;
    }

    offset += sizeof(DebugInfoType);
}

ClassTypeInfo::ClassTypeInfo(TypeHandle typeHandle, int num_members)
        : TypeInfoBase(typeHandle),
          m_num_members(num_members),
          members(new TypeMember[num_members])
{
}

ClassTypeInfo::~ClassTypeInfo()
{
    if (members != nullptr && m_num_members > 0)
    {
        delete[] members;
    }
}

void TypeMember::DumpStrings(char* ptr, int& offset)
{
    if (ptr != nullptr)
    {
        strcpy(ptr + offset, m_member_name);
        m_member_name_offset = offset;
    }
    offset += strlen(m_member_name) + 1;
}

void TypeMember::DumpDebugInfo(char* ptr, int& offset)
{
    if (ptr != nullptr)
    {
        DebugInfoClassMember memberEntry;

        if (m_static_member_address == 0)
            memberEntry.m_member_abbrev = 8;
        else
        {
            memberEntry.m_member_abbrev = 14;
            m_member_offset = offset;
        }
        memberEntry.m_member_name = m_member_name_offset;
        memberEntry.m_member_type = m_member_type->m_type_offset;

        memcpy(ptr + offset, &memberEntry, sizeof(DebugInfoClassMember));
        if (m_static_member_address == 0)
            memcpy(ptr + offset + sizeof(DebugInfoClassMember), &m_member_offset, sizeof(m_member_offset));
    }
    offset += sizeof(DebugInfoClassMember);
    if (m_static_member_address == 0)
        offset += sizeof(m_member_offset);
}

void TypeMember::DumpStaticDebugInfo(char* ptr, int& offset)
{
    const int ptrSize = sizeof(TADDR);
    if (ptr != nullptr)
    {
        DebugInfoStaticMember memberEntry;

        memberEntry.m_member_abbrev = 15;
        memberEntry.m_member_specification = m_member_offset;
        memcpy(ptr + offset, &memberEntry, sizeof(DebugInfoStaticMember));

        char buf[ptrSize + 2] = {0};
        buf[0] = ptrSize + 1;
        buf[1] = DW_OP_addr;

        for (int i = 0; i < ptrSize; i++)
        {
            buf[i + 2] = m_static_member_address >> (i * 8);
        }

        memcpy(ptr + offset + sizeof(DebugInfoStaticMember), &buf, ptrSize + 2);
    }
    offset += sizeof(DebugInfoStaticMember);
    offset += ptrSize + 2;
}

void FunctionMember::MangleName(char *buf, int &buf_offset, const char *name)
{
    int name_length = strlen(name);

    char tmp[20];
    int tmp_len = sprintf_s(tmp, _countof(tmp), "%i", name_length);
    if (tmp_len <= 0)
        return;

    if (buf)
        strncpy(buf + buf_offset, tmp, tmp_len);
    buf_offset += tmp_len;

    if (buf)
    {
        for (int i = 0; i < name_length; i++)
        {
            char c = name[i];
            bool valid = (c >= 'a' && c <= 'z') ||
                         (c >= 'A' && c <= 'Z') ||
                         (c >= '0' && c <= '9');
            *(buf + buf_offset + i) = valid ? c : '_';
        }
    }
    buf_offset += name_length;
}

void FunctionMember::DumpMangledNamespaceAndMethod(char *buf, int &offset, const char *nspace, const char *mname)
{
    static const char *begin_mangled = "_ZN";
    static const char *end_mangled = "Ev";
    static const int begin_mangled_len = strlen(begin_mangled);
    static const int end_mangled_len = strlen(end_mangled);

    if (buf)
        strncpy(buf + offset, begin_mangled, begin_mangled_len);
    offset += begin_mangled_len;

    MangleName(buf, offset, nspace);
    MangleName(buf, offset, mname);

    if (buf)
        strncpy(buf + offset, end_mangled, end_mangled_len);
    offset += end_mangled_len;

    if (buf)
        buf[offset] = '\0';
    ++offset;
}

void FunctionMember::DumpLinkageName(char* ptr, int& offset)
{
    SString namespaceOrClassName;
    SString methodName;

    md->GetMethodInfoNoSig(namespaceOrClassName, methodName);
    SString utf8namespaceOrClassName;
    SString utf8methodName;
    namespaceOrClassName.ConvertToUTF8(utf8namespaceOrClassName);
    methodName.ConvertToUTF8(utf8methodName);

    const char *nspace = utf8namespaceOrClassName.GetUTF8NoConvert();
    const char *mname = utf8methodName.GetUTF8NoConvert();

    if (!nspace || !mname)
    {
        m_linkage_name_offset = 0;
        return;
    }

    m_linkage_name_offset = offset;
    DumpMangledNamespaceAndMethod(ptr, offset, nspace, mname);
}

void FunctionMember::DumpStrings(char* ptr, int& offset)
{
    TypeMember::DumpStrings(ptr, offset);

    for (int i = 0; i < m_num_vars; ++i)
    {
        vars[i].DumpStrings(ptr, offset);
    }

    DumpLinkageName(ptr, offset);
}

bool FunctionMember::GetBlockInNativeCode(int blockILOffset, int blockILLen, TADDR *startOffset, TADDR *endOffset)
{
    PCODE pCode = md->GetNativeCode();

    const int blockILEnd = blockILOffset + blockILLen;

    *startOffset = 0;
    *endOffset = 0;

    bool inBlock = false;

    for (int i = 0; i < nlines; ++i)
    {
        TADDR nativeOffset = lines[i].nativeOffset + pCode;

        // Limit block search to current function addresses
        if (nativeOffset < m_sub_low_pc)
            continue;
        if (nativeOffset >= m_sub_low_pc + m_sub_high_pc)
            break;

        // Skip invalid IL offsets
        switch(lines[i].ilOffset)
        {
            case ICorDebugInfo::PROLOG:
            case ICorDebugInfo::EPILOG:
            case ICorDebugInfo::NO_MAPPING:
                continue;
            default:
                break;
        }

        // Check if current IL is within block
        if (blockILOffset <= lines[i].ilOffset && lines[i].ilOffset < blockILEnd)
        {
            if (!inBlock)
            {
                *startOffset = lines[i].nativeOffset;
                inBlock = true;
            }
        }
        else
        {
            if (inBlock)
            {
                *endOffset = lines[i].nativeOffset;
                inBlock = false;
                break;
            }
        }
    }

    if (inBlock)
    {
        *endOffset = m_sub_low_pc + m_sub_high_pc - pCode;
    }

    return *endOffset != *startOffset;
}

void FunctionMember::DumpTryCatchBlock(char* ptr, int& offset, int ilOffset, int ilLen, int abbrev)
{
    TADDR startOffset;
    TADDR endOffset;

    if (!GetBlockInNativeCode(ilOffset, ilLen, &startOffset, &endOffset))
        return;

    if (ptr != nullptr)
    {
        DebugInfoTryCatchSub subEntry;

        subEntry.m_sub_abbrev = abbrev;
        subEntry.m_sub_low_pc = md->GetNativeCode() + startOffset;
        subEntry.m_sub_high_pc = endOffset - startOffset;

        memcpy(ptr + offset, &subEntry, sizeof(DebugInfoTryCatchSub));
    }
    offset += sizeof(DebugInfoTryCatchSub);
}

void FunctionMember::DumpTryCatchDebugInfo(char* ptr, int& offset)
{
    if (!md)
        return;

    COR_ILMETHOD *pHeader = md->GetILHeader();
    COR_ILMETHOD_DECODER header(pHeader);

    unsigned ehCount = header.EHCount();

    for (unsigned e = 0; e < ehCount; e++)
    {
        IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT ehBuff;
        const IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT* ehInfo;

        ehInfo = header.EH->EHClause(e, &ehBuff);

        DumpTryCatchBlock(ptr, offset, ehInfo->TryOffset, ehInfo->TryLength, 16);
        DumpTryCatchBlock(ptr, offset, ehInfo->HandlerOffset, ehInfo->HandlerLength, 17);
    }
}

void FunctionMember::DumpDebugInfo(char* ptr, int& offset)
{
    if (ptr != nullptr)
    {
        DebugInfoSub subEntry;

        subEntry.m_sub_abbrev = 4;
        subEntry.m_sub_name = m_member_name_offset;
        subEntry.m_linkage_name = m_linkage_name_offset;
        subEntry.m_file = m_file;
        subEntry.m_line = m_line;
        subEntry.m_sub_type = m_member_type->m_type_offset;
        subEntry.m_sub_low_pc = m_sub_low_pc;
        subEntry.m_sub_high_pc = m_sub_high_pc;
        subEntry.m_sub_loc[0] = m_sub_loc[0];
        subEntry.m_sub_loc[1] = m_sub_loc[1];

        if (!md->IsStatic())
        {
            DebugInfoSubMember subMemberEntry;
            subEntry.m_sub_abbrev = 12;
            subMemberEntry.sub = subEntry;
            subMemberEntry.m_obj_ptr = offset+sizeof(DebugInfoSubMember);
            memcpy(ptr + offset, &subMemberEntry, sizeof(DebugInfoSubMember));
        }
        else
        {
            memcpy(ptr + offset, &subEntry, sizeof(DebugInfoSub));
        }
        m_entry_offset = offset;
        dumped = true;
    }

    if (!md->IsStatic())
    {
        offset += sizeof(DebugInfoSubMember);
    }
    else
    {
        offset += sizeof(DebugInfoSub);
    }
    for (int i = 0; i < m_num_vars; ++i)
    {
        vars[i].DumpDebugInfo(ptr, offset);
    }

    DumpTryCatchDebugInfo(ptr, offset);

    // terminate children
    if (ptr != nullptr)
    {
        ptr[offset] = 0;
    }
    offset++;
}

int FunctionMember::GetArgsAndLocalsLen()
{
    int locSize = 0;
    char tmpBuf[16];

    // Format for DWARF location expression: [expression length][operation][offset in SLEB128 encoding]
    for (int i = 0; i < m_num_vars; i++)
    {
        locSize += 2; // First byte contains expression length, second byte contains operation (DW_OP_fbreg).
        locSize += Leb128Encode(static_cast<int32_t>(vars[i].m_native_offset), tmpBuf, sizeof(tmpBuf));
    }
    return locSize;
}

void ClassTypeInfo::DumpStrings(char* ptr, int& offset)
{
    TypeInfoBase::DumpStrings(ptr, offset);

    for (int i = 0; i < m_num_members; ++i)
    {
        members[i].DumpStrings(ptr, offset);
    }
}

void RefTypeInfo::DumpStrings(char* ptr, int& offset)
{
    TypeInfoBase::DumpStrings(ptr, offset);
    m_value_type->DumpStrings(ptr, offset);
}

void RefTypeInfo::DumpDebugInfo(char* ptr, int& offset)
{
    if (m_type_offset != 0)
    {
        return;
    }
    m_type_offset = offset;
    offset += sizeof(DebugInfoRefType);
    m_value_type->DumpDebugInfo(ptr, offset);
    if (ptr != nullptr)
    {
        DebugInfoRefType refType;
        refType.m_type_abbrev = 9;
        refType.m_ref_type = m_value_type->m_type_offset;
        refType.m_byte_size = m_type_size;
        memcpy(ptr + m_type_offset, &refType, sizeof(DebugInfoRefType));
    }
    else
    {
        m_type_offset = 0;
    }
}
void ClassTypeInfo::DumpDebugInfo(char* ptr, int& offset)
{
    if (m_type_offset != 0)
    {
        return;
    }
    // make sure that types of all members are dumped
    for (int i = 0; i < m_num_members; ++i)
    {
        if (members[i].m_member_type->m_type_offset == 0 && members[i].m_member_type != this)
        {
            members[i].m_member_type->DumpDebugInfo(ptr, offset);
        }
    }

    if (ptr != nullptr)
    {
        DebugInfoClassType bufType;
        bufType.m_type_abbrev = 7;
        bufType.m_type_name = m_type_name_offset;
        bufType.m_byte_size = m_type_size;

        memcpy(ptr + offset, &bufType, sizeof(DebugInfoClassType));
        m_type_offset = offset;
    }
    offset += sizeof(DebugInfoClassType);

    for (int i = 0; i < m_num_members; ++i)
    {
        members[i].DumpDebugInfo(ptr, offset);
    }

    for (int i = 0; i < method.GetCount(); ++i)
    {
        if (method[i]->md->GetMethodTable() == GetTypeHandle().GetMethodTable())
        {
            // our method is part of this class, we should dump it now before terminating members
            method[i]->DumpDebugInfo(ptr, offset);
        }
    }

    // members terminator
    if (ptr != nullptr)
    {
        ptr[offset] = 0;
    }
    offset++;

    for (int i = 0; i < m_num_members; ++i)
    {
        if (members[i].m_static_member_address != 0)
            members[i].DumpStaticDebugInfo(ptr, offset);
    }

}

void ArrayTypeInfo::DumpDebugInfo(char* ptr, int& offset)
{
    if (m_type_offset != 0)
    {
        return;
    }
    if (m_elem_type->m_type_offset == 0)
    {
        m_elem_type->DumpDebugInfo(ptr, offset);
    }
    if (ptr != nullptr)
    {
        DebugInfoArrayType arrType;

        arrType.m_abbrev = 10; // DW_TAG_array_type abbrev
        arrType.m_type = m_elem_type->m_type_offset;

        memcpy(ptr + offset, &arrType, sizeof(DebugInfoArrayType));
        m_type_offset = offset;
    }
    offset += sizeof(DebugInfoArrayType);

    char tmp[16] = { 0 };
    int len = Leb128Encode(static_cast<int32_t>(m_count_offset), tmp, sizeof(tmp));
    if (ptr != nullptr)
    {
        char buf[64];
        buf[0] = 11; // DW_TAG_subrange_type abbrev
        buf[1] = len + 3;
        buf[2] = DW_OP_push_object_address;
        buf[3] = DW_OP_plus_uconst;
        for (int j = 0; j < len; j++)
        {
            buf[j + 4] = tmp[j];
        }
        buf[len + 4] = DW_OP_deref;

        memcpy(ptr + offset, buf, len + 5);
    }
    offset += (len + 5);

    if (ptr != nullptr)
    {
        memset(ptr + offset, 0, 1);
    }
    offset += 1;
}

void VarDebugInfo::DumpStrings(char *ptr, int& offset)
{
    if (ptr != nullptr)
    {
        strcpy(ptr + offset, m_var_name);
        m_var_name_offset = offset;
    }
    offset += strlen(m_var_name) + 1;
}

void VarDebugInfo::DumpDebugInfo(char* ptr, int& offset)
{
    char bufVarLoc[16];
    int len = GetFrameLocation(m_native_offset, bufVarLoc);
    if (ptr != nullptr)
    {
        DebugInfoVar bufVar;

        bufVar.m_var_abbrev = m_var_abbrev;
        bufVar.m_var_name = m_var_name_offset;
        bufVar.m_var_file = 1;
        bufVar.m_var_line = 1;
        bufVar.m_var_type = m_var_type->m_type_offset;
        memcpy(ptr + offset, &bufVar, sizeof(DebugInfoVar));
        memcpy(ptr + offset + sizeof(DebugInfoVar), bufVarLoc, len);
    }
    offset += sizeof(DebugInfoVar);
    offset += len;
}

/* static data for symbol strings */
struct Elf_Symbol {
    const char* m_name;
    int m_off;
    TADDR m_value;
    int m_section, m_size;
    bool m_releaseName;
    Elf_Symbol() : m_name(nullptr), m_off(0), m_value(0), m_section(0), m_size(0), m_releaseName(false) {}
    ~Elf_Symbol()
    {
        if (m_releaseName)
            delete [] m_name;
    }
};

static int countFuncs(const SymbolsInfo *lines, int nlines)
{
    int count = 0;
    for (int i = 0; i < nlines; i++) {
        if (lines[i].ilOffset == ICorDebugInfo::PROLOG)
        {
            count++;
        }
    }
    return count;
}

static int getNextPrologueIndex(int from, const SymbolsInfo *lines, int nlines)
{
    for (int i = from; i < nlines; ++i) {
        if (lines[i].ilOffset == ICorDebugInfo::PROLOG)
        {
            return i;
        }
    }
    return -1;
}

int SymbolCount = 0;
NewArrayHolder<Elf_Symbol> SymbolNames;
NotifyGdb::AddrSet codeAddrs;

/* Create ELF/DWARF debug info for jitted method */
void NotifyGdb::MethodCompiled(MethodDesc* MethodDescPtr)
{
    PCODE pCode = MethodDescPtr->GetNativeCode();
    if (pCode == NULL)
        return;
    unsigned int symInfoLen = 0;
    NewArrayHolder<SymbolsInfo> symInfo = nullptr;
    LocalsInfo locals;

    /* Get method name & size of jitted code */
    LPCUTF8 methodName = MethodDescPtr->GetName();
    EECodeInfo codeInfo(pCode);
    TADDR codeSize = codeInfo.GetCodeManager()->GetFunctionSize(codeInfo.GetGCInfoToken());

    pCode = PCODEToPINSTR(pCode);

    /* Get module name */
    const Module* mod = MethodDescPtr->GetMethodTable()->GetModule();
    SString modName = mod->GetFile()->GetPath();
    StackScratchBuffer scratch;
    const char* szModName = modName.GetUTF8(scratch);
    const char *szModulePath, *szModuleFile;
    SplitPathname(szModName, szModulePath, szModuleFile);


    int length = MultiByteToWideChar(CP_UTF8, 0, szModuleFile, -1, NULL, 0);
    if (length == 0)
        return;
    NewArrayHolder<WCHAR> wszModuleFile = new (nothrow) WCHAR[length+1];
    length = MultiByteToWideChar(CP_UTF8, 0, szModuleFile, -1, wszModuleFile, length);

    if (length == 0)
        return;

    static NewArrayHolder<WCHAR> wszModuleNames = nullptr;
    DWORD cCharsNeeded = 0;

    // Get names of interesting modules from environment
    if (wszModuleNames == nullptr)
    {
        cCharsNeeded = GetEnvironmentVariableW(W("CORECLR_GDBJIT"), NULL, 0);

        if((cCharsNeeded == 0) || (cCharsNeeded >= MAX_LONGPATH))
            return;
        wszModuleNames = new WCHAR[cCharsNeeded+1];
        cCharsNeeded = GetEnvironmentVariableW(W("CORECLR_GDBJIT"), wszModuleNames, cCharsNeeded);
        if(cCharsNeeded == 0)
            return;
    }
    else
    {
        cCharsNeeded = wcslen(wszModuleNames);
    }

    BOOL isUserDebug = FALSE;

    NewArrayHolder<WCHAR> wszModuleName = new WCHAR[cCharsNeeded+1];
    LPWSTR pComma = wcsstr(wszModuleNames, W(","));
    LPWSTR tmp = wszModuleNames;

    while (pComma != NULL)
    {
        wcsncpy(wszModuleName, tmp, pComma - tmp);
        wszModuleName[pComma - tmp] = W('\0');

        if (wcscmp(wszModuleName, wszModuleFile) == 0)
        {
            isUserDebug = TRUE;
            break;
        }
        tmp = pComma + 1;
        pComma = wcsstr(tmp, W(","));
    }
    if (isUserDebug == FALSE)
    {
        wcsncpy(wszModuleName, tmp, wcslen(tmp));
        wszModuleName[wcslen(tmp)] = W('\0');
        if (wcscmp(wszModuleName, wszModuleFile) == 0)
        {
            isUserDebug = TRUE;
        }
    }

    if (isUserDebug == FALSE)
    {
        return;
    }

    NewHolder<TK_TypeInfoMap> pTypeMap = new TK_TypeInfoMap();

    if (pTypeMap == nullptr)
    {
        return;
    }

    /* Get debug info for method from portable PDB */
    HRESULT hr = GetDebugInfoFromPDB(MethodDescPtr, &symInfo, symInfoLen, locals);
    if (FAILED(hr) || symInfoLen == 0)
    {
        return;
    }

    int method_count = countFuncs(symInfo, symInfoLen);
    if (!method.Alloc(method_count)) {
        return;
    }

    CodeHeader* pCH = (CodeHeader*)pCode - 1;
    CalledMethod* pCalledMethods = reinterpret_cast<CalledMethod*>(pCH->GetCalledMethods());
    /* Collect addresses of thunks called by method */
    if (!CollectCalledMethods(pCalledMethods, (TADDR)MethodDescPtr->GetNativeCode()))
    {
        return;
    }
    pCH->SetCalledMethods(NULL);

    MetaSig sig(MethodDescPtr);
    int nArgsCount = sig.NumFixedArgs();
    if (sig.HasThis())
        nArgsCount++;

    unsigned int firstLineIndex = 0;
    for (;firstLineIndex < symInfoLen; firstLineIndex++) {
        if (symInfo[firstLineIndex].lineNumber != 0 && symInfo[firstLineIndex].lineNumber != HiddenLine) break;
    }

    if (firstLineIndex >= symInfoLen)
    {
        return;
    }

    int start_index = getNextPrologueIndex(0, symInfo, symInfoLen);

    for (int method_index = 0; method_index < method.GetCount(); ++method_index)
    {
        method[method_index] = new FunctionMember(MethodDescPtr, locals.size, nArgsCount);

        int end_index = getNextPrologueIndex(start_index + 1, symInfo, symInfoLen);

        PCODE method_start = symInfo[start_index].nativeOffset;
        TADDR method_size = end_index == -1 ? codeSize - method_start : symInfo[end_index].nativeOffset - method_start;

        // method return type
        method[method_index]->m_member_type = GetArgTypeInfo(MethodDescPtr, pTypeMap, 0);
        method[method_index]->GetLocalsDebugInfo(pTypeMap, locals, symInfo[firstLineIndex].nativeOffset);
        method[method_index]->m_sub_low_pc = pCode + method_start;
        method[method_index]->m_sub_high_pc = method_size;
        size_t methodNameSize = strlen(methodName) + 10;
        method[method_index]->m_member_name = new char[methodNameSize];
        if (method_index == 0)
            sprintf_s(method[method_index]->m_member_name, methodNameSize, "%s", methodName);
        else
            sprintf_s(method[method_index]->m_member_name, methodNameSize, "%s_%i", methodName, method_index);

        // method's class
        GetTypeInfoFromTypeHandle(TypeHandle(method[method_index]->md->GetMethodTable()), pTypeMap);

        start_index = end_index;
    }

    MemBuf elfHeader, sectHeaders, sectStr, sectSymTab, sectStrTab, dbgInfo, dbgAbbrev, dbgPubname, dbgPubType, dbgLine,
        dbgStr, elfFile;

    /* Build .debug_abbrev section */
    if (!BuildDebugAbbrev(dbgAbbrev))
    {
        return;
    }

    /* Build .debug_line section */
    if (!BuildLineTable(dbgLine, pCode, codeSize, symInfo, symInfoLen))
    {
        return;
    }
    
    DebugStrings[1] = szModuleFile;
    
    /* Build .debug_str section */
    if (!BuildDebugStrings(dbgStr, pTypeMap))
    {
        return;
    }
    
    /* Build .debug_info section */
    if (!BuildDebugInfo(dbgInfo, pTypeMap, symInfo, symInfoLen))
    {
        return;
    }

    for (int i = 0; i < locals.size; i++)
    {
        delete[] locals.localsName[i];
    }
    /* Build .debug_pubname section */
    if (!BuildDebugPub(dbgPubname, methodName, dbgInfo.MemSize, 0x28))
    {
        return;
    }
    
    /* Build debug_pubtype section */
    if (!BuildDebugPub(dbgPubType, "int", dbgInfo.MemSize, 0x1a))
    {
        return;
    }
    
    /* Build .strtab section */
    SymbolNames[0].m_name = "";
    for (int i = 0; i < method.GetCount(); ++i)
    {
        SymbolNames[1 + i].m_name = method[i]->m_member_name;
        SymbolNames[1 + i].m_value = method[i]->m_sub_low_pc;
        SymbolNames[1 + i].m_section = 1;
        SymbolNames[1 + i].m_size = method[i]->m_sub_high_pc;
    }
    if (!BuildStringTableSection(sectStrTab))
    {
        return;
    }
    /* Build .symtab section */
    if (!BuildSymbolTableSection(sectSymTab, pCode, codeSize))
    {
        return;
    }

    /* Build section headers table and section names table */
    if (!BuildSectionTables(sectHeaders, sectStr))
    {
        return;
    }

    /* Patch section offsets & sizes */
    long offset = sizeof(Elf_Ehdr);
    Elf_Shdr* pShdr = reinterpret_cast<Elf_Shdr*>(sectHeaders.MemPtr.GetValue());
    ++pShdr; // .text
    pShdr->sh_addr = pCode;
    pShdr->sh_size = codeSize;
    ++pShdr; // .shstrtab
    pShdr->sh_offset = offset;
    pShdr->sh_size = sectStr.MemSize;
    offset += sectStr.MemSize;
    ++pShdr; // .debug_str
    pShdr->sh_offset = offset;
    pShdr->sh_size = dbgStr.MemSize;
    offset += dbgStr.MemSize;
    ++pShdr; // .debug_abbrev
    pShdr->sh_offset = offset;
    pShdr->sh_size = dbgAbbrev.MemSize;
    offset += dbgAbbrev.MemSize;
    ++pShdr; // .debug_info
    pShdr->sh_offset = offset;
    pShdr->sh_size = dbgInfo.MemSize;
    offset += dbgInfo.MemSize;
    ++pShdr; // .debug_pubnames
    pShdr->sh_offset = offset;
    pShdr->sh_size = dbgPubname.MemSize;
    offset += dbgPubname.MemSize;
    ++pShdr; // .debug_pubtypes
    pShdr->sh_offset = offset;
    pShdr->sh_size = dbgPubType.MemSize;
    offset += dbgPubType.MemSize;
    ++pShdr; // .debug_line
    pShdr->sh_offset = offset;
    pShdr->sh_size = dbgLine.MemSize;
    offset += dbgLine.MemSize;
    ++pShdr; // .symtab
    pShdr->sh_offset = offset;
    pShdr->sh_size = sectSymTab.MemSize;
    pShdr->sh_link = GetSectionIndex(".strtab");
    offset += sectSymTab.MemSize;
    ++pShdr; // .strtab
    pShdr->sh_offset = offset;
    pShdr->sh_size = sectStrTab.MemSize;
    offset += sectStrTab.MemSize;

    // .thunks
    for (int i = 1 + method.GetCount(); i < SymbolCount; i++)
    {
        ++pShdr;
        pShdr->sh_addr = PCODEToPINSTR(SymbolNames[i].m_value);
        pShdr->sh_size = 8;
    }

    /* Build ELF header */
    if (!BuildELFHeader(elfHeader))
    {
        return;
    }
    Elf_Ehdr* header = reinterpret_cast<Elf_Ehdr*>(elfHeader.MemPtr.GetValue());
#ifdef _TARGET_ARM_
    header->e_flags = EF_ARM_EABI_VER5;
#ifdef ARM_SOFTFP
    header->e_flags |= EF_ARM_SOFT_FLOAT;
#else    
    header->e_flags |= EF_ARM_VFP_FLOAT;
#endif
#endif    
    header->e_shoff = offset;
    header->e_shentsize = sizeof(Elf_Shdr);
    int thunks_count = SymbolCount - method.GetCount() - 1;
    header->e_shnum = SectionNamesCount + thunks_count;
    header->e_shstrndx = GetSectionIndex(".shstrtab");

    /* Build ELF image in memory */
    elfFile.MemSize = elfHeader.MemSize + sectStr.MemSize + dbgStr.MemSize + dbgAbbrev.MemSize + dbgInfo.MemSize +
                      dbgPubname.MemSize + dbgPubType.MemSize + dbgLine.MemSize + sectSymTab.MemSize +
                      sectStrTab.MemSize + sectHeaders.MemSize;
    elfFile.MemPtr =  new (nothrow) char[elfFile.MemSize];
    if (elfFile.MemPtr == nullptr)
    {
        return;
    }
    
    /* Copy section data */
    offset = 0;
    memcpy(elfFile.MemPtr, elfHeader.MemPtr, elfHeader.MemSize);
    offset += elfHeader.MemSize;
    memcpy(elfFile.MemPtr + offset, sectStr.MemPtr, sectStr.MemSize);
    offset +=  sectStr.MemSize;
    memcpy(elfFile.MemPtr + offset, dbgStr.MemPtr, dbgStr.MemSize);
    offset +=  dbgStr.MemSize;
    memcpy(elfFile.MemPtr + offset, dbgAbbrev.MemPtr, dbgAbbrev.MemSize);
    offset +=  dbgAbbrev.MemSize;
    memcpy(elfFile.MemPtr + offset, dbgInfo.MemPtr, dbgInfo.MemSize);
    offset +=  dbgInfo.MemSize;
    memcpy(elfFile.MemPtr + offset, dbgPubname.MemPtr, dbgPubname.MemSize);
    offset +=  dbgPubname.MemSize;
    memcpy(elfFile.MemPtr + offset, dbgPubType.MemPtr, dbgPubType.MemSize);
    offset +=  dbgPubType.MemSize;
    memcpy(elfFile.MemPtr + offset, dbgLine.MemPtr, dbgLine.MemSize);
    offset +=  dbgLine.MemSize;
    memcpy(elfFile.MemPtr + offset, sectSymTab.MemPtr, sectSymTab.MemSize);
    offset +=  sectSymTab.MemSize;
    memcpy(elfFile.MemPtr + offset, sectStrTab.MemPtr, sectStrTab.MemSize);
    offset +=  sectStrTab.MemSize;

    memcpy(elfFile.MemPtr + offset, sectHeaders.MemPtr, sectHeaders.MemSize);

    elfFile.MemPtr.SuppressRelease();

#ifdef GDBJIT_DUMPELF
    DumpElf(methodName, elfFile);
#endif

    /* Create GDB JIT structures */
    NewHolder<jit_code_entry> jit_symbols = new (nothrow) jit_code_entry;
    
    if (jit_symbols == nullptr)
    {
        return;
    }
    
    /* Fill the new entry */
    jit_symbols->next_entry = jit_symbols->prev_entry = 0;
    jit_symbols->symfile_addr = elfFile.MemPtr;
    jit_symbols->symfile_size = elfFile.MemSize;
    
    /* Link into list */
    jit_code_entry *head = __jit_debug_descriptor.first_entry;
    __jit_debug_descriptor.first_entry = jit_symbols;
    if (head != 0)
    {
        jit_symbols->next_entry = head;
        head->prev_entry = jit_symbols;
    }
    
    jit_symbols.SuppressRelease();

    /* Notify the debugger */
    __jit_debug_descriptor.relevant_entry = jit_symbols;
    __jit_debug_descriptor.action_flag = JIT_REGISTER_FN;
    __jit_debug_register_code();
}

void NotifyGdb::MethodDropped(MethodDesc* MethodDescPtr)
{
    static const int textSectionIndex = GetSectionIndex(".text");

    if (textSectionIndex < 0)
        return;

    PCODE pCode = MethodDescPtr->GetNativeCode();

    if (pCode == NULL)
        return;
    
    /* Find relevant entry */
    for (jit_code_entry* jit_symbols = __jit_debug_descriptor.first_entry; jit_symbols != 0; jit_symbols = jit_symbols->next_entry)
    {
        const char* ptr = jit_symbols->symfile_addr;
        uint64_t size = jit_symbols->symfile_size;
        
        const Elf_Ehdr* pEhdr = reinterpret_cast<const Elf_Ehdr*>(ptr);
        const Elf_Shdr* pShdr = reinterpret_cast<const Elf_Shdr*>(ptr + pEhdr->e_shoff);
        pShdr += textSectionIndex; // bump to .text section
        if (pShdr->sh_addr == pCode)
        {
            /* Notify the debugger */
            __jit_debug_descriptor.relevant_entry = jit_symbols;
            __jit_debug_descriptor.action_flag = JIT_UNREGISTER_FN;
            __jit_debug_register_code();
            
            /* Free memory */
            delete[] ptr;
            
            /* Unlink from list */
            if (jit_symbols->prev_entry == 0)
                __jit_debug_descriptor.first_entry = jit_symbols->next_entry;
            else
                jit_symbols->prev_entry->next_entry = jit_symbols->next_entry;
            delete jit_symbols;
            break;
        }
    }
}

/* Build the DWARF .debug_line section */
bool NotifyGdb::BuildLineTable(MemBuf& buf, PCODE startAddr, TADDR codeSize, SymbolsInfo* lines, unsigned nlines)
{
    MemBuf fileTable, lineProg;
    
    /* Build file table */
    if (!BuildFileTable(fileTable, lines, nlines))
        return false;
    /* Build line info program */ 
    if (!BuildLineProg(lineProg, startAddr, codeSize, lines, nlines))
    {
        return false;
    }
    
    buf.MemSize = sizeof(DwarfLineNumHeader) + 1 + fileTable.MemSize + lineProg.MemSize;
    buf.MemPtr = new (nothrow) char[buf.MemSize];
    
    if (buf.MemPtr == nullptr)
    {
        return false;
    }
    
    /* Fill the line info header */
    DwarfLineNumHeader* header = reinterpret_cast<DwarfLineNumHeader*>(buf.MemPtr.GetValue());
    memcpy(buf.MemPtr, &LineNumHeader, sizeof(DwarfLineNumHeader));
    header->m_length = buf.MemSize - sizeof(uint32_t);
    header->m_hdr_length = sizeof(DwarfLineNumHeader) + 1 + fileTable.MemSize - 2 * sizeof(uint32_t) - sizeof(uint16_t);
    buf.MemPtr[sizeof(DwarfLineNumHeader)] = 0; // this is for missing directory table
    /* copy file table */
    memcpy(buf.MemPtr + sizeof(DwarfLineNumHeader) + 1, fileTable.MemPtr, fileTable.MemSize);
    /* copy line program */
    memcpy(buf.MemPtr + sizeof(DwarfLineNumHeader) + 1 + fileTable.MemSize, lineProg.MemPtr, lineProg.MemSize);

    return true;
}

/* Buid the source files table for DWARF source line info */
bool NotifyGdb::BuildFileTable(MemBuf& buf, SymbolsInfo* lines, unsigned nlines)
{
    NewArrayHolder<const char*> files = nullptr;
    unsigned nfiles = 0;
    
    /* GetValue file names and replace them with indices in file table */
    files = new (nothrow) const char*[nlines];
    if (files == nullptr)
        return false;
    for (unsigned i = 0; i < nlines; ++i)
    {
        if (lines[i].fileName[0] == 0)
            continue;
        const char *filePath, *fileName;
        SplitPathname(lines[i].fileName, filePath, fileName);

        /* if this isn't first then we already added file, so adjust index */
        lines[i].fileIndex = (nfiles) ? (nfiles - 1) : (nfiles);

        bool found = false;
        for (int j = 0; j < nfiles; ++j)
        {
            if (strcmp(fileName, files[j]) == 0)
            {
                found = true;
                break;
            }
        }
        
        /* add new source file */
        if (!found)
        {
            files[nfiles++] = fileName;
        }
    }
    
    /* build file table */
    unsigned totalSize = 0;
    
    for (unsigned i = 0; i < nfiles; ++i)
    {
        totalSize += strlen(files[i]) + 1 + 3;
    }
    totalSize += 1;
    
    buf.MemSize = totalSize;
    buf.MemPtr = new (nothrow) char[buf.MemSize];
    
    if (buf.MemPtr == nullptr)
    {
        return false;
    }
    
    /* copy collected file names */
    char *ptr = buf.MemPtr;
    for (unsigned i = 0; i < nfiles; ++i)
    {
        strcpy(ptr, files[i]);
        ptr += strlen(files[i]) + 1;
        // three LEB128 entries which we don't care
        *ptr++ = 0;
        *ptr++ = 0;
        *ptr++ = 0;
    }
    // final zero byte
    *ptr = 0;

    return true;
}

/* Command to set absolute address */
void NotifyGdb::IssueSetAddress(char*& ptr, PCODE addr)
{
    *ptr++ = 0;
    *ptr++ = ADDRESS_SIZE + 1;
    *ptr++ = DW_LNE_set_address;
    *reinterpret_cast<PCODE*>(ptr) = addr;
    ptr += ADDRESS_SIZE;
}

/* End of line program */
void NotifyGdb::IssueEndOfSequence(char*& ptr)
{
    *ptr++ = 0;
    *ptr++ = 1;
    *ptr++ = DW_LNE_end_sequence;
}

/* Command w/o parameters */
void NotifyGdb::IssueSimpleCommand(char*& ptr, uint8_t command)
{
    *ptr++ = command;
}

/* Command with one LEB128 parameter */
void NotifyGdb::IssueParamCommand(char*& ptr, uint8_t command, char* param, int param_size)
{
    *ptr++ = command;
    while (param_size-- > 0)
    {
        *ptr++ = *param++;
    }
}

static void fixLineMapping(SymbolsInfo* lines, unsigned nlines)
{
    // Fix EPILOGUE line mapping
    int prevLine = 0;
    for (int i = 0; i < nlines; ++i)
    {
        if (lines[i].lineNumber == HiddenLine)
            continue;
        if (lines[i].ilOffset == ICorDebugInfo::PROLOG) // will be fixed in next step
        {
            prevLine = 0;
        }
        else
        {
            if (lines[i].lineNumber == 0)
            {
                lines[i].lineNumber = prevLine;
            }
            else
            {
                prevLine = lines[i].lineNumber;
            }
        }
    }
    // Fix PROLOGUE line mapping
    prevLine = lines[nlines - 1].lineNumber;
    for (int i = nlines - 1; i >= 0; --i)
    {
        if (lines[i].lineNumber == HiddenLine)
            continue;
        if (lines[i].lineNumber == 0)
            lines[i].lineNumber = prevLine;
        else
            prevLine = lines[i].lineNumber;
    }
    // Skip HiddenLines
    for (int i = 0; i < nlines; ++i)
    {
        if (lines[i].lineNumber == HiddenLine)
        {
            lines[i].lineNumber = 0;
            if (i + 1 < nlines && lines[i + 1].ilOffset == ICorDebugInfo::NO_MAPPING)
                lines[i + 1].lineNumber = 0;
        }
    }
}

/* Build program for DWARF source line section */
bool NotifyGdb::BuildLineProg(MemBuf& buf, PCODE startAddr, TADDR codeSize, SymbolsInfo* lines, unsigned nlines)
{
    static char cnv_buf[16];
    
    /* reserve memory assuming worst case: set address, advance line command, set proglogue/epilogue and copy for each line */
    buf.MemSize =
                + 6                              /* set file command */
                + nlines * 6                     /* advance line commands */
                + nlines * (3 + ADDRESS_SIZE)    /* set address commands */
                + nlines * 1                     /* set prologue end or epilogue begin commands */
                + nlines * 1                     /* copy commands */
                + 6                              /* advance PC command */
                + 3;                             /* end of sequence command */
    buf.MemPtr = new (nothrow) char[buf.MemSize];
    char* ptr = buf.MemPtr;
  
    if (buf.MemPtr == nullptr)
        return false;

    fixLineMapping(lines, nlines);

    int prevLine = 1, prevFile = 0;

    for (int i = 0; i < nlines; ++i)
    {
        /* different source file */
        if (lines[i].fileIndex != prevFile)
        {
            int len = Leb128Encode(static_cast<uint32_t>(lines[i].fileIndex+1), cnv_buf, sizeof(cnv_buf));
            IssueParamCommand(ptr, DW_LNS_set_file, cnv_buf, len);
            prevFile = lines[i].fileIndex;
        }

        // GCC don't use the is_prologue_end flag to mark the first instruction after the prologue.
        // Instead of it it is issueing a line table entry for the first instruction of the prologue
        // and one for the first instruction after the prologue.
        // We do not want to confuse the debugger so we have to avoid adding a line in such case.
        if (i > 0 && lines[i - 1].nativeOffset == lines[i].nativeOffset)
            continue;

        IssueSetAddress(ptr, startAddr + lines[i].nativeOffset);

        if (lines[i].lineNumber != prevLine) {
            int len = Leb128Encode(static_cast<int32_t>(lines[i].lineNumber - prevLine), cnv_buf, sizeof(cnv_buf));
            IssueParamCommand(ptr, DW_LNS_advance_line, cnv_buf, len);
            prevLine = lines[i].lineNumber;
        }

        if (lines[i].ilOffset == ICorDebugInfo::EPILOG)
            IssueSimpleCommand(ptr, DW_LNS_set_epilogue_begin);
        else if (i > 0 && lines[i - 1].ilOffset == ICorDebugInfo::PROLOG)
            IssueSimpleCommand(ptr, DW_LNS_set_prologue_end);

        IssueParamCommand(ptr, DW_LNS_copy, NULL, 0);
    }

    int lastAddr = nlines > 0 ? lines[nlines - 1].nativeOffset : 0;

    // Advance PC to the end of function
    if (lastAddr < codeSize) {
        int len = Leb128Encode(static_cast<uint32_t>(codeSize - lastAddr), cnv_buf, sizeof(cnv_buf));
        IssueParamCommand(ptr, DW_LNS_advance_pc, cnv_buf, len);
    }

    IssueEndOfSequence(ptr); 
    
    buf.MemSize = ptr - buf.MemPtr;
    return true;
}

/* Build the DWARF .debug_str section */
bool NotifyGdb::BuildDebugStrings(MemBuf& buf, PTK_TypeInfoMap pTypeMap)
{
    int totalLength = 0;

    /* calculate total section size */
    for (int i = 0; i < DebugStringCount; ++i)
    {
        totalLength += strlen(DebugStrings[i]) + 1;
    }

    for (int i = 0; i < method.GetCount(); ++i)
    {
        method[i]->DumpStrings(nullptr, totalLength);
    }

    {
        auto iter = pTypeMap->Begin();
        while (iter != pTypeMap->End())
        {
            TypeInfoBase *typeInfo = iter->Value();
            typeInfo->DumpStrings(nullptr, totalLength);
            iter++;
        }
    }

    buf.MemSize = totalLength;
    buf.MemPtr = new (nothrow) char[totalLength];
    
    if (buf.MemPtr == nullptr)
        return false;

    /* copy strings */
    char* bufPtr = buf.MemPtr;
    int offset = 0;
    for (int i = 0; i < DebugStringCount; ++i)
    {
        strcpy(bufPtr + offset, DebugStrings[i]);
        offset += strlen(DebugStrings[i]) + 1;
    }

    for (int i = 0; i < method.GetCount(); ++i)
    {
        method[i]->DumpStrings(bufPtr, offset);
    }

    {
        auto iter = pTypeMap->Begin();
        while (iter != pTypeMap->End())
        {
            TypeInfoBase *typeInfo = iter->Value();
            typeInfo->DumpStrings(bufPtr, offset);
            iter++;
        }
    }

    return true;
}

/* Build the DWARF .debug_abbrev section */
bool NotifyGdb::BuildDebugAbbrev(MemBuf& buf)
{
    buf.MemPtr = new (nothrow) char[AbbrevTableSize];
    buf.MemSize = AbbrevTableSize;

    if (buf.MemPtr == nullptr)
        return false;
    
    memcpy(buf.MemPtr, AbbrevTable, AbbrevTableSize);
    return true;
}

/* Build tge DWARF .debug_info section */
bool NotifyGdb::BuildDebugInfo(MemBuf& buf, PTK_TypeInfoMap pTypeMap, SymbolsInfo* lines, unsigned nlines)
{
    int totalTypeVarSubSize = 0;
    {
        auto iter = pTypeMap->Begin();
        while (iter != pTypeMap->End())
        {
            TypeInfoBase *typeInfo = iter->Value();
            typeInfo->DumpDebugInfo(nullptr, totalTypeVarSubSize);
            iter++;
        }
    }

    for (int i = 0; i < method.GetCount(); ++i)
    {
        method[i]->lines = lines;
        method[i]->nlines = nlines;
        method[i]->DumpDebugInfo(nullptr, totalTypeVarSubSize);
    }
    // Drop pointers to lines when exiting current scope
    struct DropMethodLines
    {
        ~DropMethodLines()
        {
            for (int i = 0; i < method.GetCount(); ++i)
            {
                method[i]->lines = nullptr;
                method[i]->nlines = 0;
            }
        }
    } dropMethodLines;

    //int locSize = GetArgsAndLocalsLen(argsDebug, argsDebugSize, localsDebug, localsDebugSize);
    buf.MemSize = sizeof(DwarfCompUnit) + sizeof(DebugInfoCU) + totalTypeVarSubSize + 2;
    buf.MemPtr = new (nothrow) char[buf.MemSize];

    if (buf.MemPtr == nullptr)
        return false;
    int offset = 0;
    /* Compile uint header */
    DwarfCompUnit* cu = reinterpret_cast<DwarfCompUnit*>(buf.MemPtr.GetValue());
    cu->m_length = buf.MemSize - sizeof(uint32_t);
    cu->m_version = 4;
    cu->m_abbrev_offset = 0;
    cu->m_addr_size = ADDRESS_SIZE;
    offset += sizeof(DwarfCompUnit);
    DebugInfoCU* diCU =
       reinterpret_cast<DebugInfoCU*>(buf.MemPtr + offset);
    memcpy(buf.MemPtr + offset, &debugInfoCU, sizeof(DebugInfoCU));
    offset += sizeof(DebugInfoCU);
    diCU->m_prod_off = 0;
    diCU->m_cu_name = strlen(DebugStrings[0]) + 1;
    {
        auto iter = pTypeMap->Begin();
        while (iter != pTypeMap->End())
        {
            TypeInfoBase *typeInfo = iter->Value();
            typeInfo->DumpDebugInfo(buf.MemPtr, offset);
            iter++;
        }
    }
    for (int i = 0; i < method.GetCount(); ++i)
    {
        if (!method[i]->IsDumped())
        {
            method[i]->DumpDebugInfo(buf.MemPtr, offset);
        }
        else
        {
            method[i]->DumpDebugInfo(buf.MemPtr, method[i]->m_entry_offset);
        }
    }
    memset(buf.MemPtr + offset, 0, buf.MemSize - offset);
    return true;
}

/* Build the DWARF lookup section */
bool NotifyGdb::BuildDebugPub(MemBuf& buf, const char* name, uint32_t size, uint32_t die_offset)
{
    uint32_t length = sizeof(DwarfPubHeader) + sizeof(uint32_t) + strlen(name) + 1 + sizeof(uint32_t);
    
    buf.MemSize = length;
    buf.MemPtr = new (nothrow) char[buf.MemSize];
    
    if (buf.MemPtr == nullptr)
        return false;

    DwarfPubHeader* header = reinterpret_cast<DwarfPubHeader*>(buf.MemPtr.GetValue());
    header->m_length = length - sizeof(uint32_t);
    header->m_version = 2;
    header->m_debug_info_off = 0;
    header->m_debug_info_len = size;
    *reinterpret_cast<uint32_t*>(buf.MemPtr + sizeof(DwarfPubHeader)) = die_offset;
    strcpy(buf.MemPtr + sizeof(DwarfPubHeader) + sizeof(uint32_t), name);
    *reinterpret_cast<uint32_t*>(buf.MemPtr + length - sizeof(uint32_t)) = 0;
    
    return true;
}

/* Store addresses and names of the called methods into symbol table */
bool NotifyGdb::CollectCalledMethods(CalledMethod* pCalledMethods, TADDR nativeCode)
{
    AddrSet tmpCodeAddrs;

    if (!codeAddrs.Contains(nativeCode))
        codeAddrs.Add(nativeCode);

    CalledMethod* pList = pCalledMethods;

     /* count called methods */
    while (pList != NULL)
    {
        TADDR callAddr = (TADDR)pList->GetCallAddr();
        if (!tmpCodeAddrs.Contains(callAddr) && !codeAddrs.Contains(callAddr)) {
            tmpCodeAddrs.Add(callAddr);
        }
        pList = pList->GetNext();
    }

    SymbolCount = 1 + method.GetCount() + tmpCodeAddrs.GetCount();
    SymbolNames = new (nothrow) Elf_Symbol[SymbolCount];

    pList = pCalledMethods;
    int i = 1 + method.GetCount();
    while (i < SymbolCount && pList != NULL)
    {
        TADDR callAddr = (TADDR)pList->GetCallAddr();
        if (!codeAddrs.Contains(callAddr))
        {
            MethodDesc* pMD = pList->GetMethodDesc();
            LPCUTF8 methodName = pMD->GetName();
            int symbolNameLength = strlen(methodName) + sizeof("__thunk_");
            SymbolNames[i].m_name = new char[symbolNameLength];
            SymbolNames[i].m_releaseName = true;
            sprintf_s((char*)SymbolNames[i].m_name, symbolNameLength, "__thunk_%s", methodName);
            SymbolNames[i].m_value = callAddr;
            ++i;
            codeAddrs.Add(callAddr);
        }
        CalledMethod* ptr = pList;
        pList = pList->GetNext();
        delete ptr;
    }
    SymbolCount = i;
    return true;
}

/* Build ELF .strtab section */
bool NotifyGdb::BuildStringTableSection(MemBuf& buf)
{
    int len = 0;
    for (int i = 0; i < SymbolCount; ++i)
        len += strlen(SymbolNames[i].m_name) + 1;
    len++; // end table with zero-length string
    
    buf.MemSize = len;
    buf.MemPtr = new (nothrow) char[buf.MemSize];
    if (buf.MemPtr == nullptr)
        return false;
    char* ptr = buf.MemPtr;
    for (int i = 0; i < SymbolCount; ++i)
    {
        SymbolNames[i].m_off = ptr - buf.MemPtr;
        strcpy(ptr, SymbolNames[i].m_name);
        ptr += strlen(SymbolNames[i].m_name) + 1;
    }
    buf.MemPtr[buf.MemSize-1] = 0;
    
    return true;
}

/* Build ELF .symtab section */
bool NotifyGdb::BuildSymbolTableSection(MemBuf& buf, PCODE addr, TADDR codeSize)
{
    static const int textSectionIndex = GetSectionIndex(".text");

    buf.MemSize = SymbolCount * sizeof(Elf_Sym);
    buf.MemPtr = new (nothrow) char[buf.MemSize];
    if (buf.MemPtr == nullptr)
        return false;

    Elf_Sym *sym = reinterpret_cast<Elf_Sym*>(buf.MemPtr.GetValue());

    sym[0].st_name = 0;
    sym[0].st_info = 0;
    sym[0].st_other = 0;
    sym[0].st_value = 0;
    sym[0].st_size = 0;
    sym[0].st_shndx = SHN_UNDEF;
    
    for (int i = 1; i < 1 + method.GetCount(); ++i)
    {
        sym[i].st_name = SymbolNames[i].m_off;
        sym[i].setBindingAndType(STB_GLOBAL, STT_FUNC);
        sym[i].st_other = 0;
        sym[i].st_value = PINSTRToPCODE(SymbolNames[i].m_value - addr);
        sym[i].st_shndx = textSectionIndex;
        sym[i].st_size = SymbolNames[i].m_size;
    }

    for (int i = 1 + method.GetCount(); i < SymbolCount; ++i)
    {
        sym[i].st_name = SymbolNames[i].m_off;
        sym[i].setBindingAndType(STB_GLOBAL, STT_FUNC);
        sym[i].st_other = 0;
        sym[i].st_shndx = SectionNamesCount + (i - (1 + method.GetCount())); // .thunks section index
        sym[i].st_size = 8;
#ifdef _TARGET_ARM_
        sym[i].st_value = 1; // for THUMB code
#else
        sym[i].st_value = 0;
#endif
    }
    return true;
}

int NotifyGdb::GetSectionIndex(const char *sectName)
{
    for (int i = 0; i < SectionNamesCount; ++i)
        if (strcmp(SectionNames[i], sectName) == 0)
            return i;
    return -1;
}

/* Build the ELF section headers table and section names table */
bool NotifyGdb::BuildSectionTables(MemBuf& sectBuf, MemBuf& strBuf)
{
    static const int symtabSectionIndex = GetSectionIndex(".symtab");
    static const int nullSectionIndex = GetSectionIndex("");

    const int thunks_count = SymbolCount - 1 - method.GetCount();

    // Approximate length of single section name.
    // Used only to reduce memory reallocations.
    static const int SECT_NAME_LENGTH = 11;

    if (!strBuf.Resize(SECT_NAME_LENGTH * (SectionNamesCount + thunks_count)))
    {
        return false;
    }

    Elf_Shdr* sectionHeaders = new (nothrow) Elf_Shdr[SectionNamesCount + thunks_count];
    if (sectionHeaders == nullptr)
    {
        return false;
    }

    sectBuf.MemPtr = reinterpret_cast<char*>(sectionHeaders);
    sectBuf.MemSize = sizeof(Elf_Shdr) * (SectionNamesCount + thunks_count);

    Elf_Shdr* pSh = sectionHeaders;
    uint32_t sectNameOffset = 0;

    // Additional memory for remaining section names,
    // grows twice on each reallocation.
    int addSize = SECT_NAME_LENGTH;

    // Fill section headers and names
    for (int i = 0; i < SectionNamesCount + thunks_count; ++i, ++pSh)
    {
        char thunkSectNameBuf[256]; // temporary buffer for .thunk_# section name
        const char *sectName;

        bool isThunkSection = i >= SectionNamesCount;
        if (isThunkSection)
        {
            sprintf_s(thunkSectNameBuf, _countof(thunkSectNameBuf), ".thunk_%i", i);
            sectName = thunkSectNameBuf;
        }
        else
        {
            sectName = SectionNames[i];
        }

        // Ensure that there is enough memory for section name,
        // reallocate if necessary.
        pSh->sh_name = sectNameOffset;
        sectNameOffset += strlen(sectName) + 1;
        if (sectNameOffset > strBuf.MemSize)
        {
            // Allocate more memory for remaining section names
            if (!strBuf.Resize(sectNameOffset + addSize))
                return false;
            addSize *= 2;
        }

        strcpy(strBuf.MemPtr + pSh->sh_name, sectName);

        // All .thunk_* sections have the same type and flags
        int index = isThunkSection ? SectionNamesCount : i;
        pSh->sh_type = Sections[index].m_type;
        pSh->sh_flags = Sections[index].m_flags;

        pSh->sh_addr = 0;
        pSh->sh_offset = 0;
        pSh->sh_size = 0;
        pSh->sh_link = SHN_UNDEF;
        pSh->sh_info = 0;
        pSh->sh_addralign = i == nullSectionIndex ? 0 : 1;
        pSh->sh_entsize = i == symtabSectionIndex ? sizeof(Elf_Sym) : 0;
    }

    // Set actual used size to avoid garbage in ELF section
    strBuf.MemSize = sectNameOffset;
    return true;
}

/* Build the ELF header */
bool NotifyGdb::BuildELFHeader(MemBuf& buf)
{
    Elf_Ehdr* header = new (nothrow) Elf_Ehdr;

    if (header == nullptr)
    {
        return false;
    }

    buf.MemPtr = reinterpret_cast<char*>(header);
    buf.MemSize = sizeof(Elf_Ehdr);
    return true;
}

/* Split full path name into directory & file names */
void NotifyGdb::SplitPathname(const char* path, const char*& pathName, const char*& fileName)
{
    char* pSlash = strrchr(path, '/');
    
    if (pSlash != nullptr)
    {
        *pSlash = 0;
        fileName = ++pSlash;
        pathName = path;
    }
    else 
    {
        fileName = path;
        pathName = nullptr;
    }
}

#ifdef _DEBUG
void NotifyGdb::DumpElf(const char* methodName, const MemBuf& elfFile)
{
    char dump[1024];
    strcpy(dump, "./");
    strcat(dump, methodName);
    strcat(dump, ".o");
    FILE *f = fopen(dump,  "wb");
    fwrite(elfFile.MemPtr, sizeof(char),elfFile.MemSize, f);
    fclose(f);
}
#endif

/* ELF 32bit header */
Elf32_Ehdr::Elf32_Ehdr()
{
    e_ident[EI_MAG0] = ElfMagic[0];
    e_ident[EI_MAG1] = ElfMagic[1];
    e_ident[EI_MAG2] = ElfMagic[2];
    e_ident[EI_MAG3] = ElfMagic[3];
    e_ident[EI_CLASS] = ELFCLASS32;
    e_ident[EI_DATA] = ELFDATA2LSB;
    e_ident[EI_VERSION] = EV_CURRENT;
    e_ident[EI_OSABI] = ELFOSABI_NONE;
    e_ident[EI_ABIVERSION] = 0;
    for (int i = EI_PAD; i < EI_NIDENT; ++i)
        e_ident[i] = 0;

    e_type = ET_REL;
#if defined(_TARGET_X86_)
    e_machine = EM_386;
#elif defined(_TARGET_ARM_)
    e_machine = EM_ARM;
#endif    
    e_flags = 0;
    e_version = 1;
    e_entry = 0;
    e_phoff = 0;
    e_ehsize = sizeof(Elf32_Ehdr);
    e_phentsize = 0;
    e_phnum = 0;
}

/* ELF 64bit header */
Elf64_Ehdr::Elf64_Ehdr()
{
    e_ident[EI_MAG0] = ElfMagic[0];
    e_ident[EI_MAG1] = ElfMagic[1];
    e_ident[EI_MAG2] = ElfMagic[2];
    e_ident[EI_MAG3] = ElfMagic[3];
    e_ident[EI_CLASS] = ELFCLASS64;
    e_ident[EI_DATA] = ELFDATA2LSB;
    e_ident[EI_VERSION] = EV_CURRENT;
    e_ident[EI_OSABI] = ELFOSABI_NONE;
    e_ident[EI_ABIVERSION] = 0;
    for (int i = EI_PAD; i < EI_NIDENT; ++i)
        e_ident[i] = 0;

    e_type = ET_REL;
#if defined(_TARGET_AMD64_)
    e_machine = EM_X86_64;
#elif defined(_TARGET_ARM64_)
    e_machine = EM_AARCH64;
#endif
    e_flags = 0;
    e_version = 1;
    e_entry = 0;
    e_phoff = 0;
    e_ehsize = sizeof(Elf64_Ehdr);
    e_phentsize = 0;
    e_phnum = 0;
}
