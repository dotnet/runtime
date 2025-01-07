add_executable(tracepoint-register
    tracepoint-register.cpp)
target_link_libraries(tracepoint-register
    PUBLIC tracepoint)
install(TARGETS tracepoint-register)
