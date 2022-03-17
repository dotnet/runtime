// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/********************************************************************************/
/* Code goes here */

/********************************************************************************/
extern int yyparse();

struct Keywords {
    const char* name;
    unsigned short token;
    unsigned short tokenVal;// this holds the instruction enumeration for those keywords that are instrs
    size_t   stname;
};

#define NO_VALUE        ((unsigned short)-1)              // The token has no value

static Keywords keywords[] = {
// Attention! Because of aliases, the instructions MUST go first!
// Redefine all the instructions (defined in assembler.h <- asmenum.h <- opcode.def)
#undef InlineNone
#undef InlineVar
#undef ShortInlineVar
#undef InlineI
#undef ShortInlineI
#undef InlineI8
#undef InlineR
#undef ShortInlineR
#undef InlineBrTarget
#undef ShortInlineBrTarget
#undef InlineMethod
#undef InlineField
#undef InlineType
#undef InlineString
#undef InlineSig
#undef InlineTok
#undef InlineSwitch
#undef InlineVarTok


#define InlineNone              INSTR_NONE
#define InlineVar               INSTR_VAR
#define ShortInlineVar          INSTR_VAR
#define InlineI                 INSTR_I
#define ShortInlineI            INSTR_I
#define InlineI8                INSTR_I8
#define InlineR                 INSTR_R
#define ShortInlineR            INSTR_R
#define InlineBrTarget          INSTR_BRTARGET
#define ShortInlineBrTarget             INSTR_BRTARGET
#define InlineMethod            INSTR_METHOD
#define InlineField             INSTR_FIELD
#define InlineType              INSTR_TYPE
#define InlineString            INSTR_STRING
#define InlineSig               INSTR_SIG
#define InlineTok               INSTR_TOK
#define InlineSwitch            INSTR_SWITCH

#define InlineVarTok            0
#define NEW_INLINE_NAMES
                // The volatile instruction collides with the volatile keyword, so
                // we treat it as a keyword everywhere and modify the grammar accordingly (Yuck!)
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) { s, args, c, STRING_LENGTH(s) },
#define OPALIAS(alias_c, s, c) { s, NO_VALUE, c, STRING_LENGTH(s) },
#include "opcode.def"
#undef OPALIAS
#undef OPDEF

                /* keywords */
#define KYWD(name, sym, val)    { name, sym, val, STRING_LENGTH(name) },
#include "il_kywd.h"
#undef KYWD

};

/********************************************************************************/
/* File encoding-dependent functions */
/*--------------------------------------------------------------------------*/
char* nextcharU(_In_ __nullterminated char* pos)
{
    return ++pos;
}

char* nextcharW(_In_ __nullterminated char* pos)
{
    return (pos+2);
}
/*--------------------------------------------------------------------------*/
unsigned SymAU(_In_ __nullterminated char* curPos)
{
    return (unsigned)*curPos;
}

unsigned SymW(_In_ __nullterminated char* curPos)
{
    return (unsigned)*((WCHAR*)curPos);
}
/*--------------------------------------------------------------------------*/
char* NewStrFromTokenAU(_In_reads_(tokLen) char* curTok, size_t tokLen)
{
    char *nb = new char[tokLen+1];
    if(nb != NULL)
    {
        memcpy(nb, curTok, tokLen);
        nb[tokLen] = 0;
    }
    return nb;
}
char* NewStrFromTokenW(_In_reads_(tokLen) char* curTok, size_t tokLen)
{
    WCHAR* wcurTok = (WCHAR*)curTok;
    char *nb = new char[(tokLen<<1) + 2];
    if(nb != NULL)
    {
        tokLen = WszWideCharToMultiByte(CP_UTF8,0,(LPCWSTR)wcurTok,(int)(tokLen >> 1),nb,(int)(tokLen<<1) + 2,NULL,NULL);
        nb[tokLen] = 0;
    }
    return nb;
}
/*--------------------------------------------------------------------------*/
char* NewStaticStrFromTokenAU(_In_reads_(tokLen) char* curTok, size_t tokLen, _Out_writes_(bufSize) char* staticBuf, size_t bufSize)
{
    if(tokLen >= bufSize) return NULL;
    memcpy(staticBuf, curTok, tokLen);
    staticBuf[tokLen] = 0;
    return staticBuf;
}
char* NewStaticStrFromTokenW(_In_reads_(tokLen) char* curTok, size_t tokLen, _Out_writes_(bufSize) char* staticBuf, size_t bufSize)
{
    WCHAR* wcurTok = (WCHAR*)curTok;
    if(tokLen >= bufSize/2) return NULL;
    tokLen = WszWideCharToMultiByte(CP_UTF8,0,(LPCWSTR)wcurTok,(int)(tokLen >> 1),staticBuf,(int)bufSize,NULL,NULL);
    staticBuf[tokLen] = 0;
    return staticBuf;
}
/*--------------------------------------------------------------------------*/
unsigned GetDoubleAU(_In_ __nullterminated char* begNum, unsigned L, double** ppRes)
{
    static char dbuff[128];
    char* pdummy = NULL;
    if(L > 127) L = 127;
    memcpy(dbuff,begNum,L);
    dbuff[L] = 0;
    *ppRes = new double(strtod(dbuff, &pdummy));
    return ((unsigned)(pdummy - dbuff));
}

unsigned GetDoubleW(_In_ __nullterminated char* begNum, unsigned L, double** ppRes)
{
    static char dbuff[256];
    char* pdummy = NULL;
    if(L > 254) L = 254;
    memcpy(dbuff,begNum,L);
    dbuff[L] = 0;
    dbuff[L+1] = 0;
    *ppRes = new double(wcstod((const WCHAR*)dbuff, (WCHAR**)&pdummy));
    return ((unsigned)(pdummy - dbuff));
}
/*--------------------------------------------------------------------------*/
char* yygetline(int Line)
{
    static char buff[0x4000];
    char *pLine=NULL, *pNextLine=NULL;
    char *pBegin=NULL, *pEnd = NULL;
    unsigned uCount = parser->getAll(&pBegin);
    pEnd = pBegin + uCount;
    buff[0] = 0;
    for(uCount=0, pLine=pBegin; pLine < pEnd; pLine = nextchar(pLine))
    {
        if(Sym(pLine) == '\n') uCount++;
        if(uCount == (unsigned int)(Line-1)) break;
    }
    pLine = nextchar(pLine);
    if(pLine < pEnd)
    {
        for(pNextLine = pLine; pNextLine < pEnd; pNextLine = nextchar(pNextLine))
        {
            if(Sym(pNextLine) == '\n') break;
        }
        if(Sym == SymW) // Unicode file
        {
            if(*((WCHAR*)pNextLine - 1) == '\r') pNextLine -= 2;
            uCount = (unsigned)(pNextLine - pLine);
            uCount &= 0x1FFF; // limit: 8K wchars
            WCHAR* wzBuff = (WCHAR*)buff;
            memcpy(buff,pLine,uCount);
            wzBuff[uCount >> 1] = 0;
        }
        else
        {
            if(*(pNextLine-1)=='\r') pNextLine--;
            uCount = (unsigned)(pNextLine - pLine);
            uCount &= 0x3FFF; // limit: 16K chars
            memcpy(buff,pLine,uCount);
            buff[uCount]=0;
        }
    }
    return buff;
}

void yyerror(_In_ __nullterminated const char* str) {
    char tokBuff[64];
    WCHAR *wzfile = (WCHAR*)(PENV->in->namew());
    int iline = PENV->curLine;

    size_t len = PENV->curPos - PENV->curTok;
    if (len > 62) len = 62;
    memcpy(tokBuff, PENV->curTok, len);
    tokBuff[len] = 0;
    tokBuff[len+1] = 0;
    if(PENV->bExternSource)
    {
        wzfile = PASM->m_wzSourceFileName;
        iline = PENV->nExtLine;
    }
    if(Sym == SymW) // Unicode file
        fprintf(stderr, "%S(%d) : error : %s at token '%S' in: %S\n",
                wzfile, iline, str, (WCHAR*)tokBuff, (WCHAR*)yygetline(PENV->curLine));
    else
        fprintf(stderr, "%S(%d) : error : %s at token '%s' in: %s\n",
                wzfile, iline, str, tokBuff, yygetline(PENV->curLine));
    parser->success = false;
}

/********************************************************************************/
/* looks up the typedef 'name' of length 'nameLen' (name does not need to be
   null terminated)   Returns 0 on failure */
TypeDefDescr* findTypedef(_In_reads_(NameLen) char* name, size_t NameLen)
{
    TypeDefDescr* pRet = NULL;
    static char Name[4096];
    if(PASM->NumTypeDefs())
    {
        if(NewStaticStrFromToken(name,NameLen,Name,4096))
            pRet = PASM->FindTypeDef(Name);
    }
    return pRet;
}

int TYPEDEF(TypeDefDescr* pTDD)
{
    switch(TypeFromToken(pTDD->m_tkTypeSpec))
    {
        case mdtTypeDef:
        case mdtTypeRef:
            return TYPEDEF_T;
        case mdtMethodDef:
        case 0x99000000:
            return TYPEDEF_M;
        case mdtFieldDef:
        case 0x98000000:
            return TYPEDEF_F;
        case mdtMemberRef:
            return TYPEDEF_MR;
        case mdtTypeSpec:
            return TYPEDEF_TS;
        case mdtCustomAttribute:
            return TYPEDEF_CA;
    }
    return ERROR_;

}

/********************************************************************************/
void indexKeywords(Indx* indx)  // called in Assembler constructor (assem.cpp)
{
    Keywords* low = keywords;
    Keywords* high = keywords + (sizeof(keywords) / sizeof(Keywords));
    Keywords* mid;
    for(mid = low; mid < high; mid++)
    {
        indx->IndexString((char*)(mid->name),mid);
    }
}

Instr* SetupInstr(unsigned short opcode)
{
    Instr* pVal = NULL;
    if((pVal = PASM->GetInstr()))
    {
        pVal->opcode = opcode;
        if(PASM->m_fGeneratePDB)
        {
            if(PENV->bExternSource)
            {
                pVal->linenum = PENV->nExtLine;
                pVal->column = PENV->nExtCol;
                pVal->linenum_end = PENV->nExtLineEnd;
                pVal->column_end = PENV->nExtColEnd;
                pVal->pc = nCurrPC;
            }
            else
            {
                pVal->linenum = PENV->curLine;
                pVal->column = 1;
                pVal->linenum_end = PENV->curLine;
                // Portable PDB rule:
                // - If Start Line is equal to End Line then End Column is greater than Start Column.
                // To fulfill this condition the column_end is set to 2 instead of 0
                pVal->column_end = 2;
                pVal->pc = PASM->m_CurPC;
            }
            pVal->pOwnerDocument = PASM->m_pPortablePdbWriter->GetCurrentDocument();
        }
    }
    return pVal;
}
/* looks up the keyword 'name' of length 'nameLen' (name does not need to be
   null terminated)   Returns 0 on failure */
int findKeyword(const char* name, size_t nameLen, unsigned short* pOpcode)
{
    static char Name[128];
    Keywords* mid;

    if(NULL == NewStaticStrFromToken((char*)name,nameLen,Name,128)) return 0; // can't be a keyword
    mid = (Keywords*)(PASM->indxKeywords.FindString(Name));
    if(mid == NULL) return 0;
    *pOpcode = mid->tokenVal;

    return(mid->token);
}

/********************************************************************************/
/* convert str to a uint64 */
unsigned digits[128];
void Init_str2uint64()
{
    int i;
    memset(digits,255,sizeof(digits));
    for(i='0'; i <= '9'; i++) digits[i] = i - '0';
    for(i='A'; i <= 'Z'; i++) digits[i] = i + 10 - 'A';
    for(i='a'; i <= 'z'; i++) digits[i] = i + 10 - 'a';
}
static unsigned __int64 str2uint64(const char* str, const char** endStr, unsigned radix)
{
    unsigned __int64 ret = 0;
    unsigned digit,ix;
    _ASSERTE(radix <= 36);
    for(;;str = nextchar((char*)str))
    {
        ix = Sym((char*)str);
        if(ix <= 0x7F)
        {
            digit = digits[ix];
            if(digit < radix)
            {
                ret = ret * radix + digit;
                continue;
            }
        }
        *endStr = str;
        return(ret);
    }
}
/********************************************************************************/
/* Append an UTF-8 string preceded by compressed length, no zero terminator, to a BinStr */
static void AppendStringWithLength(BinStr* pbs, _In_ __nullterminated char* sz)
{
    if((pbs != NULL) && (sz != NULL))
    {
        unsigned L = (unsigned) strlen(sz);
        BYTE* pb = NULL;
        corEmitInt(pbs,L);
        if((pb = pbs->getBuff(L)) != NULL)
            memcpy(pb,sz,L);
    }
}

/********************************************************************************/
/* Append a typed field initializer to an untyped custom attribute blob
 * Since the result is untyped, we have to byte-swap here on big-endian systems
 */
#ifdef BIGENDIAN
static int ByteSwapCustomBlob(BYTE *ptr, int length, int type, bool isSZArray)
{
    BYTE *orig_ptr = ptr;

    int nElem = 1;
    if (isSZArray)
    {
        _ASSERTE(length >= 4);
        nElem = GET_UNALIGNED_32(ptr);
        SET_UNALIGNED_VAL32(ptr, nElem);
        if (nElem == 0xffffffff)
            nElem = 0;
        ptr += 4;
        length -= 4;
    }

    for (int i = 0; i < nElem; i++)
    {
        switch (type)
        {
            case ELEMENT_TYPE_BOOLEAN:
            case ELEMENT_TYPE_I1:
            case ELEMENT_TYPE_U1:
                _ASSERTE(length >= 1);
                ptr++;
                length--;
                break;
            case ELEMENT_TYPE_CHAR:
            case ELEMENT_TYPE_I2:
            case ELEMENT_TYPE_U2:
                _ASSERTE(length >= 2);
                SET_UNALIGNED_VAL16(ptr, GET_UNALIGNED_16(ptr));
                ptr += 2;
                length -= 2;
                break;
            case ELEMENT_TYPE_I4:
            case ELEMENT_TYPE_U4:
            case ELEMENT_TYPE_R4:
                _ASSERTE(length >= 4);
                SET_UNALIGNED_VAL32(ptr, GET_UNALIGNED_32(ptr));
                ptr += 4;
                length -= 4;
                break;
            case ELEMENT_TYPE_I8:
            case ELEMENT_TYPE_U8:
            case ELEMENT_TYPE_R8:
                _ASSERTE(length >= 8);
                SET_UNALIGNED_VAL64(ptr, GET_UNALIGNED_64(ptr));
                ptr += 8;
                length -= 8;
                break;
            case ELEMENT_TYPE_STRING:
            case SERIALIZATION_TYPE_TYPE:
                _ASSERTE(length >= 1);
                if (*ptr == 0xFF)
                {
                    ptr++;
                    length--;
                }
                else
                {
                    int skipped = CorSigUncompressData((PCCOR_SIGNATURE&)ptr);
                    _ASSERTE(length >= skipped);
                    ptr += skipped;
                    length -= skipped;
                }
                break;
            case SERIALIZATION_TYPE_TAGGED_OBJECT:
            {
                _ASSERTE(length >= 1);
                bool objIsSZArray = false;
                int objType = *ptr;
                ptr++;
                length--;
                if (type == ELEMENT_TYPE_SZARRAY)
                {
                    _ASSERTE(length >= 1);
                    objIsSZArray = false;
                    objType = *ptr;
                    ptr++;
                    length--;
                }
                int skipped = ByteSwapCustomBlob(ptr, length, objType, objIsSZArray);
                _ASSERTE(length >= skipped);
                ptr += skipped;
                length -= skipped;
                break;
            }
        }
    }

    return ptr - orig_ptr;
}
#endif

static void AppendFieldToCustomBlob(BinStr* pBlob, _In_ BinStr* pField)
{
    pBlob->appendFrom(pField, (*(pField->ptr()) == ELEMENT_TYPE_SZARRAY) ? 2 : 1);

#ifdef BIGENDIAN
    BYTE *fieldPtr = pField->ptr();
    int fieldLength = pField->length();

    bool isSZArray = false;
    int type = fieldPtr[0];
    fieldLength--;
    if (type == ELEMENT_TYPE_SZARRAY)
    {
        isSZArray = true;
        type = fieldPtr[1];
        fieldLength--;
    }

    // This may be a bytearray that must not be swapped.
    if (type == ELEMENT_TYPE_STRING && !isSZArray)
        return;

    BYTE *blobPtr = pBlob->ptr() + (pBlob->length() - fieldLength);
    ByteSwapCustomBlob(blobPtr, fieldLength, type, isSZArray);
#endif
}


/********************************************************************************/
/* fetch the next token, and return it   Also set the yylval.union if the
   lexical token also has a value */


BOOL _Alpha[128];
BOOL _Digit[128];
BOOL _AlNum[128];
BOOL _ValidSS[128];
BOOL _ValidCS[128];
void SetSymbolTables()
{
    unsigned i;
    memset(_Alpha,0,sizeof(_Alpha));
    memset(_Digit,0,sizeof(_Digit));
    memset(_AlNum,0,sizeof(_AlNum));
    memset(_ValidSS,0,sizeof(_ValidSS));
    memset(_ValidCS,0,sizeof(_ValidCS));
    for(i = 'A'; i <= 'Z'; i++)
    {
        _Alpha[i] = TRUE;
        _AlNum[i] = TRUE;
        _ValidSS[i] = TRUE;
        _ValidCS[i] = TRUE;
    }
    for(i = 'a'; i <= 'z'; i++)
    {
        _Alpha[i] = TRUE;
        _AlNum[i] = TRUE;
        _ValidSS[i] = TRUE;
        _ValidCS[i] = TRUE;
    }
    for(i = '0'; i <= '9'; i++)
    {
        _Digit[i] = TRUE;
        _AlNum[i] = TRUE;
        _ValidCS[i] = TRUE;
    }
    _ValidSS[(unsigned char)'_'] = TRUE;
    _ValidSS[(unsigned char)'#'] = TRUE;
    _ValidSS[(unsigned char)'$'] = TRUE;
    _ValidSS[(unsigned char)'@'] = TRUE;

    _ValidCS[(unsigned char)'_'] = TRUE;
    _ValidCS[(unsigned char)'?'] = TRUE;
    _ValidCS[(unsigned char)'$'] = TRUE;
    _ValidCS[(unsigned char)'@'] = TRUE;
    _ValidCS[(unsigned char)'`'] = TRUE;
}
BOOL IsAlpha(unsigned x) { return (x < 128)&&_Alpha[x]; }
BOOL IsDigit(unsigned x) { return (x < 128)&&_Digit[x]; }
BOOL IsAlNum(unsigned x) { return (x < 128)&&_AlNum[x]; }
BOOL IsValidStartingSymbol(unsigned x) { return (x < 128)&&_ValidSS[x]; }
BOOL IsValidContinuingSymbol(unsigned x) { return (x < 128)&&_ValidCS[x]; }


char* nextBlank(_In_ __nullterminated char* curPos)
{
    for(;;)
    {
        switch(Sym(curPos))
        {
            case '/' :
                if ((Sym(nextchar(curPos)) == '/')|| (Sym(nextchar(curPos)) == '*'))
                    return curPos;
                else
                {
                    curPos = nextchar(curPos);
                    break;
                }
            case 0:
            case '\n':
            case '\r':
            case ' ' :
            case '\t':
            case '\f':
                return curPos;

            default:
                curPos = nextchar(curPos);
        }
    }
}

char* skipBlanks(_In_ __nullterminated char* curPos, unsigned* pstate)
{
    const unsigned eolComment = 1;
    const unsigned multiComment = 2;
    unsigned nextSym, state = *pstate;
    char* nextPos;
    for(;;)
    {   // skip whitespace and comments
        if (curPos >= PENV->endPos)
        {
            *pstate = state;
            return NULL;
        }
        switch(Sym(curPos))
        {
            case 0:
                return NULL;       // EOF
            case '\n':
                state &= ~eolComment;
                PENV->curLine++;
                if(PENV->bExternSource)
                {
                    if(PENV->bExternSourceAutoincrement) PENV->nExtLine++;
                    PASM->m_ulCurLine = PENV->nExtLine;
                    PASM->m_ulCurColumn = PENV->nExtCol;
                }
                else
                {
                    PASM->m_ulCurLine = PENV->curLine;
                    PASM->m_ulCurColumn = 1;
                }
                break;
            case '\r':
            case ' ' :
            case '\t':
            case '\f':
                break;

            case '*' :
                if(state == 0) goto PAST_WHITESPACE;
                if(state & multiComment)
                {
                    nextPos = nextchar(curPos);
                    if (Sym(nextPos) == '/')
                    {
                        curPos = nextPos;
                        state &= ~multiComment;
                    }
                }
                break;

            case '/' :
                if(state == 0)
                {
                    nextPos = nextchar(curPos);
                    nextSym = Sym(nextPos);
                    if (nextSym == '/')
                    {
                        curPos = nextPos;
                        state |= eolComment;
                    }
                    else if (nextSym == '*')
                    {
                        curPos = nextPos;
                        state |= multiComment;
                    }
                    else goto PAST_WHITESPACE;
                }
                break;

            default:
                if (state == 0)  goto PAST_WHITESPACE;
        }
        curPos = nextchar(curPos);
    }
PAST_WHITESPACE:
    *pstate = state;
    return curPos;
}

char* FullFileName(_In_ __nullterminated WCHAR* wzFileName, unsigned uCodePage);

int ProcessEOF()
{
    PARSING_ENVIRONMENT* prev_penv = parser->PEStack.POP();
    if(prev_penv != NULL)
    {
        //delete [] (WCHAR*)(PENV->in->namew());
        delete PENV->in;
        delete PENV;
        parser->penv = prev_penv;
        SetFunctionPtrs();
        char* szFileName = new char[strlen(PENV->szFileName)+1];
        strcpy_s(szFileName,strlen(PENV->szFileName)+1,PENV->szFileName);
        PASM->SetSourceFileName(szFileName); // deletes the argument!
        return ';';
    }
    //PENV->in = NULL;
    return 0;
}

#define NEXT_TOKEN  {state=0; curPos=PENV->curPos; goto NextToken;}

int parse_literal(unsigned curSym, __inout __nullterminated char* &curPos, BOOL translate_escapes)
{
    unsigned quote = curSym;
    curPos = nextchar(curPos);
    char* fromPtr = curPos;
    bool escape = false;

    for(;;)
    {     // Find matching quote
        curSym = (curPos >= PENV->endPos) ? 0 : Sym(curPos);
        if(curSym == 0)
        {
            PENV->curPos = curPos;
            return(BAD_LITERAL_);
        }
        else if(curSym == '\\')
            escape = !escape;
        else
        {
            if(curSym == '\n')
            {
                PENV->curLine++;
                if(PENV->bExternSource)
                {
                    if(PENV->bExternSourceAutoincrement) PENV->nExtLine++;
                    PASM->m_ulCurLine = PENV->nExtLine;
                    PASM->m_ulCurColumn = PENV->nExtCol;
                }
                else
                {
                    PASM->m_ulCurLine = PENV->curLine;
                    PASM->m_ulCurColumn = 1;
                }
                if (!escape) { PENV->curPos = curPos; return(BAD_LITERAL_); }
            }
            else if ((curSym == quote) && (!escape)) break;
            escape = false;
        }
        curPos = nextchar(curPos);
    }
    // translate escaped characters
    unsigned tokLen = (unsigned)(curPos - fromPtr);
    char* newstr = NewStrFromToken(fromPtr, tokLen);
    char* toPtr;
    curPos = nextchar(curPos);  // skip closing quote
    if(translate_escapes)
    {
        fromPtr = newstr;
        //_ASSERTE(0);
        tokLen = (unsigned)strlen(newstr);
        toPtr = new char[tokLen+1];
        if(toPtr==NULL) return BAD_LITERAL_;
        yylval.string = toPtr;
        char* endPtr = fromPtr+tokLen;
        while(fromPtr < endPtr)
        {
            if (*fromPtr == '\\')
            {
                fromPtr++;
                switch(*fromPtr)
                {
                    case 't':
                            *toPtr++ = '\t';
                            break;
                    case 'n':
                            *toPtr++ = '\n';
                            break;
                    case 'b':
                            *toPtr++ = '\b';
                            break;
                    case 'f':
                            *toPtr++ = '\f';
                            break;
                    case 'v':
                            *toPtr++ = '\v';
                            break;
                    case '?':
                            *toPtr++ = '\?';
                            break;
                    case 'r':
                            *toPtr++ = '\r';
                            break;
                    case 'a':
                            *toPtr++ = '\a';
                            break;
                    case '\n':
                            do      fromPtr++;
                            while(isspace(*fromPtr));
                            --fromPtr;              // undo the increment below
                            break;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                            if (IsDigit(fromPtr[1]) && IsDigit(fromPtr[2]))
                            {
                                *toPtr++ = ((fromPtr[0] - '0') * 8 + (fromPtr[1] - '0')) * 8 + (fromPtr[2] - '0');
                                fromPtr+= 2;
                            }
                            else if(*fromPtr == '0') *toPtr++ = 0;
                            else *toPtr++ = *fromPtr;
                            break;
                    default:
                            *toPtr++ = *fromPtr;
                }
                fromPtr++;
            }
            else
            //  *toPtr++ = *fromPtr++;
            {
                char* tmpPtr = fromPtr;
                fromPtr = (nextchar == nextcharW) ? nextcharU(fromPtr) : nextchar(fromPtr);
                while(tmpPtr < fromPtr) *toPtr++ = *tmpPtr++;
            }

        } //end while(fromPtr < endPtr)
        *toPtr = 0;                     // terminate string
        delete [] newstr;
    }
    else
    {
        yylval.string = newstr;
        toPtr = newstr + strlen(newstr);
    }

    PENV->curPos = curPos;
    if(quote == '"')
    {
        BinStr* pBS = new BinStr();
        unsigned size = (unsigned)(toPtr - yylval.string);
        memcpy(pBS->getBuff(size),yylval.string,size);
        delete [] yylval.string;
        yylval.binstr = pBS;
        return QSTRING;
    }
    else
    {
        if(PASM->NumTypeDefs())
        {
            TypeDefDescr* pTDD = PASM->FindTypeDef(yylval.string);
            if(pTDD != NULL)
            {
                delete [] yylval.string;
                yylval.tdd = pTDD;
                return(TYPEDEF(pTDD));
            }
        }
        return SQSTRING;
    }
}

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
int yylex()
{
    char* curPos = PENV->curPos;
    unsigned state = 0;
    const unsigned multiComment = 2;
    unsigned curSym;

    char* newstr;

NextToken:
    // Skip any leading whitespace and comments
    curPos = skipBlanks(curPos, &state);
    if(curPos == NULL)
    {
        if (state & multiComment) return (BAD_COMMENT_);
        if(ProcessEOF() == 0) return 0;       // EOF
        NEXT_TOKEN;
    }
    char* curTok = curPos;
    PENV->curTok = curPos;
    PENV->curPos = curPos;
    int tok = ERROR_;
    yylval.string = 0;

    curSym = Sym(curPos);
    if(bParsingByteArray) // only hexadecimals w/o 0x, ')' and white space allowed!
    {
        int i,s=0;
        for(i=0; i<2; i++, curPos = nextchar(curPos), curSym = Sym(curPos))
        {
            if(('0' <= curSym)&&(curSym <= '9')) s = s*16+(curSym - '0');
            else if(('A' <= curSym)&&(curSym <= 'F')) s = s*16+(curSym - 'A' + 10);
            else if(('a' <= curSym)&&(curSym <= 'f')) s = s*16+(curSym - 'a' + 10);
            else break; // don't increase curPos!
        }
        if(i)
        {
            tok = HEXBYTE;
            yylval.int32 = s;
        }
        else
        {
            if(curSym == ')' || curSym == '}')
            {
                bParsingByteArray = FALSE;
                goto Just_A_Character;
            }
        }
        PENV->curPos = curPos;
        return(tok);
    }
    if(curSym == '?') // '?' may be part of an identifier, if it's not followed by punctuation
    {
        if(IsValidContinuingSymbol(Sym(nextchar(curPos)))) goto Its_An_Id;
        goto Just_A_Character;
    }

    if (IsValidStartingSymbol(curSym))
    { // is it an ID
Its_An_Id:
        size_t offsetDot = (size_t)-1; // first appearance of '.'
                size_t offsetDotDigit = (size_t)-1; // first appearance of '.<digit>' (not DOTTEDNAME!)
        do
        {
            curPos = nextchar(curPos);
            if (Sym(curPos) == '.')
            {
                if (offsetDot == (size_t)-1) offsetDot = curPos - curTok;
                curPos = nextchar(curPos);
                if((offsetDotDigit==(size_t)-1)&&(Sym(curPos) >= '0')&&(Sym(curPos) <= '9'))
                        offsetDotDigit = curPos - curTok - 1;
            }
        } while(IsValidContinuingSymbol(Sym(curPos)));

        size_t tokLen = curPos - curTok;
        // check to see if it is a keyword
        int token = findKeyword(curTok, tokLen, &yylval.opcode);
        if (token != 0)
        {
            //printf("yylex: TOK = %d, curPos=0x%8.8X\n",token,curPos);
            PENV->curPos = curPos;
            PENV->curTok = curTok;
            if(!SkipToken)
            {
                switch(token)
                {
                    case P_INCLUDE:
                        //if(include_first_pass)
                        //{
                        //    PENV->curPos = curTok;
                        //    include_first_pass = FALSE;
                        //    return ';';
                        //}
                        //include_first_pass = TRUE;
                        curPos = skipBlanks(curPos,&state);
                        if(curPos == NULL)
                        {
                            if (state & multiComment) return (BAD_COMMENT_);
                            if(ProcessEOF() == 0) return 0;       // EOF
                            NEXT_TOKEN;
                        }
                        if(Sym(curPos) != '"') return ERROR_;
                        curPos = nextchar(curPos);
                        curTok = curPos;
                        PENV->curTok = curPos;
                        while(Sym(curPos) != '"')
                        {
                            curPos = nextchar(curPos);
                            if(curPos >= PENV->endPos) return ERROR_;
                            PENV->curPos = curPos;
                        }
                        tokLen = PENV->curPos - curTok;
                        curPos = nextchar(curPos);
                        PENV->curPos = curPos;
                        {
                            WCHAR* wzFile=NULL;
                            if(Sym == SymW)
                            {
                                if((wzFile = new WCHAR[tokLen/2 + 1]) != NULL)
                                {
                                    memcpy(wzFile,curTok,tokLen);
                                    wzFile[tokLen/2] = 0;
                                }
                            }
                            else
                            {
                                if((wzFile = new WCHAR[tokLen+1]) != NULL)
                                {
                                    tokLen = WszMultiByteToWideChar(g_uCodePage,0,curTok,(int)tokLen,wzFile,(int)tokLen+1);
                                    wzFile[tokLen] = 0;
                                }
                            }
                            if(wzFile != NULL)
                            {
                                if((parser->wzIncludePath != NULL)
                                 &&(wcschr(wzFile,DIRECTORY_SEPARATOR_CHAR_A)==NULL)
#ifdef TARGET_WINDOWS
                                 &&(wcschr(wzFile,':')==NULL)
#endif
                                )
                                {
                                    PathString wzFullName;

                                    WCHAR* pwz;
                                    DWORD dw = WszSearchPath(parser->wzIncludePath,wzFile,NULL,
                                                TRUE, wzFullName,&pwz);
                                    if(dw != 0)
                                    {
                                        delete [] wzFile;

                                        wzFile = wzFullName.GetCopyOfUnicodeString();
                                    }

                                }
                                if(PASM->m_fReportProgress)
                                    parser->msg("\nIncluding '%S'\n",wzFile);
                                MappedFileStream *pIn = new MappedFileStream(wzFile);
                                if((pIn != NULL)&&pIn->IsValid())
                                {
                                    parser->PEStack.PUSH(PENV);
                                    PASM->SetSourceFileName(FullFileName(wzFile,CP_UTF8)); // deletes the argument!
                                    parser->CreateEnvironment(pIn);
                                    NEXT_TOKEN;
                                }
                                else
                                {
                                    delete [] wzFile;
                                    PASM->report->error("#include failed\n");
                                    return ERROR_;
                                }
                            }
                            else
                            {
                                PASM->report->error("Out of memory\n");
                                return ERROR_;
                            }
                        }
                        curPos = PENV->curPos;
                        curTok = PENV->curTok;
                        break;
                    case P_IFDEF:
                    case P_IFNDEF:
                    case P_DEFINE:
                    case P_UNDEF:
                        curPos = skipBlanks(curPos,&state);
                        if(curPos == NULL)
                        {
                            if (state & multiComment) return (BAD_COMMENT_);
                            if(ProcessEOF() == 0) return 0;       // EOF
                            NEXT_TOKEN;
                        }
                        curTok = curPos;
                        PENV->curTok = curPos;
                        PENV->curPos = curPos;
                        if (!IsValidStartingSymbol(Sym(curPos))) return ERROR_;
                        do
                        {
                            curPos = nextchar(curPos);
                        } while(IsValidContinuingSymbol(Sym(curPos)));
                        tokLen = curPos - curTok;

                        newstr = NewStrFromToken(curTok, tokLen);
                        if((token==P_DEFINE)||(token==P_UNDEF))
                        {
                            if(token == P_DEFINE)
                            {
                                curPos = skipBlanks(curPos,&state);
                                if ((curPos == NULL) && (ProcessEOF() == 0))
                                {
                                    DefineVar(newstr, NULL);
                                    return 0;
                                }
                                curSym = Sym(curPos);
                                if(curSym != '"')
                                    DefineVar(newstr, NULL);
                                else
                                {
                                    tok = parse_literal(curSym, curPos, FALSE);
                                    if(tok == QSTRING)
                                    {
                                        yylval.binstr->appendInt8(' ');
                                        DefineVar(newstr, yylval.binstr);
                                    }
                                    else
                                        return tok;
                                }
                            }
                            else UndefVar(newstr);
                        }
                        else
                        {
                            SkipToken = IsVarDefined(newstr);
                            if(token == P_IFDEF) SkipToken = !SkipToken;
                            IfEndif++;
                            if(SkipToken) IfEndifSkip=IfEndif;
                        }
                        break;
                    case P_ELSE:
                        SkipToken = TRUE;
                        IfEndifSkip=IfEndif;
                        break;
                    case P_ENDIF:
                        if(IfEndif == 0)
                        {
                            PASM->report->error("Unmatched #endif\n");
                            return ERROR_;
                        }
                        IfEndif--;
                        break;
                    default:
                        return(token);
                }
                goto NextToken;
            }
            if(SkipToken)
            {
                switch(token)
                {
                    case P_IFDEF:
                    case P_IFNDEF:
                        IfEndif++;
                        break;
                    case P_ELSE:
                        if(IfEndif == IfEndifSkip) SkipToken = FALSE;
                        break;
                    case P_ENDIF:
                        if(IfEndif == IfEndifSkip) SkipToken = FALSE;
                        IfEndif--;
                        break;
                    default:
                        break;
                }
                //if(yylval.instr) yylval.instr->opcode = -1;
                goto NextToken;
            }
            return(token);
        } // end if token != 0
        if(SkipToken) { curPos = nextBlank(curPos); goto NextToken; }

        VarName* pVarName = FindVarDef(NewStrFromToken(curTok, tokLen));
        if(pVarName != NULL)
        {
            if(pVarName->pbody != NULL)
            {
                BinStrStream *pIn = new BinStrStream(pVarName->pbody);
                if((pIn != NULL)&&pIn->IsValid())
                {
                    PENV->curPos = curPos;
                    parser->PEStack.PUSH(PENV);
                    parser->CreateEnvironment(pIn);
                    NEXT_TOKEN;
                }
            }
        }

        TypeDefDescr* pTDD = findTypedef(curTok,tokLen);

        if(pTDD != NULL)
        {
            yylval.tdd = pTDD;
            PENV->curPos = curPos;
            PENV->curTok = curTok;
            return(TYPEDEF(pTDD));
        }
        if(Sym(curTok) == '#')
        {
            PENV->curPos = curPos;
            PENV->curTok = curTok;
            return(ERROR_);
        }
        // Not a keyword, normal identifiers don't have '.' in them
        if (offsetDot < (size_t)-1)
        {
            if(offsetDotDigit < (size_t)-1)
            {
                curPos = curTok+offsetDotDigit;
                tokLen = offsetDotDigit;
            }
            // protection against something like Foo.Bar..123 or Foo.Bar.
            unsigned D = (Sym == SymW) ? 2 : 1; // Unicode or ANSI/UTF8!
            while((Sym(curPos-D)=='.')&&(tokLen))
            {
                curPos -= D;
                tokLen -= D;
            }
        }
        if((yylval.string = NewStrFromToken(curTok,tokLen)))
        {
            tok = (offsetDot == (size_t)(-1))? ID : DOTTEDNAME;
            //printf("yylex: ID = '%s', curPos=0x%8.8X\n",yylval.string,curPos);
        }
        else return BAD_LITERAL_;
    }
    else if(SkipToken) { curPos = nextBlank(curPos); goto NextToken; }
    else if (IsDigit(curSym)
        || (curSym == '.' && IsDigit(Sym(nextchar(curPos))))
        || (curSym == '-' && IsDigit(Sym(nextchar(curPos)))))
    {
        const char* begNum = curPos;
        unsigned radix = 10;

        neg = (curSym == '-');    // always make it unsigned
        if (neg) curPos = nextchar(curPos);

        if (Sym(curPos) == '0' && Sym(nextchar(curPos)) != '.')
        {
            curPos = nextchar(curPos);
            radix = 8;
            if (Sym(curPos) == 'x' || Sym(curPos) == 'X')
            {
                curPos = nextchar(curPos);
                radix = 16;
            }
        }
        begNum = curPos;
        {
            unsigned __int64 i64 = str2uint64(begNum, const_cast<const char**>(&curPos), radix);
            unsigned __int64 mask64 = neg ? UI64(0xFFFFFFFF80000000) : UI64(0xFFFFFFFF00000000);
            unsigned __int64 largestNegVal32 = UI64(0x0000000080000000);
            if ((i64 & mask64) && (i64 != largestNegVal32))
            {
                yylval.int64 = new __int64(i64);
                tok = INT64;
                if (neg) *yylval.int64 = -*yylval.int64;
            }
            else
            {
                yylval.int32 = (__int32)i64;
                tok = INT32;
                if(neg) yylval.int32 = -yylval.int32;
            }
        }
        if (radix == 10 && ((Sym(curPos) == '.' && Sym(nextchar(curPos)) != '.') || Sym(curPos) == 'E' || Sym(curPos) == 'e'))
        {
            unsigned L = (unsigned)(PENV->endPos - begNum);
            curPos = (char*)begNum + GetDouble((char*)begNum,L,&yylval.float64);
            if (neg) *yylval.float64 = -*yylval.float64;
            tok = FLOAT64;
        }
    }
    else
    {   //      punctuation
        if (curSym == '"' || curSym == '\'')
        {
            return parse_literal(curSym, curPos, TRUE);
        } // end if (*curPos == '"' || *curPos == '\'')
        else if (curSym==':' && Sym(nextchar(curPos))==':')
        {
            curPos = nextchar(nextchar(curPos));
            tok = DCOLON;
        }
        else if(curSym == '.')
        {
            if (Sym(nextchar(curPos))=='.' && Sym(nextchar(nextchar(curPos)))=='.')
            {
                curPos = nextchar(nextchar(nextchar(curPos)));
                tok = ELIPSIS;
            }
            else
            {
                do
                {
                    curPos = nextchar(curPos);
                    if (curPos >= PENV->endPos)
                    return ERROR_;
                    curSym = Sym(curPos);
                }
                while(IsAlNum(curSym) || curSym == '_' || curSym == '$'|| curSym == '@'|| curSym == '?');
                size_t tokLen = curPos - curTok;

                // check to see if it is a keyword
                int token = findKeyword(curTok, tokLen, &yylval.opcode);
                if(token)
                        {
                    //printf("yylex: TOK = %d, curPos=0x%8.8X\n",token,curPos);
                    PENV->curPos = curPos;
                    PENV->curTok = curTok;
                    return(token);
                }
                tok = '.';
                curPos = nextchar(curTok);
            }
        }
        else
        {
Just_A_Character:
            tok = curSym;
            curPos = nextchar(curPos);
        }
        //printf("yylex: PUNCT curPos=0x%8.8X\n",curPos);
    }
    dbprintf(("    Line %d token %d (%c) val = %s\n", PENV->curLine, tok,
            (tok < 128 && isprint(tok)) ? tok : ' ',
            (tok > 255 && tok != INT32 && tok != INT64 && tok!= FLOAT64) ? yylval.string : ""));

    PENV->curPos = curPos;
    PENV->curTok = curTok;
    return(tok);
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

/**************************************************************************/
static char* newString(_In_ __nullterminated const char* str1)
{
    char* ret = new char[strlen(str1)+1];
    if(ret) strcpy_s(ret, strlen(str1)+1, str1);
    return(ret);
}

/**************************************************************************/
/* concatenate strings and release them */

static char* newStringWDel(_In_ __nullterminated char* str1, char delimiter, _In_ __nullterminated char* str3)
{
    size_t len1 = strlen(str1);
    size_t len = len1+2;
    if (str3) len += strlen(str3);
    char* ret = new char[len];
    if(ret)
    {
        strcpy_s(ret, len, str1);
        delete [] str1;
        ret[len1] = delimiter;
        ret[len1+1] = 0;
        if (str3)
        {
            strcat_s(ret, len, str3);
            delete [] str3;
        }
    }
    return(ret);
}

/**************************************************************************/
static void corEmitInt(BinStr* buff, unsigned data)
{
    unsigned cnt = CorSigCompressData(data, buff->getBuff(5));
    buff->remove(5 - cnt);
}


/**************************************************************************/
/* move 'ptr past the exactly one type description */

unsigned __int8* skipType(unsigned __int8* ptr, BOOL fFixupType)
{
    mdToken  tk;
AGAIN:
    switch(*ptr++) {
        case ELEMENT_TYPE_VOID         :
        case ELEMENT_TYPE_BOOLEAN      :
        case ELEMENT_TYPE_CHAR         :
        case ELEMENT_TYPE_I1           :
        case ELEMENT_TYPE_U1           :
        case ELEMENT_TYPE_I2           :
        case ELEMENT_TYPE_U2           :
        case ELEMENT_TYPE_I4           :
        case ELEMENT_TYPE_U4           :
        case ELEMENT_TYPE_I8           :
        case ELEMENT_TYPE_U8           :
        case ELEMENT_TYPE_R4           :
        case ELEMENT_TYPE_R8           :
        case ELEMENT_TYPE_U            :
        case ELEMENT_TYPE_I            :
        case ELEMENT_TYPE_STRING       :
        case ELEMENT_TYPE_OBJECT       :
        case ELEMENT_TYPE_TYPEDBYREF   :
        case ELEMENT_TYPE_SENTINEL     :
                /* do nothing */
                break;

        case ELEMENT_TYPE_VALUETYPE   :
        case ELEMENT_TYPE_CLASS        :
                ptr += CorSigUncompressToken(ptr, &tk);
                break;

        case ELEMENT_TYPE_CMOD_REQD    :
        case ELEMENT_TYPE_CMOD_OPT     :
                ptr += CorSigUncompressToken(ptr, &tk);
                goto AGAIN;

        case ELEMENT_TYPE_ARRAY         :
                {
                    ptr = skipType(ptr, fFixupType);                    // element Type
                    unsigned rank = CorSigUncompressData((PCCOR_SIGNATURE&) ptr);
                    if (rank != 0)
                    {
                        unsigned numSizes = CorSigUncompressData((PCCOR_SIGNATURE&) ptr);
                        while(numSizes > 0)
                                                {
                            CorSigUncompressData((PCCOR_SIGNATURE&) ptr);
                                                        --numSizes;
                                                }
                        unsigned numLowBounds = CorSigUncompressData((PCCOR_SIGNATURE&) ptr);
                        while(numLowBounds > 0)
                                                {
                            CorSigUncompressData((PCCOR_SIGNATURE&) ptr);
                                                        --numLowBounds;
                                                }
                    }
                }
                break;

                // Modifiers or dependent types
        case ELEMENT_TYPE_PINNED                :
        case ELEMENT_TYPE_PTR                   :
        case ELEMENT_TYPE_BYREF                 :
        case ELEMENT_TYPE_SZARRAY               :
                // tail recursion optimization
                // ptr = skipType(ptr, fFixupType);
                // break
                goto AGAIN;

        case ELEMENT_TYPE_VAR:
        case ELEMENT_TYPE_MVAR:
                CorSigUncompressData((PCCOR_SIGNATURE&) ptr);  // bound
                break;

        case ELEMENT_TYPE_VARFIXUP:
        case ELEMENT_TYPE_MVARFIXUP:
                if(fFixupType)
                {
                    BYTE* pb = ptr-1; // ptr incremented in switch
                    unsigned __int8* ptr_save = ptr;
                    int n = CorSigUncompressData((PCCOR_SIGNATURE&) ptr);  // fixup #
                    int compressed_size_n = (int)(ptr - ptr_save);  // ptr was updated by CorSigUncompressData()
                    int m = -1;
                    if(PASM->m_TyParList)
                        m = PASM->m_TyParList->IndexOf(TyParFixupList.PEEK(n));
                    if(m == -1)
                    {
                        PASM->report->error("(fixupType) Invalid %stype parameter '%s'\n",
                            (*pb == ELEMENT_TYPE_MVARFIXUP)? "method ": "",
                            TyParFixupList.PEEK(n));
                        m = 0;
                    }
                    *pb = (*pb == ELEMENT_TYPE_MVARFIXUP)? ELEMENT_TYPE_MVAR : ELEMENT_TYPE_VAR;
                    int compressed_size_m = (int)CorSigCompressData(m,pb+1);

                    // Note that CorSigCompressData() (and hence, CorSigUncompressData()) store a number
                    // 0 <= x <= 0x1FFFFFFF in 1, 2, or 4 bytes. Above, 'n' is the fixup number being read,
                    // and 'm' is the generic parameter number being written out (in the same place where 'n'
                    // came from). If 'n' takes more space to compress than 'm' (e.g., 0x80 <= n <= 0x3fff so
                    // it takes 2 bytes, and m < 0x80 so it takes one byte), then when we overwrite the fixup
                    // number with the generic parameter number, we'll leave extra bytes in the signature following
                    // the written generic parameter number. Thus, we do something of a hack to ensure that the
                    // compressed number is correctly readable even if 'm' compresses smaller than 'n' did: we
                    // recompress 'm' to use the same amount of space as 'n' used. This is possible because smaller
                    // numbers can still be compressed in a larger amount of space, even though it's not optimal (and
                    // CorSigCompressData() would never do it). If, however, the compressed sizes are the other
                    // way around (m takes more space to compress than n), then we've already corrupted the
                    // signature that we're reading by writing beyond what we should (is there some reason why
                    // this is not possible?).
                    // Note that 'ptr' has already been adjusted, above, to point to the next type after this one.
                    // There is no need to update it when recompressing the data.

                    if (compressed_size_m > compressed_size_n)
                    {
                        // We've got a problem: we just corrupted the rest of the signature!
                        // (Can this ever happen in practice?)
                        PASM->report->error("(fixupType) Too many %stype parameters\n",
                            (*pb == ELEMENT_TYPE_MVARFIXUP)? "method ": "");
                    }
                    else if (compressed_size_m < compressed_size_n)
                    {
                        // We didn't write out as much data as we read. This will leave extra bytes in the
                        // signature that will be incorrectly recognized. Ideally, we would just shrink the
                        // signature. That's not easy to do here. Instead, pad the bytes to force it to use
                        // a larger encoding than needed. This assumes knowledge of the CorSigCompressData()
                        // encoding.
                        //
                        // The cases:
                        //      compressed_size_m   m bytes     compressed_size_n   result bytes
                        //      1                   m1          2                   0x80 m1
                        //      1                   m1          4                   0xC0 0x00 0x00 m1
                        //      2                   m1 m2       4                   0xC0 0x00 (m1 & 0x7f) m2

                        _ASSERTE((compressed_size_m == 1) || (compressed_size_m == 2) || (compressed_size_m == 4));
                        _ASSERTE((compressed_size_n == 1) || (compressed_size_n == 2) || (compressed_size_n == 4));

                        if ((compressed_size_m == 1) &&
                            (compressed_size_n == 2))
                        {
                            unsigned __int8 m1 = *(pb + 1);
                            _ASSERTE(m1 < 0x80);
                            *(pb + 1) = 0x80;
                            *(pb + 2) = m1;
                        }
                        else
                        if ((compressed_size_m == 1) &&
                            (compressed_size_n == 4))
                        {
                            unsigned __int8 m1 = *(pb + 1);
                            _ASSERTE(m1 < 0x80);
                            *(pb + 1) = 0xC0;
                            *(pb + 2) = 0x00;
                            *(pb + 3) = 0x00;
                            *(pb + 4) = m1;
                        }
                        else
                        if ((compressed_size_m == 2) &&
                            (compressed_size_n == 4))
                        {
                            unsigned __int8 m1 = *(pb + 1);
                            unsigned __int8 m2 = *(pb + 2);
                            _ASSERTE(m1 >= 0x80);
                            m1 &= 0x7f; // strip the bit indicating it's a 2-byte thing
                            *(pb + 1) = 0xC0;
                            *(pb + 2) = 0x00;
                            *(pb + 3) = m1;
                            *(pb + 4) = m2;
                        }
                    }
                }
                else
                    CorSigUncompressData((PCCOR_SIGNATURE&) ptr);  // bound
                break;

        case ELEMENT_TYPE_FNPTR:
                {
                    CorSigUncompressData((PCCOR_SIGNATURE&) ptr);    // calling convention
                    unsigned argCnt = CorSigUncompressData((PCCOR_SIGNATURE&) ptr);    // arg count
                    ptr = skipType(ptr, fFixupType);                             // return type
                    while(argCnt > 0)
                    {
                        ptr = skipType(ptr, fFixupType);
                        --argCnt;
                    }
                }
                break;

        case ELEMENT_TYPE_GENERICINST:
               {
                   ptr = skipType(ptr, fFixupType);                 // type constructor
                   unsigned argCnt = CorSigUncompressData((PCCOR_SIGNATURE&)ptr);               // arg count
                   while(argCnt > 0) {
                       ptr = skipType(ptr, fFixupType);
                       --argCnt;
                   }
               }
               break;

        default:
        case ELEMENT_TYPE_END                   :
                _ASSERTE(!"Unknown Type");
                break;
    }
    return(ptr);
}

/**************************************************************************/
void FixupTyPars(PCOR_SIGNATURE pSig, ULONG cSig)
{
    if(TyParFixupList.COUNT() > 0)
    {
        BYTE* ptr = (BYTE*)pSig;
        BYTE* ptrEnd = ptr + cSig;
        while(ptr < ptrEnd)
        {
            ptr = skipType(ptr, TRUE);
        } // end while
    } // end if(COUNT>0)
}
void FixupTyPars(BinStr* pbstype)
{
    FixupTyPars((PCOR_SIGNATURE)(pbstype->ptr()),(ULONG)(pbstype->length()));
}
/**************************************************************************/
static unsigned corCountArgs(BinStr* args)
{
    unsigned __int8* ptr = args->ptr();
    unsigned __int8* end = &args->ptr()[args->length()];
    unsigned ret = 0;
    while(ptr < end)
    {
        if (*ptr != ELEMENT_TYPE_SENTINEL)
        {
            ptr = skipType(ptr, FALSE);
            ret++;
        }
        else ptr++;
    }
    return(ret);
}

/********************************************************************************/
AsmParse::AsmParse(ReadStream* aIn, Assembler *aAssem)
{
#ifdef DEBUG_PARSING
    extern int yydebug;
    yydebug = 1;
#endif

    assem = aAssem;
    assem->SetErrorReporter((ErrorReporter *)this);

    assem->m_ulCurLine = 1;
    assem->m_ulCurColumn = 1;

    wzIncludePath = NULL;
    penv = NULL;

    hstdout = GetStdHandle(STD_OUTPUT_HANDLE);
    hstderr = GetStdHandle(STD_ERROR_HANDLE);

    success = true;
    _ASSERTE(parser == 0);          // Should only be one parser instance at a time

   // Resolve aliases
   for (unsigned int i = 0; i < sizeof(keywords) / sizeof(Keywords); i++)
   {
       if (keywords[i].token == NO_VALUE)
           keywords[i].token = keywords[keywords[i].tokenVal].token;
   }
    SetSymbolTables();
    Init_str2uint64();
    parser = this;
    //yyparse();
}

/********************************************************************************/
AsmParse::~AsmParse()
{
    parser = 0;
    delete penv;
    while(m_ANSLast.POP());
}

/**************************************************************************/
void AsmParse::CreateEnvironment(ReadStream* stream)
{
    penv = new PARSING_ENVIRONMENT;
    memset(penv,0,sizeof(PARSING_ENVIRONMENT));
    penv->in = stream;
    penv->curLine = 1;
    strcpy_s(penv->szFileName, MAX_FILENAME_LENGTH*3+1,assem->m_szSourceFileName);

    penv->curPos = fillBuff(NULL);
    penv->uCodePage = g_uCodePage;

    SetFunctionPtrs();
};

/**************************************************************************/
void AsmParse::ParseFile(ReadStream* stream)
{
    CreateEnvironment(stream);
    yyparse();
    penv->in = NULL;
};

/**************************************************************************/
char* AsmParse::fillBuff(_In_opt_z_ char* pos)
{
    int iPutToBuffer;
    g_uCodePage = CP_UTF8;
    iPutToBuffer = (int)penv->in->getAll(&(penv->curPos));

    penv->endPos = penv->curPos + iPutToBuffer;
    if(iPutToBuffer > 128) iPutToBuffer = 128;
    if(iPutToBuffer >= 4 && (penv->curPos[0] & 0xFF) == 0xFF && (penv->curPos[1] & 0xFF) == 0xFE)
    {
        // U+FFFE followed by U+0000 is UTF-32 LE, any other value than 0 is a true UTF-16 LE
        if((penv->curPos[2] & 0xFF) == 0x00 && (penv->curPos[3] & 0xFF) == 0x00)
        {
            error("UTF-32 LE is not supported\n\n");
            return NULL;
        }

        penv->curPos += 2; // skip signature
        if(assem->m_fReportProgress) printf("Source file is UNICODE\n\n");
        penv->pfn_Sym = SymW;
        penv->pfn_nextchar = nextcharW;
        penv->pfn_NewStrFromToken = NewStrFromTokenW;
        penv->pfn_NewStaticStrFromToken = NewStaticStrFromTokenW;
        penv->pfn_GetDouble = GetDoubleW;
    }
    else
    {
        if((penv->curPos[0] & 0xFF) == 0xEF && (penv->curPos[1] & 0xFF) == 0xBB && (penv->curPos[2] & 0xFF) == 0xBF)
        {
            penv->curPos += 3;
        }

        if(assem->m_fReportProgress) printf("Source file is UTF-8\n\n");
        penv->pfn_nextchar = nextcharU;
        penv->pfn_Sym = SymAU;
        penv->pfn_NewStrFromToken = NewStrFromTokenAU;
        penv->pfn_NewStaticStrFromToken = NewStaticStrFromTokenAU;
        penv->pfn_GetDouble = GetDoubleAU;
    }
    return(penv->curPos);
}

/********************************************************************************/
BinStr* AsmParse::MakeSig(unsigned callConv, BinStr* retType, BinStr* args, int ntyargs)
{
    _ASSERTE((ntyargs != 0) == ((callConv & IMAGE_CEE_CS_CALLCONV_GENERIC) != 0));
    BinStr* ret = new BinStr();
    if(ret)
    {
        //if (retType != 0)
        ret->insertInt8(callConv);
        if (ntyargs != 0)
            corEmitInt(ret, ntyargs);
        corEmitInt(ret, corCountArgs(args));

        if (retType != 0)
        {
            ret->append(retType);
            delete retType;
        }
        ret->append(args);
    }
    else
        error("\nOut of memory!\n");

    delete args;
    return(ret);
}

/********************************************************************************/
BinStr* AsmParse::MakeTypeArray(CorElementType kind, BinStr* elemType, BinStr* bounds)
{
    // 'bounds' is a binary buffer, that contains an array of 'struct Bounds'
    struct Bounds {
        int lowerBound;
        unsigned numElements;
    };

    _ASSERTE(bounds->length() % sizeof(Bounds) == 0);
    unsigned boundsLen = bounds->length() / sizeof(Bounds);
    _ASSERTE(boundsLen > 0);
    Bounds* boundsArr = (Bounds*) bounds->ptr();

    BinStr* ret = new BinStr();

    ret->appendInt8(kind);
    ret->append(elemType);
    corEmitInt(ret, boundsLen);                     // emit the rank

    unsigned lowerBoundsDefined = 0;
    unsigned numElementsDefined = 0;
    unsigned i;
    for(i=0; i < boundsLen; i++)
    {
        if(boundsArr[i].lowerBound < 0x7FFFFFFF) lowerBoundsDefined = i+1;
        else boundsArr[i].lowerBound = 0;

        if(boundsArr[i].numElements < 0x7FFFFFFF) numElementsDefined = i+1;
        else boundsArr[i].numElements = 0;
    }

    corEmitInt(ret, numElementsDefined);                    // emit number of bounds

    for(i=0; i < numElementsDefined; i++)
    {
        _ASSERTE (boundsArr[i].numElements >= 0);               // enforced at rule time
        corEmitInt(ret, boundsArr[i].numElements);

    }

    corEmitInt(ret, lowerBoundsDefined);    // emit number of lower bounds
    for(i=0; i < lowerBoundsDefined; i++)
        {
                unsigned cnt = CorSigCompressSignedInt(boundsArr[i].lowerBound, ret->getBuff(5));
                ret->remove(5 - cnt);
        }
    delete elemType;
    delete bounds;
    return(ret);
}

/********************************************************************************/
BinStr* AsmParse::MakeTypeClass(CorElementType kind, mdToken tk)
{

    BinStr* ret = new BinStr();
    _ASSERTE(kind == ELEMENT_TYPE_CLASS || kind == ELEMENT_TYPE_VALUETYPE ||
                     kind == ELEMENT_TYPE_CMOD_REQD || kind == ELEMENT_TYPE_CMOD_OPT);
    ret->appendInt8(kind);
    unsigned cnt = CorSigCompressToken(tk, ret->getBuff(5));
    ret->remove(5 - cnt);
    return(ret);
}
/**************************************************************************/
void PrintANSILine(FILE* pF, _In_ __nullterminated char* sz)
{
        WCHAR *wz = &wzUniBuf[0];
        if(g_uCodePage != CP_ACP)
        {
                memset(wz,0,dwUniBuf); // dwUniBuf/2 WCHARs = dwUniBuf bytes
                WszMultiByteToWideChar(g_uCodePage,0,sz,-1,wz,(dwUniBuf >> 1)-1);

                memset(sz,0,dwUniBuf);
                WszWideCharToMultiByte(g_uConsoleCP,0,wz,-1,sz,dwUniBuf-1,NULL,NULL);
        }
        fprintf(pF,"%s",sz);
}
/**************************************************************************/
void AsmParse::error(const char* fmt, ...)
{
    char *sz = (char*)(&wzUniBuf[(dwUniBuf >> 1)]);
    char *psz=&sz[0];
    FILE* pF = ((!assem->m_fReportProgress)&&(assem->OnErrGo)) ? stdout : stderr;
    success = false;
    va_list args;
    va_start(args, fmt);

    if((penv) && (penv->in)) psz+=sprintf_s(psz, (dwUniBuf >> 1), "%S(%d) : ", penv->in->namew(), penv->curLine);
    psz+=sprintf_s(psz, (dwUniBuf >> 1), "error : ");
    _vsnprintf_s(psz, (dwUniBuf >> 1),(dwUniBuf >> 1)-strlen(sz)-1, fmt, args);
    PrintANSILine(pF,sz);
}

/**************************************************************************/
void AsmParse::warn(const char* fmt, ...)
{
    char *sz = (char*)(&wzUniBuf[(dwUniBuf >> 1)]);
    char *psz=&sz[0];
    FILE* pF = ((!assem->m_fReportProgress)&&(assem->OnErrGo)) ? stdout : stderr;
    va_list args;
    va_start(args, fmt);

    if((penv) && (penv->in)) psz+=sprintf_s(psz, (dwUniBuf >> 1), "%S(%d) : ", penv->in->namew(), penv->curLine);
    psz+=sprintf_s(psz, (dwUniBuf >> 1), "warning : ");
    _vsnprintf_s(psz, (dwUniBuf >> 1),(dwUniBuf >> 1)-strlen(sz)-1, fmt, args);
    PrintANSILine(pF,sz);
}
/**************************************************************************/
void AsmParse::msg(const char* fmt, ...)
{
    char *sz = (char*)(&wzUniBuf[(dwUniBuf >> 1)]);
    va_list args;
    va_start(args, fmt);

    _vsnprintf_s(sz, (dwUniBuf >> 1),(dwUniBuf >> 1)-1, fmt, args);
    PrintANSILine(stdout,sz);
}

#ifdef _MSC_VER
#pragma warning(default : 4640)
#endif
