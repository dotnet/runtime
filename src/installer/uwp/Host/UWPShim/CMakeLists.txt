include_directories("${CLI_CMAKE_RESOURCE_DIR}/uwpshim") 
add_executable(UWPShim
  uwpshim.cpp
  native.rc
)

target_link_libraries(UWPShim
    UWPHost
)

install(TARGETS UWPShim DESTINATION .)
