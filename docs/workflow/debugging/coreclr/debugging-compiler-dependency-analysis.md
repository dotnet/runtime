# Debugging Compiler Dependency Analysis

* [Dependency Graph Viewer](#dependency-graph-viewer)
  * [Graphs View](#graphs-view)
  * [Dependency Graph View](#dependency-graph-view)
  * [Single Node Exploration](#single-node-exploration)
* [Why DGML](#why-dgml)
* [Dependency Graph EventSource](#dependency-graph-eventsource)

The general technique is to identify what node is missing from the graph, or is erroneously present in the graph, and change the dependency analysis logic to adjust the graph. This document describes the various ways of debugging to identify what's happening.

Analysis techniques for the dependency graph:

* Use the _DependencyGraphViewer_ tool (if running on Windows). This tool is located in `src/coreclr/tools/aot/DependencyGraphViewer`
  * This is the only convenient way to examine the graph while also simultaneously debugging the compiler.
  * While this is currently Windows only due to use of _WinForms_, it would be fairly straightforward to make a command line based tool.
  * Dependency graph does not support multiple simultaneous logging facilities, so make sure that you do not set _IlcGenerateDgmlFile_ or invoke _ILCompiler_ with the _DGML_ generation turned on.
* Pass command line switches to the compiler to generate a _dependency graph DGML_ file. This will produce the same data that is viewable in the viewer tool, but in a textual XML format.
  * Future efforts may make the XML file loadable by the viewer tool.
* Instrument the compiler dependency analysis. (This may be necessary in cases where the viewer is unable to provide sufficient information about why the graph is structured as it is.)

## Dependency Graph Viewer

This application allows viewing the dependency graph produced by the AOT compilation.

<!-- TODO: Add an example. -->
Usage instructions:

1. Launch the process as an administrator.
2. Run the compiler. The compiler can be run to completion, or stopped.
3. Explore through the graph.

### Graphs View

* Choose one of the graphs that appears in the Dependency Graphs view to explore. As compilers execute, new graphs will automatically appear here.
* The set of graphs loaded into the process is limited by available memory space. To clear the used memory, close all windows of the application.

### Dependency Graph View

* In the Dependency Graph View, enter a regular expression in the text box, and then press `Filter`. This will display a list of the nodes in the graph which have names that match the regular expression.
* Commonly, if there is an object file symbol associated with the node, it should be used as part of the regular expression. See the various implementations of `GetName` in the compiler for naming behavior.
* Additionally, the event source marking mode assigns an _id_ to each node, and that is found as the mark object on the node. So, if a specific _id_ is known, just type that in, and it will appear in the window. This is for use when using this tool in parallel with debugging the compiler.

### Single Node Exploration

* Once the interesting node(s) have been identified in the dependency graph window, select one of them, and then press `Explore`.
* In the _Node Explorer_ window, the Dependent nodes (the ones which depend on the current node) are the ones displayed above, and the Dependee nodes (the ones that this node depends on) are displayed below. Each node in the list is paired with a textual reason as to why that edge in the graph exists.
* Select a node to explore further and press the corresponding button to make it happen.

## Why DGML

This tool can be used to visualize paths from a node of interest to the roots. To use it, pass command line option to the compiler to generate the DGML file (`--dgmllog name_of_output_file`) and then use this tool to find the path to the root. If you're looking at an optimized NativeAOT compilation, `--scandgmllog` might be preferable since it will have more details.

The input to the tool is the DGML file and name of a node of interest. The output is the list of reasons why that node was included.

This tool located in folder `src/coreclr/tools/aot/WhyDgml`

See <https://github.com/dotnet/corert/pull/7962> for example of usage and output.

## Dependency Graph EventSource

ILCompiler publishes its dependency graph through the `Microsoft-ILCompiler-DependencyGraph` EventSource. You can capture those events with `dotnet-trace`. The resulting `*.nettrace` file can be processed with the TraceEvent library. This can be useful for investigating ILCompiler issues on Linux, but note that it requires additional tooling to process the trace into a DGML file that can be used with the dependency graph viewer.

1. Install or update the tool: `dotnet tool install --global dotnet-trace`.
2. Start the collection, listening on a custom diagnostic port:
   ```bash
   dotnet-trace collect \
    --diagnostic-port /tmp/ilc-depgraph.socket \
    --providers Microsoft-ILCompiler-DependencyGraph:0x1:4
   ```

   The `0x1:4` keyword/level pair enables the dependency graph events at Informational verbosity.
3. Ensure the `IlcGenerateDgmlFile` MSBuild property is **not** enabled (e.g., do not pass `/p:IlcGenerateDgmlFile=true` to the build); DGML generation suppresses these EventSource events.
4. In another terminal, point ILCompiler at the same diagnostics port and run your build:
   ```bash
   export DOTNET_DiagnosticPorts=/tmp/ilc-depgraph.socket
   ```

   Then invoke the usual command (for example `ilc @path/to/ilc.rsp` or `dotnet publish`).
5. When ILCompiler finishes, `dotnet-trace` finishes writing the trace file.

Events emitted by the `Microsoft-ILCompiler-DependencyGraph` EventSource provider follow this schema:

### Event Types

#### Graph Event
Declares a new graph instance.

| Field | Type | Description |
|-------|------|-------------|
| `id` | int | Unique graph identifier (allows multiple graphs in one trace) |
| `name` | string | Human-readable graph name |

Graph identifiers are stable within a trace: typically graph `1` corresponds to the dependency scanner graph and graph `2` corresponds to the code generation graph.

#### Node Event
Defines a node in the graph.

| Field | Type | Description |
|-------|------|-------------|
| `id` | int | Identifier of the graph this node belongs to |
| `index` | int | Unique node identifier within the graph |
| `name` | string | Display label for the node (if empty, shows "Node {index}") |

#### Edge Event
Creates a directed edge from a dependent node to its dependency.

| Field | Type | Description |
|-------|------|-------------|
| `id` | int | Identifier of the graph this edge belongs to |
| `dependentIndex` | int | Source node (the one that depends on the target) |
| `dependencyIndex` | int | Target node (the dependency) |
| `reason` | string | Edge label describing why the dependency exists |

#### ConditionalEdge Event
Creates a conditional dependency where two nodes must both be present for the dependency to exist.

| Field | Type | Description |
|-------|------|-------------|
| `id` | int | Identifier of the graph this conditional edge belongs to |
| `dependentIndex1` | int | First dependent node |
| `dependentIndex2` | int | Second dependent node |
| `dependencyIndex` | int | The dependency node |
| `reason` | string | Edge label describing why the dependency exists |

In the dependency graph viewer, this corresponds to a synthetic "AND" node representing `(dependent1, dependent2)` with three edges:
- `dependent1 → combined` (labeled "Primary")
- `dependent2 → combined` (labeled "Secondary")
- `combined → dependency` (with the original reason)
