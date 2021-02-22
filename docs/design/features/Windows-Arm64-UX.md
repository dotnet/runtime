# Windows Arm64 User Experience

## Mixed Architecture Side By Side Installs

This design is incomplete.  A rough sketch follows.

### Proposed Side-By-Side Design

For Windows the proposed plan is to:

- On Windows Arm .NET runtime versions TBD will always require `x64` architecture. All other runtime versions will require `arm64` architecture.

- Make the .NET 6 `dotnet` & `libhostfxr.dylib` (`hostfxr`) `arm64` binaries. This means dotnet will always start as a native process.

- The `hostfxr` will be modified to enforce the architecture version  policy above. It will return an architecture mismatch error when the runtime (by version policy above) require a different architecture than the currently running process.

- `dotnet` will be modified to enable switching the architecture of the current process when it sees an architecture mismatch error returned by `hostfxr`. The current process will exec a new process `dotnet_x64` at least the same arguments as the new process.

- The installer will be modified to enforce TBD policy.
-
- The existing installer releases will need to be tested to make sure they do not clobber the native `dotnet`.