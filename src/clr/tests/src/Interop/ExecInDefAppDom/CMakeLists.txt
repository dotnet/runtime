cmake_minimum_required (VERSION 2.6)
project (ExecInDefAppDomDll)
set(SOURCES ExecInDefAppDomDll.cpp )

# add the executable
add_library (ExecInDefAppDomDll SHARED ${SOURCES})
target_link_libraries(ExecInDefAppDomDll ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS ExecInDefAppDomDll DESTINATION bin)
