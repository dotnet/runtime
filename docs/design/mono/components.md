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

### Base component contract

Each component may use the following types and preprocessor definitions:

- (from `mono/component/component.h`) `MonoComponent` a struct that is the "base vtable" of all components.  It provides a single member `cleanup` that each component must implement.
   - The component cleanup function should be prepared to be called multiple
     times.  Second and subsequent calls should be ignored.
- (from `mono/utils/mono-compiler.h`) `MONO_COMPONENT_API` when a component
  needs to call a runtime function that is not part of the public Mono API, it
  can only call a `MONO_COMPONENT_API` function.  Care should be taken to use
  the most general version of a group of functions so that we can keep the
  total number of exposed functions to a minimum.
- (from `mono/utils/mono-compiler.h`) `MONO_COMPONENT_EXPORT_ENTRYPOINT` each
  component must expose a function named `mono_component_<component_name>_init`
  that is tagged with this macro.  When the component is compiled dynamically,
  the build will ensure that the entrypoint is exported and visible.
- (set by cmake) `COMPILING_COMPONENT_DYNAMIC` defined if the component is
  being compiled into a shared library.  Generally components don't need to
  explicitly act on this define.
- (set by cmake) `STATIC_COMPONENTS` defined if all components are being
  compiled statically.  If this is set, the component stub should export an
  entrypoint with the name `mono_component_<component_name>_init` rather than
  `mono_component_<component_name>_stub_init`.

### To implement a component

To implement `feature_X` as a component.  Carry out the following steps:

* Add a new entry to the `components` list in `src/mono/mono/component/CMakeLists.txt`:
  ```
  list(APPEND components
	feature_X
  )
  ```
* Add a new list `feature_X-sources_base` to `src/mono/mono/component/CMakeLists.txt` that lists the source files of the component:
  ```
  set(feature_X-sources_base feature_X.h feature_X.c)
  ```
* Add a new list `feature_X-stub-sources_base` to `src/mono/mono/component/CMakeLists.txt` that lists the source files for the component stub:
  ```
  set(feature_X-stub-sources_base feature_X-stub.c)
  ```
* Declare a struct `_MonoComponentFeatureX` in `src/mono/mono/component/feature_X.h`
  ```
  typedef struct _MonoComponentFeatureX {
    MonoComponent component; /* First member _must_ be MonoComponent */
	void (*hello)(void); /* Additional function pointers for each method for feature_X */
  } MonoComponentFeatureX;
  ```
* Declare an entrypoint `mono_component_feature_X_init` in `src/mono/mono/component/feature_X.h`
  that takes no arguments and returns the component vtable:
  ```
  #ifdef STATIC_COMPONENTS
  MONO_COMPONENT_EXPORT_ENTRYPOINT
  MonoComponentFeatureX *
  mono_component_feature_X_init (void);
  #endif
  ```
* Implement the component in `src/mono/mono/component/feature_X.c` (and other sources, if necessary).
  Re-declare and then dcefine the component entrypoint and populate a function table:
    ```
	#ifndef STATIC_COMPONENTS
    MONO_COMPONENT_EXPORT_ENTRYPOINT
    MonoComponentFeatureX *
    mono_component_feature_X_init (void);
    #endif

	/* declare static functions that implement the feature_X vtable */
    static void feature_X_cleanup (MonoComponent *self);
	static void feature_X_hello (void);

    static MonoComponentFeatureX fn_table = {
	  { feature_X_cleanup },
	  feature_X_hello,
    };
	
	MonoComponentFeatureX *
	mono_component_feature_X_init (void) { return &fn_table; }
	
	void feature_X_cleanup (MonoComponent *self)
	{
	  static int cleaned = 0;
	  if (cleaned)
	    return;
	  /* do cleanup */
	  cleaned = 1;
	}

    void feature_X_hello (void)
	{
	   /* implement the feature_X hello functionality */
	}
    ```
* Implement a component stub in `src/mono/mono/component/feature_X-stub.c`.  This looks exactly like the component, except most function will be no-ops or `g_assert_not_reached`.
   One tricky point is that the entrypoint is exported as `mono_component_feature_X_stub_init` and *also* as `mono_component_feature_X_init` if the component is being compiled statically.
    ```
    #ifdef STATIC_COMPONENTS
    MONO_COMPONENT_EXPORT_ENTRYPOINT
    MonoComponentFeatureX *
    mono_component_feature_X_init (void)
    {
        return mono_component_feature_X_stub_init ();
    }
    #endif
	#ifndef STATIC_COMPONENTS
    MONO_COMPONENT_EXPORT_ENTRYPOINT
    MonoComponentFeatureX *
    mono_component_feature_X_stub_init (void);
    #endif

	/* declare static functions that implement the feature_X vtable */
    static void feature_X_cleanup (MonoComponent *self);
	static void feature_X_hello (void);

    static MonoComponentFeatureX fn_table = {
	  { feature_X_cleanup },
	  feature_X_hello,
    };
	
	MonoComponentFeatureX *
	mono_component_feature_X_init (void) { return &fn_table; }
	
	void feature_X_cleanup (MonoComponent *self)
	{
	  static int cleaned = 0;
	  if (cleaned)
	    return;
	  /* do cleanup */
	  cleaned = 1;
	}

    void feature_X_hello (void)
	{
	   /* implement the feature_X hello functionality */
	}
    ```
* Add a getter for the component to `mono/metadata/components.h`, and also add a declaration for the component stub initialization function here
  ```c
    MonoComponentFeatureX*
	mono_component_feature_X (void);
	
    ...
    MonoComponentFeatureX*
	mono_component_feature_X_stub_init (void);
  ```

* Add an entry to the `components` list to load the component to `mono/metadata/components.c`, and also implement the getter for the component:
  ```c
    static MonoComponentFeatureX *feature_X = NULL;
	
    MonoComponentEntry components[] = {
	   ...
	   {"feature_X", "feature_X", COMPONENT_INIT_FUNC (feature_X), (MonoComponent**)&feature_X, NULL },
    }


    ...
	MonoComponentFeatureX*
	mono_component_feature_X (void)
	{
	  return feature_X;
    }
  ```

* In the runtime, call the component functions through the getter.
   ```c
     mono_component_feature_X()->hello()
   ```
* To call runtime functions from the component, use either `MONO_API` functions
  from the runtime, or `MONO_COMPONENT_API` functions.  It is permissible to
  mark additional functions with `MONO_COMPONENT_API`, provided they have a
  `mono_`- or `m_`-prefixed name.

## Detailed design - Packaging and runtime packs

The components are building blocks to put together a functional runtime.  The
runtime pack includes the base runtime and the components and additional
properties and targets that enable the workload to construct a runtime for
various scenarios.

In each runtime pack we include:

- The compiled compnents for the apropriate host architectures in a well-known subdirectory
- An MSBuild props file that defines an item group that list each component name and has metadata that indicates:
   - the path to the component in the runtime pack
   - the path to the stub component in the runtime pack (if components are static)
- An MSBuild targets file that defines targets to copy a specified set of components to the app publish folder (if components are dynamic); or to link the runtime together with stubs and a set of enabled components (if components are static)

** TODO ** Write this up in more detail
