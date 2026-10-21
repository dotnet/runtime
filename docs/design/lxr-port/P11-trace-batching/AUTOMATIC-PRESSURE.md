# Automatic-pressure supplement

Fixed before its first run, separate from the unchanged pre-edit batching
design and goals: use the original `VerticalBody("allocation")` loop from
the retained log4 compiler input: 524288 allocations, checksum1073479680,
no explicit collection inside the body, one explicit warmup and one
explicit post-body collection outside it.

Run two observations per eight arms at LXR8MiB Immix+16MiB LOS and managed
capacity limit24MiB. Built-ins use standard GCHeapHardLimit24MiB, whose
bookkeeping-inclusive meaning is explicitly not identical to LXR's managed
capacity limit. The separately hashed observation fixture adds process-CPU
reads and loaded-module/mode assertions, not cheaper allocation or checks.
It also supplies diagnostic-only counter snapshots. It is not used for
any normal forced-full timing, and its counter interval is not the earlier
P10 log4/GDB cohort. Unavailable old batch counters remain null.
