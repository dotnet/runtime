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

  set(UNWINDER_SOURCES
    baseunwinder.cpp
    ${ARCH_SOURCES_DIR}/unwinder.cpp
  )

  convert_to_absolute_path(UNWINDER_SOURCES ${UNWINDER_SOURCES})

  add_library_clr(${TARGETDETAILS_TARGET}
    SHARED
    ${UNWINDER_SOURCES}
  )

  add_unwinder_include_directories(${TARGETDETAILS_TARGET})

  target_link_libraries(${TARGETDETAILS_TARGET} PRIVATE  ${STATIC_MT_CRT_LIB} ${STATIC_MT_VCRT_LIB})

  # add the install targets
  install_clr(TARGETS ${TARGETDETAILS_TARGET} DESTINATIONS ${TARGETDETAILS_DESTINATIONS} COMPONENT debug INSTALL_ALL_ARTIFACTS)

  # Set the target to be built for the specified OS and ARCH
  set_target_definitions_to_custom_os_and_arch(TARGET ${TARGETDETAILS_TARGET} OS win ARCH ${TARGETDETAILS_ARCH})

  target_compile_definitions(${TARGETDETAILS_TARGET} PRIVATE FEATURE_NO_HOST FEATURE_CDAC_UNWINDER)
endfunction()

# TODO: Support building cDAC unwinders on other platforms
# https://github.com/dotnet/runtime/issues/112272#issue-2838611496
if(CLR_CMAKE_TARGET_WIN32 AND CLR_CMAKE_TARGET_ARCH_AMD64)
  create_platform_unwinder(TARGET unwinder_cdac_amd64 ARCH x64 DESTINATIONS cdaclibs)
  create_platform_unwinder(TARGET unwinder_cdac_arm64 ARCH arm64 DESTINATIONS cdaclibs)
endif(CLR_CMAKE_TARGET_WIN32 AND CLR_CMAKE_TARGET_ARCH_AMD64)
