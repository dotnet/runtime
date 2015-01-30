include_directories(BEFORE "../vm")
include_directories("../inc")
include_directories("../vm/amd64") # TODO: use the target cpu
include_directories("../debug/inc")
include_directories("../debug/inc/dump")

#TODO for x86
add_definitions(-DDBG_TARGET_64BIT=1)
add_definitions(-DDBG_TARGET_AMD64=1)
add_definitions(-DDBG_TARGET_WIN64=1)

add_subdirectory(bcltype)
add_subdirectory(float)

if(WIN32)
  add_subdirectory(nls)
  add_subdirectory(cryptography)
endif(WIN32)

