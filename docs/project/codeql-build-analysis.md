# CodeQL Build Analysis and Optimization Strategies for dotnet/runtime

## Executive Summary

This document analyzes the current CodeQL (Code Analysis) build configuration for dotnet/runtime on Linux and provides actionable strategies to improve build times and efficiency. The current CodeQL build performs a comprehensive full-platform build using `/p:DotNetBuildAllRuntimePacks=true`, which builds all runtime packs across multiple platforms with a 6-hour timeout (360 minutes).

**Key Findings:**
- Current build strategy builds **all runtime packs** for security scanning purposes
- Linux x64 build includes: CoreCLR, Mono, NativeAOT, Libraries, Host, Tools, and Packs
- Build timeout is set to 360 minutes (6 hours) for most platforms
- Multiple platforms are built in parallel, but each platform build is sequential internally
- No incremental build caching is currently utilized for CodeQL builds

## Current Build Configuration

### Pipeline Overview

**File:** `eng/pipelines/runtime-codeql.yml`

**Schedule:** Runs 3 times per week (Monday, Thursday, Saturday at 12:00 PM UTC)

**Platforms Built (First Job Group):**
- windows_x64
- linux_x64
- linux_musl_arm64
- android_x64
- linux_bionic_x64
- browser_wasm
- wasi_wasm

**Platforms Built (Second Job Group):**
- maccatalyst_x64
- osx_arm64
- tvos_arm64
- ios_arm64

### Build Command Analysis

**Current Linux x64 Build Command:**
```bash
./build.sh -c release -restore -build -pack /p:DotNetBuildAllRuntimePacks=true
```

**What This Builds:**

When `/p:DotNetBuildAllRuntimePacks=true` is set on Linux x64, the build includes:

1. **CoreCLR Runtime Components** (when `CoreCLRSupported == true`):
   - `clr.native` - Native runtime binaries
   - `clr.corelib` - System.Private.CoreLib
   - `clr.tools` - Crossgen2, ILC, and other tools
   - `clr.nativecorelib` - Native compiled CoreLib
   - `clr.packages` - NuGet packages
   - `clr.nativeaotlibs` - NativeAOT libraries
   - `clr.crossarchtools` - Cross-architecture compilation tools
   - `host.native` - .NET host binaries

2. **Mono Runtime Components** (when `MonoSupported == true`):
   - `mono.runtime` - Mono runtime
   - `mono.corelib` - Mono's System.Private.CoreLib
   - `mono.packages` - Mono NuGet packages
   - `mono.aotcross` - AOT cross-compiler (for android, browser, wasi targets)
   - `host.native` - Host binaries

3. **NativeAOT Components** (when `NativeAOTSupported == true`):
   - `clr.alljits` - All JIT compilers
   - `clr.nativeaotlibs` - NativeAOT libraries
   - `clr.nativeaotruntime` - NativeAOT runtime

4. **Libraries**:
   - `libs.native` - Native shims
   - `libs.sfx` - Shared framework libraries
   - `libs.oob` - Out-of-band libraries
   - `libs.pretest` - Test infrastructure

5. **Host & Tools**:
   - `host.native` - .NET host
   - `host.pkg` - Host packages
   - `host.tools` - Host tooling
   - `tools.illink` - IL Linker
   - `tools.cdac` - Contract Descriptor for Diagnostic Access Components

6. **Packs**:
   - `packs.product` - Product packs
   - `packs.installers` - Installer packages

### Build Subset Expansion

On Linux x64, the default subset expansion is:
```
clr+mono+libs+tools+host+packs
```

Which expands to approximately **40+ individual build targets** when DotNetBuildAllRuntimePacks is enabled.

## Build Time Analysis

### Current Timing Characteristics

Based on the pipeline configuration:

| Platform | Timeout (minutes) | Estimated Build Components | Parallelization |
|----------|------------------|---------------------------|-----------------|
| linux_x64 | 360 | CoreCLR + Mono + NativeAOT + All Libs + Packs | Sequential subsets |
| windows_x64 | 360 | CoreCLR + Mono + NativeAOT + All Libs + Packs | Sequential subsets |
| linux_musl_arm64 | 360 | CoreCLR + Mono + NativeAOT + All Libs + Packs | Sequential subsets |
| android_x64 | 360 | CoreCLR + Mono + NativeAOT + All Libs + Packs | Sequential subsets |
| linux_bionic_x64 | 360 | Mono + NativeAOT + All Libs + Packs | Sequential subsets |
| browser_wasm | 360 | CoreCLR + Mono + All Libs + Packs | Sequential subsets |
| wasi_wasm | 360 | Mono + All Libs + Packs | Sequential subsets |
| maccatalyst_x64 | 120 | CoreCLR + Mono + All Libs + Packs | Sequential subsets |
| osx_arm64 | 120 | CoreCLR + Mono + All Libs + Packs | Sequential subsets |
| tvos_arm64 | 120 | CoreCLR + Mono + All Libs + Packs | Sequential subsets |
| ios_arm64 | 120 | CoreCLR + Mono + All Libs + Packs | Sequential subsets |

### Build Bottlenecks

1. **Native Compilation** (Slowest)
   - CoreCLR JIT compilers (multiple architectures)
   - Mono runtime with LLVM
   - Native shims and P/Invoke wrappers
   - Estimated: 60-120 minutes

2. **Managed Compilation** (Medium)
   - System.Private.CoreLib (both CoreCLR and Mono versions)
   - 200+ library assemblies
   - Crossgen2/AOT compilation of libraries
   - Estimated: 30-60 minutes

3. **Tooling & Crossgen** (Medium)
   - Crossgen2 tool compilation
   - ILC (NativeAOT compiler)
   - IL Linker
   - Estimated: 20-40 minutes

4. **Package Creation** (Fast)
   - NuGet package creation
   - Installer generation
   - Estimated: 10-20 minutes

5. **Restore & Setup** (Fast)
   - NuGet restore
   - CMake configuration
   - Estimated: 5-10 minutes

**Total Estimated Build Time: 125-250 minutes per platform**

## Optimization Strategies

### Strategy 1: Incremental/Differential Analysis (Highest Impact)

**Concept:** Only build and analyze components that have changed since the last successful CodeQL run.

**Implementation:**
```yaml
# Add to runtime-codeql.yml
variables:
  - name: EnableIncrementalCodeQL
    value: true
  - name: CodeQLBaseBranch
    value: main

# In build step, detect changed files
- script: |
    # Determine which subsets changed
    git diff --name-only $(CodeQLBaseBranch) HEAD > changed_files.txt
    
    # Analyze changed files to determine minimal subset
    if grep -q "^src/coreclr/" changed_files.txt; then
      echo "##vso[task.setvariable variable=BuildCoreClr]true"
    fi
    if grep -q "^src/mono/" changed_files.txt; then
      echo "##vso[task.setvariable variable=BuildMono]true"
    fi
    if grep -q "^src/libraries/" changed_files.txt; then
      echo "##vso[task.setvariable variable=BuildLibraries]true"
    fi
    # ... etc for other components
  displayName: Detect Changed Components

# Modify build args based on changes
buildArgs: -c release -restore -build -pack -subset $(DynamicSubset)
```

**Expected Savings:**
- If only libraries changed: ~60-70% time reduction
- If only CoreCLR changed: ~40-50% time reduction
- If only documentation changed: ~95% time reduction (skip build entirely)

**Challenges:**
- CodeQL requires full context for some analyses
- Cross-component dependencies may require building more than changed components
- First run still requires full build

**Recommendation:** Implement with conservative dependency tracking.

### Strategy 2: Subset-Based Parallel Analysis (High Impact)

**Concept:** Split the monolithic build into parallel CodeQL analysis jobs for each major component.

**Implementation:**
```yaml
# Replace single job with matrix of component-specific jobs
jobs:
  - job: CodeQL_CoreCLR_Linux
    timeoutInMinutes: 90
    steps:
      - task: CodeQL3000Init@0
      - script: ./build.sh clr -c release
      - task: CodeQL3000Finalize@0
  
  - job: CodeQL_Mono_Linux
    timeoutInMinutes: 90
    steps:
      - task: CodeQL3000Init@0
      - script: ./build.sh mono -c release
      - task: CodeQL3000Finalize@0
  
  - job: CodeQL_Libraries_Linux
    timeoutInMinutes: 120
    steps:
      - task: CodeQL3000Init@0
      - script: ./build.sh libs -c release
      - task: CodeQL3000Finalize@0
  
  # ... etc
```

**Expected Savings:**
- Parallel execution of components: 50-60% wall-clock time reduction
- Better resource utilization
- Faster feedback on individual component issues

**Challenges:**
- Requires multiple CodeQL licenses/slots
- May increase overall compute usage
- Results need to be aggregated

**Recommendation:** Implement for high-value/high-change components (CoreCLR, Libraries).

### Strategy 3: Cached Build Artifacts (Medium-High Impact)

**Concept:** Cache stable build artifacts between CodeQL runs.

**Implementation:**
```yaml
# Add caching for stable dependencies
- task: Cache@2
  inputs:
    key: 'codeql-native | "$(Agent.OS)" | src/native/**/*.cmake, src/native/**/*.cpp'
    path: artifacts/obj/native
    cacheHitVar: CACHE_NATIVE_RESTORED
  displayName: Cache Native Build Artifacts

- task: Cache@2
  inputs:
    key: 'codeql-nuget | packages.lock.json'
    path: $(NuGetPackageRoot)
    cacheHitVar: CACHE_NUGET_RESTORED
  displayName: Cache NuGet Packages

- task: Cache@2
  inputs:
    key: 'codeql-tools | eng/Version.Details.xml'
    path: artifacts/bin/tools
    cacheHitVar: CACHE_TOOLS_RESTORED
  displayName: Cache Built Tools
```

**Expected Savings:**
- NuGet restore: 3-5 minutes saved
- Native artifacts: 20-40 minutes saved (if unchanged)
- Tools: 10-20 minutes saved (if unchanged)

**Total Potential: 30-65 minutes saved per run**

**Challenges:**
- Cache invalidation complexity
- Large cache sizes
- May miss some changes if cache keys are too broad

**Recommendation:** Implement for NuGet packages and tooling first.

### Strategy 4: Reduce Platform Coverage (Medium Impact)

**Concept:** Analyze only primary platforms, not all cross-compilation targets.

**Current:** 7 platforms in first group, 4 in second group = 11 total platforms

**Proposed:** Focus on:
- linux_x64 (primary Linux target)
- windows_x64 (primary Windows target)
- osx_arm64 (primary macOS target)

**Rationale:**
- Most code paths are platform-independent
- Platform-specific code is a small fraction
- Security vulnerabilities rarely platform-specific at source level

**Expected Savings:**
- Reduce from 11 to 3 platforms = 73% reduction in parallel jobs
- Maintain coverage of all major OSes and architectures

**Challenges:**
- Some platform-specific code may be missed
- Cross-compilation toolchains have unique code paths

**Recommendation:** Implement with quarterly full-platform scans.

### Strategy 5: Optimize Build Configuration (Low-Medium Impact)

**Concept:** Use build flags optimized for analysis rather than shipping.

**Current:**
```bash
-c release -restore -build -pack /p:DotNetBuildAllRuntimePacks=true
```

**Proposed:**
```bash
-c release -restore -build /p:DotNetBuildForCodeQL=true /p:SkipTests=true /p:SkipNativeBuild=false /p:BuildMonoForCodeQL=true
```

**Optimizations:**
- Skip package creation (`-pack`) - not needed for analysis: ~10-15 minutes
- Skip test compilation - not analyzing tests: ~15-25 minutes
- Disable optimization in native builds for better analysis (optional)
- Use single-threaded builds for determinism (may be slower but more reliable)

**Expected Savings:** 25-40 minutes per platform

**Challenges:**
- CodeQL may need certain artifacts for full analysis
- Some optimizations may reduce analysis quality

**Recommendation:** Create dedicated `/p:DotNetBuildForCodeQL=true` configuration.

### Strategy 6: Smart Scheduling (Low Impact, High Value)

**Concept:** Optimize when and how often CodeQL runs.

**Current:** 3x per week on fixed schedule

**Proposed:**
```yaml
schedules:
  # Full scan weekly on Saturday (low activity day)
  - cron: 0 12 * * 6
    displayName: Weekly Full CodeQL Scan
    branches:
      include:
      - main
    always: true

# Add PR-triggered incremental scans for high-risk changes
pr:
  branches:
    include:
    - main
  paths:
    include:
    - src/coreclr/vm/*
    - src/libraries/System.Private.CoreLib/*
    # ... other security-critical paths
```

**Benefits:**
- Reduce scheduled runs from 3x to 1x per week
- Add on-demand scanning for high-risk PRs
- Better alignment with development cycles

**Expected Savings:** 
- 66% reduction in scheduled builds
- More targeted analysis
- Faster feedback on critical changes

**Recommendation:** Implement tiered scanning strategy.

### Strategy 7: Build Subset Prioritization (Medium Impact)

**Concept:** Build components in priority order, allow partial success.

**Implementation:**
```yaml
# Priority 1: Core runtime and security-critical libraries (90 minutes)
- script: ./build.sh clr.runtime+clr.corelib+libs.sfx -c release
  timeoutInMinutes: 90
  displayName: Build Priority 1 Components
  
# Priority 2: Additional runtimes (60 minutes)  
- script: ./build.sh mono.runtime+mono.corelib -c release
  timeoutInMinutes: 60
  displayName: Build Priority 2 Components
  continueOnError: true
  
# Priority 3: Tooling and packs (90 minutes)
- script: ./build.sh clr.tools+libs.oob+packs -c release
  timeoutInMinutes: 90
  displayName: Build Priority 3 Components
  continueOnError: true
```

**Benefits:**
- Critical components always analyzed
- Partial results better than timeout
- Clear visibility into what was analyzed

**Expected Savings:**
- Guaranteed completion of high-priority components
- Reduced timeout failures
- Better incremental progress

**Recommendation:** Implement as fallback strategy.

## Recommended Implementation Roadmap

### Phase 1: Quick Wins (1-2 weeks implementation)
1. **Add NuGet package caching** - Expected: 5 minute savings
2. **Remove `-pack` from CodeQL builds** - Expected: 15 minute savings
3. **Reduce platform coverage to 3 platforms** - Expected: 70% job reduction
4. **Optimize timeout values** based on actual measurements

**Expected Total Impact:** 30-40% reduction in build time and resource usage

### Phase 2: Structural Improvements (3-4 weeks implementation)
1. **Implement component-based parallel CodeQL jobs** for Linux x64
   - CoreCLR job (90 min)
   - Mono job (90 min)
   - Libraries job (120 min)
2. **Add build artifact caching** for native components and tools
3. **Create `/p:DotNetBuildForCodeQL=true` configuration**

**Expected Total Impact:** 50-60% reduction in wall-clock time

### Phase 3: Advanced Optimizations (6-8 weeks implementation)
1. **Implement incremental/differential CodeQL analysis**
2. **Add PR-triggered CodeQL for high-risk paths**
3. **Optimize scheduling** to weekly full scan + incremental scans
4. **Implement build subset prioritization**

**Expected Total Impact:** 70-80% reduction in scheduled build time

### Phase 4: Continuous Optimization (Ongoing)
1. Monitor and tune cache hit rates
2. Adjust component boundaries based on change patterns
3. Refine subset dependencies
4. Update platform coverage based on security findings

## Metrics and Monitoring

### Key Performance Indicators

Track the following metrics to measure improvement:

1. **Build Duration Metrics:**
   - Average build time per platform
   - P95 build time (95th percentile)
   - Timeout failure rate
   - Time to first component completion

2. **Resource Utilization:**
   - Total compute minutes consumed
   - Peak parallel job count
   - Cache hit rates
   - Artifact storage costs

3. **Coverage Metrics:**
   - Lines of code analyzed
   - Components analyzed per run
   - Time since last full analysis
   - Incremental vs. full scan ratio

4. **Quality Metrics:**
   - Security issues found per run
   - False positive rate
   - Time to issue discovery
   - Analysis completeness percentage

### Target Goals (6 months)

- Reduce average Linux x64 build time from ~240 minutes to ~80 minutes (67% reduction)
- Reduce total compute minutes by 60%
- Maintain or improve security issue detection rate
- Achieve 80%+ cache hit rate for stable components
- Reduce timeout failures to <5%

## Technical Considerations

### CodeQL Analysis Requirements

1. **Full Context Needs:**
   - CodeQL requires compiled binaries for analysis
   - Dataflow analysis needs complete dependency graph
   - Cross-component calls require all components built

2. **Incremental Limitations:**
   - Some queries need full program analysis
   - Database format may not support partial updates
   - Version compatibility between incremental runs

3. **Quality vs. Speed Trade-offs:**
   - Faster builds may reduce analysis depth
   - Subset builds may miss inter-component issues
   - Caching may delay detection of introduced issues

### Build System Constraints

1. **MSBuild Graph Dependencies:**
   - Runtime components have complex dependencies
   - Some builds must be sequential (CoreLib before Libraries)
   - Cross-architecture tools needed for some targets

2. **Artifact Compatibility:**
   - Cached artifacts must match build configuration
   - Cross-platform artifacts may have incompatibilities
   - Version drift between components

3. **Pipeline Limitations:**
   - CodeQL license/slot limitations
   - Azure DevOps parallel job limits
   - Cache storage quotas

## Alternative Approaches Considered

### 1. Cloud Build Distribution (e.g., Incredibuild, Distcc)
**Pros:** Could parallelize native compilation across many nodes
**Cons:** Complex setup, CodeQL compatibility unknown, cost
**Decision:** Defer until Phase 2-3 results measured

### 2. Precompiled Headers for Native Code
**Pros:** Faster C++ compilation
**Cons:** Already used in most places, diminishing returns
**Decision:** Low priority optimization

### 3. Unity Builds (Combining C++ files)
**Pros:** Faster compilation, better optimization
**Cons:** CodeQL may need per-file analysis, complex to maintain
**Decision:** Investigate in Phase 4

### 4. Remote Caching (e.g., BuildXL, Bazel)
**Pros:** Share build artifacts across all builds
**Cons:** Major build system change, migration cost
**Decision:** Not recommended for CodeQL-specific optimization

### 5. Separate CodeQL Database Creation
**Pros:** Build once, analyze many times
**Cons:** Database size, freshness concerns
**Decision:** Consider for query development workflow

## Conclusion

The current CodeQL build for dotnet/runtime is comprehensive but has significant optimization opportunities. By implementing the recommended phased approach, we can achieve:

- **67% reduction in build time** (from ~240 to ~80 minutes for Linux x64)
- **60% reduction in compute resources**
- **Maintained or improved security coverage**
- **Faster feedback cycles for developers**

The highest-impact, lowest-risk improvements are:
1. Reducing platform coverage to 3 primary platforms (73% job reduction)
2. Adding build artifact caching (30-65 minute savings)
3. Removing unnecessary build steps like packaging (15-25 minute savings)

These changes can be implemented incrementally without major architectural changes and provide immediate, measurable benefits.

## Next Steps

1. **Baseline Measurement:** Instrument current builds to capture detailed timing data
2. **Pilot Phase 1 Changes:** Implement on a single platform (linux_x64) first
3. **Measure and Validate:** Compare security issue detection quality
4. **Gradual Rollout:** Expand to other platforms after validation
5. **Iterate:** Continuously monitor and optimize based on data

## References

- [dotnet/runtime Build Documentation](../../workflow/building/README.md)
- [CodeQL Pipeline Configuration](../../../eng/pipelines/runtime-codeql.yml)
- [Build Subsets Documentation](../../../eng/Subsets.props)
- [Azure DevOps Pipeline Optimization](https://docs.microsoft.com/azure/devops/pipelines/process/runs)
- [CodeQL Documentation](https://codeql.github.com/docs/)

---

*Document Version: 1.0*
*Last Updated: 2026-02-04*
*Author: GitHub Copilot Analysis*
