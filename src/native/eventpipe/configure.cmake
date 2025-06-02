include(CheckSymbolExists)
include(CheckIncludeFile)
include(CheckIncludeFiles)

check_include_file(
    sys/socket.h
    HAVE_SYS_SOCKET_H
)

check_symbol_exists(
    accept4
    sys/socket.h
    HAVE_ACCEPT4)

# Use TCP for EventPipe on mobile platforms
if (CLR_CMAKE_HOST_IOS OR CLR_CMAKE_HOST_TVOS OR CLR_CMAKE_HOST_ANDROID)
  set(FEATURE_PERFTRACING_PAL_TCP 1)
  set(FEATURE_PERFTRACING_DISABLE_DEFAULT_LISTEN_PORT 1)
endif()

check_include_files(
    "linux/user_events.h;sys/ioctl.h;unistd.h"
    HAVE_LINUX_USER_EVENTS_H
)

check_include_file(
    "sys/uio.h"
    HAVE_SYS_UIO_H
)

if (NOT DEFINED EP_GENERATED_HEADER_PATH)
    message(FATAL_ERROR "Required configuration EP_GENERATED_HEADER_PATH not set.")
endif (NOT DEFINED EP_GENERATED_HEADER_PATH)

configure_file(${CLR_SRC_NATIVE_DIR}/eventpipe/ep-shared-config.h.in ${EP_GENERATED_HEADER_PATH}/ep-shared-config.h)

set (SHARED_EVENTPIPE_CONFIG_HEADER_PATH "${EP_GENERATED_HEADER_PATH}")