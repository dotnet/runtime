// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
/******************************************************************************/
/*                                 dasm_formatType.cpp                        */
/******************************************************************************/
#include "ildasmpch.h"

#include "formattype.h"

BOOL    g_fQuoteAllNames = FALSE;   // used by ILDASM
BOOL    g_fDumpTokens = FALSE;      // used by ILDASM
LPCSTR *rAsmRefName = NULL;         // used by ILDASM
ULONG   ulNumAsmRefs = 0;           // used by ILDASM
BOOL    g_fDumpRTF = FALSE;         // used by ILDASM
BOOL    g_fDumpHTML = FALSE;      // used by ILDASM
BOOL    g_fUseProperName = FALSE; // used by ILDASM
DynamicArray<mdToken>      *g_dups = NULL;           // used by ILDASM
DWORD                       g_NumDups=0;              // used by ILDASM
DynamicArray<TypeDefDescr> *g_typedefs = NULL;           // used by ILDASM
DWORD                       g_NumTypedefs=0;              // used by ILDASM

// buffers created in Init and deleted in Uninit (dasm.cpp)
CQuickBytes             * g_szBuf_KEYWORD  = NULL;
CQuickBytes             * g_szBuf_COMMENT  = NULL;
CQuickBytes             * g_szBuf_ERRORMSG = NULL;
CQuickBytes             * g_szBuf_ANCHORPT = NULL;
CQuickBytes             * g_szBuf_JUMPPT  = NULL;
CQuickBytes             * g_szBuf_UnquotedProperName = NULL;
CQuickBytes             * g_szBuf_ProperName = NULL;

// Protection against null names, used by ILDASM
const char * const szStdNamePrefix[] = {"MO","TR","TD","","FD","","MD","","PA","II","MR","","CA","","PE","","","SG","","","EV",
"","","PR","","","MOR","TS","","","","","AS","","","AR","","","FL","ET","MAR"};

//-------------------------------------------------------------------------------
// Reference analysis (ILDASM)
DynamicArray<TokPair>    *g_refs = NULL;
DWORD                    g_NumRefs=0;
mdToken                  g_tkRefUser=0; // for PrettyPrintSig

mdToken                  g_tkVarOwner = 0;
mdToken                  g_tkMVarOwner = 0;

// Include the shared formatting routines
#include "formattype.cpp"

// Special dumping routines for keywords, comments and errors, used by ILDASM

const char* KEYWORD(_In_opt_z_ const char* szOrig)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    const char* szPrefix = "";
    const char* szPostfix = "";
    if(g_fDumpHTML)
    {
        szPrefix = "<B><FONT COLOR=NAVY>";
        szPostfix = "</FONT></B>";
    }
    else if(g_fDumpRTF)
    {
        szPrefix = "\\b\\cf1 ";
        szPostfix = "\\cf0\\b0 ";
    }
    if(szOrig == NULL) return szPrefix;
    if(szOrig == (char*)-1) return szPostfix;
    if(*szPrefix)
    {
        g_szBuf_KEYWORD->Shrink(0);
        appendStr(g_szBuf_KEYWORD,szPrefix);
        appendStr(g_szBuf_KEYWORD,szOrig);
        appendStr(g_szBuf_KEYWORD,szPostfix);
        return asString(g_szBuf_KEYWORD);
    }
    else
        return szOrig;
}
const char* COMMENT(_In_opt_z_ const char* szOrig)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    const char* szPrefix = "";
    const char* szPostfix = "";
    if(g_fDumpHTML)
    {
        szPrefix = "<I><FONT COLOR=GREEN>";
        szPostfix = "</FONT></I>";
    }
    else if(g_fDumpRTF)
    {
        szPrefix = "\\cf2\\i ";
        szPostfix = "\\i0\\cf0 ";
    }
    else
    {
        szPrefix = "";
        szPostfix = "";
    }
    if(szOrig == NULL) return szPrefix;
    if(szOrig == (char*)-1) return szPostfix;
    if(*szPrefix)
    {
        g_szBuf_COMMENT->Shrink(0);
        appendStr(g_szBuf_COMMENT,szPrefix);
        appendStr(g_szBuf_COMMENT,szOrig);
        appendStr(g_szBuf_COMMENT,szPostfix);
        return asString(g_szBuf_COMMENT);
    }
    else
        return szOrig;
}
const char* ERRORMSG(_In_opt_z_ const char* szOrig)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    const char* szPrefix = "";
    const char* szPostfix = "";
    if(g_fDumpHTML)
    {
        szPrefix = "<I><B><FONT COLOR=RED>";
        szPostfix = "</FONT></B></I>";
    }
    else if(g_fDumpRTF)
    {
        szPrefix = "\\cf3\\i\\b ";
        szPostfix = "\\cf0\\b0\\i0 ";
    }
    if(szOrig == NULL) return szPrefix;
    if(szOrig == (char*)-1) return szPostfix;
    if(*szPrefix)
    {
        g_szBuf_ERRORMSG->Shrink(0);
        appendStr(g_szBuf_ERRORMSG,szPrefix);
        appendStr(g_szBuf_ERRORMSG,szOrig);
        appendStr(g_szBuf_ERRORMSG,szPostfix);
        return asString(g_szBuf_ERRORMSG);
    }
    else
        return szOrig;
}

const char* ANCHORPT(_In_ __nullterminated const char* szOrig, mdToken tk)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if(g_fDumpHTML)
    {
        char  szPrefix[64];
        const char* szPostfix = "</A>";
        sprintf_s(szPrefix, COUNTOF(szPrefix), "<A NAME=A%08X>",tk);
        g_szBuf_ANCHORPT->Shrink(0);
        appendStr(g_szBuf_ANCHORPT,szPrefix);
        appendStr(g_szBuf_ANCHORPT,szOrig);
        appendStr(g_szBuf_ANCHORPT,szPostfix);
        return asString(g_szBuf_ANCHORPT);
    }
    else
        return szOrig;
}
const char* JUMPPT(_In_ __nullterminated const char* szOrig, mdToken tk)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if(g_fDumpHTML)
    {
        char  szPrefix[64];
        const char* szPostfix = "</A>";
        sprintf_s(szPrefix,COUNTOF(szPrefix), "<A HREF=#A%08X>",tk);
        g_szBuf_JUMPPT->Shrink(0);
        appendStr(g_szBuf_JUMPPT,szPrefix);
        appendStr(g_szBuf_JUMPPT,szOrig);
        appendStr(g_szBuf_JUMPPT,szPostfix);
        return asString(g_szBuf_JUMPPT);
    }
    else
        return szOrig;
}
const char* SCOPE(void) { return g_fDumpRTF ? "\\{" : "{"; }
const char* UNSCOPE(void) { return g_fDumpRTF ? "\\}" : "}"; }
const char* LTN(void) { return g_fDumpHTML ? "&lt;" : "<"; }
const char* GTN(void) { return g_fDumpHTML ? "&gt;" : ">"; }
const char* AMP(void) { return g_fDumpHTML ? "&amp;" : "&"; }



/******************************************************************************/
// Function: convert spec.symbols to esc sequences and single-quote if necessary
const char* UnquotedProperName(_In_ __nullterminated const char* name, unsigned len/*=(unsigned)-1*/)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CQuickBytes *buff = g_szBuf_UnquotedProperName;
    _ASSERTE (buff);
    if(g_fUseProperName)
    {
        const char *pcn,*pcend,*ret;
        if (name != NULL)
        {
            if (*name != 0)
            {
                pcn = name;
                if (len == (unsigned)(-1))
                    len = (unsigned)strlen(name);
                pcend = pcn + len;
                buff->Shrink(0);
                for (pcn = name; pcn < pcend; pcn++)
                {
                    switch(*pcn)
                    {
                        case '\t': appendChar(buff,'\\'); appendChar(buff,'t'); break;
                        case '\n': appendChar(buff,'\\'); appendChar(buff,'n'); break;
                        case '\b': appendChar(buff,'\\'); appendChar(buff,'b'); break;
                        case '\r': appendChar(buff,'\\'); appendChar(buff,'r'); break;
                        case '\f': appendChar(buff,'\\'); appendChar(buff,'f'); break;
                        case '\v': appendChar(buff,'\\'); appendChar(buff,'v'); break;
                        case '\a': appendChar(buff,'\\'); appendChar(buff,'a'); break;
                        case '\\': appendChar(buff,'\\'); appendChar(buff,'\\'); break;
                        case '\'': appendChar(buff,'\\'); appendChar(buff,'\''); break;
                        case '\"': appendChar(buff,'\\'); appendChar(buff,'\"'); break;
                        case '{': appendStr(buff,SCOPE()); break;
                        case '}': appendStr(buff,UNSCOPE()); break;
                        case '<': appendStr(buff,LTN()); break;
                        case '>': appendStr(buff,GTN()); break;
                        case '&': appendStr(buff,AMP()); break;
                        default: appendChar(buff,*pcn);
                    }
                }
                ret = asString(buff);
            }
            else ret = "";
        }
        else ret = NULL;
        return ret;
    }
    return name;
}
/******************************************************************************/
// Function: convert spec.symbols to esc sequences and single-quote if necessary
const char* ProperName(_In_ __nullterminated const char* name, bool isLocalName)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CQuickBytes *buff = g_szBuf_ProperName;
    _ASSERTE (buff);
    if(g_fUseProperName)
    {
        const char *ret;
        BOOL fQuoted;
        if(name)
        {
            if(*name)
            {
                buff->Shrink(0);
                fQuoted = isLocalName ? IsLocalToQuote(name) : IsNameToQuote(name);
                if(fQuoted) appendChar(buff,'\'');
                appendStr(buff,UnquotedProperName(name));
                if(fQuoted) appendChar(buff,'\'');
                ret = asString(buff);
            }
            else ret = "";
        }
        else ret = NULL;
        return ret;
    }
    return name;
}
