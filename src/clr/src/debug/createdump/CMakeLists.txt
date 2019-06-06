project(createdump)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

# Set the RPATH of createdump so that it can find dependencies without needing to set LD_LIBRARY_PATH
# For more information: http://www.cmake.org/Wiki/CMake_RPATH_handling.
if (CORECLR_SET_RPATH)
  set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
  if(CLR_CMAKE_PLATFORM_DARWIN)
    set(CMAKE_INSTALL_RPATH "@loader_path")
  else()
    set(CMAKE_INSTALL_RPATH "\$ORIGIN")
  endif(CLR_CMAKE_PLATFORM_DARWIN)
endif (CORECLR_SET_RPATH)

remove_definitions(-DUNICODE)
remove_definitions(-D_UNICODE)

include_directories(BEFORE ${VM_DIR})

add_definitions(-DPAL_STDCPP_COMPAT)

add_compile_options(-fPIE)

set(CREATEDUMP_SOURCES 
    createdump.cpp 
    crashinfo.cpp
    threadinfo.cpp
    datatarget.cpp
    dumpwriter.cpp
)

_add_library(createdump_lib
    ${CREATEDUMP_SOURCES}
)

_add_executable(createdump
    main.cpp
    ${PAL_REDEFINES_FILE}
)

add_dependencies(createdump pal_redefines_file)

target_link_libraries(createdump
    createdump_lib
    # share the PAL/corguids in the dac module
    mscordaccore
)

add_dependencies(createdump mscordaccore)

install_clr(createdump)
