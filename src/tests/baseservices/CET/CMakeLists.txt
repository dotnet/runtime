add_library(cet_check ${TEST_LIB_TYPE} CETCheck.cpp)

# add the install targets
install (TARGETS cet_check DESTINATION bin)
