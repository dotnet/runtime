project(jitstructtests)

set(SOURCES structinregs.cpp structinregs.def)
add_library(jitstructtests_lib ${TEST_LIB_TYPE} ${SOURCES})
