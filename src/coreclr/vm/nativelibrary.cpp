// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "nativelibrary.h"

#include "clr/fs/path.h"
using namespace clr::fs;

// Specifies whether hostpolicy is embedded in executable or standalone
extern bool g_hostpolicy_embedded;

// remove when we get an updated SDK
#define LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR 0x00000100

#ifdef TARGET_UNIX
#define PLATFORM_SHARED_LIB_SUFFIX_W PAL_SHLIB_SUFFIX_W
#define PLATFORM_SHARED_LIB_PREFIX_W PAL_SHLIB_PREFIX_W
#else // !TARGET_UNIX
// The default for Windows OS is ".DLL". This causes issues with case-sensitive file systems on Windows.
// We are using the lowercase version due to historical precedence and how common it is now.
#define PLATFORM_SHARED_LIB_SUFFIX_W W(".dll")
#define PLATFORM_SHARED_LIB_PREFIX_W W("")
#endif // !TARGET_UNIX

// The Bit 0x2 has different semantics in DllImportSearchPath and LoadLibraryExA flags.
// In DllImportSearchPath enum, bit 0x2 represents SearchAssemblyDirectory -- which is performed by CLR.
// Unlike other bits in this enum, this bit shouldn't be directly passed on to LoadLibrary()
#define DLLIMPORTSEARCHPATH_ASSEMBLYDIRECTORY 0x2

namespace
{
    // Preserving good error info from DllImport-driven LoadLibrary is tricky because we keep loading from different places
    // if earlier loads fail and those later loads obliterate error codes.
    //
    // This tracker object will keep track of the error code in accordance to priority:
    //
    //   low-priority:      unknown error code (should never happen)
    //   medium-priority:   dll not found
    //   high-priority:     dll found but error during loading
    //
    // We will overwrite the previous load's error code only if the new error code is higher priority.
    //
    class LoadLibErrorTracker
    {
    private:
        static const DWORD const_priorityNotFound     = 10;
        static const DWORD const_priorityAccessDenied = 20;
        static const DWORD const_priorityCouldNotLoad = 99999;
    public:
        LoadLibErrorTracker()
        {
            LIMITED_METHOD_CONTRACT;
            m_hr = E_FAIL;
            m_priorityOfLastError = 0;
            m_message = SString(SString::Utf8, "\n");
        }

        VOID TrackErrorCode()
        {
            LIMITED_METHOD_CONTRACT;

            DWORD priority;

#ifdef TARGET_UNIX
            SetMessage(PAL_GetLoadLibraryError());
#else
            DWORD dwLastError = GetLastError();

            switch (dwLastError)
            {
                case ERROR_FILE_NOT_FOUND:
                case ERROR_PATH_NOT_FOUND:
                case ERROR_MOD_NOT_FOUND:
                case ERROR_DLL_NOT_FOUND:
                    priority = const_priorityNotFound;
                    break;

                // If we can't access a location, we can't know if the dll's there or if it's good.
                // Still, this is probably more unusual (and thus of more interest) than a dll-not-found
                // so give it an intermediate priority.
                case ERROR_ACCESS_DENIED:
                    priority = const_priorityAccessDenied;

                // Assume all others are "dll found but couldn't load."
                default:
                    priority = const_priorityCouldNotLoad;
                    break;
            }
            UpdateHR(priority, HRESULT_FROM_WIN32(dwLastError));
#endif
        }

        HRESULT GetHR()
        {
            return m_hr;
        }

        SString& GetMessage()
        {
            return m_message;
        }

        void DECLSPEC_NORETURN Throw(SString &libraryNameOrPath)
        {
            STANDARD_VM_CONTRACT;

#if defined(__APPLE__)
            COMPlusThrow(kDllNotFoundException, IDS_EE_NDIRECT_LOADLIB_MAC, libraryNameOrPath.GetUnicode(), GetMessage());
#elif defined(TARGET_UNIX)
            COMPlusThrow(kDllNotFoundException, IDS_EE_NDIRECT_LOADLIB_LINUX, libraryNameOrPath.GetUnicode(), GetMessage());
#else // __APPLE__
            HRESULT theHRESULT = GetHR();
            if (theHRESULT == HRESULT_FROM_WIN32(ERROR_BAD_EXE_FORMAT))
            {
                COMPlusThrow(kBadImageFormatException);
            }
            else
            {
                SString hrString;
                GetHRMsg(theHRESULT, hrString);
                COMPlusThrow(kDllNotFoundException, IDS_EE_NDIRECT_LOADLIB_WIN, libraryNameOrPath.GetUnicode(), hrString);
            }
#endif // TARGET_UNIX

            __UNREACHABLE();
        }

    private:
        void UpdateHR(DWORD priority, HRESULT hr)
        {
            if (priority > m_priorityOfLastError)
            {
                m_hr                  = hr;
                m_priorityOfLastError = priority;
            }
        }

        void SetMessage(LPCSTR message)
        {
#ifdef TARGET_UNIX
            //Append dlerror() messages
            SString new_message = SString(SString::Utf8, message);
            SString::Iterator i = m_message.Begin();
            if (!m_message.Find(i, new_message))
            {
                m_message += new_message;
                m_message += SString(SString::Utf8, "\n");
            }
#else
            m_message = SString(SString::Utf8, message);
#endif
        }

        HRESULT m_hr;
        DWORD   m_priorityOfLastError;
        SString  m_message;
    };  // class LoadLibErrorTracker

    // Load the library directly and return the raw system handle
    NATIVE_LIBRARY_HANDLE LocalLoadLibraryHelper( LPCWSTR name, DWORD flags, LoadLibErrorTracker *pErrorTracker )
    {
        STANDARD_VM_CONTRACT;

        NATIVE_LIBRARY_HANDLE hmod = NULL;

#ifndef TARGET_UNIX
        if ((flags & 0xFFFFFF00) != 0)
        {
            hmod = CLRLoadLibraryEx(name, NULL, flags & 0xFFFFFF00);
            if (hmod != NULL)
            {
                return hmod;
            }

            DWORD dwLastError = GetLastError();
            if (dwLastError != ERROR_INVALID_PARAMETER)
            {
                pErrorTracker->TrackErrorCode();
                return hmod;
            }
        }

        hmod = CLRLoadLibraryEx(name, NULL, flags & 0xFF);

#else // !TARGET_UNIX
        hmod = PAL_LoadLibraryDirect(name);
#endif // !TARGET_UNIX

        if (hmod == NULL)
        {
            pErrorTracker->TrackErrorCode();
        }

        return hmod;
    }

    // DllImportSearchPathFlags is a special enumeration, whose values are tied closely with LoadLibrary flags.
    // There is no "default" value DllImportSearchPathFlags. In the absence of DllImportSearchPath attribute,
    // CoreCLR's LoadLibrary implementation uses the following defaults.
    // Other implementations of LoadLibrary callbacks/events are free to use other default conventions.
    void GetDefaultDllImportSearchPathFlags(DWORD *dllImportSearchPathFlags, BOOL *searchAssemblyDirectory)
    {
        STANDARD_VM_CONTRACT;

        *searchAssemblyDirectory = TRUE;
        *dllImportSearchPathFlags = 0;
    }

    // If a module has the DefaultDllImportSearchPathsAttribute, get DllImportSearchPathFlags from it, and return true.
    // Otherwise, get CoreCLR's default value for DllImportSearchPathFlags, and return false.
    BOOL GetDllImportSearchPathFlags(Module *pModule, DWORD *dllImportSearchPathFlags, BOOL *searchAssemblyDirectory)
    {
        STANDARD_VM_CONTRACT;

        if (pModule->HasDefaultDllImportSearchPathsAttribute())
        {
            *dllImportSearchPathFlags = pModule->DefaultDllImportSearchPathsAttributeCachedValue();
            *searchAssemblyDirectory = pModule->DllImportSearchAssemblyDirectory();
            return TRUE;
        }

        GetDefaultDllImportSearchPathFlags(dllImportSearchPathFlags, searchAssemblyDirectory);
        return FALSE;
    }

    // If a pInvoke has the DefaultDllImportSearchPathsAttribute, get DllImportSearchPathFlags from it, and returns true.
    // Otherwise, if the containing assembly has the DefaultDllImportSearchPathsAttribute, get DllImportSearchPathFlags from it, and returns true.
    // Otherwise, get CoreCLR's default value for DllImportSearchPathFlags, and return false.
    BOOL GetDllImportSearchPathFlags(NDirectMethodDesc * pMD, DWORD *dllImportSearchPathFlags, BOOL *searchAssemblyDirectory)
    {
        STANDARD_VM_CONTRACT;

        if (pMD->HasDefaultDllImportSearchPathsAttribute())
        {
            *dllImportSearchPathFlags = pMD->DefaultDllImportSearchPathsAttributeCachedValue();
            *searchAssemblyDirectory = pMD->DllImportSearchAssemblyDirectory();
            return TRUE;
        }

        return GetDllImportSearchPathFlags(pMD->GetModule(), dllImportSearchPathFlags, searchAssemblyDirectory);
    }
}

// static
NATIVE_LIBRARY_HANDLE NativeLibrary::LoadLibraryFromPath(LPCWSTR libraryPath, BOOL throwOnError)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(libraryPath));
    }
    CONTRACTL_END;

    LoadLibErrorTracker errorTracker;
    const NATIVE_LIBRARY_HANDLE hmod =
        LocalLoadLibraryHelper(libraryPath, GetLoadWithAlteredSearchPathFlag(), &errorTracker);

    if (throwOnError && (hmod == nullptr))
    {
        SString libraryPathSString(libraryPath);
        errorTracker.Throw(libraryPathSString);
    }
    return hmod;
}

// static
void NativeLibrary::FreeNativeLibrary(NATIVE_LIBRARY_HANDLE handle)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(handle != NULL);

#ifndef TARGET_UNIX
    BOOL retVal = FreeLibrary(handle);
#else // !TARGET_UNIX
    BOOL retVal = PAL_FreeLibraryDirect(handle);
#endif // !TARGET_UNIX

    if (retVal == 0)
        COMPlusThrow(kInvalidOperationException, W("Arg_InvalidOperationException"));
}

//static
INT_PTR NativeLibrary::GetNativeLibraryExport(NATIVE_LIBRARY_HANDLE handle, LPCWSTR symbolName, BOOL throwOnError)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(handle));
        PRECONDITION(CheckPointer(symbolName));
    }
    CONTRACTL_END;

    MAKE_UTF8PTR_FROMWIDE(lpstr, symbolName);

#ifndef TARGET_UNIX
    INT_PTR address = reinterpret_cast<INT_PTR>(GetProcAddress((HMODULE)handle, lpstr));
    if ((address == NULL) && throwOnError)
        COMPlusThrow(kEntryPointNotFoundException, IDS_EE_NDIRECT_GETPROCADDR_WIN_DLL, symbolName);
#else // !TARGET_UNIX
    INT_PTR address = reinterpret_cast<INT_PTR>(PAL_GetProcAddressDirect(handle, lpstr));
    if ((address == NULL) && throwOnError)
        COMPlusThrow(kEntryPointNotFoundException, IDS_EE_NDIRECT_GETPROCADDR_UNIX_SO, symbolName);
#endif // !TARGET_UNIX

    return address;
}

namespace
{
#ifndef TARGET_UNIX
    BOOL IsWindowsAPISet(PCWSTR wszLibName)
    {
        STANDARD_VM_CONTRACT;

        // This is replicating quick check from the OS implementation of api sets.
        return SString::_wcsnicmp(wszLibName, W("api-"), 4) == 0 ||
               SString::_wcsnicmp(wszLibName, W("ext-"), 4) == 0;
    }
#endif // !TARGET_UNIX

    NATIVE_LIBRARY_HANDLE LoadNativeLibraryViaAssemblyLoadContext(Assembly * pAssembly, PCWSTR wszLibName)
    {
        STANDARD_VM_CONTRACT;

#ifndef TARGET_UNIX
        if (IsWindowsAPISet(wszLibName))
        {
            // Prevent Overriding of Windows API sets.
            return NULL;
        }
#endif // !TARGET_UNIX

        NATIVE_LIBRARY_HANDLE hmod = NULL;
        PEAssembly *pManifestFile = pAssembly->GetPEAssembly();
        PTR_AssemblyBinder pBinder = pManifestFile->GetAssemblyBinder();

        //Step 0: Check if  the assembly was bound using TPA.
        AssemblyBinder *pCurrentBinder = pBinder;

        // For assemblies bound via default binder, we should use the standard mechanism to make the pinvoke call.
        if (pCurrentBinder->IsDefault())
        {
            return NULL;
        }

        //Step 1: If the assembly was not bound using TPA,
        //        Call System.Runtime.Loader.AssemblyLoadContext.ResolveUnmanagedDll to give
        //        The custom assembly context a chance to load the unmanaged dll.

        GCX_COOP();

        STRINGREF pUnmanagedDllName;
        pUnmanagedDllName = StringObject::NewString(wszLibName);

        GCPROTECT_BEGIN(pUnmanagedDllName);

        // Get the pointer to the managed assembly load context
        INT_PTR ptrManagedAssemblyLoadContext = pCurrentBinder->GetManagedAssemblyLoadContext();

        // Prepare to invoke  System.Runtime.Loader.AssemblyLoadContext.ResolveUnmanagedDll method.
        PREPARE_NONVIRTUAL_CALLSITE(METHOD__ASSEMBLYLOADCONTEXT__RESOLVEUNMANAGEDDLL);
        DECLARE_ARGHOLDER_ARRAY(args, 2);
        args[ARGNUM_0]  = STRINGREF_TO_ARGHOLDER(pUnmanagedDllName);
        args[ARGNUM_1]  = PTR_TO_ARGHOLDER(ptrManagedAssemblyLoadContext);

        // Make the call
        CALL_MANAGED_METHOD(hmod, NATIVE_LIBRARY_HANDLE, args);

        GCPROTECT_END();

        return hmod;
    }

    // Return the AssemblyLoadContext for an assembly
    INT_PTR GetManagedAssemblyLoadContext(Assembly* pAssembly)
    {
        STANDARD_VM_CONTRACT;

        PTR_AssemblyBinder pBinder = pAssembly->GetPEAssembly()->GetAssemblyBinder();
        return pBinder->GetManagedAssemblyLoadContext();
    }

    NATIVE_LIBRARY_HANDLE LoadNativeLibraryViaAssemblyLoadContextEvent(Assembly * pAssembly, PCWSTR wszLibName)
    {
        STANDARD_VM_CONTRACT;

        INT_PTR ptrManagedAssemblyLoadContext = GetManagedAssemblyLoadContext(pAssembly);
        if (ptrManagedAssemblyLoadContext == NULL)
        {
            return NULL;
        }

        NATIVE_LIBRARY_HANDLE hmod = NULL;

        GCX_COOP();

        struct {
            STRINGREF DllName;
            OBJECTREF AssemblyRef;
        } gc = { NULL, NULL };

        GCPROTECT_BEGIN(gc);

        gc.DllName = StringObject::NewString(wszLibName);
        gc.AssemblyRef = pAssembly->GetExposedObject();

        // Prepare to invoke  System.Runtime.Loader.AssemblyLoadContext.ResolveUnmanagedDllUsingEvent method
        // While ResolveUnmanagedDllUsingEvent() could compute the AssemblyLoadContext using the AssemblyRef
        // argument, it will involve another pInvoke to the runtime. So AssemblyLoadContext is passed in
        // as an additional argument.
        PREPARE_NONVIRTUAL_CALLSITE(METHOD__ASSEMBLYLOADCONTEXT__RESOLVEUNMANAGEDDLLUSINGEVENT);
        DECLARE_ARGHOLDER_ARRAY(args, 3);
        args[ARGNUM_0] = STRINGREF_TO_ARGHOLDER(gc.DllName);
        args[ARGNUM_1] = OBJECTREF_TO_ARGHOLDER(gc.AssemblyRef);
        args[ARGNUM_2] = PTR_TO_ARGHOLDER(ptrManagedAssemblyLoadContext);

        // Make the call
        CALL_MANAGED_METHOD(hmod, NATIVE_LIBRARY_HANDLE, args);

        GCPROTECT_END();

        return hmod;
    }

    NATIVE_LIBRARY_HANDLE LoadNativeLibraryViaDllImportResolver(NDirectMethodDesc * pMD, LPCWSTR wszLibName)
    {
        STANDARD_VM_CONTRACT;

        if (pMD->GetModule()->IsSystem())
        {
            // Don't attempt to callback on Corelib itself.
            // The LoadLibrary callback stub is managed code that requires CoreLib
            return NULL;
        }

        DWORD dllImportSearchPathFlags;
        BOOL searchAssemblyDirectory;
        BOOL hasDllImportSearchPathFlags = GetDllImportSearchPathFlags(pMD, &dllImportSearchPathFlags, &searchAssemblyDirectory);
        dllImportSearchPathFlags |= searchAssemblyDirectory ? DLLIMPORTSEARCHPATH_ASSEMBLYDIRECTORY : 0;

        Assembly* pAssembly = pMD->GetMethodTable()->GetAssembly();
        NATIVE_LIBRARY_HANDLE handle = NULL;

        GCX_COOP();

        struct {
            STRINGREF libNameRef;
            OBJECTREF assemblyRef;
        } gc = { NULL, NULL };

        GCPROTECT_BEGIN(gc);

        gc.libNameRef = StringObject::NewString(wszLibName);
        gc.assemblyRef = pAssembly->GetExposedObject();

        PREPARE_NONVIRTUAL_CALLSITE(METHOD__NATIVELIBRARY__LOADLIBRARYCALLBACKSTUB);
        DECLARE_ARGHOLDER_ARRAY(args, 4);
        args[ARGNUM_0] = STRINGREF_TO_ARGHOLDER(gc.libNameRef);
        args[ARGNUM_1] = OBJECTREF_TO_ARGHOLDER(gc.assemblyRef);
        args[ARGNUM_2] = BOOL_TO_ARGHOLDER(hasDllImportSearchPathFlags);
        args[ARGNUM_3] = DWORD_TO_ARGHOLDER(dllImportSearchPathFlags);

         // Make the call
        CALL_MANAGED_METHOD(handle, NATIVE_LIBRARY_HANDLE, args);
        GCPROTECT_END();

        return handle;
    }

    // Try to load the module alongside the assembly where the PInvoke was declared.
    NATIVE_LIBRARY_HANDLE LoadFromPInvokeAssemblyDirectory(Assembly *pAssembly, LPCWSTR libName, DWORD flags, LoadLibErrorTracker *pErrorTracker)
    {
        STANDARD_VM_CONTRACT;

        NATIVE_LIBRARY_HANDLE hmod = NULL;

        SString path = pAssembly->GetPEAssembly()->GetPath();

        SString::Iterator lastPathSeparatorIter = path.End();
        if (PEAssembly::FindLastPathSeparator(path, lastPathSeparatorIter))
        {
            lastPathSeparatorIter++;
            path.Truncate(lastPathSeparatorIter);

            path.Append(libName);
            hmod = LocalLoadLibraryHelper(path, flags, pErrorTracker);
        }

        return hmod;
    }

    // Try to load the module from the native DLL search directories
    NATIVE_LIBRARY_HANDLE LoadFromNativeDllSearchDirectories(LPCWSTR libName, DWORD flags, LoadLibErrorTracker *pErrorTracker)
    {
        STANDARD_VM_CONTRACT;

        NATIVE_LIBRARY_HANDLE hmod = NULL;
        AppDomain* pDomain = GetAppDomain();

        if (pDomain->HasNativeDllSearchDirectories())
        {
            AppDomain::PathIterator pathIter = pDomain->IterateNativeDllSearchDirectories();
            while (hmod == NULL && pathIter.Next())
            {
                SString qualifiedPath(*(pathIter.GetPath()));
                qualifiedPath.Append(libName);
                if (!Path::IsRelative(qualifiedPath))
                {
                    hmod = LocalLoadLibraryHelper(qualifiedPath, flags, pErrorTracker);
                }
            }
        }

        return hmod;
    }

#ifdef TARGET_UNIX
    const int MaxVariationCount = 4;
    void DetermineLibNameVariations(const WCHAR** libNameVariations, int* numberOfVariations, const SString& libName, bool libNameIsRelativePath)
    {
        // Supported lib name variations
        static auto NameFmt = W("%.0s%s%.0s");
        static auto PrefixNameFmt = W("%s%s%.0s");
        static auto NameSuffixFmt = W("%.0s%s%s");
        static auto PrefixNameSuffixFmt = W("%s%s%s");

        _ASSERTE(*numberOfVariations >= MaxVariationCount);

        int varCount = 0;
        if (!libNameIsRelativePath)
        {
            libNameVariations[varCount++] = NameFmt;
        }
        else
        {
            // We check if the suffix is contained in the name, because on Linux it is common to append
            // a version number to the library name (e.g. 'libicuuc.so.57').
            bool containsSuffix = false;
            SString::CIterator it = libName.Begin();
            if (libName.Find(it, PLATFORM_SHARED_LIB_SUFFIX_W))
            {
                it += ARRAY_SIZE(PLATFORM_SHARED_LIB_SUFFIX_W);
                containsSuffix = it == libName.End() || *it == (WCHAR)'.';
            }

            // If the path contains a path delimiter, we don't add a prefix
            it = libName.Begin();
            bool containsDelim = libName.Find(it, DIRECTORY_SEPARATOR_STR_W);

            if (containsSuffix)
            {
                libNameVariations[varCount++] = NameFmt;

                if (!containsDelim)
                    libNameVariations[varCount++] = PrefixNameFmt;

                libNameVariations[varCount++] = NameSuffixFmt;

                if (!containsDelim)
                    libNameVariations[varCount++] = PrefixNameSuffixFmt;
            }
            else
            {
                libNameVariations[varCount++] = NameSuffixFmt;

                if (!containsDelim)
                    libNameVariations[varCount++] = PrefixNameSuffixFmt;

                libNameVariations[varCount++] = NameFmt;

                if (!containsDelim)
                    libNameVariations[varCount++] = PrefixNameFmt;
            }
        }

        *numberOfVariations = varCount;
    }
#else // TARGET_UNIX
    const int MaxVariationCount = 2;
    void DetermineLibNameVariations(const WCHAR** libNameVariations, int* numberOfVariations, const SString& libName, bool libNameIsRelativePath)
    {
        // Supported lib name variations
        static auto NameFmt = W("%.0s%s%.0s");
        static auto NameSuffixFmt = W("%.0s%s%s");

        _ASSERTE(*numberOfVariations >= MaxVariationCount);

        int varCount = 0;

        // Follow LoadLibrary rules in MSDN doc: https://docs.microsoft.com/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibrarya
        // To prevent the function from appending ".DLL" to the module name, include a trailing point character (.) in the module name string
        // or provide an absolute path.
        libNameVariations[varCount++] = NameFmt;

        // The runtime will append the '.dll' extension if the path is relative and the name doesn't end with a "."
        // or an existing known extension. This is done due to issues with case-sensitive file systems
        // on Windows. The Windows loader always appends ".DLL" as opposed to the more common ".dll".
        if (libNameIsRelativePath
            && !libName.EndsWith(W("."))
            && !libName.EndsWithCaseInsensitive(W(".dll"))
            && !libName.EndsWithCaseInsensitive(W(".exe")))
        {
            libNameVariations[varCount++] = NameSuffixFmt;
        }

        *numberOfVariations = varCount;
    }
#endif // TARGET_UNIX

    // Search for the library and variants of its name in probing directories.
    NATIVE_LIBRARY_HANDLE LoadNativeLibraryBySearch(Assembly *callingAssembly,
                                                    BOOL searchAssemblyDirectory, DWORD dllImportSearchPathFlags,
                                                    LoadLibErrorTracker * pErrorTracker, LPCWSTR wszLibName)
    {
        STANDARD_VM_CONTRACT;

        NATIVE_LIBRARY_HANDLE hmod = NULL;

#if !defined(TARGET_UNIX)
        // Try to go straight to System32 for Windows API sets. This is replicating quick check from
        // the OS implementation of api sets.
        if (IsWindowsAPISet(wszLibName))
        {
            hmod = LocalLoadLibraryHelper(wszLibName, LOAD_LIBRARY_SEARCH_SYSTEM32, pErrorTracker);
            if (hmod != NULL)
            {
                return hmod;
            }
        }
#endif // !TARGET_UNIX

        if (g_hostpolicy_embedded)
        {
#ifdef TARGET_WINDOWS
            if (wcscmp(wszLibName, W("hostpolicy.dll")) == 0)
            {
                return WszGetModuleHandle(NULL);
            }
#else
            if (wcscmp(wszLibName, W("libhostpolicy")) == 0)
            {
                return PAL_LoadLibraryDirect(NULL);
            }
#endif
        }

        AppDomain* pDomain = GetAppDomain();
        DWORD loadWithAlteredPathFlags = GetLoadWithAlteredSearchPathFlag();
        bool libNameIsRelativePath = Path::IsRelative(wszLibName);

        // P/Invokes are often declared with variations on the actual library name.
        // For example, it's common to leave off the extension/suffix of the library
        // even if it has one, or to leave off a prefix like "lib" even if it has one
        // (both of these are typically done to smooth over cross-platform differences).
        // We try to dlopen with such variations on the original.
        const WCHAR* prefixSuffixCombinations[MaxVariationCount] = {};
        int numberOfVariations = ARRAY_SIZE(prefixSuffixCombinations);
        DetermineLibNameVariations(prefixSuffixCombinations, &numberOfVariations, wszLibName, libNameIsRelativePath);
        for (int i = 0; i < numberOfVariations; i++)
        {
            SString currLibNameVariation;
            currLibNameVariation.Printf(prefixSuffixCombinations[i], PLATFORM_SHARED_LIB_PREFIX_W, wszLibName, PLATFORM_SHARED_LIB_SUFFIX_W);

            // NATIVE_DLL_SEARCH_DIRECTORIES set by host is considered well known path
            hmod = LoadFromNativeDllSearchDirectories(currLibNameVariation, loadWithAlteredPathFlags, pErrorTracker);
            if (hmod != NULL)
            {
                return hmod;
            }

            if (!libNameIsRelativePath)
            {
                DWORD flags = loadWithAlteredPathFlags;
                if ((dllImportSearchPathFlags & LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR) != 0)
                {
                    // LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR is the only flag affecting absolute path. Don't OR the flags
                    // unconditionally as all absolute path P/Invokes could then lose LOAD_WITH_ALTERED_SEARCH_PATH.
                    flags |= dllImportSearchPathFlags;
                }

                hmod = LocalLoadLibraryHelper(currLibNameVariation, flags, pErrorTracker);
                if (hmod != NULL)
                {
                    return hmod;
                }
            }
            else if ((callingAssembly != nullptr) && searchAssemblyDirectory)
            {
                hmod = LoadFromPInvokeAssemblyDirectory(callingAssembly, currLibNameVariation, loadWithAlteredPathFlags | dllImportSearchPathFlags, pErrorTracker);
                if (hmod != NULL)
                {
                    return hmod;
                }
            }

            hmod = LocalLoadLibraryHelper(currLibNameVariation, dllImportSearchPathFlags, pErrorTracker);
            if (hmod != NULL)
            {
                return hmod;
            }
        }

        // This may be an assembly name
        // Format is "fileName, assemblyDisplayName"
        MAKE_UTF8PTR_FROMWIDE(szLibName, wszLibName);
        char *szComma = strchr(szLibName, ',');
        if (szComma)
        {
            *szComma = '\0';
            // Trim white spaces
            while (COMCharacter::nativeIsWhiteSpace(*(++szComma)));

            AssemblySpec spec;
            SString ssAssemblyDisplayName(SString::Utf8, szComma);
            if (SUCCEEDED(spec.InitNoThrow(ssAssemblyDisplayName)))
            {
                // Need to perform case insensitive hashing.
                SString moduleName(SString::Utf8, szLibName);
                moduleName.LowerCase();

                szLibName = (LPSTR)moduleName.GetUTF8();

                Assembly *pAssembly = spec.LoadAssembly(FILE_LOADED);
                Module *pModule = pAssembly->FindModuleByName(szLibName);

                hmod = LocalLoadLibraryHelper(pModule->GetPath(), loadWithAlteredPathFlags | dllImportSearchPathFlags, pErrorTracker);
            }
        }

        return hmod;
    }

    NATIVE_LIBRARY_HANDLE LoadNativeLibraryBySearch(NDirectMethodDesc *pMD, LoadLibErrorTracker *pErrorTracker, PCWSTR wszLibName)
    {
        STANDARD_VM_CONTRACT;

        BOOL searchAssemblyDirectory;
        DWORD dllImportSearchPathFlags;

        GetDllImportSearchPathFlags(pMD, &dllImportSearchPathFlags, &searchAssemblyDirectory);

        Assembly *pAssembly = pMD->GetMethodTable()->GetAssembly();
        return LoadNativeLibraryBySearch(pAssembly, searchAssemblyDirectory, dllImportSearchPathFlags, pErrorTracker, wszLibName);
    }
}

// static
NATIVE_LIBRARY_HANDLE NativeLibrary::LoadLibraryByName(LPCWSTR libraryName, Assembly *callingAssembly,
    BOOL hasDllImportSearchFlags, DWORD dllImportSearchFlags,
    BOOL throwOnError)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(libraryName));
        PRECONDITION(CheckPointer(callingAssembly));
    }
    CONTRACTL_END;

    NATIVE_LIBRARY_HANDLE hmod = nullptr;

    // Resolve using the AssemblyLoadContext.LoadUnmanagedDll implementation
    hmod = LoadNativeLibraryViaAssemblyLoadContext(callingAssembly, libraryName);
    if (hmod != nullptr)
        return hmod;

    // Check if a default dllImportSearchPathFlags was passed in. If so, use that value.
    // Otherwise, check if the assembly has the DefaultDllImportSearchPathsAttribute attribute.
    // If so, use that value.
    BOOL searchAssemblyDirectory;
    DWORD dllImportSearchPathFlags;
    if (hasDllImportSearchFlags)
    {
        dllImportSearchPathFlags = dllImportSearchFlags & ~DLLIMPORTSEARCHPATH_ASSEMBLYDIRECTORY;
        searchAssemblyDirectory = dllImportSearchFlags & DLLIMPORTSEARCHPATH_ASSEMBLYDIRECTORY;

    }
    else
    {
        GetDllImportSearchPathFlags(callingAssembly->GetModule(),
                                    &dllImportSearchPathFlags, &searchAssemblyDirectory);
    }

    LoadLibErrorTracker errorTracker;
    hmod = LoadNativeLibraryBySearch(callingAssembly, searchAssemblyDirectory, dllImportSearchPathFlags, &errorTracker, libraryName);
    if (hmod != nullptr)
        return hmod;

    // Resolve using the AssemblyLoadContext.ResolvingUnmanagedDll event
    hmod = LoadNativeLibraryViaAssemblyLoadContextEvent(callingAssembly, libraryName);
    if (hmod != nullptr)
        return hmod;

    if (throwOnError)
    {
        SString libraryPathSString(libraryName);
        errorTracker.Throw(libraryPathSString);
    }

    return hmod;
}

namespace
{
    NATIVE_LIBRARY_HANDLE LoadNativeLibrary(NDirectMethodDesc * pMD, LoadLibErrorTracker * pErrorTracker)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION( CheckPointer( pMD ) );
        }
        CONTRACTL_END;

        LPCUTF8 name = pMD->GetLibName();
        if ( !name || !*name )
            return NULL;

        PREFIX_ASSUME( name != NULL );
        MAKE_WIDEPTR_FROMUTF8( wszLibName, name );

        NativeLibraryHandleHolder hmod = LoadNativeLibraryViaDllImportResolver(pMD, wszLibName);
        if (hmod != NULL)
        {
            return hmod.Extract();
        }

        AppDomain* pDomain = GetAppDomain();
        Assembly* pAssembly = pMD->GetMethodTable()->GetAssembly();

        hmod = LoadNativeLibraryViaAssemblyLoadContext(pAssembly, wszLibName);
        if (hmod != NULL)
        {
            return hmod.Extract();
        }

        hmod = pDomain->FindUnmanagedImageInCache(wszLibName);
        if (hmod != NULL)
        {
            return hmod.Extract();
        }

        hmod = LoadNativeLibraryBySearch(pMD, pErrorTracker, wszLibName);
        if (hmod != NULL)
        {
            // If we have a handle add it to the cache.
            pDomain->AddUnmanagedImageToCache(wszLibName, hmod);
            return hmod.Extract();
        }

        hmod = LoadNativeLibraryViaAssemblyLoadContextEvent(pAssembly, wszLibName);
        if (hmod != NULL)
        {
            return hmod.Extract();
        }

        return hmod.Extract();
    }
}

NATIVE_LIBRARY_HANDLE NativeLibrary::LoadLibraryFromMethodDesc(NDirectMethodDesc * pMD)
{
    CONTRACT(NATIVE_LIBRARY_HANDLE)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMD));
        POSTCONDITION(RETVAL != NULL);
    }
    CONTRACT_END;

    LoadLibErrorTracker errorTracker;
    NATIVE_LIBRARY_HANDLE hmod = LoadNativeLibrary(pMD, &errorTracker);
    if (hmod == NULL)
    {
        if (pMD->GetLibName() == NULL)
            COMPlusThrow(kEntryPointNotFoundException, IDS_EE_NDIRECT_GETPROCADDRESS_NONAME);

        StackSString ssLibName(SString::Utf8, pMD->GetLibName());
        errorTracker.Throw(ssLibName);
    }

    RETURN hmod;
}
