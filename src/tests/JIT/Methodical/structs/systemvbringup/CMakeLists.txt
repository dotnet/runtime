project(jitstructtests)

set(CMAKE_SHARED_LIBRARY_PREFIX "")

set(SOURCES structinregs.cpp structinregs.def)
add_library(jitstructtests_lib SHARED ${SOURCES})

# add the install targets (this "installs" the native file on Windows systems)
install(TARGETS jitstructtests_lib DESTINATION bin)

# This "installs" the native file on System V systems
set_target_properties(jitstructtests_lib PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/structinregs)
