To compile: 

For FReeBSD Platform use the following to compile:
gcc -pthread -lm -lgcc -lstdc++ -xc++ -Di386 pal_composite_native_cs.c

For Solaris Platform use the following to compile:
gcc -lpthread -lm -lgcc -lstdc++ -xc++ -D__sparc__ pal_composite_native_cs.c

For HPUX Platform use the following to compile:
gcc -lpthread -mlp64 -lm -lgcc -lstdc++ -xc++ -D_HPUX_ -D__ia64__ pal_composite_native_cs.c

To execute:
./a.out [PROCESS_COUNT]  [THREAD_COUNT] [REPEAT_COUNT] 


 ./a.out 1 32 1000000 4102406



