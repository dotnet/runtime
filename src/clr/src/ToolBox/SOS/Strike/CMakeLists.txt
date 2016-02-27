# Set the RPATH of sos so that it can find dependencies without needing to set LD_LIBRARY_PATH
# For more information: http://www.cmake.org/Wiki/CMake_RPATH_handling.
if (CORECLR_SET_RPATH)
  if(CLR_CMAKE_PLATFORM_DARWIN)
    set(CMAKE_INSTALL_RPATH "@loader_path")
  else()
    set(CMAKE_INSTALL_RPATH "\$ORIGIN")
  endif(CLR_CMAKE_PLATFORM_DARWIN)
endif (CORECLR_SET_RPATH)

if(CLR_CMAKE_PLATFORM_ARCH_AMD64)
  add_definitions(-DSOS_TARGET_AMD64=1)
  add_definitions(-D_TARGET_WIN64_=1)
  add_definitions(-DDBG_TARGET_64BIT)
  add_definitions(-DDBG_TARGET_WIN64=1)
elseif(CLR_CMAKE_PLATFORM_ARCH_I386)
  add_definitions(-DSOS_TARGET_X86=1)
  add_definitions(-D_TARGET_X86_=1)
  add_definitions(-DDBG_TARGET_32BIT)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
  add_definitions(-DSOS_TARGET_ARM=1)
  add_definitions(-D_TARGET_WIN32_=1)
  add_definitions(-DDBG_TARGET_32BIT)
endif()
add_definitions(-DSTRIKE)  

remove_definitions(-DUNICODE)
remove_definitions(-D_UNICODE)

include_directories(BEFORE ${VM_DIR})
include_directories(${CLR_DIR}/src/gcdump)
include_directories(${CLR_DIR}/src/debug/shim)

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
    kernel32.lib
    user32.lib
    ole32.lib
    dbghelp.lib
    uuid.lib
    version.lib
    dbgeng.lib
    advapi32.lib
    psapi.lib
    ntdll.lib
  )
else(WIN32)
  add_definitions(-DFEATURE_ENABLE_HARDWARE_EXCEPTIONS)
  add_definitions(-DPAL_STDCPP_COMPAT=1)
  add_compile_options(-Wno-null-arithmetic)
  add_compile_options(-Wno-format)

  include_directories(BEFORE xplat)
  include_directories(BEFORE ../lldbplugin/inc)

  add_compile_options(-fPIC)

  set(SOS_SOURCES
    disasm.cpp
    datatarget.cpp
    eeheap.cpp
    exts.cpp
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
  )
endif(WIN32)

if(CLR_CMAKE_PLATFORM_ARCH_AMD64)
  set(SOS_SOURCES_ARCH
    disasmX86.cpp
  )
elseif(CLR_CMAKE_PLATFORM_ARCH_I386)
  set(SOS_SOURCES_ARCH 
    disasmX86.cpp
  )
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
  set(SOS_SOURCES_ARCH
    disasmARM.cpp
  )
endif()

list(APPEND SOS_SOURCES ${SOS_SOURCES_ARCH})

add_library_clr(sos SHARED ${SOS_SOURCES})
add_dependencies(sos mscordaccore)
target_link_libraries(sos ${SOS_LIBRARY})

# add the install targets
install (TARGETS sos DESTINATION .)
if(WIN32)
  install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/sos.pdb DESTINATION PDB)
else(WIN32)
  install (FILES sosdocsunix.txt DESTINATION .)
endif(WIN32)
