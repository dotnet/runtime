# Pipelines to Monitor

Azure DevOps org: `dnceng-public`, project: `public`
(unless marked as **private** — those require `dnceng` / `internal`)

To add or remove pipelines, edit this table.

## Cached Definition ID Mapping

Last verified: 2026-02-24. Use these IDs directly — do NOT re-resolve
unless a pipeline returns unexpected results.

| Pipeline Name | Def ID | Notes |
|---------------|--------|-------|
| runtime-coreclr jitstress | 109 | |
| runtime-coreclr jitstressregs | 110 | |
| runtime-coreclr jitstress2-jitstressregs | 111 | |
| runtime-coreclr gcstress0x3-gcstress0xc | 112 | |
| runtime-coreclr gcstress-extra | 113 | |
| runtime-coreclr jitstress-isas-x86 | 115 | |
| runtime-coreclr jitstress-isas-arm | 116 | |
| runtime-coreclr jitstressregs-x86 | 117 | |
| runtime-coreclr libraries-jitstressregs | 118 | |
| runtime-coreclr libraries-jitstress2-jitstressregs | 119 | |
| runtime-jit-experimental | 137 | |
| runtime-coreclr libraries-jitstress | 138 | |
| runtime-coreclr ilasm | 140 | |
| runtime-coreclr pgo | 144 | |
| runtime-coreclr libraries-pgo | 145 | |
| runtime-coreclr superpmi-replay | 150 | |
| runtime-coreclr superpmi-asmdiffs-checked-release | 153 | |
| runtime-coreclr jitstress-random | 159 | |
| runtime-coreclr libraries-jitstress-random | 160 | |
| runtime-coreclr pgostress | 230 | |
| runtime-coreclr jitstress-isas-avx512 | 235 | |
| runtime-coreclr superpmi-collect | — | **Private — skip** |
| runtime-coreclr jitrollingbuild | — | **Private — skip** |

## Pipeline Details

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

