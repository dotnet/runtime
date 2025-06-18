#ifndef _LOOKUPINTRINSIC_H_
#define _LOOKUPINTRINSIC_H_

#include "namedintrinsiclist.h"

typedef NamedIntrinsic (*lookupHWNamedIntrinsicHandler)(void* context,
                                                CORINFO_SIG_INFO* sig,
                                                const char*       className,
                                                const char*       methodName,
                                                const char*       innerEnclosingClassName,
                                                const char*       outerEnclosingClassName);

// HACK: We can't guarantee that COMP_HANDLE has been defined by the headers we include so we need to provide a definition here
typedef class ICorJitInfo* COMP_HANDLE;

class NamedIntrinsicLookup
{
private:
    void* m_context;
    COMP_HANDLE m_compHnd;
    CORINFO_METHOD_HANDLE m_compMethod;
    int m_vectorTByteLength;
    lookupHWNamedIntrinsicHandler m_lookupHWNamedIntrinsic;
    bool m_Zbb;

public:
    NamedIntrinsicLookup (void* context, COMP_HANDLE compHnd, CORINFO_METHOD_HANDLE compMethod, int vectorTByteLength, bool zbb, lookupHWNamedIntrinsicHandler lookupHWNamedIntrinsic)
    {
        m_context = context;
        m_compHnd = compHnd;
        m_compMethod = compMethod;
        m_vectorTByteLength = vectorTByteLength;
        m_Zbb = zbb;
        m_lookupHWNamedIntrinsic = lookupHWNamedIntrinsic;
    }

    NamedIntrinsic lookupPrimitiveFloatNamedIntrinsic(CORINFO_METHOD_HANDLE method, const char* methodName);
    NamedIntrinsic lookupPrimitiveIntNamedIntrinsic(CORINFO_METHOD_HANDLE method, const char* methodName);
    NamedIntrinsic lookupNamedIntrinsic(CORINFO_METHOD_HANDLE method);
};

#endif // _LOOKUPINTRINSIC_H_
