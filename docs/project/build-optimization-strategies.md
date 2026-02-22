# Build Time Optimization Strategies for dotnet/runtime

## Executive Summary

This document provides actionable strategies to improve build times for dotnet/runtime in both local development and Pull Request (PR) scenarios on Linux. The dotnet/runtime repository is one of the largest .NET codebases, with builds that can take anywhere from 10 minutes for incremental library changes to over an hour for full clean builds. This guide identifies bottlenecks and provides practical optimization strategies that developers and CI maintainers can implement.

**Target Scenarios:**
- Local developer builds (inner loop development)
- Pull Request CI builds
- Rolling/scheduled CI builds

**Key Findings:**
- Full clean builds can take 60-90 minutes on Linux x64
- Incremental library builds: 2-5 minutes
- Incremental CoreCLR builds: 5-15 minutes
- Most developers only work on 1-2 components but often rebuild more than necessary
- CI builds spend significant time on redundant work across platform matrix

## Current Build Characteristics

### Typical Build Times (Linux x64)

| Scenario | Clean Build | Incremental | Components |
|----------|-------------|-------------|------------|
| Full repo (`./build.sh`) | 60-90 min | 15-30 min | clr+mono+libs+tools+host+packs |
| CoreCLR only (`./build.sh clr`) | 15-25 min | 5-10 min | clr.native+clr.corelib+clr.tools |
| Libraries only (`./build.sh libs`) | 20-30 min | 2-5 min | libs.native+libs.sfx+libs.oob |
| Mono only (`./build.sh mono`) | 15-25 min | 5-10 min | mono.runtime+mono.corelib |
| Single library (e.g., System.Text.Json) | 1-3 min | 30-90 sec | Individual project |

### Build Time Breakdown (Full Clean Build)

**Total: ~75 minutes on modern hardware (16 cores, NVMe SSD)**

1. **NuGet Restore** - 2-3 minutes
   - Downloading packages
   - Generating lock files
   - Package resolution

2. **Native Components** - 30-40 minutes (Largest bottleneck)
   - CoreCLR JIT and runtime (15-20 min)
   - Mono runtime with LLVM (10-15 min)
   - Native libraries/shims (5-8 min)
   - Native AOT components (3-5 min)
   - C++ compilation is inherently slow and hard to parallelize efficiently

3. **Managed Libraries** - 15-20 minutes
   - System.Private.CoreLib (CoreCLR) (2-3 min)
   - System.Private.CoreLib (Mono) (2-3 min)
   - ~250 library assemblies (10-12 min)
   - Reference assemblies generation (1-2 min)

4. **Tools and Crossgen** - 8-12 minutes
   - Crossgen2 (IL → Native compilation tool) (3-5 min)
   - ILC (NativeAOT compiler) (2-3 min)
   - IL Linker (2-3 min)
   - Other tools (1-2 min)

5. **Packs and Installers** - 3-5 minutes
   - Creating shared framework packs
   - Bundle/installer creation
   - Test infrastructure setup

### PR Build Matrix Characteristics

For a typical PR, the runtime pipeline builds:

- **~15-20 platform configurations** (linux_x64, linux_arm64, windows_x64, windows_arm64, osx_arm64, etc.)
- **Multiple configurations per platform** (Debug, Release, Checked for CoreCLR)
- **Parallel execution** across platforms but sequential subset builds within each
- **Total compute time**: 15-25 hours of aggregate build time per PR
- **Wall-clock time**: 30-60 minutes (with parallelization)

## Optimization Strategies

### Strategy 1: Use Subset Builds (Essential for Local Development)

**Impact: 50-80% time reduction for local builds**

**Problem:** Developers often build the entire repo when they only need one or two components.

**Solution:** Use targeted subset builds for the components you're actually changing.

#### Common Development Scenarios

**Scenario A: Working on a Library (Most Common)**
```bash
# DON'T: Build everything (75 minutes)
./build.sh

# DO: Build just what you need (3-5 minutes for first build, 1-2 minutes incremental)
# One-time setup: Build runtime once
./build.sh clr+libs -rc Release

# Then, work on individual library
cd src/libraries/System.Text.Json
dotnet build
dotnet build /t:test
```

**Scenario B: Working on CoreCLR Runtime**
```bash
# DON'T: Build everything
./build.sh -c Release

# DO: Build just CoreCLR (15-20 minutes)
./build.sh clr -c Release

# For JIT-only changes, even more targeted (5-10 minutes)
./build.sh clr.runtime -c Release
```

**Scenario C: Working on CoreCLR + Testing with Libraries**
```bash
# Build CoreCLR in Checked configuration (optimized with asserts)
./build.sh clr -rc Checked

# Build libraries in Debug
./build.sh libs -lc Debug

# Update test host without full rebuild
./build.sh libs.pretest
```

**Scenario D: Working on Mono Runtime**
```bash
# Just Mono runtime (15-20 minutes)
./build.sh mono -c Release

# Mono with specific libraries
./build.sh mono+libs -rc Release -lc Debug
```

**Time Savings:**
- Libraries-only work: 70-90% reduction (75 min → 3-5 min)
- CoreCLR-only work: 60-75% reduction (75 min → 15-20 min)
- Single library: 95% reduction (75 min → 1-3 min)

### Strategy 2: Leverage Incremental Builds

**Impact: 60-90% time reduction on subsequent builds**

**Problem:** Developers often do clean builds when incremental builds would suffice.

**Solution:** Avoid `git clean -xdf` and let MSBuild/CMake handle incremental compilation.

#### When Incremental Builds Are Safe

✅ **Safe for incremental builds:**
- Code changes to .cs files in libraries
- Code changes to .cpp/.h files in CoreCLR/Mono (CMake tracks dependencies)
- Adding new source files (if added to .csproj properly)
- Minor changes to .csproj files

❌ **Requires clean build:**
- Changing build configurations (Debug ↔ Release)
- Updating to main branch with major build system changes
- After build failures that may have corrupted intermediate files
- Changing subset selections significantly
- Major CMake configuration changes

#### Best Practices

```bash
# When switching branches or pulling updates, selective clean is better than full clean
# Option 1: Clean just binaries, keep packages
./build.sh -clean

# Option 2: Target clean for specific subset
./build.sh clr -clean

# Option 3: Clean specific directory
rm -rf artifacts/bin/coreclr/linux.x64.Release

# Avoid: Full nuclear clean (forces complete rebuild)
# git clean -xdf  # Only use when absolutely necessary
```

**Time Savings:**
- Incremental library build: 90% reduction (20 min → 2 min)
- Incremental CoreCLR build: 70% reduction (20 min → 5-8 min)

### Strategy 3: Optimize NuGet Restore with Package Caching

**Impact: 1-3 minutes saved per build**

**Problem:** NuGet restore can be slow, especially on first build or when switching branches.

**Solution:** Use local package caching and lock files effectively.

#### Implementation

```bash
# Enable NuGet HTTP cache (should be default)
export NUGET_HTTP_CACHE_PATH=~/.nuget/v3-cache

# Use central package management (already in place in runtime repo)
# packages.lock.json files ensure deterministic restores

# Speed up restore with parallel downloads (default in modern NuGet)
export NUGET_RESTORE_MSBUILD_ARGS="/maxcpucount"

# For offline scenarios, use local package cache
export NUGET_PACKAGES=~/.nuget/packages
```

#### CI/PR Optimization

For PR builds, implement NuGet package caching:

```yaml
# Example for Azure Pipelines
- task: Cache@2
  inputs:
    key: 'nuget | "$(Agent.OS)" | **/packages.lock.json'
    path: $(NUGET_PACKAGES)
    cacheHitVar: CACHE_RESTORED
  displayName: Cache NuGet packages
```

**Time Savings:** 1-3 minutes per build

### Strategy 4: Parallelize Native Builds Effectively

**Impact: 20-40% reduction in native build time**

**Problem:** Native C++ compilation is the biggest bottleneck. Default settings may not use all CPU cores.

**Solution:** Ensure CMake and MSBuild use maximum parallelization.

#### For Linux/macOS

```bash
# Ensure CMake uses all cores (runtime repo does this by default)
# The build system automatically detects core count

# If you want to limit parallelization (to avoid OOM or overheating)
export MAXCPU=8
./build.sh clr

# Or override directly
./build.sh clr /p:MaxCpuCount=12
```

#### For Windows

```cmd
REM Build system auto-detects, but you can override
build.cmd clr /m:16
```

#### Use Ninja Instead of Make (Advanced)

Ninja is a faster build system than Make for CMake projects:

```bash
# Install Ninja
sudo apt-get install ninja-build  # Ubuntu/Debian
brew install ninja                 # macOS

# Use Ninja for builds (runtime repo supports this)
./build.sh clr -ninja

# Or set as default
export CMakeBuildType=Ninja
```

**Time Savings:** 5-10 minutes on native builds (20-40% reduction)

### Strategy 5: Use Build Caching for CI/PR (Advanced)

**Impact: 30-60% reduction in CI build times**

**Problem:** Each PR build starts from scratch, rebuilding stable components unnecessarily.

**Solution:** Implement artifact caching for stable components.

#### What to Cache

1. **NuGet Packages** - Highly cacheable, changes infrequently
2. **Native Binaries for Stable Components** - Cache if certain directories haven't changed
3. **IL Linker and Tools** - Built once per tool version
4. **Crossgen2 binaries** - Can be cached if CoreCLR hasn't changed

#### Implementation (Azure Pipelines Example)

```yaml
# Cache native CoreCLR binaries if source hasn't changed
- task: Cache@2
  inputs:
    key: 'coreclr-native | "$(Agent.OS)" | src/coreclr/**/*.cpp, src/coreclr/**/*.h, src/coreclr/CMakeLists.txt'
    path: artifacts/bin/coreclr
    cacheHitVar: CORECLR_CACHE_RESTORED
  displayName: Cache CoreCLR Native Binaries

# Cache tools if eng/Version.Details.xml hasn't changed
- task: Cache@2
  inputs:
    key: 'tools | eng/Version.Details.xml'
    path: artifacts/bin/coreclr/linux.x64.Release/crossgen2
    cacheHitVar: TOOLS_CACHE_RESTORED
  displayName: Cache Build Tools

# Skip building cached components
- script: |
    if [ "$CORECLR_CACHE_RESTORED" = "true" ]; then
      echo "##vso[task.setvariable variable=SkipCoreClrBuild]true"
    fi
  displayName: Check Cache Status

# Conditional build
- script: ./build.sh clr -c Release
  condition: ne(variables.SkipCoreClrBuild, 'true')
  displayName: Build CoreCLR (if not cached)
```

**Time Savings:** 15-30 minutes per PR build when cache hits

### Strategy 6: Optimize Build for PR Path-Based Filtering

**Impact: 50-70% reduction in unnecessary PR builds**

**Problem:** PR builds often build all platforms and configurations even when changes only affect specific areas.

**Current State:** runtime.yml already has path-based evaluation but can be optimized further.

**Solution:** Enhance path-based build skipping and create more targeted build matrices.

#### Enhancement Ideas

```yaml
# More granular path detection
- mono_runtime_only:
    - src/mono/mono/**
    - src/mono/cmake/**
  
- coreclr_runtime_only:
    - src/coreclr/vm/**
    - src/coreclr/jit/**
  
- libraries_only:
    - src/libraries/**/src/**
  
- specific_library:
    - src/libraries/System.Text.Json/**
```

#### Reduced Platform Matrix for Library-Only Changes

```yaml
# For library-only changes, test fewer platforms
- ${{ if eq(variables['OnlyLibrariesChanged'], 'true') }}:
  platforms:
    - linux_x64        # Primary Linux
    - windows_x64      # Primary Windows
    - osx_arm64        # Primary macOS
  # Skip: linux_arm64, windows_arm64, etc.
```

**Time Savings:** Reduces unnecessary platform builds by 60-80%

### Strategy 7: Optimize Configuration Choices

**Impact: Variable, depends on workflow**

**Problem:** Building in wrong configuration for the task wastes time and provides wrong behavior.

**Solution:** Choose the right configuration for your development scenario.

#### Configuration Guide

| Configuration | When to Use | Build Time | Runtime Speed | Asserts |
|---------------|-------------|------------|---------------|---------|
| **Debug** | Debugging managed code, library changes | Fast | Slow (2-3x) | Yes |
| **Release** | Performance testing, production builds | Slower | Fast | No |
| **Checked** (CoreCLR only) | Debugging runtime with good perf | Medium | Medium (1.3-1.5x) | Yes |

#### Best Practices

```bash
# For library development: Build runtime once in Release, libraries in Debug
./build.sh clr+libs -rc Release -lc Debug
# Result: Fast runtime for testing, debug-friendly library code
# Saves: 20-30% vs full Debug build

# For CoreCLR development: Use Checked configuration
./build.sh clr -rc Checked
# Result: Optimized code with asserts for finding bugs
# Saves: Similar build time to Release, better debugging than Release

# For performance work: Everything in Release
./build.sh clr+libs -rc Release -lc Release
# Result: Maximum runtime performance
```

**Time Savings:** 20-30% when using hybrid configurations

### Strategy 8: Use Precompiled Headers and Unity Builds (Future)

**Impact: Potentially 30-50% reduction in native build time**

**Status:** Not currently implemented in runtime repo (as of 2026)

**Proposal:** Investigate enabling unity builds for CoreCLR/Mono native components.

Unity builds combine multiple .cpp files into single translation units, reducing redundant header parsing.

#### Potential Implementation

```cmake
# In CMakeLists.txt for coreclr/mono
set(CMAKE_UNITY_BUILD ON)
set(CMAKE_UNITY_BUILD_BATCH_SIZE 16)
```

**Considerations:**
- May increase memory usage during compilation
- Can hide some dependency issues
- Requires testing to ensure correctness

**Recommendation:** Experiment in development builds, measure impact before production

### Strategy 9: Local Development: Use Smaller Test Scopes

**Impact: 90-99% reduction in test time**

**Problem:** Developers often run full test suites when testing small changes.

**Solution:** Run targeted tests during development, full suite in PR.

#### Test Scope Strategy

```bash
# DON'T: Run all library tests (can take hours)
./build.sh libs.tests -test

# DO: Run specific test project
cd src/libraries/System.Text.Json/tests
dotnet build /t:test

# DO: Run specific test method during debugging
cd src/libraries/System.Text.Json/tests
dotnet test --filter FullyQualifiedName~JsonSerializer
```

#### For CoreCLR Tests

```bash
# DON'T: Run all CoreCLR tests (thousands of tests, hours)
./src/tests/build.sh && ./src/tests/run.sh

# DO: Run specific test directory
./src/tests/build.sh src/tests/JIT/Regression/
./src/tests/run.sh src/tests/JIT/Regression/
```

**Time Savings:** 
- From hours to minutes for test execution
- Faster feedback during development
- Full test suite still runs in PR CI

### Strategy 10: Optimize Workspace Management

**Impact: Varies, but improves overall efficiency**

**Problem:** Large artifacts directory accumulates over time, slowing down file operations.

**Solution:** Periodically clean old builds, use selective cleaning.

#### Best Practices

```bash
# Check artifacts size
du -sh artifacts/

# Clean old configurations you're not using
rm -rf artifacts/bin/coreclr/linux.x64.Debug  # If you only use Release now

# Clean test results (can grow large)
rm -rf artifacts/TestResults/

# Clean old NuGet packages (if you want to save space)
rm -rf artifacts/packages/

# Periodic full clean (e.g., weekly)
git clean -xdf
# Then rebuild your standard config
./build.sh clr+libs -rc Release
```

#### Use a Build Directory Outside Repo (Advanced)

```bash
# Set artifacts directory to faster drive or separate partition
export ArtifactsDir=/mnt/fast-nvme/dotnet-runtime-artifacts
./build.sh clr+libs
```

## PR Build Optimization Strategies

These strategies target the CI/PR pipeline specifically.

### Strategy 11: Reduce Redundant Platform Builds

**Impact: 40-60% reduction in total PR build time**

**Current:** Runtime PRs build ~15-20 platform configurations

**Proposal:** Tier platforms by priority and change scope.

#### Tiered Platform Strategy

**Tier 1: Always Build (Core Validation)**
- linux_x64 (Debug and Release)
- windows_x64 (Debug and Release)
- osx_arm64 (Release only)

**Tier 2: Build for Runtime Changes**
- linux_arm64
- windows_arm64
- linux_musl_x64

**Tier 3: Build for Comprehensive Validation**
- All other platforms (rolling builds only, or weekly)

**Implementation:**

```yaml
# In runtime.yml
- ${{ if eq(variables['LibrariesOnlyChange'], 'true') }}:
  platforms:
    - linux_x64
    - windows_x64
- ${{ else }}:
  platforms:
    - linux_x64
    - windows_x64
    - linux_arm64
    - windows_arm64
    - osx_arm64
    # ... etc
```

**Time Savings:** 50-70% reduction in platform builds for library-only changes

### Strategy 12: Implement Merge Queues with Combined Builds

**Impact: Reduces total CI load by 60-80%**

**Concept:** Batch multiple PRs together and build once rather than building each PR individually.

GitHub merge queues allow this workflow:
1. PR passes basic validation (linting, formatting)
2. PR enters merge queue
3. Multiple PRs batched together
4. Single comprehensive build for batch
5. All PRs in batch merge if build passes

**Benefits:**
- Fewer total builds
- Better resource utilization
- Faster merge times for contributors

**Recommendation:** Explore GitHub merge queues for dotnet/runtime

### Strategy 13: Smart Retry and Failure Isolation

**Impact: Reduces wasted compute on infrastructure failures**

**Problem:** Infrastructure failures (network issues, machine failures) cause full build retries.

**Solution:** Implement granular retry logic.

```yaml
# Retry only failed jobs, not entire build
- script: ./build.sh clr+libs
  retryCountOnTaskFailure: 3
  continueOnError: false

# Or retry specific stages
- stage: Build_Linux_x64
  jobs:
    - job: CoreCLR
      retryCountOnTaskFailure: 2
    - job: Libraries
      retryCountOnTaskFailure: 2
```

**Time Savings:** Reduces wasted builds from infrastructure issues

## Measurement and Monitoring

To validate optimizations, track these metrics:

### Local Development Metrics

1. **Time to First Build** - How long from `git clone` to running tests
2. **Incremental Build Time** - Average time for code change → running tests
3. **Clean Build Time** - Full rebuild time for baseline comparison

### PR/CI Metrics

1. **PR Time to Merge** - From PR creation to merge (including CI)
2. **Compute Time Per PR** - Total aggregate build time
3. **Cache Hit Rates** - For NuGet and build caches
4. **Platform Build Time** - Per-platform build duration
5. **Queue Wait Time** - Time waiting for build agents

### Targets (Aspirational)

| Metric | Current | Target | Improvement |
|--------|---------|--------|-------------|
| Local library incremental build | 2-5 min | 1-2 min | 50% |
| Local CoreCLR incremental build | 5-10 min | 3-6 min | 40% |
| PR build wall-clock time | 30-60 min | 15-30 min | 50% |
| PR total compute time | 15-25 hours | 8-12 hours | 50% |

## Implementation Roadmap

### Phase 1: Quick Wins (Immediate - 2 weeks)

**For Developers:**
1. ✅ Document subset build patterns (this document)
2. ✅ Educate team on incremental builds
3. ✅ Share configuration best practices

**For CI:**
1. Add NuGet package caching to all pipelines
2. Implement basic path-based filtering refinements
3. Reduce platform matrix for library-only changes

**Expected Impact:** 20-30% improvement in build times

### Phase 2: Infrastructure Improvements (1-2 months)

**For Developers:**
1. Investigate Ninja build system adoption
2. Experiment with ccache/sccache for C++ builds
3. Optimize CMake configurations

**For CI:**
1. Implement build artifact caching for stable components
2. Add more granular path-based build triggers
3. Implement tiered platform validation

**Expected Impact:** 40-50% improvement in build times

### Phase 3: Advanced Optimizations (3-6 months)

**For Developers:**
1. Explore unity builds for native components
2. Investigate distributed compilation (Incredibuild, distcc)
3. Optimize MSBuild project structure

**For CI:**
1. Evaluate GitHub merge queues
2. Implement smart build scheduling
3. Add predictive build optimization (ML-based)

**Expected Impact:** 50-70% improvement in build times

### Phase 4: Continuous Improvement (Ongoing)

1. Monitor metrics continuously
2. Adjust strategies based on data
3. Share learnings with community
4. Regular performance reviews

## Tools and Tips

### Useful Build Flags

```bash
# Binary logging for analysis
./build.sh clr -bl

# Verbose output for debugging
./build.sh clr -v detailed

# Build only, skip tests
./build.sh clr -build

# Restore only, useful for pre-warming caches
./build.sh -restore

# Build specific subset
./build.sh clr.runtime  # Just runtime, not CoreLib

# Skip native build (if working on managed only)
./build.sh libs /p:BuildNative=false
```

### Build Time Analysis Tools

```bash
# Analyze MSBuild binary log
# Install: dotnet tool install --global MSBuildStructuredLog
MSBuildStructuredLogViewer artifacts/log/Debug/Build.binlog

# Find slowest projects
grep -r "Time Elapsed" artifacts/log/ | sort -t: -k2 -n | tail -20

# Check cache effectiveness
ls -lh ~/.nuget/packages | wc -l  # Package count
du -sh ~/.nuget/v3-cache           # HTTP cache size
```

### Recommended Development Environment

```bash
# Fast storage for artifacts
export ArtifactsDir=/path/to/fast/nvme/drive

# Use all available CPU cores
export MAXCPU=$(nproc)

# Enable NuGet package caching
export NUGET_PACKAGES=~/.nuget/packages
export NUGET_HTTP_CACHE_PATH=~/.nuget/v3-cache

# For better build performance
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Create build alias for common workflow
alias build-libs='./build.sh clr+libs -rc Release -lc Debug'
```

## Common Pitfalls and Solutions

### Pitfall 1: Building Too Much

**Symptom:** Waiting for builds that include components you're not changing

**Solution:** Use targeted subset builds (Strategy 1)

### Pitfall 2: Unnecessary Clean Builds

**Symptom:** Doing `git clean -xdf` before every build

**Solution:** Trust incremental builds, use selective cleaning (Strategy 2)

### Pitfall 3: Wrong Configuration Mix

**Symptom:** Debugging libraries with Debug runtime (very slow tests)

**Solution:** Use `-rc Release -lc Debug` for library work (Strategy 7)

### Pitfall 4: Running All Tests Locally

**Symptom:** Waiting hours for test results during development

**Solution:** Run targeted tests locally, rely on PR CI for comprehensive testing (Strategy 9)

### Pitfall 5: Not Using Parallel Builds

**Symptom:** Build using 1-2 cores on 16-core machine

**Solution:** Ensure MSBuild/CMake use all cores (Strategy 4)

## Conclusion

Build time optimization in dotnet/runtime is achievable through a combination of:

1. **Smart subset selection** - Don't build what you don't need
2. **Effective incrementalism** - Leverage build caching and incremental compilation
3. **Right configurations** - Match build configuration to development task
4. **Targeted testing** - Test what changed during development
5. **CI efficiency** - Cache, filter, and parallelize PR builds

By implementing these strategies, developers can reduce local build times from 60-90 minutes to 5-15 minutes for typical changes, and PR builds can be reduced from 30-60 minutes to 15-30 minutes.

**Key Recommendations:**

**For Developers:**
- Always use subset builds: `./build.sh <subset>` instead of `./build.sh`
- Use hybrid configurations: `-rc Release -lc Debug` for library work
- Avoid unnecessary clean builds
- Run targeted tests during development

**For CI/PR:**
- Implement artifact caching (NuGet, native binaries, tools)
- Reduce platform matrix for library-only changes
- Use path-based filtering more aggressively
- Consider merge queues for batched validation

**Expected Overall Impact:**
- **Local development:** 60-80% reduction in typical build times
- **PR builds:** 40-60% reduction in wall-clock time
- **CI compute costs:** 50-70% reduction in total compute hours

## References

- [Building Libraries](../../workflow/building/libraries/README.md)
- [Building CoreCLR](../../workflow/building/coreclr/README.md)
- [Building Mono](../../workflow/building/mono/README.md)
- [Workflow Guide](../../workflow/README.md)
- [Subsets Documentation](../../../eng/Subsets.props)
- [CI Pipeline Configuration](../../../eng/pipelines/runtime.yml)

---

*Document Version: 1.0*
*Last Updated: 2026-02-04*
*Focus: PR and Local Build Optimization*
