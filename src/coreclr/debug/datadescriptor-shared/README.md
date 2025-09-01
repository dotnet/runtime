# Datadescriptor Implementation Infrastructure

This folder contains infrastructure to create data descriptors as defined in the [data_descriptor.md](../../../../docs/design/datacontracts/data_descriptor.md). Data descriptors enable diagnostic tooling (debuggers, profilers, etc.) to understand the internal layout and structure of .NET runtime objects without requiring intimate knowledge of implementation details.

## CMake Integration and Build System

### Function Parameters

The `generate_data_descriptors` function defined in `clrdatadescriptors.cmake` takes the following arguments:

* **`LIBRARY_NAME`** (Required) - Sets the name of the target object being created
* **`CONTRACT_FILE`** (Required) - Path to the contract JSON file defining supported contracts
* **`CONTRACT_NAME`** (Required) - Name of the `ContractDescriptor` export symbol
* **`INTERFACE_TARGET`** (Required) - Interface target providing dependencies, include directories, and definitions
* **`EXPORT_VISIBLE`** (Optional) - Controls if the `CONTRACT_NAME` will be exported from the DLL

### Two-Phase Build Process

The build system uses a two-phase approach:

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

**`CDAC_GLOBAL_SUB_DESCRIPTOR(globalName, address)`**
- Defines a reference to another contract descriptor
- `address` must be a compile-time constant pointer to a pointer to a contract descriptor
- Used for multi-contract scenarios where one contract references another
- Example: `CDAC_GLOBAL_SUB_DESCRIPTOR(GC, &(g_gc_dac_vars.gc_descriptor))`


## Current Implementation

For reference, see the current implementation in:
- **`src/coreclr/vm/datadescriptor/`** - Complete real-world implementation
  - `datadescriptor.h` - Headers and includes
  - `datadescriptor.inc` - Full type definitions for runtime objects
  - `contracts.jsonc` - Contract definitions
  - `CMakeLists.txt` - Build integration

## Related Documentation

- **[Data Contracts Design](../../../../docs/design/datacontracts/datacontracts_design.md)** - Overall design and motivation
- **[Contract Descriptor](../../../../docs/design/datacontracts/contract-descriptor.md)** - Binary format specification
- **[Data Descriptor](../../../../docs/design/datacontracts/data_descriptor.md)** - Logical format specification
