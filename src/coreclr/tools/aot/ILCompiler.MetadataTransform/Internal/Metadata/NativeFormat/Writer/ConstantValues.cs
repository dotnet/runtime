// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Reflection;

using Internal.LowLevelLinq;

namespace Internal.Metadata.NativeFormat.Writer
{
    public partial class ConstantBooleanValue
    {
        public override string ToString()
        {
            //return String.Format("ConstantBooleanValue : {0}", this.Value);
            return String.Format("(Boolean){0}", this.Value);
        }
    }

    public partial class ConstantBooleanArray
    {
        public override string ToString()
        {
            //return "ConstantBooleanArray : {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
            return "(Boolean[]) {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
        }
    }
    public partial class ConstantCharValue
    {
        public override string ToString()
        {
            //return String.Format("ConstantCharValue : {0}", this.Value);

            return String.Format("'{0}'", this.Value);
        }
    }

    public partial class ConstantCharArray
    {
        public override string ToString()
        {
            //return "ConstantCharArray : {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
            return "(Char[]) {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
        }
    }
    public partial class ConstantStringValue
    {
        public override string ToString()
        {
            //return String.Format("ConstantStringValue : {0}", this.Value);
            if (this.Value == null) return "null";
            else return String.Format("\"{0}\"", this.Value);
        }
    }

    public partial class ConstantStringArray
    {
        public override string ToString()
        {
            //return "ConstantStringArray : {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
            return "(String[]) {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
        }
    }
    public partial class ConstantByteValue
    {
        public override string ToString()
        {
            //return String.Format("ConstantByteValue : {0}", this.Value);
            return String.Format("(Byte){0}", this.Value);
        }
    }

    public partial class ConstantByteArray
    {
        public override string ToString()
        {
            //return "ConstantByteArray : {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
            return "(Byte[]) {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
        }
    }
    public partial class ConstantSByteValue
    {
        public override string ToString()
        {
            //return String.Format("ConstantSByteValue : {0}", this.Value);
            return String.Format("(SByte){0}", this.Value);
        }
    }

    public partial class ConstantSByteArray
    {
        public override string ToString()
        {
            //return "ConstantSByteArray : {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
            return "(SByte[]) {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
        }
    }
    public partial class ConstantInt16Value
    {
        public override string ToString()
        {
            //return String.Format("ConstantInt16Value : {0}", this.Value);
            return String.Format("(Int16){0}", this.Value);
        }
    }

    public partial class ConstantInt16Array
    {
        public override string ToString()
        {
            //return "ConstantInt16Array : {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
            return "(Int16[]) {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
        }
    }
    public partial class ConstantUInt16Value
    {
        public override string ToString()
        {
            //return String.Format("ConstantUInt16Value : {0}", this.Value);
            return String.Format("(UInt16){0}", this.Value);
        }
    }

    public partial class ConstantUInt16Array
    {
        public override string ToString()
        {
            //return "ConstantUInt16Array : {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
            return "(UInt16[]) {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
        }
    }
    public partial class ConstantInt32Value
    {
        public override string ToString()
        {
            //return String.Format("ConstantInt32Value : {0}", this.Value);
            return String.Format("(Int32){0}", this.Value);
        }
    }

    public partial class ConstantInt32Array
    {
        public override string ToString()
        {
            //return "ConstantInt32Array : {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
            return "(Int32[]) {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
        }
    }
    public partial class ConstantUInt32Value
    {
        public override string ToString()
        {
            //return String.Format("ConstantUInt32Value : {0}", this.Value);
            return String.Format("(UInt32){0}", this.Value);
        }
    }

    public partial class ConstantUInt32Array
    {
        public override string ToString()
        {
            //return "ConstantUInt32Array : {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
            return "(UInt32[]) {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
        }
    }
    public partial class ConstantInt64Value
    {
        public override string ToString()
        {
            //return String.Format("ConstantInt64Value : {0}", this.Value);
            return String.Format("(Int64){0}", this.Value);
        }
    }

    public partial class ConstantInt64Array
    {
        public override string ToString()
        {
            //return "ConstantInt64Array : {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
            return "(Int64[]) {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
        }
    }
    public partial class ConstantUInt64Value
    {
        public override string ToString()
        {
            //return String.Format("ConstantUInt64Value : {0}", this.Value);
            return String.Format("(UInt64){0}", this.Value);
        }
    }

    public partial class ConstantUInt64Array
    {
        public override string ToString()
        {
            //return "ConstantUInt64Array : {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
            return "(UInt64[]) {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
        }
    }
    public partial class ConstantSingleValue
    {
        public override string ToString()
        {
            //return String.Format("ConstantSingleValue : {0}", this.Value);
            return String.Format("(Single){0}", this.Value);
        }
    }

    public partial class ConstantSingleArray
    {
        public override string ToString()
        {
            //return "ConstantSingleArray : {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
            return "(Single[]) {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
        }
    }
    public partial class ConstantDoubleValue
    {
        public override string ToString()
        {
            //return String.Format("ConstantDoubleValue : {0}", this.Value);
            return String.Format("(Double){0}", this.Value);
        }
    }

    public partial class ConstantDoubleArray
    {
        public override string ToString()
        {
            //return "ConstantDoubleArray : {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
            return "(Double[]) {" + String.Join(", ", this.Value.Select(v => v.ToString())) + "}";
        }
    }
    public partial class ConstantReferenceValue
    {
        public override string ToString()
        {
            return "null";
        }
    }
}
