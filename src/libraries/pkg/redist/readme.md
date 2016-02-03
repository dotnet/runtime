These packages are used for UWP 10 RTM - shared library

They contain the same libraries shipped at UWP RTM, repackaged in the 
latest nuget format (netstandard, runtime packages, etc).

We use them as the baseline for package dependencies so that we can have
CoreFx packages use updated packages with netstandard support without 
breaking shared library.

Directory structure:
Folder per package that will be redistributed.

Lib folder contains a deployment project (depproj) to restore and wrap 
the old implementation DLLs.

We reuse the ref depproj's in the src tree to restore and wrap the old 
reference DLLs.

New package projects are created depending on what type of project we 
are trying to redist.

Standalone packages contain a single package,  redisting ref and lib.

Runtime packages with API change in latest contain a reference package 
redisting ref, and runtime packages redisting lib.

Runtime packages with no API change in latest contain runtime packages 
redisting lib, and a validation-only package for reference.  These
validation-only packages do not actually build any output since the live
reference packages are sufficient (no API change).

In any case where we needed to insert a version that would collide with 
the latest package version we bumped up the current package version.

Here's a table of what is contained here:

Package name | UWP RTM version | Live version | What we need to ship
-------------|-----------------|--------------|---------------------
System.Collections | 4.0.10.0 | 4.0.11.0 | runtime packages
System.Collections.Concurrent | 4.0.10.0 | 4.0.11.0 | pre-versioned standalone package
System.Diagnostics.Tracing | 4.0.20.0 | 4.1.0.0 | pre-versioned ref + runtime packages
System.Dynamic.Runtime | 4.0.10.0 | 4.0.11.0 | runtime packages
System.IO | 4.0.10.0 | 4.1.0.0 | pre-versioned ref + runtime packages
System.Linq | 4.0.0.0 | 4.0.1.0 | pre-versioned standalone package
System.Linq.Expressions | 4.0.10.0 | 4.0.11.0 | runtime packages
System.ObjectModel | 4.0.10.0 | 4.0.11.0 | pre-versioned standalone package
System.Reflection.Extensions | 4.0.0.0 | 4.0.1.0 | runtime packages
System.Reflection.Primitives | 4.0.0.0 | 4.0.1.0 | runtime packages
System.Reflection.TypeExtensions | 4.0.0.0 | 4.1.0.0 | pre-versioned ref + runtime packages
System.Resources.ResourceManager | 4.0.0.0 | 4.0.1.0 | runtime packages
System.Runtime.Extensions | 4.0.10.0 | 4.1.0.0 | pre-versioned ref + runtime packages
System.Runtime | 4.0.20.0 | 4.1.0.0 | pre-versioned ref + runtime packages
System.Text.RegularExpressions | 4.0.10.0 | 4.0.11.0 | pre-versioned standalone package