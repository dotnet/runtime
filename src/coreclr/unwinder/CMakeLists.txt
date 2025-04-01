# helper to add set of include directories to unwinder targets
macro(add_unwinder_include_directories TARGET)
  target_include_directories(${TARGET} BEFORE PRIVATE ${VM_DIR})
  target_include_directories(${TARGET} BEFORE PRIVATE ${VM_DIR}/${ARCH_SOURCES_DIR})
  target_include_directories(${TARGET} BEFORE PRIVATE ${CMAKE_CURRENT_SOURCE_DIR})
  target_include_directories(${TARGET} BEFORE PRIVATE ${CLR_DIR}/unwinder)
  target_include_directories(${TARGET} PRIVATE ${CLR_DIR}/debug/ee)
  target_include_directories(${TARGET} PRIVATE ${CLR_DIR}/gc)
  target_include_directories(${TARGET} PRIVATE ${CLR_DIR}/gcdump)
  target_include_directories(${TARGET} PRIVATE ${CLR_DIR}/debug/daccess)
  target_include_directories(${TARGET} PRIVATE ${ARCH_SOURCES_DIR})
endmacro()

set(UNWINDER_SOURCES
    baseunwinder.cpp
    ${ARCH_SOURCES_DIR}/unwinder.cpp
)

convert_to_absolute_path(UNWINDER_SOURCES ${UNWINDER_SOURCES})

if(CLR_CMAKE_HOST_UNIX)
    add_library_clr(unwinder_wks OBJECT ${UNWINDER_SOURCES})
    add_unwinder_include_directories(unwinder_wks)
    add_dependencies(unwinder_wks eventing_headers)
endif(CLR_CMAKE_HOST_UNIX)

add_library_clr(unwinder_dac ${UNWINDER_SOURCES})
add_unwinder_include_directories(unwinder_dac)
add_dependencies(unwinder_dac eventing_headers)
set_target_properties(unwinder_dac PROPERTIES DAC_COMPONENT TRUE)
target_compile_definitions(unwinder_dac PRIVATE FEATURE_NO_HOST)

### cDAC Unwinders ####

set(BASE_UNWINDER_SOURCES baseunwinder.cpp)
convert_to_absolute_path(BASE_UNWINDER_SOURCES ${BASE_UNWINDER_SOURCES})
add_library_clr(unwinder_cdac_base STATIC ${BASE_UNWINDER_SOURCES})

target_include_directories(unwinder_cdac_base BEFORE PUBLIC ${VM_DIR})
target_include_directories(unwinder_cdac_base BEFORE PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
target_include_directories(unwinder_cdac_base BEFORE PUBLIC ${CLR_DIR}/unwinder)
target_include_directories(unwinder_cdac_base PUBLIC ${CLR_DIR}/debug/ee)
target_include_directories(unwinder_cdac_base PUBLIC ${CLR_DIR}/gc)
target_include_directories(unwinder_cdac_base PUBLIC ${CLR_DIR}/gcdump)
target_include_directories(unwinder_cdac_base PUBLIC ${CLR_DIR}/debug/daccess)
target_compile_definitions(unwinder_cdac_base PUBLIC FEATURE_NO_HOST FEATURE_CDAC_UNWINDER)

if (CLR_CMAKE_TARGET_WIN32)
  # cDAC unwinders are statically linked into the NativeAOT runtime which is built with
  # release version of the statically linked CRT. Therefore we do the same here.
  set_property(TARGET unwinder_cdac_base PROPERTY MSVC_RUNTIME_LIBRARY MultiThreaded)

  # _DEBUG is always passed as a parameter if the build is a debug build.
  # This causes the debug CRT on MSVC to be used so we need to undefine it.
  target_compile_options(unwinder_cdac_base PRIVATE -U_DEBUG)
endif()

install_clr(TARGETS unwinder_cdac_base DESTINATIONS cdaclibs COMPONENT cdac)

# Helper function for platform specific cDAC uwninder builds.
function(create_platform_unwinder)
  set(oneValueArgs TARGET ARCH)
  set(multiValueArgs DESTINATIONS)
  cmake_parse_arguments(TARGETDETAILS "" "${oneValueArgs}" "${multiValueArgs}" ${ARGN})

  if(TARGETDETAILS_ARCH STREQUAL "x64")
    set(ARCH_SOURCES_DIR amd64)
  elseif((TARGETDETAILS_ARCH STREQUAL "arm") OR (TARGETDETAILS_ARCH STREQUAL "armel"))
    set(ARCH_SOURCES_DIR arm)
  elseif(TARGETDETAILS_ARCH STREQUAL "x86")
    set(ARCH_SOURCES_DIR i386)
  elseif(TARGETDETAILS_ARCH STREQUAL "arm64")
    set(ARCH_SOURCES_DIR arm64)
  else()
    clr_unknown_arch()
  endif()

  set(UNWINDER_SOURCES ${ARCH_SOURCES_DIR}/unwinder.cpp)
  convert_to_absolute_path(UNWINDER_SOURCES ${UNWINDER_SOURCES})
  add_library_clr(${TARGETDETAILS_TARGET} STATIC ${UNWINDER_SOURCES})

  target_include_directories(${TARGETDETAILS_TARGET} BEFORE PRIVATE ${VM_DIR}/${ARCH_SOURCES_DIR})
  target_include_directories(${TARGETDETAILS_TARGET} PRIVATE ${ARCH_SOURCES_DIR})

  target_link_libraries(${TARGETDETAILS_TARGET} PRIVATE unwinder_cdac_base)
  if (CLR_CMAKE_TARGET_WIN32)
    # cDAC unwinders are statically linked into the NativeAOT runtime which is built with
    # release version of the statically linked CRT. Therefore we do the same here.
    set_property(TARGET ${TARGETDETAILS_TARGET} PROPERTY MSVC_RUNTIME_LIBRARY MultiThreaded)

    # _DEBUG is always passed as a parameter if the build is a debug build.
    # This causes the debug CRT on MSVC to be used so we need to undefine it.
    target_compile_options(${TARGETDETAILS_TARGET} PRIVATE -U_DEBUG)
  endif()

  # add the install targets
  install_clr(TARGETS ${TARGETDETAILS_TARGET} DESTINATIONS ${TARGETDETAILS_DESTINATIONS} COMPONENT cdac)

  # Set the target to be built for the specified OS and ARCH
  set_target_definitions_to_custom_os_and_arch(TARGET ${TARGETDETAILS_TARGET} OS win ARCH ${TARGETDETAILS_ARCH})

  target_compile_definitions(${TARGETDETAILS_TARGET} PRIVATE FEATURE_NO_HOST FEATURE_CDAC_UNWINDER)
endfunction()

if(CLR_CMAKE_TARGET_WIN32 AND CLR_CMAKE_TARGET_ARCH_AMD64)
  create_platform_unwinder(TARGET unwinder_cdac_amd64 ARCH x64 DESTINATIONS cdaclibs)
  create_platform_unwinder(TARGET unwinder_cdac_arm64 ARCH arm64 DESTINATIONS cdaclibs)
endif(CLR_CMAKE_TARGET_WIN32 AND CLR_CMAKE_TARGET_ARCH_AMD64)

if(CLR_CMAKE_TARGET_WIN32 AND CLR_CMAKE_TARGET_ARCH_ARM64)
  create_platform_unwinder(TARGET unwinder_cdac_arm64 ARCH arm64 DESTINATIONS cdaclibs)
endif(CLR_CMAKE_TARGET_WIN32 AND CLR_CMAKE_TARGET_ARCH_ARM64)

if(NOT CLR_CMAKE_TARGET_WIN32 AND CLR_CMAKE_TARGET_ARCH_AMD64)
  create_platform_unwinder(TARGET unwinder_cdac_amd64 ARCH x64 DESTINATIONS cdaclibs)
endif(NOT CLR_CMAKE_TARGET_WIN32 AND CLR_CMAKE_TARGET_ARCH_AMD64)

if(NOT CLR_CMAKE_TARGET_WIN32 AND CLR_CMAKE_TARGET_ARCH_ARM64)
  create_platform_unwinder(TARGET unwinder_cdac_arm64 ARCH arm64 DESTINATIONS cdaclibs)
endif(NOT CLR_CMAKE_TARGET_WIN32 AND CLR_CMAKE_TARGET_ARCH_ARM64)

