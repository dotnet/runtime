add_definitions(-MT)
add_executable(GenClrDebugResource GenClrDebugResource.cpp)

target_link_libraries(GenClrDebugResource
    ${STATIC_MT_CRT_LIB}
    ${STATIC_MT_VCRT_LIB}
)

install(TARGETS GenClrDebugResource EXPORT dactabletools DESTINATION dactabletools)
