// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// asmman.hpp - header file for manifest-related ILASM functions
//

#ifndef ASMMAN_HPP
#define ASMMAN_HPP

#include "specstrings.h"

struct AsmManFile
{
    char*   szName;
    mdToken tkTok;
    DWORD   dwAttr;
    BinStr* pHash;
    BOOL    m_fNew;
    CustomDescrList m_CustomDescrList;
    AsmManFile() = default;
    ~AsmManFile()
    {
        if(szName)  delete szName;
        if(pHash)   delete pHash;
    }
    int ComparedTo(AsmManFile* pX){ return strcmp(szName,pX->szName); }
};
//typedef SORTEDARRAY<AsmManFile> AsmManFileList;
typedef FIFO<AsmManFile> AsmManFileList;

struct AsmManAssembly
{
	BOOL	isRef;
	BOOL	isAutodetect;
	char*	szName;
	char*	szAlias;
    DWORD   dwAlias;
	mdToken	tkTok;
	DWORD	dwAttr;
	BinStr* pPublicKey;
	BinStr* pPublicKeyToken;
	ULONG	ulHashAlgorithm;
	BinStr*	pHashBlob;
	BinStr*	pLocale;
    BOOL    m_fNew;
    // Security attributes
    PermissionDecl* m_pPermissions;
    PermissionSetDecl* m_pPermissionSets;
	CustomDescrList m_CustomDescrList;
	USHORT	usVerMajor;
	USHORT	usVerMinor;
	USHORT	usBuild;
	USHORT	usRevision;
    AsmManAssembly() = default;
    ~AsmManAssembly()
    {
        if(szAlias && (szAlias != szName)) delete [] szAlias;
        if(szName) delete [] szName;
        if(pPublicKey) delete pPublicKey;
        if(pPublicKeyToken) delete pPublicKeyToken;
        if(pHashBlob) delete pHashBlob;
        if(pLocale) delete pLocale;
    }
    int ComparedTo(AsmManAssembly* pX){ return strcmp(szAlias,pX->szAlias); }
};
//typedef SORTEDARRAY<AsmManAssembly> AsmManAssemblyList;
typedef FIFO<AsmManAssembly> AsmManAssemblyList;

struct AsmManComType
{
    char*   szName;
    mdToken tkTok;
    mdToken tkImpl;
    DWORD   dwAttr;
    char*   szFileName;
    char*   szAsmRefName;
    char*   szComTypeName;
    mdToken tkClass;
    BOOL    m_fNew;
    CustomDescrList m_CustomDescrList;
    AsmManComType() = default;
    ~AsmManComType()
    {
        if(szName) delete szName;
        if(szFileName) delete szFileName;
    };
    int ComparedTo(AsmManComType* pX){ return strcmp(szName,pX->szName); };
};
//typedef SORTEDARRAY<AsmManComType> AsmManComTypeList;
typedef FIFO<AsmManComType> AsmManComTypeList;


struct AsmManRes
{
	char*	szName;
    char*   szAlias;
	mdToken	tkTok;
	DWORD	dwAttr;
	char*	szFileName;
	ULONG	ulOffset;
    BOOL    m_fNew;
	CustomDescrList m_CustomDescrList;
	char*	szAsmRefName;
	AsmManRes() = default;
	~AsmManRes()
	{
        if(szAlias && (szAlias != szName)) delete szAlias;
		if(szName) delete szName;
		if(szFileName) delete szFileName;
		if(szAsmRefName) delete szAsmRefName;
	}
};
typedef FIFO<AsmManRes> AsmManResList;

struct AsmManModRef
{
    char*   szName;
    mdToken tkTok;
    BOOL    m_fNew;
    AsmManModRef() {szName = NULL; tkTok = 0; m_fNew = TRUE; };
    ~AsmManModRef() { if(szName) delete szName; };
};
typedef FIFO<AsmManModRef> AsmManModRefList;

struct AsmManStrongName
{
    enum AllocationState
    {
        NotAllocated = 0,
        AllocatedBySNApi,
        AllocatedByNew
    };

    BYTE *m_pbSignatureKey;
    DWORD m_cbSignatureKey;
    BYTE   *m_pbPublicKey;
    DWORD   m_cbPublicKey;
    BYTE   *m_pbPrivateKey;
    DWORD   m_cbPrivateKey;
    WCHAR  *m_wzKeyContainer;
    BOOL    m_fFullSign;

    // Where has the memory pointed to by m_pbPublicKey been taken from:
    AllocationState   m_dwPublicKeyAllocated;

    AsmManStrongName() { ZeroMemory(this, sizeof(*this)); }
    ~AsmManStrongName()
    {
        if (m_dwPublicKeyAllocated == AllocatedByNew)
            delete [] m_pbPublicKey;

        if (m_pbPrivateKey)
            delete [] m_pbPrivateKey;

        if (m_pbSignatureKey)
            delete [] m_pbSignatureKey;
    }
};

class ErrorReporter;

class AsmMan
{
    AsmManFileList      m_FileLst;
    AsmManComTypeList   m_ComTypeLst;
    AsmManResList       m_ManResLst;
    AsmManModRefList    m_ModRefLst;

    AsmManComType*      m_pCurComType;
    AsmManRes*          m_pCurManRes;
    ErrorReporter*      report;
    void*               m_pAssembler;

    AsmManFile*         GetFileByName(_In_ __nullterminated char* szFileName);
    AsmManAssembly*     GetAsmRefByName(_In_ __nullterminated const char* szAsmRefName);
    AsmManComType*      GetComTypeByName(_In_opt_z_ char* szComTypeName,
                                         _In_opt_z_ char* szComEnclosingTypeName = NULL);
    mdToken             GetComTypeTokByName(_In_opt_z_ char* szComTypeName,
                                            _In_opt_z_ char* szComEnclosingTypeName = NULL);

    IMetaDataEmit*          m_pEmitter;

public:
    IMetaDataAssemblyEmit*  m_pAsmEmitter;
    AsmManAssemblyList  m_AsmRefLst;
    AsmManAssembly*     m_pAssembly;
    AsmManAssembly*     m_pCurAsmRef;
    char*   m_szScopeName;
    BinStr* m_pGUID;
    AsmManStrongName    m_sStrongName;
	// Embedded manifest resources paraphernalia:
    WCHAR*  m_wzMResName[MAX_MANIFEST_RESOURCES];
	DWORD	m_dwMResSize[MAX_MANIFEST_RESOURCES];
    BOOL    m_fMResNew[MAX_MANIFEST_RESOURCES];
	DWORD	m_dwMResNum;
	DWORD	m_dwMResSizeTotal;
	AsmMan() { m_pAssembly = NULL; m_szScopeName = NULL; m_pGUID = NULL; m_pAsmEmitter = NULL;
				memset(m_wzMResName,0,sizeof(m_wzMResName));
				memset(m_dwMResSize,0,sizeof(m_dwMResSize));
				m_dwMResNum = m_dwMResSizeTotal = 0; };
	AsmMan(void* pAsm) { m_pAssembly = NULL; m_szScopeName = NULL; m_pGUID = NULL; m_pAssembler = pAsm;  m_pAsmEmitter = NULL;
				memset(m_wzMResName,0,sizeof(m_wzMResName));
				memset(m_dwMResSize,0,sizeof(m_dwMResSize));
				m_dwMResNum = m_dwMResSizeTotal = 0; };
	AsmMan(ErrorReporter* rpt) { m_pAssembly = NULL; m_szScopeName = NULL; m_pGUID = NULL; report = rpt;  m_pAsmEmitter = NULL;
				memset(m_wzMResName,0,sizeof(m_wzMResName));
				memset(m_dwMResSize,0,sizeof(m_dwMResSize));
				m_dwMResNum = m_dwMResSizeTotal = 0; };
	~AsmMan()
	{
		if(m_pAssembly) delete m_pAssembly;
		if(m_szScopeName) delete m_szScopeName;
		if(m_pGUID) delete m_pGUID;
	};
	void	SetErrorReporter(ErrorReporter* rpt) { report = rpt; };
	HRESULT EmitManifest(void);

    void    SetEmitter( IMetaDataEmit* pEmitter) { m_pEmitter = pEmitter; };

    void    SetModuleName(__inout_opt __nullterminated char* szName);

    void    AddFile(_In_ __nullterminated char* szName, DWORD dwAttr, BinStr* pHashBlob);
    void    EmitFiles();
	void	EmitDebuggableAttribute(mdToken tkOwner);

	void	StartAssembly(_In_ __nullterminated char* szName, _In_opt_z_ char* szAlias, DWORD dwAttr, BOOL isRef);
	void	EndAssembly();
    void    EmitAssemblyRefs();
    void    EmitAssembly();
	void	SetAssemblyPublicKey(BinStr* pPublicKey);
	void	SetAssemblyPublicKeyToken(BinStr* pPublicKeyToken);
	void	SetAssemblyHashAlg(ULONG ulAlgID);
	void	SetAssemblyVer(USHORT usMajor, USHORT usMinor, USHORT usBuild, USHORT usRevision);
	void	SetAssemblyLocale(BinStr* pLocale, BOOL bConvertToUnicode);
	void	SetAssemblyHashBlob(BinStr* pHashBlob);
    void    SetAssemblyAutodetect();

    void    StartComType(_In_ __nullterminated char* szName, DWORD dwAttr);
    void    EndComType();
    void    SetComTypeFile(_In_ __nullterminated char* szFileName);
    void    SetComTypeAsmRef(_In_ __nullterminated char* szAsmRefName);
    void    SetComTypeComType(_In_ __nullterminated char* szComTypeName);
    BOOL    SetComTypeImplementationTok(mdToken tk);
    BOOL    SetComTypeClassTok(mdToken tkClass);

    void    StartManifestRes(_In_ __nullterminated char* szName, _In_ __nullterminated char* szAlias, DWORD dwAttr);
    void    EndManifestRes();
    void    SetManifestResFile(_In_ __nullterminated char* szFileName, ULONG ulOffset);
    void    SetManifestResAsmRef(_In_ __nullterminated char* szAsmRefName);

    AsmManAssembly*     GetAsmRefByAsmName(_In_ __nullterminated const char* szAsmName);

    mdToken             GetFileTokByName(_In_ __nullterminated char* szFileName);
    mdToken             GetAsmRefTokByName(_In_ __nullterminated const char* szAsmRefName);
    mdToken             GetAsmTokByName(_In_ __nullterminated const char* szAsmName)
        { return (m_pAssembly && (strcmp(m_pAssembly->szName,szAsmName)==0)) ? m_pAssembly->tkTok : 0; };

    mdToken GetModuleRefTokByName(_In_ __nullterminated char* szName)
    {
        if(szName && *szName)
        {
            AsmManModRef* pMR;
            for(unsigned i=0; (pMR=m_ModRefLst.PEEK(i)); i++)
            {
                if(!strcmp(szName, pMR->szName)) return pMR->tkTok;
            }
        }
        return 0;
    };

};

#endif
