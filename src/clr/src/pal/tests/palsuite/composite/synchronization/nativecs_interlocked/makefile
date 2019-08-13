#
# Use gmake to build this project
#


OSNAME=$(shell uname -s)
OSVER=$(shell uname -r | cut -c1-3)
BASEOBJDIR=./obj
TGTDIR=$(BASEOBJDIR)/$(shell uname -m)
COMP=gcc
DEBUGOPT=-D_DEBUG -g
# DEBUGOPT=-DNDEBUG 


ifeq ($(OSNAME),SunOS)
COPT=-DSUNOS -O2 -finline-functions $(DEBUGOPT)  -xc++ 
LOPT=-lpthread -lrt -O2 -finline-functions $(DEBUGOPT)
INTERLOCK_OBJ=sparcinterloc.o
else
  ifeq ($(OSNAME),FreeBSD)
    ifeq ($(OSVER),5.2)
      COMP=g++
      COPT=-DFREEBSD -Di386 -O2 -finline-functions $(DEBUGOPT)  -xc++ 
      LOPT=-lkse -O2 -finline-functions $(DEBUGOPT)
    else
      COPT=-DFREEBSD -Di386 -O2 -finline-functions $(DEBUGOPT)
      LOPT=-pthread -O2 -finline-functions $(DEBUGOPT)
    endif
    INTERLOCK_OBJ=interlocked.o
  else
    ifeq ($(OSNAME),HP-UX)
      COMP=g++
      INTERLOCK_OBJ=hpitinterlock.o
      COPT=-DHPUX -O2 -mlp64 -finline -fPIC -DPIC -DBIT64 -DBIT64 -DLP64COMPATIBLE \
       -D_POSIX_C_SOURCE=199506L -D_HPUX_ -D_XOPEN_SOURCE_EXTENDED -DBIT64 \
       -DBIGENDIAN -DBIT64 $(DEBUGOPT)  -xc++ 
      LOPT=-O2 -mlp64 -DBIT64 -DBIT64 -lgcc -lpthread /usr/lib/hpux64/libunwind.so
    endif
  endif
endif


TARGET0=$(TGTDIR)/native_cs_interlocked

all:    dirs $(TARGET0) $(TARGET1) $(TARGET2) $(TARGET3) $(TARGET4) 

dirs:	
	@echo Target dir: $(TGTDIR) 
	@if [ \! -d $(BASEOBJDIR) ]; then mkdir $(BASEOBJDIR); fi
	@if [ \! -d $(TGTDIR) ]; then mkdir $(TGTDIR); fi

$(TGTDIR)/native_cs_interlocked:	$(TGTDIR)/$(INTERLOCK_OBJ) $(TGTDIR)/native_cs_interlocked.o
	$(COMP) $(LOPT) -o $(TARGET0) $(TGTDIR)/native_cs_interlocked.o $(TGTDIR)/$(INTERLOCK_OBJ)

$(TGTDIR)/native_cs_interlocked.o:        pal_composite_native_cs.c
	$(COMP) $(COPT) -DFULL_CSIMPL -o $(TGTDIR)/native_cs_interlocked.o -c pal_composite_native_cs.c

$(TGTDIR)/interlocked.o:        interlocked.cpp
	$(COMP) $(COPT) -c -o $(TGTDIR)/interlocked.o interlocked.cpp

$(TGTDIR)/sparcinterloc.o:        sparcinterloc.s
	$(COMP) -x assembler-with-cpp -c -Wa,-Av9 -o $(TGTDIR)/sparcinterloc.o sparcinterloc.s

$(TGTDIR)/hpitinterlock.o:        hpitinterlock.s
	$(COMP) $(COPT) -x assembler-with-cpp -c -fno-strict-aliasing -o $(TGTDIR)/hpitinterlock.o hpitinterlock.s

clean:
	@echo Cleaning
	@rm -f $(TGTDIR)/*.o $(TARGET0) $(TARGET1) $(TARGET2) $(TARGET3) $(TARGET4) 
	@if [ -d $(TGTDIR) ]; then rmdir $(TGTDIR); fi
