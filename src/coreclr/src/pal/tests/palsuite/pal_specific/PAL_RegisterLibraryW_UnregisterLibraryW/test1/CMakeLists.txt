set(SOURCES
  PAL_RegisterLibraryW_UnregisterLibraryW.cpp
)

add_executable(paltest_pal_registerlibraryw_unregisterlibraryw_test1
  ${SOURCES}
)

add_dependencies(paltest_pal_registerlibraryw_unregisterlibraryw_test1 coreclrpal)

target_link_libraries(paltest_pal_registerlibraryw_unregisterlibraryw_test1
  ${COMMON_TEST_LIBRARIES}
)
