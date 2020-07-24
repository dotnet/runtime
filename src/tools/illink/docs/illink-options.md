# Available Command Line Options

## `illink` tool

The `illink` is IL linker version shipping with .NET Core or .NET 5 platforms. It's bundled with
the .NET SDK and most of the options are accessible using msbuild properties but any option
can also be passed using `_ExtraTrimmerArgs` property.

### Linking from the main assembly

The command:

`illink -a Program.exe`

will use the assembly `Program.exe` as a source. That means that the linker will
start with the main entry point method of `Program.exe` (typically the `Main` method) and process all its dependencies to determine what is necessary for this assembly to run.

### Linking from an [XML descriptor](data-formats.md#descriptor-format)

The command:

`illink -x desc.xml`

will use the XML descriptor as a source. That means that the linker will
use this file to decide what to link in a set of assemblies. The format of the
descriptors is described in [data-formats document](../data-formats.md).

### Actions on the assemblies

You can specify what the linker should do exactly per assembly.

The linker can do the following things on all or individual assemblies

- `skip` - skip them, and do nothing with them
- `copy` - copy them to the output directory
- `copyused` - copy used assemblies to the output directory
- `link` - link them to reduce their size
- `delete`- remove them from the output
- `save` - save them in memory without linking

You can specify an action per assembly using `-p` option like this:

`illink -p link Foo`

or

`illink -p skip System.Windows.Forms`

Or you can specify what to do for the core assemblies.

Core assemblies are the assemblies that belong to the base class library,
like `System.Private.CoreLib.dll`, `System.dll` or `System.Windows.Forms.dll`.

You can specify what action to do on the core assemblies with the option:

`-c skip|copy|link`

### The output directory

By default, the linker will create an `output` directory in the current
directory where it will store the processed files, to avoid overwritting input
assemblies. You can change the output directory with the option:

`-out PATH`

If you specify the ouput directory `.`, please ensure that you won't write over
important assemblies of yours.

### Specifying assembly lookup paths

By default, the linker will first look for assemblies in the directories `.`
and `bin`. You can specify additional locations where assemblies will be searched
for by using `-d PATH` option.

Example:

`illink -d ../../libs -a program.exe`

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

### Adding custom linker steps

You can write [custom steps](/doc/custom-steps.md) for the linker and instruct
the linker to add them into its existing pipeline. To tell the linker where this assembly is
located, you have to append its full path after a comma which separates the custom
 step's name from the custom assembly's path

`--custom-step [custom step],[custom assembly]`

You can now ask the linker to add the custom step at the end of the pipeline:

`illink --custom-step Foo.FooStep,D:\Bar\Foo.dll`

Or you can ask the linker to add it after a specific step:

`illink --custom-step +MarkStep:Foo.FooStep,D:\Bar\Foo.dll -a program.exe`

Or before a specific step:

`illink --custom-step -MarkStep:Foo.FooStep,D:\Bar\Foo.dll -a program.exe`

### Passing data to custom steps

For advanced custom steps which need interaction with external values there is a
`--custom-data KEY=VALUE` option. Each key can have a simple value assigned which means
if you need to store multiple values for the same key, you should use custom separators for the
values and pass them as one key-value pair.

### Supplementary [custom attributes](data-formats.md#custom-attributes-annotations-format)

Much of the linker behaviour is controlled by the custom attributes but they are not always
present in the input assemblies. The attributes can be applied to any existing metadata using
`--link-attributes FILE` option.

Alternatively, the linker recognizes the embedded XML resource 'ILLink.LinkAttributes.xml' as a
special resource to alter the custom attributes applied.

### Ignoring embedded XML control files

The linker recognizes embedded XML resources based on name as special ones which can
alter the linking behaviour. The behaviour can be suppressed if necessary by using
control options listed below.

| File Format | Resource Name  |  Control Option  |
|---|---|---|
| Descriptor  | ILLink.Descriptors.xml  |   --ignore-descriptors |
| Substition  | ILLink.Substitutions.xml  |  --ignore-substitutions |
| LinkAttributes  | ILLink.LinkAttributes.xml    |  --ignore-link-attributes |

### Treat warnings as errors

The `--warnasserror` (or `--warnaserror+`) option will make the linker report any warning
messages as error messages instead. By default, the linker behaves as if the `--warnaserror-`
option was used, which causes the linker to report warnings as usual.

Optionally, you may specify a list of warnings that you'd like to be treated as errors. These
warnings have to be prepended with `IL` and must be separated by either a comma or semicolon.

### Turning off warnings

The `--nowarn` option prevents the linker from displaying one or more linker warnings by
specifying its warning codes. All warning codes must be prepended with `IL` and multiple
warnings should be separated with a comma or semicolon.

### Control warning versions

The `--warn VERSION` option prevents the linker from displaying warnings newer than the specified
version. Valid versions are in the range 0-9999, where 9999 will display all current and future
warnings.

### Generating warning suppressions

For each of the linked assemblies that triggered any warnings during the linking, the
`--generate-warning-suppressions` option will generate a file containing a list with the
necessary attributes to suppres these. The attributes contained in this files are
assembly-level attributes of type `UnconditionalSuppressMessage` specifying the required
`Scope` and `Target` properties for each of the warnings seen. The generated files are
saved in the ouput directory and named `<AssemblyName>.WarningSuppressions.cs`.

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

### Linking from an API info file

The command:

`illink -i assembly.info`

will use a file produced by `mono-api-info` as a source. The linker will use
this file to link only what is necessary to match the public API defined in
the info file.
