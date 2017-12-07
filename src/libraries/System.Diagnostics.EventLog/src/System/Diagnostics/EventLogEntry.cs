// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Win32;

namespace System.Diagnostics
{
    [
    ToolboxItem(false),
    DesignTimeVisible(false),
    ]
    public sealed class EventLogEntry : Component, ISerializable
    {
        internal byte[] dataBuf;
        internal int bufOffset;
        private EventLogInternal owner;
        private string category;
        private string message;

        internal EventLogEntry(byte[] buf, int offset, EventLogInternal log)
        {
            this.dataBuf = buf;
            this.bufOffset = offset;
            this.owner = log;

            GC.SuppressFinalize(this);
        }

        private EventLogEntry(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// The machine on which this event log resides.
        /// </summary>
        public string MachineName
        {
            get
            {
                // first skip over the source name
                int pos = bufOffset + FieldOffsets.RAWDATA;
                while (CharFrom(dataBuf, pos) != '\0')
                    pos += 2;
                pos += 2;
                char ch = CharFrom(dataBuf, pos);
                StringBuilder buf = new StringBuilder();
                while (ch != '\0')
                {
                    buf.Append(ch);
                    pos += 2;
                    ch = CharFrom(dataBuf, pos);
                }

                return buf.ToString();
            }
        }

        /// <summary>
        /// The binary data associated with this entry in the event log.
        /// </summary>
        public byte[] Data
        {
            get
            {
                int dataLen = IntFrom(dataBuf, bufOffset + FieldOffsets.DATALENGTH);
                byte[] data = new byte[dataLen];
                Array.Copy(dataBuf, bufOffset + IntFrom(dataBuf, bufOffset + FieldOffsets.DATAOFFSET),
                           data, 0, dataLen);
                return data;
            }
        }

        /// <summary>
        /// The sequence of this entry in the event log.
        /// </summary>
        public int Index
        {
            get
            {
                return IntFrom(dataBuf, bufOffset + FieldOffsets.RECORDNUMBER);
            }
        }

        /// <summary>
        /// The category for this message.
        /// </summary>
        public string Category
        {
            get
            {
                if (category == null)
                {
                    string dllName = GetMessageLibraryNames("CategoryMessageFile");
                    string cat = owner.FormatMessageWrapper(dllName, (uint)CategoryNumber, null);
                    if (cat == null)
                        category = "(" + CategoryNumber.ToString(CultureInfo.CurrentCulture) + ")";
                    else
                        category = cat;
                }

                return category;
            }
        }

        /// <summary>
        /// An application-specific category number assigned to this entry.
        /// </summary>
        public short CategoryNumber
        {
            get
            {
                return ShortFrom(dataBuf, bufOffset + FieldOffsets.EVENTCATEGORY);
            }
        }

        /// <summary>
        /// The number identifying the message for this source.
        /// </summary>
        [Obsolete("This property has been deprecated.  Please use System.Diagnostics.EventLogEntry.InstanceId instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public int EventID
        {
            get
            {
                return IntFrom(dataBuf, bufOffset + FieldOffsets.EVENTID) & 0x3FFFFFFF;
            }
        }

        /// <summary>
        /// The type of entry - Information, Warning, etc.
        /// </summary>
        public EventLogEntryType EntryType
        {
            get
            {
                return (EventLogEntryType)ShortFrom(dataBuf, bufOffset + FieldOffsets.EVENTTYPE);
            }
        }

        /// <summary>
        /// The text of the message for this entry.
        /// </summary>
        public string Message
        {
            get
            {
                if (message == null)
                {
                    string dllNames = GetMessageLibraryNames("EventMessageFile");
                    int msgId = IntFrom(dataBuf, bufOffset + FieldOffsets.EVENTID);
                    string msg = owner.FormatMessageWrapper(dllNames, (uint)msgId, ReplacementStrings);
                    if (msg == null)
                    {
                        StringBuilder msgBuf = new StringBuilder(SR.Format(SR.MessageNotFormatted, msgId, Source));
                        string[] strings = ReplacementStrings;
                        for (int i = 0; i < strings.Length; i++)
                        {
                            if (i != 0)
                                msgBuf.Append(", ");
                            msgBuf.Append("'");
                            msgBuf.Append(strings[i]);
                            msgBuf.Append("'");
                        }

                        msg = msgBuf.ToString();
                    }
                    else
                        msg = ReplaceMessageParameters(msg, ReplacementStrings);

                    message = msg;
                }

                return message;
            }
        }

        /// <summary>
        /// The name of the application that wrote this entry.
        /// </summary>
        public string Source
        {
            get
            {
                StringBuilder buf = new StringBuilder();
                int pos = bufOffset + FieldOffsets.RAWDATA;

                char ch = CharFrom(dataBuf, pos);
                while (ch != '\0')
                {
                    buf.Append(ch);
                    pos += 2;
                    ch = CharFrom(dataBuf, pos);
                }

                return buf.ToString();
            }
        }

        /// <summary>
        /// The application-supplied strings used in the message.
        /// </summary>
        public string[] ReplacementStrings
        {
            get
            {
                string[] strings = new string[ShortFrom(dataBuf, bufOffset + FieldOffsets.NUMSTRINGS)];
                int i = 0;
                int bufpos = bufOffset + IntFrom(dataBuf, bufOffset + FieldOffsets.STRINGOFFSET);
                StringBuilder buf = new StringBuilder();
                while (i < strings.Length)
                {
                    char ch = CharFrom(dataBuf, bufpos);
                    if (ch != '\0')
                        buf.Append(ch);
                    else
                    {
                        strings[i] = buf.ToString();
                        i++;
                        buf = new StringBuilder();
                    }

                    bufpos += 2;
                }

                return strings;
            }
        }

        /// <summary>
        /// The full number identifying the message in the event message dll.
        /// </summary>
        public Int64 InstanceId
        {
            get
            {
                return (UInt32)IntFrom(dataBuf, bufOffset + FieldOffsets.EVENTID);
            }
        }

        /// <summary>
        /// The time at which the application logged this entry.
        /// </summary>
        public DateTime TimeGenerated
        {
            get
            {
                return beginningOfTime.AddSeconds(IntFrom(dataBuf, bufOffset + FieldOffsets.TIMEGENERATED)).ToLocalTime();
            }
        }

        /// <summary>
        /// The time at which the system logged this entry to the event log.
        /// </summary>
        public DateTime TimeWritten
        {
            get
            {
                return beginningOfTime.AddSeconds(IntFrom(dataBuf, bufOffset + FieldOffsets.TIMEWRITTEN)).ToLocalTime();
            }
        }

        /// <summary>
        /// The username of the account associated with this entry by the writing application.
        /// </summary>
        public string UserName
        {
            get
            {
                int sidLen = IntFrom(dataBuf, bufOffset + FieldOffsets.USERSIDLENGTH);
                if (sidLen == 0)
                    return null;
                byte[] sid = new byte[sidLen];
                Array.Copy(dataBuf, bufOffset + IntFrom(dataBuf, bufOffset + FieldOffsets.USERSIDOFFSET),
                           sid, 0, sid.Length);

                int userNameLen = 256;
                int domainNameLen = 256;
                int sidNameUse = 0;
                StringBuilder bufUserName = new StringBuilder(userNameLen);
                StringBuilder bufDomainName = new StringBuilder(domainNameLen);
                StringBuilder retUserName = new StringBuilder();

                if (Interop.Kernel32.LookupAccountSid(MachineName, sid, bufUserName, ref userNameLen, bufDomainName, ref domainNameLen, ref sidNameUse) != 0)
                {
                    retUserName.Append(bufDomainName.ToString());
                    retUserName.Append("\\");
                    retUserName.Append(bufUserName.ToString());
                }

                return retUserName.ToString();
            }
        }

        private char CharFrom(byte[] buf, int offset)
        {
            return (char)ShortFrom(buf, offset);
        }

        public bool Equals(EventLogEntry otherEntry)
        {
            if (otherEntry == null)
                return false;
            int ourLen = IntFrom(dataBuf, bufOffset + FieldOffsets.LENGTH);
            int theirLen = IntFrom(otherEntry.dataBuf, otherEntry.bufOffset + FieldOffsets.LENGTH);
            if (ourLen != theirLen)
            {
                return false;
            }
            int min = bufOffset;
            int max = bufOffset + ourLen;
            int j = otherEntry.bufOffset;
            for (int i = min; i < max; i++, j++)
                if (dataBuf[i] != otherEntry.dataBuf[j])
                {
                    return false;
                }

            return true;
        }

        private int IntFrom(byte[] buf, int offset)
        {
            // assumes Little Endian byte order.
            return (unchecked((int)0xFF000000) & (buf[offset + 3] << 24)) | (0xFF0000 & (buf[offset + 2] << 16)) |
            (0xFF00 & (buf[offset + 1] << 8)) | (0xFF & (buf[offset]));
        }

        internal string ReplaceMessageParameters(String msg, string[] insertionStrings)
        {
            int percentIdx = msg.IndexOf('%');
            if (percentIdx < 0)
                return msg;

            int startCopyIdx = 0;
            int msgLength = msg.Length;
            StringBuilder buf = new StringBuilder();
            string paramDLLNames = GetMessageLibraryNames("ParameterMessageFile");

            while (percentIdx >= 0)
            {
                string param = null;
                int lasNumIdx = percentIdx + 1;
                while (lasNumIdx < msgLength && Char.IsDigit(msg, lasNumIdx))
                    lasNumIdx++;

                uint paramMsgID = 0;

                if (lasNumIdx != percentIdx + 1)
                    UInt32.TryParse(msg.Substring(percentIdx + 1, lasNumIdx - percentIdx - 1), out paramMsgID);

                if (paramMsgID != 0)
                    param = owner.FormatMessageWrapper(paramDLLNames, paramMsgID, insertionStrings);

                if (param != null)
                {
                    if (percentIdx > startCopyIdx)
                        buf.Append(msg, startCopyIdx, percentIdx - startCopyIdx);    // original chars from msg
                    buf.Append(param);
                    startCopyIdx = lasNumIdx;
                }

                percentIdx = msg.IndexOf('%', percentIdx + 1);
            }

            if (msgLength - startCopyIdx > 0)
                buf.Append(msg, startCopyIdx, msgLength - startCopyIdx);          // last span of original msg
            return buf.ToString();
        }

        private static RegistryKey GetSourceRegKey(string logName, string source, string machineName)
        {
            RegistryKey eventKey = null;
            RegistryKey logKey = null;

            try
            {
                eventKey = EventLog.GetEventLogRegKey(machineName, false);
                return eventKey?.OpenSubKey(logName ?? "Application", /*writable*/false)?.OpenSubKey(source, /*writeable*/false);
            }
            finally
            {
                eventKey?.Close();
                logKey?.Close();
            }
        }

        private string GetMessageLibraryNames(string libRegKey)
        {
            // get the value stored in the registry
            string fileName = null;
            RegistryKey regKey = null;
            try
            {
                regKey = GetSourceRegKey(owner.Log, Source, owner.MachineName);
                if (regKey != null)
                {
                    fileName = (string)regKey.GetValue(libRegKey);
                }
            }
            finally
            {
                regKey?.Close();
            }

            if (fileName == null)
                return null;
            // convert any absolute paths on a remote machine to use the \\MACHINENAME\DRIVELETTER$ shares
            if (owner.MachineName != ".")
            {
                string[] fileNames = fileName.Split(';');

                StringBuilder result = new StringBuilder();

                for (int i = 0; i < fileNames.Length; i++)
                {
                    if (fileNames[i].Length >= 2 && fileNames[i][1] == ':')
                    {
                        result.Append(@"\\");
                        result.Append(owner.MachineName);
                        result.Append(@"\");
                        result.Append(fileNames[i][0]);
                        result.Append("$");
                        result.Append(fileNames[i], 2, fileNames[i].Length - 2);
                        result.Append(';');
                    }
                }

                if (result.Length == 0)
                {
                    return null;
                }
                else
                {
                    return result.ToString(0, result.Length - 1);
                }
            }
            else
            {
                return fileName;
            }
        }

        private short ShortFrom(byte[] buf, int offset)
        {
            // assumes little Endian byte order.
            return (short)((0xFF00 & (buf[offset + 1] << 8)) | (0xFF & buf[offset]));
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        private static class FieldOffsets
        {
            internal const int LENGTH = 0;
            internal const int RESERVED = 4;
            internal const int RECORDNUMBER = 8;
            internal const int TIMEGENERATED = 12;
            internal const int TIMEWRITTEN = 16;
            internal const int EVENTID = 20;
            internal const int EVENTTYPE = 24;
            internal const int NUMSTRINGS = 26;
            internal const int EVENTCATEGORY = 28;
            internal const int RESERVEDFLAGS = 30;
            internal const int CLOSINGRECORDNUMBER = 32;
            internal const int STRINGOFFSET = 36;
            internal const int USERSIDLENGTH = 40;
            internal const int USERSIDOFFSET = 44;
            internal const int DATALENGTH = 48;
            internal const int DATAOFFSET = 52;
            internal const int RAWDATA = 56;
        }

        private static readonly DateTime beginningOfTime = new DateTime(1970, 1, 1, 0, 0, 0);
        private const int OFFSETFIXUP = 4 + 4 + 4 + 4 + 4 + 4 + 2 + 2 + 2 + 2 + 4 + 4 + 4 + 4 + 4 + 4;
    }
}
