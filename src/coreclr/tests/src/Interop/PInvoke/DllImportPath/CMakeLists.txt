cmake_minimum_required (VERSION 2.6) 
project (DllImportPath) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
include_directories(${INC_PLATFORM_DIR}) 
set(SOURCES 
    DllImportPathNative.cpp 
) 

# Set CMP0037 to OLD for this directory, so that
# CMake doesn't complain about the libraries with Unicode characters.
if(NOT (CMAKE_MAJOR_VERSION LESS 3)) 
    cmake_policy(SET CMP0037 OLD)
endif()
  
# Additional files to reference: 
# add the executable 
add_library (DllImportPath_Local SHARED ${SOURCES})
add_library (DllImportPath.Local SHARED ${SOURCES})
add_library (DllImportPath_PathEnv SHARED ${SOURCES})
add_library (DllImportPath_Relative SHARED ${SOURCES})
add_library (DllImportPath_U�n�i�c�o�d�e SHARED ${SOURCES}) 
target_link_libraries(DllImportPath_Local ${LINK_LIBRARIES_ADDITIONAL}) 
target_link_libraries(DllImportPath.Local ${LINK_LIBRARIES_ADDITIONAL}) 
target_link_libraries(DllImportPath_PathEnv ${LINK_LIBRARIES_ADDITIONAL}) 
target_link_libraries(DllImportPath_Relative ${LINK_LIBRARIES_ADDITIONAL}) 
target_link_libraries(DllImportPath_U�n�i�c�o�d�e ${LINK_LIBRARIES_ADDITIONAL}) 
# add the install targets 
install (TARGETS DllImportPath_Local DESTINATION bin) 
install (TARGETS DllImportPath.Local DESTINATION bin) 
install (TARGETS DllImportPath_PathEnv DESTINATION bin) 
install (TARGETS DllImportPath_Relative DESTINATION bin) 
install (TARGETS DllImportPath_U�n�i�c�o�d�e DESTINATION bin) 
