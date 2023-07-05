# ILLink Analyzer

The ILLink analyzer is a command line tool to analyze dependencies, which
were recorded during ILLink processing, and led ILLink to mark an item
to keep it in the resulting linked assembly.

It works on an oriented graph of dependencies, which are collected and
dumped during the ILLink run. The vertices of this graph are the items
of interest like assemblies, types, methods, fields, linker steps,
etc. The edges represent the dependencies.

## How to dump dependencies

The ILLink analyzer needs a ILLink dependencies file as an input. It
can be retrieved by enabling dependencies dumping during trimming of a
project.

For console .NET projects you need to publish the application
with trimming enabled and use the `_TrimmerDumpDependencies` property:

```dotnet publish /p:PublishTrimmed=true /p:_TrimmerDumpDependencies=true```

In this case the dependencies file will be in
`obj/<Configuration>/<TargetFramework>/<RID>/linked/linker-dependencies.xml`.

For Xamarin.Android and Xamarin.iOS, that can be done on the command line by setting
`LinkerDumpDependencies` property to `true` and building the
project. (make sure the LinkAssemblies task is called, it might
require cleaning the project sometimes) Usually it is enough to build
the project like this:

```msbuild /p:LinkerDumpDependencies=true /p:Configuration=Release YourAppProject.csproj```

For .NET SDK style projects, you will want to use `_TrimmerDumpDependencies` instead:

```msbuild /p:_TrimmerDumpDependencies=true /p:Configuration=Release YourAppProject.csproj```

After a successful build, there will be a linker-dependencies.xml
file created, containing the information for the analyzer.

## How to use the analyzer

Let say you would like to know, why a type, Android.App.Activity for
example, was marked by ILLink. So run the analyzer like this:

```illinkanalyzer -t Android.App.Activity linker-dependencies.xml```

Output:

```
Loading dependency tree from: linker-dependencies.xml

--- Type dependencies: 'Android.App.Activity' -----------------------

--- TypeDef:Android.App.Activity dependencies -----------------------
Dependency #1
	TypeDef:Android.App.Activity
	| TypeDef:XA.App.MainActivity [2 deps]
	| Assembly:XA.App, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null [3 deps]
	| Other:Mono.Linker.Steps.ResolveFromAssemblyStep
```

The output contains dependencies string(s), starting with the type and continuing with the item of interest, which depends on the type. The dependency could be a result of multiple reasons. For example, the type was referenced from a method, or the type was listed in the ILLink XML descriptor file.

In our example there is only one dependency string called `Dependency
#1`. It shows us that the type `Android.App.Activity` was marked
during processing of type `XA.App.MainActivity` by ILLink. In this
case because the `MainActivity` type is based on the `Activity` type
and thus ILLink marked it and kept it in the linked assembly. We
can also see that there are 2 dependencies for the `MainActivity`
class. Note that in the string (above) we see only 1st dependency of
the 2, the dependency on the assembly `XA.App`. And finally the
assembly vertex depends on the `ResolveFromAssemblyStep` vertex. So we
see that the assembly was processed in the `ResolveFromAssembly`
ILLink step.

Now we might want to see the `MainActivity` dependencies. That could
be done by the following analyzer run:

```illinkanalyzer -r TypeDef:XA.App.MainActivity linker-dependencies.xml```

Output:

```
Loading dependency tree from: linker-dependencies.xml

--- Raw dependencies: 'TypeDef:XA.App.MainActivity' -----------------

--- TypeDef:XA.App.MainActivity dependencies ------------------------
Dependency #1
	TypeDef:XA.App.MainActivity
	| Assembly:XA.App, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null [3 deps]
	| Other:Mono.Linker.Steps.ResolveFromAssemblyStep
Dependency #2
	TypeDef:XA.App.MainActivity
	| TypeDef:XA.App.MainActivity/<>c__DisplayClass1_0 [2 deps]
	| TypeDef:XA.App.MainActivity [2 deps]
	| Assembly:XA.App, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null [3 deps]
	| Other:Mono.Linker.Steps.ResolveFromAssemblyStep
```

### Known issues

Sometimes ILLink processing is not straight forward and the
marking is postponed, like processing of some of the methods. They are
queued to be processed later. In such case the dependencies are
"interrupted" and the dependecy string for the method usually shows
just dependency on the Mark step.

# Command line help

```
Usage:

	illinkanalyzer [Options] <linker-dependency-file.xml>

Options:

  -a, --alldeps              show all dependencies
  -h, --help                 show this message and exit.
  -r, --rawdeps=VALUE        show raw vertex dependencies. Raw vertex VALUE is
                               in the raw format written by ILLink to the
                               dependency XML file. VALUE can be regular
                               expression
      --roots                show root dependencies.
      --stat                 show statistic of loaded dependencies.
      --tree                 reduce the dependency graph to the tree.
      --types                show all types dependencies.
  -t, --typedeps=VALUE       show type dependencies. The VALUE can be regular
                               expression
  -f, --flat                 show all dependencies per vertex and their distance
  -v, --verbose              be more verbose. Enables stat and roots options.
```
