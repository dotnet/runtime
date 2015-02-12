include_directories("inc")
include_directories("strongname/inc")
include_directories("inc/winrt")

include_directories("debug/inc")
include_directories("debug/inc/amd64")
include_directories("debug/inc/dump")
include_directories("md/inc")
include_directories("classlibnative/bcltype")
include_directories("classlibnative/cryptography")
include_directories("classlibnative/inc")

if(WIN32)
enable_language(ASM_MASM)
else()
enable_language(ASM)
# This prevents inclusion of standard C compiler headers
add_compile_options(-nostdinc)

if(NOT CLR_CMAKE_PLATFORM_DARWIN)
  # This prevents inclusion of standard C++ compiler headers
  set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -nostdinc++")
endif(NOT CLR_CMAKE_PLATFORM_DARWIN)

endif(WIN32)

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
add_subdirectory(unwinder)

if(WIN32)
  add_subdirectory(ipcman)
  add_subdirectory(tools)
endif(WIN32)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_subdirectory(palrt)
endif(CLR_CMAKE_PLATFORM_UNIX)
