# MonoVM Runtime Components

## Summary

MonoVM runtime components are units of optional functionality that may be provided by the runtime to some workloads.

The idea is to provide some components as optional units that can be loaded dynamically (on workloads that support
dynamic loading) or that are statically linked into the final application (on workloads that support only static
linking).

To that end this document describes a methodology for defining and implementing components and for calling component
functionality from the runtime.

## Goals and scenarios

Breaking up the runtime into components allows us to pursue two goals:
1. Provide workloads with the ability to ship a runtime that
contains only the required capabilities and excludes native code for
unsupported operations.
2. Reduce the number of different build configurations and reduce the build
complexity by allowing composition to happen closer to application execution
time, instead of requiring custom builds of the runtime.

For example, each of the following experiences requires different runtime
capabilities:

- Developer inner loop on on a mobile or WebAssembly workload: The runtime
  should include support for the interpreter, hot reload, and the diagnostic
  server.
- Release build iPhone app for the app store: The runtime should not include the
  interpreter, hot reload, or the diagnostic server.
- Release build iPhone app with `System.Reflection.Emit` support: The runtime
  should include the interpreter, but not hot reload or the diagnostic server.
- Line of business Android app company-wide internal beta: The runtime should
  not include interpreter support or hot reload, but should include the
  diagnostic server.

## Building Components

For each workload we choose one of two strategies for building the runtime and
the components: we either build the components as shared libraries that are
loaded by the runtime at execution time; or we build the components as static
libraries that are statically linked together with the runtime (static
library), and the application host.  We assume that workloads that would
utilize static linking already do native linking of the application host and
the runtime as part of the app build.

The choice of which strategy to pursue depends on platform capabilities.  For
example there is no dynamic linking on WebAssembly at this time, so static
linking is the only option.  On iOS dynamic linking is supported for debug
scenarios, but is disallowed in apps that want to be published to the Apple App
Store. Thus for ios release builds we must use static linking.

We can summarize the different options:

| Scenario(s)                                             | Dynamic loading allowed | Component build strategy                                                                              | Disabled components                                                                     |
| ------------------------------------------------------- | ----------------------- | ----------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------- |
| Console, Android,<br>ios simulator,<br>ios device debug | yes                     | component stubs in runtime; component in shared libraries next to runtime; dlopen to load.            | Just leave out the component shared library from the app bundle.                        |
| webassembly,<br>ios device release                      | no                      | component stubs in runtime; components in static libraries; embedding API calls to register non-stubs | Don’t link the component static libs.<br>Leave out the embedding API registration call. |



## High level overview

Each component is defined in `src/mono/mono/components`.

The runtime is compiled for different configurations with either static or dynamic linking of components.

When the components are dynamically linked, each component produces a shared library `mono-component-*component-name*`.
When the components are statically linked, each component produces a static library.

The choice of dynamic or static linking is a global compile-time configuration parameter of the runtime: either all
components are dynamic or they're all static. In either case, each component may be either present or stubbed out at execution time.

With dynamic linking, all the stubs are built into the runtime, and the runtime probes for the dynamic library of each
component to see if it is present, or else it calls the stub component.

With static linking, all the present components are statically linked with the runtime into the final app.

Each component exposes a table of functions to the runtime.  The stubs also implement the same table.  In the places
where it makes sense, the runtime may call to get the function table and call the methods.

When a component implementation needs to call the runtime, it may call only functions that are marked
`MONO_COMPONENT_API` (or `MONO_API` - as long as it is not `MONO_RT_EXTERNAL_ONLY` - same as what is allowed for normal
runtime internal functions).

Components, their vtables, etc are not versioned.  The runtime and the components together represent a single
indivisible unit.  Mixing components from different versions of the runtime is not supported.

## Detailed design - C code organization

** TODO ** Copy from draft document

## Detailed design - Packaging and runtime packs

** TODO ** Write this up
