// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

using Internal.TypeSystem;

namespace Internal.IL
{
    internal sealed class MethodILDebugView
    {
        private readonly MethodIL _methodIL;

        public MethodILDebugView(MethodIL methodIL)
        {
            _methodIL = methodIL;
        }

        public string Disassembly
        {
            get
            {
                ILDisassembler disasm = new ILDisassembler(_methodIL);

                StringBuilder sb = new StringBuilder();

                MethodDesc owningMethod = _methodIL.OwningMethod;

                sb.Append("// ");
                sb.AppendLine(owningMethod.ToString());
                sb.Append(".method ");
                // TODO: accessibility, specialname, calling conventions etc.
                if (!owningMethod.Signature.IsStatic)
                    sb.Append("instance ");
                disasm.AppendType(sb, owningMethod.Signature.ReturnType);
                sb.Append(' ');
                sb.Append(owningMethod.Name);
                if (owningMethod.HasInstantiation)
                {
                    sb.Append('<');
                    for (int i = 0; i < owningMethod.Instantiation.Length; i++)
                    {
                        if (i != 0)
                            sb.Append(", ");
                        disasm.AppendType(sb, owningMethod.Instantiation[i]);
                    }
                    sb.Append('>');
                }
                sb.Append('(');
                for (int i = 0; i < owningMethod.Signature.Length; i++)
                {
                    if (i != 0)
                        sb.Append(", ");
                    disasm.AppendType(sb, owningMethod.Signature[i]);
                }
                sb.AppendLine(") cil managed");

                sb.AppendLine("{");

                sb.Append("  // Code size: ");
                sb.Append(disasm.CodeSize);
                sb.AppendLine();
                sb.Append("  .maxstack ");
                sb.Append(_methodIL.MaxStack);
                sb.AppendLine();

                LocalVariableDefinition[] locals = _methodIL.GetLocals();
                if (locals != null && locals.Length > 0)
                {
                    sb.Append("  .locals ");
                    if (_methodIL.IsInitLocals)
                        sb.Append("init ");

                    sb.Append('(');

                    for (int i = 0; i < locals.Length; i++)
                    {
                        if (i != 0)
                        {
                            sb.AppendLine(",");
                            sb.Append(' ', 6);
                        }
                        disasm.AppendType(sb, locals[i].Type);
                        sb.Append(' ');
                        if (locals[i].IsPinned)
                            sb.Append("pinned ");
                        sb.Append("V_");
                        sb.Append(i);
                    }
                    sb.AppendLine(")");
                }
                sb.AppendLine();

                const string pad = "  ";

                // TODO: pretty exception regions
                foreach (ILExceptionRegion region in _methodIL.GetExceptionRegions())
                {
                    sb.Append(pad);
                    sb.Append(".try ");
                    ILDisassembler.AppendOffset(sb, region.TryOffset);
                    sb.Append(" to ");
                    ILDisassembler.AppendOffset(sb, region.TryOffset + region.TryLength);

                    switch (region.Kind)
                    {
                        case ILExceptionRegionKind.Catch:
                            sb.Append(" catch ");
                            disasm.AppendType(sb, (TypeDesc)_methodIL.GetObject(region.ClassToken));
                            break;
                        case ILExceptionRegionKind.Fault:
                            sb.Append(" fault");
                            break;
                        case ILExceptionRegionKind.Filter:
                            sb.Append(" filter ");
                            ILDisassembler.AppendOffset(sb, region.FilterOffset);
                            break;
                        case ILExceptionRegionKind.Finally:
                            sb.Append(" finally");
                            break;
                    }

                    sb.Append(" handler ");
                    ILDisassembler.AppendOffset(sb, region.HandlerOffset);
                    sb.Append(" to ");
                    ILDisassembler.AppendOffset(sb, region.HandlerOffset + region.HandlerLength);
                    sb.AppendLine();
                }

                while (disasm.HasNextInstruction)
                {
                    sb.Append(pad);
                    sb.AppendLine(disasm.GetNextInstruction());
                }

                sb.AppendLine("}");

                return sb.ToString();
            }
        }
    }
}
