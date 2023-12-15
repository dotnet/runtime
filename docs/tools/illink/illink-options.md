# Available Command Line Options

## `illink` tool

The `illink` is IL linker version shipping with .NET Core or .NET 5 platforms. It's bundled with
the .NET SDK and most of the options are accessible using msbuild properties but any option
can also be passed using `_ExtraTrimmerArgs` property.

### Trimming from the root assembly

The command:

`illink -a Program.dll`

will use the assembly `Program.dll` as root ILLink input. That means that the ILLink will
start with the main entry point method of `Program.dll` (typically the `Main` method) and
process all its dependencies to determine what is necessary for this assembly to run.

It's possible to use multiple input files and ILLink will use them all as multiple sources.
When a library is used instead of executable ILLink will root and mark all members instead of
assembly entry point. This rooting behaviour can be customized by passing additional option
which can use one of following values.

- `all` - Keep all members in root assembly. This is equivalent to using `copy` link action
for the assembly.
- `default` - Use entry point for applications and all members for libraries
- `entrypoint` - Use assembly entry point as the only root in the assembly. This option is useful
for multi entry-point libraries bundles.
- `library` - All visible members and metadata are retained. This useful mode for trimming a library before publishing.
- `visible` - Keep all members and types visible outside of the assembly. All internals members
are also rooted when assembly contains InternalsVisibleToAttribute.

You can retain all public members of `Program.dll` application even if they are not
referenced by any dependency by calling ILLink like

`illink -a Program.dll visible`

### Trimming from an [XML descriptor](data-formats.md#descriptor-format)

The command:

`illink -x desc.xml`

will use the XML descriptor as a source. That means that ILLink will
use this file to decide what to link in a set of assemblies. The format of the
descriptors is described in [data-formats document](data-formats.md).

### Actions on the assemblies

You can specify what the ILLink should do exactly per assembly.

ILLink can do the following things on all or individual assemblies

- `skip` - skip them, and do nothing with them
- `copy` - copy them to the output directory
- `copyused` - copy used assemblies to the output directory
- `link` - trim them to reduce their size
- `delete`- remove them from the output
- `save` - save them in memory without trimming

You can specify an action per assembly using `--action` option like this:

`illink --action link Foo`

or

`illink --action skip System.Windows.Forms`

Or you can specify what to do for the trimmed assemblies.

A trimmable assembly is any assembly that includes the attribute `System.Reflection.AssemblyMetadata("IsTrimmable", "True")`.

You can specify what action to do on the trimmed assemblies with the option:

`--trim-mode skip|copy|copyused|link`

You can specify what action to do on assemblies without such an attribute with the option:

`--action copy|link`

### The output directory

By default, ILLink will create an `output` directory in the current
directory where it will store the processed files, to avoid overwritting input
assemblies. You can change the output directory with the option:

`-out PATH`

If you specify the output directory `.`, please ensure that you won't write over
important assemblies of yours.

### Specifying assembly lookup paths

By default, ILLink will first look for assemblies in the directories `.`
and `bin`. You can specify additional locations where assemblies will be searched
for by using `-d PATH` option.

Example:

`illink -d ../../libs -a Program.dll`

### Excluding framework features

One of the ways to reduce core assembly sizes is by removing framework capabilities. This
is usually something that the developer decides about as it alters the program behaviour
therefore the decision whether the size saving is worth it is left to the developer.

Each feature can be controlled independently using `--feature NAME value` option.

The list of available feature names is framework-dependent and can vary between different
framework versions.

The list of controllable features for .NET Core is available at https://docs.microsoft.com/en-us/dotnet/core/run-time-config/.

### Using custom substitutions

An option called `--substitutions FILE` allows external customization of any
method or field for assemblies which are linked. The syntax used is fully described
in [data-formats document](data-formats.md#substitution-format). Using substitutions
with `ipconstprop` optimization (enabled by default) can help reduce output
size as any dependencies under conditional logic which will be evaluated as
unreachable will be removed.

### Adding custom ILLink steps

You can write [custom steps](custom-steps.md) for ILLink and instruct
ILLink to add them into its existing pipeline. To tell ILLink where this assembly is
located, you have to append its full path after a comma which separates the custom
 step's name from the custom assembly's path

`--custom-step [custom step],[custom assembly]`

You can now ask ILLink to add the custom step at the end of the pipeline:

`illink --custom-step Foo.FooStep,D:\Bar\Foo.dll`

Or you can ask ILLink to add it after a specific step:

`illink --custom-step +MarkStep:Foo.FooStep,D:\Bar\Foo.dll -a Program.dll`

Or before a specific step:

`illink --custom-step -MarkStep:Foo.FooStep,D:\Bar\Foo.dll -a Program.dll`

### Passing data to custom steps

For advanced custom steps which need interaction with external values there is a
`--custom-data KEY=VALUE` option. Each key can have a simple value assigned which means
if you need to store multiple values for the same key, you should use custom separators for the
values and pass them as one key-value pair.

### Supplementary [custom attributes](data-formats.md#custom-attributes-annotations-format)

Much of the trimmer behaviour is controlled by the custom attributes but they are not always
present in the input assemblies. The attributes can be applied to any existing metadata using
`--link-attributes FILE` option.

Alternatively, the trimmer recognizes the embedded XML resource 'ILLink.LinkAttributes.xml' as a
special resource to alter the custom attributes applied.

### Ignoring embedded XML control files

The trimmer recognizes embedded XML resources based on name as special ones which can
alter the trimming behaviour. The behaviour can be suppressed if necessary by using
control options listed below.

| File Format | Resource Name  |  Control Option  |
|---|---|---|
| Descriptor  | ILLink.Descriptors.xml  |   --ignore-descriptors |
| Substition  | ILLink.Substitutions.xml  |  --ignore-substitutions |
| LinkAttributes  | ILLink.LinkAttributes.xml    |  --ignore-link-attributes |

### Treat warnings as errors

The `--warnasserror` (or `--warnaserror+`) option will make the trimmer report any warning
messages as error messages instead. By default, the trimmer behaves as if the `--warnaserror-`
option was used, which causes the trimmer to report warnings as usual.

Optionally, you may specify a list of warnings that you'd like to be treated as errors. These
warnings have to be prepended with `IL` and must be separated by either a comma or semicolon.

### Turning off warnings

The `--nowarn` option prevents the trimmer from displaying one or more trimmer warnings by
specifying its warning codes. All warning codes must be prepended with `IL` and multiple
warnings should be separated with a comma or semicolon.

### Control warning versions

The `--warn VERSION` option prevents the trimmer from displaying warnings newer than the specified
version. Valid versions are in the range 0-9999, where 9999 will display all current and future
warnings.

### Emit single warnings per assembly

The `--singlewarn` (or `--singlewarn+`) option will show at most one trim analysis warning per
assembly which represents all of the warnings produced by code in the assembly. The default is to show all trim analysis warnings.

You may also pass `--singlewarn Assembly` (or `--singlewarn- Assembly`) to control this behavior for a particular assembly.

### Generating warning suppressions

For each of the linked assemblies that triggered any warnings during trimming, the
`--generate-warning-suppressions [cs | xml]` option will generate a file containing a list
with the necessary attributes to suppress these. The generated files can either be C# source
files or XML files in a [format](data-formats.md#custom-attributes-annotations-format) that is supported by the trimmer,
the emitted format depends upon the argument that is passed to this option (`cs` or `xml`.)
The attributes contained in these files are assembly-level attributes of type `UnconditionalSuppressMessage`
specifying the required `Scope` and `Target` properties for each of the warnings seen. The
generated files are saved in the output directory and named `<AssemblyName>.WarningSuppressions.<extension>`.

### Detailed dependencies tracing

For tracking why trimmer kept specific metadata you can use `--dump-dependencies` option
which by default writes detailed information into a compressed file called `linker-dependencies.xml.gz`
inside output directory. The default output filename can be changed with `--dependencies-file`
option.

The format of the data is XML and it's intentionally human-readable but due
to a large amount of data, it's recommended to use tools which can analyze the data.

## monolinker specific options

### The i18n Assemblies

Mono has a few assemblies which contains everything region specific:

    I18N.CJK.dll
    I18N.MidEast.dll
    I18N.Other.dll
    I18N.Rare.dll
    I18N.West.dll

By default, they will all be copied to the output directory. But you can
specify which one you want using the command:

`illink -l choice`

Where choice can either be: none, all, cjk, mideast, other, rare or west. You can
combine the values with a comma.

Example:

`illink -a assembly -l mideast,cjk`

### Trimming from an API info file

The command:

`illink -i assembly.info`

will use a file produced by `mono-api-info` as a source. The trimmer will use
this file to link only what is necessary to match the public API defined in
the info file.
