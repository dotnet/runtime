add_executable(tracepoint-collect
    tracepoint-collect.cpp)
target_link_libraries(tracepoint-collect
    tracepoint-control tracepoint-decode)
target_compile_features(tracepoint-collect
    PRIVATE cxx_std_17)
install(TARGETS tracepoint-collect)
