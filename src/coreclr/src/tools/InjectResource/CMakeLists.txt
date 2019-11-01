remove_definitions(-DUNICODE)
remove_definitions(-D_UNICODE)
add_definitions(-MT)

add_executable(InjectResource InjectResource.cpp)

target_link_libraries(InjectResource
    ${STATIC_MT_CRT_LIB}
    ${STATIC_MT_VCRT_LIB}
)

install(TARGETS InjectResource EXPORT dactabletools DESTINATION dactabletools)
