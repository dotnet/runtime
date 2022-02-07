// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//On unix make sure to compile using -ldl and -pthread flags.

//Set this value accordingly to your workspace settings
#if defined(_WIN32)
#define PathToLibrary "bin\\Debug\\net6.0\\win-x64\\native\\NativeLibrary.dll"
#elif defined(__APPLE__)
#define PathToLibrary "./bin/Debug/net6.0/osx-x64/native/NativeLibrary.dylib"
#else
#define PathToLibrary "./bin/Debug/net6.0/linux-x64/native/NativeLibrary.so"
#endif

#ifdef _WIN32
#include "windows.h"
#define symLoad GetProcAddress
#else
#include "dlfcn.h"
#include <unistd.h>
#define symLoad dlsym
#endif

#include <stdlib.h>
#include <stdio.h>

#ifndef F_OK
#define F_OK    0
#endif

int callSumFunc(char *path, char *funcName, int a, int b);
char *callSumStringFunc(char *path, char *funcName, char *a, char *b);

int main()
{
    // Check if the library file exists
    if (access(PathToLibrary, F_OK) == -1)
    {
        puts("Couldn't find library at the specified path");
        return 0;
    }

    // Sum two integers
    int sum = callSumFunc(PathToLibrary, "add", 2, 8);
    printf("The sum is %d \n", sum);

    // Concatenate two strings
    char *sumstring = callSumStringFunc(PathToLibrary, "sumstring", "ok", "ko");
    printf("The concatenated string is %s \n", sumstring);

    // Free string
    free(sumstring);
}

int callSumFunc(char *path, char *funcName, int firstInt, int secondInt)
{
    // Call sum function defined in C# shared library
    #ifdef _WIN32
        HINSTANCE handle = LoadLibraryA(path);
    #else
        void *handle = dlopen(path, RTLD_LAZY);
    #endif

    typedef int(*myFunc)(int,int);
    myFunc MyImport = (myFunc)symLoad(handle, funcName);

    int result = MyImport(firstInt, secondInt);

    // CoreRT libraries do not support unloading
    // See https://github.com/dotnet/corert/issues/7887
    return result;
}

char *callSumStringFunc(char *path, char *funcName, char *firstString, char *secondString)
{
    // Library loading
    #ifdef _WIN32
        HINSTANCE handle = LoadLibraryA(path);
    #else
        void *handle = dlopen(path, RTLD_LAZY);
    #endif

    // Declare a typedef
    typedef char *(*myFunc)(char*,char*);

    // Import Symbol named funcName
    myFunc MyImport = (myFunc)symLoad(handle, funcName);

    // The C# function will return a pointer
    char *result = MyImport(firstString, secondString);

    // CoreRT libraries do not support unloading
    // See https://github.com/dotnet/corert/issues/7887
    return result;
}
