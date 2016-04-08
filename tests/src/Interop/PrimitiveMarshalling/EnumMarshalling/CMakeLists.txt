cmake_minimum_required (VERSION 2.6)
project (MarshalEnumNative)
set(SOURCES MarshalEnumNative.cpp )

# add the executable
add_library (MarshalEnumNative SHARED ${SOURCES})

# add the install targets
install (TARGETS MarshalEnumNative DESTINATION bin)