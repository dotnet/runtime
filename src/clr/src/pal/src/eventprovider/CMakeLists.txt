cmake_minimum_required(VERSION 2.8.12)

set(EVENT_MANIFEST ${VM_DIR}/ClrEtwAll.man)

if(CLR_CMAKE_PLATFORM_LINUX OR CLR_CMAKE_PLATFORM_FREEBSD)
  add_subdirectory(lttngprovider)
else()
  add_subdirectory(dummyprovider)
endif()
