#!/bin/bash
# P26: binarytrees nursery CPU, with labels, for several settings of the final frame.
F=/root/p25/frames/p26-final-Release-linux
C=/root/p25/scripts26/cpucmp.sh
echo "-- default";            $C $F binarytrees lxr-nursery 4
echo "-- WaitForLazy=1";      $C $F binarytrees lxr-nursery 4 DOTNET_LXRWaitForLazy=1
echo "-- ConcGCThreads=13";   $C $F binarytrees lxr-nursery 4 DOTNET_LXRConcGCThreads=13
echo "-- GCThreads=4";        $C $F binarytrees lxr-nursery 4 DOTNET_LXRGCThreads=4
echo "-- GCThreads=1";        $C $F binarytrees lxr-nursery 4 DOTNET_LXRGCThreads=1
