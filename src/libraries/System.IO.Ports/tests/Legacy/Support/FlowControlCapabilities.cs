namespace Legacy.Support
{
    public class FlowControlCapabilities
    {
        public FlowControlCapabilities(int minimumBlockingByteCount, int hardwareTransmitBufferSize, bool hardwareWriteBlockingAvailable)
        {
            MinimumBlockingByteCount = minimumBlockingByteCount;
            HardwareTransmitBufferSize = hardwareTransmitBufferSize;
            HardwareWriteBlockingAvailable = hardwareWriteBlockingAvailable;
        }

        public int MinimumBlockingByteCount { get; }
        public int HardwareTransmitBufferSize { get; }
        public bool HardwareWriteBlockingAvailable { get; }

        public override string ToString()
        {
            return $"MinBlock: {MinimumBlockingByteCount}, HwBuffer: {HardwareTransmitBufferSize}, Flow Ctrl Available?: {HardwareWriteBlockingAvailable}";
        }
    }
}