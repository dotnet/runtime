project(ClrJit)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

include_directories("./jitstd")
include_directories("../inc")

# Enable the following for UNIX altjit on Windows
# add_definitions(-DALT_JIT)

set( SOURCES
  alloc.cpp
  bitset.cpp
  block.cpp
  compiler.cpp
  disasm.cpp
  eeinterface.cpp
  ee_il_dll.cpp
  jiteh.cpp
  error.cpp
  flowgraph.cpp
  gcinfo.cpp
  gcdecode.cpp
  gcencode.cpp
  gentree.cpp
  gschecks.cpp
  hashbv.cpp
  importer.cpp
  instr.cpp
  lclvars.cpp
  liveness.cpp
  morph.cpp
  optimizer.cpp
  optcse.cpp
  rationalize.cpp
  regalloc.cpp
  regset.cpp
  register_arg_convention.cpp
  emit.cpp
  scopeinfo.cpp
  sharedfloat.cpp
  sm.cpp
  smdata.cpp
  smweights.cpp
  typeinfo.cpp
  unwind.cpp
  utils.cpp
  ssabuilder.cpp
  ssarenamestate.cpp
  valuenum.cpp
  copyprop.cpp
  codegencommon.cpp
  assertionprop.cpp
  rangecheck.cpp
  loopcloning.cpp
  lower.cpp
  lsra.cpp
  emitxarch.cpp
  targetamd64.cpp
  lowerxarch.cpp
  codegenxarch.cpp
  simd.cpp
  simdcodegenxarch.cpp
  unwindamd64.cpp
)

if( WIN32 )

  add_precompiled_header(jitpch.h jitpch.cpp SOURCES)

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

# Disable the following for UNIX altjit on Windows
if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-Wno-trigraphs)
    add_compile_options(-fPIC)
    add_definitions(-DFEATURE_MERGE_JIT_AND_ENGINE)

    add_library(ClrJit
      STATIC
      ${SHARED_LIB_SOURCES}
    )
    add_dependencies(ClrJit coreclrpal gcinfo)
else()
    add_library(ClrJit
       ${SOURCES}
    )
# Disable up to here (see above) the following for UNIX altjit on Windows 
# Enable the following for UNIX altjit on Windows
#    add_library(ClrJit
#       SHARED
#       ${SHARED_LIB_SOURCES}
#    )

# Enable the following for UNIX altjit on Windows
#target_link_libraries(ClrJit
#      utilcode
#      gcinfo
#      runtime_library
#    )

# Disable the following for UNIX altjit on Windows
endif(CLR_CMAKE_PLATFORM_UNIX)
