project(dbgshim)

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

include(${PROJECT_SOURCE_DIR}/../../../../eng/native/configuretools.cmake)
include(${PROJECT_SOURCE_DIR}/../../../../eng/native/configurepaths.cmake)
include(${PROJECT_SOURCE_DIR}/../../../../eng/native/configureplatform.cmake)
include(${PROJECT_SOURCE_DIR}/../../../../eng/native/configurecompiler.cmake)


add_definitions(-D_WIN32_WINNT=0x0602)

include_directories(
  ${CMAKE_CURRENT_SOURCE_DIR}/../../..
  ${PROJECT_SOURCE_DIR}/../../
  ${PROJECT_SOURCE_DIR}/../mscordbi
  ${PROJECT_SOURCE_DIR}/../mscordbi/socket-dbi
  ${PROJECT_SOURCE_DIR}/../../../coreclr/md/enc
  ${PROJECT_SOURCE_DIR}/../../../coreclr/inc
  ${PROJECT_SOURCE_DIR}/../../../coreclr/pal/inc
  ${PROJECT_SOURCE_DIR}/../../../coreclr/md/inc
  ${PROJECT_SOURCE_DIR}/../../../coreclr/md/compiler)

include_directories("../../../coreclr/pal/prebuilt/inc")
include_directories("../../../coreclr/nativeresources")

if (CLR_CMAKE_HOST_UNIX)
  include_directories("${PROJECT_SOURCE_DIR}/../../../coreclr/pal/inc")
  include_directories("${PROJECT_SOURCE_DIR}/../../../coreclr/pal/inc/rt")
  include_directories("${PROJECT_SOURCE_DIR}/../../../coreclr/pal/src/safecrt")

  append("-Wno-missing-prototypes  -Wno-pointer-arith -Wno-macro-redefined" CMAKE_C_FLAGS CMAKE_CXX_FLAGS)

  include_directories("../../../coreclr/pal/inc/rt/cpp")
endif (CLR_CMAKE_HOST_UNIX)

set(DBGSHIM_SOURCES
    dbgshim.cpp
)

add_library(dbgshim SHARED ${DBGSHIM_SOURCES})

if(HOST_DARWIN)
set(OS_LIBS "-framework CoreFoundation" "-framework Foundation")
elseif(HOST_LINUX)
set(OS_LIBS pthread m dl)
elseif(HOST_WIN32)
set(OS_LIBS bcrypt.lib Mswsock.lib ws2_32.lib psapi.lib version.lib advapi32.lib winmm.lib kernel32.lib)
endif()


set(DBGSHIM_LIBRARIES
    ${OS_LIBS}
    utilcodestaticnohost
)

if(CLR_CMAKE_HOST_UNIX)
    list(APPEND DBGSHIM_LIBRARIES
        coreclrpal
        palrt
        nativeresourcestring
    )
endif()

target_link_libraries(dbgshim ${DBGSHIM_LIBRARIES})
install(TARGETS dbgshim DESTINATION lib)
