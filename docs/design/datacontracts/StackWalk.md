# Contract StackWalk

This contract encapsulates support for StackWalking managed threads.

## APIs of contract

```csharp
public interface IStackWalkHandle { };
public interface IStackDataFrameHandle { };
```

```csharp
// Creates a stack walk and returns a handle
IStackWalkHandle CreateStackWalk(ThreadData threadData);
// Iterates the stackWalkHandle to the next frame. If successful, returns true. Otherwise false.
bool Next(IStackWalkHandle stackWalkHandle);
// Gets the current frame from a stack walk and returns a IStackDataFrameHandle to it.
IStackDataFrameHandle GetCurrentFrame(IStackWalkHandle stackWalkHandle);
// Gets the thread context at the given stack dataframe.
byte[] GetRawContext(IStackDataFrameHandle stackDataFrameHandle);
// Gets the Frame address at the given stack dataframe. Returns TargetPointer.Null if the current dataframe does not have a valid Frame.
TargetPointer GetFrameAddress(IStackDataFrameHandle stackDataFrameHandle);
```

## Version 1

The `StackWalk` contract provides an interface for walking the stack of a managed thread. It includes methods to create a stack walk, move to the next frame, get the current frame, retrieve the raw context, and get the frame address.

### CreateStackWalk

```csharp
IStackWalkHandle CreateStackWalk(ThreadData threadData);
```

Creates a stack walk handle for the given thread data. This initializes the context and frame iterator for the stack walk.

### Next

```csharp
bool Next(IStackWalkHandle stackWalkHandle);
```

Moves to the next frame in the stack walk. Returns `true` if successfully moved to the next frame. Otherwise returns `false`.

### GetCurrentFrame

```csharp
IStackDataFrameHandle GetCurrentFrame(IStackWalkHandle stackWalkHandle);
```

Gets the current frame in the stack walk. Returns a handle to the stack data frame.

### GetRawContext

```csharp
byte[] GetRawContext(IStackDataFrameHandle stackDataFrameHandle);
```

Retrieves the raw Windows thread context of the current frame as a byte array. The size and shape of the context is platform dependent. See [CONTEXT structure](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-context) for more info.

### GetFrameAddress

```csharp
TargetPointer GetFrameAddress(IStackDataFrameHandle stackDataFrameHandle);
```

Gets the frame address of the current frame. Returns `TargetPointer.Null` if the frame is not valid.
