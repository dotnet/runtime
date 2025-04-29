set(SOURCES
    doublemapping.cpp
    dn-u16.cpp
)

if (CLR_CMAKE_HOST_ANDROID)
    add_library(coreclrminipal_objects
        OBJECT
        ${SOURCES}
    )
endif(CLR_CMAKE_HOST_ANDROID)

add_library(coreclrminipal
    STATIC
    ${SOURCES}
)

target_link_libraries(coreclrminipal PRIVATE minipal)
