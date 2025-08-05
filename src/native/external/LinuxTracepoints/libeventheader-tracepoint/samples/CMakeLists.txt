add_executable(eventheader-dynamic-sample
    dynamic-sample.cpp)
target_link_libraries(eventheader-dynamic-sample
    PUBLIC eventheader-tracepoint tracepoint)
target_compile_features(eventheader-dynamic-sample
    PRIVATE cxx_std_17)

add_executable(eventheader-sample
    sample.cpp)
target_link_libraries(eventheader-sample
    PUBLIC eventheader-tracepoint tracepoint)

add_executable(eventheader-tracepoint-sample
    tracepoint-sample.cpp
    TestProviderC.c
    TestProviderCpp.cpp)
target_link_libraries(eventheader-tracepoint-sample
    PUBLIC eventheader-tracepoint tracepoint)
target_compile_features(eventheader-tracepoint-sample
    PRIVATE cxx_std_17)

add_executable(eventheader-interceptor-sample
    interceptor-sample.cpp
    TestProviderC.c
    TestProviderCpp.cpp
    tracepoint-file.cpp)
target_link_libraries(eventheader-interceptor-sample
    PUBLIC eventheader-tracepoint)
target_compile_features(eventheader-interceptor-sample
    PRIVATE cxx_std_17)
