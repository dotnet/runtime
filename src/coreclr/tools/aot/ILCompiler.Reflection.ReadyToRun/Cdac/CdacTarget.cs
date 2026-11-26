// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;

// The GC info decoders under src/native/managed/cdac are compiled into this assembly as shared
// source (see ILCompiler.Reflection.ReadyToRun.csproj). They are written against the cDAC
// Target abstraction; this file supplies the minimal subset of that abstraction they use.
// Member signatures must match Microsoft.Diagnostics.DataContractReader.Target so the shared
// sources compile unchanged.

namespace Microsoft.Diagnostics.DataContractReader
{
    /// <summary>
    /// Minimal stand-in for the cDAC <c>Target</c> used by the shared GC info decoders.
    /// </summary>
    public abstract class Target
    {
        public abstract int PointerSize { get; }

        public abstract bool IsLittleEndian { get; }

        public abstract T Read<T>(ulong address) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>;

        public abstract TargetPointer ReadPointer(ulong address);

        public abstract void ReadBuffer(ulong address, Span<byte> buffer);

        internal abstract TargetContracts Contracts { get; }
    }

    internal sealed class TargetContracts(IRuntimeInfo runtimeInfo)
    {
        public IRuntimeInfo RuntimeInfo { get; } = runtimeInfo;
    }

    /// <summary>
    /// Stand-in for the cDAC <c>TargetStream</c>. The cDAC GC info decoder requests an arbitrary
    /// fixed-size window; R2RDump decodes the entire blob, which can exceed it, so the stream
    /// instead extends to the end of the image.
    /// </summary>
    internal sealed class TargetStream(Target target, ulong startPosition, long size) : System.IO.Stream
    {
        private readonly ulong _startPosition = startPosition;
        private long _offset;
        private readonly long _size = target is ILCompiler.Reflection.ReadyToRun.Cdac.ImageTarget image
            ? Math.Max(0, image.Length - (long)startPosition)
            : size;
        private readonly Target _target = target;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _size;

        public override long Position { get => _offset; set => _offset = value; }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            _target.ReadBuffer(_startPosition + (ulong)_offset, buffer);
            _offset += buffer.Length;
            return buffer.Length;
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            _offset = origin switch
            {
                System.IO.SeekOrigin.Begin => offset,
                System.IO.SeekOrigin.Current => _offset + offset,
                _ => throw new NotSupportedException(),
            };
            return _offset;
        }

        public override void Flush() { }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

namespace Microsoft.Diagnostics.DataContractReader.Contracts
{
    // Mirrors GenericContextLoc in Microsoft.Diagnostics.DataContractReader.Abstractions/Contracts/IRuntimeTypeSystem.cs.
    public enum GenericContextLoc
    {
        None,
        InstArgMethodDesc,
        InstArgMethodTable,
        ThisPtr,
    }
}

namespace ILCompiler.Reflection.ReadyToRun.Cdac
{
    using Microsoft.Diagnostics.DataContractReader;

    /// <summary>
    /// Exposes a ReadyToRun image to the shared GC info decoders. Addresses are image offsets.
    /// </summary>
    internal sealed class ImageTarget : Target
    {
        private readonly NativeReader _imageReader;
        private readonly TargetContracts _contracts;

        public ImageTarget(NativeReader imageReader, Machine machine)
        {
            _imageReader = imageReader;
            RuntimeInfoArchitecture architecture = GetArchitecture(machine);
            PointerSize = architecture is RuntimeInfoArchitecture.X86 or RuntimeInfoArchitecture.Arm or RuntimeInfoArchitecture.Wasm ? 4 : 8;
            _contracts = new TargetContracts(new ImageRuntimeInfo(architecture));
        }

        public override int PointerSize { get; }

        public override bool IsLittleEndian => true;

        public long Length => _imageReader.Length;

        internal override TargetContracts Contracts => _contracts;

        /// <summary>
        /// One past the highest image offset read so far. The shared decoders read GC info a
        /// byte at a time, so this is the end of the encoded data once decoding is complete.
        /// </summary>
        public ulong ReadEnd { get; private set; }

        public override T Read<T>(ulong address)
        {
            Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<T>()];
            ReadBuffer(address, buffer);
            return T.ReadLittleEndian(buffer, isUnsigned: T.IsZero(T.MinValue));
        }

        public override TargetPointer ReadPointer(ulong address)
        {
            Span<byte> buffer = stackalloc byte[PointerSize];
            ReadBuffer(address, buffer);
            return new TargetPointer(PointerSize == 4 ? BinaryPrimitives.ReadUInt32LittleEndian(buffer) : BinaryPrimitives.ReadUInt64LittleEndian(buffer));
        }

        public override void ReadBuffer(ulong address, Span<byte> buffer)
        {
            if (address > int.MaxValue)
                throw new BadImageFormatException($"GC info read at 0x{address:X} is outside the image.");

            int start = (int)address;
            _imageReader.ReadSpanAt(ref start, buffer);
            ReadEnd = Math.Max(ReadEnd, (ulong)start);
        }

        internal static RuntimeInfoArchitecture GetArchitecture(Machine machine) => machine switch
        {
            Machine.Amd64 => RuntimeInfoArchitecture.X64,
            Machine.I386 => RuntimeInfoArchitecture.X86,
            Machine.ArmThumb2 => RuntimeInfoArchitecture.Arm,
            Machine.Arm64 => RuntimeInfoArchitecture.Arm64,
            Machine.LoongArch64 => RuntimeInfoArchitecture.LoongArch64,
            Machine.RiscV64 => RuntimeInfoArchitecture.RiscV64,
            WasmMachine.Wasm32 => RuntimeInfoArchitecture.Wasm,
            _ => throw new NotImplementedException(machine.ToString()),
        };

        private sealed class ImageRuntimeInfo(RuntimeInfoArchitecture architecture) : IRuntimeInfo
        {
            public RuntimeInfoArchitecture GetTargetArchitecture() => architecture;
        }
    }
}
