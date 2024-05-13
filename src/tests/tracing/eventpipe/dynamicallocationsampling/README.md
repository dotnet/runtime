This folder contains simple code that compares the time spent to allocate arrays of bytes with event analysis as an example.
The manual folder contains code to allocate and count objects in different runs. 

More interestingly, itthe result of 10 x runs of GCPerfSim to allocate 500 GB of mixed size objects on 4 threads with a 50MB live object size are also available. 
The goal is to emphasize the impact of allocation performance and GC collection overhead:
- GCPerfSimx10_Baseline.txt: .NET version before the PR
- GCPerfSimx10_Baseline+AllocationTick.txt: same but with AllocationTick emitted
- GCPerfSimx10_PullRequest.txt: PR without provider enabled
- GCPerfSimx10_PullRequest+Events.txt: same but with AllocationSampled emitted
In each scenario, the PR is faster than the baseline (expected for AllocationTick because verbose instead of information)


