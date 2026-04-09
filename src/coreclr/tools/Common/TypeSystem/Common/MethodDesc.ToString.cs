// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Internal.TypeSystem
{
    public partial class MethodSignature
    {
        public override string ToString()
        {
            return ToString(includeReturnType: true);
        }

        public string ToString(bool includeReturnType)
        {
            var sb = new StringBuilder();

            if (includeReturnType)
            {
                DebugNameFormatter.Instance.AppendName(sb, ReturnType, DebugNameFormatter.FormatOptions.None);
                sb.Append('(');
            }

            bool first = true;
            foreach (TypeDesc param in _parameters)
            {
                if (first)
                    first = false;
                else
                    sb.Append(',');
                DebugNameFormatter.Instance.AppendName(sb, param, DebugNameFormatter.FormatOptions.None);
            }

            if (includeReturnType)
                sb.Append(')');

            return sb.ToString();
        }
    }

    public partial class MethodDesc
    {
        public override string ToString()
        {
            var sb = new StringBuilder();

            // (Skipping return type to keep things short)
            sb.Append(OwningType);
            sb.Append('.');
            sb.Append(DiagnosticName);

            bool first = true;
            for (int i = 0; i < Instantiation.Length; i++)
            {
                if (first)
                {
                    sb.Append('<');
                    first = false;
                }
                else
                {
                    sb.Append(',');
                }
                DebugNameFormatter.Instance.AppendName(sb, Instantiation[i], DebugNameFormatter.FormatOptions.None);
            }
            if (!first)
                sb.Append('>');

            sb.Append('(');
            try
            {
                sb.Append(Signature.ToString(includeReturnType: false));
            }
            catch
            {
                sb.Append("Unknown");
            }
            sb.Append(')');

            return sb.ToString();
        }
    }
}
