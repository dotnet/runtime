# GC Counters for .NET 5

This document describes the new GC EventCounters that are being added in .NET 5. Specifically, it goes over the following:

* Problem
* Design Choice
* Computation


## Problem

The garbage collector is one of the key runtime components that contribute to the performance characteristics of a .NET application. 