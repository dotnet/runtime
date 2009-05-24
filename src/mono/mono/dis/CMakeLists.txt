if(PLATFORM_WIN32)
###export HOST_CC
endif()

set(libmonodis_a_SOURCES  
	get.c	
	get.h	
	dis-cil.c
	dis-cil.h
	util.c	
	util.h)

set(monodis_SOURCES  
	dump.c	
	dump.h	
	main.c	
	meta.h	
	declsec.c
	declsec.h)

set(monodis_LDADD 		
	libmonodis.a		
	${runtime_lib}		
	${GLIB_LIBS})

include_directories(${top_srcdir})
include_directories(${GLIB2_INCLUDE_DIRS})
set(CMAKE_C_FLAGS "${CFLAGS} ${CPPFLAGS}")

# FIXME: cmake doesn't seem to link the libs together into libmono-static.a
link_directories(../../libgc/.libs)

add_executable(monodis ${libmonodis_a_SOURCES} ${monodis_SOURCES})
target_link_libraries(monodis mono-static ${GLIB_LIBS})

###bin_PROGRAMS monodis

###noinst_LIBRARIES libmonodis.a

###man_MANS monodis.1

###EXTRA_DIST ${man_MANS}
