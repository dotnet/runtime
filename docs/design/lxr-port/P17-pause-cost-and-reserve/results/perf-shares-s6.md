### s5b-bt
top self: 20.91% libclrlxrgc.so [.] ReferenceCountTable::RegisterObjectInClearedSpan - -; 10.85% libclrlxrgc.so [.] LxrReferenceCounting::RecordFastAllocation - -; 9.27% libclrlxrgc.so [.] LxrGcHeap::AllocationComplete - -; 6.12% libclrlxrgc.so [.] LxrGcHeap::LeaseFinalizer - -; 4.02% libclrlxrgc.so [.] LxrReferenceCounting::CompleteAllocation - -
| function | self % | inclusive % |
|---|---|---|
| `LxrReferenceCounting::WalkObjectStarts` | 2.54 | 9.19 |
| `LxrGcHeap::LeaseFinalizer` | 6.12 | 6.47 |
| `LxrGcHeap::ExecuteScan` | 0.00 | 9.80 |
| `LxrGcHeap::AllocationComplete` | 9.27 | 49.08 |

### s6-bt
top self: 19.19% libclrlxrgc.so [.] ReferenceCountTable::RegisterObjectInClearedSpan - -; 7.29% libclrlxrgc.so [.] LxrGcHeap::AllocationComplete - -; 7.04% libclrlxrgc.so [.] LxrReferenceCounting::RecordFastAllocation - -; 5.89% libclrlxrgc.so [.] LxrReferenceCounting::TryCopyNursery - -; 2.82% libclrlxrgc.so [.] LxrReferenceCounting::ProcessTraceObject - -
| function | self % | inclusive % |
|---|---|---|
| `LxrGcHeap::ExecuteScan` | 0.13 | 0.19 |
| `LxrGcHeap::AllocationComplete` | 7.29 | 41.01 |

### s5b-req
top self: 10.59% libclrlxrgc.so [.] ReferenceCountTable::RegisterObjectInClearedSpan - -; 9.34% memfd:doublemapper (deleted) [.] 0x000000000014d992 - -; 8.98% libclrlxrgc.so [.] LxrGcHeap::AllocationComplete - -; 5.18% libclrlxrgc.so [.] LxrGcHeap::LeaseFinalizer - -; 4.57% libclrlxrgc.so [.] LxrReferenceCounting::RecordFastAllocation - -
| function | self % | inclusive % |
|---|---|---|
| `LxrReferenceCounting::WalkObjectStarts` | 1.91 | 7.71 |
| `LxrGcHeap::LeaseFinalizer` | 5.18 | 5.40 |
| `LxrGcHeap::QueueFinalizer` | 0.11 | 0.11 |
| `LxrGcHeap::ExecuteScan` | 0.00 | 8.28 |
| `LxrGcHeap::AllocationComplete` | 8.98 | 28.63 |

### s6-req
top self: 11.88% libclrlxrgc.so [.] ReferenceCountTable::RegisterObjectInClearedSpan - -; 10.49% libclrlxrgc.so [.] LxrGcHeap::AllocationComplete - -; 9.69% memfd:doublemapper (deleted) [.] 0x000000000014d992 - -; 5.31% libclrlxrgc.so [.] LxrReferenceCounting::RecordFastAllocation - -; 3.05% libclrlxrgc.so [.] LxrReferenceCounting::TryCopyNursery - -
| function | self % | inclusive % |
|---|---|---|
| `LxrGcHeap::ExecuteScan` | 0.06 | 0.12 |
| `LxrGcHeap::AllocationComplete` | 10.49 | 33.07 |

### s5b-ls
top self: 11.87% libclrlxrgc.so [.] LxrReferenceCounting::ProcessTraceObject - -; 8.53% libclrlxrgc.so [.] LxrReferenceCounting::FixForwardingReaders - -; 7.81% libclrlxrgc.so [.] LxrReferenceCounting::ResolveForwarded - -; 7.51% libclrlxrgc.so [.] LxrReferenceCounting::NewWork - -; 5.50% libclrlxrgc.so [.] LxrReferenceCounting::FinishDyingTraceObject - -
| function | self % | inclusive % |
|---|---|---|
| `LxrReferenceCounting::WalkObjectStarts` | 0.25 | 1.15 |
| `LxrGcHeap::LeaseFinalizer` | 0.84 | 0.88 |
| `LxrGcHeap::ExecuteScan` | 0.00 | 1.27 |
| `LxrGcHeap::AllocationComplete` | 1.11 | 6.01 |

### s6-ls
top self: 11.81% libclrlxrgc.so [.] LxrReferenceCounting::ProcessTraceObject - -; 8.27% libclrlxrgc.so [.] LxrReferenceCounting::FixForwardingReaders - -; 8.14% libclrlxrgc.so [.] LxrReferenceCounting::NewWork - -; 7.85% libclrlxrgc.so [.] LxrReferenceCounting::ResolveForwarded - -; 5.43% libclrlxrgc.so [.] LxrReferenceCounting::FinishDyingTraceObject - -
| function | self % | inclusive % |
|---|---|---|
| `LxrGcHeap::ExecuteScan` | 0.02 | 0.02 |
| `LxrGcHeap::AllocationComplete` | 1.17 | 6.03 |

