project(clrgcsample)

include_directories(..)
include_directories(../env)

set(SOURCES
    GCSample.cpp    
)

add_executable(gcsample
    ${SOURCES}
)

if(CLR_CMAKE_PLATFORM_ARCH_AMD64)
  add_definitions(-D_TARGET_AMD64_=1)
  add_definitions(-D_WIN64=1)
elseif(CLR_CMAKE_PLATFORM_ARCH_I386)
  add_definitions(-D_TARGET_X86_=1)
  add_definitions(-D_WIN32=1)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
  add_definitions(-D_TARGET_ARM_=1)
  add_definitions(-D_WIN32=1)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM64)
  add_definitions(-D_TARGET_ARM64_=1)
  add_definitions(-D_WIN64=1)
else()
  clr_unknown_arch()
endif()

target_link_libraries(gcsample 
    clrgc
)
