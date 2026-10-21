### s3-binarytrees
top self: 14.57% libclrlxrgc.so [.] ReferenceCountTable::RegisterObjectInClearedSpan - -; 9.51% libclrlxrgc.so [.] LxrReferenceCounting::RecordFastAllocation - -; 6.68% libclrlxrgc.so [.] SideMetadataAccessor::IsRangeZero - -; 5.18% libclrlxrgc.so [.] LxrGcHeap::AllocationComplete - -; 4.82% libclrlxrgc.so [.] LxrGcHeap::LeaseFinalizer - -
| function | self % | inclusive % |
|---|---|---|
| `LxrReferenceCounting::Sweep` | 0.00 | 6.98 |
| `LxrReferenceCounting::GetHeapStatistics` | 0.66 | 1.93 |
| `LxrReferenceCounting::ReleaseEpoch` | 0.00 | 7.34 |
| `ImmixBlockManager::TryReleaseIfRcDead` | 0.00 | 6.68 |
| `ImmixAllocationManager::TryFindNextHole` | 0.00 | 0.30 |

### s4-binarytrees
top self: 18.55% libclrlxrgc.so [.] ReferenceCountTable::RegisterObjectInClearedSpan - -; 6.99% libclrlxrgc.so [.] LxrReferenceCounting::RecordFastAllocation - -; 6.45% libclrlxrgc.so [.] LxrGcHeap::AllocationComplete - -; 5.31% libclrlxrgc.so [.] LxrGcHeap::LeaseFinalizer - -; 4.77% libclrlxrgc.so [.] LxrReferenceCounting::TryCopyNursery - -
| function | self % | inclusive % |
|---|---|---|
| `LxrReferenceCounting::Sweep` | 0.00 | 0.54 |
| `LxrReferenceCounting::SweepCycles` | 0.13 | 0.13 |
| `LxrReferenceCounting::ReleaseEpoch` | 0.00 | 0.74 |
| `LxrReferenceCounting::ReleaseNurseryBlocks` | 0.00 | 0.27 |
| `LxrReferenceCounting::ReleaseBlockIfDead` | 0.00 | 0.40 |
| `ImmixBlockManager::TryReleaseIfRcDead` | 0.00 | 0.40 |
| `ImmixBlockManager::TryReleaseNursery` | 0.00 | 0.27 |
| `ImmixAllocationManager::TryFindNextHole` | 0.00 | 0.13 |

### s3-requests
top self: 9.73% libclrlxrgc.so [.] SideMetadataAccessor::IsRangeZero - -; 9.11% libclrlxrgc.so [.] ReferenceCountTable::RegisterObjectInClearedSpan - -; 8.47% libclrlxrgc.so [.] LxrGcHeap::AllocationComplete - -; 6.94% memfd:doublemapper (deleted) [.] 0x000000000014d992 - -; 4.75% libclrlxrgc.so [.] LxrGcHeap::LeaseFinalizer - -
| function | self % | inclusive % |
|---|---|---|
| `LxrReferenceCounting::Sweep` | 0.03 | 12.64 |
| `LxrReferenceCounting::SweepCycles` | 0.59 | 0.59 |
| `LxrReferenceCounting::GetHeapStatistics` | 2.10 | 5.40 |
| `LxrReferenceCounting::ReleaseEpoch` | 0.00 | 12.16 |
| `ImmixBlockManager::TryReleaseIfRcDead` | 0.03 | 10.46 |
| `ImmixBlockManager::SetReusable` | 0.03 | 0.10 |
| `ImmixAllocationManager::TryFindNextHole` | 0.37 | 2.99 |
| `LxrReferenceCounting::GetMaturePages` | 0.00 | 0.02 |

### s4-requests
top self: 10.85% libclrlxrgc.so [.] ReferenceCountTable::RegisterObjectInClearedSpan - -; 9.66% libclrlxrgc.so [.] LxrGcHeap::AllocationComplete - -; 7.97% memfd:doublemapper (deleted) [.] 0x000000000014d992 - -; 5.81% libclrlxrgc.so [.] LxrGcHeap::LeaseFinalizer - -; 4.99% libclrlxrgc.so [.] LxrReferenceCounting::RecordFastAllocation - -
| function | self % | inclusive % |
|---|---|---|
| `LxrReferenceCounting::Sweep` | 0.00 | 2.94 |
| `LxrReferenceCounting::SweepCycles` | 0.67 | 0.69 |
| `LxrReferenceCounting::ReleaseEpoch` | 0.00 | 1.01 |
| `LxrReferenceCounting::ReleaseNurseryBlocks` | 0.00 | 0.46 |
| `LxrReferenceCounting::RefreshBlock` | 0.02 | 1.73 |
| `LxrReferenceCounting::ReleaseBlockIfDead` | 0.00 | 0.46 |
| `ImmixBlockManager::TryReleaseIfRcDead` | 0.08 | 0.46 |
| `ImmixBlockManager::TryReleaseNursery` | 0.11 | 0.44 |
| `ImmixBlockManager::TrySetReusableLazily` | 0.00 | 0.11 |
| `ImmixAllocationManager::TryFindNextHole` | 0.44 | 3.35 |

### s3-listsuffix
top self: 11.01% libclrlxrgc.so [.] LxrReferenceCounting::ProcessTraceObject - -; 8.46% libclrlxrgc.so [.] LxrReferenceCounting::NewWork - -; 8.03% libclrlxrgc.so [.] LxrReferenceCounting::FixForwardingReaders - -; 7.87% libclrlxrgc.so [.] LxrReferenceCounting::ResolveForwarded - -; 5.15% libclrlxrgc.so [.] LxrReferenceCounting::FinishDyingTraceObject - -
| function | self % | inclusive % |
|---|---|---|
| `LxrReferenceCounting::Sweep` | 0.00 | 0.57 |
| `LxrReferenceCounting::SweepCycles` | 0.07 | 0.07 |
| `LxrReferenceCounting::GetHeapStatistics` | 0.67 | 1.49 |
| `LxrReferenceCounting::ReleaseEpoch` | 0.00 | 0.55 |
| `ImmixBlockManager::TryReleaseIfRcDead` | 0.01 | 0.24 |
| `ImmixBlockManager::SetReusable` | 0.02 | 0.03 |
| `ImmixAllocationManager::TryFindNextHole` | 0.08 | 0.55 |
| `LxrReferenceCounting::GetMaturePages` | 0.00 | 0.01 |

### s4-listsuffix
top self: 12.02% libclrlxrgc.so [.] LxrReferenceCounting::ProcessTraceObject - -; 8.74% libclrlxrgc.so [.] LxrReferenceCounting::FixForwardingReaders - -; 8.41% libclrlxrgc.so [.] LxrReferenceCounting::ResolveForwarded - -; 6.81% libclrlxrgc.so [.] LxrReferenceCounting::NewWork - -; 5.70% libclrlxrgc.so [.] LxrReferenceCounting::FinishDyingTraceObject - -
| function | self % | inclusive % |
|---|---|---|
| `LxrReferenceCounting::Sweep` | 0.00 | 0.36 |
| `LxrReferenceCounting::SweepCycles` | 0.04 | 0.04 |
| `LxrReferenceCounting::ReleaseEpoch` | 0.00 | 0.16 |
| `LxrReferenceCounting::ReleaseNurseryBlocks` | 0.00 | 0.01 |
| `LxrReferenceCounting::RefreshBlock` | 0.00 | 0.23 |
| `LxrReferenceCounting::ReleaseBlockIfDead` | 0.00 | 0.09 |
| `ImmixBlockManager::TryReleaseIfRcDead` | 0.00 | 0.08 |
| `ImmixBlockManager::TryReleaseNursery` | 0.00 | 0.01 |
| `ImmixBlockManager::TrySetReusableLazily` | 0.00 | 0.02 |
| `ImmixAllocationManager::TryFindNextHole` | 0.07 | 0.53 |

