# cDAC Dump Test Known Failures

This document tracks known test failures in the cDAC dump test suite along with their root causes and affected platforms.

## Linux heap dumps missing MethodDesc size table

**Affected tests:**
- `StackWalkDumpTests_Local.StackWalk_ManagedFramesHaveValidMethodDescs`
- `StackWalkDumpTests_Local.StackWalk_FramesHaveRawContext`
- `RuntimeTypeSystemDumpTests_Local.RuntimeTypeSystem_ObjectMethodTableHasIntroducedMethods`
- `EcmaMetadataDumpTests_Local.EcmaMetadata_CanGetMetadataReader`
- `EcmaMetadataDumpTests_Local.EcmaMetadata_RootModuleHasMetadataAddress`

**Platforms:** Linux (x64, arm64), macOS (x64, arm64)

**Dump type:** Heap dump (MiniDumpType=2)

**Symptom:** `VirtualReadException: Failed to read System.Byte at <address>` during `MethodDesc.ComputeSize` or `EcmaMetadata_1.GetReadOnlyMetadataAddress`. The address points into memory regions that are not included in heap dumps.

**Root cause:** Heap dumps (`MiniDumpType=2`) on Linux and macOS do not include all mapped memory regions needed by the cDAC reader. Specifically, the MethodDesc size lookup table and PE metadata regions reside in memory pages that `createdump` does not capture in heap mode. These regions are outside the managed heap and GC data structures.

**Workaround:** Use full dumps (`MiniDumpType=4`) on Linux and macOS. Full dumps include all virtual memory and resolve these failures. The Helix pipeline currently uses `MiniDumpType=4` for this reason. On Windows, heap dumps also exhibit this issue for some tests. Note that macOS full dumps are significantly larger (~5 GB) due to the dyld shared cache being mapped into every process.

**Status:** Won't fix — heap dumps are inherently limited. The pipeline uses full dumps.
