To compile: 

For FReeBSD Platform use the following to compile:
gcc -pthread -lm -lgcc -lstdc++ -xc++ -Di386 pal_composite_native_cs.c


--------------------------------------------------------
For Solaris Platform use the following to compile:
 gcc -lpthread -lm -lgcc -lstdc++ -xc++ -D__sparc__  -mimpure-text  -shared -o critsect.so mtx_critsect.cpp interlock.s
gcc -lpthread -lm -lgcc -lstdc++ -xc++ -D__sparc__ pal_composite_native_cs.c


setenv LD_LIBRARY_PATH /usr/lib/lwp:/usr/local/lib:/usr/lib:/opt/sfw/lib:.
 gcc -lpthread -lm -lgcc -lstdc++ -lcritsect -xc++ -D__sparc__ pal_composite_native_cs.c
 gcc -lpthread -lm -lgcc -lstdc++ -xc++ -D__sparc__  -mimpure-text  -shared -o critsect.so mtx_critsect.cpp 

(pts/2):{4}% ldd critsect.so
        libpthread.so.1 =>       /usr/lib/libpthread.so.1
        libm.so.1 =>     /usr/lib/libm.so.1
        libstdc++.so.2.10.0 =>   /usr/local/lib/libstdc++.so.2.10.0
        libc.so.1 =>     /usr/lib/libc.so.1
        libdl.so.1 =>    /usr/lib/libdl.so.1
        libthread.so.1 =>        /usr/lib/libthread.so.1
        /usr/platform/SUNW,Serverblade1/lib/libc_psr.so.1


--------------------------------------------------------
For HPUX Platform use the following to compile:
gcc -lpthread -mlp64 -lm -lgcc -lstdc++ -xc++ -D_HPUX_ -D__ia64__ pal_composite_native_cs.c

--------------------------------------------------------
To execute:
./a.out [PROCESS_COUNT]  [THREAD_COUNT] [REPEAT_COUNT] 


 ./a.out 1 32 1000000 4102406



