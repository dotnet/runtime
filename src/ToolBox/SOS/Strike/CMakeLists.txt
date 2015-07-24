if(CLR_CMAKE_PLATFORM_ARCH_AMD64)
  add_definitions(-DSOS_TARGET_AMD64=1)
  add_definitions(-D_TARGET_WIN64_=1)
  add_definitions(-DDBG_TARGET_64BIT)
  add_definitions(-DDBG_TARGET_WIN64=1)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
  add_definitions(-DSOS_TARGET_ARM=1)
  add_definitions(-D_TARGET_WIN32_=1)
  add_definitions(-DDBG_TARGET_32BIT)
  add_definitions(-DDBG_TARGET_WIN32=1)
endif()
add_definitions(-DSTRIKE)  

remove_definitions(-DUNICODE)
remove_definitions(-D_UNICODE)

if(WIN32)
  include_directories(inc)
  include_directories(BEFORE ${VM_DIR})
  include_directories(${CLR_DIR}/src/gcdump)
  include_directories(${CLR_DIR}/src/debug/shim)
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
  
  if(CLR_CMAKE_PLATFORM_ARCH_AMD64)
      set(SOS_SOURCES_ARCH
        disasmX86.cpp
      )
  elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
      set(SOS_SOURCES_ARCH
        disasmARM.cpp
      )
  endif()
  
  list(APPEND SOS_SOURCES ${SOS_SOURCES_ARCH})

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
  include_directories(../lldbplugin/inc)
  include_directories(BEFORE ${VM_DIR})
  include_directories(${CLR_DIR}/src/debug/shim)

  add_compile_options(-fPIC)

  set(SOS_SOURCES
    disasm.cpp
    datatarget.cpp
    eeheap.cpp
    exts.cpp
    gcroot.cpp
    metadata.cpp
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

add_library(sos SHARED ${SOS_SOURCES})
add_dependencies(sos mscordaccore)
target_link_libraries(sos ${SOS_LIBRARY})

# add the install targets
install (TARGETS sos DESTINATION .)
if(WIN32)
  install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/sos.pdb DESTINATION PDB)
endif(WIN32)
