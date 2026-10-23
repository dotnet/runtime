#!/bin/bash
# P26: run a P26 script from the Windows worktree with LF line endings.
# usage: runwin.sh <worktree-wsl-path> <script-name> [args...]
W=$1; S=$2
shift 2
tr -d '\r' < "$W/docs/design/lxr-port/P26-scheduler-and-lazy-decrements/scripts/$S" | bash -s "$@"
