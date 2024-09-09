add_executable(perf-file-rewrite
    perf-file-rewrite.cpp)
target_link_libraries(perf-file-rewrite
    tracepoint-decode)
target_compile_features(perf-file-rewrite
    PRIVATE cxx_std_17)
