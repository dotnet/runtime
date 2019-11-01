To compile: 

1) create a dat file (say threadsuspension.dat) with contents:
PAL,Composite,palsuite\composite\threading\threadsuspension,wfmo=mainWrapper.c threadsuspension.c,<SUPPORTEXE>,<TESTLANGCPP>,<COMPILEONLY>

2) perl rrunmod.pl -r threadsuspension.dat


To execute:
mainWrapper  [PROCESS_COUNT] [THREAD_COUNT] [REPEAT_COUNT] 

