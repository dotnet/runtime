cmake_minimum_required (VERSION 2.6)
project (VariantNative)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES VariantNative.cpp)

# add the executable
add_library (VariantNative SHARED ${SOURCES})

if(WIN32)
    list(APPEND LINK_LIBRARIES_ADDITIONAL
        OleAut32.lib
    )
endif(WIN32)

target_link_libraries(VariantNative ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS VariantNative DESTINATION bin)
