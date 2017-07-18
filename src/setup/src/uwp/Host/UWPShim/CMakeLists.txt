include(../../../settings.cmake)

add_definitions(-DUNICODE)
add_definitions(-D_UNICODE)
set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /SUBSYSTEM:WINDOWS")
set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /ENTRY:wmain")
set(CMAKE_EXE_LINKER_FLAGS_DEBUG "/NODEFAULTLIB:libucrt.lib /DEFAULTLIB:ucrt.lib")

include_directories("${CLI_CMAKE_RESOURCE_DIR}/uwpshim")
link_directories(${UWPHOST_LIB_PATH})
set (PLATFORM STORE)

add_executable(UWPShim
  uwpshim.cpp
  native.rc
)

target_link_libraries(UWPShim
    UWPHost
    vcruntime
)

install(TARGETS UWPShim DESTINATION .)
