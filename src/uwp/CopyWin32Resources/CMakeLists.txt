include_directories("${CLI_CMAKE_RESOURCE_DIR}/copywin32resources")

add_compile_options(/MT)


add_executable(CopyWin32Resources
  CopyWin32Resources.cpp
  native.rc
)

target_link_libraries(CopyWin32Resources
  imagehlp.lib
)

install(FILES $<TARGET_FILE:CopyWin32Resources> DESTINATION .)