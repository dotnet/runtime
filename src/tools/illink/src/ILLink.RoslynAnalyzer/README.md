# ILLink.RoslynAnalyzer

## Using a local build

To use a local build of the analyzer in another project, modify the `Analyzer` ItemGroup as follows:

```xml
<Target Name="UseLocalILLinkAnalyzer" BeforeTargets="CoreCompile">
  <ItemGroup>
    <Analyzer Remove="@(Analyzer)" Condition="'%(Filename)' == 'ILLink.RoslynAnalyzer'" />
    <Analyzer Include="/path/to/linker/repo/artifacts/bin/ILLink.RoslynAnalyzer/Debug/netstandard2.0/ILLink.RoslynAnalyzer.dll" />
  </ItemGroup>
</Target>
```