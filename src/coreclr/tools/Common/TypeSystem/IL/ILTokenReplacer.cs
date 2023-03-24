// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Diagnostics;

namespace Internal.IL
{
    public class ILTokenReplacer
    {
        public static void Replace(byte[] ilStream, Func<int, int> tokenReplaceFunc)
        {
            ILReader ilReader = new ILReader(ilStream);
            while (ilReader.HasNext)
            {
                int offsetBefore = ilReader.Offset;

                ILOpcode opcode = ilReader.ReadILOpcode();
                ilReader.Skip(opcode);
                int offsetAfter = ilReader.Offset;

                if (ILOpcodeHasToken(opcode))
                {
                    Debug.Assert((offsetAfter - offsetBefore) == 5 || (offsetAfter - offsetBefore) == 6);

                    var tokenSpan = ilStream.AsSpan(checked(offsetAfter - 4), 4);

                    // Replace token in IL stream with a new token provided by the tokenReplaceFunc
                    //
                    // This is used by the StandaloneMethodMetadata logic to create method local tokens
                    // and by the IL provider used for cross module inlining to create tokens which are
                    // stable and contained within the R2R module instead of being in a module separated
                    // by a version boundary.
                    int token = BinaryPrimitives.ReadInt32LittleEndian(tokenSpan);
                    var alternateToken = tokenReplaceFunc(token);
                    BinaryPrimitives.WriteInt32LittleEndian(tokenSpan, alternateToken);
                }
            }
        }

        private static bool ILOpcodeHasToken(ILOpcode opcode)
        {
            switch (opcode)
            {
                case ILOpcode.jmp:
                case ILOpcode.call:
                case ILOpcode.calli:
                case ILOpcode.callvirt:
                case ILOpcode.cpobj:
                case ILOpcode.ldobj:
                case ILOpcode.ldstr:
                case ILOpcode.newobj:
                case ILOpcode.castclass:
                case ILOpcode.isinst:
                case ILOpcode.unbox:
                case ILOpcode.ldfld:
                case ILOpcode.ldflda:
                case ILOpcode.stfld:
                case ILOpcode.ldsfld:
                case ILOpcode.ldsflda:
                case ILOpcode.stsfld:
                case ILOpcode.stobj:
                case ILOpcode.box:
                case ILOpcode.newarr:
                case ILOpcode.ldelema:
                case ILOpcode.ldelem:
                case ILOpcode.stelem:
                case ILOpcode.unbox_any:
                case ILOpcode.refanyval:
                case ILOpcode.mkrefany:
                case ILOpcode.ldtoken:
                case ILOpcode.ldftn:
                case ILOpcode.ldvirtftn:
                case ILOpcode.initobj:
                case ILOpcode.constrained:
                case ILOpcode.sizeof_:
                    return true;
            }
            return false;
        }
    }
}
