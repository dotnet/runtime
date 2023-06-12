project(utils C)

set(utils_win32_sources
    mono-os-semaphore-win32.c
    mono-os-wait-win32.c
    mono-windows-thread-name.c
    os-event-win32.c
    w32subset.h)

if(HOST_WIN32 AND HOST_AMD64)
    enable_language(ASM_MASM)
    set(CMAKE_ASM_MASM_COMPILE_OPTIONS_MSVC_RUNTIME_LIBRARY_MultiThreaded         "")
    set(CMAKE_ASM_MASM_COMPILE_OPTIONS_MSVC_RUNTIME_LIBRARY_MultiThreadedDLL      "")
    set(CMAKE_ASM_MASM_COMPILE_OPTIONS_MSVC_RUNTIME_LIBRARY_MultiThreadedDebug    "")
    set(CMAKE_ASM_MASM_COMPILE_OPTIONS_MSVC_RUNTIME_LIBRARY_MultiThreadedDebugDLL "")

    list(APPEND utils_win32_sources win64.asm)
elseif(HOST_WASI)
    set (CMAKE_ASM_COMPILER_VERSION "${CMAKE_C_COMPILER_VERSION}")
    set (CMAKE_ASM_COMPILER_TARGET "${CMAKE_C_COMPILER_TARGET}")
    enable_language(ASM)
endif()

set(utils_unix_sources
    dlmalloc.h
    dlmalloc.c
    os-event-unix.c)

if(HOST_WIN32)
    set(utils_platform_sources ${utils_win32_sources})
elseif(HOST_WASI)
    set(utils_platform_sources ${utils_unix_sources} mono-threads-wasi.S)
else()
    set(utils_platform_sources ${utils_unix_sources})
endif()

set(utils_common_sources
    mono-md5.c
    mono-sha1.c
    mono-logger.c
    mono-logger-internals.h
    mono-cgroup.c
    mono-codeman.c
    mono-counters.c
    mono-compiler.h
    mono-dl.c
    mono-dl-windows.c
    mono-dl-darwin.c
    mono-dl-posix.c
    mono-dl-wasm.c
    mono-dl.h
    mono-dl-windows-internals.h
    mono-log-windows.c
    mono-log-common.c
    mono-log-posix.c
    mono-log-android.c
    mono-log-darwin.c
    mono-log-flight-recorder.c
    mono-internal-hash.c
    mono-internal-hash.h
    monobitset.c
    mono-filemap.c
    mono-math-c.c
    mono-mmap.c
    mono-mmap-windows.c
    mono-mmap.h
    mono-mmap-internals.h
    mono-os-mutex.h
    mono-os-mutex.c
    mono-flight-recorder.h
    mono-flight-recorder.c
    mono-os-wait.h
    mono-coop-mutex.h
    mono-once.h
    mono-lazy-init.h
    mono-proclib.c
    mono-proclib.h
    mono-publib.c
    mono-string.h
    mono-time.c
    mono-time.h
    strenc-internals.h
    strenc.h
    strenc.c
    mono-uri.c
    mono-poll.c
    mono-path.c
    mono-os-semaphore.h
    mono-coop-semaphore.h
    mono-sigcontext.h
    mono-stdlib.c
    mono-property-hash.h
    mono-property-hash.c
    freebsd-dwarf.h
    dtrace.h
    mono-error.c
    mono-error-internals.h
    monobitset.h
    mono-codeman.h
    mono-digest.h
    mono-forward-internal.h
    mono-machine.h
    mono-math.h
    mono-membar.h
    mono-path.h
    mono-poll.h
    mono-uri.h
    mono-stdlib.h
    valgrind.h
    mach-support.h
    memcheck.h
    mono-context.c
    mono-context.h
    mono-stack-unwinding.h
    hazard-pointer.c
    hazard-pointer.h
    lifo-semaphore.c
    lifo-semaphore.h
    lock-free-queue.c
    lock-free-queue.h
    lock-free-alloc.c
    lock-free-alloc.h
    lock-free-array-queue.c
    lock-free-array-queue.h
    mono-linked-list-set.c
    mono-linked-list-set.h
    mono-threads.c
    mono-threads-state-machine.c
    mono-threads-posix.c
    mono-threads-posix-signals.c
    mono-threads-mach.c
    mono-threads-mach-helper.c
    mono-threads-windows.c
    mono-threads-linux.c
    mono-threads-freebsd.c
    mono-threads-netbsd.c
    mono-threads-openbsd.c
    mono-threads-android.c
    mono-threads-haiku.c
    mono-threads-aix.c
    mono-threads-wasm.h
    mono-threads-wasm.c
    mono-threads-sunos.c
    mono-threads.h
    mono-threads-debug.h
    mono-threads-api.h
    mono-threads-coop.c
    mono-threads-coop.h
    mono-utility-thread.c
    mono-utility-thread.h
    mono-tls.h
    mono-tls-inline.h
    mono-tls.c
    mono-utils-debug.c
    mono-utils-debug.h
    mono-memory-model.h
    atomic.h
    atomic.c
    mono-hwcap.h
    mono-hwcap.c
    mono-hwcap-vars.h
    bsearch.h
    bsearch.c
    mono-signal-handler.h
    mono-signal-handler.c
    mono-conc-hashtable.h
    mono-conc-hashtable.c
    json.h
    json.c
    mono-rand.c
    mono-rand-windows.c
    mono-rand.h
    memfuncs.c
    memfuncs.h
    parse.c
    parse.h
    checked-build.c
    checked-build.h
    os-event.h
    refcount.h
    w32api.h
    unlocked.h
    ward.h
    options.h
    options-def.h
    options.c
    ftnptr.h
    wasm-module-reader.h
    wasm-module-reader.c
  )

if(MONO_CROSS_COMPILE)
set(utils_arch_sources mach-support-unknown.c)
elseif(HOST_AMD64)
set(utils_arch_sources
    mach-support-amd64.c)
elseif(HOST_X86)
set(utils_arch_sources
    mach-support-x86.c)
elseif(HOST_ARM64)
set(utils_arch_sources
    mach-support-arm64.c)
elseif(HOST_ARM)
set(utils_arch_sources
    mach-support-arm.c)
else()
#message(FATAL_ERROR "")
endif()

if(MONO_CROSS_COMPILE)
set(utils_arch_sources "${utils_arch_sources};mono-hwcap-cross.c")
elseif(TARGET_AMD64 OR TARGET_X86)
set(utils_arch_sources "${utils_arch_sources};mono-hwcap-x86.c")
elseif(TARGET_ARM64)
set(utils_arch_sources "${utils_arch_sources};mono-hwcap-arm64.c")
elseif(TARGET_ARM)
set(utils_arch_sources "${utils_arch_sources};mono-hwcap-arm.c")
elseif(TARGET_RISCV64)
set(utils_arch_sources "${utils_arch_sources};mono-hwcap-riscv.c")
elseif(TARGET_S390X)
set(utils_arch_sources "${utils_arch_sources};mono-hwcap-s390x.c")
elseif(TARGET_WASM)
set(utils_arch_sources "${utils_arch_sources};mono-hwcap-wasm.c;mono-mmap-wasm.c")
elseif(TARGET_WASI)
set(utils_arch_sources "${utils_arch_sources};mono-hwcap-wasm.c")
elseif(TARGET_POWERPC OR TARGET_POWERPC64)
set(utils_arch_sources "${utils_arch_sources};mono-hwcap-ppc.c")
else()
  message(FATAL_ERROR "")
endif()

set(utils_sources "${utils_platform_sources};${utils_arch_sources};${utils_common_sources}")

set(utils_sources
    ${utils_sources}
    ${CLR_SRC_NATIVE_DIR}/external/libunwind/include/remote/freebsd-elf_common.h
    ${CLR_SRC_NATIVE_DIR}/external/libunwind/include/remote/freebsd-elf32.h
    ${CLR_SRC_NATIVE_DIR}/external/libunwind/include/remote/freebsd-elf64.h)

if(ENABLE_DTRACE)
    find_program(DTRACE dtrace)
    if(TARGET_OSX)
        set(dtrace_flags "-arch ${TARGET_ARCH}")
    else()
        if(TARGET_SIZEOF_VOID_P EQUAL 8)
            set(dtrace_flags "-64")
        elseif(TARGET_SIZEOF_VOID_P EQUAL 4)
            set(dtrace_flags "-32")
        endif()
    endif()

    add_custom_command(
        OUTPUT ${CMAKE_CURRENT_LIST_DIR}/mono-dtrace.h
        COMMAND ${DTRACE} -h -s "${CMAKE_CURRENT_LIST_DIR}/mono.d" -o "${CMAKE_CURRENT_LIST_DIR}/mono-dtrace.h"
        DEPENDS ${CMAKE_CURRENT_LIST_DIR}/mono.d
        VERBATIM
    )

    set(utils_sources "${utils_sources};${CMAKE_CURRENT_LIST_DIR}/mono-dtrace.h;../utils/dtrace.h")
endif()

add_library(utils_objects OBJECT ${utils_sources})
target_link_libraries(utils_objects PRIVATE monoapi eglib_api)
target_compile_definitions(utils_objects PRIVATE -DMONO_DLL_EXPORT)
target_include_directories(utils_objects PRIVATE ${PROJECT_BINARY_DIR}/../..
  ${PROJECT_SOURCE_DIR}/../..
  ${PROJECT_SOURCE_DIR}/..)

