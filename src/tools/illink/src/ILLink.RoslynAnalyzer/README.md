# ILLink.RoslynAnalyzer

## Using a local build

To use a local build of the analyzer in another project, modify the `Analyzer` ItemGroup as follows:

```xml
<Target Name="UseLocalILLinkAnalyzer" BeforeTargets="CoreCompile">
  <ItemGroup>
    <Analyzer Remove="@(Analyzer)" Condition="'%(Filename)' == 'ILLink.RoslynAnalyzer'" />
    <Analyzer Include="path/to/runtime/artifacts/bin/ILLink.RoslynAnalyzer/Debug/netstandard2.0/ILLink.RoslynAnalyzer.dll" />
  </ItemGroup>
</Target>
```

## Analysis state lifetime

Compiler-derived caches belong to `AnalyzerCompilationState`, created for an enabled compilation
by the compilation-start callback. CFG entries are weakly keyed so that the compilation does not
keep unused graphs alive. Cached type classification and CFG metadata are immutable; mutable
flow states, exception snapshots, and reporting patterns remain local to each analysis.
Annotation helpers called without compilation state perform uncached classification.