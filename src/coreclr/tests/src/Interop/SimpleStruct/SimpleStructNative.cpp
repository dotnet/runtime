#include <platformdefines.h>

#include <stdio.h>
#include <stdlib.h>
#include <xplatform.h>

typedef void *voidPtr;
 
typedef struct { int a; 
bool b;
char* str;} Sstr;

typedef struct { int a; 
bool b;
double c;} Sstr_simple;

typedef struct { 
	int a;
	int extra; //padding needs to be added here as we have added 8 byte offset.
	union
	{
		int i;
		BOOL b;
		double d;
	}udata;
}ExplStruct;

extern "C"
DLL_EXPORT BOOL __cdecl CdeclSimpleStructByRef(Sstr *p) 
{
  p->a = 100;
  p->b=1;
  strcpy_s(p->str, 7, "after");
  return TRUE;
}

extern "C"
DLL_EXPORT BOOL __cdecl CdeclSimpleExplStructByRef(ExplStruct *p)
{
	if((p->a != 0) || (p->udata.i != 10))
	{
		printf("\np->a=%d, p->udata.i=%d\n",p->a,p->udata.i);
		return FALSE;
	}
	p->a = 1;
	p->udata.b = TRUE;
	return TRUE;
}

extern "C"
DLL_EXPORT Sstr_simple* __cdecl CdeclSimpleStruct(Sstr_simple p,BOOL *result)
{
	Sstr_simple *pSimpleStruct;
	if((p.a !=100) || (p.b != FALSE) || (p.c != 3.142))
	{
		*(result)= FALSE;
		return NULL;
	}
	pSimpleStruct = (Sstr_simple*) CoreClrAlloc(sizeof(Sstr_simple) * 1);
	pSimpleStruct->a = 101;
	pSimpleStruct->b = TRUE;
	pSimpleStruct->c = 10.11;
	*(result)= TRUE;
	return pSimpleStruct;
}

extern "C"
DLL_EXPORT ExplStruct* __cdecl CdeclSimpleExplStruct(ExplStruct p,BOOL *result)
{
	ExplStruct *pExplStruct;
	if((p.a !=1) || (p.udata.b != FALSE))
	{
		*(result)= FALSE;
		return NULL;
	}
	pExplStruct = (ExplStruct*) CoreClrAlloc(sizeof(ExplStruct) * 1);
	pExplStruct->a = 2;
	pExplStruct->udata.d = 3.142;
	*(result)= TRUE;
	return pExplStruct;
}

extern "C"
DLL_EXPORT voidPtr STDMETHODCALLTYPE GetFptr(int i)
{
	switch(i)
	{

	case 14:
		return (voidPtr) &CdeclSimpleStructByRef;
		break;

	case 16:
		return (voidPtr) &CdeclSimpleStruct;
		break;

	case 18:
		return (voidPtr) &CdeclSimpleExplStructByRef;
		break;

	case 20:
		return (voidPtr) &CdeclSimpleExplStruct;
		break;

	}
	return (voidPtr) &CdeclSimpleStruct;
}
