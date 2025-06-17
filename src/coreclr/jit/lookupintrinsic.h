#ifndef _LOOKUPINTRINSIC_H_
#define _LOOKUPINTRINSIC_H_

#include "namedintrinsiclist.h"

typedef NamedIntrinsic (*lookupHWNamedIntrinsicHandler)(void* context,
                                                CORINFO_SIG_INFO* sig,
                                                const char*       className,
                                                const char*       methodName,
                                                const char*       innerEnclosingClassName,
                                                const char*       outerEnclosingClassName);


class NamedIntrinsicLookup
{
private:
    void* m_context;
    COMP_HANDLE m_compHnd;
    CORINFO_METHOD_HANDLE m_compMethod;
    int m_vectorTByteLength;
    lookupHWNamedIntrinsicHandler m_lookupHWNamedIntrinsic;

public:
    NamedIntrinsicLookup (void* context, COMP_HANDLE compHnd, CORINFO_METHOD_HANDLE compMethod, int vectorTByteLength, lookupHWNamedIntrinsicHandler lookupHWNamedIntrinsic)
    {
        m_context = context;
        m_compHnd = compHnd;
        m_compMethod = compMethod;
        m_vectorTByteLength = vectorTByteLength;
        m_lookupHWNamedIntrinsic = lookupHWNamedIntrinsic;
    }

    NamedIntrinsic lookupPrimitiveFloatNamedIntrinsic(CORINFO_METHOD_HANDLE method, const char* methodName);
    NamedIntrinsic lookupPrimitiveIntNamedIntrinsic(CORINFO_METHOD_HANDLE method, const char* methodName);
    NamedIntrinsic lookupNamedIntrinsic(CORINFO_METHOD_HANDLE method);
};

#endif // _LOOKUPINTRINSIC_H_
