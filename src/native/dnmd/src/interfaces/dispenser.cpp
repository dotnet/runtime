#ifdef DNMD_BUILD_SHARED
#ifdef _MSC_VER
#define DNMD_EXPORT __declspec(dllexport)
#else
#define DNMD_EXPORT __attribute__((__visibility__("default")))
#endif // !_MSC_VER
#endif // DNMD_BUILD_SHARED

#include <internal/dnmd_platform.hpp>
#include "dnmd_interfaces.hpp"
#include "metadatainitialsize.h"
#include "controllingiunknown.hpp"
#include "metadataimportro.hpp"
#include "metadataemit.hpp"
#include "threadsafe.hpp"
#include "internal/metadataimport.hpp"
#include <minipal/guid.h>
#include <minipal/rwlock.h>

#include <cerrno>
#include <cstddef>
#include <cstring>
#include <fstream>
#include <limits>
#include <memory>
#include <mutex>
#include <vector>

#if !defined(_MSC_VER) && !defined(DNMD_USE_CORECLR_GUIDS)
extern "C" const GUID MetaDataCheckDuplicatesFor =
    { 0x30fe7be8, 0xd7d9, 0x11d2, { 0x9f, 0x80, 0x00, 0xc0, 0x4f, 0x79, 0xa0, 0xa3 } };
#endif

namespace
{
    struct RegisteredMetadataScopes
    {
        std::mutex mutex;
        std::vector<ControllingIUnknown*> scopes;
    };

    RegisteredMetadataScopes& GetRegisteredMetadataScopes()
    {
        static RegisteredMetadataScopes registry;
        return registry;
    }
}

void MetadataScopeRegistry::RegisterScope(ControllingIUnknown* scope)
{
    RegisteredMetadataScopes& registry = GetRegisteredMetadataScopes();
    std::lock_guard<std::mutex> lock{ registry.mutex };
    registry.scopes.push_back(scope);
    scope->_registeredScope.store(true, std::memory_order_release);
}

void MetadataScopeRegistry::UnregisterScope(ControllingIUnknown* scope) noexcept
{
    RegisteredMetadataScopes& registry = GetRegisteredMetadataScopes();
    std::lock_guard<std::mutex> lock{ registry.mutex };
    for (std::vector<ControllingIUnknown*>::iterator it = registry.scopes.begin(); it != registry.scopes.end(); ++it)
    {
        if (*it == scope)
        {
            registry.scopes.erase(it);
            return;
        }
    }
    assert(false);
}

std::vector<minipal::com_ptr<IUnknown>> MetadataScopeRegistry::AcquireScopes()
{
    // The references must outlive the lock if allocation fails and the snapshot unwinds.
    std::vector<minipal::com_ptr<IUnknown>> snapshot;
    RegisteredMetadataScopes& registry = GetRegisteredMetadataScopes();
    std::lock_guard<std::mutex> lock{ registry.mutex };
    snapshot.reserve(registry.scopes.size());
    for (ControllingIUnknown* scope : registry.scopes)
    {
        snapshot.emplace_back();
        if (scope->TryAddRef())
            snapshot.back().Attach(scope);
        else
            snapshot.pop_back();
    }
    return snapshot;
}

namespace
{
    constexpr uint16_t PE32Magic = 0x10b;
    constexpr uint16_t PE32PlusMagic = 0x20b;
    constexpr uint32_t PESignature = 0x00004550;
    constexpr uint32_t ReadyToRunSignature = 0x00525452;

    struct ReadyToRunHeaderPrefix
    {
        uint32_t signature;
        uint16_t majorVersion;
        uint16_t minorVersion;
        uint32_t flags;
        uint32_t sectionCount;
    };
    static_assert(sizeof(ReadyToRunHeaderPrefix) == 16, "ReadyToRun header prefix must be 16 bytes");

    template<typename T>
    bool ReadPEValue(uint8_t const* image, size_t size, size_t offset, T& value)
    {
        if (offset > size || sizeof(value) > size - offset)
            return false;
        std::memcpy(&value, image + offset, sizeof(value));
        return true;
    }

    bool MapRva(uint8_t const* image, size_t size, size_t sectionsOffset, uint16_t sectionCount,
                uint32_t sizeOfHeaders, uint32_t rva, size_t length, size_t& offset)
    {
        if (rva < sizeOfHeaders && length <= sizeOfHeaders - rva &&
            rva <= size && length <= size - rva)
        {
            offset = rva;
            return true;
        }

        for (uint16_t i = 0; i < sectionCount; ++i)
        {
            IMAGE_SECTION_HEADER section;
            if (!ReadPEValue(image, size, sectionsOffset + size_t(i) * sizeof(section), section))
                return false;
            if (rva < section.VirtualAddress)
                continue;
            uint32_t sectionOffset = rva - section.VirtualAddress;
            if (sectionOffset > section.SizeOfRawData || length > section.SizeOfRawData - sectionOffset ||
                section.PointerToRawData > size || sectionOffset > size - section.PointerToRawData ||
                length > size - section.PointerToRawData - sectionOffset)
                continue;
            offset = size_t(section.PointerToRawData) + sectionOffset;
            return true;
        }
        return false;
    }

    bool FindPEMetadata(uint8_t const* image, size_t size, size_t& metadataOffset, uint32_t& metadataSize,
                        DWORD& peKind, DWORD& machine)
    {
        IMAGE_DOS_HEADER dos;
        if (!ReadPEValue(image, size, 0, dos) || dos.e_magic != IMAGE_DOS_SIGNATURE || dos.e_lfanew < 0)
            return false;

        size_t ntOffset = static_cast<size_t>(dos.e_lfanew);
        uint32_t signature;
        IMAGE_FILE_HEADER fileHeader;
        if (!ReadPEValue(image, size, ntOffset, signature) || signature != PESignature ||
            !ReadPEValue(image, size, ntOffset + sizeof(signature), fileHeader))
            return false;

        size_t optionalOffset = ntOffset + sizeof(signature) + sizeof(fileHeader);
        if (optionalOffset > size || fileHeader.SizeOfOptionalHeader > size - optionalOffset)
            return false;
        uint16_t magic;
        if (!ReadPEValue(image, size, optionalOffset, magic))
            return false;

        IMAGE_DATA_DIRECTORY comDirectory;
        uint32_t sizeOfHeaders, directoryCount;
        size_t directoryOffset;
        if (magic == PE32Magic)
        {
            directoryOffset = offsetof(IMAGE_OPTIONAL_HEADER32, DataDirectory);
            if (!ReadPEValue(image, size, optionalOffset + offsetof(IMAGE_OPTIONAL_HEADER32, SizeOfHeaders), sizeOfHeaders) ||
                !ReadPEValue(image, size, optionalOffset + offsetof(IMAGE_OPTIONAL_HEADER32, NumberOfRvaAndSizes), directoryCount))
                return false;
        }
        else if (magic == PE32PlusMagic)
        {
            directoryOffset = offsetof(IMAGE_OPTIONAL_HEADER64, DataDirectory);
            if (!ReadPEValue(image, size, optionalOffset + offsetof(IMAGE_OPTIONAL_HEADER64, SizeOfHeaders), sizeOfHeaders) ||
                !ReadPEValue(image, size, optionalOffset + offsetof(IMAGE_OPTIONAL_HEADER64, NumberOfRvaAndSizes), directoryCount))
                return false;
        }
        else
        {
            return false;
        }

        size_t requiredOptionalSize = directoryOffset +
            (IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR + 1) * sizeof(IMAGE_DATA_DIRECTORY);
        if (directoryCount <= IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR ||
            fileHeader.SizeOfOptionalHeader < requiredOptionalSize ||
            !ReadPEValue(image, size, optionalOffset + directoryOffset +
                IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR * sizeof(IMAGE_DATA_DIRECTORY), comDirectory) ||
            comDirectory.VirtualAddress == 0 || comDirectory.Size < sizeof(IMAGE_COR20_HEADER))
            return false;

        size_t sectionsOffset = optionalOffset + fileHeader.SizeOfOptionalHeader;
        if (sectionsOffset > size || fileHeader.NumberOfSections > (size - sectionsOffset) / sizeof(IMAGE_SECTION_HEADER))
            return false;
        size_t corOffset;
        if (!MapRva(image, size, sectionsOffset, fileHeader.NumberOfSections, sizeOfHeaders,
            comDirectory.VirtualAddress, sizeof(IMAGE_COR20_HEADER), corOffset))
            return false;
        IMAGE_COR20_HEADER corHeader;
        if (!ReadPEValue(image, size, corOffset, corHeader) ||
            corHeader.cb < sizeof(corHeader) || corHeader.MetaData.Size == 0)
            return false;

        machine = fileHeader.Machine;
        peKind = magic == PE32PlusMagic ? pe32Plus : peNot;
        if ((corHeader.Flags & COMIMAGE_FLAGS_ILONLY) != 0)
        {
            peKind |= peILonly;
            if (magic == PE32PlusMagic && machine == IMAGE_FILE_MACHINE_I386)
                peKind &= ~static_cast<DWORD>(pe32Plus);
        }
        if (COR_IS_32BIT_REQUIRED(corHeader.Flags))
            peKind |= pe32BitRequired;
        else if (COR_IS_32BIT_PREFERRED(corHeader.Flags))
            peKind |= pe32BitPreferred;
        if (peKind == peNot)
            peKind = pe32BitRequired;

        if (corHeader.ManagedNativeHeader.Size >= sizeof(ReadyToRunHeaderPrefix))
        {
            size_t nativeOffset;
            ReadyToRunHeaderPrefix nativeHeader;
            if (MapRva(image, size, sectionsOffset, fileHeader.NumberOfSections, sizeOfHeaders,
                    corHeader.ManagedNativeHeader.VirtualAddress,
                    corHeader.ManagedNativeHeader.Size, nativeOffset) &&
                ReadPEValue(image, size, nativeOffset, nativeHeader) &&
                nativeHeader.signature == ReadyToRunSignature)
            {
                DWORD nativeMachine = 0;
#if defined(TARGET_X86)
                nativeMachine = IMAGE_FILE_MACHINE_I386;
#elif defined(TARGET_AMD64)
                nativeMachine = IMAGE_FILE_MACHINE_AMD64;
#elif defined(TARGET_ARM64)
                nativeMachine = IMAGE_FILE_MACHINE_ARM64;
#endif
                DWORD osMask = 0;
#if defined(TARGET_LINUX)
                osMask = IMAGE_FILE_MACHINE_OS_MASK_LINUX;
#elif defined(TARGET_OSX) || defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_MACCATALYST)
                osMask = IMAGE_FILE_MACHINE_OS_MASK_APPLE;
#elif defined(TARGET_FREEBSD)
                osMask = IMAGE_FILE_MACHINE_OS_MASK_FREEBSD;
#elif defined(TARGET_NETBSD)
                osMask = IMAGE_FILE_MACHINE_OS_MASK_NETBSD;
#elif defined(TARGET_SUNOS)
                osMask = IMAGE_FILE_MACHINE_OS_MASK_SUN;
#endif
                if (nativeMachine != 0 && machine == (nativeMachine ^ osMask))
                    machine = nativeMachine;
                if ((nativeHeader.flags & 1) != 0)
                {
                    peKind = peILonly;
                    machine = IMAGE_FILE_MACHINE_I386;
                }
            }
        }

        metadataSize = corHeader.MetaData.Size;
        return MapRva(image, size, sectionsOffset, fileHeader.NumberOfSections, sizeOfHeaders,
            corHeader.MetaData.VirtualAddress, metadataSize, metadataOffset);
    }

    constexpr uint32_t SupportedDuplicateChecks =
        MDDupDefault | MDDupTypeDef | MDDupModuleRef | MDDupExportedType |
        MDDupAssemblyRef | MDDupPermission | MDDupFile;

    minipal::com_ptr<ControllingIUnknown> CreateExposedObject(
        minipal::com_ptr<ControllingIUnknown> unknown, DNMDOwner* owner,
        bool threadSafe, uint32_t duplicateChecks)
    {
        mdhandle_view handle_view{ owner };
        MetadataEmit* emit = unknown->CreateAndAddTearOff<MetadataEmit>(handle_view, duplicateChecks);
        MetadataImportRO* import = unknown->CreateAndAddTearOff<MetadataImportRO>(handle_view);
        if (!threadSafe)
        {
            (void)unknown->CreateAndAddTearOff<InternalMetadataImportRW>(handle_view);
            return unknown;
        }

        minipal::com_ptr<ControllingIUnknown> threadSafeUnknown;
        threadSafeUnknown.Attach(new ControllingIUnknown());

        (void)threadSafeUnknown->CreateAndAddTearOff<DelegatingDNMDOwner>(handle_view);
        auto* wrapper = threadSafeUnknown->CreateAndAddTearOff<ThreadSafeImportEmit<MetadataImportRO, MetadataEmit>>(
            std::move(unknown), import, emit);
        (void)threadSafeUnknown->CreateAndAddTearOff<InternalMetadataImportRW>(handle_view, wrapper->GetLock());
        return threadSafeUnknown;
    }

    template<typename T>
    HRESULT PublishScope(minipal::com_ptr<ControllingIUnknown> scope, REFIID riid, T** output)
    {
        if (output == nullptr)
            return E_POINTER;
        *output = nullptr;

        minipal::com_ptr<T> requestedInterface;
        HRESULT hr = scope->QueryInterface(riid, (void**)&requestedInterface);
        if (FAILED(hr))
            return hr;

        MetadataScopeRegistry::RegisterScope(scope.p);
        *output = requestedInterface.Detach();
        return hr;
    }

    class MDDispenser final : public TearOffBase<IMetaDataDispenserEx>
    {
        bool _threadSafe = false;
        uint32_t _duplicateChecks = MDDupDefault;
        uint32_t _updateMode = MDUpdateFull;
        CorMetaDataInitialSize _initialSize = MDInitialSizeDefault;
    protected:
        virtual bool TryGetInterfaceOnThis(REFIID riid, void** ppvObject) override
        {
            if (riid == IID_IMetaDataDispenserEx || riid == IID_IMetaDataDispenser)
            {
                *ppvObject = static_cast<IMetaDataDispenserEx*>(this);
                return true;
            }
            return false;
        }

    public: // IMetaDataDispenser
        using TearOffBase<IMetaDataDispenserEx>::TearOffBase;

        STDMETHOD(DefineScope)(
            REFCLSID    rclsid,
            DWORD       dwCreateFlags,
            REFIID      riid,
            IUnknown** ppIUnk) override
        {
            if (rclsid != CLSID_CLR_v2_MetaData)
            {
                // DNMD::Interfaces only creating v2 metadata images.
                return CLDB_E_FILE_OLDVER;
            }

            if (dwCreateFlags != 0)
            {
                return E_INVALIDARG;
            }

            mdhandle_ptr md_ptr { md_create_new_handle() };
            if (md_ptr == nullptr)
                return E_OUTOFMEMORY;

            // Initialize the MVID of the new image.
            mdcursor_t moduleCursor;
            if (!md_token_to_cursor(md_ptr.get(), TokenFromRid(1, mdtModule), &moduleCursor))
                return E_FAIL;

            GUID guid;
            if (!minipal_guid_v4_create(&guid))
                return E_FAIL;

            static_assert(sizeof(mdguid_t) == sizeof(GUID), "DNMD and minipal GUID sizes must match");
            mdguid_t mvid;
            std::memcpy(&mvid, &guid, sizeof(mvid));

            if (!md_set_column_value_as_guid(moduleCursor, mdtModule_Mvid, mvid))
                return E_OUTOFMEMORY;

            minipal::com_ptr<ControllingIUnknown> obj;
            obj.Attach(new (std::nothrow) ControllingIUnknown());
            if (obj == nullptr)
                return E_OUTOFMEMORY;

            try
            {
                DNMDOwner* owner = obj->CreateAndAddTearOff<DNMDOwner>(std::move(md_ptr), _duplicateChecks, _updateMode, true);
                return PublishScope(CreateExposedObject(std::move(obj), owner, _threadSafe, _duplicateChecks), riid, ppIUnk);
            }
            catch(std::bad_alloc const&)
            {
                return E_OUTOFMEMORY;
            }
        }

        STDMETHOD(OpenScope)(
            LPCWSTR     szScope,
            DWORD       dwOpenFlags,
            REFIID      riid,
            IUnknown** ppIUnk) override
        {
            if (ppIUnk == nullptr)
                return E_INVALIDARG;
            *ppIUnk = nullptr;
            if (szScope == nullptr || szScope[0] == 0 || (dwOpenFlags & ofTakeOwnership) != 0)
                return E_INVALIDARG;

            if (szScope[0] == 'f' && szScope[1] == 'i' && szScope[2] == 'l' &&
                szScope[3] == 'e' && szScope[4] == ':')
                szScope += 5;

#ifdef BUILD_WINDOWS
            std::ifstream file(szScope, std::ios::binary | std::ios::ate);
#else
            pal::StringConvert<WCHAR, char> path(szScope);
            if (!path.Success())
                return E_INVALIDARG;
            char const* fileName = path;
            std::ifstream file(fileName, std::ios::binary | std::ios::ate);
#endif
            if (!file)
            {
                if (errno == ENOENT)
                    return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, 2);
                if (errno == EACCES)
                    return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, 5);
                return E_FAIL;
            }

            std::streamoff length = file.tellg();
            if (length <= 0)
                return CLDB_E_FILE_CORRUPT;
            if (static_cast<uint64_t>(length) > std::numeric_limits<ULONG>::max())
                return CLDB_E_TOO_BIG;

            malloc_ptr<uint8_t> image{ static_cast<uint8_t*>(::malloc(static_cast<size_t>(length))) };
            if (image == nullptr)
                return E_OUTOFMEMORY;
            file.seekg(0);
            if (!file.read(reinterpret_cast<char*>(image.get()), length))
                return E_FAIL;

            size_t offset = 0;
            uint32_t metadataSize = static_cast<uint32_t>(length);
            DWORD peKind = peNot, machine = 0;
            uint32_t signature;
            bool isPE = !ReadPEValue(image.get(), static_cast<size_t>(length), 0, signature) ||
                signature != 0x424a5342;
            if (isPE)
            {
                if (!FindPEMetadata(image.get(), static_cast<size_t>(length),
                    offset, metadataSize, peKind, machine))
                    return COR_E_BADIMAGEFORMAT;
            }

            HRESULT hr = OpenScopeOnMemory(image.get() + offset, metadataSize,
                dwOpenFlags | ofCopyMemory, riid, ppIUnk);
            if (FAILED(hr) || !isPE)
                return hr;

            minipal::com_ptr<IDNMDOwner> owner;
            hr = (*ppIUnk)->QueryInterface(IID_IDNMDOwner, (void**)&owner.p);
            if (FAILED(hr))
            {
                (*ppIUnk)->Release();
                *ppIUnk = nullptr;
                return hr;
            }
            owner->SetPEKind(peKind, machine);
            return S_OK;
        }

        STDMETHOD(OpenScopeOnMemory)(
            LPCVOID     pData,
            ULONG       cbData,
            DWORD       dwOpenFlags,
            REFIID      riid,
            IUnknown** ppIUnk) override
        {
            if (ppIUnk == nullptr)
                return E_INVALIDARG;
            *ppIUnk = nullptr;
            if (pData == nullptr || cbData == 0)
                return E_INVALIDARG;

            minipal::cotaskmem_ptr<void> nowOwned;
            if (dwOpenFlags & ofTakeOwnership)
                nowOwned.reset((void*)pData);

            malloc_ptr<void> copiedMem;
            if (dwOpenFlags & ofCopyMemory)
            {
                copiedMem.reset(::malloc(cbData));
                if (copiedMem == nullptr)
                    return E_OUTOFMEMORY;

                // Reassign the newly allocated memory to the param variable.
                pData = ::memcpy(copiedMem.get(), pData, cbData);
            }

            mdhandle_t mdhandle;
            if (!md_create_handle(pData, cbData, &mdhandle))
                return CLDB_E_FILE_CORRUPT;

            mdhandle_ptr md_ptr{ mdhandle };

            minipal::com_ptr<ControllingIUnknown> obj;
            obj.Attach(new (std::nothrow) ControllingIUnknown());
            if (obj == nullptr)
                return E_OUTOFMEMORY;

            try
            {
                // Internal opens of compressed (#~) metadata start RO and upgrade on demand.
                bool internalReadOnly = riid == IID_IMDInternalImport &&
                    (dwOpenFlags & ofReadWriteMask) == ofRead &&
                    !md_is_uncompressed_table_heap(md_ptr.get());
                bool readWrite = (dwOpenFlags & ofReadOnly) == 0 && !internalReadOnly;
                DNMDOwner* owner = obj->CreateAndAddTearOff<DNMDOwner>(
                    std::move(md_ptr), std::move(copiedMem), std::move(nowOwned), _duplicateChecks, _updateMode, readWrite);
                mdhandle_view handle_view{ owner };

                if (!readWrite)
                {
                    // If we're read-only, then we don't need to deal with thread safety.
                    (void)obj->CreateAndAddTearOff<MetadataImportRO>(handle_view);
                    (void)obj->CreateAndAddTearOff<InternalMetadataImportRO>(handle_view);
                    return PublishScope(std::move(obj), riid, ppIUnk);
                }

                // If we're read-write, go through our helper to create an object that respects all of the options
                // (as the various options affect writing operations only).
                return PublishScope(CreateExposedObject(std::move(obj), owner, _threadSafe, _duplicateChecks), riid, ppIUnk);
            }
            catch(std::bad_alloc const&)
            {
                return E_OUTOFMEMORY;
            }
        }

        public: // IMetaDataDispenserEx
            STDMETHOD(SetOption)(
            REFGUID     optionid,
            VARIANT const *value) override
        {
                if (value == nullptr)
                    return E_INVALIDARG;

                if (optionid == MetaDataCheckDuplicatesFor)
                {
                    if (V_VT(value) != VT_UI4 || (V_UI4(value) & ~SupportedDuplicateChecks) != 0)
                        return E_INVALIDARG;

                    _duplicateChecks = V_UI4(value);
                    return S_OK;
                }

                if (optionid == MetaDataInitialSize)
                {
                    if (V_VT(value) != VT_UI4 ||
                        (V_UI4(value) != MDInitialSizeDefault && V_UI4(value) != MDInitialSizeMinimal))
                        return E_INVALIDARG;

                    // DNMD allocates table and heap storage on demand, so Minimal is already satisfied.
                    _initialSize = static_cast<CorMetaDataInitialSize>(V_UI4(value));
                    return S_OK;
                }

                if (optionid == MetaDataSetUpdate)
                {
                    if (V_VT(value) != VT_UI4)
                        return E_INVALIDARG;

                    HRESULT hr = ValidateDNMDUpdateMode(V_UI4(value));
                    if (FAILED(hr))
                        return hr;
                    _updateMode = V_UI4(value);
                    return S_OK;
                }

                if (optionid == MetaDataThreadSafetyOptions)
            {
                _threadSafe = V_UI4(value) == CorThreadSafetyOptions::MDThreadSafetyOn;
                return S_OK;
            }
            return E_INVALIDARG;
        }

        STDMETHOD(GetOption)(
            REFGUID     optionid,
            VARIANT *pvalue) override
        {
            if (pvalue == nullptr)
                return E_INVALIDARG;

            if (optionid == MetaDataCheckDuplicatesFor)
            {
                V_VT(pvalue) = VT_UI4;
                V_UI4(pvalue) = _duplicateChecks;
                return S_OK;
            }

            if (optionid == MetaDataInitialSize)
            {
                V_VT(pvalue) = VT_UI4;
                V_UI4(pvalue) = _initialSize;
                return S_OK;
            }

            if (optionid == MetaDataSetUpdate)
            {
                V_VT(pvalue) = VT_UI4;
                V_UI4(pvalue) = _updateMode;
                return S_OK;
            }

            if (optionid == MetaDataThreadSafetyOptions)
            {
                V_UI4(pvalue) = _threadSafe ? CorThreadSafetyOptions::MDThreadSafetyOn : CorThreadSafetyOptions::MDThreadSafetyOff;
                return S_OK;
            }
            return E_INVALIDARG;
        }

        STDMETHOD(OpenScopeOnITypeInfo)(
            ITypeInfo   *pITI,
            DWORD       dwOpenFlags,
            REFIID      riid,
            IUnknown    **ppIUnk) override
        {
            UNREFERENCED_PARAMETER(pITI);
            UNREFERENCED_PARAMETER(dwOpenFlags);
            UNREFERENCED_PARAMETER(riid);
            UNREFERENCED_PARAMETER(ppIUnk);
            return E_NOTIMPL;
        }

        STDMETHOD(GetCORSystemDirectory)(
        _Out_writes_to_opt_(cchBuffer, *pchBuffer)
            LPWSTR      szBuffer,
            DWORD       cchBuffer,
            DWORD*      pchBuffer) override
        {
            UNREFERENCED_PARAMETER(szBuffer);
            UNREFERENCED_PARAMETER(cchBuffer);
            UNREFERENCED_PARAMETER(pchBuffer);
            return E_NOTIMPL;
        }

        STDMETHOD(FindAssembly)(
            LPCWSTR  szAppBase,
            LPCWSTR  szPrivateBin,
            LPCWSTR  szGlobalBin,
            LPCWSTR  szAssemblyName,
            LPCWSTR  szName,
            ULONG    cchName,
            ULONG    *pcName) override
        {
            UNREFERENCED_PARAMETER(szAppBase);
            UNREFERENCED_PARAMETER(szPrivateBin);
            UNREFERENCED_PARAMETER(szGlobalBin);
            UNREFERENCED_PARAMETER(szAssemblyName);
            UNREFERENCED_PARAMETER(szName);
            UNREFERENCED_PARAMETER(cchName);
            UNREFERENCED_PARAMETER(pcName);
            return E_NOTIMPL;
        }

        STDMETHOD(FindAssemblyModule)(
            LPCWSTR  szAppBase,
            LPCWSTR  szPrivateBin,
            LPCWSTR  szGlobalBin,
            LPCWSTR  szAssemblyName,
            LPCWSTR  szModuleName,
        _Out_writes_to_opt_(cchName, *pcName)
            LPWSTR   szName,
            ULONG    cchName,
            ULONG    *pcName) override
        {
            UNREFERENCED_PARAMETER(szAppBase);
            UNREFERENCED_PARAMETER(szPrivateBin);
            UNREFERENCED_PARAMETER(szGlobalBin);
            UNREFERENCED_PARAMETER(szAssemblyName);
            UNREFERENCED_PARAMETER(szModuleName);
            UNREFERENCED_PARAMETER(szName);
            UNREFERENCED_PARAMETER(cchName);
            UNREFERENCED_PARAMETER(pcName);
            return E_NOTIMPL;
        }
    };
}

extern "C" DNMD_EXPORT
HRESULT ConvertDNMDInternalImport(IMDInternalImport* source, IMDInternalImport** converted)
{
    if (converted == nullptr)
        return E_INVALIDARG;
    *converted = nullptr;
    if (source == nullptr)
        return E_INVALIDARG;

    minipal::com_ptr<IDNMDOwner> sourceOwner;
    HRESULT hr = source->QueryInterface(IID_IDNMDOwner, (void**)&sourceOwner);
    if (FAILED(hr))
        return hr;
    if (sourceOwner->IsReadWrite())
    {
        *converted = source;
        return S_FALSE;
    }

    size_t size = 0;
    (void)md_write_to_buffer(sourceOwner->MetaData(), nullptr, &size);
    if (size == 0)
        return CLDB_E_FILE_CORRUPT;
    if (size > std::numeric_limits<ULONG>::max())
        return CLDB_E_TOO_BIG;

    malloc_ptr<void> image{ ::malloc(size) };
    if (image == nullptr)
        return E_OUTOFMEMORY;
    if (!md_write_to_buffer(sourceOwner->MetaData(), static_cast<uint8_t*>(image.get()), &size))
        return CLDB_E_FILE_CORRUPT;

    mdhandle_t handle;
    if (!md_create_handle(image.get(), size, &handle))
        return CLDB_E_FILE_CORRUPT;
    mdhandle_ptr newHandle{ handle };

    try
    {
        minipal::com_ptr<ControllingIUnknown> object;
        object.Attach(new ControllingIUnknown());
        DNMDOwner* owner = object->CreateAndAddTearOff<DNMDOwner>(
            std::move(newHandle), std::move(image), minipal::cotaskmem_ptr<void>{},
            sourceOwner->DuplicateChecks(), sourceOwner->UpdateMode(), true);
        auto exposed = CreateExposedObject(std::move(object), owner, true, owner->DuplicateChecks());
        return PublishScope(std::move(exposed), IID_IMDInternalImport, converted);
    }
    catch (std::bad_alloc const&)
    {
        return E_OUTOFMEMORY;
    }
}

extern "C" DNMD_EXPORT
HRESULT GetDNMDPublicInterfaceFromInternal(
    IMDInternalImport* source, REFIID riid, void** publicInterface)
{
    if (publicInterface == nullptr)
        return E_INVALIDARG;
    *publicInterface = nullptr;
    if (source == nullptr)
        return E_INVALIDARG;

    IMDInternalImport* writable = nullptr;
    HRESULT hr = ConvertDNMDInternalImport(source, &writable);
    if (FAILED(hr))
        return hr;
    if (hr == S_FALSE)
        return source->QueryInterface(riid, publicInterface);

    minipal::com_ptr<IMDInternalImport> converted;
    converted.Attach(writable);
    return converted->QueryInterface(riid, publicInterface);
}

extern "C" DNMD_EXPORT
HRESULT ReOpenDNMDMetaDataWithMemory(IUnknown* scope, void const* data, ULONG size, DWORD flags)
{
    if (scope == nullptr || data == nullptr || size == 0 ||
        (flags & ~(ofCopyMemory | ofTakeOwnership)) != 0)
        return E_INVALIDARG;

    minipal::com_ptr<IDNMDOwner> owner;
    HRESULT hr = scope->QueryInterface(IID_IDNMDOwner, (void**)&owner.p);
    if (FAILED(hr))
        return hr;

    minipal::com_ptr<IMDInternalImport> internal;
    hr = scope->QueryInterface(IID_IMDInternalImport, (void**)&internal.p);
    if (FAILED(hr))
        return hr;

    minipal_rwlock* lock = internal->GetReaderWriterLock();
    if (lock != nullptr && !minipal_rwlock_enter_write(lock))
        return E_FAIL;
    std::unique_ptr<minipal_rwlock, decltype(&minipal_rwlock_leave_write)> writeLock(lock, minipal_rwlock_leave_write);

    malloc_ptr<void> backing{ ::malloc(size) };
    if (backing == nullptr)
        return E_OUTOFMEMORY;
    std::memcpy(backing.get(), data, size);

    mdhandle_t handle;
    if (!md_create_handle(backing.get(), size, &handle))
        return CLDB_E_FILE_CORRUPT;
    mdhandle_ptr replacement{ handle };
    if (!md_validate(replacement.get()))
        return CLDB_E_FILE_CORRUPT;

    hr = owner->ReplaceMetaData(std::move(replacement), std::move(backing));
    if (SUCCEEDED(hr) && (flags & ofTakeOwnership) != 0)
        CoTaskMemFree(const_cast<void*>(data));
    if (SUCCEEDED(hr))
        owner->ClearPEKind();
    return hr;
}

extern "C" DNMD_EXPORT
HRESULT GetDispenser(
    REFGUID riid,
    void** ppObj)
{
    if (riid != IID_IMetaDataDispenser
        && riid != IID_IMetaDataDispenserEx)
    {
        return E_INVALIDARG;
    }

    if (ppObj == nullptr)
        return E_INVALIDARG;

    try
    {
        minipal::com_ptr<ControllingIUnknown> obj;
        obj.Attach(new ControllingIUnknown());
        (void)obj->CreateAndAddTearOff<MDDispenser>();
        return obj->QueryInterface(riid, (void**)ppObj);
    }
    catch(std::bad_alloc const&)
    {
        return E_OUTOFMEMORY;
    }
}
