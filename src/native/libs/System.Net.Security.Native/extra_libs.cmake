
macro(append_extra_security_libs NativeLibsExtra)
  if (HAVE_GSSFW_HEADERS)
     find_library(LIBGSS NAMES GSS)
     if(LIBGSS STREQUAL LIBGSS-NOTFOUND)
        message(FATAL_ERROR "Cannot find GSS.Framework and System.Net.Security.Native cannot build without it. Try installing GSS.Framework (or the appropriate package for your platform)")
     endif()
  elseif(HAVE_HEIMDAL_HEADERS)
     set(_heimdal_hints)
     if (CLR_CMAKE_TARGET_OPENBSD)
        list(APPEND _heimdal_hints
            "${CMAKE_SYSROOT}/heimdal/lib"
            "${CMAKE_SYSROOT}/usr/heimdal/lib")
     endif()

     if (_heimdal_hints)
        # Prefer Heimdal location in sysroot on OpenBSD, where libgssapi is not in /usr/lib.
        find_library(LIBGSS NAMES gssapi libgssapi.so.9.0 PATHS ${_heimdal_hints})
     else()
        find_library(LIBGSS NAMES gssapi)
     endif()

     if(LIBGSS STREQUAL LIBGSS-NOTFOUND)
        message(FATAL_ERROR "Cannot find libgssapi and System.Net.Security.Native cannot build without it. Try installing heimdal (or the appropriate package for your platform)")
     endif()
  elseif(HeimdalGssApi)
       message(FATAL_ERROR "HeimdalGssApi option was set but gssapi headers could not be found and System.Net.Security.Native cannot build without the headers. Try installing heimdal (or the appropriate package for your platform)")
  else()
     find_library(LIBGSS NAMES gssapi_krb5)
     if(LIBGSS STREQUAL LIBGSS-NOTFOUND)
        message(FATAL_ERROR "Cannot find libgssapi_krb5 and System.Net.Security.Native cannot build without it. Try installing libkrb5-dev (or the appropriate package for your platform)")
     endif()
  endif()

  if(CLR_CMAKE_TARGET_LINUX)
    # On Linux libgssapi_krb5.so is loaded on demand to tolerate its absence in singlefile apps that do not use it
    list(APPEND ${NativeLibsExtra} dl)
    add_definitions(-DGSS_SHIM)
  else()
    list(APPEND ${NativeLibsExtra} ${LIBGSS})
  endif()
endmacro()
