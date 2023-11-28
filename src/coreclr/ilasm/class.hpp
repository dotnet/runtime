// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// class.hpp
//

#ifndef _CLASS_HPP
#define _CLASS_HPP

class PermissionDecl;
class PermissionSetDecl;

extern unsigned int g_uCodePage;
extern WCHAR    wzUniBuf[];

class Class
{
public:
    Class * m_pEncloser;
    LPCUTF8 m_szFQN;
    DWORD   m_dwFQN;
    unsigned m_Hash;
    mdTypeDef m_cl;
    mdTypeRef m_crExtends;
    mdTypeRef *m_crImplements;
    TyParDescr* m_TyPars;
    DWORD   m_NumTyPars;
    GenericParamConstraintList m_GPCList;

    DWORD   m_Attr;
    DWORD   m_dwNumInterfaces;
    DWORD	m_dwNumFieldsWithOffset;
    PermissionDecl* m_pPermissions;
    PermissionSetDecl* m_pPermissionSets;
    ULONG	m_ulSize;
    ULONG	m_ulPack;
    BOOL	m_bIsMaster;
    BOOL    m_fNew;
    BOOL    m_fNewMembers;

    MethodList			m_MethodList;
    //MethodSortedList    m_MethodSList;
    FieldDList          m_FieldDList;
    EventDList          m_EventDList;
    PropDList           m_PropDList;
    CustomDescrList     m_CustDList;

    Class(LPCUTF8 pszFQN)
    {
        m_pEncloser = NULL;
        m_cl = mdTypeDefNil;
        m_crExtends = mdTypeRefNil;
        m_NumTyPars = 0;
        m_TyPars = NULL;
        m_dwNumInterfaces = 0;
        m_dwNumFieldsWithOffset = 0;
        m_crImplements = NULL;
        m_szFQN = pszFQN;
        m_dwFQN = pszFQN ? (DWORD)strlen(pszFQN) : 0;
        m_Hash = pszFQN ? hash((const BYTE*)pszFQN, m_dwFQN, 10) : 0;

        m_Attr = tdPublic;

        m_bIsMaster  = TRUE;
        m_fNew = TRUE;

        m_pPermissions = NULL;
        m_pPermissionSets = NULL;

        m_ulPack = 0;
        m_ulSize = 0xFFFFFFFF;
    }

    ~Class()
    {
        delete [] m_szFQN;
        delete [] m_crImplements;
        delete [] m_TyPars;
    }

    int FindTyPar(LPCWSTR wz)
    {
        int i,retval=-1;
        for(i=0; i < (int)m_NumTyPars; i++)
        {
            if(!u16_strcmp(wz,m_TyPars[i].Name()))
            {
                retval = i;
                break;
            }
        }
        return retval;
    };
    int FindTyPar(LPCUTF8 sz)
    {
        if(sz)
        {
            wzUniBuf[0] = 0;
            WszMultiByteToWideChar(g_uCodePage,0,sz,-1,wzUniBuf,dwUniBuf);
            return FindTyPar(wzUniBuf);
        }
        else return -1;
    };
    int ComparedTo(Class* T)
    {
        if (m_Hash == T->m_Hash)
        {
            // Properly handle hash conflict
            return (m_szFQN == T->m_szFQN) ? 0 : strcmp(m_szFQN, T->m_szFQN);
        } else
        {
            return (m_Hash > T->m_Hash) ? 1 : -1;
        }
    }
};


#endif /* _CLASS_HPP */

