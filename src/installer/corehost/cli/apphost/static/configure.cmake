include(CheckIncludeFiles)

check_include_files(
    GSS/GSS.h
    HAVE_GSSFW_HEADERS)

option(HeimdalGssApi "use heimdal implementation of GssApi" OFF)
if (HeimdalGssApi)
   check_include_files(
       gssapi/gssapi.h
       HAVE_HEIMDAL_HEADERS)
endif()
