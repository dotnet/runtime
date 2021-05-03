// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Runtime.Serialization
{
    // needs to be a separate class so it can get its own namespace
    [DataContract(Name = nameof(MarshalByRefObject), Namespace = Globals.DataContractXsdBaseNamespace + "System")]
    internal abstract class MarshalByRefObjectAdapter
    {
        // never used, but must be first in the hierarchy in order to maintain NetFX compat
        [DataMember(Name = "__identity", Order = 0)]
        public object? Identity { get { return null; } set { } }
    }

    /// <summary>
    /// Members on this type correspond to the fields in MemoryStream's hierarchy on NetFX.
    /// </summary>
    [DataContract(Name = "MemoryStream", Namespace = Globals.DataContractXsdBaseNamespace + "System.IO")]
    internal sealed class MemoryStreamAdapter : MarshalByRefObjectAdapter
    {
        [DataMember(Name = "_buffer", Order = 1)]
        public byte[]? Buffer { get; set; }

        [DataMember(Name = "_capacity", Order = 2)]
        public int Capacity { get; set; }

        [DataMember(Name = "_expandable", Order = 3)]
        public bool Expandable { get; set; }

        [DataMember(Name = "_exposable", Order = 4)]
        public bool Exposable { get; set; }

        [DataMember(Name = "_isOpen", Order = 5)]
        public bool IsOpen { get; set; }

        [DataMember(Name = "_length", Order = 6)]
        public int Length { get; set; }

        [DataMember(Name = "_origin", Order = 7)]
        public int Origin { get; set; }

        [DataMember(Name = "_position", Order = 8)]
        public int Position { get; set; }

        [DataMember(Name = "_writable", Order = 9)]
        public bool Writable { get; set; }

        public static MemoryStream GetMemoryStream(MemoryStreamAdapter value)
        {
            // The input may be coming from an untrusted payload, so we perform a few extra
            // checks. We'll slice the buffer ourselves to validate that origin and length are
            // accurate, creating a new buffer that doesn't have any leading or trailing data.
            // The AsSpan method performs argument validation on our behalf. We'll also validate
            // that the desired Position is not beyond the end of the buffer, otherwise subsequent
            // accesses to the buffer may cause unintended allocations. Some properties (Capacity,
            // IsOpen, etc.) are ignored since we allow the MemoryStream ctor to set them itself.

            byte[] buffer = value.Buffer!; // we don't expect this to be null, but if it is we'll throw NRE below
            Span<byte> slicedBuffer = value.Buffer.AsSpan(value.Origin, value.Length - value.Origin);
            if (slicedBuffer.Length < buffer.Length)
            {
                buffer = slicedBuffer.ToArray(); // trim leading and trailing data
            }

            MemoryStream memoryStream = new MemoryStream(
                buffer: buffer,
                index: 0,
                count: buffer.Length,
                writable: value.Writable,
                publiclyVisible: value.Exposable);

            int desiredPosition = value.Position - value.Origin;
            if (desiredPosition < 0 || desiredPosition > memoryStream.Length)
            {
                throw new InvalidOperationException();
            }
            memoryStream.Position = desiredPosition;

            return memoryStream;
        }

        public static MemoryStreamAdapter GetMemoryStreamAdapter(MemoryStream memoryStream)
        {
            MemoryStreamAdapter adapter = new MemoryStreamAdapter();

            // If the MemoryStream's inner buffer is visible, mark it as such in the proxy object.
            // We'll also try to avoid the copy of the MemoryStream's inner buffer, but we can
            // only do this if there's no leading or trailing data surrounding the real to-be-serialized
            // segment.

            if (memoryStream.TryGetBuffer(out ArraySegment<byte> innerBuffer))
            {
                adapter.Exposable = true;
                if (innerBuffer.Count == innerBuffer.Array!.Length)
                {
                    adapter.Buffer = innerBuffer.Array;
                }
            }

            adapter.Buffer ??= memoryStream.ToArray(); // couldn't get the inner buffer; clone it now

            // Copy additional information to the proxy object

            adapter.Length = checked((int)memoryStream.Length);
            adapter.Capacity = memoryStream.Capacity;
            adapter.Position = checked((int)memoryStream.Position);
            adapter.Writable = memoryStream.CanWrite;
            adapter.Origin = 0; // Length, Capacity, and Position are already offset appropriately

            // Properties below are needed for Full Framework back-compat.

            adapter.Expandable = false; // we have no way of knowing this, so default it to false
            adapter.IsOpen = true; // we know this is true because the earlier prop accessors didn't throw

            return adapter;
        }
    }
}
