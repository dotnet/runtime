#ifndef _LOOKUPINTRINSIC_H_
#define _LOOKUPINTRINSIC_H_

class NamedIntrinsicLookup
{
private:
    COMP_HANDLE m_compHnd;

public:
    NamedIntrinsic lookupPrimitiveFloatNamedIntrinsic(CORINFO_METHOD_HANDLE method, const char* methodName);
    NamedIntrinsic lookupPrimitiveIntNamedIntrinsic(CORINFO_METHOD_HANDLE method, const char* methodName);
    NamedIntrinsic lookupNamedIntrinsic(CORINFO_METHOD_HANDLE method);
}

#endif // _LOOKUPINTRINSIC_H_
