project(mini)

function(addprefix var prefix list)
  foreach(i ${list})
    set(f ${f} ${prefix}/${i})
  endforeach()
  set(${var} ${f} PARENT_SCOPE)
endfunction()

include_directories(
  ${PROJECT_BINARY_DIR}/
  ${PROJECT_BINARY_DIR}/../..
  ${PROJECT_BINARY_DIR}/../../mono/eglib
  ${CMAKE_SOURCE_DIR}/
  ${PROJECT_SOURCE_DIR}/../
  ${PROJECT_SOURCE_DIR}/../eglib
  ${PROJECT_SOURCE_DIR}/../sgen)

#
# SUBDIRS
#
include(../eglib/CMakeLists.txt)
include(../utils/CMakeLists.txt)
include(../metadata/CMakeLists.txt)
include(../sgen/CMakeLists.txt)
include(../eventpipe/CMakeLists.txt)

# ICU
if(HAVE_SYS_ICU)
if(STATIC_ICU)
set(pal_icushim_sources_base
    pal_icushim_static.c)
else()
set(pal_icushim_sources_base
    pal_icushim.c)
endif()

set(icu_shim_sources_base
    pal_calendarData.c
    pal_casing.c
    pal_collation.c
    pal_idna.c
    pal_locale.c
    pal_localeNumberData.c
    pal_localeStringData.c
    pal_normalization.c
    pal_timeZoneInfo.c
    entrypoints.c
    ${pal_icushim_sources_base})
addprefix(icu_shim_sources "${ICU_SHIM_PATH}" "${icu_shim_sources_base}")
set_source_files_properties(${icu_shim_sources} PROPERTIES COMPILE_DEFINITIONS OSX_ICU_LIBRARY_PATH="${OSX_ICU_LIBRARY_PATH}")
set_source_files_properties(${icu_shim_sources} PROPERTIES COMPILE_FLAGS "-I${ICU_INCLUDEDIR} -I${CMAKE_SOURCE_DIR}/../libraries/Native/Unix/System.Globalization.Native/ -I${CMAKE_SOURCE_DIR}/../libraries/Native/Unix/Common/ ${ICU_FLAGS}")
set(ICU_LDFLAGS "-L${ICU_LIBDIR}")
endif()

#
# MINI
#

set(mini_common_sources
    mini.c
    mini-runtime.c
    seq-points.c
    seq-points.h
    ir-emit.h
    method-to-ir.c
    cfgdump.h
    cfgdump.c
    calls.c
    decompose.c
    mini.h
    version.h
    optflags-def.h
    jit-icalls.h
    jit-icalls.c
    trace.c
    trace.h
    patch-info.h
    mini-ops.h
    mini-arch.h
    dominators.c
    cfold.c
    regalloc.h
    helpers.c
    liveness.c
    ssa.c
    abcremoval.c
    abcremoval.h
    local-propagation.c
    driver.c
    debug-mini.c
    linear-scan.c
    aot-compiler.h
    aot-compiler.c
    aot-runtime.c
    graph.c
    mini-codegen.c
    mini-exceptions.c
    mini-trampolines.c
    branch-opts.c
    mini-generic-sharing.c
    simd-methods.h
    tasklets.c
    tasklets.h
    simd-intrinsics.c
    mini-native-types.c
    mini-unwind.h
    unwind.c
    image-writer.h
    image-writer.c
    dwarfwriter.h
    dwarfwriter.c
    mini-gc.h
    mini-gc.c
    xdebug.c
    mini-llvm.h
    mini-llvm-cpp.h
    llvm-jit.h
    alias-analysis.c
    mini-cross-helpers.c
    arch-stubs.c
    llvm-runtime.h
    llvm-intrinsics.h
    type-checking.c
    lldb.h
    lldb.c
    memory-access.c
    intrinsics.c
    mini-profiler.c
    interp-stubs.c
    aot-runtime.h
    ee.h
    mini-runtime.h
    llvmonly-runtime.h
    llvmonly-runtime.c
    simd-intrinsics-netcore.c
    monovm.h
    monovm.c)

set(debugger_sources
  debugger-engine.h
  debugger-engine.c
  debugger-agent.h
  debugger-agent.c
  debugger-agent-stubs.c
  debugger-state-machine.h
  debugger-state-machine.c)

set(amd64_sources
    mini-amd64.c
    mini-amd64.h
    exceptions-amd64.c
    tramp-amd64.c
    mini-amd64-gsharedvt.c
    mini-amd64-gsharedvt.h
    tramp-amd64-gsharedvt.c
    cpu-amd64.h)

set(x86_sources
    mini-x86.c
    mini-x86.h
    exceptions-x86.c
    tramp-x86.c
    mini-x86-gsharedvt.c
    tramp-x86-gsharedvt.c
    cpu-x86.h)

set(arm64_sources
    mini-arm64.c
    mini-arm64.h
    exceptions-arm64.c
    tramp-arm64.c
    mini-arm64-gsharedvt.c
    mini-arm64-gsharedvt.h
    tramp-arm64-gsharedvt.c
    cpu-arm64.h)

set(arm_sources
    mini-arm.c
    mini-arm.h
    exceptions-arm.c
    tramp-arm.c
    mini-arm-gsharedvt.c
    tramp-arm-gsharedvt.c
    cpu-arm.h)

set(wasm_sources
    mini-wasm.c
    tramp-wasm.c
    exceptions-wasm.c
    aot-runtime-wasm.c
    mini-wasm-debugger.c
    wasm_m2n_invoke.g.h
    cpu-wasm.h)

if(TARGET_AMD64)
set(arch_sources ${amd64_sources})
elseif(TARGET_X86)
set(arch_sources ${x86_sources})
elseif(TARGET_ARM64)
set(arch_sources ${arm64_sources})
elseif(TARGET_ARM)
set(arch_sources ${arm_sources})
elseif(TARGET_WASM)
set(arch_sources ${wasm_sources})
endif()

set(darwin_sources
    mini-darwin.c)

set(windows_sources
    mini-windows.c
    mini-windows.h
    mini-windows-dllmain.c
    mini-windows-dlldac.c)

set(posix_sources
    mini-posix.c)

if(HOST_DARWIN)
set(os_sources "${darwin_sources};${posix_sources}")
elseif(HOST_LINUX)
set(os_sources "${posix_sources}")
endif()

set(interp_sources
    interp/interp.h
    interp/interp-internals.h
    interp/interp.c
    interp/interp-intrins.h
    interp/interp-intrins.c
    interp/mintops.h
    interp/mintops.c
    interp/transform.c)
set(interp_stub_sources
    interp-stubs.c)

if(NOT DISABLE_INTERPRETER)
set(mini_interp_sources ${interp_sources})
else()
set(mini_interp_sources ${interp_stub_sources})
endif()

if(ENABLE_INTERP_LIB)
add_library(mono-ee-interp STATIC "${interp_sources}")
install(TARGETS mono-ee-interp LIBRARY)
endif()

if(ENABLE_LLVM)
set(llvm_sources
    mini-llvm.c
    mini-llvm-cpp.cpp
    llvm-jit.cpp)
endif()

if(ENABLE_LLVM)
set(llvm_runtime_sources
    llvm-runtime.cpp)
elseif(ENABLE_LLVM_RUNTIME)
set(llvm_runtime_sources
    llvm-runtime.cpp)
endif()

set(mini_public_headers_base
  jit.h
  mono-private-unstable.h)
addprefix(mini_public_headers ../mini "${mini_public_headers_base}")

set(mini_sources "${CMAKE_CURRENT_BINARY_DIR}/buildver-sgen.h;main-core.c;${mini_common_sources};${arch_sources};${os_sources};${mini_interp_sources};${llvm_sources};${debugger_sources};${llvm_runtime_sources}")

if(LLVM_LIBDIR)
  link_directories(${LLVM_LIBDIR})
endif()

if(HOST_DARWIN)
set(OS_LIBS "-framework CoreFoundation" "-framework Foundation")
elseif(HOST_IOS)
set(OS_LIBS "-framework CoreFoundation" "-lobjc" "-lc++")
elseif(HOST_LINUX)
set(OS_LIBS pthread m dl)
endif()

add_library(monosgen-objects OBJECT "${eglib_sources};${metadata_sources};${utils_sources};${sgen_sources};${icu_shim_sources};${eventpipe_sources};${mini_sources}")
add_library(monosgen-static STATIC $<TARGET_OBJECTS:monosgen-objects>)
set_target_properties(monosgen-static PROPERTIES OUTPUT_NAME monosgen-2.0)
if(NOT DISABLE_LIBS)
  install(TARGETS monosgen-static LIBRARY)
  install(FILES ${metadata_public_headers}
	DESTINATION ${CMAKE_INSTALL_INCLUDEDIR}/mono-2.0/mono/metadata)
  install(FILES ${utils_public_headers}
	DESTINATION ${CMAKE_INSTALL_INCLUDEDIR}/mono-2.0/mono/utils)
  install(FILES ${mini_public_headers}
	DESTINATION ${CMAKE_INSTALL_INCLUDEDIR}/mono-2.0/mono/jit)
endif()
if(NOT DISABLE_SHARED_LIBS)
  add_library(monosgen SHARED $<TARGET_OBJECTS:monosgen-objects>)
  set_target_properties(monosgen PROPERTIES OUTPUT_NAME monosgen-2.0)
  set_target_properties(monosgen PROPERTIES LIBRARY_OUTPUT_DIRECTORY .libs)
  target_link_libraries(monosgen ${OS_LIBS} ${ICONV_LIB} ${LLVM_LIBS} ${ICU_LIBS})
  if(ICU_LDFLAGS)
    set_target_properties(monosgen PROPERTIES LINK_FLAGS ${ICU_LDFLAGS})
  endif()
  install(TARGETS monosgen LIBRARY)
endif()

# FIXME: Always rebuilds, creates non-deterministic builds
# FIXME: Use the previous format
#string(TIMESTAMP BUILD_DATE)

#add_custom_command(
#  OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/buildver-sgen.h
#  COMMAND echo "const char *build_date = \"${BUILD_DATE}\";" > ${CMAKE_CURRENT_BINARY_DIR}/buildver-sgen.h
#  VERBATIM
#)
add_custom_command(
  OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/buildver-sgen.h
  COMMAND echo "const char *build_date = \"\";" > ${CMAKE_CURRENT_BINARY_DIR}/buildver-sgen.h
  VERBATIM
)

add_custom_command(
  OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/version.h
  COMMAND ${CMAKE_CURRENT_SOURCE_DIR}/gen-version-h.sh ${CMAKE_CURRENT_SOURCE_DIR}/../..
  VERBATIM
)

add_custom_command(
  OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/cpu-amd64.h
  COMMAND python ${CMAKE_CURRENT_SOURCE_DIR}/genmdesc.py TARGET_AMD64 ${CMAKE_CURRENT_SOURCE_DIR} cpu-amd64.h amd64_desc ${CMAKE_CURRENT_SOURCE_DIR}/cpu-amd64.md
  VERBATIM
)

add_custom_command(
  OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/cpu-x86.h
  COMMAND python ${CMAKE_CURRENT_SOURCE_DIR}/genmdesc.py TARGET_X86 ${CMAKE_CURRENT_SOURCE_DIR} cpu-x86.h x86_desc ${CMAKE_CURRENT_SOURCE_DIR}/cpu-x86.md
  VERBATIM
)

add_custom_command(
  OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/cpu-arm64.h
  COMMAND python ${CMAKE_CURRENT_SOURCE_DIR}/genmdesc.py TARGET_ARM64 ${CMAKE_CURRENT_SOURCE_DIR} cpu-arm64.h arm64_cpu_desc ${CMAKE_CURRENT_SOURCE_DIR}/cpu-arm64.md
  VERBATIM
)

add_custom_command(
  OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/cpu-arm.h
  COMMAND python ${CMAKE_CURRENT_SOURCE_DIR}/genmdesc.py TARGET_ARM ${CMAKE_CURRENT_SOURCE_DIR} cpu-arm.h arm_cpu_desc ${CMAKE_CURRENT_SOURCE_DIR}/cpu-arm.md
  VERBATIM
)

add_custom_command(
  OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/cpu-wasm.h
  COMMAND python ${CMAKE_CURRENT_SOURCE_DIR}/genmdesc.py TARGET_WASM ${CMAKE_CURRENT_SOURCE_DIR} cpu-wasm.h wasm_desc ${CMAKE_CURRENT_SOURCE_DIR}/cpu-wasm.md
  VERBATIM
)

if(NOT DISABLE_EXECUTABLES)
set(CMAKE_SKIP_RPATH 1)
add_executable(mono-sgen "main-sgen.c")
target_link_libraries(mono-sgen monosgen-static ${OS_LIBS} ${ICONV_LIB} ${LLVM_LIBS} ${ICU_LIBS})
if(ICU_LDFLAGS)
  set_target_properties(mono-sgen PROPERTIES LINK_FLAGS ${ICU_LDFLAGS})
endif()
install(TARGETS mono-sgen RUNTIME)
endif()
