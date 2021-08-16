// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/**************************************************************************/
/* a type parameter list */

#ifndef TYPAR_H
#define TYPAR_H
#include "binstr.h"

extern unsigned int g_uCodePage;

class TyParDescr
{
public:
    TyParDescr()
    {
        m_pbsBounds = NULL;
        m_wzName = NULL;
        m_dwAttrs = 0;
    };
    ~TyParDescr()
    {
        delete m_pbsBounds;
        delete [] m_wzName;
        m_lstCA.RESET(true);
    };
    void Init(BinStr* bounds, LPCUTF8 name, DWORD attrs)
    {
        m_pbsBounds = bounds;
        ULONG               cTemp = (ULONG)strlen(name)+1;
        WCHAR *pwzName;
        m_wzName = pwzName = new WCHAR[cTemp];
        if(pwzName)
        {
            memset(pwzName,0,sizeof(WCHAR)*cTemp);
            WszMultiByteToWideChar(g_uCodePage,0,name,-1,pwzName,cTemp);
        }
        m_dwAttrs = attrs;
    };
    BinStr* Bounds() { return m_pbsBounds; };
    LPCWSTR Name() { return m_wzName; };
    DWORD   Attrs() { return m_dwAttrs; };
    mdToken Token() { return m_token; };
    void    Token(mdToken value)
    {
        m_token = value;
     };
    CustomDescrList* CAList() { return &m_lstCA; };

private:
    BinStr* m_pbsBounds;
    LPCWSTR m_wzName;
    DWORD   m_dwAttrs;
    mdToken m_token;
    CustomDescrList m_lstCA;
};

class TyParList {
public:
    TyParList(DWORD a, BinStr* b, LPCUTF8 n, TyParList* nx = NULL)
    {
        bound  = (b == NULL) ? new BinStr() : b;
        bound->appendInt32(0); // zero terminator
        attrs = a; name = n; next = nx;
    };
    ~TyParList()
    {
        if( bound) delete bound;

        // To avoid excessive stack usage (especially in debug builds), we break the next chain
        // and delete as we traverse the link list
        TyParList *pCur = next;
        while (pCur != NULL)
        {
            TyParList *pTmp = pCur->next;
            pCur->next = NULL;
            delete pCur;
            pCur = pTmp;
        }
    };
    int Count()
    {
        TyParList* tp = this;
        int n;
        for(n = 1; (tp = tp->next) != NULL; n++);
        return n;
    };
    int IndexOf(LPCUTF8 name)
    {
        TyParList* tp = this;
        int n;
        int ret = -1;
        for(n=0; tp != NULL; n++, tp = tp->next)
        {
            if(tp->name == NULL)
            {
                if(name == NULL) ret = n;
            }
            else
            {
                if(name == NULL) continue;
                if(0 == strcmp(name,tp->name)) ret = n;
            }
        }
        return ret;
    };

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:6211) // "Leaking memory 'b' due to an exception. Consider using a local catch block to clean up memory"
#endif /*_PREFAST_ */

    int ToArray(BinStr ***bounds, LPCWSTR** names, DWORD **attrs)
    {
        int n = Count();
        BinStr **b = new BinStr* [n];
        LPCWSTR *nam = new LPCWSTR [n];
        DWORD *attr = attrs ? new DWORD [n] : NULL;
        TyParList *tp = this;
        int i = 0;
        while (tp)
        {
            ULONG               cTemp = (ULONG)strlen(tp->name)+1;
            WCHAR*              wzDllName = new WCHAR [cTemp];
            // Convert name to UNICODE
            memset(wzDllName,0,sizeof(WCHAR)*cTemp);
            WszMultiByteToWideChar(g_uCodePage,0,tp->name,-1,wzDllName,cTemp);
            nam[i] = (LPCWSTR)wzDllName;
            b[i] = tp->bound;
            if (attr)
                attr[i] = tp->attrs;
            tp->bound = 0; // to avoid deletion by destructor
            i++;
            tp = tp->next;
        }
        *bounds = b;
        *names = nam;
        if (attrs)
            *attrs = attr;
        return n;
    };

#ifdef _PREFAST_
#pragma warning(pop)
#endif /*_PREFAST_*/

    int ToArray(TyParDescr **ppTPD)
    {
        int n = Count();
        TyParDescr *pTPD = NULL;
        if(n)
        {
            pTPD = new TyParDescr[n];
            if(pTPD)
            {
                int i = 0;
                TyParList *tp = this;
                while (tp)
                {
                    pTPD[i].Init(tp->bound,tp->name,tp->attrs);
                    tp->bound = 0; // to avoid deletion by destructor
                    i++;
                    tp = tp->next;
                }
            }
        }
        *ppTPD = pTPD;
        return n;
    };
    TyParList* Next() { return next; };
    BinStr* Bound() { return bound; };
private:
    BinStr* bound;
    LPCUTF8 name;
    TyParList* next;
    DWORD   attrs;
};

typedef TyParList* pTyParList;

#endif

