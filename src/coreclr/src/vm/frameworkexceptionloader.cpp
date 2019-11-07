// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



// Just the subset of functionality from the MscorlibBinder necessary for exceptions.

#include "common.h"
#include "frameworkexceptionloader.h"
#include "typeparse.h"


struct ExceptionLocationData
{
    LPCUTF8 Namespace;
    LPCUTF8 Name;
    LPCUTF8 AssemblySimpleName;
    LPCUTF8 PublicKeyToken;
};

static const
ExceptionLocationData g_ExceptionLocationData[] = {
#define DEFINE_EXCEPTION(ns, reKind, bHRformessage, ...)
#define DEFINE_EXCEPTION_HR_WINRT_ONLY(ns, reKind, ...)
#define DEFINE_EXCEPTION_IN_OTHER_FX_ASSEMBLY(ns, reKind, assemblySimpleName, publicKeyToken, bHRformessage, ...) { ns, PTR_CSTR((TADDR) # reKind), assemblySimpleName, publicKeyToken },
#include "rexcep.h"
    {NULL, NULL, NULL, NULL}  // On Silverlight, this table may be empty.  This dummy entry allows us to compile.
};


// Note that some assemblies, like System.Runtime.WindowsRuntime, might not be installed on pre-Windows 8 machines.
// This may return null.
MethodTable* FrameworkExceptionLoader::GetException(RuntimeExceptionKind kind)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;

        PRECONDITION(kind > kLastExceptionInMscorlib);
        PRECONDITION(kind - (kLastExceptionInMscorlib + 1) < COUNTOF(g_ExceptionLocationData) - 1);
    }
    CONTRACTL_END;

    // This is for loading rarely-used exception objects in arbitrary appdomains.
    // The loader should do caching - let's not create a multi-appdomain cache of these exception types here.
    // Note that some assemblies, like System.Runtime.WindowsRuntime, might not be installed on pre-Windows 8 machines.
    int index = kind - (kLastExceptionInMscorlib + 1);
    ExceptionLocationData exData = g_ExceptionLocationData[index];
    _ASSERTE(exData.Name != NULL && exData.AssemblySimpleName != NULL && exData.PublicKeyToken != NULL);  // Was the exception defined in mscorlib instead?
    StackSString assemblyQualifiedName;
    _ASSERTE(exData.Namespace != NULL);  // If we need to support stuff in a global namespace, fix this.
    assemblyQualifiedName.SetUTF8(exData.Namespace);
    assemblyQualifiedName.AppendUTF8(".");
    assemblyQualifiedName.AppendUTF8(exData.Name);
    assemblyQualifiedName.AppendUTF8(", ");
    assemblyQualifiedName.AppendUTF8(exData.AssemblySimpleName);
    assemblyQualifiedName.AppendUTF8(", PublicKeyToken=");
    assemblyQualifiedName.AppendUTF8(exData.PublicKeyToken);
    assemblyQualifiedName.AppendUTF8(", Version=");
    assemblyQualifiedName.AppendUTF8(VER_ASSEMBLYVERSION_STR);
    assemblyQualifiedName.AppendUTF8(", Culture=neutral");

    MethodTable* pMT = NULL;
    // Loading will either succeed or throw a FileLoadException.  Catch & swallow that exception.
    EX_TRY
    {
        pMT = TypeName::GetTypeFromAsmQualifiedName(assemblyQualifiedName.GetUnicode()).GetMethodTable();

        // Since this type is from another assembly, we must ensure that assembly has been sufficiently loaded.
        pMT->EnsureActive();
    }
    EX_CATCH
    {
        Exception *ex = GET_EXCEPTION();

        // Let non-file-not-found exceptions propagate
        if (EEFileLoadException::GetFileLoadKind(ex->GetHR()) != kFileNotFoundException)
            EX_RETHROW;

        // Return COMException if we can't load the assembly we expect.
        pMT = MscorlibBinder::GetException(kCOMException);
    }
    EX_END_CATCH(RethrowTerminalExceptions);

    return pMT;
}

void FrameworkExceptionLoader::GetExceptionName(RuntimeExceptionKind kind, SString & exceptionName)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;

        PRECONDITION(kind > kLastExceptionInMscorlib);
        PRECONDITION(kind - (kLastExceptionInMscorlib + 1) < COUNTOF(g_ExceptionLocationData) - 1);
    } CONTRACTL_END;

    exceptionName.SetUTF8(g_ExceptionLocationData[kind].Namespace);
    exceptionName.AppendUTF8(".");
    exceptionName.AppendUTF8(g_ExceptionLocationData[kind].Name);
}
