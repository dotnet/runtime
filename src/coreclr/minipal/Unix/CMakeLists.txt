set(SOURCES
    ${RUNTIME_DIR}/unix/CLREventBase.cpp
    dn-u16.cpp
    dn-stdio.cpp
    memory.cpp
)

if(NOT CLR_CMAKE_TARGET_ARCH_WASM)
    list(APPEND SOURCES
        doublemapping.cpp
        execmemallocator.cpp
        mapping.cpp
        ${CMAKE_CURRENT_SOURCE_DIR}/../mappingholders.cpp
    )
endif()

add_library(coreclrminipal_objects
    OBJECT
    ${SOURCES}
)

add_library(coreclrminipal
    STATIC
    $<TARGET_OBJECTS:coreclrminipal_objects>
)

target_link_libraries(coreclrminipal PRIVATE minipal)

if (CLR_CMAKE_TARGET_ARCH_WASM)
  install_clr(TARGETS coreclrminipal DESTINATIONS sharedFramework COMPONENT runtime)
endif (CLR_CMAKE_TARGET_ARCH_WASM)
