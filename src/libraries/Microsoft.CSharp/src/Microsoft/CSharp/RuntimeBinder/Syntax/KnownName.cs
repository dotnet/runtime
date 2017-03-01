// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;

namespace Microsoft.CSharp.RuntimeBinder.Syntax
{
    internal partial class NameManager
    {
        private sealed class KnownName : Name
        {
            public KnownName(string text)
                : base(text)
            {
            }

            public KnownName(string text, PredefinedName id)
                : base(text)
            {
                PredefinedName = id;
            }

            public PredefinedName PredefinedName { get; } = PredefinedName.PN_COUNT;
        }

        private static NameTable s_knownNames;

        private void InitKnownNames()
        {
            if (s_knownNames == null)
            {
                NameTable tmp = new NameTable();

                // add all predefined names
                Debug.Assert(s_predefinedNames.Length == (int)PredefinedName.PN_COUNT);
                for (int i = 0, n = s_predefinedNames.Length; i < n; i++)
                {
                    Debug.Assert((int)s_predefinedNames[i].PredefinedName == i);
                    Name name = s_predefinedNames[i];
                    tmp.Add(name);
                }

                // add all other names
                foreach (KnownName name in s_otherNames)
                {
                    tmp.Add(name);
                }

                Interlocked.CompareExchange<NameTable>(ref s_knownNames, tmp, null);
            }
        }

        private static readonly KnownName[] s_predefinedNames = new KnownName[(int)PredefinedName.PN_COUNT] {
            new KnownName(".ctor", PredefinedName.PN_CTOR),
            new KnownName("Finalize", PredefinedName.PN_DTOR),
            new KnownName(".cctor", PredefinedName.PN_STATCTOR),
            new KnownName("*", PredefinedName.PN_PTR),
            new KnownName("?*", PredefinedName.PN_NUB),
            new KnownName("#", PredefinedName.PN_OUTPARAM),
            new KnownName("&", PredefinedName.PN_REFPARAM),
            new KnownName("[X\001", PredefinedName.PN_ARRAY0),
            new KnownName("[X\002", PredefinedName.PN_ARRAY1),
            new KnownName("[X\003", PredefinedName.PN_ARRAY2),
            new KnownName("[G\001", PredefinedName.PN_GARRAY0),
            new KnownName("[G\002", PredefinedName.PN_GARRAY1),
            new KnownName("[G\003", PredefinedName.PN_GARRAY2),
            new KnownName("Invoke", PredefinedName.PN_INVOKE),
            new KnownName("Length", PredefinedName.PN_LENGTH),
            new KnownName("Item", PredefinedName.PN_INDEXER),
            new KnownName("$Item$", PredefinedName.PN_INDEXERINTERNAL),
            new KnownName("Combine", PredefinedName.PN_COMBINE),
            new KnownName("Remove", PredefinedName.PN_REMOVE),
            new KnownName("op_Explicit", PredefinedName.PN_OPEXPLICITMN),
            new KnownName("op_Implicit", PredefinedName.PN_OPIMPLICITMN),
            new KnownName("op_UnaryPlus", PredefinedName.PN_OPUNARYPLUS),
            new KnownName("op_UnaryNegation", PredefinedName.PN_OPUNARYMINUS),
            new KnownName("op_OnesComplement", PredefinedName.PN_OPCOMPLEMENT),
            new KnownName("op_Increment", PredefinedName.PN_OPINCREMENT),
            new KnownName("op_Decrement", PredefinedName.PN_OPDECREMENT),
            new KnownName("op_Addition", PredefinedName.PN_OPPLUS),
            new KnownName("op_Subtraction", PredefinedName.PN_OPMINUS),
            new KnownName("op_Multiply", PredefinedName.PN_OPMULTIPLY),
            new KnownName("op_Division", PredefinedName.PN_OPDIVISION),
            new KnownName("op_Modulus", PredefinedName.PN_OPMODULUS),
            new KnownName("op_ExclusiveOr", PredefinedName.PN_OPXOR),
            new KnownName("op_BitwiseAnd", PredefinedName.PN_OPBITWISEAND),
            new KnownName("op_BitwiseOr", PredefinedName.PN_OPBITWISEOR),
            new KnownName("op_LeftShift", PredefinedName.PN_OPLEFTSHIFT),
            new KnownName("op_RightShift", PredefinedName.PN_OPRIGHTSHIFT),
            new KnownName("op_Equals", PredefinedName.PN_OPEQUALS),
            new KnownName("op_Compare", PredefinedName.PN_OPCOMPARE),
            new KnownName("op_Equality", PredefinedName.PN_OPEQUALITY),
            new KnownName("op_Inequality", PredefinedName.PN_OPINEQUALITY),
            new KnownName("op_GreaterThan", PredefinedName.PN_OPGREATERTHAN),
            new KnownName("op_LessThan", PredefinedName.PN_OPLESSTHAN),
            new KnownName("op_GreaterThanOrEqual", PredefinedName.PN_OPGREATERTHANOREQUAL),
            new KnownName("op_LessThanOrEqual", PredefinedName.PN_OPLESSTHANOREQUAL),
            new KnownName("op_True", PredefinedName.PN_OPTRUE),
            new KnownName("op_False", PredefinedName.PN_OPFALSE),
            new KnownName("op_LogicalNot", PredefinedName.PN_OPNEGATION),
            new KnownName("Concat", PredefinedName.PN_CONCAT),
            new KnownName("Add", PredefinedName.PN_ADD),
            new KnownName("get_Length", PredefinedName.PN_GETLENGTH),
            new KnownName("get_Chars", PredefinedName.PN_GETCHARS),
            new KnownName("CreateDelegate", PredefinedName.PN_CREATEDELEGATE),
            new KnownName("FixedElementField", PredefinedName.PN_FIXEDELEMENT),
            new KnownName("HasValue", PredefinedName.PN_HASVALUE),
            new KnownName("get_HasValue", PredefinedName.PN_GETHASVALUE),
            new KnownName("Value", PredefinedName.PN_CAP_VALUE),
            new KnownName("get_Value", PredefinedName.PN_GETVALUE),
            new KnownName("GetValueOrDefault", PredefinedName.PN_GET_VALUE_OR_DEF),
            new KnownName("?", PredefinedName.PN_MISSING),
            new KnownName("<?>", PredefinedName.PN_MISSINGSYM),
            new KnownName("Lambda", PredefinedName.PN_LAMBDA),
            new KnownName("Parameter", PredefinedName.PN_PARAMETER),
            new KnownName("Constant", PredefinedName.PN_CONSTANT),
            new KnownName("Convert", PredefinedName.PN_CONVERT),
            new KnownName("ConvertChecked", PredefinedName.PN_CONVERTCHECKED),
            new KnownName("AddChecked", PredefinedName.PN_ADDCHECKED),
            new KnownName("Divide", PredefinedName.PN_DIVIDE),
            new KnownName("Modulo", PredefinedName.PN_MODULO),
            new KnownName("Multiply", PredefinedName.PN_MULTIPLY),
            new KnownName("MultiplyChecked", PredefinedName.PN_MULTIPLYCHECKED),
            new KnownName("Subtract", PredefinedName.PN_SUBTRACT),
            new KnownName("SubtractChecked", PredefinedName.PN_SUBTRACTCHECKED),
            new KnownName("And", PredefinedName.PN_AND),
            new KnownName("Or", PredefinedName.PN_OR),
            new KnownName("ExclusiveOr", PredefinedName.PN_EXCLUSIVEOR),
            new KnownName("LeftShift", PredefinedName.PN_LEFTSHIFT),
            new KnownName("RightShift", PredefinedName.PN_RIGHTSHIFT),
            new KnownName("AndAlso", PredefinedName.PN_ANDALSO),
            new KnownName("OrElse", PredefinedName.PN_ORELSE),
            new KnownName("Equal", PredefinedName.PN_EQUAL),
            new KnownName("NotEqual", PredefinedName.PN_NOTEQUAL),
            new KnownName("GreaterThanOrEqual", PredefinedName.PN_GREATERTHANOREQUAL),
            new KnownName("GreaterThan", PredefinedName.PN_GREATERTHAN),
            new KnownName("LessThan", PredefinedName.PN_LESSTHAN),
            new KnownName("LessThanOrEqual", PredefinedName.PN_LESSTHANOREQUAL),
            new KnownName("ArrayIndex", PredefinedName.PN_ARRAYINDEX),
            new KnownName("Assign", PredefinedName.PN_ASSIGN),
            new KnownName("Condition", PredefinedName.PN_CONDITION),
            new KnownName("Field", PredefinedName.PN_CAP_FIELD),
            new KnownName("Call", PredefinedName.PN_CALL),
            new KnownName("New", PredefinedName.PN_NEW),
            new KnownName("Quote", PredefinedName.PN_QUOTE),
            new KnownName("ArrayLength", PredefinedName.PN_ARRAYLENGTH),
            new KnownName("UnaryPlus", PredefinedName.PN_PLUS),
            new KnownName("Negate", PredefinedName.PN_NEGATE),
            new KnownName("NegateChecked", PredefinedName.PN_NEGATECHECKED),
            new KnownName("Not", PredefinedName.PN_NOT),
            new KnownName("NewArrayInit", PredefinedName.PN_NEWARRAYINIT),
            new KnownName("Property", PredefinedName.PN_EXPRESSION_PROPERTY),
            new KnownName("AddEventHandler", PredefinedName.PN_ADDEVENTHANDLER),
            new KnownName("RemoveEventHandler", PredefinedName.PN_REMOVEEVENTHANDLER),
            new KnownName("InvocationList", PredefinedName.PN_INVOCATIONLIST),
            new KnownName("GetOrCreateEventRegistrationTokenTable", PredefinedName.PN_GETORCREATEEVENTREGISTRATIONTOKENTABLE)
        };

        private static readonly KnownName[] s_otherNames = new KnownName[] {
            new KnownName("true"),
            new KnownName("false"),
            new KnownName("null"),
            new KnownName("base"),
            new KnownName("this"),
            new KnownName("explicit"),
            new KnownName("implicit"),
            new KnownName("__arglist"),
            new KnownName("__makeref"),
            new KnownName("__reftype"),
            new KnownName("__refvalue"),
            new KnownName("as"),
            new KnownName("checked"),
            new KnownName("is"),
            new KnownName("typeof"),
            new KnownName("unchecked"),
            new KnownName("void"),
        };
    }
}
