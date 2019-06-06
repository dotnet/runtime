# Set the RPATH of sos so that it can find dependencies without needing to set LD_LIBRARY_PATH
# For more information: http://www.cmake.org/Wiki/CMake_RPATH_handling.
if (CORECLR_SET_RPATH)
  set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
  if(CLR_CMAKE_PLATFORM_DARWIN)
    set(CMAKE_INSTALL_RPATH "@loader_path")
  else()
    set(CMAKE_INSTALL_RPATH "\$ORIGIN")
  endif(CLR_CMAKE_PLATFORM_DARWIN)
endif (CORECLR_SET_RPATH)

if(CLR_CMAKE_PLATFORM_ARCH_AMD64)
  add_definitions(-DSOS_TARGET_AMD64)
  add_definitions(-D_TARGET_WIN64_)
  add_definitions(-DDBG_TARGET_64BIT)
  add_definitions(-DDBG_TARGET_WIN64)
  if(WIN32)
    add_definitions(-DSOS_TARGET_ARM64)
  endif(WIN32)
  remove_definitions(-D_TARGET_ARM64_)
  add_definitions(-D_TARGET_AMD64_)
  add_definitions(-DDBG_TARGET_AMD64)
elseif(CLR_CMAKE_PLATFORM_ARCH_I386)
  add_definitions(-DSOS_TARGET_X86)
  add_definitions(-D_TARGET_X86_)
  add_definitions(-DDBG_TARGET_32BIT)
  if(WIN32)
    add_definitions(-DSOS_TARGET_ARM)
  endif(WIN32)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
  add_definitions(-DSOS_TARGET_ARM)
  add_definitions(-D_TARGET_WIN32_)
  add_definitions(-D_TARGET_ARM_)
  add_definitions(-DDBG_TARGET_32BIT)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM64)
  add_definitions(-DSOS_TARGET_ARM64)
  add_definitions(-D_TARGET_WIN64_)
  add_definitions(-DDBG_TARGET_64BIT)
  add_definitions(-DDBG_TARGET_WIN64)
endif()

add_definitions(-DSTRIKE)  

remove_definitions(-DUNICODE)
remove_definitions(-D_UNICODE)

include_directories(BEFORE ${VM_DIR})
include_directories(${CLR_DIR}/src/gcdump)
include_directories(${CLR_DIR}/src/debug/shim)
include_directories(${CLR_DIR}/src/coreclr/hosts/unixcoreruncommon)
include_directories(${CLR_DIR}/src/coreclr/hosts/inc)

if(WIN32)
  include_directories(inc)
  include_directories("$ENV{VSInstallDir}/DIA SDK/include")

  add_definitions(-DUSE_STL)

  #use static crt
  add_definitions(-MT) 

  set(SOS_SOURCES
    disasm.cpp
    dllsext.cpp
    eeheap.cpp
    EventCallbacks.cpp
    ExpressionNode.cpp
    exts.cpp
    gchist.cpp
    gcroot.cpp
    metadata.cpp
    sildasm.cpp
    sos.cpp
    stressLogDump.cpp
    strike.cpp
    util.cpp
    vm.cpp
    WatchCmd.cpp
    Native.rc
  )
  
  add_definitions(-DFX_VER_INTERNALNAME_STR=SOS.dll)

  #Preprocess exports definition file
  preprocess_def_file(${CMAKE_CURRENT_SOURCE_DIR}/sos.def ${CMAKE_CURRENT_BINARY_DIR}/sos.def)
  list(APPEND SOS_SOURCES ${CMAKE_CURRENT_BINARY_DIR}/sos.def)

  set(SOS_LIBRARY
    corguids
    debugshim
    dbgutil
    ${STATIC_MT_CRT_LIB}
    ${STATIC_MT_CPP_LIB}
    ${STATIC_MT_VCRT_LIB}
    kernel32.lib
    user32.lib
    ole32.lib
    oleaut32.lib
    dbghelp.lib
    uuid.lib
    version.lib
    dbgeng.lib
    advapi32.lib
    psapi.lib
    ntdll.lib
  )
else(WIN32)
  add_definitions(-DPAL_STDCPP_COMPAT)
  if (CMAKE_CXX_COMPILER_ID MATCHES "Clang")
    add_compile_options(-Wno-null-arithmetic)
  elseif (CMAKE_CXX_COMPILER_ID STREQUAL "GNU")
    add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-Wno-conversion-null>)
    add_compile_options(-Wno-pointer-arith)
  endif()
  add_compile_options(-Wno-format)

  include_directories(BEFORE xplat)
  include_directories(BEFORE ../lldbplugin/inc)

  add_compile_options(-fPIC)

  set(SOS_SOURCES
    disasm.cpp
    datatarget.cpp
    eeheap.cpp
    exts.cpp
    gchist.cpp
    gcroot.cpp
    metadata.cpp
    sildasm.cpp
    stressLogDump.cpp
    strike.cpp
    sos.cpp
    util.cpp
  )

  set(SOS_LIBRARY
    corguids
    debugshim
    dbgutil
    # share the PAL in the dac module
    mscordaccore
    palrt
    unixcoreruncommon
  )

  set(DEF_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/sos_unixexports.src)
  set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/sos.exports)
  generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})
endif(WIN32)

if(CLR_CMAKE_PLATFORM_ARCH_AMD64)
  if(CLR_CMAKE_PLATFORM_LINUX)
    list(APPEND 
      SOS_LIBRARY 
      createdump_lib
    )
    add_definitions(-DCREATE_DUMP_SUPPORTED)
  endif(CLR_CMAKE_PLATFORM_LINUX)
  set(SOS_SOURCES_ARCH
    disasmX86.cpp
  )
  if(WIN32)
    list(APPEND 
      SOS_SOURCES_ARCH  
      disasmARM64.cpp
    )
  endif(WIN32)
elseif(CLR_CMAKE_PLATFORM_ARCH_I386)
  set(SOS_SOURCES_ARCH 
    disasmX86.cpp
  )
  if(WIN32)
    list(APPEND
      SOS_SOURCES_ARCH
      disasmARM.cpp
    )
  endif(WIN32)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
  set(SOS_SOURCES_ARCH
    disasmARM.cpp
  )
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM64)
  set(SOS_SOURCES_ARCH
    disasmARM64.cpp
  )
endif()

list(APPEND SOS_SOURCES ${SOS_SOURCES_ARCH})

# Add the DAC PAL export mapping file
if(CLR_CMAKE_PLATFORM_LINUX)
    list(APPEND SOS_SOURCES ${PAL_REDEFINES_FILE})
endif(CLR_CMAKE_PLATFORM_LINUX)

if(CLR_CMAKE_PLATFORM_LINUX OR CLR_CMAKE_PLATFORM_FREEBSD OR CLR_CMAKE_PLATFORM_NETBSD)
    # Add linker exports file option
    set(EXPORTS_LINKER_OPTION -Wl,--version-script=${EXPORTS_FILE})
endif(CLR_CMAKE_PLATFORM_LINUX OR CLR_CMAKE_PLATFORM_FREEBSD OR CLR_CMAKE_PLATFORM_NETBSD)

if(CLR_CMAKE_PLATFORM_DARWIN)
    # Add linker exports file option
    set(EXPORTS_LINKER_OPTION -Wl,-exported_symbols_list,${EXPORTS_FILE})
endif(CLR_CMAKE_PLATFORM_DARWIN)

add_library_clr(sos SHARED ${SOS_SOURCES})

if(CLR_CMAKE_PLATFORM_LINUX)
    add_dependencies(sos pal_redefines_file)
endif(CLR_CMAKE_PLATFORM_LINUX)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_custom_target(sos_exports DEPENDS ${EXPORTS_FILE})
    add_dependencies(sos sos_exports)

    set_property(TARGET sos APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
    set_property(TARGET sos APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})

    add_dependencies(sos mscordaccore)
endif(CLR_CMAKE_PLATFORM_UNIX)

target_link_libraries(sos ${SOS_LIBRARY})

# add the install targets
install_clr(sos)

if(NOT WIN32)
  _install(FILES sosdocsunix.txt DESTINATION .)
endif(NOT WIN32)
