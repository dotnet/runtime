project(mscordbi)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(CMAKE_EXPORT_COMPILE_COMMANDS ON)
set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

set(CLR_DIR ${PROJECT_SOURCE_DIR}/../../coreclr)
set(VM_DIR ${PROJECT_SOURCE_DIR}/../../coreclr/vm)
set(CMAKE_OSX_ARCHITECTURES ${CMAKE_SYSTEM_PROCESSOR})
set(CMAKE_EXE_LINKER_FLAGS_CHECKED "")
set(CMAKE_SHARED_LINKER_FLAGS_CHECKED "")
set(CLR_CMAKE_HOST_ARCH ${CMAKE_GENERATOR_PLATFORM})
set(FEATURE_EVENT_TRACE 0)

if(HOST_WIN32)
    if(HOST_X86)
        set(CLR_CMAKE_HOST_ARCH x86)
    elseif(HOST_ARM64)
        set(CLR_CMAKE_HOST_ARCH arm64)
    elseif(HOST_ARM)
        set(CLR_CMAKE_HOST_ARCH arm)
    elseif(HOST_AMD64)
        set(CLR_CMAKE_HOST_ARCH x64)
    endif()
endif()

add_definitions(-DDBI_COMPONENT_MONO)

include_directories(
  ${CMAKE_CURRENT_SOURCE_DIR}/../..
  ${PROJECT_SOURCE_DIR}/../
  ${PROJECT_SOURCE_DIR}/../dbi
  ${PROJECT_SOURCE_DIR}/../dbi/socket-dbi
  ${PROJECT_SOURCE_DIR}/../../coreclr/md/enc
  ${PROJECT_SOURCE_DIR}/../../coreclr/inc
  ${PROJECT_SOURCE_DIR}/../../coreclr/md/inc
  ${PROJECT_SOURCE_DIR}/../../coreclr/md/compiler)

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
    cordb-thread.cpp
    cordb-thread.h
    cordb-type.cpp
    cordb-type.h
    cordb-value.cpp
    cordb-value.h
)


if(HOST_DARWIN)
set(OS_LIBS "-framework CoreFoundation" "-framework Foundation")
elseif(HOST_LINUX)
set(OS_LIBS pthread m dl)
elseif(HOST_WIN32)
set(OS_LIBS bcrypt.lib Mswsock.lib ws2_32.lib psapi.lib version.lib advapi32.lib winmm.lib kernel32.lib)
endif()

addprefix(mscorbi_sources ../dbi/ "${mscorbi_sources_base}")
add_subdirectory(${PROJECT_SOURCE_DIR}/socket-dbi)

include(${PROJECT_SOURCE_DIR}/../../../eng/native/configuretools.cmake)
include(${PROJECT_SOURCE_DIR}/../../../eng/native/configurepaths.cmake)
include(${PROJECT_SOURCE_DIR}/../../../eng/native/configureplatform.cmake)
include(${PROJECT_SOURCE_DIR}/../../../eng/native/configurecompiler.cmake)

if (CLR_CMAKE_HOST_UNIX)
  include_directories("${PROJECT_SOURCE_DIR}/../../coreclr/pal/inc")
  include_directories("${PROJECT_SOURCE_DIR}/../../coreclr/pal/inc/rt")
  include_directories("${PROJECT_SOURCE_DIR}/../../coreclr/pal/src/safecrt")

  append("-Wno-missing-prototypes  -Wno-pointer-arith -Wno-macro-redefined" CMAKE_C_FLAGS CMAKE_CXX_FLAGS)
  add_subdirectory(${PROJECT_SOURCE_DIR}/../../coreclr/pal pal)

  include_directories("../../coreclr/pal/inc/rt/cpp")
  add_compile_options(-nostdinc)
endif (CLR_CMAKE_HOST_UNIX)

include_directories("../../coreclr/pal/prebuilt/inc")
include_directories("../../coreclr/nativeresources")

if (CLR_CMAKE_HOST_UNIX)
    add_subdirectory(${PROJECT_SOURCE_DIR}/../../coreclr/nativeresources nativeresources)
endif()

add_subdirectory(${PROJECT_SOURCE_DIR}/../../coreclr/md/runtime md/runtime)
add_subdirectory(${PROJECT_SOURCE_DIR}/../../coreclr/md/compiler md/compiler)

include(${PROJECT_SOURCE_DIR}/../../coreclr/clrdefinitions.cmake)
include_directories(${CMAKE_CURRENT_BINARY_DIR}/../)
include_directories(${CMAKE_CURRENT_BINARY_DIR}/../inc/)
add_subdirectory(${PROJECT_SOURCE_DIR}/../../coreclr/md/enc md/enc)
add_subdirectory(${PROJECT_SOURCE_DIR}/../../coreclr/utilcode utilcode)
if (CLR_CMAKE_HOST_UNIX)
    add_subdirectory(${PROJECT_SOURCE_DIR}/../../coreclr/palrt palrt)
    append("-Wno-strict-prototypes -Wno-deprecated -Wno-pointer-arith" CMAKE_C_FLAGS CMAKE_CXX_FLAGS)
endif (CLR_CMAKE_HOST_UNIX)

add_library(mscordbi SHARED "${mscorbi_sources};${PROJECT_SOURCE_DIR}/../mono/mini/debugger-protocol.c;${PROJECT_SOURCE_DIR}/../../coreclr/pal/prebuilt/idl/xcordebug_i.cpp;${PROJECT_SOURCE_DIR}/../../coreclr/pal/prebuilt/idl/cordebug_i.cpp")

#SET(CMAKE_C_COMPILER ${CMAKE_CXX_COMPILER})

set_source_files_properties(${PROJECT_SOURCE_DIR}/../mono/mini/debugger-protocol.c PROPERTIES LANGUAGE CXX)

set(COREDBI_LIBRARIES
    utilcodestaticnohost
    mdruntime-dbi
    mdcompiler-dbi
    mdruntimerw-dbi
    socket-dbi
    ${OS_LIBS}
)

if(CLR_CMAKE_HOST_UNIX)
    list(APPEND COREDBI_LIBRARIES
        coreclrpal
        palrt
        nativeresourcestring
    )
endif()

target_link_libraries(mscordbi ${COREDBI_LIBRARIES} )
install(TARGETS mscordbi DESTINATION lib)
