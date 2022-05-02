project(jitstructtests)

set(SOURCES structinregs.cpp structinregs.def)
add_library(jitstructtests_lib SHARED ${SOURCES})
