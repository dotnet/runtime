add_executable_clr(watchdog ${CMAKE_CURRENT_LIST_DIR}/watchdog.cpp)
install_clr(TARGETS watchdog DESTINATIONS . COMPONENT hosts)
install_clr(TARGETS watchdog DESTINATIONS . COMPONENT nativeaot)

