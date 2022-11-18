// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.CSharp.RuntimeBinder.Semantics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Errors
{
    internal struct UserStringBuilder
    {
        private StringBuilder _strBuilder;

        private void BeginString()
        {
            Debug.Assert(_strBuilder == null || _strBuilder.Length == 0);
            _strBuilder ??= new StringBuilder();
        }

        private string EndString()
        {
            Debug.Assert(_strBuilder != null);
            string s = _strBuilder.ToString();
            _strBuilder.Clear();
            return s;
        }

        private static string ErrSK(SYMKIND sk)
        {
            MessageID id;
            switch (sk)
            {
                case SYMKIND.SK_MethodSymbol:
                    id = MessageID.SK_METHOD;
                    break;
                case SYMKIND.SK_AggregateSymbol:
                    id = MessageID.SK_CLASS;
                    break;
                case SYMKIND.SK_NamespaceSymbol:
                    id = MessageID.SK_NAMESPACE;
                    break;
                case SYMKIND.SK_FieldSymbol:
                    id = MessageID.SK_FIELD;
                    break;
                case SYMKIND.SK_LocalVariableSymbol:
                    id = MessageID.SK_VARIABLE;
                    break;
                case SYMKIND.SK_PropertySymbol:
                    id = MessageID.SK_PROPERTY;
                    break;
                case SYMKIND.SK_EventSymbol:
                    id = MessageID.SK_EVENT;
                    break;
                case SYMKIND.SK_TypeParameterSymbol:
                    id = MessageID.SK_TYVAR;
                    break;
                default:
                    Debug.Fail("impossible sk");
                    id = MessageID.SK_UNKNOWN;
                    break;
            }

            return ErrId(id);
        }
        /*
         * Create a fill-in string describing a parameter list.
         * Does NOT include ()
         */

        private void ErrAppendParamList(TypeArray @params, bool isParamArray)
        {
            if (null == @params)
                return;

            for (int i = 0; i < @params.Count; i++)
            {
                if (i > 0)
                {
                    ErrAppendString(", ");
                }

                if (isParamArray && i == @params.Count - 1)
                {
                    ErrAppendString("params ");
                }

                // parameter type name
                ErrAppendType(@params[i], null);
            }
        }

        private void ErrAppendString(string str)
        {
            _strBuilder.Append(str);
        }

        private void ErrAppendChar(char ch)
        {
            _strBuilder.Append(ch);
        }

        private void ErrAppendPrintf(string format, params object[] args)
        {
            ErrAppendString(string.Format(CultureInfo.InvariantCulture, format, args));
        }
        private void ErrAppendName(Name name)
        {
            if (name == NameManager.GetPredefinedName(PredefinedName.PN_INDEXERINTERNAL))
            {
                ErrAppendString("this");
            }
            else
            {
                ErrAppendString(name.Text);
            }
        }

        private void ErrAppendParentSym(Symbol sym, SubstContext pctx)
        {
            ErrAppendParentCore(sym.parent, pctx);
        }

        private void ErrAppendParentCore(Symbol parent, SubstContext pctx)
        {
            if (parent == null || parent == NamespaceSymbol.Root)
            {
                return;
            }

            if (pctx != null && !pctx.IsNop && parent is AggregateSymbol agg && 0 != agg.GetTypeVarsAll().Count)
            {
                CType pType = TypeManager.SubstType(agg.getThisType(), pctx);
                ErrAppendType(pType, null);
            }
            else
            {
                ErrAppendSym(parent, null);
            }
            ErrAppendChar('.');
        }

        private void ErrAppendTypeParameters(TypeArray @params, SubstContext pctx)
        {
            if (@params != null && @params.Count != 0)
            {
                ErrAppendChar('<');
                ErrAppendType(@params[0], pctx);
                for (int i = 1; i < @params.Count; i++)
                {
                    ErrAppendString(",");
                    ErrAppendType(@params[i], pctx);
                }
                ErrAppendChar('>');
            }
        }

        private void ErrAppendMethod(MethodSymbol meth, SubstContext pctx, bool fArgs)
        {
            if (meth.IsExpImpl() && meth.swtSlot)
            {
                ErrAppendParentSym(meth, pctx);

                // Get the type args from the explicit impl type and substitute using pctx (if there is one).
                SubstContext ctx = new SubstContext(TypeManager.SubstType(meth.swtSlot.GetType(), pctx));
                ErrAppendSym(meth.swtSlot.Sym, ctx, fArgs);

                // args already added
                return;
            }

            MethodKindEnum methodKind = meth.MethKind;
            switch (methodKind)
            {
                case MethodKindEnum.PropAccessor:
                    PropertySymbol prop = meth.getProperty();

                    // this includes the parent class
                    ErrAppendSym(prop, pctx);

                    // add accessor name
                    if (prop.GetterMethod == meth)
                    {
                        ErrAppendString(".get");
                    }
                    else
                    {
                        Debug.Assert(meth == prop.SetterMethod);
                        ErrAppendString(".set");
                    }

                    // args already added
                    return;

                case MethodKindEnum.EventAccessor:
                    EventSymbol @event = meth.getEvent();

                    // this includes the parent class
                    ErrAppendSym(@event, pctx);

                    // add accessor name
                    if (@event.methAdd == meth)
                    {
                        ErrAppendString(".add");
                    }
                    else
                    {
                        Debug.Assert(meth == @event.methRemove);
                        ErrAppendString(".remove");
                    }

                    // args already added
                    return;
            }

            ErrAppendParentSym(meth, pctx);
            switch (methodKind)
            {
                case MethodKindEnum.Constructor:
                    // Use the name of the parent class instead of the name "<ctor>".
                    ErrAppendName(meth.getClass().name);
                    break;

                case MethodKindEnum.Destructor:
                    // Use the name of the parent class instead of the name "Finalize".
                    ErrAppendChar('~');
                    goto case MethodKindEnum.Constructor;

                case MethodKindEnum.ExplicitConv:
                    ErrAppendString("explicit");
                    goto convOperatorName;

                case MethodKindEnum.ImplicitConv:
                    ErrAppendString("implicit");

                convOperatorName:
                    ErrAppendString(" operator ");

                    // destination type name
                    ErrAppendType(meth.RetType, pctx);
                    break;

                default:
                    if (meth.isOperator)
                    {
                        // handle user defined operators
                        // map from CLS predefined names to "operator <X>"
                        ErrAppendString("operator ");
                        ErrAppendString(Operators.OperatorOfMethodName(meth.name));
                    }
                    else if (!meth.IsExpImpl())
                    {
                        // regular method
                        ErrAppendName(meth.name);
                    }

                    break;
            }

            ErrAppendTypeParameters(meth.typeVars, pctx);

            if (fArgs)
            {
                // append argument types
                ErrAppendChar('(');
                ErrAppendParamList(TypeManager.SubstTypeArray(meth.Params, pctx), meth.isParamArray);
                ErrAppendChar(')');
            }
        }

        private void ErrAppendIndexer(IndexerSymbol indexer, SubstContext pctx)
        {
            ErrAppendString("this[");
            ErrAppendParamList(TypeManager.SubstTypeArray(indexer.Params, pctx), indexer.isParamArray);
            ErrAppendChar(']');
        }
        private void ErrAppendProperty(PropertySymbol prop, SubstContext pctx)
        {
            ErrAppendParentSym(prop, pctx);
            if (prop.IsExpImpl())
            {
                if (prop.swtSlot.Sym != null)
                {
                    SubstContext ctx = new SubstContext(TypeManager.SubstType(prop.swtSlot.GetType(), pctx));
                    ErrAppendSym(prop.swtSlot.Sym, ctx);
                }
                else if (prop is IndexerSymbol indexer)
                {
                    ErrAppendChar('.');
                    ErrAppendIndexer(indexer, pctx);
                }
            }
            else if (prop is IndexerSymbol indexer)
            {
                ErrAppendIndexer(indexer, pctx);
            }
            else
            {
                ErrAppendName(prop.name);
            }
        }

        private void ErrAppendId(MessageID id) => ErrAppendString(ErrId(id));

        /*
         * Create a fill-in string describing a symbol.
         */
        private void ErrAppendSym(Symbol sym, SubstContext pctx)
        {
            ErrAppendSym(sym, pctx, true);
        }

        private void ErrAppendSym(Symbol sym, SubstContext pctx, bool fArgs)
        {
            switch (sym.getKind())
            {
                case SYMKIND.SK_AggregateSymbol:
                    {
                        // Check for a predefined class with a special "nice" name for
                        // error reported.
                        string text = PredefinedTypes.GetNiceName(sym as AggregateSymbol);
                        if (text != null)
                        {
                            // Found a nice name.
                            ErrAppendString(text);
                        }
                        else
                        {
                            ErrAppendParentSym(sym, pctx);
                            ErrAppendName(sym.name);
                            ErrAppendTypeParameters(((AggregateSymbol)sym).GetTypeVars(), pctx);
                        }
                        break;
                    }

                case SYMKIND.SK_MethodSymbol:
                    ErrAppendMethod((MethodSymbol)sym, pctx, fArgs);
                    break;

                case SYMKIND.SK_PropertySymbol:
                    ErrAppendProperty((PropertySymbol)sym, pctx);
                    break;

                case SYMKIND.SK_EventSymbol:
                    break;

                case SYMKIND.SK_NamespaceSymbol:
                    if (sym == NamespaceSymbol.Root)
                    {
                        ErrAppendId(MessageID.GlobalNamespace);
                    }
                    else
                    {
                        ErrAppendParentSym(sym, null);
                        ErrAppendName(sym.name);
                    }
                    break;

                case SYMKIND.SK_FieldSymbol:
                    ErrAppendParentSym(sym, pctx);
                    ErrAppendName(sym.name);
                    break;

                case SYMKIND.SK_TypeParameterSymbol:
                    if (null == sym.name)
                    {
                        var parSym = (TypeParameterSymbol)sym;
                        // It's a standard type variable.
                        if (parSym.IsMethodTypeParameter())
                            ErrAppendChar('!');
                        ErrAppendChar('!');
                        ErrAppendPrintf("{0}", parSym.GetIndexInTotalParameters());
                    }
                    else
                        ErrAppendName(sym.name);
                    break;

                case SYMKIND.SK_LocalVariableSymbol:
                    // Generate symbol name.
                    ErrAppendName(sym.name);
                    break;

                default:
                    // Shouldn't happen.
                    Debug.Fail($"Bad symbol kind: {sym.getKind()}");
                    break;
            }
        }

        private void ErrAppendType(CType pType, SubstContext pctx)
        {
            if (pctx != null && !pctx.IsNop)
            {
                pType = TypeManager.SubstType(pType, pctx);
            }

            switch (pType.TypeKind)
            {
                case TypeKind.TK_AggregateType:
                    {
                        AggregateType pAggType = (AggregateType)pType;

                        // Check for a predefined class with a special "nice" name for
                        // error reported.
                        string text = PredefinedTypes.GetNiceName(pAggType.OwningAggregate);
                        if (text != null)
                        {
                            // Found a nice name.
                            ErrAppendString(text);
                        }
                        else
                        {
                            if (pAggType.OuterType != null)
                            {
                                ErrAppendType(pAggType.OuterType, null);
                                ErrAppendChar('.');
                            }
                            else
                            {
                                // In a namespace.
                                ErrAppendParentSym(pAggType.OwningAggregate, null);
                            }

                            ErrAppendName(pAggType.OwningAggregate.name);
                        }

                        ErrAppendTypeParameters(pAggType.TypeArgsThis, null);
                        break;
                    }

                case TypeKind.TK_TypeParameterType:
                    TypeParameterType tpType = (TypeParameterType)pType;
                    if (null == tpType.Name)
                    {
                        // It's a standard type variable.
                        if (tpType.IsMethodTypeParameter)
                        {
                            ErrAppendChar('!');
                        }

                        ErrAppendChar('!');
                        ErrAppendPrintf("{0}", tpType.IndexInTotalParameters);
                    }
                    else
                    {
                        ErrAppendName(tpType.Name);
                    }

                    break;

                case TypeKind.TK_NullType:
                    // Load the string "<null>".
                    ErrAppendId(MessageID.NULL);
                    break;

                case TypeKind.TK_MethodGroupType:
                    ErrAppendId(MessageID.MethodGroup);
                    break;

                case TypeKind.TK_ArgumentListType:
                    ErrAppendString(TokenFacts.GetText(TokenKind.ArgList));
                    break;

                case TypeKind.TK_ArrayType:
                    {
                        CType elementType = ((ArrayType)pType).BaseElementType;

                        Debug.Assert(elementType != null, "No element type");

                        ErrAppendType(elementType, null);

                        for (elementType = pType;
                                elementType is ArrayType arrType;
                                elementType = arrType.ElementType)
                        {
                            int rank = arrType.Rank;

                            // Add [] with (rank-1) commas inside
                            ErrAppendChar('[');

                            // known rank.
                            if (rank == 1)
                            {
                                if (!arrType.IsSZArray)
                                {
                                    ErrAppendChar('*');
                                }
                            }
                            else
                            {
                                for (int i = rank; i > 1; --i)
                                {
                                    ErrAppendChar(',');
                                }
                            }

                            ErrAppendChar(']');
                        }
                        break;
                    }

                case TypeKind.TK_VoidType:
                    ErrAppendName(NameManager.GetPredefinedName(PredefinedName.PN_VOID));
                    break;

                case TypeKind.TK_ParameterModifierType:
                    ParameterModifierType mod = (ParameterModifierType)pType;
                    // add ref or out
                    ErrAppendString(mod.IsOut ? "out " : "ref ");

                    // add base type name
                    ErrAppendType(mod.ParameterType, null);
                    break;

                case TypeKind.TK_PointerType:
                    // Generate the base type.
                    ErrAppendType(((PointerType)pType).ReferentType, null);
                    {
                        // add the trailing *
                        ErrAppendChar('*');
                    }
                    break;

                case TypeKind.TK_NullableType:
                    ErrAppendType(((NullableType)pType).UnderlyingType, null);
                    ErrAppendChar('?');
                    break;

                default:
                    // Shouldn't happen.
                    Debug.Fail("Bad type kind");
                    break;
            }
        }

        // Returns true if the argument could be converted to a string.
        public bool ErrArgToString(out string psz, ErrArg parg, out bool fUserStrings)
        {
            fUserStrings = false;
            psz = null;
            bool result = true;

            switch (parg.eak)
            {
                case ErrArgKind.SymKind:
                    psz = ErrSK(parg.sk);
                    break;
                case ErrArgKind.Type:
                    BeginString();
                    ErrAppendType(parg.pType, null);
                    psz = EndString();
                    fUserStrings = true;
                    break;
                case ErrArgKind.Sym:
                    BeginString();
                    ErrAppendSym(parg.sym, null);
                    psz = EndString();
                    fUserStrings = true;
                    break;
                case ErrArgKind.Name:
                    if (parg.name == NameManager.GetPredefinedName(PredefinedName.PN_INDEXERINTERNAL))
                    {
                        psz = "this";
                    }
                    else
                    {
                        psz = parg.name.Text;
                    }
                    break;

                case ErrArgKind.Str:
                    psz = parg.psz;
                    break;
                case ErrArgKind.SymWithType:
                    {
                        SubstContext ctx = new SubstContext(parg.swtMemo.ats, null);
                        BeginString();
                        ErrAppendSym(parg.swtMemo.sym, ctx, true);
                        psz = EndString();
                        fUserStrings = true;
                        break;
                    }

                case ErrArgKind.MethWithInst:
                    {
                        SubstContext ctx = new SubstContext(parg.mpwiMemo.ats, parg.mpwiMemo.typeArgs);
                        BeginString();
                        ErrAppendSym(parg.mpwiMemo.sym, ctx, true);
                        psz = EndString();
                        fUserStrings = true;
                        break;
                    }
                default:
                    result = false;
                    break;
            }

            return result;
        }

        private static string ErrId(MessageID id) => ErrorFacts.GetMessage(id);
    }
}
