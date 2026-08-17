set(SOURCES
    doublemapping.cpp
    dn-u16.cpp
    dn-stdio.cpp
    memory.cpp
)

if (NOT CLR_CMAKE_TARGET_ARCH_WASM)
    list(APPEND SOURCES wait.cpp)
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

if (HAVE_PIPE2)
    target_compile_definitions(coreclrminipal_objects PRIVATE HAVE_PIPE2=1)
else()
    target_compile_definitions(coreclrminipal_objects PRIVATE HAVE_PIPE2=0)
endif()

if (CLR_CMAKE_TARGET_ARCH_WASM)
  install_clr(TARGETS coreclrminipal DESTINATIONS sharedFramework COMPONENT runtime)
endif (CLR_CMAKE_TARGET_ARCH_WASM)
