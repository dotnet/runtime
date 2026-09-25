project(nativeaot-createdump)

# Enabled variant: full ELF core dump writer (Linux only).
# macOS support requires Mach-O dump format and Mach APIs — deferred to future work.
if(CLR_CMAKE_TARGET_LINUX AND (CLR_CMAKE_TARGET_ARCH_AMD64 OR CLR_CMAKE_TARGET_ARCH_ARM64))
  set(CREATEDUMP_SHARED_DIR ${CLR_DIR}/debug/createdump/shared)
  set(NATIVEAOT_CREATEDUMP_SOURCES
    nativeaot_createdump_main.cpp
    ${CREATEDUMP_SHARED_DIR}/createdumpcore.cpp
    ${CREATEDUMP_SHARED_DIR}/createdumpunixcore.cpp
    ${CREATEDUMP_SHARED_DIR}/crashinfocore.cpp
    ${CREATEDUMP_SHARED_DIR}/crashinfounixcore.cpp
    ${CREATEDUMP_SHARED_DIR}/dumpname.cpp
    ${CREATEDUMP_SHARED_DIR}/dumpwriter.cpp
    ${CREATEDUMP_SHARED_DIR}/dumpwriterelf.cpp
    ${CREATEDUMP_SHARED_DIR}/threadsnapshot.cpp
  )

  add_library(nativeaot-createdump-enabled STATIC ${NATIVEAOT_CREATEDUMP_SOURCES})
  target_include_directories(nativeaot-createdump-enabled PRIVATE ${CREATEDUMP_SHARED_DIR})
  install_static_library(nativeaot-createdump-enabled aotsdk nativeaot)
endif()

# Disabled variant: no-op stubs (all platforms).
add_library(nativeaot-createdump-disabled STATIC createdump_disabled.cpp)
install_static_library(nativeaot-createdump-disabled aotsdk nativeaot)
