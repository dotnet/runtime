// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Win32 Resource extractor
//
#include "ildasmpch.h"

#ifndef TARGET_UNIX
#include "debugmacros.h"
#include "corpriv.h"
#include "dasmenum.hpp"
#include "formattype.h"
#include "dis.h"
#include "resource.h"
#include "ilformatter.h"
#include "outstring.h"

#include "ceeload.h"
#include "dynamicarray.h"
extern IMAGE_COR20_HEADER *    g_CORHeader;
extern IMDInternalImport*      g_pImport;
extern PELoader * g_pPELoader;
extern IMetaDataImport2*        g_pPubImport;
extern char g_szAsmCodeIndent[];
extern unsigned g_uConsoleCP;

struct ResourceHeader
{
    DWORD   dwDataSize;
    DWORD   dwHeaderSize;
    DWORD   dwTypeID;
    DWORD   dwNameID;
    DWORD   dwDataVersion;
    WORD    wMemFlags;
    WORD    wLangID;
    DWORD   dwVersion;
    DWORD   dwCharacteristics;
    ResourceHeader()
    {
        memset(this,0,sizeof(ResourceHeader));
        dwHeaderSize = sizeof(ResourceHeader);
        dwTypeID = dwNameID = 0xFFFF;
    };
};

struct ResourceNode
{
    ResourceHeader  ResHdr;
    IMAGE_RESOURCE_DATA_ENTRY DataEntry;
    WCHAR* wzType;
    WCHAR* wzName;
    ResourceNode(DWORD tid, DWORD nid, DWORD lid, DWORD dataOffset, BYTE* ptrBase)
    {
        if(tid & 0x80000000)
        {
            ResHdr.dwTypeID = 0;
            tid &= 0x7FFFFFFF;
            WORD L = *((WORD*)(ptrBase+tid));
            wzType = new WCHAR[L+1];
            memcpy(wzType,ptrBase+tid+sizeof(WORD),L*sizeof(WCHAR));
            wzType[L]=0;
        }
        else
        {
            ResHdr.dwTypeID = (0xFFFF |((tid & 0xFFFF)<<16));
            wzType = NULL;
        }

        if(nid & 0x80000000)
        {
            ResHdr.dwNameID = 0;
            nid &= 0x7FFFFFFF;
            WORD L = *((WORD*)(ptrBase+nid));
            wzName = new WCHAR[L+1];
            memcpy(wzName, ptrBase+nid+sizeof(WORD), L*sizeof(WCHAR));
            wzName[L]=0;
        }
        else
        {
            ResHdr.dwNameID = (0xFFFF |((nid & 0xFFFF)<<16));
            wzName = NULL;
        }

        //ResHdr.dwTypeID = (tid & 0x80000000) ? tid : (0xFFFF |((tid & 0xFFFF)<<16));
        //ResHdr.dwNameID = (nid & 0x80000000) ? nid : (0xFFFF |((nid & 0xFFFF)<<16));
        ResHdr.wLangID = (WORD)lid;
        if(ptrBase) memcpy(&DataEntry,(ptrBase+dataOffset),sizeof(IMAGE_RESOURCE_DATA_ENTRY));
        ResHdr.dwDataSize = DataEntry.Size;
    };
    ~ResourceNode()
    {
        if(wzType) VDELETE(wzType);
        if(wzName) VDELETE(wzName);
    };
    void Save(FILE* pF)
    {
        // Dump them to pF
        BYTE* pbData;
        DWORD   dwFiller = 0;
        BYTE    bNil[3] = {0,0,0};
        // For each resource write header and data
        if(g_pPELoader->getVAforRVA(VAL32(DataEntry.OffsetToData), (void **) &pbData))
        {
            //fwrite(&(g_prResNodePtr[i]->ResHdr),g_prResNodePtr[i]->ResHdr.dwHeaderSize,1,pF);
            ResHdr.dwHeaderSize = sizeof(ResourceHeader);
            if(wzType) ResHdr.dwHeaderSize += (DWORD)((u16_strlen(wzType) + 1)*sizeof(WCHAR) - sizeof(DWORD));
            if(wzName) ResHdr.dwHeaderSize += (DWORD)((u16_strlen(wzName) + 1)*sizeof(WCHAR) - sizeof(DWORD));

            //---- Constant part of the header: DWORD,DWORD
            fwrite(&ResHdr.dwDataSize, sizeof(DWORD),1,pF);
            fwrite(&ResHdr.dwHeaderSize, sizeof(DWORD),1,pF);
            //--- Variable part of header: type and name
            if(wzType)
            {
                fwrite(wzType,(u16_strlen(wzType) + 1)*sizeof(WCHAR), 1, pF);
                dwFiller += (DWORD)u16_strlen(wzType) + 1;
            }
            else
                fwrite(&ResHdr.dwTypeID,sizeof(DWORD),1,pF);
            if(wzName)
            {
                fwrite(wzName,(u16_strlen(wzName) + 1)*sizeof(WCHAR), 1, pF);
                dwFiller += (DWORD)u16_strlen(wzName) + 1;
            }
            else
                fwrite(&ResHdr.dwNameID,sizeof(DWORD),1,pF);

            // Align remaining fields on DWORD
            if(dwFiller & 1)
                fwrite(bNil,2,1,pF);

            //---- Constant part of the header: DWORD,WORD,WORD,DWORD,DWORD
            fwrite(&ResHdr.dwDataVersion,8*sizeof(WORD),1,pF);
            //---- Header done, now data
            fwrite(pbData,VAL32(DataEntry.Size),1,pF);
            dwFiller = VAL32(DataEntry.Size) & 3;
            if(dwFiller)
            {
                dwFiller = 4 - dwFiller;
                fwrite(bNil,dwFiller,1,pF);
            }
        }
    };
};


#define RES_FILE_DUMP_ENABLED

DWORD   DumpResourceToFile(_In_ __nullterminated WCHAR*   wzFileName)
{

    BYTE*   pbResBase;
    FILE*   pF = NULL;
    DWORD ret = 0;
    DWORD   dwResDirRVA;
    DWORD   dwResDirSize;
    unsigned ulNumResNodes=0;
    DynamicArray<ResourceNode*> g_prResNodePtr;

    if (g_pPELoader->IsPE32())
    {
        IMAGE_OPTIONAL_HEADER32 *pOptHeader = &(g_pPELoader->ntHeaders32()->OptionalHeader);

        dwResDirRVA = VAL32(pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE].VirtualAddress);
        dwResDirSize = VAL32(pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE].Size);
    }
    else
    {
        IMAGE_OPTIONAL_HEADER64 *pOptHeader = &(g_pPELoader->ntHeaders64()->OptionalHeader);

        dwResDirRVA = VAL32(pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE].VirtualAddress);
        dwResDirSize = VAL32(pOptHeader->DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE].Size);
    }

    if(dwResDirRVA && dwResDirSize)
    {
        if(g_pPELoader->getVAforRVA(dwResDirRVA, (void **) &pbResBase))
        {
            // First, pull out all resource nodes (tree leaves), see ResourceNode struct
            PIMAGE_RESOURCE_DIRECTORY pirdType = (PIMAGE_RESOURCE_DIRECTORY)pbResBase;
            PIMAGE_RESOURCE_DIRECTORY_ENTRY pirdeType = (PIMAGE_RESOURCE_DIRECTORY_ENTRY)(pbResBase+sizeof(IMAGE_RESOURCE_DIRECTORY));
            DWORD	dwTypeID;
            unsigned i = 0,N = pirdType->NumberOfNamedEntries+pirdType->NumberOfIdEntries;
            PAL_CPP_TRY {
                for(i=0; i < N; i++, pirdeType++)
                {
                    dwTypeID = VAL32(IMAGE_RDE_NAME(pirdeType));
                    if(IMAGE_RDE_OFFSET_FIELD(pirdeType, DataIsDirectory))
                    {
                        BYTE*   pbNameBase = pbResBase + VAL32(IMAGE_RDE_OFFSET_FIELD(pirdeType, OffsetToDirectory));
                        PIMAGE_RESOURCE_DIRECTORY pirdName = (PIMAGE_RESOURCE_DIRECTORY)pbNameBase;
                        PIMAGE_RESOURCE_DIRECTORY_ENTRY pirdeName = (PIMAGE_RESOURCE_DIRECTORY_ENTRY)(pbNameBase+sizeof(IMAGE_RESOURCE_DIRECTORY));
                        DWORD   dwNameID;
                        unsigned i,N = VAL16(pirdName->NumberOfNamedEntries)+VAL16(pirdName->NumberOfIdEntries);

                        for(i=0; i < N; i++, pirdeName++)
                        {
                            dwNameID = VAL32(IMAGE_RDE_NAME(pirdeName));
                            if(IMAGE_RDE_OFFSET_FIELD(pirdeName, DataIsDirectory))
                            {
                                BYTE*   pbLangBase = pbResBase + VAL32(IMAGE_RDE_OFFSET_FIELD(pirdeName, OffsetToDirectory));
                                PIMAGE_RESOURCE_DIRECTORY pirdLang = (PIMAGE_RESOURCE_DIRECTORY)pbLangBase;
                                PIMAGE_RESOURCE_DIRECTORY_ENTRY pirdeLang = (PIMAGE_RESOURCE_DIRECTORY_ENTRY)(pbLangBase+sizeof(IMAGE_RESOURCE_DIRECTORY));
                                DWORD   dwLangID;
                                unsigned i,N = VAL16(pirdLang->NumberOfNamedEntries)+VAL16(pirdLang->NumberOfIdEntries);

                                for(i=0; i < N; i++, pirdeLang++)
                                {
                                    dwLangID = VAL32(IMAGE_RDE_NAME(pirdeLang));
                                    if(IMAGE_RDE_OFFSET_FIELD(pirdeLang, DataIsDirectory))
                                    {
                                        _ASSERTE(!"Resource hierarchy exceeds three levels");
                                    }
                                    else
                                    {
                                        g_prResNodePtr[ulNumResNodes++] = new ResourceNode(dwTypeID,dwNameID,dwLangID, VAL32(IMAGE_RDE_OFFSET(pirdeLang)),pbResBase);
                                    }
                                }
                            }
                            else
                            {
                                g_prResNodePtr[ulNumResNodes++] = new ResourceNode(dwTypeID,dwNameID,0,VAL32(IMAGE_RDE_OFFSET(pirdeName)),pbResBase);
                            }
                        }
                    }
                    else
                    {
                        g_prResNodePtr[ulNumResNodes++] = new ResourceNode(dwTypeID,0,0,VAL32(IMAGE_RDE_OFFSET(pirdeType)),pbResBase);
                    }
                }
            } PAL_CPP_CATCH_ALL {
                ret= 0xDFFFFFFF;
                ulNumResNodes = 0;
            }
            PAL_CPP_ENDTRY
            // OK, all tree leaves are in ResourceNode structs, and ulNumResNodes ptrs are in g_prResNodePtr
            if(ulNumResNodes)
            {
                ret = 1;
#ifdef RES_FILE_DUMP_ENABLED

                _wfopen_s(&pF,wzFileName,L"wb");
                if(pF)
                {
                    // Dump them to pF
                    // Write dummy header
                    ResourceHeader  *pRH = new ResourceHeader();
                    fwrite(pRH,sizeof(ResourceHeader),1,pF);
                    SDELETE(pRH);
                    // For each resource write header and data
                    PAL_CPP_TRY {
                        for(i=0; i < ulNumResNodes; i++)
                        {
                            /*
                            sprintf_s(szString,SZSTRING_SIZE,"// Res.# %d Type=0x%X Name=0x%X Lang=0x%X DataOffset=0x%X DataLength=%d",
                                i+1,
                                g_prResNodePtr[i]->ResHdr.dwTypeID,
                                g_prResNodePtr[i]->ResHdr.dwNameID,
                                g_prResNodePtr[i]->ResHdr.wLangID,
                                VAL32(g_prResNodePtr[i]->DataEntry.OffsetToData),
                                VAL32(g_prResNodePtr[i]->DataEntry.Size));
                            printLine(NULL,szString);
                            */
                            g_prResNodePtr[i]->Save(pF);
                            SDELETE(g_prResNodePtr[i]);
                        }
                    }
                    PAL_CPP_CATCH_ALL {
                        ret= 0xDFFFFFFF;
                    }
                    PAL_CPP_ENDTRY
                    fclose(pF);
                }// end if file opened
                else ret = 0xEFFFFFFF;
#else
                // Dump to text, using wzFileName as GUICookie
                //char szString[4096];
                void* GUICookie = (void*)wzFileName;
                BYTE* pbData;
                printLine(GUICookie,"");
                sprintf_s(szString, ARRAY_SIZE(szString), "// ========== Win32 Resource Entries (%d) ========",ulNumResNodes);
                for(i=0; i < ulNumResNodes; i++)
                {
                    printLine(GUICookie,"");
                    sprintf_s(szString, ARRAY_SIZE(szString), "// Res.# %d Type=0x%X Name=0x%X Lang=0x%X DataOffset=0x%X DataLength=%d",
                        i+1,
                        g_prResNodePtr[i]->ResHdr.dwTypeID,
                        g_prResNodePtr[i]->ResHdr.dwNameID,
                        g_prResNodePtr[i]->ResHdr.wLangID,
                        VAL32(g_prResNodePtr[i]->DataEntry.OffsetToData),
                        VAL32(g_prResNodePtr[i]->DataEntry.Size));
                    printLine(GUICookie,szString);
                    if(g_pPELoader->getVAforRVA(VAL32(g_prResNodePtr[i]->DataEntry.OffsetToData), (void **) &pbData))
                    {
                        strcat(g_szAsmCodeIndent,"//  ");
                        strcpy(szString,g_szAsmCodeIndent);
                        DumpByteArray(szString,pbData,VAL32(g_prResNodePtr[i]->DataEntry.Size),GUICookie);
                        printLine(GUICookie,szString);
                        g_szAsmCodeIndent[strlen(g_szAsmCodeIndent)-4] = 0;
                    }
                    SDELETE(g_prResNodePtr[i]);
                }
                ret = 1;
#endif
            } // end if there are nodes
        }// end if got ptr to resource
        else ret = 0xFFFFFFFF;
    } // end if there is resource
    else ret = 0;

    return ret;
}
#endif // TARGET_UNIX

