// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.IO.MemoryMappedFiles
{
    internal sealed unsafe class MemoryMappedFileMemoryManager : MemoryManager<byte>
    {
        private byte* _pointer;
        private int _length;
        private MemoryMappedFile _mappedFile;
        private MemoryMappedViewAccessor _accessor;

        public MemoryMappedFileMemoryManager(
            byte* pointer,
            int length,
            MemoryMappedFile mappedFile,
            MemoryMappedViewAccessor accessor)
        {
            _pointer = pointer;
            _length = length;
            _mappedFile = mappedFile;
            _accessor = accessor;
        }

#if DEBUG
#pragma warning disable CA2015
        ~MemoryMappedFileMemoryManager()
#pragma warning restore CA2015
        {
            Environment.FailFast("MemoryMappedFileMemoryManager was finalized.");
        }
#endif

        internal static MemoryMappedFileMemoryManager CreateFromFileClamped(
            FileStream fileStream,
            MemoryMappedFileAccess access = MemoryMappedFileAccess.Read,
            HandleInheritability inheritability = HandleInheritability.None,
            bool leaveOpen = false)
        {
            int length = (int)Math.Min(int.MaxValue, fileStream.Length);
            MemoryMappedFile mapped = MemoryMappedFile.CreateFromFile(fileStream, null, 0, access, inheritability, leaveOpen);
            MemoryMappedViewAccessor? accessor = null;
            byte* pointer = null;

            try
            {
                accessor = mapped.CreateViewAccessor(0, length, access);
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);

                return new MemoryMappedFileMemoryManager(pointer, length, mapped, accessor);
            }
            catch (Exception)
            {
                if (pointer != null)
                {
                    accessor!.SafeMemoryMappedViewHandle.ReleasePointer();
                }

                accessor?.Dispose();
                mapped.Dispose();
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            _pointer = null;
            _length = -1;
            _accessor?.SafeMemoryMappedViewHandle.ReleasePointer();
            _accessor?.Dispose();
            _mappedFile?.Dispose();
            _accessor = null!;
            _mappedFile = null!;
        }

        public override Span<byte> GetSpan()
        {
            ThrowIfDisposed();
            return new Span<byte>(_pointer, _length);
        }

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            ThrowIfDisposed();
            return default;
        }

        public override void Unpin()
        {
            ThrowIfDisposed();
            // nop
        }

        private void ThrowIfDisposed()
        {
#if NET
            ObjectDisposedException.ThrowIf(_length < 0, this);
#else
            if (_length < 0)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
#endif
        }
    }
}
