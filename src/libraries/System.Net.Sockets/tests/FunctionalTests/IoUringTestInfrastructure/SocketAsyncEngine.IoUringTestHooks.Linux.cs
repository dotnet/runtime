// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Net.Sockets
{
    internal sealed unsafe partial class SocketAsyncEngine
    {
#if DEBUG
        // Raw Linux errno value used by forced-completion test hooks.
        private const int ErrnoECANCELED = 125;
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static partial void ResetDebugTestForcedResult(ref IoUringCompletionSlot slot)
        {
#if DEBUG
            slot.HasTestForcedResult = false;
            slot.TestForcedResult = 0;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static partial void ResolveDebugTestForcedResult(ref IoUringCompletionSlot slot, ref int result)
        {
#if DEBUG
            if (slot.HasTestForcedResult)
            {
                result = slot.TestForcedResult;
                slot.HasTestForcedResult = false;
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        partial void ApplyDebugTestForcedResult(ref IoUringCompletionSlot slot, byte opcode)
        {
#if DEBUG
            if ((_testForceEagainOnceMask | _testForceEcanceledOnceMask) == 0)
            {
                return;
            }

            if (TryConsumeTestForcedResult(opcode, out int forced))
            {
                slot.HasTestForcedResult = true;
                slot.TestForcedResult = forced;
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        partial void RestoreDebugTestForcedResultIfNeeded(int slotIndex, byte opcode)
        {
#if DEBUG
            Debug.Assert(_completionSlots is not null);
            ref IoUringCompletionSlot slot = ref _completionSlots![slotIndex];
            if (slot.HasTestForcedResult)
            {
                RestoreTestForcedResult(slot.TestForcedResult, opcode);
            }
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        partial void InitializeDebugTestHooksFromEnvironment()
        {
#if DEBUG
            // Mirrors native pal_io_uring.c test hooks.
            _testForceEagainOnceMask = ParseTestOpcodeMask(
                Environment.GetEnvironmentVariable(IoUringTestEnvironmentVariables.ForceEagainOnceMask));
            _testForceEcanceledOnceMask = ParseTestOpcodeMask(
                Environment.GetEnvironmentVariable(IoUringTestEnvironmentVariables.ForceEcanceledOnceMask));
            string? forceSubmitEperm = Environment.GetEnvironmentVariable(
                IoUringTestEnvironmentVariables.ForceSubmitEpermOnce);
            _testForceSubmitEpermOnce = string.Equals(forceSubmitEperm, "1", StringComparison.Ordinal) ? 1 : 0;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryConsumeDebugForcedSubmitError(out Interop.Error forcedError)
        {
            _ = _ioUringInitialized;

#if DEBUG
            if (Interlocked.Exchange(ref _testForceSubmitEpermOnce, 0) != 0)
            {
                forcedError = Interop.Error.EPERM;
                return true;
            }
#endif

            forcedError = Interop.Error.SUCCESS;
            return false;
        }

#if DEBUG
        /// <summary>
        /// Parses a comma-separated list of opcode names (e.g. "send,recv,accept") into a
        /// bitmask of <see cref="IoUringConstants"/> TestOpcodeMask* values.
        /// Mirrors GetIoUringTestOpcodeMaskFromOpcodeNameList in pal_io_uring.c.
        /// </summary>
        private static byte ParseTestOpcodeMask(string? opcodeNameList)
        {
            if (string.IsNullOrEmpty(opcodeNameList))
            {
                return IoUringConstants.TestOpcodeMaskNone;
            }

            byte mask = IoUringConstants.TestOpcodeMaskNone;
            foreach (var name in opcodeNameList.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (name.Equals("send", StringComparison.OrdinalIgnoreCase))
                {
                    mask |= IoUringConstants.TestOpcodeMaskSend;
                }
                else if (name.Equals("recv", StringComparison.OrdinalIgnoreCase))
                {
                    mask |= IoUringConstants.TestOpcodeMaskRecv;
                }
                else if (name.Equals("sendmsg", StringComparison.OrdinalIgnoreCase))
                {
                    mask |= IoUringConstants.TestOpcodeMaskSendMsg;
                }
                else if (name.Equals("recvmsg", StringComparison.OrdinalIgnoreCase))
                {
                    mask |= IoUringConstants.TestOpcodeMaskRecvMsg;
                }
                else if (name.Equals("accept", StringComparison.OrdinalIgnoreCase))
                {
                    mask |= IoUringConstants.TestOpcodeMaskAccept;
                }
                else if (name.Equals("connect", StringComparison.OrdinalIgnoreCase))
                {
                    mask |= IoUringConstants.TestOpcodeMaskConnect;
                }
                else if (name.Equals("sendzc", StringComparison.OrdinalIgnoreCase) || name.Equals("send_zc", StringComparison.OrdinalIgnoreCase))
                {
                    mask |= IoUringConstants.TestOpcodeMaskSendZc;
                }
                else if (name.Equals("sendmsgzc", StringComparison.OrdinalIgnoreCase) || name.Equals("sendmsg_zc", StringComparison.OrdinalIgnoreCase))
                {
                    mask |= IoUringConstants.TestOpcodeMaskSendMsgZc;
                }
            }
            return mask;
        }

        /// <summary>
        /// Maps an io_uring opcode to its corresponding test opcode mask bit.
        /// Mirrors GetIoUringTestOpcodeMaskFromOpcode in pal_io_uring.c.
        /// </summary>
        private static byte GetTestOpcodeMaskFromOpcode(byte opcode)
        {
            return opcode switch
            {
                IoUringOpcodes.Send => IoUringConstants.TestOpcodeMaskSend,
                IoUringOpcodes.Recv => IoUringConstants.TestOpcodeMaskRecv,
                IoUringOpcodes.SendMsg => IoUringConstants.TestOpcodeMaskSendMsg,
                IoUringOpcodes.RecvMsg => IoUringConstants.TestOpcodeMaskRecvMsg,
                IoUringOpcodes.Accept => IoUringConstants.TestOpcodeMaskAccept,
                IoUringOpcodes.Connect => IoUringConstants.TestOpcodeMaskConnect,
                IoUringOpcodes.SendZc => IoUringConstants.TestOpcodeMaskSendZc,
                IoUringOpcodes.SendMsgZc => IoUringConstants.TestOpcodeMaskSendMsgZc,
                _ => IoUringConstants.TestOpcodeMaskNone,
            };
        }

        /// <summary>
        /// Tries to consume a forced test result for the given opcode.
        /// EAGAIN takes priority over ECANCELED when both are set.
        /// Mirrors TryConsumeIoUringForcedCompletionResultLocked in pal_io_uring.c.
        /// </summary>
        private bool TryConsumeTestForcedResult(byte opcode, out int forcedResult)
        {
            forcedResult = 0;
            byte opcodeMask = GetTestOpcodeMaskFromOpcode(opcode);
            if (opcodeMask == IoUringConstants.TestOpcodeMaskNone)
            {
                return false;
            }

            if ((_testForceEagainOnceMask & opcodeMask) != 0)
            {
                _testForceEagainOnceMask &= (byte)~opcodeMask;
                forcedResult = -Interop.Sys.ConvertErrorPalToPlatform(Interop.Error.EAGAIN);
                return true;
            }

            if ((_testForceEcanceledOnceMask & opcodeMask) != 0)
            {
                _testForceEcanceledOnceMask &= (byte)~opcodeMask;
                forcedResult = -ErrnoECANCELED;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Restores a previously consumed forced test result mask bit.
        /// Called when SQE acquisition fails after the forced result was consumed,
        /// so the test hook can fire on the next attempt.
        /// Mirrors RestoreIoUringForcedCompletionResultLocked in pal_io_uring.c.
        /// </summary>
        private void RestoreTestForcedResult(int forcedResult, byte opcode)
        {
            byte opcodeMask = GetTestOpcodeMaskFromOpcode(opcode);
            if (opcodeMask == IoUringConstants.TestOpcodeMaskNone)
            {
                return;
            }

            if (forcedResult == -Interop.Sys.ConvertErrorPalToPlatform(Interop.Error.EAGAIN))
            {
                _testForceEagainOnceMask |= opcodeMask;
            }
            else if (forcedResult == -ErrnoECANCELED)
            {
                _testForceEcanceledOnceMask |= opcodeMask;
            }
        }
#endif
    }
}
