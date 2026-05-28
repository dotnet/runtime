// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal class MockFrame : TypedView
{
    private const string IdentifierFieldName = "Identifier";
    private const string NextFieldName = "Next";

    public static Layout<MockFrame> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("Frame", architecture)
            .AddPointerField(IdentifierFieldName)
            .AddPointerField(NextFieldName)
            .Build<MockFrame>();

    public ulong Identifier
    {
        get => ReadPointerField(IdentifierFieldName);
        set => WritePointerField(IdentifierFieldName, value);
    }

    public ulong Next
    {
        get => ReadPointerField(NextFieldName);
        set => WritePointerField(NextFieldName, value);
    }
}

internal sealed class MockInlinedCallFrame : MockFrame
{
    // Field order mirrors src/coreclr/vm/frames.h InlinedCallFrame so that the cDAC
    // InlinedCallFrame data adapter can read the same field offsets in tests.
    private const string DatumFieldName = "Datum";
    private const string CallSiteSPFieldName = "CallSiteSP";
    private const string CallerReturnAddressFieldName = "CallerReturnAddress";
    private const string CalleeSavedFPFieldName = "CalleeSavedFP";

    public static Layout<MockInlinedCallFrame> CreateLayout(Layout<MockFrame> baseLayout)
        => new SequentialLayoutBuilder("InlinedCallFrame", baseLayout.Architecture, baseLayout)
            .AddPointerField(DatumFieldName)
            .AddPointerField(CallSiteSPFieldName)
            .AddPointerField(CallerReturnAddressFieldName)
            .AddPointerField(CalleeSavedFPFieldName)
            .Build<MockInlinedCallFrame>();

    public ulong Datum
    {
        get => ReadPointerField(DatumFieldName);
        set => WritePointerField(DatumFieldName, value);
    }

    public ulong CallerReturnAddress
    {
        get => ReadPointerField(CallerReturnAddressFieldName);
        set => WritePointerField(CallerReturnAddressFieldName, value);
    }
}

internal sealed class MockFramedMethodFrame : MockFrame
{
    private const string TransitionBlockPtrFieldName = "TransitionBlockPtr";
    private const string MethodDescPtrFieldName = "MethodDescPtr";

    public static Layout<MockFramedMethodFrame> CreateLayout(Layout<MockFrame> baseLayout)
        => new SequentialLayoutBuilder("FramedMethodFrame", baseLayout.Architecture, baseLayout)
            .AddPointerField(TransitionBlockPtrFieldName)
            .AddPointerField(MethodDescPtrFieldName)
            .Build<MockFramedMethodFrame>();

    public ulong MethodDescPtr
    {
        get => ReadPointerField(MethodDescPtrFieldName);
        set => WritePointerField(MethodDescPtrFieldName, value);
    }
}

internal sealed class MockFuncEvalFrame : MockFrame
{
    // Mirrors the cDAC FuncEvalFrame data class which only reads DebuggerEvalPtr.
    // Identifier/Next are inherited from the base MockFrame layout so the
    // FrameIterator.Next() chain walk works the same as for a plain Frame.
    private const string DebuggerEvalPtrFieldName = "DebuggerEvalPtr";

    public static Layout<MockFuncEvalFrame> CreateLayout(Layout<MockFrame> baseLayout)
        => new SequentialLayoutBuilder("FuncEvalFrame", baseLayout.Architecture, baseLayout)
            .AddPointerField(DebuggerEvalPtrFieldName)
            .Build<MockFuncEvalFrame>();

    public ulong DebuggerEvalPtr
    {
        get => ReadPointerField(DebuggerEvalPtrFieldName);
        set => WritePointerField(DebuggerEvalPtrFieldName, value);
    }
}

internal sealed class MockDebuggerEval : TypedView
{
    // Field order mirrors the cDAC DebuggerEval data class reads: TargetContext (only
    // its address is taken, so a pointer-sized placeholder is sufficient), then the
    // EvalUsesHijack/MethodToken/AssemblyPtr fields read by GetDebuggerEvalData.
    private const string TargetContextFieldName = "TargetContext";
    private const string EvalUsesHijackFieldName = "EvalUsesHijack";
    private const string MethodTokenFieldName = "MethodToken";
    private const string AssemblyPtrFieldName = "AssemblyPtr";

    public static Layout<MockDebuggerEval> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("DebuggerEval", architecture)
            .AddPointerField(TargetContextFieldName)
            .AddByteField(EvalUsesHijackFieldName)
            .AddUInt32Field(MethodTokenFieldName)
            .AddPointerField(AssemblyPtrFieldName)
            .Build<MockDebuggerEval>();

    public uint MethodToken
    {
        get => ReadUInt32Field(MethodTokenFieldName);
        set => WriteUInt32Field(MethodTokenFieldName, value);
    }

    public ulong AssemblyPtr
    {
        get => ReadPointerField(AssemblyPtrFieldName);
        set => WritePointerField(AssemblyPtrFieldName, value);
    }
}

/// <summary>
/// Helper for building a Frame chain in the mock target memory. The Frame chain is
/// terminated with the FRAME_TOP sentinel (~0 sized to the target pointer width).
/// Identifier values for each frame type are arbitrary distinct pointer-sized values
/// registered as globals (e.g. <c>InlinedCallFrameIdentifier</c>) so that the cDAC
/// FrameIterator's pointer-based identification logic resolves them correctly.
/// </summary>
internal sealed class MockFrameBuilder
{
    private const ulong DefaultAllocationRangeStart = 0x0007_0000;
    private const ulong DefaultAllocationRangeEnd = 0x0008_0000;

    // Arbitrary, unique identifier values per frame kind. Their actual numeric value
    // is irrelevant - they only need to be distinct, non-null, and registered as the
    // matching <FrameType>Identifier global.
    public const ulong InlinedCallFrameIdentifierValue = 0x0001_F001;
    public const ulong FramedMethodFrameIdentifierValue = 0x0001_F002;
    public const ulong FuncEvalFrameIdentifierValue = 0x0001_F003;
    public const ulong DebuggerExitFrameIdentifierValue = 0x0001_F004;
    public const ulong PrestubMethodFrameIdentifierValue = 0x0001_F005;
    public const ulong DebuggerClassInitMarkFrameIdentifierValue = 0x0001_F006;
    public const ulong SoftwareExceptionFrameIdentifierValue = 0x0001_F007;
    public const ulong DebuggerU2MCatchHandlerFrameIdentifierValue = 0x0001_F008;
    public const ulong InterpreterFrameIdentifierValue = 0x0001_F009;
    public const ulong HijackFrameIdentifierValue = 0x0001_F00A;

    private readonly MockMemorySpace.Builder _builder;
    private readonly MockMemorySpace.BumpAllocator _allocator;
    private readonly TargetTestHelpers _helpers;
    private readonly ulong _terminator;

    public Layout<MockFrame> FrameLayout { get; }
    public Layout<MockInlinedCallFrame> InlinedCallFrameLayout { get; }
    public Layout<MockFramedMethodFrame> FramedMethodFrameLayout { get; }
    public Layout<MockFuncEvalFrame> FuncEvalFrameLayout { get; }
    public Layout<MockDebuggerEval> DebuggerEvalLayout { get; }

    public MockFrameBuilder(MockMemorySpace.Builder builder)
        : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
    {
    }

    public MockFrameBuilder(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange)
    {
        _builder = builder;
        _helpers = builder.TargetTestHelpers;
        _allocator = _builder.CreateAllocator(allocationRange.Start, allocationRange.End);
        _terminator = _helpers.Arch.Is64Bit ? ulong.MaxValue : uint.MaxValue;

        FrameLayout = MockFrame.CreateLayout(_helpers.Arch);
        InlinedCallFrameLayout = MockInlinedCallFrame.CreateLayout(FrameLayout);
        FramedMethodFrameLayout = MockFramedMethodFrame.CreateLayout(FrameLayout);
        FuncEvalFrameLayout = MockFuncEvalFrame.CreateLayout(FrameLayout);
        DebuggerEvalLayout = MockDebuggerEval.CreateLayout(_helpers.Arch);
    }

    public ulong FrameTopTerminator => _terminator;

    /// <summary>
    /// Allocates a basic Frame with the given identifier and returns its address.
    /// Used for frame kinds whose internal frame type classification only depends on
    /// the identifier (i.e. that the cDAC does not also read additional fields from).
    /// </summary>
    public MockFrame AddFrame(ulong identifierValue, string allocName)
    {
        MockFrame frame = FrameLayout.Create(_allocator.Allocate((ulong)FrameLayout.Size, allocName));
        frame.Identifier = identifierValue;
        frame.Next = _terminator;
        return frame;
    }

    /// <summary>
    /// Allocates an InlinedCallFrame. <paramref name="callerReturnAddress"/> set non-zero
    /// makes the frame "active" (matching native InlinedCallFrame::HasActiveCall).
    /// </summary>
    public MockInlinedCallFrame AddInlinedCallFrame(ulong callerReturnAddress, ulong datum)
    {
        MockInlinedCallFrame frame = InlinedCallFrameLayout.Create(_allocator.Allocate((ulong)InlinedCallFrameLayout.Size, "InlinedCallFrame"));
        frame.Identifier = InlinedCallFrameIdentifierValue;
        frame.Next = _terminator;
        frame.CallerReturnAddress = callerReturnAddress;
        frame.Datum = datum;
        return frame;
    }

    public MockFramedMethodFrame AddFramedMethodFrame(ulong methodDescPtr)
    {
        MockFramedMethodFrame frame = FramedMethodFrameLayout.Create(_allocator.Allocate((ulong)FramedMethodFrameLayout.Size, "FramedMethodFrame"));
        frame.Identifier = FramedMethodFrameIdentifierValue;
        frame.Next = _terminator;
        frame.MethodDescPtr = methodDescPtr;
        return frame;
    }

    /// <summary>
    /// Allocates a DebuggerEval object in mock memory. The mock layout only includes
    /// the fields the cDAC DebuggerEval data class reads.
    /// </summary>
    public MockDebuggerEval AddDebuggerEval(uint methodToken, ulong assemblyPtr)
    {
        MockDebuggerEval eval = DebuggerEvalLayout.Create(_allocator.Allocate((ulong)DebuggerEvalLayout.Size, "DebuggerEval"));
        eval.MethodToken = methodToken;
        eval.AssemblyPtr = assemblyPtr;
        return eval;
    }

    /// <summary>
    /// Allocates a FuncEvalFrame whose DebuggerEvalPtr field points at the given
    /// DebuggerEval address.
    /// </summary>
    public MockFuncEvalFrame AddFuncEvalFrame(ulong debuggerEvalPtr)
    {
        MockFuncEvalFrame frame = FuncEvalFrameLayout.Create(_allocator.Allocate((ulong)FuncEvalFrameLayout.Size, "FuncEvalFrame"));
        frame.Identifier = FuncEvalFrameIdentifierValue;
        frame.Next = _terminator;
        frame.DebuggerEvalPtr = debuggerEvalPtr;
        return frame;
    }

    /// <summary>
    /// Builds a singly-linked frame chain from <paramref name="frames"/> (head first)
    /// by writing each frame's Next pointer to the address of the following frame and
    /// terminating the last with FRAME_TOP. Returns the head address (or the terminator
    /// for an empty chain).
    /// </summary>
    public ulong LinkChain(params ulong[] frameAddresses)
    {
        if (frameAddresses.Length == 0)
            return _terminator;

        for (int i = 0; i < frameAddresses.Length - 1; i++)
        {
            // The Next field is the second pointer-sized slot in every frame layout
            // because all our mock frame layouts start with Identifier then Next.
            ulong nextFieldAddress = frameAddresses[i] + (ulong)_helpers.PointerSize;
            _helpers.WritePointer(_builder.BorrowAddressRangeMemory(nextFieldAddress, _helpers.PointerSize).Span, frameAddresses[i + 1]);
        }

        ulong lastNextField = frameAddresses[^1] + (ulong)_helpers.PointerSize;
        _helpers.WritePointer(_builder.BorrowAddressRangeMemory(lastNextField, _helpers.PointerSize).Span, _terminator);

        return frameAddresses[0];
    }
}
