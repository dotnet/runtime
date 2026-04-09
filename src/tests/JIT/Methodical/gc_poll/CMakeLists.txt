project (GCPollNative)

set(SOURCES 
    GCPollNative.cpp 
) 

add_library (GCPollNative SHARED ${SOURCES})

# add the install targets 
install (TARGETS GCPollNative DESTINATION bin) 
