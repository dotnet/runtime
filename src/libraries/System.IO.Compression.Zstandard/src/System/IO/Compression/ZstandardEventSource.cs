// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.IO.Compression
{
    [EventSource(Name = "Private.InternalDiagnostics.System.IO.Compression.Zstandard")]
    internal sealed partial class ZstandardEventSource : EventSource
    {
        public static readonly ZstandardEventSource Log = new ZstandardEventSource();

        private const string NullInstance = "(null)";

        private const int InfoEventId = 1;
        private const int ErrorEventId = 2;

        [NonEvent]
        public static void Info(object? thisOrContextObject, FormattableString? formattableString = null, [CallerMemberName] string? memberName = null)
        {
            Log.Info(IdOf(thisOrContextObject), memberName, formattableString != null ? Format(formattableString) : null);
        }

        [Event(InfoEventId, Level = EventLevel.Informational)]
        public void Info(string thisOrContextObject, string? memberName, string? message)
        {
            WriteEvent(InfoEventId, thisOrContextObject, memberName, message);
        }

        [NonEvent]
        public static void Error(object? thisOrContextObject, FormattableString? formattableString = null, [CallerMemberName] string? memberName = null)
        {
            Log.Error(IdOf(thisOrContextObject), memberName, formattableString != null ? Format(formattableString) : null);
        }

        [Event(ErrorEventId, Level = EventLevel.Informational)]
        public void Error(string thisOrContextObject, string? memberName, string? message)
        {
            WriteEvent(ErrorEventId, thisOrContextObject, memberName, message);
        }

        [NonEvent]
        public static string IdOf(object? value) => value != null ? value.GetType().Name + "#" + GetHashCode(value) : NullInstance;

        [NonEvent]
        public static int GetHashCode(object? value) => value?.GetHashCode() ?? 0;

        [NonEvent]
        public static string? Format(object? value)
        {
            // If it's null, return a known string for null values
            if (value == null)
            {
                return NullInstance;
            }

            // Format arrays with their element type name and length
            if (value is Array arr)
            {
                return $"{arr.GetType().GetElementType()}[{((Array)value).Length}]";
            }

            // Format ICollections as the name and count
            if (value is ICollection c)
            {
                return $"{c.GetType().Name}({c.Count})";
            }

            // Format SafeHandles as their type, hash code, and pointer value
            if (value is SafeHandle handle)
            {
                return $"{handle.GetType().Name}:{handle.GetHashCode()}(0x{handle.DangerousGetHandle():X})";
            }

            // Format IntPtrs as hex
            if (value is IntPtr)
            {
                return $"0x{value:X}";
            }

            // If the string representation of the instance would just be its type name,
            // use its id instead.
            string? toString = value.ToString();
            if (toString == null || toString == value.GetType().FullName)
            {
                return IdOf(value);
            }

            // Otherwise, return the original object so that the caller does default formatting.
            return value.ToString();
        }

        [NonEvent]
        private static string Format(FormattableString s)
        {
            switch (s.ArgumentCount)
            {
                case 0: return s.Format;
                case 1: return string.Format(s.Format, Format(s.GetArgument(0)));
                case 2: return string.Format(s.Format, Format(s.GetArgument(0)), Format(s.GetArgument(1)));
                case 3: return string.Format(s.Format, Format(s.GetArgument(0)), Format(s.GetArgument(1)), Format(s.GetArgument(2)));
                default:
                    string?[] formattedArgs = new string?[s.ArgumentCount];
                    for (int i = 0; i < formattedArgs.Length; i++)
                    {
                        formattedArgs[i] = Format(s.GetArgument(i));
                    }
                    return string.Format(s.Format, formattedArgs);
            }
        }
    }
}
