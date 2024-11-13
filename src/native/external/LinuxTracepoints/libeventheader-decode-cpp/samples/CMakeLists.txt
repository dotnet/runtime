add_executable(eventheader-decode-file
    decode-file.cpp)
target_link_libraries(eventheader-decode-file
    eventheader-decode)

if(NOT WIN32)

    find_library(TRACEFS_LIBRARIES tracefs)
    find_library(TRACEEVENT_LIBRARIES traceevent)

    if(TRACEFS_LIBRARIES AND TRACEEVENT_LIBRARIES)

        add_executable(eventheader-decode-live
            decode-live.cpp)
        target_link_libraries(eventheader-decode-live
            eventheader-decode
            "${TRACEFS_LIBRARIES}" "${TRACEEVENT_LIBRARIES}")

    endif()
endif()
