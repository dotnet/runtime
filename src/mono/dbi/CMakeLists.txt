project(mscordbi)


include_directories(
  ${PROJECT_BINARY_DIR}/
  ${PROJECT_BINARY_DIR}/../..
  ${PROJECT_BINARY_DIR}/../mono/eglib
  ${CMAKE_CURRENT_SOURCE_DIR}/../..
  ${CMAKE_CURRENT_BINARY_DIR}/../
  ${PROJECT_SOURCE_DIR}/../mono/mini/  
  ${PROJECT_SOURCE_DIR}/../
  ${PROJECT_SOURCE_DIR}/../mono/eglib
  ${PROJECT_SOURCE_DIR}/../dbi
  ${PROJECT_SOURCE_DIR}/../../coreclr/md/enc
  ${PROJECT_SOURCE_DIR}/../../coreclr/inc
  ${PROJECT_SOURCE_DIR}/../../coreclr/pal/prebuilt/inc
  ${PROJECT_SOURCE_DIR}/../../coreclr/md/inc
  ${PROJECT_SOURCE_DIR}/../../coreclr/md/compiler)


include(../mono/eglib/CMakeLists.txt)


set(UTILCODE_COMMON_SOURCES_BASE
  clrhost_nodependencies.cpp
  ccomprc.cpp
  ex.cpp
  sbuffer.cpp
  sstring_com.cpp
  fstring.cpp
  namespaceutil.cpp
  makepath.cpp
  splitpath.cpp
  clrconfig.cpp
  configuration.cpp
  collections.cpp
  posterror.cpp
  fstream.cpp
  clrhelpers.cpp
  stgpool.cpp
  stgpooli.cpp
  stgpoolreadonly.cpp
  utsem.cpp
  peinformation.cpp
  check.cpp
  log.cpp
  arraylist.cpp
  bitvector.cpp
  comex.cpp
  guidfromname.cpp
  memorypool.cpp
  iallocator.cpp
  loaderheap.cpp
  outstring.cpp
  ilformatter.cpp
  opinfo.cpp
  corimage.cpp
  format1.cpp
  prettyprintsig.cpp
  regutil.cpp
  sha1.cpp
  sigbuilder.cpp
  sigparser.cpp
  sstring.cpp
  util_nodependencies.cpp
  utilmessagebox.cpp
  safewrap.cpp
  clrhost.cpp
  cycletimer.cpp
  md5.cpp
  util.cpp
  stresslog.cpp
  debug.cpp
  pedecoder.cpp
  winfix.cpp
  longfilepathwrappers.cpp
  yieldprocessornormalized.cpp
  hostimpl.cpp
)

set(MDRUNTIMERW_SOURCES_BASE
  md/enc/liteweightstgdbrw.cpp
  md/enc/metamodelenc.cpp
  md/enc/metamodelrw.cpp
  md/enc/peparse.cpp
  md/enc/rwutil.cpp
  md/enc/stgio.cpp
  md/enc/stgtiggerstorage.cpp
  md/enc/stgtiggerstream.cpp
  md/enc/mdinternalrw.cpp
  md/enc/pdbheap.cpp
  md/compiler/importhelper.cpp
  md/runtime/metamodel.cpp
  md/runtime/mdcolumndescriptors.cpp
  md/runtime/recordpool.cpp
  md/runtime/mdfileformat.cpp
)

set(MDRUNTIMERW_HEADERS_BASE
  inc/corhdr.h  
  inc/metadata.h
  inc/mdfileformat.h
  inc/pedecoder.h
  inc/pedecoder.inl
  inc/posterror.h
  inc/sstring.h
  inc/sstring.inl
  md/compiler/importhelper.h
  md/compiler/regmeta.h
  md/hotdata/hotdataformat.h
  md/inc/liteweightstgdb.h
  md/inc/mdinternalrw.h
  md/inc/mdlog.h
  md/inc/metadatahash.h
  md/inc/metamodel.h
  md/inc/metamodelro.h
  md/inc/metamodelrw.h
  md/inc/pdbheap.h
  md/inc/portablepdbmdds.h
  md/inc/portablepdbmdi.h
  md/inc/rwutil.h
  md/inc/stgio.h
  md/inc/stgtiggerstorage.h
  md/inc/stgtiggerstream.h
  md/inc/streamutil.h
  md/runtime/mdinternalro.h
  inc/debugmacros.h
)

set(mscorbi_sources_base
    cordb.cpp
    cordb.h
    cordb-appdomain.cpp
    cordb-appdomain.h
    cordb-assembly.cpp
    cordb-assembly.h
    cordb-blocking-obj.cpp
    cordb-blocking-obj.h
    cordb-breakpoint.cpp
    cordb-breakpoint.h
    cordb-chain.cpp
    cordb-chain.h
    cordb-class.cpp
    cordb-class.h
    cordb-code.cpp
    cordb-code.h
    cordb-eval.cpp
    cordb-eval.h
    cordb-frame.cpp
    cordb-frame.h
    cordb-function.cpp
    cordb-function.h
    cordb-process.cpp
    cordb-process.h
    cordb-register.cpp
    cordb-register.h
    cordb-stepper.cpp
    cordb-stepper.h
    cordb-symbol.cpp
    cordb-symbol.h
    cordb-thread.cpp
    cordb-thread.h
    cordb-type.cpp
    cordb-type.h
    cordb-value.cpp
    cordb-value.h
)

if(HOST_WIN32)
set(OS_LIBS bcrypt.lib Mswsock.lib ws2_32.lib psapi.lib version.lib advapi32.lib winmm.lib kernel32.lib)
endif()


if(HOST_WIN32)
  list(APPEND UTILCODE_COMMON_SOURCES_BASE
    dacutil.cpp
    dlwrap.cpp
    securitywrapper.cpp
    securityutil.cpp
    stacktrace.cpp
  )
endif(HOST_WIN32)

addprefix(mscorbi_sources ../dbi/ "${mscorbi_sources_base}")
addprefix(UTILCODE_COMMON_SOURCES ${PROJECT_SOURCE_DIR}/../../coreclr/utilcode/ "${UTILCODE_COMMON_SOURCES_BASE}")
addprefix(MDRUNTIMERW_SOURCES ${PROJECT_SOURCE_DIR}/../../coreclr/ "${MDRUNTIMERW_SOURCES_BASE}")
addprefix(MDRUNTIMERW_HEADERS ${PROJECT_SOURCE_DIR}/../../coreclr/ "${MDRUNTIMERW_HEADERS_BASE}")
addprefix(eglib_sources ../mono/eglib/ "${eglib_sources}")

add_library(mscordbi SHARED "${eglib_sources};${mscorbi_sources};${PROJECT_SOURCE_DIR}/../mono/mini/debugger-protocol.c;${PROJECT_SOURCE_DIR}/../../coreclr/pal/prebuilt/idl/xcordebug_i.cpp;${PROJECT_SOURCE_DIR}/../../coreclr/pal/prebuilt/idl/cordebug_i.cpp")

add_library(mdruntimerw-dbi STATIC "${MDRUNTIMERW_SOURCES};${MDRUNTIMERW_HEADERS}")
add_library(utilcode STATIC "${UTILCODE_COMMON_SOURCES}")


target_link_libraries(mscordbi mdruntimerw-dbi utilcode ${OS_LIBS})

set(TARGET_AMD64 1)
add_compile_definitions(TARGET_AMD64)
add_compile_definitions(DBI_COMPONENT)
add_compile_definitions(HOST_WINDOWS)
add_compile_definitions(HOST_64BIT)
add_compile_definitions(FEATURE_COMINTEROP)
add_compile_definitions(SELF_NO_HOST)


target_precompile_headers(mdruntimerw-dbi PRIVATE ${PROJECT_SOURCE_DIR}/../../coreclr/md/enc/stdafx.h)
target_precompile_headers(utilcode PRIVATE ${PROJECT_SOURCE_DIR}/../../coreclr/utilcode/stdafx.h)


if(HOST_WIN32)
target_compile_definitions(utilcode PRIVATE _CRTIMP=)
endif(HOST_WIN32)

SET(CMAKE_C_COMPILER ${CMAKE_CXX_COMPILER})
SET_TARGET_PROPERTIES(mdruntimerw-dbi PROPERTIES LINKER_LANGUAGE CXX)
SET_TARGET_PROPERTIES(utilcode PROPERTIES LINKER_LANGUAGE CXX)

set_target_properties(mdruntimerw-dbi PROPERTIES DBI_COMPONENT TRUE)

set_source_files_properties(${PROJECT_SOURCE_DIR}/../mono/mini/debugger-protocol.c PROPERTIES LANGUAGE CXX)
