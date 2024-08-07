add_executable(decode-perf
    decode-perf.cpp)
target_link_libraries(decode-perf
    eventheader-decode
    tracepoint-decode)
target_compile_features(decode-perf
    PRIVATE cxx_std_17)
install(TARGETS decode-perf)
