# Stack Buffer Overflow Protection

This document describes mechanisms in the .NET code generator to guard against stack buffer overflows at runtime.

## Background

While .NET is primarily a type and memory safe "managed" programming platform, it also offers lower-level
facilities to allow for interop with native code, as well as some constructs that cannot be proven safe.

Use of these potentially "unsafe" constructs can threaten the integrity of the .NET runtime stack, enabling modification
of key information on a stack frame, like the values of code and data addresses.

The .NET code generator includes stack buffer overflow protection (aka Guard Stack or "GS") so that the integrity of the stack
can be checked at key points during program execution&mdash;walking the stack for EH or GC, or returning from methods.

Stack buffer overflow protection is one part of a more comprehensive set of
[.NET runtime security mitigations](https://github.com/dotnet/designs/blob/main/accepted/2021/runtime-security-mitigations.md).

## How GS Works

GS is intended to detect buffer overruns from unsafe on-stack buffers that might corrupt vulnerable data on the stack.

Unsafe buffers include:
* memory regions allocated dynamically on the stack, via `stackalloc` in C# (aka `localloc`, in IL)
* value classes marked as unsafe by language compilers, via `System.Runtime.CompilerServices.UnsafeValueTypeAttribute`.
For instance, C# [fixed-sized buffers](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/unsafe-code#fixed-size-buffers).

Vulnerable data on the stack frame generally includes addresses of code and data.

GS protects this data in two ways:
* When possible, vulnerable data is moved lower on the stack frame, below unsafe buffers.
* For data that cannot be relocated (like the return address), a "stack cookie" (aka "stack canary") is allocated between
the unsafe buffers and the un-relocatable vulnerable data. This cookie value varies from run to run and its value
is verified before method exit and on stack walks done by the runtime for EH and GC.

The net effect is that the stack layout for methods with unsafe buffers is as follows (note stacks grow down,
so any caller frames would be above and any callee frames below)

| Stack Frame |
| --------- |
| memory arguments |
| return address   |
| saved frame pointer |
| callee save area |
| stack cookie |
| fixed-sized unsafe buffers (without pointers) |
| fixed-sized unsafe buffers (with pointers) |
| shadow copies of vulnerable memory arguments |
| local variables |
| dynamically allocated buffers (localloc) |
| outgoing arguments |
| (stack pointer points here) |

Vulnerable memory arguments are relocated to a shadow copy region below the unsafe fixed buffers. Within the fixed-sized
buffer region, buffers are ordered so that buffers containing pointers are at lower addresses than buffers without pointers.

A buffer overrun that can corrupt vulnerable data will likely also corrupt the stack cookie. The cookie value is verified
before the method returns (and also when the runtime triggers stack walks).

In addition, the return address may also be protected by hardware mechanisms like
[Control-flow Enforcement Technology (CET)](https://github.com/dotnet/runtime/blob/main/docs/design/features/cet-feature.md),
when these facilities are available on the host machine.

## GS Check Failures

A cookie verification failure leads to an immediate, uncatchable process exit (`FailFast`) since the integrity
of the process is in question.