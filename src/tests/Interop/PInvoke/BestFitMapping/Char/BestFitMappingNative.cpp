// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <platformdefines.h>


extern "C" bool DLL_EXPORT STDMETHODCALLTYPE Char_In(char c)
{
    printf ("Char_In ");
    printf ("%c",c);
    printf ("\n");

    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE Char_InByRef(char* c)
{
    printf ("Char_InByRef ");
    printf ("%c",*c);
    printf ("\n");

    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE Char_InOutByRef(char* c)
{
    printf ("Char_InOutByRef ");
    printf ("%c",*c);
    printf ("\n");

    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE CharBuffer_In_String(char* c)
{
    printf ("native %s \n", c);

    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE CharBuffer_InByRef_String(char** c)
{
    printf ("native %s \n", *c);

    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE CharBuffer_InOutByRef_String(char** c)
{
    printf ("native %s \n", *c);

    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE CharBuffer_In_StringBuilder(char* c)
{
    printf ("native %s \n", c);

    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE CharBuffer_InByRef_StringBuilder(char** c)
{
    printf ("native %s \n", *c);

    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE CharBuffer_InOutByRef_StringBuilder(char** c)
{
    printf ("native %s \n", *c);

    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE Char_In_ArrayWithOffset (char* pArrayWithOffset)
{
    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE Char_InOut_ArrayWithOffset (char* pArrayWithOffset)
{
    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE Char_InByRef_ArrayWithOffset (char** ppArrayWithOffset)
{
    return TRUE;
}

extern "C" bool DLL_EXPORT STDMETHODCALLTYPE Char_InOutByRef_ArrayWithOffset (char** ppArrayWithOffset)
{
    return TRUE;
}
