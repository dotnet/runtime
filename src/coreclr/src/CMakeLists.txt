include_directories("inc")
include_directories("strongname/inc")
include_directories("inc/winrt")

include_directories("debug/inc")
if(CLR_CMAKE_PLATFORM_ARCH_AMD64)
  include_directories("debug/inc/amd64")
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
  include_directories("debug/inc/arm")
else()
  message(FATAL_ERROR "Only ARM and AMD64 is supported")
endif()
  
include_directories("debug/inc/dump")
include_directories("md/inc")
include_directories("classlibnative/bcltype")
include_directories("classlibnative/cryptography")
include_directories("classlibnative/inc")

if(CLR_CMAKE_PLATFORM_UNIX)
  # This prevents inclusion of standard C compiler headers
  add_compile_options(-nostdinc)

  if(NOT CLR_CMAKE_PLATFORM_DARWIN)
    # This prevents inclusion of standard C++ compiler headers
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -nostdinc++")
  endif(NOT CLR_CMAKE_PLATFORM_DARWIN)

endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(utilcode)
add_subdirectory(gcinfo)
add_subdirectory(coreclr)
add_subdirectory(jit)
add_subdirectory(vm)
add_subdirectory(md)
add_subdirectory(debug)
add_subdirectory(inc)
add_subdirectory(strongname)
add_subdirectory(binder)
add_subdirectory(classlibnative)
add_subdirectory(dlls)
add_subdirectory(ToolBox)
add_subdirectory(tools)
add_subdirectory(unwinder)

if(WIN32)
  add_subdirectory(ipcman)
endif(WIN32)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_subdirectory(palrt)
endif(CLR_CMAKE_PLATFORM_UNIX)
