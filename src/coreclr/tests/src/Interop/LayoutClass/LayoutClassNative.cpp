#include "platformdefines.h"

#include <stdio.h>
#include <stdlib.h>
#include <xplatform.h>

typedef void *voidPtr;
 
struct SeqClass
{
    int a; 
    bool b;
    char* str;
};

struct ExpClass
{ 
	int a;
	int extra; //padding needs to be added here as we have added 8 byte offset.
	union
	{
		int i;
		BOOL b;
		double d;
	} udata;
};

struct BlittableClass
{
    int a;
};

struct NestedLayoutClass
{
    SeqClass str;
};

extern "C"
DLL_EXPORT BOOL STDMETHODCALLTYPE SimpleSeqLayoutClassByRef(SeqClass *p) 
{
    if((p->a != 0) || (p->b) || strcmp(p->str, "before") != 0)
    {
        printf("\np->a=%d, p->b=%s, p->str=%s", p->a, p->b ? "true" : "false", p->str);
        return FALSE;
    }
    return TRUE;
}

extern "C"
DLL_EXPORT BOOL STDMETHODCALLTYPE SimpleBlittableSeqLayoutClassByRef(BlittableClass *p) 
{
    if(p->a != 10)
    {
        printf("\np->a=%d", p->a);
        return FALSE;
    }
    return TRUE;
}

extern "C"
DLL_EXPORT BOOL STDMETHODCALLTYPE SimpleExpLayoutClassByRef(ExpClass *p)
{
	if((p->a != 0) || (p->udata.i != 10))
	{
		printf("\np->a=%d, p->udata.i=%d\n",p->a,p->udata.i);
		return FALSE;
	}
	return TRUE;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE SimpleNestedLayoutClassByValue(NestedLayoutClass v)
{
    return SimpleSeqLayoutClassByRef(&v.str);
}

extern "C" DLL_EXPORT void __cdecl Invalid(...)
{
}
