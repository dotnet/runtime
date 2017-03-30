# Roadmap for CoreFx

Related: [Overall .NET Core Roadmap](https://github.com/dotnet/core/blob/master/roadmap.md).

---8<---8<---8<---8<---8<---8<---8<---8<---8<---8<---8<---8<---8<---8<---8<---8<

We need a document that outlines our vision for .NET Core:

* Startup space: highly scalable, efficient applications
* When in conflict, favor this over pure productivity
* Efficient data manipulation APIs
    - Low allocation APIs
    - Better blending of native code and managed code (Span, DllExport)

As the scissors indicate, these are notes for Krzysztof to fill in more details here.

Example:

## Provide a better alternative for `System.Diagnostics.Process` 

Our `Process` class was designed for a time where one would drag & drop a process component on a design surface, configure it in a visual designer, and then write a line or two to launch it. Also, it was designed around many concepts that are arguably specific to the Windows platform, such as `ShellExecute`.

It seems we should take another look at this and see whether we can create a better API for dealing with processes.

---8<---8<---8<---8<---8<---8<---8<---8<---8<---8<---8<---8<---8<---8<---8<---8<
