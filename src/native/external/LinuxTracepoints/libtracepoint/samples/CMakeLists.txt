add_executable(tracepoint-sample
    tracepoint-sample.c)
target_link_libraries(tracepoint-sample
    PUBLIC tracepoint)

add_executable(tpp-sample
    tpp-sample.c)
target_link_libraries(tpp-sample
    PUBLIC tracepoint)
