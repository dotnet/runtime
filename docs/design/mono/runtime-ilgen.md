# IL generation at runtime

## Introduction

The mono runtime makes extensive use of generating IL methods at runtime. These
methods are called 'wrappers' in the runtime code, because some of them 'wrap' other
methods, like a managed-to-native wrapper would wrap the native function being called.
Wrappers have the `MonoMethod.wrapper_type` field set to the type of the wrapper.

## Source code structure

- `wrapper-types.h`: Enumeration of wrapper types
- `marshal*`: Functions for generating wrappers
- `method-builder*`: Low level functions for creating new IL methods/code at runtime

## WrapperInfo

Every wrapper has an associated `WrapperInfo` structure which describes the wrapper.
This can be retrieved using the `mono_marshal_get_wrapper_info ()` function.
Some wrappers have subtypes, these are stored in `WrapperInfo.subtype`.

## Caching wrappers

Wrappers should be unique, i.e. there should be only one instance of every wrapper. This is
achieved by caching wrappers in wrapper type specific hash tables, which are stored in
`MonoMemoryManager.wrapper_caches`.

## Generics and wrappers

Wrappers for generic instances should be created by doing:
instance method -> generic method definition -> generic wrapper -> inflated wrapper

## AOT support

In full-aot mode, the AOT compiler will collect and emit the wrappers needed by the
application at runtime. This involves serializing/deserializing the `WrapperInfo` structure.

## Wrapper types

### Managed-to-native

These wrappers are used to make calls to native code. They are responsible for marshalling
arguments and result values, setting up EH structures etc.

### Native-to-managed

These wrappers are used to call managed methods from native code. When a delegate is passed to
native code, the native code receives a native-to-managed wrapper.

### Delegate-invoke

Used to handle more complicated cases of delegate invocation that the fastpaths in the JIT can't handle.

### Synchronized

Used to wrap synchronized methods. The wrapper does the locking.

### Runtime-invoke

Used to implement `mono_runtime_invoke ()`.

### Dynamic-method

These are not really wrappers, but methods created by user code using the `DynamicMethod` class.

Note that these have no associated `WrapperInfo` structure.

### Alloc

SGEN allocator methods.

### Write-barrier

SGEN write barrier methods.

### Castclass

Used to implement complex casts.

### Stelemref

Used to implement stelem.ref.

### Unbox

Used to unbox the receiver before calling a method.

### Managed-to-managed/other

The rest of the wrappers, distinguished by their subtype.

#### String-ctor

Used to implement string ctors, the first argument is ignored, and a new string is allocated.

#### Element-addr

Used to implement ldelema in multi-dimensional arrays.

#### Generic-array-helper

Used to implement the implicit interfaces on arrays like IList<T> etc. Delegate to helper methods on the Array class.

#### Structure-to-ptr

Used to implement Marshal.StructureToPtr.

#### Ptr-to-structure

Used to implement Marshal.PtrToStructure.
