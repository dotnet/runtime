// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Native test of the LXR write barrier assembly (src/coreclr/gc/amd64/lxr_writebarrier.S and .asm).
//
// The test does what the runtime does in InitJITWriteBarrierHelpers: it copies the barrier block to a new executable
// page, fills the patchable slots from their source globals, and calls the copy. A side-metadata array and a small
// fake heap stand in for LXR. The slow path is a test function (the "C++ log function") that records every call.
//
// Every check prints PASS: <name> or FAIL: <name>. A control is a define that breaks the behavior; the build script
// builds the program with each control and requires that the named check fails:
//   assembly: LXR_WB_CONTROL_NO_VECTOR_SAVE -> vector,  LXR_WB_CONTROL_MISALIGN -> alignment,
//             LXR_WB_CONTROL_NO_STORE_AFTER_SLOW -> store-after-slow
//   this file: LXR_WBTEST_CONTROL_READ_OLD_AFTER_CLAIM -> old-value-race,  LXR_WBTEST_CONTROL_NO_PATCH -> patch

#include "lxr_write_barrier.h"

#include <atomic>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <thread>
#include <vector>

#if defined(_MSC_VER)
#include <intrin.h>
#include <immintrin.h>
#include <windows.h>
#else
#include <cpuid.h>
#include <immintrin.h>
#include <csignal>
#include <csetjmp>
#include <sys/mman.h>
#include <ucontext.h>
#include <unistd.h>
#endif

static int g_failures = 0;

static void Check(bool ok, const char* name, const char* detail = "")
{
    if (!ok)
    {
        g_failures++;
    }
    std::printf("%s: %s%s%s\n", ok ? "PASS" : "FAIL", name, detail[0] != 0 ? " - " : "", detail);
    std::fflush(stdout);
}

// ---------------------------------------------------------------------------------------------------------------
// The fake heap and its side metadata (one bit for every 8-byte slot, a set bit means "not logged yet").

static const size_t HeapBytes = 1 << 20;
static uint8_t* g_heap;
static uint8_t* g_metadataStorage;
static uint8_t* g_metadataStorage2;
static const size_t SlotCount = HeapBytes / 8;

static uintptr_t BiasedBase(uint8_t* storage, uint8_t* heap)
{
    return reinterpret_cast<uintptr_t>(storage) - (reinterpret_cast<uintptr_t>(heap) >> 6);
}

static std::atomic<uint8_t>* MetadataByte(uint8_t* storage, void* slot)
{
    uintptr_t offset = reinterpret_cast<uintptr_t>(slot) - reinterpret_cast<uintptr_t>(g_heap);
    return reinterpret_cast<std::atomic<uint8_t>*>(storage + (offset >> 6));
}

static unsigned MetadataBit(void* slot)
{
    return static_cast<unsigned>((reinterpret_cast<uintptr_t>(slot) >> 3) & 7);
}

static void SetUnlogged(void** slot, uint8_t* storage)
{
    MetadataByte(storage, slot)->fetch_or(static_cast<uint8_t>(1u << MetadataBit(slot)));
}

static bool IsUnlogged(void** slot, uint8_t* storage)
{
    return ((MetadataByte(storage, slot)->load() >> MetadataBit(slot)) & 1) != 0;
}

// ---------------------------------------------------------------------------------------------------------------
// The test slow path: what LXR's LxrBarrierSlowPath does, plus recording and optional damage.

struct LogEntry
{
    void** slot;
    void* old;
};

static const size_t MaxLog = 1 << 16;
static LogEntry* g_log;
static std::atomic<size_t> g_logCount;
static std::atomic<size_t> g_slowCalls;
static std::atomic<size_t> g_misaligned;
static std::atomic<bool> g_clobber;
static uint8_t* g_activeStorage;

#if defined(_MSC_VER)
#define NOINLINE __declspec(noinline)
#define FRAME_ADDRESS() (reinterpret_cast<uintptr_t>(_AddressOfReturnAddress()) - 8)
#else
#define NOINLINE __attribute__((noinline))
#define FRAME_ADDRESS() (reinterpret_cast<uintptr_t>(__builtin_frame_address(0)))
#endif

#if !defined(_MSC_VER)
__attribute__((target("avx"))) static void ClobberAllVector()
{
    _mm256_zeroall();
}
#else
static void ClobberAllVector()
{
    _mm256_zeroall();
}
#endif

static bool CpuHasAvx()
{
    uint64_t mask = LxrWriteBarrierComputeXsaveMask();
    return (mask & 4) != 0;
}

// The slow path must be entered with the stack aligned as at the entry of any C function (rsp + 8 is a multiple of
// 16, so the frame address is). A frame pointer is forced so that the address is exact.
extern "C" NOINLINE
#if !defined(_MSC_VER)
__attribute__((optnone))
#endif
void TestBarrierSlowPath(void** slot)
{
    g_slowCalls.fetch_add(1);
    if ((FRAME_ADDRESS() & 15) != 0)
    {
        g_misaligned.fetch_add(1);
    }

    // Read the old value BEFORE the claim (see LxrGcHeap::LogOldValue).
    void* old = *reinterpret_cast<void* volatile*>(slot);
    std::atomic<uint8_t>* byte = MetadataByte(g_activeStorage, slot);
    uint8_t mask = static_cast<uint8_t>(1u << MetadataBit(slot));
    uint8_t expected = byte->load();
    bool claimed = false;
    while ((expected & mask) != 0)
    {
        if (byte->compare_exchange_weak(expected, static_cast<uint8_t>(expected & ~mask)))
        {
            claimed = true;
            break;
        }
    }
#ifdef LXR_WBTEST_CONTROL_READ_OLD_AFTER_CLAIM
    if (claimed)
    {
        for (volatile int spin = 0; spin < 20000; spin++)
        {
        }
        old = *reinterpret_cast<void* volatile*>(slot);
    }
#endif
    if (claimed)
    {
        size_t index = g_logCount.fetch_add(1);
        if (index < MaxLog)
        {
            g_log[index].slot = slot;
            g_log[index].old = old;
        }
    }

    if (g_clobber.load() && CpuHasAvx())
    {
        ClobberAllVector();
    }
}

// ---------------------------------------------------------------------------------------------------------------
// Installing a copy of the block (what the runtime does) and patching its slots.

struct Installed
{
    uint8_t* copy;
    uint8_t* original;
    size_t size;
    void (*assignRef)(void**, void*);
    void (*checkedAssignRef)(void**, void*);
    LxrWriteBarrierBlockInfo info;
};

static uint8_t* AllocExecutable(size_t size)
{
#if defined(_MSC_VER)
    return static_cast<uint8_t*>(VirtualAlloc(nullptr, size, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE));
#else
    void* p = mmap(nullptr, size, PROT_READ | PROT_WRITE | PROT_EXEC, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
    return p == MAP_FAILED ? nullptr : static_cast<uint8_t*>(p);
#endif
}

static void Install(Installed* result, size_t offsetInPage)
{
    LxrWriteBarrierGetBlockInfo(&result->info);
    uint8_t* start = reinterpret_cast<uint8_t*>(result->info.checked_assign_ref);
    uint8_t* assign = reinterpret_cast<uint8_t*>(result->info.assign_ref);
    size_t size = static_cast<size_t>(&LxrWriteBarrierBlock_End - start);
    uint8_t* page = AllocExecutable(8192);
    result->copy = page + offsetInPage;
    result->original = start;
    result->size = size;
    std::memcpy(result->copy, start, size);
    result->checkedAssignRef = reinterpret_cast<void (*)(void**, void*)>(result->copy);
    result->assignRef = reinterpret_cast<void (*)(void**, void*)>(result->copy + (assign - start));
}

// The same loop as UpdateCustomWriteBarrierGlobals in the runtime.
static void UpdatePatchableSlots(Installed* installed)
{
    for (LxrWriteBarrierPatchable* p = installed->info.patchable; p->slot != nullptr; p++)
    {
        uintptr_t* slot = reinterpret_cast<uintptr_t*>(
            installed->copy + (reinterpret_cast<uint8_t*>(p->slot) - installed->original));
        *slot = *reinterpret_cast<uintptr_t*>(p->source);
    }
}

static void SetGlobals(uint8_t* storage, size_t heapOffset, size_t heapSize)
{
    g_activeStorage = storage;
    LxrWriteBarrierSetGlobals(
        BiasedBase(storage, g_heap),
        reinterpret_cast<uintptr_t>(g_heap) + heapOffset,
        heapSize,
        reinterpret_cast<void*>(&TestBarrierSlowPath),
        LxrWriteBarrierComputeXsaveMask());
}

static void ResetLog()
{
    g_logCount = 0;
    g_slowCalls = 0;
    g_misaligned = 0;
}

static void** Slot(size_t index)
{
    return reinterpret_cast<void**>(g_heap) + index;
}

// ---------------------------------------------------------------------------------------------------------------
// Vector state

// Offsets and sizes of the xsave components in the standard (non-compacted) format; these are architectural.
static uint32_t ComponentOffset(uint32_t component)
{
    static const uint32_t offsets[8] = {0, 160, 576, 0, 0, 1088, 1152, 1664};
    return offsets[component];
}

static uint32_t ComponentSize(uint32_t component)
{
    static const uint32_t sizes[8] = {0, 0, 256, 0, 0, 64, 512, 1024};
    return sizes[component];
}

alignas(64) static uint8_t g_stateIn[8192];
alignas(64) static uint8_t g_stateOut[8192];

// Load the prepared state into the registers, call the helper, read the registers back. Nothing in between uses a
// vector register.
#if !defined(_MSC_VER)
__attribute__((target("xsave,avx"), noinline, optnone))
#else
#pragma optimize("", off)
__declspec(noinline)
#endif
static void CallWithVectorState(void (*barrier)(void**, void*), void** dst, void* ref, uint64_t mask)
{
    if (mask != 0)
    {
        _xrstor64(g_stateIn, mask);
        barrier(dst, ref);
        _xsave64(g_stateOut, mask);
    }
    else
    {
        _fxrstor64(g_stateIn);
        barrier(dst, ref);
        _fxsave64(g_stateOut);
    }
}
#if defined(_MSC_VER)
#pragma optimize("", on)
#endif

// Returns the number of differing bytes in the register areas of the state.
static size_t VectorStateDifference(uint64_t mask)
{
    size_t differences = 0;
    // legacy area: xmm0-15 at 160..415
    for (size_t i = 160; i < 416; i++)
    {
        differences += g_stateIn[i] != g_stateOut[i];
    }
    for (uint32_t component = 2; component < 8; component++)
    {
        if ((mask & (1ull << component)) == 0)
        {
            continue;
        }
        uint32_t offset = ComponentOffset(component);
        uint32_t size = ComponentSize(component);
        for (uint32_t i = 0; i < size; i++)
        {
            differences += g_stateIn[offset + i] != g_stateOut[offset + i];
        }
    }
    return differences;
}

#if !defined(_MSC_VER)
__attribute__((target("xsave,avx"), noinline, optnone))
#else
__declspec(noinline)
#endif
static void PrepareVectorState(uint64_t mask)
{
    std::memset(g_stateIn, 0, sizeof(g_stateIn));
    if (mask != 0)
    {
        _xsave64(g_stateIn, mask);
    }
    else
    {
        _fxsave64(g_stateIn);
    }
    uint8_t pattern = 0x11;
    for (size_t i = 160; i < 416; i++)
    {
        g_stateIn[i] = pattern++;
    }
    uint64_t bits = 0x3;
    for (uint32_t component = 2; component < 8; component++)
    {
        if ((mask & (1ull << component)) == 0)
        {
            continue;
        }
        uint32_t offset = ComponentOffset(component);
        uint32_t size = ComponentSize(component);
        for (uint32_t i = 0; i < size; i++)
        {
            g_stateIn[offset + i] = static_cast<uint8_t>(0xA0 + component * 7 + i * 3);
        }
        bits |= 1ull << component;
    }
    if (mask != 0)
    {
        // XSTATE_BV: all saved components are in use, so xrstor loads them.
        std::memcpy(g_stateIn + 512, &bits, sizeof(bits));
    }
}

// ---------------------------------------------------------------------------------------------------------------
// Fault handling: a null destination must fault at one of the listed places.

#if !defined(_MSC_VER)
static sigjmp_buf g_jump;
static uintptr_t g_faultIp;
static void FaultHandler(int, siginfo_t*, void* context)
{
    g_faultIp = static_cast<uintptr_t>(static_cast<ucontext_t*>(context)->uc_mcontext.gregs[REG_RIP]);
    siglongjmp(g_jump, 1);
}

static bool CallAndCatchFault(void (*barrier)(void**, void*), void** dst, void* ref, uintptr_t* faultIp)
{
    struct sigaction action = {};
    action.sa_sigaction = FaultHandler;
    action.sa_flags = SA_SIGINFO | SA_NODEFER;
    sigemptyset(&action.sa_mask);
    struct sigaction previous;
    sigaction(SIGSEGV, &action, &previous);
    bool faulted = false;
    if (sigsetjmp(g_jump, 1) == 0)
    {
        barrier(dst, ref);
    }
    else
    {
        faulted = true;
        *faultIp = g_faultIp;
    }
    sigaction(SIGSEGV, &previous, nullptr);
    return faulted;
}
#else
static LONG FaultFilter(EXCEPTION_POINTERS* pointers, uintptr_t* faultIp)
{
    *faultIp = static_cast<uintptr_t>(pointers->ContextRecord->Rip);
    return EXCEPTION_EXECUTE_HANDLER;
}

static bool CallAndCatchFault(void (*barrier)(void**, void*), void** dst, void* ref, uintptr_t* faultIp)
{
    __try
    {
        barrier(dst, ref);
    }
    __except (FaultFilter(GetExceptionInformation(), faultIp))
    {
        return true;
    }
    return false;
}
#endif

// ---------------------------------------------------------------------------------------------------------------

static void TestBasic(Installed& b)
{
    g_clobber = false;
    SetGlobals(g_metadataStorage, 0, HeapBytes);
    UpdatePatchableSlots(&b);

    void** slot = Slot(100);
    void* v1 = reinterpret_cast<void*>(0x1111);
    void* v2 = reinterpret_cast<void*>(0x2222);
    void* v3 = reinterpret_cast<void*>(0x3333);

    // Logged field: no call, store happens.
    ResetLog();
    *slot = nullptr;
    std::memset(g_metadataStorage, 0, HeapBytes >> 6);
    b.assignRef(slot, v1);
    Check(g_slowCalls == 0 && *slot == v1, "fast-path", "a logged field is stored without the slow path");

    // Unlogged field: the slow path runs once, the old value is logged, the store happens.
    ResetLog();
    *slot = v1;
    SetUnlogged(slot, g_metadataStorage);
    b.assignRef(slot, v2);
    Check(g_slowCalls == 1 && g_logCount == 1 && g_log[0].slot == slot && g_log[0].old == v1,
        "slow-once", "an unlogged field logs (slot, old value) once");
    Check(*slot == v2, "store-after-slow", "the store happens after the slow path");
    b.assignRef(slot, v3);
    Check(g_slowCalls == 1 && *slot == v3, "once-per-epoch", "the second store in the epoch does not log");

    // Null old value is logged as null.
    ResetLog();
    void** slot2 = Slot(200);
    *slot2 = nullptr;
    SetUnlogged(slot2, g_metadataStorage);
    b.assignRef(slot2, v1);
    Check(g_logCount == 1 && g_log[0].old == nullptr && *slot2 == v1, "null-old-value");

    // A store of null into an unlogged field also logs the old value.
    ResetLog();
    SetUnlogged(slot2, g_metadataStorage);
    b.assignRef(slot2, nullptr);
    Check(g_logCount == 1 && g_log[0].old == v1 && *slot2 == nullptr, "null-new-value");

    // Neighbors in the same metadata byte are independent.
    ResetLog();
    void** a = Slot(8);
    void** c = Slot(15);
    *a = v1;
    *c = v2;
    SetUnlogged(a, g_metadataStorage);
    SetUnlogged(c, g_metadataStorage);
    b.assignRef(a, v3);
    Check(g_logCount == 1 && g_log[0].slot == a && IsUnlogged(c, g_metadataStorage), "bit-position");
    b.assignRef(c, v3);
    Check(g_logCount == 2 && g_log[1].slot == c && g_log[1].old == v2, "bit-position-2");

    // Checked variant: outside the heap only the store happens, inside it behaves like the plain one.
    ResetLog();
    void* local[2] = {v1, v1};
    b.checkedAssignRef(reinterpret_cast<void**>(&local[1]), v2);
    Check(g_slowCalls == 0 && local[1] == v2, "checked-outside", "the checked helper stores outside the heap without a check");
    *slot = v1;
    SetUnlogged(slot, g_metadataStorage);
    b.checkedAssignRef(slot, v2);
    Check(g_logCount == 1 && g_log[0].old == v1 && *slot == v2, "checked-inside");
    void** last = Slot(SlotCount - 1);
    *last = v1;
    SetUnlogged(last, g_metadataStorage);
    b.checkedAssignRef(last, v2);
    Check(g_logCount == 2 && *last == v2, "checked-last-slot");
}

static void TestVector(Installed& b)
{
    SetGlobals(g_metadataStorage, 0, HeapBytes);
    UpdatePatchableSlots(&b);
    uint64_t mask = static_cast<uint64_t>(LxrWriteBarrierComputeXsaveMask());
    bool anyDifference = false;
    for (int clobber = 0; clobber < 2; clobber++)
    {
        g_clobber = clobber != 0 && CpuHasAvx();
        void** slot = Slot(300 + clobber);
        *slot = reinterpret_cast<void*>(0x77);
        SetUnlogged(slot, g_metadataStorage);
        ResetLog();
        PrepareVectorState(mask);
        std::memset(g_stateOut, 0, sizeof(g_stateOut));
        CallWithVectorState(b.assignRef, slot, reinterpret_cast<void*>(0x88), mask);
        if (g_slowCalls != 1)
        {
            Check(false, "vector", "the slow path did not run");
            return;
        }
        anyDifference |= VectorStateDifference(mask) != 0;
    }
    g_clobber = false;
    char detail[128];
    std::snprintf(detail, sizeof(detail), "xsave mask 0x%llx, the slow path %s the vector registers",
        static_cast<unsigned long long>(mask), CpuHasAvx() ? "clobbers (vzeroall)" : "does not clobber");
    Check(!anyDifference, "vector", detail);

    // The fast path must leave the vector state alone too.
    void** slot = Slot(310);
    *slot = nullptr;
    PrepareVectorState(mask);
    std::memset(g_stateOut, 0, sizeof(g_stateOut));
    CallWithVectorState(b.assignRef, slot, reinterpret_cast<void*>(0x99), mask);
    Check(VectorStateDifference(mask) == 0, "vector-fast-path");
}

static void TestAlignment(Installed& b)
{
    SetGlobals(g_metadataStorage, 0, HeapBytes);
    UpdatePatchableSlots(&b);
    ResetLog();
    for (size_t i = 0; i < 16; i++)
    {
        void** slot = Slot(400 + i);
        *slot = nullptr;
        SetUnlogged(slot, g_metadataStorage);
        b.assignRef(slot, reinterpret_cast<void*>(0x55));
    }
    Check(g_slowCalls == 16 && g_misaligned == 0, "alignment", "the C++ slow path is entered with an aligned stack");
}

static void TestFault(Installed& b)
{
    SetGlobals(g_metadataStorage, 0, HeapBytes);
    UpdatePatchableSlots(&b);
    uintptr_t fault;
    bool ok = true;
    char detail[160] = "";
    for (int variant = 0; variant < 2; variant++)
    {
        fault = 0;
        void (*fn)(void**, void*) = variant == 0 ? b.assignRef : b.checkedAssignRef;
        bool faulted = CallAndCatchFault(fn, reinterpret_cast<void**>(0x10), reinterpret_cast<void*>(0x1), &fault);
        bool listed = false;
        for (uintptr_t* av = b.info.av_locations; *av != 0; av++)
        {
            listed |= (b.copy + (*av - reinterpret_cast<uintptr_t>(b.original))) == reinterpret_cast<uint8_t*>(fault);
        }
        if (!faulted || !listed)
        {
            ok = false;
            std::snprintf(detail, sizeof(detail), "variant %d faulted=%d ip=%p listed=%d", variant, faulted,
                reinterpret_cast<void*>(fault), listed);
        }
    }
    Check(ok, "av-location", detail);
}

static void RunThreads(int count, void (*body)(int, void*), void* context)
{
    std::atomic<int> ready{0};
    std::atomic<bool> go{false};
    std::vector<std::thread> threads;
    for (int t = 0; t < count; t++)
    {
        threads.emplace_back([&, t]() {
            ready.fetch_add(1);
            while (!go.load())
            {
            }
            body(t, context);
        });
    }
    while (ready.load() != count)
    {
    }
    go = true;
    for (std::thread& thread : threads)
    {
        thread.join();
    }
}

struct RaceContext
{
    Installed* barrier;
    size_t first;
    size_t slots;
    int epoch;
};

static void RaceBody(int thread, void* context)
{
    RaceContext* race = static_cast<RaceContext*>(context);
    void* value = reinterpret_cast<void*>((static_cast<uintptr_t>(thread + 1) << 32) | static_cast<uintptr_t>(race->epoch));
    for (size_t i = 0; i < race->slots; i++)
    {
        race->barrier->assignRef(Slot(race->first + i), value);
    }
}

static void TestConcurrent(Installed& b)
{
    SetGlobals(g_metadataStorage, 0, HeapBytes);
    UpdatePatchableSlots(&b);
    g_clobber = false;
    const int Threads = 8;

    // 1. Many threads store into ONE field, many epochs. Exactly one log per epoch, with the value the field had at
    // the start of the epoch.
    bool oneFieldOk = true;
    char detail[160] = "";
    const int Epochs = 2000;
    for (int epoch = 1; epoch <= Epochs && oneFieldOk; epoch++)
    {
        void** slot = Slot(1000);
        void* start = reinterpret_cast<void*>(0xAB000000ull + static_cast<unsigned>(epoch));
        *slot = start;
        SetUnlogged(slot, g_metadataStorage);
        ResetLog();
        RaceContext context{&b, 1000, 1, epoch};
        RunThreads(Threads, RaceBody, &context);
        if (g_logCount != 1 || g_log[0].slot != slot || g_log[0].old != start)
        {
            oneFieldOk = false;
            std::snprintf(detail, sizeof(detail), "epoch %d: %zu logs, old %p (start %p)", epoch,
                static_cast<size_t>(g_logCount), g_logCount != 0 ? g_log[0].old : nullptr, start);
        }
    }
    Check(oneFieldOk, "old-value-race", detail[0] != 0 ? detail : "one log per epoch for a field, old value is the start value");

    // 2. Many threads store into the same set of different fields.
    const size_t Fields = 4096;
    bool manyOk = true;
    detail[0] = 0;
    for (int epoch = 1; epoch <= 20 && manyOk; epoch++)
    {
        for (size_t i = 0; i < Fields; i++)
        {
            void** slot = Slot(2000 + i);
            *slot = reinterpret_cast<void*>(0xCD000000ull + (static_cast<uint64_t>(epoch) << 16) + i);
            SetUnlogged(slot, g_metadataStorage);
        }
        ResetLog();
        RaceContext context{&b, 2000, Fields, epoch};
        RunThreads(Threads, RaceBody, &context);
        std::vector<int> seen(Fields, 0);
        for (size_t i = 0; i < g_logCount && i < MaxLog; i++)
        {
            size_t index = static_cast<size_t>(g_log[i].slot - Slot(2000));
            if (index >= Fields || ++seen[index] != 1 ||
                g_log[i].old != reinterpret_cast<void*>(0xCD000000ull + (static_cast<uint64_t>(epoch) << 16) + index))
            {
                manyOk = false;
                std::snprintf(detail, sizeof(detail), "epoch %d field %zu", epoch, index);
                break;
            }
        }
        manyOk &= g_logCount == Fields;
        for (size_t i = 0; i < Fields && manyOk; i++)
        {
            uintptr_t final = reinterpret_cast<uintptr_t>(*Slot(2000 + i));
            manyOk &= (final & 0xFFFFFFFFull) == static_cast<uintptr_t>(epoch) && (final >> 32) >= 1 && (final >> 32) <= static_cast<uintptr_t>(Threads);
        }
    }
    Check(manyOk, "concurrent-fields", detail[0] != 0 ? detail : "every field logged once, all stores landed");
}

static void TestPatchAndRelocation()
{
    Installed first;
    Installed second;
    Install(&first, 0);
    Install(&second, 2048 + 16);
    g_clobber = false;
    void** slot = Slot(5000);
    void* v1 = reinterpret_cast<void*>(0x4141);
    void* v2 = reinterpret_cast<void*>(0x4242);

    // Alignment of the slots in the copy.
    bool aligned = true;
    for (LxrWriteBarrierPatchable* p = first.info.patchable; p->slot != nullptr; p++)
    {
        aligned &= (reinterpret_cast<uintptr_t>(first.copy + (reinterpret_cast<uint8_t*>(p->slot) - first.original)) & 7) == 0;
        aligned &= (reinterpret_cast<uintptr_t>(second.copy + (reinterpret_cast<uint8_t*>(p->slot) - second.original)) & 7) == 0;
    }
    Check(aligned, "slot-alignment", "every patchable slot is 8-byte aligned in a copy");

    // Before the first update the slots are zero: the heap has size zero, so the checked helper stores only.
    SetGlobals(g_metadataStorage, 0, HeapBytes);
    std::memset(g_metadataStorage, 0, HeapBytes >> 6);
    std::memset(g_metadataStorage2, 0, HeapBytes >> 6);
    ResetLog();
    *slot = v1;
    SetUnlogged(slot, g_metadataStorage);
    first.checkedAssignRef(slot, v2);
    bool unpatched = g_slowCalls == 0 && *slot == v2;

#ifndef LXR_WBTEST_CONTROL_NO_PATCH
    UpdatePatchableSlots(&first);
    UpdatePatchableSlots(&second);
#endif
    // After the update the same call logs.
    ResetLog();
    *slot = v1;
    SetUnlogged(slot, g_metadataStorage);
    first.checkedAssignRef(slot, v2);
    bool patched = g_logCount == 1 && g_log[0].old == v1 && *slot == v2;
    Check(unpatched && patched, "patch", "slots start empty and take the values of their sources when updated");

    // A second copy at another address works the same (relocation).
    ResetLog();
    *slot = v1;
    SetUnlogged(slot, g_metadataStorage);
    second.assignRef(slot, v2);
    bool relocated = g_logCount == 1 && g_log[0].old == v1 && *slot == v2;
    ResetLog();
    *slot = v1;
    SetUnlogged(slot, g_metadataStorage);
    second.checkedAssignRef(slot, v2);
    relocated &= g_logCount == 1 && *slot == v2;
    Check(relocated, "relocation", "a copy at another address works");

    // The metadata base changes: the new base is used after the next update, not before.
    *slot = v1;
    SetUnlogged(slot, g_metadataStorage);
    g_activeStorage = g_metadataStorage;
    LxrWriteBarrierSetGlobals(BiasedBase(g_metadataStorage2, g_heap), reinterpret_cast<uintptr_t>(g_heap), HeapBytes,
        reinterpret_cast<void*>(&TestBarrierSlowPath), LxrWriteBarrierComputeXsaveMask());
    ResetLog();
    first.assignRef(slot, v2);  // the slot still holds the old base: the bit is set there, so this logs
    bool before = g_slowCalls == 1;
    UpdatePatchableSlots(&first);
    g_activeStorage = g_metadataStorage2;
    SetUnlogged(slot, g_metadataStorage);  // set in the old array only
    ResetLog();
    *slot = v1;
    first.assignRef(slot, v2);  // new base: the bit is clear there, so no call
    bool after = g_slowCalls == 0 && *slot == v2;
    SetUnlogged(slot, g_metadataStorage2);
    ResetLog();
    first.assignRef(slot, v1);
    after &= g_slowCalls == 1;
    Check(before && after, "patch-metadata-base", "a new metadata base is used after the slot update");

    // The heap bounds change: a destination outside the new bounds is not checked any more.
    SetGlobals(g_metadataStorage2, HeapBytes / 2, HeapBytes / 2);
    UpdatePatchableSlots(&first);
    ResetLog();
    void** low = Slot(10);
    *low = v1;
    SetUnlogged(low, g_metadataStorage2);
    first.checkedAssignRef(low, v2);
    bool outside = g_slowCalls == 0 && *low == v2;
    void** high = Slot(SlotCount - 8);
    *high = v1;
    SetUnlogged(high, g_metadataStorage2);
    first.checkedAssignRef(high, v2);
    Check(outside && g_slowCalls == 1, "patch-heap-bounds", "the heap bounds of the checked helper follow their sources");
}

int main()
{
    uint8_t* raw = AllocExecutable(HeapBytes + 8192);
    g_heap = reinterpret_cast<uint8_t*>((reinterpret_cast<uintptr_t>(raw) + 4095) & ~uintptr_t(4095));
    g_metadataStorage = static_cast<uint8_t*>(std::calloc((HeapBytes >> 6) + 64, 1));
    g_metadataStorage2 = static_cast<uint8_t*>(std::calloc((HeapBytes >> 6) + 64, 1));
    g_log = static_cast<LogEntry*>(std::calloc(MaxLog, sizeof(LogEntry)));

    Installed barrier;
    Install(&barrier, 0);

    TestBasic(barrier);
    TestAlignment(barrier);
    TestVector(barrier);
    TestFault(barrier);
    TestConcurrent(barrier);
    TestPatchAndRelocation();

    std::printf("%s: lxr write barrier test, %d check failures\n", g_failures == 0 ? "PASS" : "FAIL", g_failures);
    return g_failures == 0 ? 0 : 1;
}
