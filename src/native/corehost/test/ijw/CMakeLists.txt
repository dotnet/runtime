# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

include(${CLR_ENG_NATIVE_DIR}/ijw/IJW.cmake)

add_library(ijw SHARED ijw.cpp)
target_link_libraries(ijw PRIVATE ijwhost)

remove_ijw_incompatible_target_options(ijw)
add_ijw_msbuild_project_properties(ijw ijwhost)

install_with_stripped_symbols(ijw TARGETS corehost_test)

if (CMAKE_BUILD_TYPE STREQUAL "Debug")
  # Copy over the debug CRT so that it is available for test runs
  file(TO_CMAKE_PATH "$ENV{VCToolsRedistDir}" CMAKE_VS_VCTOOLSREDISTDIR)
  file(TO_CMAKE_PATH "$ENV{ExtensionSdkDir}" CMAKE_VS_EXTENSIONSDKDIR)
  file(
    GLOB_RECURSE
    DEBUG_CRT_FILES
    ${CMAKE_VS_VCTOOLSREDISTDIR}/onecore/debug_nonredist/${ARCH_TARGET_NAME}/Microsoft.VC*.DebugCRT/vcruntime*d.dll
    ${CMAKE_VS_VCTOOLSREDISTDIR}/onecore/debug_nonredist/${ARCH_TARGET_NAME}/Microsoft.VC*.DebugCRT/msvcp*d.dll
    ${CMAKE_VS_EXTENSIONSDKDIR}/Microsoft.UniversalCRT.Debug/$ENV{UCRTVersion}/Redist/Debug/${ARCH_TARGET_NAME}/ucrtbased.dll
  )
  install(FILES ${DEBUG_CRT_FILES} DESTINATION corehost_test/ijw-deps)
endif ()
