// This is auto generated file. Please don’t modify manually.
// The file is generated as part of the build through the ResourceGenerator tool 
// which takes the project resx resource file and generated this source code file.
// By default the tool will use Resources\Strings.resx but projects can customize
// that by overriding the StringResourcesPath property group.
namespace System
{
    internal static partial class SR
    {
#pragma warning disable 0414
        private const string s_resourcesName = "System.IO.UnmanagedMemoryStream.resources"; // assembly Name + .resources
#pragma warning restore 0414

#if !DEBUGRESOURCES
        internal static string Arg_BufferTooSmall {
              get { return SR.GetResourceString("Arg_BufferTooSmall", null); }
        }
        internal static string Argument_InvalidOffLen {
              get { return SR.GetResourceString("Argument_InvalidOffLen", null); }
        }
        internal static string Argument_InvalidSafeBufferOffLen {
              get { return SR.GetResourceString("Argument_InvalidSafeBufferOffLen", null); }
        }
        internal static string Argument_InvalidSeekOrigin {
              get { return SR.GetResourceString("Argument_InvalidSeekOrigin", null); }
        }
        internal static string Argument_NotEnoughBytesToRead {
              get { return SR.GetResourceString("Argument_NotEnoughBytesToRead", null); }
        }
        internal static string Argument_NotEnoughBytesToWrite {
              get { return SR.GetResourceString("Argument_NotEnoughBytesToWrite", null); }
        }
        internal static string Argument_OffsetAndCapacityOutOfBounds {
              get { return SR.GetResourceString("Argument_OffsetAndCapacityOutOfBounds", null); }
        }
        internal static string Argument_OffsetAndLengthOutOfBounds {
              get { return SR.GetResourceString("Argument_OffsetAndLengthOutOfBounds", null); }
        }
        internal static string Argument_UnmanagedMemAccessorWrapAround {
              get { return SR.GetResourceString("Argument_UnmanagedMemAccessorWrapAround", null); }
        }
        internal static string ArgumentNull_Buffer {
              get { return SR.GetResourceString("ArgumentNull_Buffer", null); }
        }
        internal static string ArgumentOutOfRange_Enum {
              get { return SR.GetResourceString("ArgumentOutOfRange_Enum", null); }
        }
        internal static string ArgumentOutOfRange_LengthGreaterThanCapacity {
              get { return SR.GetResourceString("ArgumentOutOfRange_LengthGreaterThanCapacity", null); }
        }
        internal static string ArgumentOutOfRange_NeedNonNegNum {
              get { return SR.GetResourceString("ArgumentOutOfRange_NeedNonNegNum", null); }
        }
        internal static string ArgumentOutOfRange_PositionLessThanCapacityRequired {
              get { return SR.GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired", null); }
        }
        internal static string ArgumentOutOfRange_StreamLength {
              get { return SR.GetResourceString("ArgumentOutOfRange_StreamLength", null); }
        }
        internal static string ArgumentOutOfRange_UnmanagedMemStreamLength {
              get { return SR.GetResourceString("ArgumentOutOfRange_UnmanagedMemStreamLength", null); }
        }
        internal static string ArgumentOutOfRange_UnmanagedMemStreamWrapAround {
              get { return SR.GetResourceString("ArgumentOutOfRange_UnmanagedMemStreamWrapAround", null); }
        }
        internal static string NotSupported_Reading {
              get { return SR.GetResourceString("NotSupported_Reading", null); }
        }
        internal static string NotSupported_UnreadableStream {
              get { return SR.GetResourceString("NotSupported_UnreadableStream", null); }
        }
        internal static string NotSupported_UnwritableStream {
              get { return SR.GetResourceString("NotSupported_UnwritableStream", null); }
        }
        internal static string NotSupported_UmsSafeBuffer {
              get { return SR.GetResourceString("NotSupported_UmsSafeBuffer", null); }
        }
        internal static string NotSupported_Writing {
              get { return SR.GetResourceString("NotSupported_Writing", null); }
        }
        internal static string IndexOutOfRange_UMSPosition {
              get { return SR.GetResourceString("IndexOutOfRange_UMSPosition", null); }
        }
        internal static string InvalidOperation_CalledTwice {
              get { return SR.GetResourceString("InvalidOperation_CalledTwice", null); }
        }
        internal static string ObjectDisposed_StreamIsClosed {
              get { return SR.GetResourceString("ObjectDisposed_StreamIsClosed", null); }
        }
        internal static string ObjectDisposed_ViewAccessorClosed {
              get { return SR.GetResourceString("ObjectDisposed_ViewAccessorClosed", null); }
        }
        internal static string IO_FixedCapacity {
              get { return SR.GetResourceString("IO_FixedCapacity", null); }
        }
        internal static string IO_SeekBeforeBegin {
              get { return SR.GetResourceString("IO_SeekBeforeBegin", null); }
        }
        internal static string IO_StreamTooLong {
              get { return SR.GetResourceString("IO_StreamTooLong", null); }
        }
#else
        internal static string Arg_BufferTooSmall {
              get { return SR.GetResourceString("Arg_BufferTooSmall", @"Not enough space available in the buffer."); }
        }
        internal static string Argument_InvalidOffLen {
              get { return SR.GetResourceString("Argument_InvalidOffLen", @"Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection."); }
        }
        internal static string Argument_InvalidSafeBufferOffLen {
              get { return SR.GetResourceString("Argument_InvalidSafeBufferOffLen", @"Offset and length were greater than the size of the SafeBuffer."); }
        }
        internal static string Argument_InvalidSeekOrigin {
              get { return SR.GetResourceString("Argument_InvalidSeekOrigin", @"Invalid seek origin."); }
        }
        internal static string Argument_NotEnoughBytesToRead {
              get { return SR.GetResourceString("Argument_NotEnoughBytesToRead", @"There are not enough bytes remaining in the accessor to read at this position."); }
        }
        internal static string Argument_NotEnoughBytesToWrite {
              get { return SR.GetResourceString("Argument_NotEnoughBytesToWrite", @"There are not enough bytes remaining in the accessor to write at this position."); }
        }
        internal static string Argument_OffsetAndCapacityOutOfBounds {
              get { return SR.GetResourceString("Argument_OffsetAndCapacityOutOfBounds", @"Offset and capacity were greater than the size of the view."); }
        }
        internal static string Argument_OffsetAndLengthOutOfBounds {
              get { return SR.GetResourceString("Argument_OffsetAndLengthOutOfBounds", @"Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection."); }
        }
        internal static string Argument_UnmanagedMemAccessorWrapAround {
              get { return SR.GetResourceString("Argument_UnmanagedMemAccessorWrapAround", @"The UnmanagedMemoryAccessor capacity and offset would wrap around the high end of the address space."); }
        }
        internal static string ArgumentNull_Buffer {
              get { return SR.GetResourceString("ArgumentNull_Buffer", @"Buffer cannot be null."); }
        }
        internal static string ArgumentOutOfRange_Enum {
              get { return SR.GetResourceString("ArgumentOutOfRange_Enum", @"Enum value was out of legal range."); }
        }
        internal static string ArgumentOutOfRange_LengthGreaterThanCapacity {
              get { return SR.GetResourceString("ArgumentOutOfRange_LengthGreaterThanCapacity", @"The length cannot be greater than the capacity."); }
        }
        internal static string ArgumentOutOfRange_NeedNonNegNum {
              get { return SR.GetResourceString("ArgumentOutOfRange_NeedNonNegNum", @"Non negative number is required."); }
        }
        internal static string ArgumentOutOfRange_PositionLessThanCapacityRequired {
              get { return SR.GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired", @"The position may not be greater or equal to the capacity of the accessor."); }
        }
        internal static string ArgumentOutOfRange_StreamLength {
              get { return SR.GetResourceString("ArgumentOutOfRange_StreamLength", @"Stream length must be non-negative and less than 2^31 - 1 - origin."); }
        }
        internal static string ArgumentOutOfRange_UnmanagedMemStreamLength {
              get { return SR.GetResourceString("ArgumentOutOfRange_UnmanagedMemStreamLength", @"UnmanagedMemoryStream length must be non-negative and less than 2^63 - 1 - baseAddress."); }
        }
        internal static string ArgumentOutOfRange_UnmanagedMemStreamWrapAround {
              get { return SR.GetResourceString("ArgumentOutOfRange_UnmanagedMemStreamWrapAround", @"The UnmanagedMemoryStream capacity would wrap around the high end of the address space."); }
        }
        internal static string NotSupported_Reading {
              get { return SR.GetResourceString("NotSupported_Reading", @"Accessor does not support reading."); }
        }
        internal static string NotSupported_UnreadableStream {
              get { return SR.GetResourceString("NotSupported_UnreadableStream", @"Stream does not support reading."); }
        }
        internal static string NotSupported_UnwritableStream {
              get { return SR.GetResourceString("NotSupported_UnwritableStream", @"Stream does not support writing."); }
        }
        internal static string NotSupported_UmsSafeBuffer {
              get { return SR.GetResourceString("NotSupported_UmsSafeBuffer", @"This operation is not supported for an UnmanagedMemoryStream created from a SafeBuffer."); }
        }
        internal static string NotSupported_Writing {
              get { return SR.GetResourceString("NotSupported_Writing", @"Accessor does not support writing."); }
        }
        internal static string IndexOutOfRange_UMSPosition {
              get { return SR.GetResourceString("IndexOutOfRange_UMSPosition", @"Unmanaged memory stream position was beyond the capacity of the stream."); }
        }
        internal static string InvalidOperation_CalledTwice {
              get { return SR.GetResourceString("InvalidOperation_CalledTwice", @"The method cannot be called twice on the same instance."); }
        }
        internal static string ObjectDisposed_StreamIsClosed {
              get { return SR.GetResourceString("ObjectDisposed_StreamIsClosed", @"Cannot access a closed Stream."); }
        }
        internal static string ObjectDisposed_ViewAccessorClosed {
              get { return SR.GetResourceString("ObjectDisposed_ViewAccessorClosed", @"Cannot access a closed accessor."); }
        }
        internal static string IO_FixedCapacity {
              get { return SR.GetResourceString("IO_FixedCapacity", @"Unable to expand length of this stream beyond its capacity."); }
        }
        internal static string IO_SeekBeforeBegin {
              get { return SR.GetResourceString("IO_SeekBeforeBegin", @"An attempt was made to move the position before the beginning of the stream."); }
        }
        internal static string IO_StreamTooLong {
              get { return SR.GetResourceString("IO_StreamTooLong", @"Stream was too long."); }
        }

#endif
    }
}
