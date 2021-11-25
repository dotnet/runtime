To compile:

1) create a dat file (say criticalsection.dat) with contents:
PAL,Composite,palsuite\composite\synchronization\criticalsection,criticalsection=mainWrapper.c,criticalsection.c,<SUPPORTEXE>,<TESTLANGCPP>,<COMPILEONLY>

2) perl rrunmod.pl -r criticalsection.dat


To execute:
mainwrapper  [PROCESS_COUNT]  [WORKER_THREAD_MULTIPLIER_COUNT] [REPEAT_COUNT]

