# ILVerification

The ILVerification library is part of the ILVerify project. See details under [src/coreclr/tools/ILVerify](../ILVerify).

[PACKAGE.md](PACKAGE.md) has an example of using the library. Its API is described in the XML doc comments in its source, starting with [Verifier.cs](Verifier.cs) and [IResolver.cs](IResolver.cs).

The shared type system sources under [src/coreclr/tools/Common](../Common) are compiled into this library too. Apart from `ExceptionStringID`, which `VerificationResult.ExceptionID` returns, their public types are not part of the library's API.
