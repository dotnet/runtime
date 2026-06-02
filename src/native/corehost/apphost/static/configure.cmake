include(CheckIncludeFiles)

check_include_files(
    GSS/GSS.h
    HAVE_GSSFW_HEADERS)

option(HeimdalGssApi "use heimdal implementation of GssApi" OFF)

if (CLR_CMAKE_TARGET_OPENBSD)
    set(HeimdalGssApi ON)
    set(CMAKE_REQUIRED_INCLUDES ${CROSS_ROOTFS}/heimdal/include)
    set(CMAKE_PREFIX_PATH ${CROSS_ROOTFS}/heimdal/lib)
endif()

if (HeimdalGssApi)
   check_include_files(
       gssapi/gssapi.h
       HAVE_HEIMDAL_HEADERS)
endif()
