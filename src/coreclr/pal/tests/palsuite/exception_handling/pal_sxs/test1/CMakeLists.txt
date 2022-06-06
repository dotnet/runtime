if(CLR_CMAKE_HOST_UNIX)
    add_definitions(-DFEATURE_ENABLE_HARDWARE_EXCEPTIONS)
endif(CLR_CMAKE_HOST_UNIX)

# Set the RPATH of paltest_pal_sxs_test1 so that it can find dependencies without needing to set LD_LIBRARY
# For more information: http://www.cmake.org/Wiki/CMake_RPATH_handling.
if(CORECLR_SET_RPATH)
  if(CLR_CMAKE_HOST_OSX)
    set(CMAKE_BUILD_WITH_INSTALL_NAME_DIR ON)
    set(CMAKE_INSTALL_NAME_DIR "@rpath")
    set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
    set(CMAKE_INSTALL_RPATH "@loader_path")
  endif(CLR_CMAKE_HOST_OSX)
  if(CLR_CMAKE_HOST_LINUX)
    set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
    set(CMAKE_INSTALL_RPATH "\$ORIGIN")
  endif(CLR_CMAKE_HOST_LINUX)
endif(CORECLR_SET_RPATH)

# Test DLL1

set(DEF_SOURCES1 dlltest1.src)
convert_to_absolute_path(DEF_SOURCES1 ${DEF_SOURCES1})
set(EXPORTS_FILE1 ${CMAKE_CURRENT_BINARY_DIR}/dlltest1.exports)
generate_exports_file(${DEF_SOURCES1} ${EXPORTS_FILE1})

if(CLR_CMAKE_HOST_UNIX)
  set_exports_linker_option(${EXPORTS_FILE1})
endif(CLR_CMAKE_HOST_UNIX)

set(DLL1SOURCES dlltest1.cpp)
add_library_clr(paltest_pal_sxs_test1_dll1 SHARED EXCLUDE_FROM_ALL ${DLL1SOURCES})
add_custom_target(dlltest1_exports DEPENDS ${EXPORTS_FILE1})
set_property(TARGET paltest_pal_sxs_test1_dll1 APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION1})
set_property(TARGET paltest_pal_sxs_test1_dll1 APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE1})

add_dependencies(paltest_pal_sxs_test1_dll1
    dlltest1_exports
    coreclrpal
)

target_link_libraries(paltest_pal_sxs_test1_dll1
    ${COMMON_TEST_LIBRARIES}
    m
    coreclrpal
)

# Test DLL2

set(DEF_SOURCES2 dlltest2.src)
convert_to_absolute_path(DEF_SOURCES2 ${DEF_SOURCES2})
set(EXPORTS_FILE2 ${CMAKE_CURRENT_BINARY_DIR}/dlltest2.exports)
generate_exports_file(${DEF_SOURCES2} ${EXPORTS_FILE2})

if(CLR_CMAKE_TARGET_LINUX OR CLR_CMAKE_TARGET_FREEBSD OR CLR_CMAKE_TARGET_NETBSD)
    set(EXPORTS_LINKER_OPTION2 -Wl,--version-script=${EXPORTS_FILE2})
endif(CLR_CMAKE_TARGET_LINUX OR CLR_CMAKE_TARGET_FREEBSD OR CLR_CMAKE_TARGET_NETBSD)

if(CLR_CMAKE_TARGET_OSX)
    set(EXPORTS_LINKER_OPTION2 -Wl,-exported_symbols_list,${EXPORTS_FILE2})
endif(CLR_CMAKE_TARGET_OSX)

set(DLL2SOURCES dlltest2.cpp)
add_library_clr(paltest_pal_sxs_test1_dll2 SHARED EXCLUDE_FROM_ALL ${DLL2SOURCES})
add_custom_target(dlltest2_exports DEPENDS ${EXPORTS_FILE2})
set_property(TARGET paltest_pal_sxs_test1_dll2 APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION2})
set_property(TARGET paltest_pal_sxs_test1_dll2 APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE2})

add_dependencies(paltest_pal_sxs_test1_dll2
   dlltest2_exports
)

target_link_libraries(paltest_pal_sxs_test1_dll2
   ${COMMON_TEST_LIBRARIES}
)

# Main program

set(TESTSOURCES exceptionsxs.cpp)

add_executable_clr(paltest_pal_sxs_test1 EXCLUDE_FROM_ALL ${TESTSOURCES})

add_dependencies(paltest_pal_sxs_test1
   paltest_pal_sxs_test1_dll1
   paltest_pal_sxs_test1_dll2
)

target_link_libraries(paltest_pal_sxs_test1
   paltest_pal_sxs_test1_dll1
   paltest_pal_sxs_test1_dll2
)

install (TARGETS paltest_pal_sxs_test1 DESTINATION paltests/exception_handling/pal_sxs/test1 COMPONENT paltests EXCLUDE_FROM_ALL)
install (TARGETS paltest_pal_sxs_test1_dll1 DESTINATION paltests/exception_handling/pal_sxs/test1 COMPONENT paltests EXCLUDE_FROM_ALL)
install (TARGETS paltest_pal_sxs_test1_dll2 DESTINATION paltests/exception_handling/pal_sxs/test1 COMPONENT paltests EXCLUDE_FROM_ALL)
add_dependencies(paltests_install paltest_pal_sxs_test1 paltest_pal_sxs_test1_dll1 paltest_pal_sxs_test1_dll2)
