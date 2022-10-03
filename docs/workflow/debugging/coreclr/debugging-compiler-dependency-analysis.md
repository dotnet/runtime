Debugging Compiler Dependency Analysis
============================

The general technique is to identify what node is missing from the graph or is erroneously present in the graph, and change the dependency analysis logic to adjust the graph. This document describes the various ways of debugging to identify what's happening.

Analysis techniques for the dependency graph.
1. Use the DependencyGraphViewer tool (if running on Windows). This tool is located in `src/coreclr/tools/aot/DependencyGraphViewer`
  - This is the only convenient way to examine the graph while also simultaneously debugging the compiler.
  - While this is currently Windows only due to use of WinForms, it would be fairly straightforward to make a command line based tool.
  - Dependency graph does not support multiple simultaneous logging facilities, so make sure that you do not set IlcGenerateDgmlFile or invoke ILCompiler with the DGML generation turned on.
2. Pass command line switches to the compiler to generate a dependency graph dgml file. This will produce the same data as is viewable in the viewer tool, but in a textual xml format.
  - Future efforts may make the xml file loadable by the viewer tool.
3. Instrument the compiler dependency analysis. (This may be necessary in cases where the viewer is unable to provide sufficient information about why the graph is structured as it is.)

DependencyGraphViewer
====================================

This application allows viewing the dependency graph produced by the AOT compilation.

Usage instructions:
1. Launch the process as an administrator
2. Run the compiler
- The compiler can be run to completion, or stopped.
3. Explore through the graph

# Graphs View #
- Choose one of the graphs that appears in the Dependency Graphs view to explore. As compilers execute, new graphs will automatically appear here.
- The set of graphs loaded into the process is limited by available memory space. To clear the used memory, close all windows of the application.

# Graph View #
- In the Dependency Graph view, enter a regular expression in the text box, and then press ""Filter"". This will display a list of the nodes in the graph which have names which match the regular expression.
- Commonly, if there is a object file symbol associated with the node it should be used as part of the regular expression. See the various implementations of GetName in the compiler for naming behavior.
- Additionally, the event source marking mode assigns an Id to each node, and that is found as the mark object on the node, so if a specific id is known, just type that in, and it will appear in the window. (This is for use when using this tool in parallel with debugging the compiler.

# Single Node Exploration #
Once the interesting node(s) have been identified in the dependency graph window, select one of them, and then press Explore.
  - In the Node Explorer window, the Dependent nodes (the ones which depend on the current node are the nodes displayed above, and the Dependee nodes (the nodes that this node depends on) are displayed below. Each node in the list is paired with a textual reason as to why that edge in the graph exists.
  - Select a node to explore further and press the corresponding button to make it happen.

WhyDGML
=======
This tool can be used to visualize paths from a node of interest to the roots. To use it, pass command line option to the compiler to generate the DGML file (`--dgmllog name_of_output_file`) and then use this tool to find the path to the root. If you're looking at an optimized NativeAOT compilation, `--scandgmllog` might be preferable since it will have more details.
The input to the tool is the DGML file and name of a node of interest. The output is the list of reasons why that node was included.

This tool located in folder `src/coreclr/tools/aot/WhyDgml`

See https://github.com/dotnet/corert/pull/7962 for example of usage and output.
