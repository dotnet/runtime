include(${CLR_DIR}/dac.cmake)

add_definitions(-DSELF_NO_HOST)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_library_clr(utilcode_dac STATIC ${UTILCODE_DAC_SOURCES})
    target_link_libraries(utilcode_dac nativeresourcestring)
    add_dependencies(utilcode_dac coreclrpal)
else()
    add_precompiled_header(stdafx.h ../stdafx.cpp UTILCODE_DAC_SOURCES)
    add_library_clr(utilcode_dac STATIC ${UTILCODE_DAC_SOURCES})
endif(CLR_CMAKE_PLATFORM_UNIX)

add_dependencies(utilcode_dac ${UTILCODE_DEPENDENCIES})
