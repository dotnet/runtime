// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    using System;

    public enum ActiveDirectorySyntax : int
    {
        CaseExactString = 0,
        CaseIgnoreString = 1,
        NumericString = 2,
        DirectoryString = 3,
        OctetString = 4,
        SecurityDescriptor = 5,
        Int = 6,
        Int64 = 7,
        Bool = 8,
        Oid = 9,
        GeneralizedTime = 10,
        UtcTime = 11,
        DN = 12,
        DNWithBinary = 13,
        DNWithString = 14,
        Enumeration = 15,
        IA5String = 16,
        PrintableString = 17,
        Sid = 18,
        AccessPointDN = 19,
        ORName = 20,
        PresentationAddress = 21,
        ReplicaLink = 22
    }

    internal class OMObjectClass
    {
        public byte[] data = null;

        public OMObjectClass(byte[] data)
        {
            this.data = data;
        }

        public bool Equals(OMObjectClass OMObjectClass)
        {
            bool result = true;
            if (data.Length == OMObjectClass.data.Length)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] != OMObjectClass.data[i])
                    {
                        result = false;
                        break;
                    }
                }
            }
            else
            {
                result = false;
            }
            return result;
        }

        public byte[] Data
        {
            get
            {
                return data;
            }
        }
    }

    internal class Syntax
    {
        public string attributeSyntax = null;
        public int oMSyntax = 0;
        public OMObjectClass oMObjectClass = null;

        public Syntax(string attributeSyntax, int oMSyntax, OMObjectClass oMObjectClass)
        {
            this.attributeSyntax = attributeSyntax;
            this.oMSyntax = oMSyntax;
            this.oMObjectClass = oMObjectClass;
        }

        public bool Equals(Syntax syntax)
        {
            bool result = true;
            if ((!syntax.attributeSyntax.Equals(this.attributeSyntax))
                || (syntax.oMSyntax != this.oMSyntax))
            {
                result = false;
            }
            else
            {
                if (((this.oMObjectClass != null) && (syntax.oMObjectClass == null))
                    || ((this.oMObjectClass == null) && (syntax.oMObjectClass != null))
                    || ((this.oMObjectClass != null) && (syntax.oMObjectClass != null) && (!this.oMObjectClass.Equals(syntax.oMObjectClass))))
                {
                    result = false;
                }
            }
            return result;
        }
    }
}
