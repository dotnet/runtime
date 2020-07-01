include(CheckIncludeFiles)
include(CheckSymbolExists)

check_symbol_exists(
    inotify_init
    sys/inotify.h
    HAVE_INOTIFY_INIT)

check_symbol_exists(
    inotify_add_watch
    sys/inotify.h
    HAVE_INOTIFY_ADD_WATCH)

check_symbol_exists(
    inotify_rm_watch
    sys/inotify.h
    HAVE_INOTIFY_RM_WATCH)

set (HAVE_INOTIFY 0)
if (HAVE_INOTIFY_INIT AND HAVE_INOTIFY_ADD_WATCH AND HAVE_INOTIFY_RM_WATCH)
    set (HAVE_INOTIFY 1)
elseif (CLR_CMAKE_TARGET_LINUX AND NOT CLR_CMAKE_TARGET_BROWSER)
    message(FATAL_ERROR "Cannot find inotify functions on a Linux platform.")
endif()

check_include_files(
    GSS/GSS.h
    HAVE_GSSFW_HEADERS)

option(HeimdalGssApi "use heimdal implementation of GssApi" OFF)
if (HeimdalGssApi)
   check_include_files(
       gssapi/gssapi.h
       HAVE_HEIMDAL_HEADERS)
endif()
