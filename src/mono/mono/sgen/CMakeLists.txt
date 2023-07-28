project (sgen C)

if(NOT MONO_GC STREQUAL "sgen")
    return()
endif()

set(sgen_sources_base
    gc-internal-agnostic.h
    sgen-alloc.c
    sgen-archdep.h
    sgen-cardtable.c
    sgen-cardtable.h
    sgen-client.h
    sgen-conf.h
    sgen-copy-object.h
    sgen-debug.c
    sgen-descriptor.c
    sgen-descriptor.h
    sgen-fin-weak-hash.c
    sgen-gc.c
    sgen-gc.h
    sgen-gchandles.c
    sgen-gray.c
    sgen-gray.h
    sgen-hash-table.c
    sgen-hash-table.h
    sgen-internal.c
    sgen-layout-stats.c
    sgen-layout-stats.h
    sgen-los.c
    sgen-major-copy-object.h
    sgen-marksweep-drain-gray-stack.h
    sgen-marksweep.c
    sgen-memory-governor.c
    sgen-memory-governor.h
    sgen-minor-copy-object.h
    sgen-minor-scan-object.h
    sgen-nursery-allocator.c
    sgen-pinning-stats.c
    sgen-pinning.c
    sgen-pinning.h
    sgen-pointer-queue.c
    sgen-pointer-queue.h
    sgen-array-list.h
    sgen-array-list.c
    sgen-protocol-def.h
    sgen-protocol.c
    sgen-protocol.h
    sgen-qsort.c
    sgen-qsort.h
    sgen-scan-object.h
    sgen-simple-nursery.c
    sgen-split-nursery.c
    sgen-tagged-pointer.h
    sgen-thread-pool.c
    sgen-thread-pool.h
    sgen-workers.c
    sgen-workers.h)

set(sgen_sources "${sgen_sources_base}")

list(APPEND library_configs_to_build "default")
if(HOST_WIN32 AND NOT DISABLE_SHARED_LIBS)
    list(APPEND library_configs_to_build "shared")
endif()

foreach(library_config IN LISTS library_configs_to_build)
    set(library_name_suffix "")
    if(NOT library_config STREQUAL "default")
        set(library_name_suffix _${library_config})
    endif()
    set(utils_objects_library_name utils_objects${library_name_suffix})
    set(sgen_objects_library_name sgen_objects${library_name_suffix})

    add_library(${sgen_objects_library_name} ${sgen_sources})
    target_link_libraries(${sgen_objects_library_name} PRIVATE monoapi eglib_api ${utils_objects_library_name})
    target_compile_definitions(${sgen_objects_library_name} PRIVATE -DHAVE_SGEN_GC)
    if(NOT HOST_WIN32 OR (HOST_WIN32 AND ${library_config} STREQUAL "shared"))
        target_compile_definitions(${sgen_objects_library_name} PRIVATE -DMONO_DLL_EXPORT)
    endif()
    target_include_directories(${sgen_objects_library_name} PRIVATE ${PROJECT_BINARY_DIR}/../..
        ${PROJECT_SOURCE_DIR}/../..
        ${PROJECT_SOURCE_DIR}/..)
endforeach()
