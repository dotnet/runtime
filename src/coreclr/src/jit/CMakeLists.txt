set(CMAKE_INCLUDE_CURRENT_DIR ON)

include_directories("./jitstd")
include_directories("../inc")

# Enable the following for UNIX altjit on Windows
# add_definitions(-DALT_JIT)

if (CLR_CMAKE_PLATFORM_ARCH_AMD64)
  add_definitions(-DFEATURE_SIMD) 
  add_definitions(-DFEATURE_AVX_SUPPORT) 
endif (CLR_CMAKE_PLATFORM_ARCH_AMD64)

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
  importer.cpp
  inline.cpp
  inlinepolicy.cpp
  instr.cpp
  jitconfig.cpp
  jiteh.cpp
  jittelemetry.cpp
  lclvars.cpp
  liveness.cpp
  loopcloning.cpp
  lower.cpp
  lsra.cpp
  morph.cpp
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

if(CLR_CMAKE_PLATFORM_ARCH_AMD64)
  set( ARCH_SOURCES
    targetamd64.cpp
    unwindamd64.cpp
    emitxarch.cpp
    lowerxarch.cpp
    codegenxarch.cpp
    simdcodegenxarch.cpp
    simd.cpp
  )
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
  set( ARCH_SOURCES
    emitarm.cpp
    targetarm.cpp
    lowerarm.cpp
    codegenarm.cpp
    unwindarm.cpp
    codegenlegacy.cpp
    registerfp.cpp
  )
elseif(CLR_CMAKE_PLATFORM_ARCH_I386)
  set( ARCH_SOURCES
    emitxarch.cpp
    targetx86.cpp
    lowerxarch.cpp
    codegenxarch.cpp
    codegenlegacy.cpp
    stackfp.cpp
  )
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM64)
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

set( SOURCES
  ${JIT_SOURCES}
  ${ARCH_SOURCES}
 )

convert_to_absolute_path(SOURCES ${SOURCES})

if( WIN32 )

  add_precompiled_header(jitpch.h ../jitpch.cpp SOURCES)

  # Create .def file containing a list of exports preceeded by
  # 'EXPORTS'.  The file "ClrJit.exports" already contains the list, so we
  # massage it into the correct format here to create "ClrJit.exports.def".
  set(CLRJIT_EXPORTS_DEF ${CMAKE_CURRENT_BINARY_DIR}/ClrJit.exports.def)
  set(CLRJIT_EXPORTS_DEF_TEMP ${CLRJIT_EXPORTS_DEF}.txt)
  file(READ "ClrJit.exports" exports_list)
  file(WRITE ${CLRJIT_EXPORTS_DEF_TEMP} "LIBRARY CLRJIT\n")
  file(APPEND ${CLRJIT_EXPORTS_DEF_TEMP} "EXPORTS\n")
  file(APPEND ${CLRJIT_EXPORTS_DEF_TEMP} ${exports_list})

  # Copy the file only if it has changed.
  execute_process(COMMAND ${CMAKE_COMMAND} -E copy_if_different
    ${CLRJIT_EXPORTS_DEF_TEMP} ${CLRJIT_EXPORTS_DEF})

  set(SHARED_LIB_SOURCES ${SOURCES} ${CLRJIT_EXPORTS_DEF})
else()
  set(SHARED_LIB_SOURCES ${SOURCES})
endif()

set(CLR_EXPORTED_SYMBOL_FILE ${CLRJIT_EXPORTS_DEF})

add_subdirectory(dll)
add_subdirectory(crossgen)
add_subdirectory(standalone)

if (CLR_CMAKE_PLATFORM_ARCH_I386 OR CLR_CMAKE_PLATFORM_ARCH_ARM)
    add_subdirectory(protojit)
endif (CLR_CMAKE_PLATFORM_ARCH_I386 OR CLR_CMAKE_PLATFORM_ARCH_ARM)
