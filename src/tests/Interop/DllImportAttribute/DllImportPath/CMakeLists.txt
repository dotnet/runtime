project (DllImportPath)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES
    DllImportPathNative.cpp
)

# Additional files to reference:
# add the executable
add_library (DllImportPath_Local SHARED ${SOURCES})
add_library (DllImportPath.Local SHARED ${SOURCES})
add_library (DllImportPath_PathEnv SHARED ${SOURCES})
add_library (DllImportPath_Relative SHARED ${SOURCES})
add_executable(DllImportPath_ExeFile ${SOURCES})
target_link_libraries(DllImportPath_Local PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
target_link_libraries(DllImportPath.Local PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
target_link_libraries(DllImportPath_PathEnv PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
target_link_libraries(DllImportPath_Relative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
target_link_libraries(DllImportPath_ExeFile PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

target_compile_definitions(DllImportPath_ExeFile PRIVATE EXE)
# add the install targets
install (TARGETS DllImportPath_Local DESTINATION bin)
install (TARGETS DllImportPath.Local DESTINATION bin)
install (TARGETS DllImportPath_PathEnv DESTINATION bin)
install (TARGETS DllImportPath_Relative DESTINATION bin)
