This folder contains code that compares the time spent to allocate 
100000 arrays of 35 bytes. I wanted to manually have a rough idea of the order of magnitude without any event, with AllocationTick, with AllocationSampled.
The warmup stage is needed to pay the commit cost to store the 100000 arrays.

 ```
 "C:\github\chrisnas\runtime\artifacts\tests\coreclr\windows.x64.Debug\Tests\Core_Root\corerun.exe" -p "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization=true"  dynamicallocationsampling.dll
  0.0s: 100000 allocated arrays
  0.3s:  #GCs: 0
  0.3s:  Warmup: 100000 instances allocated for 3500000 bytes in 522 ms
  0.3s: -
  0.3s: 100000 allocated arrays
  0.3s:  #GCs: 1
  0.3s:  No Sampling: 100000 instances allocated for 3500000 bytes in 8 ms
  0.3s: -
  0.3s: ==TEST STARTING==
  5.3s: 100000 allocated arrays
  5.3s:  #GCs: 2
  5.3s:  AllocationTick: 100000 instances allocated for 3500000 bytes in 13 ms
  7.2s: AllocationTick counts validation
  7.2s: Nb events: 71
  7.2s: ==TEST FINISHED: PASSED!==
  7.2s: -
  7.2s: ==TEST STARTING==
  7.5s: 100000 allocated arrays
  7.5s:  #GCs: 3
  7.5s:  AllocationSampled: 100000 instances allocated for 3500000 bytes in 14 ms
  7.5s: AllocationSampled counts validation
  7.5s: Nb events: 54
  7.5s: ==TEST FINISHED: PASSED!==
```