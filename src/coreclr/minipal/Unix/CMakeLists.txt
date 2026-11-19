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

# Unix implementations of the Microsoft CRT secure functions (sprintf_s, sscanf_s, memcpy_s, ...)
# that Windows gets from its CRT. Linked into the PAL so all of its consumers get them.
add_library(coreclrsafecrt_objects OBJECT safecrt.cpp)
target_include_directories(coreclrsafecrt_objects PRIVATE ${CLR_DIR}/pal/inc)

if (TARGET coreclrpal)
    target_sources(coreclrpal PRIVATE $<TARGET_OBJECTS:coreclrsafecrt_objects>)
endif()
