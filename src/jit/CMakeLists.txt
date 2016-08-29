set(CMAKE_INCLUDE_CURRENT_DIR ON)
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

include_directories("./jitstd")
include_directories("../inc")

# Enable the following for UNIX altjit on Windows
# add_definitions(-DALT_JIT)

if (CLR_CMAKE_TARGET_ARCH_AMD64)
  add_definitions(-DFEATURE_SIMD) 
  add_definitions(-DFEATURE_AVX_SUPPORT) 
endif ()


if(WIN32)
  set(JIT_RESOURCES Native.rc)
endif(WIN32)

set( JIT_SOURCES
  alloc.cpp
  assertionprop.cpp
  bitset.cpp
  block.cpp
  codegencommon.cpp
  compiler.cpp
  copyprop.cpp
  disasm.cpp
  earlyprop.cpp
  ee_il_dll.cpp
  eeinterface.cpp
  emit.cpp
  error.cpp
  flowgraph.cpp
  gcdecode.cpp
  gcencode.cpp
  gcinfo.cpp
  gentree.cpp
  gschecks.cpp
  hashbv.cpp
  hostallocator.cpp
  importer.cpp
  inline.cpp
  inlinepolicy.cpp
  instr.cpp
  jitconfig.cpp
  jiteh.cpp
  jittelemetry.cpp
  lclvars.cpp
  lir.cpp
  liveness.cpp
  loopcloning.cpp
  lower.cpp
  lsra.cpp
  morph.cpp
  objectalloc.cpp
  optcse.cpp
  optimizer.cpp
  rangecheck.cpp
  rationalize.cpp
  regalloc.cpp
  register_arg_convention.cpp
  regset.cpp
  scopeinfo.cpp
  sharedfloat.cpp
  sm.cpp
  smdata.cpp
  smweights.cpp
  ssabuilder.cpp
  ssarenamestate.cpp
  typeinfo.cpp
  unwind.cpp
  utils.cpp
  valuenum.cpp
)

if(CLR_CMAKE_TARGET_ARCH_AMD64)
  set( ARCH_SOURCES
    codegenxarch.cpp
    emitxarch.cpp
    lowerxarch.cpp
    simd.cpp
    simdcodegenxarch.cpp
    targetamd64.cpp
    unwindamd64.cpp
  )
elseif(CLR_CMAKE_TARGET_ARCH_ARM)
  set( ARCH_SOURCES
    codegenarm.cpp
    decomposelongs.cpp
    emitarm.cpp
    lowerarm.cpp
    targetarm.cpp
    unwindarm.cpp
  )
elseif(CLR_CMAKE_TARGET_ARCH_I386)
  set( ARCH_SOURCES
    codegenxarch.cpp
    decomposelongs.cpp
    emitxarch.cpp
    lowerxarch.cpp
    simd.cpp
    simdcodegenxarch.cpp
    targetx86.cpp
  )
elseif(CLR_CMAKE_TARGET_ARCH_ARM64)
  set( ARCH_SOURCES
    codegenarm64.cpp
    emitarm64.cpp
    lowerarm64.cpp
    targetarm64.cpp
    unwindarm.cpp
    unwindarm64.cpp
  )
else()
  clr_unknown_arch()
endif()

# The following defines all the source files used by the "legacy" back-end (#ifdef LEGACY_BACKEND).
# It is always safe to include both legacy and non-legacy files in the build, as everything is properly
# #ifdef'ed, though it makes the build slightly slower to do so. Note there is only a legacy backend for
# x86 and ARM.

if(CLR_CMAKE_TARGET_ARCH_AMD64)
  set( ARCH_LEGACY_SOURCES
  )
elseif(CLR_CMAKE_TARGET_ARCH_ARM)
  set( ARCH_LEGACY_SOURCES
    codegenlegacy.cpp
    registerfp.cpp
  )
elseif(CLR_CMAKE_TARGET_ARCH_I386)
  set( ARCH_LEGACY_SOURCES
    codegenlegacy.cpp
    stackfp.cpp
  )
elseif(CLR_CMAKE_TARGET_ARCH_ARM64)
  set( ARCH_LEGACY_SOURCES
  )
else()
  clr_unknown_arch()
endif()

set( SOURCES
  ${JIT_SOURCES}
  ${ARCH_SOURCES}
  ${ARCH_LEGACY_SOURCES}
  ${JIT_RESOURCES}
)

convert_to_absolute_path(SOURCES ${SOURCES})

if(WIN32)
  add_precompiled_header(jitpch.h ../jitpch.cpp SOURCES)

  # Create .def file containing a list of exports preceeded by
  # 'EXPORTS'.  The file "ClrJit.exports" already contains the list, so we
  # massage it into the correct format here to create "ClrJit.exports.def".
  set(JIT_EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/ClrJit.exports.def)
  set(JIT_EXPORTS_FILE_TEMP ${JIT_EXPORTS_FILE}.txt)
  file(READ "ClrJit.exports" exports_list)
  file(WRITE ${JIT_EXPORTS_FILE_TEMP} "LIBRARY CLRJIT\n")
  file(APPEND ${JIT_EXPORTS_FILE_TEMP} "EXPORTS\n")
  file(APPEND ${JIT_EXPORTS_FILE_TEMP} ${exports_list})

  # Copy the file only if it has changed.
  execute_process(COMMAND ${CMAKE_COMMAND} -E copy_if_different
    ${JIT_EXPORTS_FILE_TEMP} ${JIT_EXPORTS_FILE})

  set(SHARED_LIB_SOURCES ${SOURCES} ${JIT_EXPORTS_FILE})
else()
  set(JIT_EXPORTS_IN_FILE ${CMAKE_CURRENT_BINARY_DIR}/clrjit.exports.in)
  file(READ "${CMAKE_CURRENT_LIST_DIR}/ClrJit.exports" jit_exports)
  file(READ "${CMAKE_CURRENT_LIST_DIR}/ClrJit.PAL.exports" pal_exports)
  file(WRITE ${JIT_EXPORTS_IN_FILE} ${jit_exports})
  file(APPEND ${JIT_EXPORTS_IN_FILE} "\n")
  file(APPEND ${JIT_EXPORTS_IN_FILE} ${pal_exports})

  set(JIT_EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/clrjit.exports)
  generate_exports_file(${JIT_EXPORTS_IN_FILE} ${JIT_EXPORTS_FILE})

  if(CMAKE_SYSTEM_NAME STREQUAL Linux OR CMAKE_SYSTEM_NAME STREQUAL FreeBSD OR CMAKE_SYSTEM_NAME STREQUAL NetBSD)
    # This is required to force using our own PAL, not one that we are loaded with.
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Xlinker -Bsymbolic -Bsymbolic-functions")

    set(JIT_EXPORTS_LINKER_OPTION -Wl,--version-script=${JIT_EXPORTS_FILE})
  elseif(CMAKE_SYSTEM_NAME STREQUAL Darwin)
    set(JIT_EXPORTS_LINKER_OPTION -Wl,-exported_symbols_list,${JIT_EXPORTS_FILE})
  endif()

  set(SHARED_LIB_SOURCES ${SOURCES})
endif()

add_custom_target(jit_exports DEPENDS ${JIT_EXPORTS_FILE})

set(JIT_BASE_NAME clrjit)
if (CLR_BUILD_JIT32)
  set(JIT_BASE_NAME ryujit)
endif()

if(WIN32)
  add_definitions(-DFX_VER_INTERNALNAME_STR=${JIT_BASE_NAME}.dll)
endif(WIN32)

add_subdirectory(dll)
add_subdirectory(crossgen)
add_subdirectory(standalone)

if (CLR_CMAKE_PLATFORM_ARCH_I386 OR CLR_CMAKE_PLATFORM_ARCH_ARM)
    add_subdirectory(protojit)
endif (CLR_CMAKE_PLATFORM_ARCH_I386 OR CLR_CMAKE_PLATFORM_ARCH_ARM)
