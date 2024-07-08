# Soft Debugger Wire Format

## Introduction

The [Mono Soft Debugger](/docs/advanced/runtime/docs/soft-debugger/) (SDB) is a debugger implemented by the Mono runtime. The Mono runtime exposes an interface that debugger clients can use to debug a Mono application. Mono provides a convenience library in the form of the Mono.Debugger.Soft.dll that can be used to communicate with a running Mono process.

The Mono.Debugger.Soft.dll library uses a protocol over sockets to debug applications. The wire protocol is inspired by the [JDWP (Java Debug Wire Protocol)](http://download.oracle.com/javase/1,5.0/docs/guide/jpda/jdwp-spec.html). Familiarity with that specification is a good read.

This document describes the wire protocol used between debugging clients and the Mono runtime.

Where possible, the corresponding protocol detail is linked to a function name and file location in Mono source code. These informations are based on Mono master version at revision *f42ba4a168e7cb9b9486b8a96c53752e4467be8a*.

## Protocol details

### Transport

Mono SDB protocol, just like its Java counterpart, was designed with no specific transport in mind. However, presently the public Mono SDB only has a TCP/IP transport available (under the transport name of `dt_socket`). Other transports can be plugged by modifying this interface.

#### Bootstraping a connection

To boostrap a connection, the client send handshake to the server (see `debugger-agent.c:1034`) in the form of the 13 ASCII characters string "DWP-Handshake" and wait for the server reply which consist of the exact same ASCII character sequence.

### Packets

Just like JDWP, Mono SDB protocol is packet-based with two types of packet: command and reply. All fields in a packet is sent in big-endian format which is transparently handled in Mono source code with corresponding helper encode/decode functions.

Command packet are used by either side (client or server) to request information, act on the execution of the debugged program or to inform of some event. Replies is only sent in response to a command with information on the success/failure of the operation and any extra data depending on the command that triggered it.

Both type of packet contains a header. The header is always 11 bytes long. Their descriptions are given afterwards:

<table border="1">
  <thead>
    <tr>
      <th colspan="12" style="text-align: center">Command packet header</th>
    </tr>
    <tr>
      <th>byte 1</th>
      <th>byte 2</th>
      <th>byte 3</th>
      <th>byte 4</th>
      <th>byte 5</th>
      <th>byte 6</th>
      <th>byte 7</th>
      <th>byte 8</th>
      <th>byte 9</th>
      <th>byte 10</th>
      <th>byte 11</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td colspan="4">length</td>
      <td colspan="4">id</td>
      <td>flags</td>
      <td>command set</td>
      <td>command</td>
    </tr>
  </tbody>
</table>

In Mono SDB source code, the command header is decoded in the server thread `debugger_thread` function at `debugger-agent.c:7583`.

<table border="1">
  <thead>
    <tr>
      <th colspan="12" style="text-align: center">Reply packet header</th>
    </tr>
    <tr>
      <th>byte 1</th>
      <th>byte 2</th>
      <th>byte 3</th>
      <th>byte 4</th>
      <th>byte 5</th>
      <th>byte 6</th>
      <th>byte 7</th>
      <th>byte 8</th>
      <th>byte 9</th>
      <th>byte 10</th>
      <th>byte 11</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td colspan="4">length</td>
      <td colspan="4">id</td>
      <td>flags</td>
      <td colspan="2">error code</td>
    </tr>
  </tbody>
</table>
In Mono SDB source code, a reply packet is constructed and sent by the `send_reply_packet` function in `debugger-agent:1514`.

#### Packet field details

##### Common fields

length : The total length in byte of the packet including header i.e. this value will be 11 if the packet only consists of header with no other data

id : Uniquely identify sent packet command/reply pair so that they can be asynchronously matched. This is in practise a simple monotonic integer counter. Note that client and server may use the same id value when sending their packets as the uniqueness property is only with respect to a specific source.

flags : At the moment this value is only used with a reply packet in which case its value is set to `0x80`. A command packet should have this value set to 0.

##### Command specific fields

command set : This value allows grouping commands into similar blocks for quicker processing. The different command sets with their values are given below:

| Command set      | Value |
|:-----------------|:------|
| Virtual Machine  | 1     |
| Object reference | 9     |
| String reference | 10    |
| Threads          | 11    |
| Array reference  | 13    |
| Event request    | 15    |
| Stack frame      | 16    |
| AppDomain        | 20    |
| Assembly         | 21    |
| Method           | 22    |
| Type             | 23    |
| Module           | 24    |
| Events           | 64    |

command : Tell what command this packet corresponds to. This value is relative to the previously defined command set so the values are reused across different command sets. Definition of each command is given in a later chapter.

##### Reply specific fields

error code : Define which error occured or if the command was successful. Error code definition is given below:

| Error name                | Value | Mono specific notes                                                        |
|:--------------------------|:------|:---------------------------------------------------------------------------|
| Success                   | 0     |                                                                            |
| Invalid object            | 20    |                                                                            |
| Invalid field ID          | 25    |                                                                            |
| Invalid frame ID          | 30    |                                                                            |
| Not Implemented           | 100   |                                                                            |
| Not Suspended             | 101   |                                                                            |
| Invalid argument          | 102   |                                                                            |
| Unloaded                  | 103   | AppDomain has been unloaded                                                |
| No Invocation             | 104   | Returned when trying to abort a thread which isn't in a runtime invocation |
| Absent information        | 105   | Returned when a requested method debug information isn't available         |
| No seq point at IL Offset | 106   | Returned when a breakpoint couldn't be set                                 |

#### Data type marshalling

| Name    | Size             | Description                                                                                                                                                             |
|:--------|:-----------------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| byte    | 1 byte           | A byte value                                                                                                                                                            |
| short   | 2 byte           | A UInt16 value                                                                                                                                                          |
| int     | 4 bytes          | A UInt32 value                                                                                                                                                          |
| long    | 8 bytes          | A UInt64 value                                                                                                                                                          |
| id      | 4 bytes          | The same size is used for all IDs (ObjectID, PointerID, TypeId, MethodID, AssemblyID, ModuleID, FieldID, PropertyID, DomainID)                                          |
| string  | At least 4 bytes | A string consists of a leading int value giving the string size followed by *size* bytes of character data. Thus an empty string is simply a 4 bytes integer value of 0 |
| variant | At least 1 byte  | A variant type is a special value which consists of a leading byte giving away the MonoType information of the variant followed directly by its raw value.              |
| boolean | 4 bytes (an int) | Tough not strictly a type, a boolean is represented by an int value whose value is 1 for true and 0 for false.                                                          |

Most of the encoding function for these types are defined as `buffer_add_*` functions starting from `debugger-agent.c:1429`. Their counterpart are of the form `decode_*` starting from `debugger-agent.c:1349`.

A lot command returns or accepts fixed-length list of value. In these case, such a list is always prefixed with an int value giving its length followed by *length* element of the same type (which needs to be inferred from the context). When such a list is used the term "list" will be used. For clarification, an empty list is thus a single int value equals to 0.

#### Various enumeration value definition

For the record, the following C enumerations define the values used for flags, kind, ... parameters in some commands.

``` c
typedef enum {
    EVENT_KIND_VM_START = 0,
    EVENT_KIND_VM_DEATH = 1,
    EVENT_KIND_THREAD_START = 2,
    EVENT_KIND_THREAD_DEATH = 3,
    EVENT_KIND_APPDOMAIN_CREATE = 4,
    EVENT_KIND_APPDOMAIN_UNLOAD = 5,
    EVENT_KIND_METHOD_ENTRY = 6,
    EVENT_KIND_METHOD_EXIT = 7,
    EVENT_KIND_ASSEMBLY_LOAD = 8,
    EVENT_KIND_ASSEMBLY_UNLOAD = 9,
    EVENT_KIND_BREAKPOINT = 10,
    EVENT_KIND_STEP = 11,
    EVENT_KIND_TYPE_LOAD = 12,
    EVENT_KIND_EXCEPTION = 13,
    EVENT_KIND_KEEPALIVE = 14,
    EVENT_KIND_USER_BREAK = 15,
    EVENT_KIND_USER_LOG = 16
} EventKind;
 
typedef enum {
    SUSPEND_POLICY_NONE = 0,
    SUSPEND_POLICY_EVENT_THREAD = 1,
    SUSPEND_POLICY_ALL = 2
} SuspendPolicy;
 
typedef enum {
    MOD_KIND_COUNT = 1,
    MOD_KIND_THREAD_ONLY = 3,
    MOD_KIND_LOCATION_ONLY = 7,
    MOD_KIND_EXCEPTION_ONLY = 8,
    MOD_KIND_STEP = 10,
    MOD_KIND_ASSEMBLY_ONLY = 11,
    MOD_KIND_SOURCE_FILE_ONLY = 12,
    MOD_KIND_TYPE_NAME_ONLY = 13,
    MOD_KIND_NONE = 14
} ModifierKind;
 
typedef enum {
    STEP_DEPTH_INTO = 0,
    STEP_DEPTH_OVER = 1,
    STEP_DEPTH_OUT = 2
} StepDepth;
 
typedef enum {
    STEP_SIZE_MIN = 0,
    STEP_SIZE_LINE = 1
} StepSize;
 
typedef enum {
    TOKEN_TYPE_STRING = 0,
    TOKEN_TYPE_TYPE = 1,
    TOKEN_TYPE_FIELD = 2,
    TOKEN_TYPE_METHOD = 3,
    TOKEN_TYPE_UNKNOWN = 4
} DebuggerTokenType;
 
typedef enum {
    VALUE_TYPE_ID_NULL = 0xf0,
    VALUE_TYPE_ID_TYPE = 0xf1,
    VALUE_TYPE_ID_PARENT_VTYPE = 0xf2
} ValueTypeId;
 
typedef enum {
    FRAME_FLAG_DEBUGGER_INVOKE = 1,

    // Use to allow the debugger to display managed-to-native transitions in stack frames.
    FRAME_FLAG_NATIVE_TRANSITION = 2
} StackFrameFlags;
 
typedef enum {
    INVOKE_FLAG_DISABLE_BREAKPOINTS = 1,
    INVOKE_FLAG_SINGLE_THREADED = 2,

    // Allow for returning the changed value types after an invocation
    INVOKE_FLAG_RETURN_OUT_THIS = 4,

    // Allows the return of modified value types after invocation
    INVOKE_FLAG_RETURN_OUT_ARGS = 8,

    // Performs a virtual method invocation
    INVOKE_FLAG_VIRTUAL = 16
} InvokeFlags;
```

### Command list

Types given in each command comments corresponds to the type described above. When there are additional arguments or multiple values in a command's reply, they are each time described in the order they appear or have to appear in the data part. Not also that there is no kind of separation sequence or added alignement padding between each value.

In all cases, if you ask for a command that doesn't exist, a reply will be sent with an error code of NOT_IMPLEMENTED.

#### Virtual machine commands

| Name                      | Value | Action and type of reply                                                                                                                                                | Additional parameters                                                                                                                                                                                                                                                                                                                                                                          | Possible error code returned                                      |
|:--------------------------|:------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:------------------------------------------------------------------|
| VERSION                   | 1     | Returns a mono virtual machine version information (string) followed by two int giving respectively the runtime major and minor version                                 | None                                                                                                                                                                                                                                                                                                                                                                                           | None                                                              |
| ALL_THREADS               | 2     | Returns a list of ObjectID each mapping to a System.Threading.Thread instance.                                                                                          | None                                                                                                                                                                                                                                                                                                                                                                                           | None                                                              |
| SUSPEND                   | 3     | Suspend the VM execution and returns an empty reply                                                                                                                     | None                                                                                                                                                                                                                                                                                                                                                                                           | None                                                              |
| RESUME                    | 4     | Resume the VM execution and returns an empty reply                                                                                                                      | None                                                                                                                                                                                                                                                                                                                                                                                           | NOT_SUSPENDED                                                     |
| EXIT                      | 5     | Stop VM and returns an empty reply                                                                                                                                      | Ask for a exit code (int) to be used by the VM when it exits                                                                                                                                                                                                                                                                                                                                   | None                                                              |
| DISPOSE                   | 6     | Clear event requests, resume the VM and disconnect                                                                                                                      | None                                                                                                                                                                                                                                                                                                                                                                                           | None                                                              |
| INVOKE_METHOD             | 7     | Returns a boolean telling if the call was successful followed by an exception object (as a variant) if it was not and by the actual returned value (variant) if it was. | Ask for an ObjectID (id) mapping to a System.Threading.Thread instance, a flags value (int) to pass to the invoke request, the MethodID (id) of the method to invoke, a variant value to be used as *this* (VALUE_TYPE_ID_NULL in case of a valuetype) and a list of variant value representing the parameters of the method.                                                                  | INVALID_OBJECT, NOT_SUSPENDED, INVALID_METHODID, INVALID_ARGUMENT |
| SET_PROTOCOL_VERSION      | 8     | Returns an empty reply                                                                                                                                                  | Ask for two int giving respectively the major and minor version of the procotol to use.                                                                                                                                                                                                                                                                                                        | None                                                              |
| ABORT_INVOKE              | 9     | Abort the invocation and returns an empty reply                                                                                                                         | Ask for an ObjectID (id) mapping to a System.Threading.Thread instance and the id (int) of the command packet that set up the invocation to cancel                                                                                                                                                                                                                                             | INVALID_OBJECT, NO_INVOCATION                                     |
| SET_KEEPALIVE             | 10    | Set up the new keep alive value and returns an empty reply                                                                                                              | Ask for a timeout value (int)                                                                                                                                                                                                                                                                                                                                                                  | None                                                              |
| GET_TYPES_FOR_SOURCE_FILE | 11    | Returns a list of TypeID (id) of class defined inside the supplied file name                                                                                            | Ask for a file name (string) and an ignore case flag (byte) although setting it to something different than 0 isn't currently supported.                                                                                                                                                                                                                                                       | None                                                              |
| GET_TYPES                 | 12    | Returns a list of TypeID (id) of type which corresponds to the provided type name                                                                                       | Ask for type name (string) and a ignore case flag (byte) which acts like a boolean value                                                                                                                                                                                                                                                                                                       | INVALID_ARGUMENT                                                  |
| INVOKE_METHODS            | 13    | Batch invocation of methods                                                                                                                                             | Ask for an ObjectID (id) mapping to a System.Threading.Thread instance, a flags value (int) to pass to the invoke request, the number of methods to invoke (int), and for each method the the MethodID (id) for each method to invoke, a variant value to be used as *this* (VALUE_TYPE_ID_NULL in case of a valuetype) and a list of variant value representing the parameters of the method. | INVALID_OBJECT, NOT_SUSPENDED, INVALID_METHODID, INVALID_ARGUMENT |
| VM_START_BUFFERING        | 14    | Initiates the buffering of reply packets to improve latency. Must be paired with a VM_STOP_BUFFERING command                                                            | None                                                                                                                                                                                                                                                                                                                                                                                           | None                                                              |
| VM_STOP_BUFFERING         | 15    | Ends the block of buffered commands, must come after a call to VM_START_BUFFERING                                                                                       | None                                                                                                                                                                                                                                                                                                                                                                                           | None                                                              |

The main function handling these commands is `vm_commands` and is situated at `debugger-agent.c:5671`

#### Events commands

Events allows the debuggee to act on program execution (stepping) and also to set up things like breakpoints, watchpoints, exception catching, etc.

| Name                          | Value | Type of reply                                        | Additional parameters                                                                                                                                                                             | Possible error code returned                                                                    |
|:------------------------------|:------|:-----------------------------------------------------|:--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:------------------------------------------------------------------------------------------------|
| REQUEST_SET                   | 1     | Returns the request id (int)                         | Ask for 3 bytes giving the event kind (EventKind enumeration), suspend policy (SuspendPolicy enumeration) and a list of modifiers which content is context dependent and given in the table below | INVALID_METHODID, INVALID_TYPEID, NO_SEQ_POINT_AT_IL_OFFSET, INVALID_OBJECT, INVALID_ASSEMBLYID |
| REQUEST_CLEAR                 | 2     | Clear the requested event and returns an empty reply | Ask for an event type (byte) and a request id (int)                                                                                                                                               | None                                                                                            |
| REQUEST_CLEAR_ALL_BREAKPOINTS | 3     | Returns an empty reply                               | None                                                                                                                                                                                              | None                                                                                            |

The main function handling these commands is `event_commands` and is situated at `debugger-agent.c:5916`

Each modifier has the first byte describing the modification it's carrying out and corresponding to the values found in the ModifierKind enumeration. The following table list the remaining body depending on the modification value.

| Mod value        | Body                                                                                                                                           |
|:-----------------|:-----------------------------------------------------------------------------------------------------------------------------------------------|
| COUNT            | a MethodID (id)                                                                                                                                |
| LOCATION_ONLY    | a MethodID (id) and a location information (long)                                                                                              |
| STEP             | A thread id, size of the step (int) corresponding to the StepSize enumeration and depth of it (int) corresponding to the StepDepth enumeration |
| THREAD_ONLY      | A thread id                                                                                                                                    |
| EXCEPTION_ONLY   | A TypeID representing a exception type and two byte values setting respectively the caught and uncaught filter                                 |
| ASSEMBLY_ONLY    | A list of AssemblyID (id)                                                                                                                      |
| SOURCE_FILE_ONLY | A list of source file name (string)                                                                                                            |
| TYPE_NAME_ONLY   | A list of type name (string)                                                                                                                   |
| NONE             |                                                                                                                                                |

#### Thread commands

Each command requires at least one ObjectID (of type id) parameter mapping to a thread instance before any additional parameter the command may require.

| Name           | Value | Type of reply                                                                                         | Additional parameters                                                                                 | Possible error code returned |
|:---------------|:------|:------------------------------------------------------------------------------------------------------|:------------------------------------------------------------------------------------------------------|:-----------------------------|
| GET_FRAME_INFO | 1     | Returns a list of quadruplet of frame ID (int), MethodID (id), IL offset (int) and frame flags (byte) | Ask for a start frame (currently other value than 0 aren't supported) as an int and a length as a int | INVALID_OBJECT               |
| GET_NAME       | 2     | Returns the name of the thread as a string                                                            | None                                                                                                  | INVALID_OBJECT               |
| GET_STATE      | 3     | Return the thread state as an int                                                                     | None                                                                                                  | INVALID_OBJECT               |
| GET_INFO       | 4     | Returns a byte value telling if the thread is a threadpool thread (1) or not (0)                      | None                                                                                                  | INVALID_OBJECT               |
| GET_ID         | 5     | Returns the thread id (address of the object) as a long                                               | None                                                                                                  | INVALID_OBJECT               |
| GET_TID        | 6     | Returns the proper thread id (or TID) as a long                                                       | None                                                                                                  | INVALID_OBJECT               |
| SET_IP         | 7     | Set the location where execution will return when this thread is resumed                              | Thread ID (int), Method ID (long), IL offset (long)                                                   | INVALID_ARGUMENT             |

The main function handling these commands is `thread_commands` and is situated at `debugger-agent.c:6991`

#### AppDomains commands

| Name               | Value | Type of reply                                                   | Additional parameters                                                                                                                   | Possible error code returned     |
|:-------------------|:------|:----------------------------------------------------------------|:----------------------------------------------------------------------------------------------------------------------------------------|:---------------------------------|
| GET_ROOT_DOMAIN    | 1     | Returns the DomainID of the root domain                         | None                                                                                                                                    | None                             |
| GET_FRIENDLY_NAME  | 2     | Returns the friendly name as a string of the provided DomainID  | Ask for a DomainID (id)                                                                                                                 | INVALID_DOMAINID                 |
| GET_ASSEMBLIES     | 3     | Returns a list of AssemblyID contained inside this AppDomain    | Ask for a DomainID (id)                                                                                                                 | INVALID_DOMAINID                 |
| GET_ENTRY_ASSEMBLY | 4     | Returns the entry AssemblyID of this domain                     | Ask for a DomainID (id)                                                                                                                 | INVALID_DOMAINID                 |
| CREATE_STRING      | 5     | Returns the ObjectID of the created string                      | Ask for a DomainID (id) where to create the new string and a string typed value to put inside the domain                                | INVALID_DOMAINID                 |
| GET_CORLIB         | 6     | Returns the AssemblyID of the load corlib inside this AppDomain | Ask for a DomainID (id)                                                                                                                 | INVALID_DOMAINID                 |
| CREATE_BOXED_VALUE | 7     | Returns the ObjectID of the boxed value                         | Ask for a DomainID (id), TypeID of the type that is going to be boxed and a variant value which is going to be put into the boxed value | INVALID_DOMAINID, INVALID_TYPEID |

The main function handling these commands is `domain_commands` and is situated at `debugger-agent.c:6104`

#### Assembly commands

Each command requires at least one AssemblyID (of type id) parameter before any additional parameter the command may require.

| Name                | Value | Type of reply                                                                                                                 | Additional parameters                                                                                            | Possible error code returned |
|:--------------------|:------|:------------------------------------------------------------------------------------------------------------------------------|:-----------------------------------------------------------------------------------------------------------------|:-----------------------------|
| GET_LOCATION        | 1     | Returns the filename (string) of image associated to the assembly                                                             | None                                                                                                             | INVALID_ASSEMBLYID           |
| GET_ENTRY_POINT     | 2     | Returns the MethodID (id) of the entry point or a 0 id if there is none (in case of dynamic assembly or library for instance) | None                                                                                                             | INVALID_ASSEMBLYID           |
| GET_MANIFEST_MODULE | 3     | Returns the ModuleID (id) of the assembly                                                                                     | None                                                                                                             | INVALID_ASSEMBLYID           |
| GET_OBJECT          | 4     | Returns the ObjectID of the AssemblyID object instance                                                                        | None                                                                                                             | INVALID_ASSEMBLYID           |
| GET_TYPE            | 5     | Returns the TypeID of the found type or a null id if it wasn't found                                                          | Ask for a type information in form of a string and a byte value to tell if case should be ignored (1) or not (0) | INVALID_ASSEMBLYID           |
| GET_NAME            | 6     | Return the full name of the assembly as a string                                                                              | None                                                                                                             | INVALID_ASSEMBLYID           |

The main function handling these commands is `assembly_commands` and is situated at `debugger-agent.c:6203`

#### Module commands

| Name                | Value | Type of reply                                                                                                   | Additional parameters   | Possible error code returned |
|:--------------------|:------|:----------------------------------------------------------------------------------------------------------------|:------------------------|:-----------------------------|
| CMD_MODULE_GET_INFO | 1     | Returns the following strings: basename of the image, scope name, full name, GUID and the image AssemblyID (id) | Ask for a ModuleID (id) | None                         |

The main function handling these commands is `module_commands` and is situated at `debugger-agent.c:6295`

#### Method commands

Each command requires at least one MethodID (of type id) parameter before any additional parameter the command may require.

| Name                | Value | Type of reply                                                                                                                                                                                                                                                                  | Additional parameters                                                          | Possible error code returned       |
|:--------------------|:------|:-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:-------------------------------------------------------------------------------|:-----------------------------------|
| GET_NAME            | 1     | Returns a string of the method name                                                                                                                                                                                                                                            | None                                                                           | INVALID_METHODID                   |
| GET_DECLARING_TYPE  | 2     | Returns a TypeID of the declaring type for this method                                                                                                                                                                                                                         | None                                                                           | INVALID_METHODID                   |
| GET_DEBUG_INFO      | 3     | Returns the code size of the method (int), source file name (string) and a list of tuple of IL offset (int) and line numbers (int) for the method                                                                                                                              | None                                                                           | INVALID_METHODID                   |
| GET_PARAM_INFO      | 4     | Returns the call convention (int), parameter count (int), generic parameter count (int), TypeID of the returned value (id), *parameter count* TypeID for each parameter type and finally *parameter count* parameter name (string) for each parameter.                         | None                                                                           | INVALID_METHODID                   |
| GET_LOCALS_INFO     | 5     | Returns the number of locals (int) followed by the TypeID (id) for each locals, followed by the name (string) of each locals (empty string if there is none) and finally followed by the scope of each locals which is a tuple of int giving the start address and end offset. | None                                                                           | INVALID_METHODID                   |
| GET_INFO            | 6     | Returns 3 int representing respectively the method flags, implementation flags and token                                                                                                                                                                                       | None                                                                           | INVALID_METHODID                   |
| GET_BODY            | 7     | Returns a list of byte corresponding to the method IL code.                                                                                                                                                                                                                    | None                                                                           | INVALID_METHODID                   |
| RESOLVE_TOKEN       | 8     | Returns a variant value corresponding to the provided token                                                                                                                                                                                                                    | Ask for a token value (int)                                                    | INVALID_METHODID                   |
| GET_CATTRS          | 9     | Returns the custom attributes for the methods                                                                                                                                                                                                                                  | Method ID, attribute-type ID                                                   | INVALID_METHODID,LOADER_ERROR      |
| MAKE_GENERIC_METHOD | 10    | Makes a generic version of the method                                                                                                                                                                                                                                          | Method ID, number of type arguments (int), TypeID for each type argument (int) | INVALID_ARGUMENT, INVALID_METHODID |

The main functions handling these commands are `method_commands` and `method_commands_internal` and are situated at `debugger-agent.c:6968` and `debugger-agent.c:6968` respectively.

#### Type commands

Each command requires at least one TypeID (of type id) parameter before any additional parameter the command may require.

| Name                | Value | Type of reply                                                                                                                                                                                                                                                                                                                                                                       | Additional parameters                                                                                                                                                      | Possible error code returned                    |
|:--------------------|:------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:------------------------------------------------|
| GET_INFO            | 1     | Returns the following informations about the type in that order: namespace (string), class name (string), full name (string), AssemblyID (id), ModuleID (id), TypeID (id), TypeID (id) of underlying type (or a 0 id if there is none), type token (int), type rank (byte), type flags (int), underlying byval type (byte) flags (see after table) and a list of nested type TypeID | None                                                                                                                                                                       | INVALID_TYPEID                                  |
| GET_METHODS         | 2     | Returns a list of MethodID corresponding to each of the method of the type                                                                                                                                                                                                                                                                                                          | None                                                                                                                                                                       | INVALID_TYPEID                                  |
| GET_FIELDS          | 3     | Returns list of quadruplet of FieldID (id), field name (string), field TypeID (id), field attributes (int)                                                                                                                                                                                                                                                                          | None                                                                                                                                                                       | INVALID_TYPEID                                  |
| GET_VALUES          | 4     | Returns a number of variant value equals to the number of FieldID that was passed as parameter. If the field had a ThreadStatic attribute applied to it, value fetched are from the current thread point of view.                                                                                                                                                                   | Ask for a list of FieldID representing this type static fields to the the value of. Only static field are supported.                                                       | INVALID_TYPEID, INVALID_FIELDID                 |
| GET_OBJECT          | 5     | Returns an ObjectID corresponding to the type instance                                                                                                                                                                                                                                                                                                                              | None                                                                                                                                                                       | INVALID_TYPEID                                  |
| GET_SOURCE_FILES    | 6     | Returns the same output than GET_SOURCE_FILES_2 except only the basename of each path is returned                                                                                                                                                                                                                                                                                   | None                                                                                                                                                                       | INVALID_TYPEID                                  |
| SET_VALUES          | 7     | Returns an empty response                                                                                                                                                                                                                                                                                                                                                           | Ask for a list of tuple of FieldID and variant value. Only pure static field can be set (i.e. with no extra attribute like ThreadStatic).                                  | INVALID_TYPEID, INVALID_FIELDID                 |
| IS_ASSIGNABLE_FROM  | 8     | Returns a boolean equals to true if the type is assignable from the other provided type, false otherwise                                                                                                                                                                                                                                                                            | Ask for an extra TypeID                                                                                                                                                    | INVALID_TYPEID                                  |
| GET_PROPERTIES      | 9     | Returns a list of quadruplet of FieldID (id), get accessor MethodID (string), set accessor MethodID (id), property attributes (int)                                                                                                                                                                                                                                                 | None                                                                                                                                                                       | INVALID_TYPEID                                  |
| GET_CATTRS          | 10    | Returns a list of custom attribute applied on the type. Custom attribute definition is given below.                                                                                                                                                                                                                                                                                 | Ask for a TypeID of an custom attribute type                                                                                                                               | INVALID_TYPEID                                  |
| GET_FIELD_CATTRS    | 11    | Returns a list of custom attributes of a type's field. Custom attribute definition is given below.                                                                                                                                                                                                                                                                                  | Ask for a FieldID of one the type field and a TypeID of an custom attribute type                                                                                           | INVALID_TYPEID, INVALID_FIELDID                 |
| GET_PROPERTY_CATTRS | 12    | Returns a list of custom attributes of a type's property. Custom attribute definition is given below.                                                                                                                                                                                                                                                                               | Ask for a PropertyID of one the type field and a TypeID of an custom attribute type                                                                                        | INVALID_TYPEID, INVALID_PROPERTYID              |
| GET_SOURCE_FILES_2  | 13    | Returns a list of source file full paths (string) where the type is defined                                                                                                                                                                                                                                                                                                         | None                                                                                                                                                                       | INVALID_TYPEID                                  |
| GET_VALUES_2        | 14    | Returns a number of variant value equals to the number of FieldID that was passed as parameter. If the field had a ThreadStatic attribute applied to it, value fetched are from the thread parameter point of view.                                                                                                                                                                 | Ask for an ObjectID representing a System.Thread instance and a list of FieldID representing this type static fields to the the value of. Only static field are supported. | INVALID_OBJECT, INVALID_TYPEID, INVALID_FIELDID |

The main functions handling these commands are `type_commands` and `type_commands_internal` and are situated at `debugger-agent.c:6726` and `debugger-agent.c:6403` respectively.

Byval flags is an indication of the type attribute for a parameter when it's passed by value. A description of these flags follows:

| byte 1            | byte 2              | byte 3          | byte 4            | byte 5 | byte 6 | byte 7 | byte 8 |
|:------------------|:--------------------|:----------------|:------------------|:-------|:-------|:-------|:-------|
| Is a pointer type | Is a primitive type | Is a value type | Is an enumeration | Unused | Unused | Unused | Unused |

Custom attribute definition is as follows: MethodID of the attribute ctor, a list of variant objects representing the typed arguments of the attribute prepended by a length attribute (int) and another list representing named arguments of which elements are either tuple of the constant 0x53 followed by a variant value (in case the named argument is a field) or a triplet of the constant 0x54 followed by a PropertyID followed by a variant value (in case the named argument is a property). In both list case, an empty list is simply one int of value 0.

#### Stackframe commands

Each command requires at least one ObjectID (of type id) parameter mapping to a System.Threading.Thread instance and a FrameID (of type id) before any additional parameter the command may require.

| Name       | Value | Type of reply                                                                                                                                                                        | Additional parameters                                                                             | Possible error code returned                                          |
|:-----------|:------|:-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:--------------------------------------------------------------------------------------------------|:----------------------------------------------------------------------|
| GET_VALUES | 1     | Returns a list of miscelleanous typed values. If the position information was negative, the value corresponds to a parameter and if it was positive to a local variable.             | Ask for a list of position (int) information.                                                     | INVALID_OBJECT, INVALID_FRAMEID, ABSENT_INFORMATION                   |
| GET_THIS   | 2     | Returns the *this* value prepended by a single byte value describing its type, or the special TYPE_ID_NULL (byte) value which is equal to 0xf0 in case there is no *this* parameter. | None                                                                                              | INVALID_OBJECT, INVALID_FRAMEID, ABSENT_INFORMATION                   |
| SET_VALUES | 3     | Returns an empty reply                                                                                                                                                               | Ask for a list of pair of position (int) information and variant whose value is going to be used. | INVALID_OBJECT, INVALID_FRAMEID, ABSENT_INFORMATION, INVALID_ARGUMENT |

The main function handling these commands is `frame_commands` and is situated at `debugger-agent.c:7082`

#### Array commands

Each command requires at least one ObjectID (of type id) parameter mapping to a System.Array instance before any additional parameter the command may require.

| Name       | Value | Type of reply                                                                                                                                                                                                                                                                                                                                    | Additional parameters                                                                                                                                                                                               | Possible error code returned |
|:-----------|:------|:-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:-----------------------------|
| GET_LENGTH | 1     | Returns an int corresponding to the array rank followed by a set of int pair corresponding respectively to the length and lower bound of each of the array dimensions. In case of a single dimensional zero-based array, the returned data amount to 3 int values with the second being the total length of the array and the third one being 0. | None                                                                                                                                                                                                                | INVALID_OBJECT               |
| GET_VALUES | 2     | Returns a list of *length* elements which individual size in bytes depends on the underlying type of the System.Array instance.                                                                                                                                                                                                                  | Ask for an index (int) and a length (int) to determine the range of value to return                                                                                                                                 | INVALID_OBJECT               |
| SET_VALUES | 3     | Return an empty reply                                                                                                                                                                                                                                                                                                                            | Ask for an index (int) and a length (int) to determine the range of value to set and a *length* number of trailing values whose type and byte size match those of the underlying type of the System.Array instance. | INVALID_OBJECT               |

The main function handling these commands is `vm_commands` and is situated at `debugger-agent.c:5671`

#### String commands

Each command requires at least one ObjectID (of type id) parameter mapping to a System.String instance before any additional parameter the command may require.

| Name       | Value | Type of reply                                                                                                      | Additional parameters                                                                   | Possible error code returned     |
|:-----------|:------|:-------------------------------------------------------------------------------------------------------------------|:----------------------------------------------------------------------------------------|:---------------------------------|
| GET_VALUE  | 1     | Returns a UTF8-encoded string corresponding to the System.String instance with its length prepended as a int value | None                                                                                    | INVALID_OBJECT                   |
| GET_LENGTH | 2     | Returns the length of a UTF8-encoded string corresponding to the System.String instance as an int value            | None                                                                                    | INVALID_OBJECT                   |
| GET_CHARS  | 3     | Returns *length* short values each encoding a character of the string slice                                        | Ask for a start index (long) and a length parameter (long) of the string slice to take. | INVALID_OBJECT, INVALID_ARGUMENT |

The main function handling these commands is `string_commands` and is situated at `debugger-agent.c:7293`

#### Object commands

Each command requires at least one ObjectID (of type id) parameter before any additional parameter the command may require.

| Name         | Value | Type of reply                                                                                                     | Additional parameters                                                             | Possible error code returned              |
|:-------------|:------|:------------------------------------------------------------------------------------------------------------------|:----------------------------------------------------------------------------------|:------------------------------------------|
| GET_TYPE     | 1     | Returns the TypeID as an id                                                                                       | None                                                                              | INVALID_OBJECT                            |
| GET_VALUES   | 2     | Returns *length* values of miscellaneous type and size corresponding to the underlying type of each queried field | Ask for a list of FieldID to fetch value of                                       | INVALID_OBJECT, UNLOADED, INVALID_FIELDID |
| IS_COLLECTED | 3     | Returns an int equals to 1 if the object has been collected by GC, 0 otherwise                                    | None                                                                              | None                                      |
| GET_ADDRESS  | 4     | Returns a long value corresponding to the address where the object is stored in memory                            | None                                                                              | INVALID_OBJECT                            |
| GET_DOMAIN   | 5     | Returns an id corresponding to the DomainID the object is located in                                              | None                                                                              | INVALID_OBJECT                            |
| SET_VALUES   | 6     | Returns an empty reply                                                                                            | Ask for a list of tuple of FieldID (id) and of the value that should be set to it | INVALID_OBJECT, UNLOADED, INVALID_FIELDID |

The main function handling these commands is `object_commands` and is situated at `debugger-agent.c:7318`

#### Composite commands

| Name      | Value | Description                                                              |
|:----------|:------|:-------------------------------------------------------------------------|
| COMPOSITE | 100   | This command is actually part of the event command set and is used for ? |

## Differences with JDWP

-   Handshake ASCII sequence is DWP-Handshake instead of JDWP-Handshake
-   Some new Mono specific command set such as AppDomain, Assembly or Module and removal/renaming of some Java specific set such as InterfaceType, ThreadGroupReference, ClassLoaderReference, etc.
-   Mono SDB protocol has its own specific ID types related to the new command sets.
-   SDB protocol has less error code although some are Mono-specific like "No Invocation", "Absent Informations" and "No seq point at IL offset" codes.
