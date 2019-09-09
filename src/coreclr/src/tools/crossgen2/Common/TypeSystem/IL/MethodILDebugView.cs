// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                sb.Append(" ");
                sb.Append(owningMethod.Name);
                if (owningMethod.HasInstantiation)
                {
                    sb.Append("<");
                    for (int i = 0; i < owningMethod.Instantiation.Length; i++)
                    {
                        if (i != 0)
                            sb.Append(", ");
                        disasm.AppendType(sb, owningMethod.Instantiation[i]);
                    }
                    sb.Append(">");
                }
                sb.Append("(");
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

                    sb.Append("(");

                    for (int i = 0; i < locals.Length; i++)
                    {
                        if (i != 0)
                        {
                            sb.AppendLine(",");
                            sb.Append(' ', 6);
                        }
                        disasm.AppendType(sb, locals[i].Type);
                        sb.Append(" ");
                        if (locals[i].IsPinned)
                            sb.Append("pinned ");
                        sb.Append("V_");
                        sb.Append(i);
                    }
                    sb.AppendLine(")");
                }
                sb.AppendLine();

                // TODO: exception regions
                while (disasm.HasNextInstruction)
                {
                    sb.Append("  ");
                    sb.AppendLine(disasm.GetNextInstruction());
                }

                sb.AppendLine("}");

                return sb.ToString();
            }
        }
    }
}
