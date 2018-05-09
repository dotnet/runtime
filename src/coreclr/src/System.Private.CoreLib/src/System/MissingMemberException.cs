// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: The exception class for versioning problems with DLLS.
**
**
=============================================================================*/


using System.Runtime.Serialization;
using System.Runtime.CompilerServices;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class MissingMemberException : MemberAccessException, ISerializable
    {
        public MissingMemberException()
            : base(SR.Arg_MissingMemberException)
        {
            HResult = HResults.COR_E_MISSINGMEMBER;
        }

        public MissingMemberException(String message)
            : base(message)
        {
            HResult = HResults.COR_E_MISSINGMEMBER;
        }

        public MissingMemberException(String message, Exception inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_MISSINGMEMBER;
        }

        protected MissingMemberException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ClassName = info.GetString("MMClassName");
            MemberName = info.GetString("MMMemberName");
            Signature = (byte[])info.GetValue("MMSignature", typeof(byte[]));
        }

        public override String Message
        {
            get
            {
                if (ClassName == null)
                {
                    return base.Message;
                }
                else
                {
                    // do any desired fixups to classname here.
                    return SR.Format(SR.MissingMember_Name, ClassName + "." + MemberName + (Signature != null ? " " + FormatSignature(Signature) : ""));
                }
            }
        }

        // Called to format signature
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern String FormatSignature(byte[] signature);

        public MissingMemberException(String className, String memberName)
        {
            ClassName = className;
            MemberName = memberName;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("MMClassName", ClassName, typeof(string));
            info.AddValue("MMMemberName", MemberName, typeof(string));
            info.AddValue("MMSignature", Signature, typeof(byte[]));
        }


        // If ClassName != null, GetMessage will construct on the fly using it
        // and the other variables. This allows customization of the
        // format depending on the language environment.
        protected String ClassName;
        protected String MemberName;
        protected byte[] Signature;
    }
}
