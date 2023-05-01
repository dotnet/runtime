project (IndirectNative)
set(SOURCES IndirectNative.cpp)
add_library (IndirectNative SHARED ${SOURCES})
# add the install targets
install (TARGETS IndirectNative DESTINATION bin)
