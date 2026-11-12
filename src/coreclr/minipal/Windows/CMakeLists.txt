set(SOURCES
    ${RUNTIME_DIR}/windows/CLREventBase.cpp
    doublemapping.cpp
    dn-u16.cpp
    dn-stdio.cpp
    memory.cpp
)

if(NOT CLR_CMAKE_TARGET_ARCH_WASM)
    list(APPEND SOURCES
        mapping.cpp
        ${CMAKE_CURRENT_SOURCE_DIR}/../mappingholders.cpp
    )
endif()

add_library(coreclrminipal
    STATIC
    ${SOURCES}
)

target_link_libraries(coreclrminipal PRIVATE minipal)
