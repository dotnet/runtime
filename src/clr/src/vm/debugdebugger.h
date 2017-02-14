// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Header: DebugDebugger.h
**
** Purpose: Native methods on System.Debug.Debugger
**
**

===========================================================*/

#ifndef __DEBUG_DEBUGGER_h__
#define __DEBUG_DEBUGGER_h__
#include <object.h>


// ! WARNING !
// The following constants mirror the constants 
// declared in the class LoggingLevelEnum in the 
// System.Diagnostic package. Any changes here will also
// need to be made there.
#define     TraceLevel0     0
#define     TraceLevel1     1
#define     TraceLevel2     2
#define     TraceLevel3     3
#define     TraceLevel4     4
#define     StatusLevel0    20
#define     StatusLevel1    21
#define     StatusLevel2    22
#define     StatusLevel3    23
#define     StatusLevel4    24
#define     WarningLevel    40
#define     ErrorLevel      50
#define     PanicLevel      100

// ! WARNING !
// The following constants mirror the constants 
// declared in the class AssertLevelEnum in the 
// System.Diagnostic package. Any changes here will also
// need to be made there.
#define     FailDebug           0
#define     FailIgnore          1
#define     FailTerminate       2
#define     FailContinueFilter  3

#define     MAX_LOG_SWITCH_NAME_LEN     256

class DebugDebugger
{
public:
    static FCDECL0(void,  Break);
    static FCDECL0(FC_BOOL_RET, Launch);
    static FCDECL0(FC_BOOL_RET, IsDebuggerAttached);
    static FCDECL3(void,  Log, INT32 Level, StringObject* strModule, StringObject* strMessage);

    // receives a custom notification object from the target and sends it to the RS via 
    // code:Debugger::SendCustomDebuggerNotification
    static FCDECL1(void,  CustomNotification, Object * dataUNSAFE); 

    static FCDECL0(FC_BOOL_RET, IsLogging);

protected:
    static BOOL IsLoggingHelper();
};




class StackFrameHelper : public Object
{
    // READ ME:
    // Modifying the order or fields of this object may require other changes to the
    // classlib defintion of the StackFrameHelper class.
public:
    THREADBASEREF targetThread;
    I4ARRAYREF rgiOffset;
    I4ARRAYREF rgiILOffset;
    BASEARRAYREF rgMethodBase; 
    PTRARRAYREF dynamicMethods;    
    BASEARRAYREF rgMethodHandle; 
    PTRARRAYREF rgAssemblyPath;
    BASEARRAYREF rgLoadedPeAddress;
    I4ARRAYREF rgiLoadedPeSize;
    BASEARRAYREF rgInMemoryPdbAddress;
    I4ARRAYREF rgiInMemoryPdbSize;
    // if rgiMethodToken[i] == 0, then don't attempt to get the portable PDB source/info
    I4ARRAYREF rgiMethodToken;
    PTRARRAYREF rgFilename;
    I4ARRAYREF rgiLineNumber;
    I4ARRAYREF rgiColumnNumber;

    BOOLARRAYREF rgiLastFrameFromForeignExceptionStackTrace;

    OBJECTREF getSourceLineInfo;
    int iFrameCount;

protected:
    StackFrameHelper() {}
    ~StackFrameHelper() {}

public:
    void SetFrameCount (int iCount) 
    { 
        iFrameCount = iCount;
    }

    int  GetFrameCount (void) 
    { 
        return iFrameCount;
    }

};

#ifdef USE_CHECKED_OBJECTREFS
typedef REF <StackFrameHelper> STACKFRAMEHELPERREF;
#else
typedef StackFrameHelper* STACKFRAMEHELPERREF;
#endif


class DebugStackTrace
{   
public:

#ifndef DACCESS_COMPILE
// the DAC directly uses the GetStackFramesData and DebugStackTraceElement types
private:
#endif // DACCESS_COMPILE
    struct DebugStackTraceElement {
        DWORD dwOffset;  // native offset
        DWORD dwILOffset;
        MethodDesc *pFunc;
        PCODE ip;
        // TRUE if this element represents the last frame of the foreign
        // exception stack trace.
        BOOL			fIsLastFrameFromForeignStackTrace;

        // Initialization done under TSL.
        // This is used when first collecting the stack frame data.
        void InitPass1(
            DWORD dwNativeOffset,
            MethodDesc *pFunc,
            PCODE ip
            , BOOL			fIsLastFrameFromForeignStackTrace = FALSE
			);

        // Initialization done outside the TSL.
        // This will init the dwILOffset field (and potentially anything else
        // that can't be done under the TSL).
        void InitPass2();
    };

public:

    struct GetStackFramesData {

        // Used for the integer-skip version
        INT32   skip;
        INT32   NumFramesRequested;
        INT32   cElementsAllocated;
        INT32   cElements;
        DebugStackTraceElement* pElements;
        THREADBASEREF   TargetThread;
        AppDomain *pDomain;
        BOOL	fDoWeHaveAnyFramesFromForeignStackTrace;


        GetStackFramesData() :  skip(0), 
                                NumFramesRequested (0),
                                cElementsAllocated(0), 
                                cElements(0), 
                                pElements(NULL),
                                TargetThread((THREADBASEREF)(TADDR)NULL)
        { 
            LIMITED_METHOD_CONTRACT;
            fDoWeHaveAnyFramesFromForeignStackTrace = FALSE;

        }

        ~GetStackFramesData()
        {
            delete [] pElements;
        }
    };

    static FCDECL4(void, 
                   GetStackFramesInternal, 
                   StackFrameHelper* pStackFrameHelper, 
                   INT32 iSkip, 
                   CLR_BOOL fNeedFileInfo,
                   Object* pException
                  );

    static void GetStackFramesFromException(OBJECTREF * e, GetStackFramesData *pData, PTRARRAYREF * pDynamicMethodArray = NULL);

#ifndef DACCESS_COMPILE
// the DAC directly calls GetStackFramesFromException
private:
#endif
    
    static void GetStackFramesHelper(Frame *pStartFrame, void* pStopStack, GetStackFramesData *pData);

    static void GetStackFrames(Frame *pStartFrame, void* pStopStack, GetStackFramesData *pData);    

    static StackWalkAction GetStackFramesCallback(CrawlFrame* pCf, VOID* data);

};

class DebuggerAssert
{
private:

public:

    static FCDECL4(INT32, 
                   ShowDefaultAssertDialog, 
                   StringObject* strConditionUNSAFE, 
                   StringObject* strMessageUNSAFE,
                   StringObject* strStackTraceUNSAFE,
                   StringObject* strWindowTitleUNSAFE
                  );

};


// The following code is taken from object.h and modified to suit 
// LogSwitchBaseObject 
//  
class LogSwitchObject : public Object
{
  protected:
    // README:
    // Modifying the order or fields of this object may require other changes to the
    //  classlib class defintion of the LogSwitch object.

    STRINGREF m_strName;
    STRINGREF strDescription;
    OBJECTREF m_ParentSwitch;   
    INT32 m_iLevel;
    INT32 m_iOldLevel;

  protected:
    LogSwitchObject() {}
   ~LogSwitchObject() {}
   
  public:
    // check for classes that wrap Ole classes 

    void SetLevel(INT32 iLevel)
    {
        m_iLevel = iLevel;
    }

    INT32 GetLevel(void) 
    {
        return m_iLevel;
    }

    OBJECTREF GetParent (void) 
    { 
        return m_ParentSwitch;
    }

    STRINGREF GetName (void) 
    { 
        return m_strName;
    }
};

#ifdef USE_CHECKED_OBJECTREFS
typedef REF <LogSwitchObject> LOGSWITCHREF;
#else
typedef LogSwitchObject* LOGSWITCHREF;
#endif


#define MAX_KEY_LENGTH      64
#define MAX_HASH_BUCKETS    20

class HashElement
{
private:

    OBJECTHANDLE m_pData;
    SString      m_strKey;
    HashElement *m_pNext;

public:
    
    HashElement () 
    {
        LIMITED_METHOD_CONTRACT;
        m_pData = NULL;
        m_pNext = NULL;
    }

    ~HashElement()
    {
        if (m_pNext!= NULL)
        {
            delete m_pNext;
        }

        m_pNext=NULL;

    }// ~HashElement

    void SetData (OBJECTHANDLE pData, const WCHAR *pKey) 
    {
        m_pData = pData;
        m_strKey.Set(pKey);
    }

    OBJECTHANDLE GetData (void) 
    { 
        return m_pData;
    }

    const WCHAR *GetKey (void) 
    { 
        return m_strKey.GetUnicode();
    }
    
    void SetNext (HashElement *pNext) 
    { 
        m_pNext = pNext;
    }

    HashElement *GetNext (void) 
    { 
        return m_pNext;
    }

};

class LogHashTable
{
private:

    HashElement *m_Buckets [MAX_HASH_BUCKETS];

public:
    // static global object, no constructors/destructors, assumes zero initialized memory

    HRESULT AddEntryToHashTable (const WCHAR *pKey, OBJECTHANDLE pData);
    
    OBJECTHANDLE GetEntryFromHashTable (const WCHAR *pKey);
    
};

extern LogHashTable g_sLogHashTable;


class Log
{
private:

public:
    static FCDECL1(void, AddLogSwitch, LogSwitchObject * m_LogSwitch);
    
    static FCDECL3(void, 
                   ModifyLogSwitch, 
                   INT32 Level, 
                   StringObject* strLogSwitchNameUNSAFE, 
                   StringObject* strParentNameUNSAFE
                  );

    // The following method is called when the level of a log switch is modified
    // from the debugger. It is not an ecall.
    static void DebuggerModifyingLogSwitch (int iNewLevel, const WCHAR *pLogSwitchName);

};

//
// Returns a textual representation of the current stack trace. The format of the stack
// trace is the same as returned by StackTrace.ToString.
//
void GetManagedStackTraceString(BOOL fNeedFileInfo, SString &result);

#endif  // __DEBUG_DEBUGGER_h__
