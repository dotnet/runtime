// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace System.Reflection
{
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class ReflectionTypeLoadException : SystemException
    {
        public ReflectionTypeLoadException(Type?[]? classes, Exception?[]? exceptions) :
            this(classes, exceptions, null)
        {
        }

        public ReflectionTypeLoadException(Type?[]? classes, Exception?[]? exceptions, string? message)
            : base(message)
        {
            Types = classes ?? Type.EmptyTypes;
            LoaderExceptions = exceptions ?? Array.Empty<Exception>();
            HResult = HResults.COR_E_REFLECTIONTYPELOAD;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        private ReflectionTypeLoadException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Types = Type.EmptyTypes;
            LoaderExceptions = (Exception?[]?)info.GetValue("Exceptions", typeof(Exception[])) ?? Array.Empty<Exception?>();
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Types", null, typeof(Type[]));
            info.AddValue("Exceptions", LoaderExceptions, typeof(Exception[]));
        }

        public Type?[] Types { get; }

        public Exception?[] LoaderExceptions { get; }

        public override string Message => CreateString(isMessage: true);

        public override string ToString() => CreateString(isMessage: false);

        private string CreateString(bool isMessage)
        {
            string baseValue = isMessage ? base.Message : base.ToString();

            Exception?[] exceptions = LoaderExceptions;
            if (exceptions.Length == 0)
            {
                return baseValue;
            }

            var text = new StringBuilder(baseValue);
            foreach (Exception? e in exceptions)
            {
                if (e != null)
                {
                    text.AppendLine().Append(isMessage ? e.Message : e.ToString());
                }
            }

            return text.ToString();
        }
    }
}
