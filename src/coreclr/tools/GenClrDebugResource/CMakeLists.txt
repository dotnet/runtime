set(CMAKE_MSVC_RUNTIME_LIBRARY MultiThreaded)
add_executable(GenClrDebugResource GenClrDebugResource.cpp)

target_link_libraries(GenClrDebugResource
    ${STATIC_MT_CRT_LIB}
    ${STATIC_MT_VCRT_LIB}
)

install(TARGETS GenClrDebugResource EXPORT dactabletools DESTINATION dactabletools COMPONENT crosscomponents)
