set(CMAKE_INCLUDE_CURRENT_DIR ON)

#this will again add vm to include directories. However, this will add vm to be the first include folder.
#This is required because util.hpp is also present in ildasm folder and so due to ordering wrong util.hpp 
#is pickedup.
include_directories(BEFORE ${VM_DIR})

include_directories(${VM_DIR}/${ARCH_SOURCES_DIR})
include_directories(${CLR_DIR}/src/md/compiler)

add_definitions(-DSTRONGNAME_IN_VM -DSNAPI_INTERNAL)

set(STRONGNAME_SOURCES
  strongname.cpp
  strongnameinternal.cpp
)

convert_to_absolute_path(STRONGNAME_SOURCES ${STRONGNAME_SOURCES})

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(dac)
add_subdirectory(wks)
add_subdirectory(crossgen)
