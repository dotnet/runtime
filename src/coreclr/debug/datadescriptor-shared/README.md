# Datadescriptor Implementation Infrastructure

This folder contains infrastructure to create data descriptors as defined in the [data_descriptor.md](../../../../docs/design/datacontracts/data_descriptor.md). Data descriptors enable diagnostic tooling (debuggers, profilers, etc.) to understand the internal layout and structure of .NET runtime objects without requiring intimate knowledge of implementation details.

## Getting Started

### Quick Example

Here's how to create a simple data descriptor for a new runtime component:

**1. Create the required files:**

```
your_component/
├── CMakeLists.txt
├── datadescriptor.h
├── datadescriptor.inc
└── contracts.jsonc
```

**2. Define your data descriptor (`datadescriptor.inc`):**

```cpp
CDAC_BASELINE("empty")
CDAC_TYPES_BEGIN()

CDAC_TYPE_BEGIN(MyRuntimeObject)
CDAC_TYPE_SIZE(sizeof(MyRuntimeObject))
CDAC_TYPE_FIELD(MyRuntimeObject, uint32, Id, offsetof(MyRuntimeObject, m_id))
CDAC_TYPE_FIELD(MyRuntimeObject, pointer, NextObject, offsetof(MyRuntimeObject, m_next))
CDAC_TYPE_END(MyRuntimeObject)

CDAC_TYPES_END()
CDAC_GLOBALS_BEGIN()

CDAC_GLOBAL(g_MyGlobalCounter, uint32, g_myGlobalCounter)

CDAC_GLOBALS_END()
```

**3. Create the header file (`datadescriptor.h`):**

```cpp
#include "my_runtime_object.h" // Your actual runtime structures
```

**4. Add CMake integration (`CMakeLists.txt`):**

```cmake
add_library(my_component_interface INTERFACE)
target_include_directories(my_component_interface INTERFACE
    ${CMAKE_CURRENT_SOURCE_DIR}
    # include dirs here)
generate_data_descriptors(
    LIBRARY_NAME my_component_contract_descriptor
    CONTRACT_FILE "${CMAKE_CURRENT_SOURCE_DIR}/contracts.jsonc"
    CONTRACT_NAME "MyComponentContractDescriptor"
    INTERFACE_TARGET my_component_interface
    DLLEXPORT
)
```

Then the output object library `my_component_contract_descriptor` can be linked into the shipping dll.

## CMake Integration and Build System

### Function Parameters

The `generate_data_descriptors` function defined in `clrdatadescriptors.cmake` takes the following arguments:

* **`LIBRARY_NAME`** (Required) - Sets the name of the target object being created
* **`CONTRACT_FILE`** (Required) - Path to the contract JSON file defining supported contracts
* **`CONTRACT_NAME`** (Required) - Name of the `ContractDescriptor` export symbol
* **`INTERFACE_TARGET`** (Required) - Interface target providing dependencies, include directories, and definitions
* **`DLLEXPORT`** (Optional) - Controls if the `CONTRACT_NAME` will be exported from the DLL

### Two-Phase Build Process

The build system uses a sophisticated two-phase approach:

**Phase 1: Intermediary Library**
- Compiles `datadescriptor.cpp` with your `datadescriptor.h` and `datadescriptor.inc`
- Creates object files that the `cdac-build-tool` can analyze
- Extracts type layout information and generates string pools

**Phase 2: Contract Descriptor Generation**
- Runs `cdac-build-tool` to process the intermediary object files
- Generates the final contract descriptor C source file
- Compiles this into the final library that gets linked into the runtime


## Macro Reference

### Structure Definition Macros

**`CDAC_BASELINE("identifier")`**
- Specifies the baseline data contract version
- Use `"empty"` for new descriptors
- Must appear before any other content

**`CDAC_TYPES_BEGIN()` / `CDAC_TYPES_END()`**
- Delimits the type definitions section
- Must contain all `CDAC_TYPE_*` macros

**`CDAC_TYPE_BEGIN(typeName)`**
- Starts a new type definition
- `typeName` must be globally unique within the descriptor

**`CDAC_TYPE_SIZE(sizeInBytes)`**
- Specifies the type has a determinate size
- Usually `sizeof(YourNativeType)`

**`CDAC_TYPE_INDETERMINATE(typeName)`**
- Specifies the type has indeterminate size
- Alternative to `CDAC_TYPE_SIZE`

**`CDAC_TYPE_FIELD(typeName, fieldType, fieldName, offset)`**
- Defines a field within the type
- `fieldType`: primitive type or another defined type
- `fieldName`: diagnostic-friendly name (use managed names for managed types)
- `offset`: byte offset, usually `offsetof()` or `cdac_data<T>::FieldName`

**`CDAC_TYPE_END(typeName)`**
- Closes the type definition
- `typeName` must match the corresponding `CDAC_TYPE_BEGIN`

### Global Value Macros

**`CDAC_GLOBALS_BEGIN()` / `CDAC_GLOBALS_END()`**
- Delimits the global values section

**`CDAC_GLOBAL(globalName, typeName, value)`**
- Defines a global literal value
- `value` must be a compile-time constant
- `typeName` can be a primitive type or defined type

**`CDAC_GLOBAL_POINTER(globalName, address)`**
- Defines a global pointer value
- `address` must be a compile-time constant pointer or `uintptr_t`

**`CDAC_GLOBAL_STRING(globalName, stringValue)`**
- Defines a global string value
- `stringValue` must be a compile-time string literal


## Reference Implementation

For comprehensive examples, see the current implementation in:
- **`src/coreclr/debug/runtimeinfo/`** - Complete real-world implementation
  - `datadescriptor.h` - Headers and includes
  - `datadescriptor.inc` - Full type definitions for runtime objects
  - `contracts.jsonc` - Contract definitions
  - `CMakeLists.txt` - Build integration

## Related Documentation

- **[Data Contracts Design](../../../../docs/design/datacontracts/datacontracts_design.md)** - Overall design and motivation
- **[Contract Descriptor](../../../../docs/design/datacontracts/contract-descriptor.md)** - Binary format specification  
- **[Data Descriptor](../../../../docs/design/datacontracts/data_descriptor.md)** - Logical format specification
