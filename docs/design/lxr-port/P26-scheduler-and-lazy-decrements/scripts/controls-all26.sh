#!/bin/bash
# P26: every control of the three steps, in Debug and in Release. Each baseline must PASS, each control must FAIL.
# usage: controls-all26.sh   (output: one line per run)
C=/root/p25/scripts26/controls26.sh
for cfg in Release Debug; do
  LINES_MAX=0 $C $cfg --f2-pause LXR_F2_WALK_BACKLOG_CONTROL LXR_F2_WALK_LOGS_CONTROL
  LINES_MAX=0 $C $cfg --f4b-scheduler LXR_F4B_NO_STEAL_CONTROL LXR_F4B_STEAL_NO_CAS_CONTROL LXR_F4B_NO_TRACE_LOOP_CONTROL LXR_F4B_NO_FLOOR_CONTROL
  LINES_MAX=0 $C $cfg --f4-scheduler LXR_F4_NO_CONC_LIMIT_CONTROL
  LINES_MAX=0 $C $cfg --f8-lazy LXR_F8_WAIT_CONTROL LXR_S4_NO_SWEEP_WAIT_CONTROL
done
