// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Data.Common;


namespace System.Data.SqlClient
{
    internal sealed class TdsParserStaticMethods
    {
        // Obfuscate password to be sent to SQL Server
        // Blurb from the TDS spec at https://msdn.microsoft.com/en-us/library/dd304523.aspx
        // "Before submitting a password from the client to the server, for every byte in the password buffer 
        // starting with the position pointed to by IbPassword, the client SHOULD first swap the four high bits 
        // with the four low bits and then do a bit-XOR with 0xA5 (10100101). After reading a submitted password, 
        // for every byte in the password buffer starting with the position pointed to by IbPassword, the server SHOULD 
        // first do a bit-XOR with 0xA5 (10100101) and then swap the four high bits with the four low bits."
        // The password exchange during Login phase happens over a secure channel i.e. SSL/TLS 
        // Note: The same logic is used in SNIPacketSetData (SniManagedWrapper) to encrypt passwords stored in SecureString
        //       If this logic changed, SNIPacketSetData needs to be changed as well
        internal static Byte[] ObfuscatePassword(string password)
        {
            Byte[] bObfuscated = new Byte[password.Length << 1];
            int s;
            byte bLo;
            byte bHi;

            for (int i = 0; i < password.Length; i++)
            {
                s = (int)password[i];
                bLo = (byte)(s & 0xff);
                bHi = (byte)((s >> 8) & 0xff);
                bObfuscated[i << 1] = (Byte)((((bLo & 0x0f) << 4) | (bLo >> 4)) ^ 0xa5);
                bObfuscated[(i << 1) + 1] = (Byte)((((bHi & 0x0f) << 4) | (bHi >> 4)) ^ 0xa5);
            }
            return bObfuscated;
        }

        private const int NoProcessId = -1;
        private static int s_currentProcessId = NoProcessId;
        internal static int GetCurrentProcessIdForTdsLoginOnly()
        {
            if (s_currentProcessId == NoProcessId)
            {
                // In ProjectK\CoreCLR we don't want to take a dependency on an assembly
                // just to grab the real Process Id that the server doesn't really use
                // So, instead, pick a random number and use that for all connections
                Random rand = new Random();
                int processId = rand.Next();
                Threading.Interlocked.CompareExchange(ref s_currentProcessId, processId, NoProcessId);
            }
            return s_currentProcessId;
        }


        internal static Int32 GetCurrentThreadIdForTdsLoginOnly()
        {
            return Environment.CurrentManagedThreadId;
        }


        private static byte[] s_nicAddress = null;
        internal static byte[] GetNetworkPhysicalAddressForTdsLoginOnly()
        {
            // For ProjectK\CoreCLR we don't want to take a dependency on the registry to try to read a value
            // that isn't usually set, so we'll just use a random value each time instead
            if (null == s_nicAddress)
            {
                byte[] newNicAddress = new byte[TdsEnums.MAX_NIC_SIZE];
                Random random = new Random();
                random.NextBytes(newNicAddress);
                Threading.Interlocked.CompareExchange(ref s_nicAddress, newNicAddress, null);
            }

            return s_nicAddress;
        }

        // translates remaining time in stateObj (from user specified timeout) to timeout value for SNI
        internal static Int32 GetTimeoutMilliseconds(long timeoutTime)
        {
            // User provided timeout t | timeout value for SNI | meaning
            // ------------------------+-----------------------+------------------------------
            //      t == long.MaxValue |                    -1 | infinite timeout (no timeout)
            //   t>0 && t<int.MaxValue |                     t |
            //          t>int.MaxValue |          int.MaxValue | must not exceed int.MaxValue

            if (Int64.MaxValue == timeoutTime)
            {
                return -1;  // infinite timeout
            }

            long msecRemaining = ADP.TimerRemainingMilliseconds(timeoutTime);

            if (msecRemaining < 0)
            {
                return 0;
            }
            if (msecRemaining > (long)Int32.MaxValue)
            {
                return Int32.MaxValue;
            }
            return (Int32)msecRemaining;
        }


        internal static long GetTimeout(long timeoutMilliseconds)
        {
            long result;
            if (timeoutMilliseconds <= 0)
            {
                result = Int64.MaxValue; // no timeout...
            }
            else
            {
                try
                {
                    result = checked(ADP.TimerCurrent() + ADP.TimerFromMilliseconds(timeoutMilliseconds));
                }
                catch (OverflowException)
                {
                    // In case of overflow, set to 'infinite' timeout
                    result = Int64.MaxValue;
                }
            }
            return result;
        }

        internal static bool TimeoutHasExpired(long timeoutTime)
        {
            bool result = false;

            if (0 != timeoutTime && Int64.MaxValue != timeoutTime)
            {
                result = ADP.TimerHasExpired(timeoutTime);
            }
            return result;
        }

        internal static int NullAwareStringLength(string str)
        {
            if (str == null)
            {
                return 0;
            }
            else
            {
                return str.Length;
            }
        }

        internal static int GetRemainingTimeout(int timeout, long start)
        {
            if (timeout <= 0)
            {
                return timeout;
            }
            long remaining = ADP.TimerRemainingSeconds(start + ADP.TimerFromSeconds(timeout));
            if (remaining <= 0)
            {
                return 1;
            }
            else
            {
                return checked((int)remaining);
            }
        }
    }
}
