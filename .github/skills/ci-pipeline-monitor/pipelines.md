# Pipelines to Monitor

Azure DevOps org: `dnceng-public`, project: `public`
(Skip pipelines marked as **private** — those require `dnceng` / `internal`)

## Pipeline Details

**This is the source of truth.** To add or remove pipelines, edit this table.
Add a new row with the pipeline name; the agent will automatically resolve the
definition ID and update the Cached Definition ID Mapping table below.
To remove a pipeline, delete its row from this table (and optionally from the
cached mapping table).

| Pipeline Name | Schedule | Notes |
|---------------|----------|-------|
| runtime-coreclr superpmi-collect | Weekly (Sun) + JIT-EE GUID changes | **Private — skip**. Critical for JIT productivity. |
| runtime-coreclr superpmi-replay | Nightly | |
| runtime-coreclr jitrollingbuild | On JIT merge | **Private — skip**. Normally very reliable. |
| runtime-coreclr superpmi-asmdiffs-checked-release | Weekends | Check every Monday. |
| runtime-jit-experimental | Sat + Sun | Mostly OSR / partial compilation.|
| runtime-coreclr jitstress | Daily, 7 jitstress configs per platform |
| runtime-coreclr jitstressregs | |
| runtime-coreclr jitstress2-jitstressregs | |
| runtime-coreclr jitstress-isas-x86 | |
| runtime-coreclr jitstress-isas-arm | |
| runtime-coreclr jitstressregs-x86 | |
| runtime-coreclr gcstress0x3-gcstress0xc | |
| runtime-coreclr gcstress-extra | |
| runtime-coreclr gc-simulator | | |
| gc-standalone | | ADO name differs from display name |
| runtime-coreclr libraries-jitstress | |
| runtime-coreclr libraries-jitstressregs | |
| runtime-coreclr libraries-jitstress2-jitstressregs | |
| runtime-coreclr ilasm | |
| runtime-coreclr jitstress-isas-avx512 | |
| runtime-coreclr jitstress-random | Need exact stress mode value from logs |
| runtime-coreclr libraries-jitstress-random | Need exact stress mode value from logs |
| runtime-coreclr pgo | |
| runtime-coreclr libraries-pgo | |
| runtime-coreclr pgostress | |
| runtime-coreclr jit-cfg | Sat-Sun 22:00 UTC | Control flow guard. |
| runtime-coreclr crossgen2 | Daily | Crossgen2 R2R + comparison tests. |
| runtime-coreclr crossgen2 outerloop | Daily | Crossgen2 outerloop R2R tests. |
| runtime-coreclr crossgen2-composite | Daily | Crossgen2 composite R2R tests. |
| runtime-coreclr crossgen2-composite gcstress | Weekends | Crossgen2 composite with GC stress. |
| runtime-coreclr r2r | | |
| runtime-coreclr r2r-extra | | |
| runtime-interpreter | | ADO name differs from display name |
| runtime-libraries-interpreter | | ADO name differs from display name |
| runtime-nativeaot-outerloop | | |
| runtime-diagnostics | | |
| runtime-coreclr outerloop | | |

## Cached Definition ID Mapping

Auto-populated by the agent. Do NOT re-resolve IDs that are already cached.
The agent compares Pipeline Details against this table, resolves any missing
entries via the AzDO Definitions API, and adds new rows here.

| Pipeline Name | Def ID | Notes |
|---------------|--------|-------|
| runtime-coreclr superpmi-replay | 150 | |
| runtime-coreclr superpmi-asmdiffs-checked-release | 153 | |
| runtime-jit-experimental | 137 | |
| runtime-coreclr jitstress | 109 | |
| runtime-coreclr jitstressregs | 110 | |
| runtime-coreclr jitstress2-jitstressregs | 111 | |
| runtime-coreclr jitstress-isas-x86 | 115 | |
| runtime-coreclr jitstress-isas-arm | 116 | |
| runtime-coreclr jitstressregs-x86 | 117 | |
| runtime-coreclr gcstress0x3-gcstress0xc | 112 | |
| runtime-coreclr gcstress-extra | 113 | |
| runtime-coreclr gc-simulator | 123 | |
| gc-standalone | 146 | ADO name differs from display name |
| runtime-coreclr libraries-jitstress | 138 | |
| runtime-coreclr libraries-jitstressregs | 118 | |
| runtime-coreclr libraries-jitstress2-jitstressregs | 119 | |
| runtime-coreclr ilasm | 140 | |
| runtime-coreclr jitstress-isas-avx512 | 235 | |
| runtime-coreclr jitstress-random | 159 | |
| runtime-coreclr libraries-jitstress-random | 160 | |
| runtime-coreclr pgo | 144 | |
| runtime-coreclr libraries-pgo | 145 | |
| runtime-coreclr pgostress | 230 | |
| runtime-coreclr jit-cfg | 155 | |
| runtime-coreclr crossgen2 | 124 | |
| runtime-coreclr crossgen2 outerloop | 134 | |
| runtime-coreclr crossgen2-composite | 136 | |
| runtime-coreclr crossgen2-composite gcstress | 141 | |
| runtime-coreclr r2r | 120 | |
| runtime-coreclr r2r-extra | 114 | |
| runtime-interpreter | 316 | ADO name differs from display name |
| runtime-libraries-interpreter | 330 | ADO name differs from display name |
| runtime-nativeaot-outerloop | 265 | |
| runtime-diagnostics | 309 | |
| runtime-coreclr outerloop | 108 | |
| runtime-coreclr superpmi-collect | — | **Private — skip** |
| runtime-coreclr jitrollingbuild | — | **Private — skip** |
