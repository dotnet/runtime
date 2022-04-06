// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/**************************************************************************/
/* asmParse is basically a wrapper around a YACC grammer COM+ assembly  */

#ifndef asmparse_h
#define asmparse_h

#include <stdio.h>		// for FILE

#include "assembler.h"	// for ErrorReporter Labels
//class Assembler;
//class BinStr;


/**************************************************************************/
/* an abstraction of a stream of input characters */
class ReadStream {
public:

    virtual ~ReadStream() = default;

    virtual unsigned getAll(_Out_ char** ppch) = 0;

    // read at most 'buffLen' bytes into 'buff', Return the
        // number of characters read.  On EOF return 0
    virtual unsigned read(_Out_writes_(buffLen) char* buff, unsigned buffLen) = 0;

        // Return the name of the stream, (for error reporting).
    //virtual const char* name() = 0;
        // Return the Unicode name of the stream
    virtual const WCHAR* namew() = 0;
		//return ptr to buffer containing specified source line
	virtual char* getLine(int lineNum) = 0;
};

/**************************************************************************/
class BinStrStream : public ReadStream {
public:
    BinStrStream(BinStr* pbs)
    {
        m_pStart = (char*)(pbs->ptr());
        m_pCurr = m_pStart;
        m_pEnd = m_pStart + pbs->length();
        m_pBS = pbs;
    };
    ~BinStrStream()
    {
        //if(m_pBS)
        //    delete m_pBS;
    };
    unsigned getAll(_Out_ char **ppbuff)
    {
        *ppbuff = m_pStart;
        return m_pBS->length();
    };
    unsigned read(_Out_writes_(buffLen) char* buff, unsigned buffLen)
    {
        _ASSERTE(m_pStart != NULL);
        unsigned Remainder = (unsigned)(m_pEnd - m_pCurr);
        unsigned Len = buffLen;
        if(Len > Remainder) Len = Remainder;
        memcpy(buff,m_pCurr,Len);
        m_pCurr += Len;
        if(Len < buffLen)
        {
            memset(buff+Len,0,buffLen-Len);
        }
        return Len;
    }

    const WCHAR* namew()
    {
        return W("local_define");
    }

    BOOL IsValid()
    {
        return(m_pStart != NULL);
    }

	char* getLine(int lineNum)
	{
        return NULL; // this function is not used
	}

private:
    char*   m_pStart;
    char*   m_pEnd;
    char*   m_pCurr;
    BinStr* m_pBS;


};
/**************************************************************************/
class MappedFileStream : public ReadStream {
public:
    MappedFileStream(_In_ __nullterminated WCHAR* wFileName)
    {
        fileNameW = wFileName;
        m_hFile = INVALID_HANDLE_VALUE;
        m_hMapFile = NULL;
        m_pStart = open(wFileName);
        m_pCurr = m_pStart;
        m_pEnd = m_pStart + m_FileSize;
		//memset(fileNameANSI,0,MAX_FILENAME_LENGTH*4);
		//WszWideCharToMultiByte(CP_ACP,0,wFileName,-1,fileNameANSI,MAX_FILENAME_LENGTH*4,NULL,NULL);
    }
    ~MappedFileStream()
    {
        if (m_hFile != INVALID_HANDLE_VALUE)
        {
            if (m_pStart)
                UnmapViewOfFile((void*)m_pStart);
            if (m_hMapFile)
                CloseHandle(m_hMapFile);
            CloseHandle(m_hFile);

            m_pStart = NULL;
            m_hMapFile = NULL;
            m_hFile = INVALID_HANDLE_VALUE;
            m_FileSize = 0;
            delete [] fileNameW;
            fileNameW = NULL;
        }
    }
    unsigned getAll(_Out_ char** pbuff)
    {
        *pbuff = m_pStart;
        return m_FileSize;
    }
    unsigned read(_Out_writes_(buffLen) char* buff, unsigned buffLen)
    {
        _ASSERTE(m_pStart != NULL);
        unsigned Remainder = (unsigned)(m_pEnd - m_pCurr);
        unsigned Len = buffLen;
        if(Len > Remainder) Len = Remainder;
        memcpy(buff,m_pCurr,Len);
        m_pCurr += Len;
        if(Len < buffLen)
        {
            memset(buff+Len,0,buffLen-Len);
        }
        return Len;
    }

    //const char* name()
    //{
    //    return(&fileNameANSI[0]);
    //}

    const WCHAR* namew()
    {
        return fileNameW;
    }

    void set_namew(const WCHAR* namew)
    {
        fileNameW = namew;
    }

    BOOL IsValid()
    {
        return(m_pStart != NULL);
    }

	char* getLine(int lineNum)
	{
        return NULL; // this function is not used
	}

private:
    char* map_file()
    {
        DWORD dwFileSizeLow;

        dwFileSizeLow = GetFileSize( m_hFile, NULL);
        if (dwFileSizeLow == INVALID_FILE_SIZE)
            return NULL;
        m_FileSize = dwFileSizeLow;

        // No difference between A and W in this case: last param (LPCTSTR) is NULL
        m_hMapFile = WszCreateFileMapping(m_hFile, NULL, PAGE_READONLY, 0, 0, NULL);
        if (m_hMapFile == NULL)
            return NULL;

        return (char*)(HMODULE) MapViewOfFile(m_hMapFile, FILE_MAP_READ, 0, 0, 0);
    }
    char* open(const WCHAR* moduleName)
    {
        _ASSERTE(moduleName);
        if (!moduleName)
            return NULL;

        m_hFile = WszCreateFile(moduleName, GENERIC_READ, FILE_SHARE_READ,
                             0, OPEN_EXISTING, 0, 0);
        return (m_hFile == INVALID_HANDLE_VALUE) ? NULL : map_file();
    }

    const WCHAR* fileNameW;     // FileName (for error reporting)
	//char	fileNameANSI[MAX_FILENAME_LENGTH*4];
    HANDLE  m_hFile;                 // File we are reading from
    DWORD   m_FileSize;
    HANDLE  m_hMapFile;
    char*   m_pStart;
    char*   m_pEnd;
    char*   m_pCurr;

};

typedef LIFO<ARG_NAME_LIST> ARG_NAME_LIST_STACK;

// functional pointers used in parsing
/*--------------------------------------------------------------------------*/
typedef char*(*PFN_NEXTCHAR)(char*);

char* nextcharU(_In_ __nullterminated char* pos);
char* nextcharW(_In_ __nullterminated char* pos);

/*--------------------------------------------------------------------------*/
typedef unsigned(*PFN_SYM)(char*);

unsigned SymAU(_In_ __nullterminated char* curPos);
unsigned SymW(_In_ __nullterminated char* curPos);
/*--------------------------------------------------------------------------*/
typedef char*(*PFN_NEWSTRFROMTOKEN)(char*,size_t);

char* NewStrFromTokenAU(_In_reads_(tokLen) char* curTok, size_t tokLen);
char* NewStrFromTokenW(_In_reads_(tokLen) char* curTok, size_t tokLen);
/*--------------------------------------------------------------------------*/
typedef char*(*PFN_NEWSTATICSTRFROMTOKEN)(char*,size_t,char*,size_t);

char* NewStaticStrFromTokenAU(_In_reads_(tokLen) char* curTok, size_t tokLen, _Out_writes_(bufSize) char* staticBuf, size_t bufSize);
char* NewStaticStrFromTokenW(_In_reads_(tokLen) char* curTok, size_t tokLen, _Out_writes_(bufSize) char* staticBuf, size_t bufSize);
/*--------------------------------------------------------------------------*/
typedef unsigned(*PFN_GETDOUBLE)(char*,unsigned,double**);

unsigned GetDoubleAU(_In_ __nullterminated char* begNum, unsigned L, double** ppRes);
unsigned GetDoubleW(_In_ __nullterminated char* begNum, unsigned L, double** ppRes);
/*--------------------------------------------------------------------------*/
struct PARSING_ENVIRONMENT
{
    char* curTok;         		// The token we are in the process of processing (for error reporting)
    char* curPos;               // current place in input buffer
    char* endPos;				// points just past the end of valid data in the buffer

    ReadStream* in;             // how we fill up our buffer

    PFN_NEXTCHAR    pfn_nextchar;
    PFN_SYM         pfn_Sym;
    PFN_NEWSTRFROMTOKEN pfn_NewStrFromToken;
    PFN_NEWSTATICSTRFROMTOKEN pfn_NewStaticStrFromToken;
    PFN_GETDOUBLE   pfn_GetDouble;

    bool bExternSource;
    bool bExternSourceAutoincrement;
    unsigned  nExtLine;
    unsigned  nExtCol;
    unsigned  nExtLineEnd;
    unsigned  nExtColEnd;
    unsigned curLine;           // Line number (for error reporting)

    unsigned  uCodePage;

    char    szFileName[MAX_FILENAME_LENGTH*3+1];

};
typedef LIFO<PARSING_ENVIRONMENT> PARSING_ENVIRONMENT_STACK;

/**************************************************************************/
/* AsmParse does all the parsing.  It also builds up simple data structures,
   (like signatures), but does not do the any 'heavy lifting' like define
   methods or classes.  Instead it calls to the Assembler object to do that */

class AsmParse : public ErrorReporter
{
public:
    AsmParse(ReadStream* stream, Assembler *aAssem);
    virtual ~AsmParse();
    void CreateEnvironment(ReadStream* stream);
	void ParseFile(ReadStream* stream);
        // The parser knows how to put line numbers on things and report the error
    virtual void error(const char* fmt, ...);
    virtual void warn(const char* fmt, ...);
    virtual void msg(const char* fmt, ...);
	char *getLine(int lineNum) { return penv->in->getLine(lineNum); };
    unsigned getAll(_Out_ char** pbuff) { return penv->in->getAll(pbuff); };
	bool Success() {return success; };
    void SetIncludePath(_In_ WCHAR* wz) { wzIncludePath = wz; };

    ARG_NAME_LIST_STACK  m_ANSFirst;
    ARG_NAME_LIST_STACK  m_ANSLast;
    PARSING_ENVIRONMENT *penv;
    PARSING_ENVIRONMENT_STACK PEStack;

private:
    BinStr* MakeSig(unsigned callConv, BinStr* retType, BinStr* args, int ntyargs = 0);
    BinStr* MakeTypeClass(CorElementType kind, mdToken tk);
    BinStr* MakeTypeArray(CorElementType kind, BinStr* elemType, BinStr* bounds);

    char* fillBuff(_In_opt_z_ char* curPos);   // refill the input buffer
    HANDLE hstdout;
    HANDLE hstderr;

private:
    friend void yyerror(_In_ __nullterminated const char* str);
    friend int parse_literal(unsigned curSym, __inout __nullterminated char* &curPos, BOOL translate_escapes);
    friend int yyparse();
    friend int yylex();
    friend Instr* SetupInstr(unsigned short opcode);
    friend int findKeyword(const char* name, size_t nameLen, unsigned short* opcode);
    friend TypeDefDescr* findTypedef(_In_reads_(nameLen) char* name, size_t nameLen);
    friend char* skipBlanks(_In_ __nullterminated char*,unsigned*);
    friend char* nextBlank(_In_ __nullterminated char*);
    friend int ProcessEOF();
    friend unsigned __int8* skipType(unsigned __int8* ptr, BOOL fFixupType);
    friend void FixupConstraints();

	Assembler* assem;			// This does most of the semantic processing
    bool success;               // overall success of the compilation
    WCHAR* wzIncludePath;
};

#endif

