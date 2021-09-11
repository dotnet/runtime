project(superpmi-shim-collector)

remove_definitions(-DUNICODE)
remove_definitions(-D_UNICODE)

add_definitions(-DFEATURE_NO_HOST)
add_definitions(-DSELF_NO_HOST)

if(CLR_CMAKE_HOST_WIN32)
  #use static crt
  set(CMAKE_MSVC_RUNTIME_LIBRARY MultiThreaded)
endif(CLR_CMAKE_HOST_WIN32)

include_directories(.)
include_directories(../superpmi-shared)
include_directories(../superpmi-shared/brotli/include)

set(SUPERPMI_SHIM_COLLECTOR_SOURCES
    jithost.cpp
    icorjitcompiler.cpp
    icorjitinfo.cpp
    superpmi-shim-collector.cpp
    ../superpmi-shared/callutils.cpp
    ../superpmi-shared/compileresult.cpp
    ../superpmi-shared/errorhandling.cpp
    ../superpmi-shared/hash.cpp
    ../superpmi-shared/logging.cpp
    ../superpmi-shared/mclist.cpp
    ../superpmi-shared/methodcontext.cpp
    ../superpmi-shared/methodcontextreader.cpp
    ../superpmi-shared/simpletimer.cpp
    ../superpmi-shared/spmiutil.cpp
    ../superpmi-shared/tocfile.cpp
    ../superpmi-shared/typeutils.cpp
    ../superpmi-shared/spmidumphelper.cpp
    ../superpmi-shared/brotli/common/constants.c
    ../superpmi-shared/brotli/common/context.c
    ../superpmi-shared/brotli/common/dictionary.c
    ../superpmi-shared/brotli/common/platform.c
    ../superpmi-shared/brotli/common/transform.c
    ../superpmi-shared/brotli/dec/bit_reader.c
    ../superpmi-shared/brotli/dec/decode.c
    ../superpmi-shared/brotli/dec/huffman.c
    ../superpmi-shared/brotli/dec/state.c
    ../superpmi-shared/brotli/enc/backward_references.c
    ../superpmi-shared/brotli/enc/backward_references_hq.c
    ../superpmi-shared/brotli/enc/bit_cost.c
    ../superpmi-shared/brotli/enc/block_splitter.c
    ../superpmi-shared/brotli/enc/brotli_bit_stream.c
    ../superpmi-shared/brotli/enc/cluster.c
    ../superpmi-shared/brotli/enc/command.c
    ../superpmi-shared/brotli/enc/compress_fragment.c
    ../superpmi-shared/brotli/enc/compress_fragment_two_pass.c
    ../superpmi-shared/brotli/enc/dictionary_hash.c
    ../superpmi-shared/brotli/enc/encode.c
    ../superpmi-shared/brotli/enc/encoder_dict.c
    ../superpmi-shared/brotli/enc/entropy_encode.c
    ../superpmi-shared/brotli/enc/fast_log.c
    ../superpmi-shared/brotli/enc/histogram.c
    ../superpmi-shared/brotli/enc/literal_cost.c
    ../superpmi-shared/brotli/enc/memory.c
    ../superpmi-shared/brotli/enc/metablock.c
    ../superpmi-shared/brotli/enc/static_dict.c
    ../superpmi-shared/brotli/enc/utf8_util.c
)
if (CLR_CMAKE_TARGET_WIN32)
    preprocess_file(${CMAKE_CURRENT_SOURCE_DIR}/superpmi-shim-collector.def ${CMAKE_CURRENT_BINARY_DIR}/superpmi-shim-collector.def)

    list(APPEND SUPERPMI_SHIM_COLLECTOR_SOURCES ${CMAKE_CURRENT_BINARY_DIR}/superpmi-shim-collector.def)
endif (CLR_CMAKE_TARGET_WIN32)

add_library_clr(superpmi-shim-collector
    SHARED
    ${SUPERPMI_SHIM_COLLECTOR_SOURCES}
)

target_precompile_headers(superpmi-shim-collector PRIVATE "$<$<COMPILE_LANGUAGE:CXX>:standardpch.h>")

if(CLR_CMAKE_HOST_UNIX)
    target_link_libraries(superpmi-shim-collector
        utilcodestaticnohost
        mscorrc
        coreclrpal
        palrt
    )
else()
    target_link_libraries(superpmi-shim-collector
        advapi32.lib
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_CPP_LIB}
    )
endif(CLR_CMAKE_HOST_UNIX)

install_clr(TARGETS superpmi-shim-collector DESTINATIONS .)
