# Transparent Huge Pages (THP) Implementation in .NET CoreCLR GC

## Overview

This document describes the implementation of Transparent Huge Pages (THP) support in the .NET CoreCLR Garbage Collector. THP allows the GC to use 2MB huge pages instead of standard 4KB pages for heap memory, potentially reducing TLB (Translation Lookaside Buffer) misses and improving memory access performance.


**Target Platform:** Linux x86_64 (also stubbed for Windows)  
**Runtime Version:** .NET 11.0.0

---

## System Requirements

### Linux Kernel Configuration

THP must be enabled in **madvise mode** on the system:

```bash
$ cat /sys/kernel/mm/transparent_hugepage/enabled
always [madvise] never
```
This shows the current THP policy (`always`, `madvise`, or `never`), with the active mode in brackets`[]`.  The `[madvise]` setting means applications must explicitly request THP via `madvise(MADV_HUGEPAGE)` system call.

THP can be enabled in the recommended `madvise` mode(This will not be part of the runtime. The GC merely checks if it is in this mode) using the following command:

`echo madvise | sudo tee /sys/kernel/mm/transparent_hugepage/enabled`


**Why madvise mode?**
- Applications opt-in to THP (prevents system-wide overhead)
- Kernel allocates 2MB huge pages only when requested

### Hardware Requirements

- **x86_64 architecture**: Native 2MB huge page support
- **CPU**: Modern processor with TLB support (all contemporary CPUs)

---

## GC Configuration

### Environment Variable

THP is controlled via the `DOTNET_GCTHP` environment variable:

```bash
# Enable THP
export DOTNET_GCTHP=1

# Disable THP (default)
export DOTNET_GCTHP=0
```

### Configuration Reading

**File:** [src/coreclr/gc/gcconfig.h](src/coreclr/gc/gcconfig.h)

```cpp
BOOL_CONFIG(DOTNET_GCTHP, ReadTHPEnabled, true, "Enable Transparent Huge Pages")
```

**Implementation:**
- Configuration key: `DOTNET_GCTHP`
- Default value: `false` (disabled by default)
- Read at GC initialization
- Stored in heap instance: `gc_heap::use_thp_p` member variable

**File:** [src/coreclr/gc/gcpriv.h](src/coreclr/gc/gcpriv.h)  
**Line:** 5370-5372

```cpp
class gc_heap
{
    BOOL use_thp_p;  // Whether THP is enabled for this heap
    // ...
};
```

---

## Implementation Architecture

### Memory Flow

The .NET GC uses a **two-phase memory model** on Unix systems: reserve address space first, then commit it on demand. This allows the GC to pre-allocate large virtual address ranges without consuming physical memory upfront. THP integrates into the commit phase by hinting to the kernel to use 2MB pages instead of standard 4KB pages when backing the committed memory with physical RAM.

**Phase 1 (Reserve):** The GC reserves a large contiguous virtual address range (e.g., 1GB for a heap segment) using `mmap(PROT_NONE)`. This creates entries in the process's page tables but does not allocate any physical memory. The reserved region is inaccessible—any attempted access triggers a segmentation fault.

**Phase 2 (Commit):** When the GC actually needs memory, it commits portions of the reserved range by calling `mprotect(PROT_READ|PROT_WRITE)` to make the region accessible. If THP is enabled (`DOTNET_GCTHP=1`) and the commit size is ≥2MB, the GC immediately follows with `madvise(MADV_HUGEPAGE)` to request that subsequent page faults use huge pages. The `madvise()` call is purely advisory—it tells the kernel "prefer 2MB pages for this range if possible," but the kernel makes the final allocation decision.

**Phase 3 (Page Fault / Physical Allocation):** Memory is not actually allocated until the application **writes** to a committed address. This triggers a page fault, and the kernel's page fault handler allocates the backing store:
- **Without THP:** Allocates one 4KB page per fault (512 faults needed for 2MB)
- **With THP (after `madvise()`):** Attempts to allocate a single 2MB huge page if the fault address is 2MB-aligned and contiguous memory is available. Falls back to 4KB pages if huge page allocation fails.

**Phase 4 (Decommit):** When the GC shrinks the heap or releases memory, it decommits regions by calling `mmap(PROT_NONE)` over the address range. This unmaps the physical memory and returns it to the OS, but keeps the virtual address reservation intact for future reuse. There is **no THP-specific cleanup**—the kernel automatically handles freeing huge pages the same way it handles normal pages.

**Key Insight:** THP operates **lazily**. The `madvise()` hint is given at commit time, but the actual huge page allocation happens later during page faults. This means you won't see `AnonHugePages` increase immediately after `VirtualCommitThp()` returns—only after the application has written to the committed memory.

**Memory Lifecycle Diagram:**

```
1. VirtualReserve()
   └─> mmap(PROT_NONE) - Reserve address space

2. VirtualCommit() / VirtualCommitThp()
   └─> mprotect(PROT_READ|PROT_WRITE) - Make memory accessible
   └─> [THP] madvise(MADV_HUGEPAGE) - Request huge pages

3. Page Fault
   └─> Kernel allocates 2MB huge page (if THP requested and feasible)

4. VirtualDecommit()
   └─> mmap(PROT_NONE) - Decommit memory (standard, no THP-specific cleanup)
```

### Core Implementation: VirtualCommitThp()

**File:** [src/coreclr/gc/unix/gcenv.unix.cpp](src/coreclr/gc/unix/gcenv.unix.cpp)  
**Lines:** 485-518

```cpp
bool GCToOSInterface::VirtualCommitThp(void* address, size_t size, uint16_t node)
{
    // First, perform standard commit (mprotect + NUMA binding)
    bool result = VirtualCommitInner(address, size, node, /* newMemory */ false);
    
    if (result)
    {
#ifdef MADV_HUGEPAGE
        // Only apply THP for allocations >= 2MB (one huge page)
        // Smaller allocations won't benefit and just add syscall overhead
        const size_t MIN_THP_SIZE = 2 * 1024 * 1024; // 2MB
        
        if (size >= MIN_THP_SIZE)
        {
            int rc = madvise(address, size, MADV_HUGEPAGE);
            if (rc == 0)
            {
                printf("THP: madvise(MADV_HUGEPAGE) succeeded for %p, size=%zu MB\n", 
                       address, size / (1024 * 1024));
            }
            else
            {
                printf("THP: madvise(MADV_HUGEPAGE) failed for %p, errno=%d\n", 
                       address, errno);
            }
        }
        else
        {
            printf("THP: Skipping madvise for small allocation %p, size=%zu KB (< 2MB threshold)\n", 
                   address, size / 1024);
        }
#endif
    }
    return result;
}
```

**Key Design Decisions:**

1. **2MB Minimum Threshold:**
   - Only allocations >= 2MB trigger `madvise(MADV_HUGEPAGE)`
   - Rationale: Huge pages are 2MB; smaller allocations cannot benefit
   - Avoids syscall overhead for small GC bookkeeping structures

2. **Syscall Order:**
   ```
   mprotect(PROT_READ|PROT_WRITE)  // Make memory accessible
   ↓
   mbind()                          // NUMA binding (optional)
   ↓
   madvise(MADV_HUGEPAGE)          // Request THP
   ```
   - `madvise()` must happen AFTER `mprotect()` because THP operates on accessible memory
   - Kernel allocates huge pages on subsequent page faults

3. **Windows Stub:**  
   **File:** [src/coreclr/gc/windows/gcenv.windows.cpp](src/coreclr/gc/windows/gcenv.windows.cpp)  
   **Lines:** 759-767
   ```cpp
   bool GCToOSInterface::VirtualCommitThp(void* address, size_t size, uint16_t node)
   {
       // Windows Large Pages require different mechanism (not implemented)
       return VirtualCommit(address, size, node);
   }
   ```

---

## Where THP Is Used

### GC Commit Sites

All memory commits go through `virtual_commit()` in [gc.cpp](src/coreclr/gc/gc.cpp), which routes to either `VirtualCommit()` or `VirtualCommitThp()` based on `use_thp_p`.

**Total Commit Sites:** 6 locations

#### 1. Heap Segment Creation (Primary THP Target)
**File:** [gc.cpp](src/coreclr/gc/gc.cpp)  
**Line:** ~12280  
**Context:** Creating new GC heap segments

```cpp
// Allocate a new heap segment (typically 4MB in regions mode)
void* mem = virtual_commit(start, size, heap_number);
```


#### 2. Segment Growth
**File:** [gc.cpp](src/coreclr/gc/gc.cpp)  
**Line:** ~15663  
**Context:** Growing existing segments

```cpp
// Grow segment by additional memory
void* mem = virtual_commit(current_end, growth_size, heap_number);
```


#### 3. Card Table Commits
**File:** [gc.cpp](src/coreclr/gc/gc.cpp)  
**Lines:** ~9501, ~9578, ~9754  
**Context:** Allocating GC card tables for write barriers

```cpp
// Commit card table memory
void* cards = virtual_commit(card_table_start, card_size, heap_number);
```


#### 4. Mark Array Commits
**File:** [gc.cpp](src/coreclr/gc/gc.cpp)  
**Line:** ~38383  
**Context:** Allocating mark bits for GC tracing

```cpp
// Commit mark array
void* mark_array = virtual_commit(mark_start, mark_size, heap_number);
```


#### 5. Pinned Object Heap (POH) Special Path
**File:** [gc.cpp](src/coreclr/gc/gc.cpp)  
**Lines:** 5689-5700  
**Context:** Pinned heap allocation (objects that never move)

```cpp
if (use_thp_p && (size >= MIN_THP_SIZE))
{
    mem = GCToOSInterface::VirtualCommitThp(start, size, heap_number);
}
```


#### 6. Bookkeeping Commits (via virtual_alloc_commit_for_heap)
**File:** [gc.cpp](src/coreclr/gc/gc.cpp)  
**Lines:** 7440-7460, 7542-7563  
**Context:** Wrapper for all heap-related commits

```cpp
inline void* virtual_alloc_commit_for_heap(void* addr, size_t size, int h_number)
{
    if (gc_heap::use_thp_p)
        return GCToOSInterface::VirtualCommitThp(addr, size, node);
    else
        return GCToOSInterface::VirtualCommit(addr, size, node);
}
```
