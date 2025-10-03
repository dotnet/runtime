project (GCPollNative)

set(SOURCES 
    GCPollNative.cpp 
) 

add_library (GCPollNative ${TEST_LIB_TYPE} ${SOURCES})

# add the install targets 
install (TARGETS GCPollNative DESTINATION bin) 
