// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace BadCheckedAdd1
{


    internal enum SniContext
    {
        Undefined = 0,
        Snix_Connect,
        Snix_PreLoginBeforeSuccessfullWrite,
        Snix_PreLogin,
        Snix_LoginSspi,
        Snix_ProcessSspi,
        Snix_Login,
        Snix_EnableMars,
        Snix_AutoEnlist,
        Snix_GetMarsSession,
        Snix_Execute,
        Snix_Read,
        Snix_Close,
        Snix_SendRows,
    }


    internal static class ADP
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static internal bool IsCatchableExceptionType(Exception e)
        {
            return false;
        }
    }


    sealed internal class SQL
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static internal Exception InvalidSSPIPacketSize()
        {
            return null;
        }
    }


    sealed internal class SqlLogin
    {
        internal int timeout;                                                       // login timeout
        internal bool userInstance = false;                                   // user instance
        internal string hostName = "";                                      // client machine name
        internal string userName = "";                                      // user id
        internal string password = "";                                      // password
        internal string applicationName = "";                                      // application name
        internal string serverName = "";                                      // server name
        internal string language = "";                                      // initial language
        internal string database = "";                                      // initial database
        internal string attachDBFilename = "";                                      // DB filename to be attached
        internal string newPassword = "";                                      // new password for reset password
        internal bool useReplication = false;                                   // user login for replication
        internal bool useSSPI = false;                                   // use integrated security
        internal int packetSize = 8000;                                    // packet size
    }


    internal sealed class TdsParserStaticMethods
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static internal byte[] GetNetworkPhysicalAddressForTdsLoginOnly()
        {
            return new Byte[8];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static internal Byte[] EncryptPassword(string password)
        {
            if (password == "")
            {
                return new Byte[0];
            }
            else
            {

                return new Byte[] {
                    0x86, 0xa5,
                    0x36, 0xa5, 0x22, 0xa5,
                    0x33, 0xa5, 0xb3, 0xa5,
                    0xb2, 0xa5, 0x77, 0xa5,
                    0xb6, 0xa5, 0x92, 0xa5
                };
            }
        }
    }


    sealed internal class TdsParserStateObject
    {
        public uint ReadDwordFromPostHeaderContentAtByteOffset(int offset)
        {
            offset += 8;

            using (var reader = new BinaryReader(new MemoryStream(this._outBuff, offset, 4)))
            {
                return reader.ReadUInt32();
            }
        }

        internal byte[] _bTmp = new byte[8];
        internal byte[] _outBuff = new Byte[1000];
        internal int _outBytesUsed = 8;
        internal readonly int _outputHeaderLen = 8;
        internal byte _outputMessageType = 0;
        internal byte _outputPacketNumber = 1;
        internal bool _pendingData = false;
        private int _timeoutSeconds;
        private long _timeoutTime;
        internal int _traceChangePasswordOffset = 0;
        internal int _traceChangePasswordLength = 0;
        internal int _tracePasswordOffset = 0;
        internal int _tracePasswordLength = 0;


        private SniContext _sniContext = SniContext.Undefined;

        internal SniContext SniContext
        {
            get { return _sniContext; }
            set { _sniContext = value; }
        }


        internal void SetTimeoutSeconds(int timeout)
        {
            _timeoutSeconds = timeout;
            if (timeout == 0)
            {
                _timeoutTime = Int64.MaxValue;
            }
        }


        internal void ResetBuffer()
        {
            this._outBytesUsed = this._outputHeaderLen;
            return;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void WritePacket(byte flushMode)
        {
            return;
        }
    }


    internal class TdsParser
    {
        internal TdsParserStateObject _physicalStateObj = new TdsParserStateObject();

        private volatile static UInt32 s_maxSSPILength = 0;

        private static byte[] s_nicAddress;


        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SSPIData(byte[] receivedBuff, UInt32 receivedLength, byte[] sendBuff, ref UInt32 sendLength)
        {
            return;
        }


        internal void WriteByte(byte b, TdsParserStateObject stateObj)
        {
            if (stateObj._outBytesUsed == stateObj._outBuff.Length)
            {
                stateObj.WritePacket(0);
            }

            stateObj._outBuff[stateObj._outBytesUsed++] = b;
        }

        internal void WriteByteArray(Byte[] b, int len, int offsetBuffer, TdsParserStateObject stateObj)
        {
            int offset = offsetBuffer;

            while (len > 0)
            {
                if ((stateObj._outBytesUsed + len) > stateObj._outBuff.Length)
                {
                    int remainder = stateObj._outBuff.Length - stateObj._outBytesUsed;

                    Buffer.BlockCopy(b, offset, stateObj._outBuff, stateObj._outBytesUsed, remainder);

                    offset += remainder;
                    stateObj._outBytesUsed += remainder;

                    if (stateObj._outBytesUsed == stateObj._outBuff.Length)
                    {
                        stateObj.WritePacket(0);
                    }

                    len -= remainder;
                }
                else
                {
                    Buffer.BlockCopy(b, offset, stateObj._outBuff, stateObj._outBytesUsed, len);

                    stateObj._outBytesUsed += len;

                    break;
                }
            }
        }

        internal void WriteShort(int v, TdsParserStateObject stateObj)
        {
            if ((stateObj._outBytesUsed + 2) > stateObj._outBuff.Length)
            {
                WriteByte((byte)(v & 0xff), stateObj);
                WriteByte((byte)((v >> 8) & 0xff), stateObj);
            }
            else
            {
                stateObj._outBuff[stateObj._outBytesUsed++] = (byte)(v & 0xFF);
                stateObj._outBuff[stateObj._outBytesUsed++] = (byte)((v >> 8) & 0xFF);
            }
        }

        internal void WriteInt(int v, TdsParserStateObject stateObj)
        {
            WriteByteArray(BitConverter.GetBytes(v), 4, 0, stateObj);
        }

        private unsafe static void CopyStringToBytes(string source, int sourceOffset, byte[] dest, int destOffset, int charLength)
        {
            int byteLength = checked(charLength * 2);

            fixed (char* sourcePtr = source)
            {
                char* srcPtr = sourcePtr;
                srcPtr += sourceOffset;
                fixed (byte* destinationPtr = dest)
                {
                    byte* destPtr = destinationPtr;
                    destPtr += destOffset;

                    byte* destByteAddress = destPtr;
                    byte* srcByteAddress = (byte*)srcPtr;

                    for (int index = 0; index < byteLength; index++)
                    {
                        *destByteAddress = *srcByteAddress;
                        destByteAddress++;
                        srcByteAddress++;
                    }
                }
            }
        }

        internal void WriteString(string s, int length, int offset, TdsParserStateObject stateObj)
        {
            int cBytes = 2 * length;

            if (cBytes < (stateObj._outBuff.Length - stateObj._outBytesUsed))
            {
                CopyStringToBytes(s, offset, stateObj._outBuff, stateObj._outBytesUsed, length);
                stateObj._outBytesUsed += cBytes;
            }
            else
            {
                if (stateObj._bTmp == null || stateObj._bTmp.Length < cBytes)
                {
                    stateObj._bTmp = new byte[cBytes];
                }

                CopyStringToBytes(s, offset, stateObj._bTmp, 0, length);
                WriteByteArray(stateObj._bTmp, cBytes, 0, stateObj);
            }
        }

        private void WriteString(string s, TdsParserStateObject stateObj)
        {
            WriteString(s, s.Length, 0, stateObj);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void TdsLogin(SqlLogin rec)
        {
            _physicalStateObj.SetTimeoutSeconds(rec.timeout);

            byte[] encryptedPassword = null;
            encryptedPassword = TdsParserStaticMethods.EncryptPassword(rec.password);

            byte[] encryptedChangePassword = null;
            encryptedChangePassword = TdsParserStaticMethods.EncryptPassword(rec.newPassword);

            _physicalStateObj._outputMessageType = 16;

            int length = 0x5e;

            string clientInterfaceName = ".Net SqlClient Data Provider";

            checked
            {
                length += (rec.hostName.Length + rec.applicationName.Length +
                            rec.serverName.Length + clientInterfaceName.Length +
                            rec.language.Length + rec.database.Length +
                            rec.attachDBFilename.Length) * 2;
            }

            byte[] outSSPIBuff = null;
            UInt32 outSSPILength = 0;

            if (!rec.useSSPI)
            {
                checked
                {
                    length += (rec.userName.Length * 2) + encryptedPassword.Length
                    + encryptedChangePassword.Length;
                }
            }
            else
            {
                if (rec.useSSPI)
                {
                    outSSPIBuff = new byte[s_maxSSPILength];
                    outSSPILength = s_maxSSPILength;

                    _physicalStateObj.SniContext = SniContext.Snix_LoginSspi;

                    SSPIData(null, 0, outSSPIBuff, ref outSSPILength);

                    if (outSSPILength > Int32.MaxValue)
                    {
                        throw SQL.InvalidSSPIPacketSize();  // SqlBu 332503
                    }

                    _physicalStateObj.SniContext = SniContext.Snix_Login;

                    checked
                    {
                        length += (Int32)outSSPILength;
                    }
                }
            }

            try
            {
                WriteInt(length, _physicalStateObj);
                WriteInt(0x730a0003, _physicalStateObj);
                WriteInt(rec.packetSize, _physicalStateObj);
                WriteInt(0x06000000, _physicalStateObj);
                WriteInt(0xa10, _physicalStateObj);
                WriteInt(0, _physicalStateObj);

                WriteByte(0xe0, _physicalStateObj);
                WriteByte(0x3, _physicalStateObj);
                WriteByte(0, _physicalStateObj);
                WriteByte(0, _physicalStateObj);
                WriteInt(0, _physicalStateObj);
                WriteInt(0, _physicalStateObj);


                int offset = 0x5e;


                WriteShort(offset, _physicalStateObj);
                WriteShort(rec.hostName.Length, _physicalStateObj);
                offset += rec.hostName.Length * 2;


                if (rec.useSSPI == false)
                {
                    WriteShort(offset, _physicalStateObj);
                    WriteShort(rec.userName.Length, _physicalStateObj);
                    offset += rec.userName.Length * 2;

                    WriteShort(offset, _physicalStateObj);
                    WriteShort(encryptedPassword.Length / 2, _physicalStateObj);
                    offset += encryptedPassword.Length;
                }
                else
                {
                    WriteShort(0, _physicalStateObj);
                    WriteShort(0, _physicalStateObj);
                    WriteShort(0, _physicalStateObj);
                    WriteShort(0, _physicalStateObj);
                }


                WriteShort(offset, _physicalStateObj);
                WriteShort(rec.applicationName.Length, _physicalStateObj);
                offset += rec.applicationName.Length * 2;


                WriteShort(offset, _physicalStateObj);
                WriteShort(rec.serverName.Length, _physicalStateObj);
                offset += rec.serverName.Length * 2;


                WriteShort(offset, _physicalStateObj);
                WriteShort(0, _physicalStateObj);


                WriteShort(offset, _physicalStateObj);
                WriteShort(clientInterfaceName.Length, _physicalStateObj);
                offset += clientInterfaceName.Length * 2;


                WriteShort(offset, _physicalStateObj);
                WriteShort(rec.language.Length, _physicalStateObj);
                offset += rec.language.Length * 2;


                WriteShort(offset, _physicalStateObj);
                WriteShort(rec.database.Length, _physicalStateObj);
                offset += rec.database.Length * 2;


                if (null == s_nicAddress)
                {
                    s_nicAddress = TdsParserStaticMethods.GetNetworkPhysicalAddressForTdsLoginOnly();
                }

                WriteByteArray(s_nicAddress, s_nicAddress.Length, 0, _physicalStateObj);


                WriteShort(offset, _physicalStateObj);

                if (rec.useSSPI)
                {
                    WriteShort((int)outSSPILength, _physicalStateObj);
                    offset += (int)outSSPILength;
                }
                else
                {
                    WriteShort(0, _physicalStateObj);
                }


                WriteShort(offset, _physicalStateObj);
                WriteShort(rec.attachDBFilename.Length, _physicalStateObj);
                offset += rec.attachDBFilename.Length * 2;


                WriteShort(offset, _physicalStateObj);
                WriteShort(encryptedChangePassword.Length / 2, _physicalStateObj);


                WriteInt(0, _physicalStateObj);


                WriteString(rec.hostName, _physicalStateObj);


                if (!rec.useSSPI)
                {
                    WriteString(rec.userName, _physicalStateObj);

                    _physicalStateObj._tracePasswordOffset = _physicalStateObj._outBytesUsed;
                    _physicalStateObj._tracePasswordLength = encryptedPassword.Length;

                    WriteByteArray(encryptedPassword, encryptedPassword.Length, 0, _physicalStateObj);
                }


                WriteString(rec.applicationName, _physicalStateObj);
                WriteString(rec.serverName, _physicalStateObj);
                WriteString(clientInterfaceName, _physicalStateObj);
                WriteString(rec.language, _physicalStateObj);
                WriteString(rec.database, _physicalStateObj);


                if (rec.useSSPI)
                {
                    WriteByteArray(outSSPIBuff, (int)outSSPILength, 0, _physicalStateObj);
                }


                WriteString(rec.attachDBFilename, _physicalStateObj);


                if (!rec.useSSPI)
                {
                    _physicalStateObj._traceChangePasswordOffset = _physicalStateObj._outBytesUsed;
                    _physicalStateObj._traceChangePasswordLength = encryptedChangePassword.Length;
                    WriteByteArray(encryptedChangePassword, encryptedChangePassword.Length, 0, _physicalStateObj);
                }
            }
            catch (Exception e)
            {
                if (ADP.IsCatchableExceptionType(e))
                {
                    _physicalStateObj._outputPacketNumber = 1;
                    _physicalStateObj.ResetBuffer();
                }

                throw;
            }

            _physicalStateObj.WritePacket(1);
            _physicalStateObj._pendingData = true;
            return;
        }
    }

    internal static class App
    {
        private static SqlLogin MakeSqlLoginForRepro()
        {
            var login = new SqlLogin();

            login.hostName = "CHRISAHNA1";
            login.userName = "etcmuser";
            login.password = "29xiaq-1s";
            login.applicationName = ".Net SqlClient Data Provider";
            login.serverName = "csetcmdb.redmond.corp.microsoft.com";
            login.language = "";
            login.database = "Tcm_Global";
            login.attachDBFilename = "";
            login.newPassword = "";

            return login;
        }


        private static int Main()
        {
            var tdsParser = new TdsParser();

            tdsParser.TdsLogin(App.MakeSqlLoginForRepro());

            uint computedLengthValue = tdsParser._physicalStateObj.ReadDwordFromPostHeaderContentAtByteOffset(0x0);

            if (computedLengthValue == 0x15e)
            {
                Console.WriteLine("Test passed.");
                return 100;
            }
            else
            {
                Console.WriteLine("Test failed: ComputedLength=({0:x8})", computedLengthValue);
            }
            return 101;
        }
    }
}
