To compile: 

1) create a dat file (say object_management.dat) with contents:

PAL,Composite,palsuite\composite\object_management\mutex\nonshared,mutex=main.c mutex.c,<SUPPORTEXE>,<TESTLANGCPP>,<COMPILEONLY>
PAL,Composite,palsuite\composite\object_management\mutex\shared,mutex=main.c mutex.c,<SUPPORTEXE>,<TESTLANGCPP>,<COMPILEONLY>
PAL,Composite,palsuite\composite\object_management\semaphore\nonshared,semaphore=main.c semaphore.c,<SUPPORTEXE>,<TESTLANGCPP>,<COMPILEONLY>
PAL,Composite,palsuite\composite\object_management\semaphore\shared,semaphore=main.c semaphore.c,<SUPPORTEXE>,<TESTLANGCPP>,<COMPILEONLY>
PAL,Composite,palsuite\composite\object_management\event\nonshared,event=main.c event.c,<SUPPORTEXE>,<TESTLANGCPP>,<COMPILEONLY>
PAL,Composite,palsuite\composite\object_management\event\shared,event=main.c event.c,<SUPPORTEXE>,<TESTLANGCPP>,<COMPILEONLY>


2) perl rrunmod.pl -r object_management.dat


To execute:
For each of the test cases, 
main  [PROCESS_COUNT]  [THREAD_COUNT] [REPEAT_COUNT]


Output:
The performance numbers will be in <process_logical_id>_[event|semaphore|mutex].txt 
(will be at palsuite\composite\object_management\[mutex|event|semaphore]\[shared|nonshared]\obj[r|c|d] directory if u use rrunmod.pl)

So if process_count is 3, you will have files 0_mutex.txt, 1_mutex.txt and so on…

For each process txt file created, 
each row represents a thread data (process id, number of failures, number of pass,  total number of repeated operations and an integer that will be used to identify a run 
(currently zero)).
