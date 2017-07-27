include_directories("${CLI_CMAKE_RESOURCE_DIR}/uwphost")

add_compile_options(/MT)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(UWPHost_SOURCES 
    UWPHost.cpp 
    HostEnvironment.cpp
    UWPHost.def
)

set(UWPHost_RESOURCES native.rc)

add_library(UWPHost
  SHARED
  ${UWPHost_SOURCES}
  ${UWPHost_RESOURCES}
)

target_link_libraries(UWPHost
    onecore.lib
)

install(TARGETS UWPHost RUNTIME DESTINATION .)
