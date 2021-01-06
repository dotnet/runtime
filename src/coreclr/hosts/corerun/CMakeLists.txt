project(CoreRun)

set(CMAKE_INCLUDE_CURRENT_DIR ON)
add_definitions(-DFX_VER_INTERNALNAME_STR=CoreRun.exe)

_add_executable(CoreRun
  corerun.cpp logger.cpp
  native.rc
)

target_link_libraries(CoreRun
    utilcodestaticnohost
    advapi32.lib
    oleaut32.lib
    uuid.lib
    user32.lib
    ${STATIC_MT_CRT_LIB}
    ${STATIC_MT_VCRT_LIB}
)

install_clr(TARGETS CoreRun)
