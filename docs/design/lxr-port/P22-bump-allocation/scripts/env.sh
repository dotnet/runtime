#!/bin/bash
# P22 (D2): shared settings of the Linux scripts. Edit the three paths for your machine.
#   W  the repository (the Windows worktree seen from WSL)
#   T  the Linux build tree (a copy of the sources that was built; the sources are copied over it, CRLF to LF)
#   D  the work directory (frames, fixtures, logs)
export W=/mnt/c/Users/konradkokosa/.copilot/repos/copilot-worktrees/runtime-fork/kkokosa-symmetrical-garbanzo
export T=/root/d1/main
export D=/root/d2
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
