 
cmake_minimum_required (VERSION 2.6) 
project (MAWSPINative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES 
    MAWSPINative.cpp 
)
# add the executable 
add_library (MAWSPINative SHARED ${SOURCES}) 
# add the install targets 
install (TARGETS MAWSPINative DESTINATION bin) 
