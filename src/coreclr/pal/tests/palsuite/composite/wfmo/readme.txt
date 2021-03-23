To compile: 

1) create a dat file (say wfmo.dat) with contents:
PAL,Composite,palsuite\composite\wfmo,wfmo=main.c mutex.c,<SUPPORTEXE>,<TESTLANGCPP>,<COMPILEONLY>

2) perl rrunmod.pl -r wfmo.dat


To execute:
main  [PROCESS_COUNT]  [THREAD_COUNT] [REPEAT_COUNT] [SLEEP_LENGTH]

Output:
The performance numbers will be in <process_logical_id>_wfmo.txt 
(will be at palsuite\composite\wfmo\obj[r|c|d] directory if u use rrunmod.pl)

So if process_count is 3, you will have files 0_wfmo.txt, 1_wfmo.txt and so on…

For each process txt file created, 
each row represents a thread data (process id, number of failures, number of pass,  total number of repeated operations and an integer that will be used to identify a run 
(currently zero)).


