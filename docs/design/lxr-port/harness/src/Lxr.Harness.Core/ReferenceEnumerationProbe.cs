// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Lxr.Harness.Core;

public sealed class ReferenceEnumerationSnapshot
{
    public required string HookLibraryPath { get; init; }

    public required string HookLibrarySha256 { get; init; }

    public required int ExpectedMode { get; init; }

    public required string ExpectedModeName { get; init; }

    public required int Mode { get; init; }

    public required int Errors { get; init; }

    public required long ObjectScans { get; init; }

    public required long Ranges { get; init; }

    public required long Slots { get; init; }

    public required long NonNullSlots { get; init; }

    public required ulong Checksum { get; init; }

    public IReadOnlyList<string> Validate()
    {
        var failures = new List<string>();
        if (Mode != ExpectedMode)
        {
            failures.Add($"reference-enumeration mode expected {ExpectedMode}, observed {Mode}");
        }
        if (Errors != 0)
        {
            failures.Add($"reference-enumeration native error count is {Errors}");
        }
        if (ObjectScans <= 0)
        {
            failures.Add("reference-enumeration observed no object scans");
        }
        if (Ranges <= 0)
        {
            failures.Add("reference-enumeration observed no ranges");
        }
        if (Slots <= 0)
        {
            failures.Add("reference-enumeration observed no slots");
        }
        if (NonNullSlots <= 0)
        {
            failures.Add("reference-enumeration observed no non-null slots");
        }
        if (Checksum == 0)
        {
            failures.Add("reference-enumeration checksum is zero");
        }
        return failures;
    }
}

public sealed class ReferenceEnumerationProbe : IDisposable
{
    public const string HookLibraryEnvironment =
        "P15_REFERENCE_ENUMERATION_HOOK_LIBRARY";
    public const string ExpectedModeEnvironment =
        "P15_REFERENCE_ENUMERATION_EXPECTED_MODE";
    public const string ExpectedModeNameEnvironment =
        "P15_REFERENCE_ENUMERATION_EXPECTED_MODE_NAME";
    public const string FixedFullCollectionCountEnvironment =
        "P15_REFERENCE_ENUMERATION_FIXED_FULL_COLLECTION_COUNT";

    private readonly nint _library;
    private readonly ResetDelegate _reset;
    private readonly StopDelegate _stop;
    private readonly GetSnapshotDelegate _getSnapshot;
    private readonly string _libraryPath;
    private readonly string _librarySha256;
    private readonly int _expectedMode;
    private readonly string _expectedModeName;
    private bool _stopped;

    private ReferenceEnumerationProbe(
        string libraryPath,
        int expectedMode,
        string expectedModeName)
    {
        _libraryPath = Path.GetFullPath(libraryPath);
        _librarySha256 = Convert.ToHexStringLower(
            SHA256.HashData(File.ReadAllBytes(_libraryPath)));
        _expectedMode = expectedMode;
        _expectedModeName = expectedModeName;
        _library = NativeLibrary.Load(_libraryPath);
        _reset = GetDelegate<ResetDelegate>(
            "GC_ObjectReferenceEnumerationTest_Reset");
        _stop = GetDelegate<StopDelegate>(
            "GC_ObjectReferenceEnumerationTest_Stop");
        _getSnapshot = GetDelegate<GetSnapshotDelegate>(
            "GC_ObjectReferenceEnumerationTest_GetSnapshot");
    }

    public int FixedFullCollectionCount { get; private init; }

    public static ReferenceEnumerationProbe? TryCreateFromEnvironment()
    {
        string? libraryPath =
            Environment.GetEnvironmentVariable(HookLibraryEnvironment);
        string? expectedModeText =
            Environment.GetEnvironmentVariable(ExpectedModeEnvironment);
        string? expectedModeName =
            Environment.GetEnvironmentVariable(ExpectedModeNameEnvironment);
        string? fixedFullCollectionCountText =
            Environment.GetEnvironmentVariable(
                FixedFullCollectionCountEnvironment);

        if (libraryPath is null &&
            expectedModeText is null &&
            expectedModeName is null &&
            fixedFullCollectionCountText is null)
        {
            return null;
        }
        if (string.IsNullOrWhiteSpace(libraryPath) ||
            !int.TryParse(expectedModeText, out int expectedMode) ||
            string.IsNullOrWhiteSpace(expectedModeName) ||
            !int.TryParse(
                fixedFullCollectionCountText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int fixedFullCollectionCount) ||
            fixedFullCollectionCount is < 0 or > 16)
        {
            throw new InvalidOperationException(
                "The P1.5 reference-enumeration hook requires a library, numeric mode, mode name, and a full-collection count from 0 through 16.");
        }

        return new ReferenceEnumerationProbe(
            libraryPath,
            expectedMode,
            expectedModeName)
        {
            FixedFullCollectionCount = fixedFullCollectionCount,
        };
    }

    public void Reset()
    {
        _reset();
        _stopped = false;
    }

    public ReferenceEnumerationSnapshot CaptureAndStop()
    {
        _stop();
        _getSnapshot(out NativeSnapshot native);
        _stopped = true;
        return new ReferenceEnumerationSnapshot
        {
            HookLibraryPath = _libraryPath,
            HookLibrarySha256 = _librarySha256,
            ExpectedMode = _expectedMode,
            ExpectedModeName = _expectedModeName,
            Mode = native.Mode,
            Errors = native.Errors,
            ObjectScans = native.ObjectScans,
            Ranges = native.Ranges,
            Slots = native.Slots,
            NonNullSlots = native.NonNullSlots,
            Checksum = native.Checksum,
        };
    }

    public void Dispose()
    {
        if (!_stopped)
        {
            _stop();
        }
        NativeLibrary.Free(_library);
    }

    private T GetDelegate<T>(string name)
        where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(
            NativeLibrary.GetExport(_library, name));

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeSnapshot
    {
        public int Mode;
        public int Errors;
        public long ObjectScans;
        public long Ranges;
        public long Slots;
        public long NonNullSlots;
        public ulong Checksum;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ResetDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void StopDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void GetSnapshotDelegate(out NativeSnapshot snapshot);
}
