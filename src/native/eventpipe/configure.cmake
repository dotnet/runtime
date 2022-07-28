include(CheckSymbolExists)

check_symbol_exists(
    accept4
    sys/socket.h
    HAVE_ACCEPT4)

if (NOT DEFINED EP_GENERATED_HEADER_PATH)
    message(FATAL_ERROR "Required configuration EP_GENERATED_HEADER_PATH not set.")
endif (NOT DEFINED EP_GENERATED_HEADER_PATH)

configure_file(${CLR_SRC_NATIVE_DIR}/eventpipe/ep-shared-config.h.in ${EP_GENERATED_HEADER_PATH}/ep-shared-config.h)

set (SHARED_EVENTPIPE_CONFIG_HEADERS "${EP_GENERATED_HEADER_PATH}/ep-shared-config.h")