// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _ASSEMBLYBINDER_H
#define _ASSEMBLYBINDER_H

#include <sarray.h>
#include "../binder/inc/applicationcontext.hpp"

class PEImage;
class NativeImage;
class Assembly;
class Module;
class AssemblyLoaderAllocator;
class AssemblySpec;

class AssemblyBinder
{
public:

    HRESULT BindAssemblyByName(AssemblyNameData* pAssemblyNameData, BINDER_SPACE::Assembly** ppAssembly);
    virtual HRESULT BindUsingPEImage(PEImage* pPEImage, bool excludeAppPaths, BINDER_SPACE::Assembly** ppAssembly) = 0;
    virtual HRESULT BindUsingAssemblyName(BINDER_SPACE::AssemblyName* pAssemblyName, BINDER_SPACE::Assembly** ppAssembly) = 0;

    /// <summary>
    /// Get LoaderAllocator for binders that contain it. For other binders, return NULL.
    /// </summary>
    virtual AssemblyLoaderAllocator* GetLoaderAllocator() = 0;

    /// <summary>
    /// Tells if the binder is a default binder (not a custom one)
    /// </summary>
    virtual bool IsDefault() = 0;

    inline BINDER_SPACE::ApplicationContext* GetAppContext()
    {
        return &m_appContext;
    }

    INT_PTR GetManagedAssemblyLoadContext()
    {
        return m_ptrManagedAssemblyLoadContext;
    }

    void SetManagedAssemblyLoadContext(INT_PTR ptrManagedDefaultBinderInstance)
    {
        m_ptrManagedAssemblyLoadContext = ptrManagedDefaultBinderInstance;
    }

    NativeImage* LoadNativeImage(Module* componentModule, LPCUTF8 nativeImageName);
    void AddLoadedAssembly(Assembly* loadedAssembly);

    void GetNameForDiagnostics(/*out*/ SString& alcName);

    static void GetNameForDiagnosticsFromManagedALC(INT_PTR managedALC, /* out */ SString& alcName);
    static void GetNameForDiagnosticsFromSpec(AssemblySpec* spec, /*out*/ SString& alcName);

#ifdef FEATURE_READYTORUN
    // Must be called under the LoadLock
    void DeclareDependencyOnMvid(LPCUTF8 simpleName, GUID mvid, bool compositeComponent, LPCUTF8 imageName);
#endif // FEATURE_READYTORUN

private:

#ifdef FEATURE_READYTORUN
    // Must be called under the LoadLock
    void DeclareLoadedAssembly(Assembly* loadedAssembly);

    struct SimpleNameToExpectedMVIDAndRequiringAssembly
    {
        LPCUTF8 SimpleName;

        // When an assembly is loaded, this Mvid value will be set to the mvid of the assembly. If there are multiple assemblies
        // with different mvid's loaded with the same simple name, then the Mvid value will be set to all zeroes.
        GUID Mvid;

        // If an assembly of this simple name is not yet loaded, but a depedency on an exact mvid is registered, then this field will
        // be filled in with the simple assembly name of the first assembly loaded with an mvid dependency.
        LPCUTF8 AssemblyRequirementName;

        // To disambiguate between component images of a composite image and requirements from a non-composite --inputbubble assembly, use this bool
        bool CompositeComponent;

        SimpleNameToExpectedMVIDAndRequiringAssembly() :
            SimpleName(NULL),
            Mvid({0}),
            AssemblyRequirementName(NULL),
            CompositeComponent(false)
        {
        }

        SimpleNameToExpectedMVIDAndRequiringAssembly(LPCUTF8 simpleName, GUID mvid, bool compositeComponent, LPCUTF8 AssemblyRequirementName) : 
            SimpleName(simpleName),
            Mvid(mvid),
            AssemblyRequirementName(AssemblyRequirementName),
            CompositeComponent(compositeComponent)
        {}

        static SimpleNameToExpectedMVIDAndRequiringAssembly GetNull() { return SimpleNameToExpectedMVIDAndRequiringAssembly(); }
        bool IsNull() const { return SimpleName == NULL; }
    };

    class SimpleNameWithMvidHashTraits : public NoRemoveSHashTraits< DefaultSHashTraits<SimpleNameToExpectedMVIDAndRequiringAssembly> >
    {
    public:
        typedef LPCUTF8 key_t;

        static SimpleNameToExpectedMVIDAndRequiringAssembly Null() { return SimpleNameToExpectedMVIDAndRequiringAssembly::GetNull(); }
        static bool IsNull(const SimpleNameToExpectedMVIDAndRequiringAssembly& e) { return e.IsNull(); }

        static LPCUTF8 GetKey(const SimpleNameToExpectedMVIDAndRequiringAssembly& e) { return e.SimpleName; }

        static BOOL Equals(LPCUTF8 a, LPCUTF8 b) { return strcmp(a, b) == 0; } // Use a case senstive comparison here even though
                                                                            // assembly name matching should be case insensitive. Case insensitive
                                                                            // comparisons are slow and have throwing scenarios, and this hash table
                                                                            // provides a best-effort match to prevent problems, not perfection

        static count_t Hash(LPCUTF8 a) { return HashStringA(a); } // As above, this is a case sensitive hash
    };

    SHash<SimpleNameWithMvidHashTraits> m_assemblySimpleNameMvidCheckHash;
#endif // FEATURE_READYTORUN

    BINDER_SPACE::ApplicationContext m_appContext;

    // A GC handle to the managed AssemblyLoadContext.
    // It is a long weak handle for collectible AssemblyLoadContexts and strong handle for non-collectible ones.
    INT_PTR m_ptrManagedAssemblyLoadContext;

    SArray<Assembly*> m_loadedAssemblies;
};

#endif
