// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CA1823 // not all IDs are used by all partial providers

namespace System.Net
{
    // Implementation:
    // This partial file is meant to be consumed into each System.Net.* assembly that needs to log.  Each such assembly also provides
    // its own NetEventSource partial class that adds an appropriate [EventSource] attribute, giving it a unique name for that assembly.
    // Those partials can then also add additional events if needed, starting numbering from the NextAvailableEventId defined by this partial.

    // Usage:
    // - Operations that may allocate (e.g. boxing a value type, using string interpolation, etc.) or that may have computations
    //   at call sites should guard access like:
    //       if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Found certificate: {cert}"); // info logging with a formattable string
    // - Operations that have zero allocations / measurable computations at call sites can use a simpler pattern, calling methods like:
    //       NetEventSource.Info(this, "literal string");  // arbitrary message with a literal string
    //   Debug.Asserts inside the logging methods will help to flag some misuse if the DEBUG_NETEVENTSOURCE_MISUSE compilation constant is defined.
    //   However, because it can be difficult by observation to understand all of the costs involved, guarding can be done everywhere.
    // - Messages can be strings, formattable strings, or any other object.  Objects (including those used in formattable strings) have special
    //   formatting applied, controlled by the Format method.  Partial specializations can also override this formatting by implementing a partial
    //   method that takes an object and optionally provides a string representation of it, in case a particular library wants to customize further.

    /// <summary>Provides logging facilities for System.Net libraries.</summary>
    internal sealed partial class NetEventSource : EventSource
    {
        private const string EventSourceSuppressMessage = "Parameters to this method are primitive and are trimmer safe";

        /// <summary>The single event source instance to use for all logging.</summary>
        public static readonly NetEventSource Log = new NetEventSource();

        #region Metadata
        public static class Keywords
        {
            public const EventKeywords Default = (EventKeywords)0x0001;
            public const EventKeywords Debug = (EventKeywords)0x0002;
        }

        private const string MissingMember = "(?)";
        private const string NullInstance = "(null)";
        private const string StaticMethodObject = "(static)";
        private const string NoParameters = "";

        private const int InfoEventId = 1;
        private const int ErrorEventId = 2;
        // private const int AssociateEventId = 3; // Defined in NetEventSource.Common.Associate.cs
        // private const int DumpArrayEventId = 4; // Defined in NetEventSource.Common.DumpBuffer.cs

        private const int NextAvailableEventId = 5; // Update this value whenever new events are added.  Derived types should base all events off of this to avoid conflicts.
        #endregion

        #region Info
        /// <summary>Logs an information message.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="formattableString">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Info(object? thisOrContextObject, FormattableString? formattableString = null, [CallerMemberName] string? memberName = null) =>
            Log.Info(IdOf(thisOrContextObject), memberName, formattableString != null ? Format(formattableString) : NoParameters);

        /// <summary>Logs an information message.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="message">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Info(object? thisOrContextObject, object? message, [CallerMemberName] string? memberName = null) =>
            Log.Info(IdOf(thisOrContextObject), memberName, Format(message));

        [Event(InfoEventId, Level = EventLevel.Informational, Keywords = Keywords.Default)]
        private void Info(string thisOrContextObject, string? memberName, string? message)
        {
            //Debug.Assert(IsEnabled());
            WriteEvent(InfoEventId, thisOrContextObject, memberName ?? MissingMember, message);
        }
        #endregion

        #region Error
        /// <summary>Logs an error message.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="formattableString">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Error(object? thisOrContextObject, FormattableString formattableString, [CallerMemberName] string? memberName = null) =>
            Log.ErrorMessage(IdOf(thisOrContextObject), memberName, Format(formattableString));

        /// <summary>Logs an error message.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="message">The message to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Error(object? thisOrContextObject, object message, [CallerMemberName] string? memberName = null) =>
            Log.ErrorMessage(IdOf(thisOrContextObject), memberName, Format(message));

        [Event(ErrorEventId, Level = EventLevel.Error, Keywords = Keywords.Default)]
        private void ErrorMessage(string thisOrContextObject, string? memberName, string? message)
        {
            //Debug.Assert(IsEnabled());
            WriteEvent(ErrorEventId, thisOrContextObject, memberName ?? MissingMember, message);
        }
        #endregion

        #region Helpers
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

            // Give another partial implementation a chance to provide its own string representation
            string? result = null;
            AdditionalCustomizedToString(value, ref result);
            if (result is not null)
            {
                return result;
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

        static partial void AdditionalCustomizedToString(object value, ref string? result);
        #endregion

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [NonEvent]
        private unsafe void WriteEvent(int eventId, string? arg1, string? arg2, int arg3)
        {
            arg1 ??= "";
            arg2 ??= "";

            fixed (char* arg1Ptr = arg1)
            fixed (char* arg2Ptr = arg2)
            {
                const int NumEventDatas = 3;
                EventData* descrs = stackalloc EventData[NumEventDatas];

                descrs[0] = new EventData
                {
                    DataPointer = (IntPtr)(arg1Ptr),
                    Size = (arg1.Length + 1) * sizeof(char)
                };
                descrs[1] = new EventData
                {
                    DataPointer = (IntPtr)(arg2Ptr),
                    Size = (arg2.Length + 1) * sizeof(char)
                };
                descrs[2] = new EventData
                {
                    DataPointer = (IntPtr)(&arg3),
                    Size = sizeof(int)
                };

                WriteEventCore(eventId, NumEventDatas, descrs);
            }
        }
    }
}
