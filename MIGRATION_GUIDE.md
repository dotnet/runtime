# Test Exclusion Migration Guide

This guide explains how to migrate test exclusions from `src/tests/issues.targets` to inline attributes and properties in test files.

## Overview

We are migrating test exclusions from the centralized `issues.targets` file to attributes on individual test methods and properties in project files. This makes the exclusions more visible and maintainable.

## Migration Script

The `migrate_exclusions.py` script automates this migration process.

### Prerequisites

- Python 3.6+
- Access to the dotnet/runtime repository

### Usage

**Dry run (shows what would be changed):**
```bash
python3 migrate_exclusions.py
```

**Apply changes:**
```bash
python3 migrate_exclusions.py --apply
```

### What the Script Does

For each `ExcludeList` item in `issues.targets`, the script:

1. **Finds the project files**: Converts the output path to source path and locates `.csproj`, `.ilproj`, or `.fsproj` files
2. **Determines the replacement**: Maps the condition to the appropriate attribute or property based on the mapping table
3. **Applies changes**:
   - For C# files: Adds `[ActiveIssue(...)]` attributes to test methods
   - For IL files: Adds custom attributes (manual review recommended)
   - For projects: Adds properties like `<CrossGenTest>false</CrossGenTest>`

### Examples

**Before (issues.targets):**
```xml
<ItemGroup Condition="'$(XunitTestBinBase)' != ''">
    <ExcludeList Include="$(XunitTestBinBase)/baseservices/finalization/CriticalFinalizer/*">
        <Issue>https://github.com/dotnet/runtime/issues/76041</Issue>
    </ExcludeList>
</ItemGroup>
```

**After (CriticalFinalizer.cs):**
```csharp
[ActiveIssue("https://github.com/dotnet/runtime/issues/76041", TestPlatforms.Any)]
[Fact]
public static int TestEntryPoint()
{
    // ...
}
```

## Manual Steps After Migration

1. **Build the tests**: Ensure the migrated tests still compile
2. **Run the tests**: Verify they still skip correctly on the intended platforms
3. **Remove from issues.targets**: Delete the migrated `ExcludeList` items
4. **Review IL files**: IL file migrations need manual review and refinement

## Condition Mapping Reference

The script uses a comprehensive mapping table to convert MSBuild conditions to test attributes. Key mappings include:

| Condition | C# Attribute Example |
|-----------|---------------------|
| All platforms | `[ActiveIssue("...", TestPlatforms.Any)]` |
| CoreCLR only | `[ActiveIssue("...", TestRuntimes.CoreCLR)]` |
| Unix platforms | `[ActiveIssue("...", TestPlatforms.AnyUnix)]` |
| Specific arch | `[ActiveIssue("...", typeof(PlatformDetection), nameof(PlatformDetection.IsArm64Process))]` |
| NativeAOT | `[ActiveIssue("...", typeof(Utilities), nameof(Utilities.IsNativeAot))]` |
| Mono | `[ActiveIssue("...", TestRuntimes.Mono)]` |

See the full mapping table in `migrate_exclusions.py`.

## Known Limitations

- **CMake-based tests**: Tests without `.csproj`/`.ilproj`/`.fsproj` files cannot be migrated automatically
- **IL files**: IL attribute syntax requires manual verification
- **Complex conditions**: Some very complex conditions may not have exact mappings

## Rollback

If you need to rollback changes:
```bash
git checkout -- src/tests/
```

Then restore the `ExcludeList` items to `issues.targets`.
