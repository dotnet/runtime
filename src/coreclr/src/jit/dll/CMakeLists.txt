project(ClrJit)

set_source_files_properties(${JIT_EXPORTS_FILE} PROPERTIES GENERATED TRUE)

add_library_clr(clrjit_static
    STATIC
    ${SHARED_LIB_SOURCES}
    ${JIT_ARCH_SOURCES}
    ${JIT_DEF_FILE}
)

add_library_clr(clrjit_mergeable
    OBJECT
    ${SHARED_LIB_SOURCES}
    ${JIT_ARCH_SOURCES}
)

target_compile_definitions(clrjit_mergeable PUBLIC FEATURE_MERGE_JIT_AND_ENGINE)

if(CLR_CMAKE_HOST_UNIX)
    add_dependencies(clrjit_static coreclrpal gcinfo)
    add_dependencies(clrjit_mergeable coreclrpal gcinfo)
endif(CLR_CMAKE_HOST_UNIX)

target_precompile_header(TARGET clrjit_static HEADER jitpch.h ADDITIONAL_INCLUDE_DIRECTORIES ${JIT_SOURCE_DIR})
target_precompile_header(TARGET clrjit_mergeable HEADER jitpch.h ADDITIONAL_INCLUDE_DIRECTORIES ${JIT_SOURCE_DIR})
