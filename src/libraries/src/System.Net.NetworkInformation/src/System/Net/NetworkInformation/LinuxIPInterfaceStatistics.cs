// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// IPInterfaceStatistics provider for Linux. 
    /// Reads information out of /proc/net/dev and other locations.
    /// </summary>
    internal class LinuxIPInterfaceStatistics : IPInterfaceStatistics
    {
        // /proc/net/dev
        // Receive section
        private uint _bytesReceived;
        private uint _packetsReceived;
        private uint _errorsReceived;
        private uint _incomingPacketsDropped;
        private uint _fifoBufferErrorsReceived;
        private uint _packetFramingErrorsReceived;
        private uint _compressedPacketsReceived;
        private uint _multicastFramesReceived;

        // Transmit section
        private uint _bytesTransmitted;
        private uint _packetsTransmitted;
        private uint _errorsTransmitted;
        private uint _outgoingPacketsDropped;
        private uint _fifoBufferErrorsTransmitted;
        private uint _collisionsDetected;
        private uint _carrierLosses;
        private uint _compressedPacketsTransmitted;

        // From /sys/class/net/<interface>/tx_queue_len
        private int _transmitQueueLength;

        public LinuxIPInterfaceStatistics(string name)
        {
            ParseTable(name);
            string transmitQueueLengthFilePath = Path.Combine(NetworkFiles.SysClassNetFolder, name, NetworkFiles.TransmitQueueLengthFileName);
            _transmitQueueLength = int.Parse(File.ReadAllText(transmitQueueLengthFilePath));
        }

        private void ParseTable(string name)
        {
            /* NOTE: This same information can be obtained using rtnetlink:
                 RTM_GETLINK:
                   Get information about a specific network interface
                   rta_type:IFLA_STATS contains all of the below information.
                   https://github.com/torvalds/linux/blob/dd5cdb48edfd34401799056a9acf61078d773f90/include/uapi/linux/if_link.h#L41
                   There does not appear to be any additional information exposed there that is not exposed in /proc/net/dev
            */

            using (StreamReader sr = File.OpenText(NetworkFiles.InterfaceListingFile))
            {
                sr.ReadLine();
                sr.ReadLine();
                int index = 0;
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (line.Contains(name))
                    {
                        ParseLine(line);
                        return;
                    }
                    index += 1;
                }

                throw new NetworkInformationException("Reached the end of the file. Interface name " + name + " was invalid.");
            }
        }

        private void ParseLine(string line)
        {
            string[] pieces = line.Split(new char[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);

            _bytesReceived = uint.Parse(pieces[1]);
            _packetsReceived = uint.Parse(pieces[2]);
            _errorsReceived = uint.Parse(pieces[3]);
            _incomingPacketsDropped = uint.Parse(pieces[4]);
            _fifoBufferErrorsReceived = uint.Parse(pieces[5]);
            _packetFramingErrorsReceived = uint.Parse(pieces[6]);
            _compressedPacketsReceived = uint.Parse(pieces[7]);
            _multicastFramesReceived = uint.Parse(pieces[8]);

            _bytesTransmitted = uint.Parse(pieces[9]);
            _packetsTransmitted = uint.Parse(pieces[10]);
            _errorsTransmitted = uint.Parse(pieces[11]);
            _outgoingPacketsDropped = uint.Parse(pieces[12]);
            _fifoBufferErrorsTransmitted = uint.Parse(pieces[13]);
            _collisionsDetected = uint.Parse(pieces[14]);
            _carrierLosses = uint.Parse(pieces[15]);
            _compressedPacketsTransmitted = uint.Parse(pieces[16]);
        }

        public override long BytesReceived { get { return _bytesReceived; } }

        public override long BytesSent { get { return _bytesTransmitted; } }

        public override long IncomingPacketsDiscarded { get { return _incomingPacketsDropped; } }

        public override long IncomingPacketsWithErrors { get { return _errorsReceived; } }

        public override long IncomingUnknownProtocolPackets { get { throw new PlatformNotSupportedException(); } }

        public override long NonUnicastPacketsReceived { get { return _multicastFramesReceived; } }

        public override long NonUnicastPacketsSent { get { throw new PlatformNotSupportedException(); } }

        public override long OutgoingPacketsDiscarded { get { return _outgoingPacketsDropped; } }

        public override long OutgoingPacketsWithErrors { get { return _errorsTransmitted; } }

        public override long OutputQueueLength { get { return _transmitQueueLength; } }

        public override long UnicastPacketsReceived { get { return _packetsReceived; } }

        public override long UnicastPacketsSent { get { return _packetsTransmitted; } }
    }
}
