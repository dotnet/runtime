// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Redundant branch opts jump-threaded flow around a block and sharpened that block's
// predicate VN, but left dominator info claiming the block still dominated its successors.
// A later dominator-based inference then folded away the null check on an 'isinst' result,
// so the 'is MergeFile' arm below was entered with a null value.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

namespace Runtime_130700;

internal static class Contract
{
    public static void Assert(bool condition, string? message = "", params object?[] args)
    {
        if (condition)
        {
            return;
        }

        message ??= string.Empty;
        throw new Exception(string.Format(message, args));
    }

    public static void Fail(string? message = "", params object?[] args)
    {
        message ??= string.Empty;
        throw new Exception(string.Format(message, args));
    }

    public static T AssertNotNull<T>(T? o, string? message = "", params object?[] args)
        where T : class
    {
        if (o != null)
        {
            return o;
        }

        message ??= string.Empty;
        throw new Exception(string.Format(message, args));
    }
}

internal static class EnumerableExtensions
{
    public static void ForEach<T>(this IEnumerable<T>? enumeration, Action<T>? action)
    {
        if (enumeration == null || action == null)
        {
            return;
        }

        foreach (T item in enumeration)
        {
            action(item);
        }
    }
}

public abstract class MergeHierarchyMember
{
}

public class MergeFile : MergeHierarchyMember
{
    public string FileName
    {
        get => field;
        set
        {
            Contract.Assert(!string.IsNullOrWhiteSpace(value));
            field = value;
        }
    }

    public MergeFile(string fileName)
    {
        Contract.Assert(fileName.All(c => c != Path.DirectorySeparatorChar && c != Path.AltDirectorySeparatorChar),
                        "FileName must not contain any directory separator characters");
        FileName = fileName;
    }
}

public class MergeHierarchy : MergeHierarchyMember
{
    public enum MergeMode
    {
        Automatic,
        Manual,
        PostProcessing
    }

    public MergeMode Mode { get; set; }

    public List<ChildWithOffset> Children { get; set; } = new();

    public MergeHierarchy(MergeMode mergeMode)
    {
        Mode = mergeMode;
    }

    public MergeHierarchy(MergeMode mergeMode, params (MergeHierarchyMember Member, long Offset)[] membersWithOffsets)
        : this(mergeMode)
    {
        Contract.AssertNotNull(membersWithOffsets);
        membersWithOffsets.ForEach(elem => AddMergeHierarchyMember(elem.Member, elem.Offset));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void AddMergeHierarchyMember(MergeHierarchyMember mergeHierarchyMember, long offset)
    {
        Contract.AssertNotNull(mergeHierarchyMember);
        Contract.Assert(mergeHierarchyMember is not MergeHierarchy { Mode: MergeMode.PostProcessing },
                        "Post-processing can only be the root node of a merge hierarchy");

        // 'mergeHierarchyMember is MergeFile' is only evaluated when Mode is Automatic.
        Contract.Assert(Mode is not MergeMode.Automatic || (Mode is MergeMode.Automatic && mergeHierarchyMember is MergeFile),
                        "An automatic merge hierarchy cannot be nested in another automatic merge hierarchy");

        Children.Add(new ChildWithOffset(mergeHierarchyMember, offset));
    }
}

public class ChildWithOffset
{
    public enum MemberKind
    {
        MergeFile,
        MergeHierarchy,
    }

    public MemberKind Kind { get; set; }

    public MergeHierarchy? MergeHierarchy { get; set; }

    public MergeFile? MergeFile
    {
        get => field;
        set
        {
            Contract.Assert(value is null || !string.IsNullOrWhiteSpace(value.FileName));
            field = value;
        }
    }

    public long Offset { get; set; }

    public ChildWithOffset(MergeHierarchyMember mergeHierarchyMember, long offset)
    {
        switch (mergeHierarchyMember)
        {
            case MergeFile mergeFile:
                Kind = MemberKind.MergeFile;
                MergeFile = mergeFile;
                break;
            case MergeHierarchy mergeHierarchy:
                Kind = MemberKind.MergeHierarchy;
                MergeHierarchy = mergeHierarchy;
                break;
            default:
                Contract.Fail("Unknown MergeHierarchyMember");
                break;
        }

        Offset = offset;
    }
}

public class Runtime_130700
{
    [Fact]
    public static void TestEntryPoint()
    {
        // Fixed sub-hierarchy, so nothing is retained across iterations.
        MergeHierarchy inner = new MergeHierarchy(
            MergeHierarchy.MergeMode.Manual, (new MergeFile("hello"), 0), (new MergeFile("world"), 0));

        // AddMergeHierarchyMember has to reach tier-1 with profile data, so keep calling it
        // (with both a MergeHierarchy and a MergeFile argument) while it tiers up.
        for (int round = 0; round < 100; round++)
        {
            for (int i = 0; i < 1000; i++)
            {
                MergeHierarchy hierarchy = new MergeHierarchy(MergeHierarchy.MergeMode.Manual, (inner, i));
                hierarchy.AddMergeHierarchyMember(new MergeFile($"file{i}"), offset: i);

                if (hierarchy.Children[0].Kind != ChildWithOffset.MemberKind.MergeHierarchy ||
                    hierarchy.Children[1].Kind != ChildWithOffset.MemberKind.MergeFile)
                {
                    Assert.Fail($"Wrong member kinds at round {round}, iteration {i}: " +
                                $"{hierarchy.Children[0].Kind}, {hierarchy.Children[1].Kind}");
                }
            }

            Thread.Sleep(1);
        }
    }
}
