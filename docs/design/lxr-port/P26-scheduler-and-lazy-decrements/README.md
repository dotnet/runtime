# P26: pause cost without the trace backlog (F2), work-stealing scheduler (F4b), no wait for lazy decrements (F8)

Status: product changes in 5 local commits (`F2:`, `F4b:`, `F2:`, `F8:`, `F2:`). Nothing was pushed. No pull request was opened.
Branch: `kkokosa-lxr-scheduler-f2-f4b-f8`, made from `kkokosa-lxr-parity-fixes-f1-f5-f4` (tip `c3352e76067`). No other branch was touched.

Labels: **Measured** = I ran it and saw the result. **Code** = I read it in the source (file and function are named).
**Inferred** = I conclude it from other facts, and it can be wrong. **Open** = not known, or not done.

Words used here:

- **the Java LXR** = the LXR collector of MMTk in OpenJDK: mmtk-openjdk `0682434ae725`, mmtk-core `304ce69d43aa`. I read the source of
  exactly these versions (exported with `git archive`). I read `scheduler/scheduler.rs`, `plan/lxr/cm.rs`, `plan/lxr/rc.rs`,
  `plan/lxr/barrier.rs`, `plan/lxr/global.rs`, `lib.rs`, `global_state.rs` and `util/alloc/allocator.rs`. I did **not** read
  `work_bucket.rs`, `worker.rs` and `worker_monitor.rs`. Where this document says what the Java LXR does, it names the function I read.
- **pause** = the time when the program threads are stopped. **RC pause** = a pause that only does reference counting work.
  **InitialMark / FinalMark** = the pauses that start and end a concurrent trace. **Full** = a pause that traces the whole heap.
- **Packet** = one unit of work for the worker threads. **Lazy work** = the decrements and the sweep that follow an RC pause.
  The workers run them while the program runs.
- **Trace backlog** = trace packets that were made and not yet run.
- **Control** = a deliberately broken variant of the code (a build define). The test must fail on it.
- **x WKS** = ratio of the median to .NET Workstation GC in the same session. **CPU** = process CPU time (user + system). **Wall** = elapsed time.

## 1. Result in short

All numbers are **Measured** on WSL (16 logical cores), Linux Release, ReadyToRun on, heap 4 times the smallest WKS heap, 3 repetitions,
natural collections only, one run at a time, nothing else running. All checksums are equal in all runs and all cells.
**Compare Java and .NET only through ratios; this document has no Java time.**

### Wall time and CPU, median of 3, with the ratio to WKS of the same session

The columns `base`, `F2`, `F4b` and `final` are four collector builds that I ran in the same session (script `scripts/bench-all.sh`).
`base` = the P25 tip. `F2` = after the first F2 commit. `F4b` = after the scheduler (all workers trace). `final` = all commits
(3 trace workers, F8). The P23 and P25 columns are the numbers of the task text (other sessions, so only the ratios are comparable).

| program | arm | P23 wall | P25 wall | base wall | F2 wall | F4b wall | final wall | final x WKS | P25 CPU x WKS | final CPU s | final CPU x WKS |
|---|---|---|---|---|---|---|---|---|---|---|---|
| binarytrees | wks | 0.32 | 0.33 | 0.36 | 0.36 | 0.34 | 0.37 | 1.0 | 1.0 | 0.27 | 1.0 |
| binarytrees | server | | | 0.36 | 0.36 | 0.34 | 0.38 | 1.0 | | 0.30 | 1.1 |
| binarytrees | lxr-nonmoving | 0.75 | 0.49 | 0.52 | 0.51 | 0.49 | 0.49 | 1.3 | 2.7 | 0.66 | 2.4 |
| binarytrees | lxr-nursery | 0.86 | 0.55 | 0.56 | 0.56 | 0.56 | 0.62 | 1.7 | 3.6 | 1.39 | 5.1 |
| requests | wks | 1.71 | 1.71 | 2.00 | 1.97 | 2.01 | 1.99 | 1.0 | 1.0 | 1.83 | 1.0 |
| requests | server | | | 1.98 | 2.12 | 2.04 | 2.04 | 1.0 | | 2.14 | 1.2 |
| requests | lxr-nonmoving | 10.57 | 4.04 | 4.24 | 4.25 | 4.05 | 3.59 | 1.8 | 3.8 | 4.40 | 2.4 |
| requests | lxr-nursery | 4.20 | 4.16 | 4.35 | 4.38 | 4.10 | 3.73 | 1.9 | 5.6 | 5.50 | 3.0 |
| listsuffix | wks | 3.41 | 3.60 | 4.25 | 4.35 | 4.10 | 4.28 | 1.0 | 1.0 | 4.38 | 1.0 |
| listsuffix | server | | | 1.89 | 1.84 | 1.81 | 1.82 | 0.4 | | 2.27 | 0.5 |
| listsuffix | lxr-nonmoving | 21.62 | 3.68 | 4.03 | 4.02 | 3.72 | 3.49 | 0.8 | 3.0 | 10.58 | 2.4 |
| listsuffix | lxr-nursery | 30.42 | 4.56 | 4.91 | 4.74 | 4.47 | 4.28 | 1.0 | 3.7 | 13.53 | 3.1 |

Ratio to WKS, wall (this session): base 1.4/1.5 (binarytrees), 2.1/2.2 (requests), 0.9/1.2 (list suffix); final 1.3/1.7, 1.8/1.9, 0.8/1.0.
CPU x WKS, final against base: requests 2.4/3.0 against 3.3/4.9; list suffix 2.4/3.1 against 2.6/3.3; binarytrees 2.4/5.1 against 2.5/3.2 (see 1.2).
**Measured:** the machine was 5 to 25 percent slower in this session than in P23 and P25 (WKS 1.99 s in requests, 4.28 s in list suffix).

### 1.1 Pauses, Full counts, CPU against wall

Full pauses: 0 in all 12 cells of all four builds (`results/s26-*-table.txt`).

Median pause, `traced run` (one run with the phase timers; the wall time of a traced run is not a result). The column **stop** is the time that
the program threads are stopped (`stw_pause_us`). The column **total** is the time of the whole collection call, which also holds the
waits for lazy work before and after the pause. `scripts/pausestw.py`, `results/s26-pause-stw.md`.

| cell (kind) | base total / stop ms | final total / stop ms |
|---|---|---|
| binarytrees nonmoving (RefCount) | 5.4 / 3.7 | 4.8 / 4.1 |
| binarytrees nursery (RefCount) | 6.6 / 5.1 | 5.5 / 4.5 |
| requests nonmoving (RefCount) | 16.5 / 6.1 | 7.0 / 6.2 |
| requests nursery (RefCount) | 19.3 / 12.8 | 14.3 / 13.4 |
| list suffix nonmoving (RefCount) | 31.1 / 16.9 | 17.6 / 16.8 |
| list suffix nursery (RefCount) | 43.6 / 28.1 | 24.3 / 23.4 |
| list suffix nursery (FinalMark) | 148.7 / 36.2 | 28.6 / 27.8 |
| list suffix nursery (max RefCount stop) | 98.3 | 55.7 |

So the stop time of an RC pause did not change much (it never held the waits). The "pause" numbers of P24 and P25 were **total** values: they
included the wait for lazy work. After F8 total and stop are almost equal.

CPU against wall of the final build (median of 3): binarytrees 0.66/0.49 and 1.39/0.62 s, requests 4.40/3.59 and 5.50/3.73 s, list suffix 10.58/3.49 and
13.53/4.28 s (nonmoving and nursery, CPU/wall). All values are in the table above and in `results/s26-final-table.txt`.

### 1.2 Notes on the table

1. **Binarytrees nursery is bimodal** (Measured, `scripts/bt-cpu.sh`). In 8 of 12 runs of the final build the trace ends before the program ends
   (19 pauses, one of them a FinalMark): CPU 1.3 to 1.5 s, wall 0.62 s. In the other runs the program ends first (18 pauses): CPU 0.8 s, wall 0.55 s. The setting
   `LXRWaitForLazy=1` shows both modes as well (2 of 4 runs with 19 pauses). So the cause is not F8. **Inferred:** the faster trace (F4b trace loop)
   ends before the program ends more often, and the finished trace costs CPU. The median of 3 hit the high mode in the final column.
2. `Server` GC and WKS have more CPU per wall second than LXR in no cell. LXR CPU is 2.4 to 5.1 times WKS. The scheduler was not the cause any more
   in the profile of the final build (section 4.6). The remaining CPU is per-object work (copy, decrement, increment), which is F9 and F10 of P24 (**Open**).

### 1.3 Extra table: the collector default (mature copy on), as the task asked

`compare.sh` of P23 sets `LXREvacuateMature=0` for all LXR arms. The arms of this table do not. `lxr-defrag` = no nursery copy, mature copy on.
`lxr-mature` = nursery copy and mature copy on (the collector default). Final build, `scripts/bench-mature.sh`, `results/s26-final-mature-table.txt`.

| program | WKS wall | lxr-defrag wall / CPU / pauses / Full | lxr-mature wall / CPU / pauses / Full | defrag x WKS | mature x WKS |
|---|---|---|---|---|---|
| binarytrees | 0.36 | 0.49 / 0.66 / 17 / 0 | 0.65 / 1.43 / 19 / 0 | 1.4 | 1.8 |
| requests | 1.89 | 3.53 / 4.35 / 58 / 0 | 3.82 / 5.61 / 64 / 0 | 1.9 | 2.0 |
| listsuffix | 4.03 | 3.42 / 10.27 / 56 / 0 | 4.85 / 14.52 / 63 / 0 | 0.8 | 1.2 |

Compare with the table above: the arms with the mature copy off give 0.49/0.62, 3.59/3.73 and 3.49/4.28 s. With the mature copy on the
results are in the same range, except list suffix with nursery copy (4.85 s against 4.28 s). The checksums are equal.
The setting does not hide a problem in these three programs. **Open:** I would still remove it from `compare.sh`, so the arms run as the collector runs.

## 2. F2: the pause cost does not follow the trace backlog

### 2.1 The basics

The concurrent trace marks the live objects. Its work is cut into trace packets. Workers run the packets while the program runs. A pause stops
the program. If the pause has to look at every pending packet, a long backlog makes a long pause. P24 found this: each pause walked all packets, and the
cycle of list suffix never ended.

When a pause moves a young object (copy), packets that name the old address must find the new one. The Java LXR looks for the new address
when it **uses** a pointer. It does not walk the packets in the pause.

### 2.2 What the Java LXR does

- **Code** (`plan/lxr/rc.rs: ProcessDecs::process_decs`): before a decrement, `if RC_MATURE_EVACUATION && is_forwarded(o) { o = read_forwarding_pointer(o) }`.
  The forwarding is resolved when the packet runs.
- **Code** (`scheduler/scheduler.rs: pause_concurrent_marking_work_packets_during_gc`, `schedule_concurrent_packets`): at the start of a pause the packets of the
  unconstrained bucket move to a list (`postponed_concurrent_work`) in one swap. At the end of the pause they go back in one swap. No packet is read.
  `LXRConcurrentTraceObjects::should_defer` tells a running packet that a pause has started.
- **Code** (`plan/lxr/cm.rs: LXRConcurrentTraceObjects::do_work`): the packet loops until its own queue of children is empty, and stops when the pause kind changes.

### 2.3 What we had before (Code, P25)

`NewWork` put every packet in one list `m_work`. In each pause three functions walked lists:

- `RetireCompletedWork` walked every packet (to free the finished ones).
- `FixForwardingReaders` walked the roots, **both banks of the decrement log**, every packet, and every trace scan.
- `PublishTraceWork` (in `Resume`) walked the pending trace packets.

**Measured** (diagnostic counters that I added for the investigation and removed): in list suffix nursery, `FixForwardingReaders` rewrote 2 241 decrement log
entries of the old bank (these entries are not used any more), 57 root entries and 55 trace packets. In requests it rewrote 127 log entries, 184 roots and 115 trace packets.
No log entry of the bank of this pause was rewritten. **Inferred:** the log of this pause holds the old values of fields, which are counted objects. The nursery copy only moves
objects with count 0. So no entry of that bank names a source of this pause.

### 2.4 What F2 does

1. Trace packets are **not in `m_work`** any more. A packet is owned by the scheduler, or by `m_trace_unsubmitted` (packets that could not go to the scheduler yet).
   A processed trace packet goes to `m_trace_done`. The next `Suspend` frees `m_trace_done`. The pause visits only the work of the last epoch.
2. `FixForwardingReaders` no longer walks the log bank that the lazy decrements consumed. It walks the log of this pause only if the pause can copy mature
   objects (FinalMark, Full with mature evacuation). In a Debug build it walks the log and checks that no entry names a source of this pause
   (`InvariantViolation` if one does).
3. `PublishTraceWork` submits only `m_trace_unsubmitted`. Packets that wait in the scheduler stay where they are.
4. **Forwarding at use.** A pending trace packet that names an old source is **not** rewritten. `ProcessTraceObject` resolves the forwarding when it runs
   (`ResolveForwarded`, already there). **Inferred, and why it is safe:** a source of the nursery copy has count 0. After the pause its ValidObject bit is cleared and
   `ProcessTraceObject` skips an object with count 0. The destination is a young object that was allocated or copied inside the trace window, so it is marked (black).
   A pending packet for it would skip it anyway. For the mature copy (FinalMark and Full) the pause runs all pending trace packets itself before the copy ends.
   **Measured:** the managed cells and the tests `--nursery-reader`, `--mature-reader`, `--f1-trace` (F1 stress) and `--f8-lazy` pass. **Open:** I have no proof for
   every case, only the tests.
5. New scheduler functions: `RunQueuedInline(stage, ...)` (the pause thread runs the queued packets of a stage that is closed, to finish the trace inside a
   FinalMark or Full pause) and `SetCancelledPacketCallback` (Shutdown gives the owner the packets that it cancels, because no list owns them now).
   `LxrPauseWalkMetrics` counts what a pause visited.

### 2.5 Tests and controls

Selector `--f2-pause` (new, 2 488 checks in Release): one trace worker is blocked by a gate, so 500 and 8 000 trace packets stay pending. 200 mature holders
are overwritten, so the decrement log has 400 entries. A RefCount pause then runs, nonmoving and nursery.

- Packets visited by Retire / Fix / Publish must not follow the backlog (at most 96). **Measured:** 7 / 18 / 1 for both backlogs.
- A Release pause that copies only young objects must not walk the log (`fixLogEntries == 0`).
- A backlog at shutdown is freed (no leak, no crash).

| control | what it breaks | result (Release / Debug) |
|---|---|---|
| `LXR_F2_WALK_BACKLOG_CONTROL` | trace packets are in `m_work` again and Retire / Fix walk them | fails: retire = 8 007 for 8 001 pending, fix = 16 012 / fails |
| `LXR_F2_WALK_LOGS_CONTROL` | `FixForwardingReaders` walks both log banks again | fails in Release (log entries = 400) / **passes in Debug**: Debug always walks the log for its check, so the control cannot change it |

(`results/s26-controls.txt`.)

### 2.6 Before and after (Measured)

| item | before F2 | after F2 / final |
|---|---|---|
| packets visited by `RetireCompletedWork` with 8 000 pending trace packets | 8 007 (control) | 6 |
| packets visited by `FixForwardingReaders` with 8 000 pending, nursery | 16 012 (control) | 18 |
| `FixForwardingReaders` + `PublishTraceWork` + `RetireCompletedWork` in the profile of requests nursery | | 0.05 + 0 + 0.13 percent of all samples (`perf`, section 4.6) |
| list suffix nursery, one run, trace limit 3, wall / CPU / pauses | 13.68 s / 33.96 s / 69 (and 10.62 s / 29.46 s / 70) | with F2 alone: 5.94 s / 25.25 s / 71 (and 5.92 s / 26.13 s / 74) |

F2 alone is not enough for the goal check of section 2.8. It did not change the main benchmark either (column F2 above): after F1 the lists were short, so
the pause was not dominated by the walks. It removes the dependence on the backlog (test) and it is needed for the limit of 3 trace workers.

### 2.7 The unexplained 193 ms FinalMark (P25 open question 4)

**Measured** (`scripts/finalmark2-26.sh`, one traced run each). The FinalMark of list suffix nursery shows 148.7 ms as `total_us` in the base build. Of this:
`policy_drain_us` 78.2 ms (the wait for the lazy work of the last RC pause, before the pause), `stw_pause_us` 36.2 ms (the real pause) and `post_drain_us` 33.5 ms
(the wait for the lazy work after the pause). So **most of the "pause" was waiting, not stopped time**. In the final build: total 28.6 ms = stop 27.8 ms, with no wait.
**Inferred:** the lazy decrements of the RC pause before the FinalMark were slow because the 13 trace workers and the lazy packets shared one mutex and one queue
(base build), and the wait for them was counted in the pause. I did not measure the 193 ms run again; the numbers above are from a different run (the maximum here is 148.7 ms).

### 2.8 The goal check: a limit of 3 concurrent trace workers

P25 said: limit 3 made list suffix nursery 4.5 s into 10 to 16 s. **Measured** (`scripts/conc-limit.sh`, 2 runs each, 16 processors, 13 workers):

| build | list suffix nursery, all workers (wall / CPU) | list suffix nursery, limit 3 (wall / CPU) |
|---|---|---|
| base (P25 tip) | 4.66 s / 14.56 s and 4.39 s / 13.25 s | 13.68 s / 33.96 s and 10.62 s / 29.46 s |
| F2 only | 4.99 s / 14.52 s and 4.79 s / 14.21 s | 5.94 s / 25.25 s and 5.92 s / 26.13 s |
| F2 + F4b | 4.62 s / 13.37 s and 4.48 s / 13.11 s | 4.60 s / 13.35 s and 4.40 s / 12.95 s |

With F2 + F4b the limit costs nothing in list suffix nursery, and the trace ends (the traced runs of the final build have 1 InitialMark and 1 FinalMark in list suffix).
In the benchmark matrix (all arms, 3 repetitions) the limit of 3 against 13 workers is: list suffix nursery 4.28 s against 4.08 s, list suffix nonmoving 3.49 s against 3.23 s,
requests nonmoving 3.59 s against 3.46 s, requests nursery 3.73 s against 3.66 s (`results/s26-final-conc13-matrix.jsonl`). So the limit costs 2 to 8 percent in these cells.
The check passes (no 10 s, no unfinished trace). **Decision: the Java value is now the default.** `LXRConcGCThreads` still overrides it.

**One difference on purpose.** On a small machine the Java rule gives 1 trace worker (2 to 5 workers). **Measured** (`scripts/small2-26.sh`, `small2-trace26.sh`, `small4-26.sh`):
with 1 trace worker on 2 processors the trace of list suffix does not end (1 InitialMark, 0 FinalMark in 111 pauses), and the run needs 6.8 s and 12.4 s CPU
against 5.6 s and 8.9 s with 2 trace workers. On 4 processors: 4.9 s with 1, 4.0 s with 2, 3.9 s with 4 trace workers (nonmoving). So the default is the Java rule with
a floor of 2 trace workers on a machine with 2 or more workers (`LxrWorkerPolicy::DefaultConcurrentTraceThreads`). With 13 workers the value is 3, as in the Java LXR.
**Inferred:** our trace needs more time for each object than the Java trace, so one worker cannot finish. **Open:** I did not find the reason.
Control `LXR_F4B_NO_FLOOR_CONTROL` (the check in `--f4b-scheduler` fails).

## 3. F4b: the scheduler

### 3.1 The basics

A **deque** is a double-ended queue. A worker pushes and pops work at one end. Other workers may **steal** work from the other end. A deque that does this without a lock
is called lock-free. A global **injector** holds packets that no worker owns. Work stealing keeps all workers busy and needs no global lock.
Before F4b all packets were in queues behind one mutex. P25 measured that the CPU of requests grew from 4.65 s and 5.87 s to 5.91 s and 8.82 s (mostly system time).

### 3.2 What the Java LXR does

- **Code** (`scheduler/scheduler.rs: poll_schedulable_work_once`): a worker takes in this order: its designated work; each work bucket (`work_bucket.poll(&worker.local_work_buffer)`);
  then `worker_shared.stealer.steal()` from every other worker. `Steal::Retry` makes the worker try again. The packet types are crossbeam `Injector` and `Stealer` (the
  fields `postponed_concurrent_work: Injector<Box<dyn GCWork>>` and the `use crossbeam::deque::{Injector, Steal}` of the file).
- **Code** (`scheduler.rs: update_buckets`, `find_more_work_for_workers`, `on_last_parked`): the last worker that parks opens the next bucket.
  Concurrent buckets (Unconstrained, Concurrent) are always or often open; the STW buckets open in order. Our P3.1 to P3.3 scheduler already has this stage logic
  and F4b did not change it. **I did not change the concurrent / STW split.**
- **Code** (`cm.rs: LXRConcurrentTraceObjects::do_work`): the trace packet loops (see 2.2). Only a full buffer (`SATB_BUFFER_SIZE` 8 192) is flushed as a new packet.
- I did not read `work_bucket.rs` and `worker.rs`; I do not know their details (batch size of the steal, the local buffer).

### 3.3 What we did

Code in `lxr_work_scheduler.cpp` (class `LxrWorkScheduler::Impl`):

1. **Chase-Lev deque** (`WorkDeque`) per worker and per class: one for general packets, one for trace packets. The owner pushes and pops at the bottom, thieves
   steal at the top with a compare-and-swap. The ring grows when full. All operations are sequentially consistent (so that ThreadSanitizer can check it). The
   Java LXR uses the crossbeam deque. **Difference:** this one is my own code, with seq_cst instead of the fence-based version.
2. **Injector** per stage (`Injector`): a deque behind a small spin lock (yield after 32 spins). Workers take **batches** (up to 16, about `1/workers` of the queue), and the
   batch goes to the own deque (as `steal_batch_and_pop`). **Difference:** the Java injector is lock-free. Ours holds a spin lock for a few instructions and one lock operation
   covers up to 16 packets. **Open:** a lock-free injector.
3. **A worker that runs a packet submits into its own deque without the scheduler mutex** (`PushQueued`; a trace packet only while the worker runs a trace packet).
   Other threads (mutators, the pause thread) still take the mutex in `Submit`.
4. **Packet claim protocol.** The worker counts the packet as running (`executing++`, trace count), and only then checks that its stage is still runnable.
   `InterruptConcurrentWork` closes the stage first and then waits for `executing == 0`. One of the two sees the other, so no packet starts after the pause saw the stage idle.
   A packet that is not runnable goes back to the deque or the injector.
5. **Published lazy-decrement packets** are copied into vectors at publication and taken with one atomic increment each (`ActiveDeferred`). No mutex.
6. **Parking.** An idle worker raises `parkedWorkers`, looks once more for work (`HasWork`), and sleeps on its own condition variable. A producer pushes first and reads
   `parkedWorkers` second. This is the usual Dekker pattern, so no wake-up is lost. The stage logic, the completion counters and the all-workers-parked check are unchanged.
7. **Trace limit** counts running trace packets with an atomic; a trace packet that cannot run because of the limit wakes nobody.
8. **In-packet loop for the trace packet** (`ProcessTraceWork`, `ShouldStopTraceLoop`): the packet collects its children in one batch (8 192) and loops over the batch until no
   child is left, as the Java trace loop. It stops when the scheduler has been asked to interrupt the concurrent work (`IsInterruptionRequested`) or the engine left the mutator phase.
   Before, each object that had children made a new packet. Previously only a Full trace used a batch.
   The batch is flushed before a scan waits at its checkpoint (the test fixtures need the packets).

### 3.4 Why it is correct

- **Code.** Every packet has the state `CallerOwned -> Queued -> Executing -> Completed`, changed with compare-and-swap. A packet that two thieves took twice is
  detected as an ownership violation (counter) and not run twice. The bucket counters `outstanding` and `executing` are atomic.
- **Measured.** No lost and no duplicated packet in the stress test (60 000 packets from 4 threads and 8 workers, packets that make packets). Control without the claim
  CAS fails.
- **Measured.** ThreadSanitizer: the stealing and stress tests, and the P3.1, P3.3 and P3.4 suites, gave **0 reports** (`results/tsan-*.log`, `scripts/tsan26.sh`, `tsan-f4b.sh`).
  The program must run with `setarch -R` (the shadow map). The trace-loop test needs the engine, whose big address reservation does not fit the TSan shadow map;
  its check fails under TSan for that reason, and the whole engine was **not** checked with TSan. **Open:** TSan on the whole engine.

### 3.5 Tests and controls

Selector `--f4b-scheduler` (new, 4 044 checks):

| test | what it shows | control that must fail |
|---|---|---|
| `ValidateF4bStealing` | 1 packet makes 20 000 packets inside a worker. **Measured:** 8 of 8 workers ran packets, the busiest ran 2 855 | `LXR_F4B_NO_STEAL_CONTROL` (workers never steal: 1 worker ran 20 000) |
| `ValidateF4bStress` | 60 000 packets, 4 submitting threads, 8 workers: each runs once, none twice, none lost | `LXR_F4B_STEAL_NO_CAS_CONTROL` (a thief takes without the CAS) |
| `ValidateF4bTraceLoop` | a chain of 2 000 objects is traced by 6 packets | `LXR_F4B_NO_TRACE_LOOP_CONTROL` (2 005 packets) |
| `ValidateF4bPauseKeepsTracePackets` | a closed trace stage keeps its 3 000 packets; the pause runs them itself, exactly once | |
| `ValidateF4bTraceLimitDefault` | the default limit: 1, 2, 2, 2, 2, 2, 3, 7, 16 for 1, 2, 4, 6, 8, 9, 13, 26, 64 workers | `LXR_F4B_NO_FLOOR_CONTROL` |

**Measured** (`results/s26-controls.txt`): each baseline passes, each control fails, in Debug and in Release. Four old tests changed:
`--trace-batching` (a check for a flush that now happens only when the batch fills) and `--nursery-reader`, `--mature-reader` (they need a packet for each child, so they
switch the trace loop off with `SetTraceLoopForTest(false)`).

### 3.6 The CPU of P25 (system time) — isolated, then fixed

**Measured**, requests nursery, 2 runs each, the same session (`scripts/cpucmp.sh`, `results/s26-cpucmp-f2-f4b.txt`):

| build | wall s | CPU s | user s | system s |
|---|---|---|---|---|
| F2 (global mutex) | 4.62 and 4.35 | 9.20 and 9.21 | 6.75 and 6.53 | **2.87 and 2.89** |
| F4b (deques) | 4.12 and 4.08 | 6.06 and 6.05 | 5.73 and 5.69 | **0.50 and 0.58** |

List suffix nursery: system 1.39 and 1.45 s against 0.41 and 0.50 s; wall 4.86 against 4.78 and 4.48 s. So the system time of P25 was the global mutex (as I
inferred in P25) and it is gone. **Measured, `perf`** of the final build (`cpu-clock:u`, user samples only; `results/` has no perf data, the commands are in
`scripts` of P24): in requests nursery the scheduler functions are `RunPacket` 4.2, `TryClaim` 3.8, `Poll` 2.6, `Submit` 2.7, `PushQueued` 1.6 percent of the samples.
In list suffix nursery they do not show among the first 24 symbols; the top ones are `TryCopyNursery` 18.6, `DecrementWithDeathCore` 9.9, `ProcessIncrement` 7.7,
`IsNurseryFast` 7.5 percent. So the remaining CPU is per-object work.

### 3.7 Before and after (Measured, same session, see the table in section 1)

| cell | F2 build | F4b build | final |
|---|---|---|---|
| requests nonmoving wall / CPU | 4.25 / 6.10 | 4.05 / 4.47 | 3.59 / 4.40 |
| requests nursery wall / CPU | 4.38 / 9.21 | 4.10 / 5.99 | 3.73 / 5.50 |
| list suffix nonmoving wall / CPU | 4.02 / 11.42 | 3.72 / 9.64 | 3.49 / 10.58 |
| list suffix nursery wall / CPU | 4.74 / 13.91 | 4.47 / 12.80 | 4.28 / 13.53 |

### 3.8 Small machines (1 to 4 processors)

**Measured** (`scripts/small26.sh`, `results/s26-small.txt`; the process is pinned with `taskset`; workers follow the processor count): every cell ends with the same checksum on 1, 2 and 4
processors. Before the floor of 2 trace workers (section 2.8) the final build was slower than base in list suffix on 2 and 4 processors; with the floor, 2 processors:
list suffix nonmoving 5.60 s and 5.66 s against 5.91 s and 6.15 s (base); nursery 7.24 s and 7.29 s against 7.83 s (base). On 1 processor (from the run before the floor; 1 worker, so the floor does not apply) list suffix nursery
is 15.9 s against 69.0 s (base) and nonmoving 11.8 s against 14.5 s. The full table with the floor on 1 and 4 processors was **not** repeated after the floor.

## 4. F8: no wait for lazy decrements

### 4.1 The basics

After an RC pause the decrements of the dead objects and the sweep of blocks run as **lazy work**. Before F8 the thread that made the pause (usually the allocating
thread) waited until this work was finished (`post_drain`), although the program was already running again. P24 measured 1.5 s of 5.8 s in list suffix with the trace off.

### 4.2 What the Java LXR does (Code)

- `plan/lxr/barrier.rs: flush_decs_and_satb`: decrement packets are `postpone_prioritized`. They wait in a list.
- `scheduler.rs: on_gc_finished`: `schedule_postponed_concurrent_packets`, then `resume_mutators`, then `schedule_concurrent_packets(queue, pqueue)`: the packets go to the
  Unconstrained bucket and the workers are woken (`wakeup_all_conc_workers`). The mutators run at the same time.
- `global.rs: schedule_collection`: `if !LazySweepingJobs::all_finished() { gc_log!("WARNING: LXR Lazy Sweeping Not Finished"); counters.gc_with_unfinished_lazy_jobs += 1 }`.
  **So a new pause does not wait for unfinished lazy work. It counts it.**
- `lib.rs: LazySweepingJobsCounter`: when the last lazy packet is dropped, `end_of_lazy` runs `LXR::on_lazy_sweeping_finished`, which computes the hints for the next
  pause (`decide_next_gc_may_perform_cycle_collection`). At the end of every pause `gc_pause_end` resets `hint_emergency_gc` to false.
- `global.rs: schedule_emergency_full_heap_collection` sets `DISABLE_LASY_DEC_FOR_CURRENT_GC`: a Full pause does its decrements inside the pause.
- **I found no wait of a mutator for lazy work in the allocation path** (`util/alloc/allocator.rs: alloc_slow_inline` has no reference to it). The condition for a mutator to help or
  wait does not exist in the Java LXR. After an allocation failure it triggers a collection; `global_state.rs: set_collection_kind` makes it emergency when the last collection was
  exhaustive, there were more than 1 attempts and the heap cannot grow.

### 4.3 What we had (Code)

`LxrGcHeap::CollectLocked` called `m_engine.Drain` after each pause (unless the trace was Concurrent). `UpdatePolicyHints` (`DrainLazySweep`) waited for the last generation
before each collection. `RunPause` waited (`DrainEpoch`) before it stopped the program. `Alloc` already waited for the lazy sweep after an allocation failure and retried,
before it collected.

### 4.4 What F8 does

`CollectLocked` does not drain after an RC or FinalMark pause. It drains (`wait`) when one of these is true: `LXRWaitForLazy=1` (setting, restores the old behaviour),
the pause is Full, the collection is forced (user, emergency, emergency hint), or the caller asks (`waitForLazy`: `WaitUntilConcurrentGCComplete` and `TryGrow`).
`GC_LxrPrototype_Collect` (the entry point of the test fixtures) is synchronous like a user collection. `FinishLazy` announces the end of a concurrent collection
(`m_concurrent_done`) because a FinalMark pause no longer waits for its sweep.
Where a mutator waits now: (a) the next collection waits for the last generation, because the decrement log banks and the double-buffered lazy queues are reused (**Code:** `ReleaseEpoch`
resets the bank that lazy packets read; `BeginStwCycle` returns `Busy` while lazy packets are left). This is a difference from the Java LXR, which can start a pause with lazy work left; **Open:**
three banks or copies would remove it. (b) `Alloc` waits for the lazy sweep after an allocation failure and retries before it collects; this is the "defined condition".
**Inferred:** in practice by the time the next collection starts (about 30 ms later) the workers have finished, so the wait is short. I did not measure how long it is.

### 4.5 Why it is correct

- **Decrement vs allocation of the same block, the cycle sweep, finalizers (Code).** The lazy sweep of a block starts only after all decrements of its generation have finished
  (`LxrMatureSweepContinuation`, the completion counter of P3.4; test "promotion precedes release and decision"). A block is reusable only after that sweep. A young allocation takes
  fresh blocks or holes of reusable blocks; both are decided by the sweep. `SweepCycles` frees only objects with count above 0 and no mark. A newborn after FinalMark has count 0.
  Finalizers are held back while `m_cycle_sweep` is set (`IsCycleSweepPending`, `GetNextFinalizable`); `FinishLazy` releases them.
  This design existed for the trace window, where lazy work already ran with the program (P3.3, P17); F8 makes it the usual case.
- **Measured.** Selector `--f8-lazy` (new, 114 956 checks Release): a random graph of 2-field objects, 60 rounds per seed, 6 seeds, nonmoving and nursery. Between pauses the program allocates,
  stores and reads while **every lazy packet sleeps 300 us first**, so lazy work is pending in 59 of 60 rounds. Pauses are RefCount, InitialMark, FinalMark and Full (11 to 14 trace cycles
  and 12 to 16 cycle sweeps per seed). After each pause every reachable object must be counted, owned and linked as in the model, and no new object may take the address of a reachable one.
  At the end a Full pause and a drain must leave exactly the reachable objects alive (the oracle knows each byte: `live == reachable` in all 12 runs).

| control | what it breaks | result |
|---|---|---|
| `LXR_F8_WAIT_CONTROL` | the pause drains (the old behaviour) | fails (6 of 115 502 checks): the rounds that overlap with lazy work fall from 59 to 16 of 60 |
| `LXR_S4_NO_SWEEP_WAIT_CONTROL` | the next pause does not wait for the last lazy generation | fails: "F8 pause completes", Release crashes (exit 139), Debug fails 12 checks |

With LXR_F8_WAIT_CONTROL the 16 overlapping rounds are the rounds after an InitialMark pause (it never drained); the check for 'many rounds' uses the number 15 per seed and fails in some seeds. So the control proves that the test **sees** the overlap; the other control proves that the test **detects** a missing safety wait.
I looked for a control that corrupts memory by an early block release and did not find a safe place to break; the existing `LXR_S4_EARLY_REUSABLE_CONTROL` and `LXR_CYCLE_GATE_CONTROL` do not fail this test (**Measured**).

- **Measured.** First run of the managed cells with F8: 9 of 90 failed (`full` and `churn` cells). **Cause (not a collector bug):** the fixtures call `GC_LxrPrototype_Collect` and then read
  metrics; they need the lazy work finished. Fix: that entry point waits (section 4.4). Then 90 of 90 pass.

### 4.6 Before and after (Measured, final build, 3 repetitions, `results/s26-final-waitlazy-*`)

`LXRWaitForLazy=1` (the old wait) against the default, same build, same session:

| cell | wait after every pause | default (F8) |
|---|---|---|
| requests nonmoving wall | 4.13 | 3.59 |
| requests nursery wall | 4.08 | 3.73 |
| list suffix nonmoving wall | 3.76 | 3.49 |
| list suffix nursery wall | 4.53 | 4.28 |
| binarytrees nonmoving / nursery wall | 0.50 / 0.53 | 0.49 / 0.62 (bimodal, see 1.2) |

F8 gives 7 to 13 percent wall time in the programs with long pauses. The CPU does not change. The `post_drain` phase of the RC pause is 0 ms after F8 (it was 13 to 34 ms per pause in
list suffix nursery).

## 5. The flaky P3.4 check

The check `child registration blocks decrement zero and release` failed in 5 of 30 runs (P25). **Cause: the test, not the product.** The parent packet submits its child inside its own
`Execute`, and the registration of the parent ends only when `Execute` returns. The child can start on another worker before that. The test took the counter snapshot as soon as the child started,
so it could see `decrementOutstanding == 2` (parent and child) instead of 1. **Fix:** the test waits for the parent packet to complete first. **Measured** (`scripts/flake-p34.sh`, new scheduler): old test 16
failing runs of 100, new test 0 of 100. The old test is the control. The product counter was correct in all runs.

## 6. What I ran, and what I did not run

All **Measured**. "Passed" means the command printed it. Outputs are in `results/`.

| item | how | result |
|---|---|---|
| Native selectors, Linux Debug and Release, final tree | `scripts/chain26.sh`, P17 `native-linux.sh` (new: `--f2-pause`, `--f4b-scheduler`, `--f8-lazy`) | 40 of 40 pass in both |
| Legacy suites P2.1 to P3.4, Linux | P17 `legacy-linux.sh` | 9 of 9 pass (P3.4 57/57, run once; flake test above) |
| Managed cells, Debug collector, 3 modes, ReadyToRun on | `scripts/run-managed.sh` | 90 of 90 ok (final tree). F4b tree: 90 of 90. F8 tree before the test-entry fix: 81 of 90 (section 4.5) |
| Controls | `scripts/controls-all26.sh` | all baselines pass, all controls fail in Release; in Debug all fail except `LXR_F2_WALK_LOGS_CONTROL`, which a Debug build cannot change (2.5) |
| TSan | `tsan26.sh`, `tsan-f4b.sh` | 0 reports in P3.1, P3.3, P3.4 and the F4b stealing and stress tests. Whole engine **not** checked |
| Windows Release, MSVC, `subst V:` | `native-windows.ps1`, `build-windows.ps1` | 40 of 40 native selectors pass; `clrlxrgc.dll` builds, SHA256 `3A8E9F5B...1594` (`results/windows-*`). Built once, at the end |
| Stock helper hash | `scripts/hash26.sh` | `46cee8bd87fd6252f5265267148d0a34`, equal to the baseline. **Code:** only `libclrlxrgc` files changed; `libcoreclr.so` was not rebuilt |
| Benchmark | `scripts/bench-all.sh` | the table of section 1; no failed run; checksums equal |
| Small machines | `small26.sh`, `small2-26.sh`, `small4-26.sh` | section 3.8 |

**Not run:** Linux Debug **runtime** (`d2dbgrt` frame) and the managed cells with a Debug runtime; Windows Debug and Windows managed cells; ARM64, NativeAOT, profiler heap walks, GCStress;
a measurement of how long the next collection waits for lazy work after F8; the benchmark on a second machine; the Java LXR (not run again; comparison by source reading and ratios only);
the small-machine table after the floor on 1 and 4 processors.

## 7. Remaining gaps against the Java LXR (ranked)

1. **The next pause waits for the last lazy generation** (4.4). The Java LXR starts a pause with lazy work left. Needs 3 log banks or copies. **Open.**
2. **Per-object RC cost** (copy, decrement, increment: `TryCopyNursery` 18.6, `DecrementWithDeathCore` 9.9 percent of the user samples in list suffix). LXR CPU is 2.4 to 5.1 times WKS. P24 F9, F10. **Open.**
3. **Our trace is slower per worker than the Java trace** (1 trace worker cannot finish list suffix; the floor of 2 is a deviation). Reason unknown. **Open.**
4. **Full pause** on one thread, without defragmentation if no block is free (P24 F6, F7). **Open.**
5. **The injector is a spin lock, not lock-free** (3.3). The deque is lock-free. **Open.**
6. **The concurrent / STW bucket split** was not compared again with `work_bucket.rs` (not read). **Open.**
7. Forwarding at use for pending trace packets is argued, not proved (2.4). **Open.**
8. The two deviations on purpose of P25 (F1 weak targets, F5 occupied pages) are unchanged.

## 8. Open questions for the coordinator

1. Do you accept the floor of 2 trace workers (2.8)? Without it the Java rule gives 1 worker on 2 to 5 processors and list suffix does not finish its trace there.
2. Should `LXRWaitForLazy` stay as a setting? It is the way to measure F8, and it costs nothing.
3. Is a lock-free injector needed? The scheduler is not in the profile of list suffix; it is 15 percent of the user samples in requests.
4. The Full pause and the per-object cost are the next big items (7.2 and 7.4). Which first?

## 9. Commits and files

Commits on `kkokosa-lxr-scheduler-f2-f4b-f8` (local only; hashes of the tip are in the handoff):

| title |
|---|
| F2: pause cost does not follow the trace backlog (no walk over pending trace packets, no walk over the consumed decrement log) |
| F4b: work-stealing deques and a batch injector replace the global-mutex packet queues; the trace packet loops |
| F2: the concurrent trace uses the Java LXR worker limit by default (ConcGCThreads, 3 of 13) |
| F8: the thread that made a pause does not wait for the lazy decrements and the lazy sweep |
| F2: the default trace limit has a floor of 2 workers on small machines (different from the Java LXR on purpose) |
| P26: report, results, P19 update |

Product files: `lxr_reference_counting.cpp/.h`, `lxr_work_scheduler.cpp/.h` (new `Impl`), `lxr_work_packet.h`, `lxr_gc.cpp/.h`, `lxr_gcload.cpp`, `lxr_worker_policy.h`.
Tests: `P4/lxr-rc-validation.cpp` (selectors `--f2-pause`, `--f4b-scheduler`, `--f8-lazy`), `P3.4/lxr-completion-counter-validation.cpp` (the race), the selector lists in `P17-pause-cost-and-reserve/native-linux.sh`
(which also removes the old test binary before it compiles, so a failed compile cannot run an old binary) and `native-windows.ps1`.
Settings: `LXRWaitForLazy` (new). `LXRConcGCThreads` default changed (2.8).
`scripts/`: `bench-all.sh`, `bench-mature.sh`, `cell.sh`, `cpucmp.sh`, `conc-limit.sh`, `chain26.sh`, `controls26.sh`, `controls-all26.sh`, `flake-p34.sh`, `tsan26.sh`, `tsan-f4b.sh`, `small*.sh`,
`finalmark*.sh`, `pausestw.py`, `hash26.sh`, `mkframe26.sh`, `frames26.sh`, `show26.sh`, `natsum.sh`, `collect26.sh`, `sync26.sh`, `bt-cpu.sh`.
`results/`: `s26-*` (benchmark tables, matrices, traces, small-machine runs, controls), `*-nat-*.txt`, `*-legacy.txt`, `managed-*`, `windows-*`.

To repeat (paths are those of this machine): `scripts/sync26.sh`, then `/root/p25/build.sh <worktree> Release`, then `scripts/bench-all.sh <prefix>`.
