add_library(cet_check SHARED CETCheck.cpp)

# add the install targets
install (TARGETS cet_check DESTINATION bin)
