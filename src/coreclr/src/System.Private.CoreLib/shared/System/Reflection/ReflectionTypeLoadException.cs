// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.Serialization;
using System.Text;

namespace System.Reflection
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class ReflectionTypeLoadException : SystemException, ISerializable
    {
        public ReflectionTypeLoadException(Type[]? classes, Exception?[]? exceptions)
            : base(null)
        {
            Types = classes;
            LoaderExceptions = exceptions;
            HResult = HResults.COR_E_REFLECTIONTYPELOAD;
        }

        public ReflectionTypeLoadException(Type[]? classes, Exception?[]? exceptions, string? message)
            : base(message)
        {
            Types = classes;
            LoaderExceptions = exceptions;
            HResult = HResults.COR_E_REFLECTIONTYPELOAD;
        }

        private ReflectionTypeLoadException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            LoaderExceptions = (Exception[]?)(info.GetValue("Exceptions", typeof(Exception[])));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Types", null, typeof(Type[]));
            info.AddValue("Exceptions", LoaderExceptions, typeof(Exception[]));
        }

        public Type[]? Types { get; }

        public Exception?[]? LoaderExceptions { get; }

        public override string Message => CreateString(isMessage: true);

        public override string ToString() => CreateString(isMessage: false);

        private string CreateString(bool isMessage)
        {
            string baseValue = isMessage ? base.Message : base.ToString();

            Exception?[]? exceptions = LoaderExceptions;
            if (exceptions == null || exceptions.Length == 0)
            {
                return baseValue;
            }

            var text = new StringBuilder(baseValue);
            foreach (Exception? e in exceptions)
            {
                if (e != null)
                {
                    text.AppendLine();
                    text.Append(isMessage ? e.Message : e.ToString());
                }
            }
            return text.ToString();
        }
    }
}
