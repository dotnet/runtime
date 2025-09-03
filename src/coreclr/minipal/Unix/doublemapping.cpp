// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <stddef.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <fcntl.h>
#include <unistd.h>
#include <inttypes.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <assert.h>
#include <limits.h>
#include <errno.h>
#include <sys/resource.h>
#if defined(TARGET_LINUX) && !defined(MFD_CLOEXEC)
#include <linux/memfd.h>
#include <sys/syscall.h> // __NR_memfd_create
#define memfd_create(...) syscall(__NR_memfd_create, __VA_ARGS__)
#elif defined(TARGET_ANDROID)
#include <sys/syscall.h> // __NR_memfd_create
#define memfd_create(...) syscall(__NR_memfd_create, __VA_ARGS__)
#endif // TARGET_LINUX && !MFD_CLOEXEC
#include "minipal.h"
#include "minipal/cpufeatures.h"

#ifndef TARGET_APPLE
#include <link.h>
#include <dlfcn.h>
#endif // TARGET_APPLE

#ifdef TARGET_APPLE

#include <mach/mach.h>

#else // TARGET_APPLE

#ifdef TARGET_64BIT
static const off_t MaxDoubleMappedSize = 2048ULL*1024*1024*1024;
#else
static const off_t MaxDoubleMappedSize = UINT_MAX;
#endif

#endif // TARGET_APPLE

bool VMToOSInterface::CreateDoubleMemoryMapper(void** pHandle, size_t *pMaxExecutableCodeSize)
{
    if (minipal_detect_rosetta())
    {
        // Rosetta doesn't support double mapping correctly
        return false;
    }

#ifndef TARGET_APPLE

#ifdef TARGET_FREEBSD
    int fd = shm_open(SHM_ANON, O_RDWR | O_CREAT, S_IRWXU);
#elif defined(TARGET_LINUX) || defined(TARGET_ANDROID)
    int fd = memfd_create("doublemapper", MFD_CLOEXEC);
#else
    int fd = -1;

#ifndef TARGET_ANDROID
    // Bionic doesn't have shm_{open,unlink}
    // POSIX fallback
    if (fd == -1)
    {
        char name[24];
        sprintf(name, "/shm-dotnet-%d", getpid());
        name[sizeof(name) - 1] = '\0';
        shm_unlink(name);
        fd = shm_open(name, O_RDWR | O_CREAT | O_EXCL | O_NOFOLLOW, 0600);
        shm_unlink(name);
    }
#endif // !TARGET_ANDROID

    if (fd == -1)
    {
        return false;
    }
#endif
    off_t maxDoubleMappedMemorySize = MaxDoubleMappedSize;
    
    // Set the maximum double mapped memory size to the size of the physical memory
    long pages = sysconf(_SC_PHYS_PAGES);
    if (pages != -1)
    {
        long pageSize = sysconf(_SC_PAGE_SIZE);
        if (pageSize != -1)
        {
            long physicalMemorySize = (long)pages * pageSize;
            if (maxDoubleMappedMemorySize > physicalMemorySize)
            {
                maxDoubleMappedMemorySize = physicalMemorySize;
            }
        }
    }

    // Clip the maximum double mapped memory size to 1/4 of the virtual address space limit.
    // When such a limit is set, GC reserves 1/2 of it, so we need to leave something
    // for the rest of the process.
    struct rlimit virtualAddressSpaceLimit;
    if ((getrlimit(RLIMIT_AS, &virtualAddressSpaceLimit) == 0) && (virtualAddressSpaceLimit.rlim_cur != RLIM_INFINITY))
    {
        virtualAddressSpaceLimit.rlim_cur /= 4;
        if (maxDoubleMappedMemorySize > virtualAddressSpaceLimit.rlim_cur)
        {
            maxDoubleMappedMemorySize = virtualAddressSpaceLimit.rlim_cur;
        }
    }

    // Clip the maximum double mapped memory size to the file size limit
    struct rlimit fileSizeLimit;
    if ((getrlimit(RLIMIT_FSIZE, &fileSizeLimit) == 0) && (fileSizeLimit.rlim_cur != RLIM_INFINITY))
    {
        if (maxDoubleMappedMemorySize > fileSizeLimit.rlim_cur)
        {
            maxDoubleMappedMemorySize = fileSizeLimit.rlim_cur;
        }
    }

    if (ftruncate(fd, maxDoubleMappedMemorySize) == -1)
    {
        close(fd);
        return false;
    }

    *pMaxExecutableCodeSize = maxDoubleMappedMemorySize;
    *pHandle = (void*)(size_t)fd;
#else // !TARGET_APPLE

    *pMaxExecutableCodeSize = SIZE_MAX;
    *pHandle = NULL;
#endif // !TARGET_APPLE

    return true;
}

void VMToOSInterface::DestroyDoubleMemoryMapper(void *mapperHandle)
{
#ifndef TARGET_APPLE
    close((int)(size_t)mapperHandle);
#endif
}

extern "C" void* PAL_VirtualReserveFromExecutableMemoryAllocatorWithinRange(const void* lpBeginAddress, const void* lpEndAddress, size_t dwSize, int fStoreAllocationInfo);

#ifdef TARGET_APPLE
bool IsMapJitFlagNeeded()
{
    static volatile int isMapJitFlagNeeded = -1;

    if (isMapJitFlagNeeded == -1)
    {
        int mapJitFlagCheckResult = 0;
        int pageSize = sysconf(_SC_PAGE_SIZE);
        // Try to map a page with read-write-execute protection. It should fail on Mojave hardened runtime and higher.
        void* testPage = mmap(NULL, pageSize, PROT_READ | PROT_WRITE | PROT_EXEC, MAP_ANONYMOUS | MAP_PRIVATE, -1, 0);
        if (testPage == MAP_FAILED && (errno == EACCES))
        {
            // The mapping has failed with EACCES, check if making the same mapping with MAP_JIT flag works
            testPage = mmap(NULL, pageSize, PROT_READ | PROT_WRITE | PROT_EXEC, MAP_ANONYMOUS | MAP_PRIVATE | MAP_JIT, -1, 0);
            if (testPage != MAP_FAILED)
            {
                mapJitFlagCheckResult = 1;
            }
        }

        if (testPage != MAP_FAILED)
        {
            munmap(testPage, pageSize);
        }

        isMapJitFlagNeeded = mapJitFlagCheckResult;
    }

    return (bool)isMapJitFlagNeeded;
}
#endif // TARGET_APPLE

void* VMToOSInterface::ReserveDoubleMappedMemory(void *mapperHandle, size_t offset, size_t size, const void *rangeStart, const void* rangeEnd)
{
    int fd = (int)(size_t)mapperHandle;

    bool isUnlimitedRange = (rangeStart == NULL) && (rangeEnd == NULL);

    if (isUnlimitedRange)
    {
        rangeEnd = (void*)SIZE_MAX;
    }

    void* result = PAL_VirtualReserveFromExecutableMemoryAllocatorWithinRange(rangeStart, rangeEnd, size, 0 /* fStoreAllocationInfo */);
#ifndef TARGET_APPLE
    int mmapFlags = MAP_SHARED;
#ifdef TARGET_HAIKU
    mmapFlags |= MAP_NORESERVE;
#endif // TARGET_HAIKU
    if (result != NULL)
    {
        // Map the shared memory over the range reserved from the executable memory allocator.
        result = mmap(result, size, PROT_NONE, mmapFlags | MAP_FIXED, fd, offset);
        if (result == MAP_FAILED)
        {
            assert(false);
            result = NULL;
        }
    }
#endif // TARGET_APPLE

    // For requests with limited range, don't try to fall back to reserving at any address
    if ((result != NULL) || !isUnlimitedRange)
    {
        return result;
    }

#ifndef TARGET_APPLE
    result = mmap(NULL, size, PROT_NONE, mmapFlags, fd, offset);
#else
    int mmapFlags = MAP_ANON | MAP_PRIVATE;
    if (IsMapJitFlagNeeded())
    {
        mmapFlags |= MAP_JIT;
    }
    result = mmap(NULL, size, PROT_NONE, mmapFlags, -1, 0);
#endif
    if (result == MAP_FAILED)
    {
        assert(false);
        result = NULL;
    }
    return result;
}

void *VMToOSInterface::CommitDoubleMappedMemory(void* pStart, size_t size, bool isExecutable)
{
    if (mprotect(pStart, size, isExecutable ? (PROT_READ | PROT_EXEC) : (PROT_READ | PROT_WRITE)) == -1)
    {
        return NULL;
    }

    return pStart;
}

bool VMToOSInterface::ReleaseDoubleMappedMemory(void *mapperHandle, void* pStart, size_t offset, size_t size)
{
#ifndef TARGET_APPLE
    int fd = (int)(size_t)mapperHandle;
    if (mmap(pStart, size, PROT_READ | PROT_WRITE, MAP_SHARED | MAP_FIXED, fd, offset) == MAP_FAILED)
    {
        return false;
    }
    memset(pStart, 0, size);
#endif // TARGET_APPLE
    return munmap(pStart, size) != -1;
}

void* VMToOSInterface::GetRWMapping(void *mapperHandle, void* pStart, size_t offset, size_t size)
{
#ifndef TARGET_APPLE
    int fd = (int)(size_t)mapperHandle;
    void* result = mmap(NULL, size, PROT_READ | PROT_WRITE, MAP_SHARED, fd, offset);
    if (result == MAP_FAILED)
    {
        result = NULL;
    }
    return result;
#else // TARGET_APPLE
#ifdef TARGET_AMD64
    vm_address_t startRW;
    vm_prot_t curProtection, maxProtection;
    kern_return_t kr = vm_remap(mach_task_self(), &startRW, size, 0, VM_FLAGS_ANYWHERE | VM_FLAGS_RANDOM_ADDR,
                                mach_task_self(), (vm_address_t)pStart, FALSE, &curProtection, &maxProtection, VM_INHERIT_NONE);

    if (kr != KERN_SUCCESS)
    {
        return NULL;
    }

    int st = mprotect((void*)startRW, size, PROT_READ | PROT_WRITE);
    if (st == -1)
    {
        munmap((void*)startRW, size);
        return NULL;
    }

    return (void*)startRW;
#else // TARGET_AMD64
    // This method should not be called on OSX ARM64
    assert(false);
    return NULL;
#endif // TARGET_AMD64
#endif // TARGET_APPLE
}

bool VMToOSInterface::ReleaseRWMapping(void* pStart, size_t size)
{
    return munmap(pStart, size) != -1;
}

#ifndef TARGET_APPLE
#define MAX_TEMPLATE_THUNK_TYPES 3 // Maximum number of times the CreateTemplate api can be called
struct TemplateThunkMappingData
{
    int fdImage;
    off_t offsetInFileOfStartOfSection;
    void* addrOfStartOfSection; // Always NULL if the template mapping data could not be initialized
    void* addrOfEndOfSection;
    bool imageTemplates;
    int templatesCreated;
    off_t nonImageTemplateCurrent;
};

struct InitializeTemplateThunkLocals
{
    void* pTemplate;
    Dl_info info;
    TemplateThunkMappingData data;
};

static TemplateThunkMappingData *s_pThunkData = NULL;

#ifdef FEATURE_MAP_THUNKS_FROM_IMAGE

static Elf32_Word Elf32_WordMin(Elf32_Word left, Elf32_Word  right)
{
    return left < right ? left : right;
}

static int InitializeTemplateThunkMappingDataPhdrCallback(struct dl_phdr_info *info, size_t size, void *dataPtr)
{
    InitializeTemplateThunkLocals *locals = (InitializeTemplateThunkLocals*)dataPtr;

    if ((void*)info->dlpi_addr == locals->info.dli_fbase)
    {
        for (size_t j = 0; j < info->dlpi_phnum; j++)
        {
            uint8_t* baseSectionAddr = (uint8_t*)locals->info.dli_fbase + info->dlpi_phdr[j].p_vaddr;
            if (locals->pTemplate < baseSectionAddr)
            {
                // Address is before the virtual address of this section begins
                continue;
            }

            // Since this is all in support of mapping code from the file, we need to ensure that the region we find
            // is actually present in the file.
            Elf32_Word sizeOfSectionWhichCanBeMapped = Elf32_WordMin(info->dlpi_phdr[j].p_filesz, info->dlpi_phdr[j].p_memsz);

            uint8_t* endAddressAllowedForTemplate = baseSectionAddr + sizeOfSectionWhichCanBeMapped;
            if (locals->pTemplate >= endAddressAllowedForTemplate)
            {
                // Template is after the virtual address of this section ends (or the mappable region of the file)
                continue;
            }

            // At this point, we have found the template section. Attempt to open the file, and record the various offsets for future use

            if (strlen(info->dlpi_name) == 0)
            {
                // This image cannot be directly referenced without capturing the argv[0] parameter
                return -1;
            }

            int fdImage = open(info->dlpi_name, O_RDONLY);
            if (fdImage == -1)
            {
                return -1; // Opening the image didn't work
            }
            
            locals->data.fdImage = fdImage;
            locals->data.offsetInFileOfStartOfSection = info->dlpi_phdr[j].p_offset;
            locals->data.addrOfStartOfSection = baseSectionAddr;
            locals->data.addrOfEndOfSection = baseSectionAddr + sizeOfSectionWhichCanBeMapped;
            locals->data.imageTemplates = true;
            return 1; // We have found the result. Abort further processing.
        }
    }

    // This isn't the interesting .so
    return 0;
}
#endif // FEATURE_MAP_THUNKS_FROM_IMAGE

TemplateThunkMappingData *InitializeTemplateThunkMappingData(void* pTemplate)
{
    InitializeTemplateThunkLocals locals;
    locals.pTemplate = pTemplate;
    locals.data.fdImage = 0;
    locals.data.offsetInFileOfStartOfSection = 0;
    locals.data.addrOfStartOfSection = NULL;
    locals.data.addrOfEndOfSection = NULL;
    locals.data.imageTemplates = false;
    locals.data.nonImageTemplateCurrent = 0;
    locals.data.templatesCreated = 0;

#ifdef FEATURE_MAP_THUNKS_FROM_IMAGE
    if (dladdr(pTemplate, &locals.info) != 0)
    {
        dl_iterate_phdr(InitializeTemplateThunkMappingDataPhdrCallback, &locals);
    }
#endif // FEATURE_MAP_THUNKS_FROM_IMAGE

    if (locals.data.addrOfStartOfSection == NULL)
    {
        // This is the detail of thunk data which indicates if we were able to compute the template mapping data from the image.

#ifdef TARGET_FREEBSD
        int fd = shm_open(SHM_ANON, O_RDWR | O_CREAT, S_IRWXU);
#elif defined(TARGET_LINUX) || defined(TARGET_ANDROID)
        int fd = memfd_create("doublemapper-template", MFD_CLOEXEC);
#else
        int fd = -1;
    
#ifndef TARGET_ANDROID
        // Bionic doesn't have shm_{open,unlink}
        // POSIX fallback
        if (fd == -1)
        {
            char name[24];
            sprintf(name, "/shm-dotnet-template-%d", getpid());
            name[sizeof(name) - 1] = '\0';
            shm_unlink(name);
            fd = shm_open(name, O_RDWR | O_CREAT | O_EXCL | O_NOFOLLOW, 0600);
            shm_unlink(name);
        }
#endif // !TARGET_ANDROID
#endif
        if (fd != -1)
        {
            off_t maxFileSize = MAX_TEMPLATE_THUNK_TYPES * 0x10000; // The largest page size we support currently is 64KB.
            if (ftruncate(fd, maxFileSize) == -1) // Reserve a decent size chunk of logical memory for these things.
            {
                close(fd);
            }
            else
            {
                locals.data.fdImage = fd;
                locals.data.offsetInFileOfStartOfSection = 0;
                // We simulate the template thunk mapping data existing in mapped ram, by declaring that it exists at at
                // an address which is not NULL, and which is naturally aligned on the largest page size supported by any
                // architecture we support (0x10000). We do this, as the generalized logic here is designed around remapping
                // already mapped memory, and by doing this we are able to share that logic.
                locals.data.addrOfStartOfSection = (void*)0x10000;
                locals.data.addrOfEndOfSection = ((uint8_t*)locals.data.addrOfStartOfSection) + maxFileSize;
                locals.data.imageTemplates = false;
            }
        }
    }


    TemplateThunkMappingData *pAllocatedData = (TemplateThunkMappingData*)malloc(sizeof(TemplateThunkMappingData));
    *pAllocatedData = locals.data;
    TemplateThunkMappingData *pExpectedNull = NULL; 
    if (__atomic_compare_exchange_n (&s_pThunkData, &pExpectedNull, pAllocatedData, false, __ATOMIC_RELEASE, __ATOMIC_RELAXED))
    {
        return pAllocatedData;
    }
    else
    {
        free(pAllocatedData);
        return __atomic_load_n(&s_pThunkData, __ATOMIC_ACQUIRE);
    }
}
#endif

bool VMToOSInterface::AllocateThunksFromTemplateRespectsStartAddress()
{
#ifdef TARGET_APPLE
    return false;
#else
    return true;
#endif
}

void* VMToOSInterface::CreateTemplate(void* pImageTemplate, size_t templateSize, void (*codePageGenerator)(uint8_t* pageBase, uint8_t* pageBaseRX, size_t size))
{
#ifdef TARGET_APPLE
    return pImageTemplate;
#elif defined(TARGET_X86)
    return NULL; // X86 doesn't support high performance relative addressing, which makes the template system not work
#else
    if (pImageTemplate == NULL)
        return NULL;

    TemplateThunkMappingData* pThunkData = __atomic_load_n(&s_pThunkData, __ATOMIC_ACQUIRE);
    if (s_pThunkData == NULL)
    {
        pThunkData = InitializeTemplateThunkMappingData(pImageTemplate);
    }

    // Unable to create template mapping region
    if (pThunkData->addrOfStartOfSection == NULL)
    {
        return NULL;
    }

    int templatesCreated = __atomic_add_fetch(&pThunkData->templatesCreated, 1, __ATOMIC_SEQ_CST);
    assert(templatesCreated <= MAX_TEMPLATE_THUNK_TYPES);

    if (!pThunkData->imageTemplates)
    {
        // Need to allocate a memory mapped region to fill in the data
        off_t locationInFileToStoreGeneratedCode = __atomic_fetch_add((off_t*)&pThunkData->nonImageTemplateCurrent, (off_t)templateSize, __ATOMIC_SEQ_CST);
        void* mappedMemory = mmap(NULL, templateSize, PROT_READ | PROT_WRITE, MAP_SHARED, pThunkData->fdImage, locationInFileToStoreGeneratedCode);
        if (mappedMemory != MAP_FAILED)
        {
            codePageGenerator((uint8_t*)mappedMemory, (uint8_t*)mappedMemory, templateSize);
            munmap(mappedMemory, templateSize);
            return ((uint8_t*)pThunkData->addrOfStartOfSection) + locationInFileToStoreGeneratedCode;
        }
        else
        {
            return NULL;
        }
    }
    else
    {
        return pImageTemplate;
    }
#endif
}

void* VMToOSInterface::AllocateThunksFromTemplate(void* pTemplate, size_t templateSize, void* pStartSpecification, void (*dataPageGenerator)(uint8_t* pageBase, size_t size))
{
#ifdef TARGET_APPLE
    vm_address_t addr, taddr;
    vm_prot_t prot, max_prot;
    kern_return_t ret;

    // Allocate two contiguous ranges of memory: the first range will contain the stubs
    // and the second range will contain their data.
    do
    {
        ret = vm_allocate(mach_task_self(), &addr, templateSize * 2, VM_FLAGS_ANYWHERE);
    } while (ret == KERN_ABORTED);

    if (ret != KERN_SUCCESS)
    {
        return NULL;
    }

    if (dataPageGenerator)
    {
        // Generate the data page before we map the code page into memory
        dataPageGenerator(((uint8_t*) addr) + templateSize, templateSize);
    }

    do
    {
        ret = vm_remap(
            mach_task_self(), &addr, templateSize, 0, VM_FLAGS_FIXED | VM_FLAGS_OVERWRITE,
            mach_task_self(), (vm_address_t)pTemplate, FALSE, &prot, &max_prot, VM_INHERIT_SHARE);
    } while (ret == KERN_ABORTED);

    if (ret != KERN_SUCCESS)
    {
        do
        {
            ret = vm_deallocate(mach_task_self(), addr, templateSize * 2);
        } while (ret == KERN_ABORTED);

        return NULL;
    }
    return (void*)addr;
#else
    TemplateThunkMappingData* pThunkData = __atomic_load_n(&s_pThunkData, __ATOMIC_ACQUIRE);
    if (s_pThunkData == NULL)
    {
        pThunkData = InitializeTemplateThunkMappingData(pTemplate);
    }

    if (pThunkData->addrOfStartOfSection == NULL)
    {
        // This is the detail of thunk data which indicates if we were able to compute the template mapping data
        return NULL;
    }

    if (pTemplate < pThunkData->addrOfStartOfSection)
    {
        return NULL;
    }

    uint8_t* endOfTemplate = ((uint8_t*)pTemplate + templateSize);
    if (endOfTemplate > pThunkData->addrOfEndOfSection)
        return NULL;

    size_t sectionOffset = (uint8_t*)pTemplate - (uint8_t*)pThunkData->addrOfStartOfSection;
    off_t fileOffset = pThunkData->offsetInFileOfStartOfSection + sectionOffset;

    void *pStart = mmap(pStartSpecification, templateSize * 2, PROT_READ | PROT_WRITE, MAP_ANONYMOUS | MAP_PRIVATE | (pStartSpecification != NULL ? MAP_FIXED : 0), -1, 0);
    if (pStart == MAP_FAILED)
    {
        return NULL;
    }

    if (dataPageGenerator)
    {
        // Generate the data page before we map the code page into memory
        dataPageGenerator(((uint8_t*) pStart) + templateSize, templateSize);
    }

    void *pStartCode = mmap(pStart, templateSize, PROT_READ | PROT_EXEC, MAP_PRIVATE | MAP_FIXED, pThunkData->fdImage, fileOffset);
    if (pStart != pStartCode)
    {
        munmap(pStart, templateSize * 2);
        return NULL;
    }

    return pStart;
#endif
}

bool VMToOSInterface::FreeThunksFromTemplate(void* thunks, size_t templateSize)
{
#ifdef TARGET_APPLE
    kern_return_t ret;

    do
    {
        ret = vm_deallocate(mach_task_self(), (vm_address_t)thunks, templateSize * 2);
    } while (ret == KERN_ABORTED);

    return ret == KERN_SUCCESS ? true : false;
#else
    munmap(thunks, templateSize * 2);
    return true;
#endif
}
