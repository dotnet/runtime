// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.CSharp.RuntimeBinder.Semantics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Errors
{
    internal class UserStringBuilder
    {
        protected bool fHadUndisplayableStringInError;
        protected bool m_buildingInProgress;
        protected GlobalSymbolContext m_globalSymbols;
        protected StringBuilder m_strBuilder;

        public UserStringBuilder(
            GlobalSymbolContext globalSymbols)
        {
            Debug.Assert(globalSymbols != null);
            fHadUndisplayableStringInError = false;
            m_buildingInProgress = false;
            m_globalSymbols = globalSymbols;
        }

        protected void BeginString()
        {
            Debug.Assert(!m_buildingInProgress);
            m_buildingInProgress = true;
            m_strBuilder = new StringBuilder();
        }
        protected void EndString(out string s)
        {
            Debug.Assert(m_buildingInProgress);
            m_buildingInProgress = false;
            s = m_strBuilder.ToString();
            m_strBuilder = null;
        }


        public bool HadUndisplayableString()
        {
            return fHadUndisplayableStringInError;
        }

        public void ResetUndisplayableStringFlag()
        {
            fHadUndisplayableStringInError = false;
        }

        protected void ErrSK(out string psz, SYMKIND sk)
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
                case SYMKIND.SK_AssemblyQualifiedNamespaceSymbol:
                    Debug.Assert(false, "Illegal sk");
                    id = MessageID.SK_ALIAS;
                    break;
                default:
                    Debug.Assert(false, "impossible sk");
                    id = MessageID.SK_UNKNOWN;
                    break;
            }

            ErrId(out psz, id);
        }
        /*
         * Create a fill-in string describing a parameter list.
         * Does NOT include ()
         */
        protected void ErrAppendParamList(TypeArray @params, bool isVarargs, bool isParamArray)
        {
            if (null == @params)
                return;

            for (int i = 0; i < @params.size; i++)
            {
                if (i > 0)
                {
                    ErrAppendString(", ");
                }

                if (isParamArray && i == @params.size - 1)
                {
                    ErrAppendString("params ");
                }

                // parameter type name
                ErrAppendType(@params.Item(i), null);
            }

            if (isVarargs)
            {
                if (@params.size != 0)
                {
                    ErrAppendString(", ");
                }

                ErrAppendString("...");
            }
        }

        public void ErrAppendString(string str)
        {
            m_strBuilder.Append(str);
        }
        public void ErrAppendChar(char ch)
        {
            m_strBuilder.Append(ch);
        }

        public void ErrAppendPrintf(string format, params object[] args)
        {
            ErrAppendString(String.Format(CultureInfo.InvariantCulture, format, args));
        }
        public void ErrAppendName(Name name)
        {
            CheckDisplayableName(name);

            if (name == GetNameManager().GetPredefName(PredefinedName.PN_INDEXERINTERNAL))
            {
                ErrAppendString("this");
            }
            else
            {
                ErrAppendString(name.Text);
            }
        }

        protected void ErrAppendMethodParentSym(MethodSymbol sym, SubstContext pcxt, out TypeArray substMethTyParams)
        {
            substMethTyParams = null;
            ErrAppendParentSym(sym, pcxt);
        }

        protected void ErrAppendParentSym(Symbol sym, SubstContext pctx)
        {
            ErrAppendParentCore(sym.parent, pctx);
        }
        protected void ErrAppendParentType(CType pType, SubstContext pctx)
        {
            if (pType.IsErrorType())
            {
                if (pType.AsErrorType().HasTypeParent())
                {
                    ErrAppendType(pType.AsErrorType().GetTypeParent(), null);
                    ErrAppendChar('.');
                }
                else
                {
                    ErrAppendParentCore(pType.AsErrorType().GetNSParent(), pctx);
                }
            }
            else if (pType.IsAggregateType())
            {
                ErrAppendParentCore(pType.AsAggregateType().GetOwningAggregate(), pctx);
            }
            else if (pType.GetBaseOrParameterOrElementType() != null)
            {
                ErrAppendType(pType.GetBaseOrParameterOrElementType(), null);
                ErrAppendChar('.');
            }
        }
        protected void ErrAppendParentCore(Symbol parent, SubstContext pctx)
        {
            if (null == parent)
                return;

            if (parent == getBSymmgr().GetRootNS())
                return;

            if (pctx != null && !pctx.FNop() && parent.IsAggregateSymbol() && 0 != parent.AsAggregateSymbol().GetTypeVarsAll().size)
            {
                CType pType = GetTypeManager().SubstType(parent.AsAggregateSymbol().getThisType(), pctx);
                ErrAppendType(pType, null);
            }
            else
            {
                ErrAppendSym(parent, null);
            }
            ErrAppendChar('.');
        }
        protected void ErrAppendTypeParameters(TypeArray @params, SubstContext pctx, bool forClass)
        {
            if (@params != null && @params.size != 0)
            {
                ErrAppendChar('<');
                ErrAppendType(@params.Item(0), pctx);
                for (int i = 1; i < @params.size; i++)
                {
                    ErrAppendString(",");
                    ErrAppendType(@params.Item(i), pctx);
                }
                ErrAppendChar('>');
            }
        }
        protected void ErrAppendMethod(MethodSymbol meth, SubstContext pctx, bool fArgs)
        {
            if (meth.IsExpImpl() && meth.swtSlot)
            {
                ErrAppendParentSym(meth, pctx);

                // Get the type args from the explicit impl type and substitute using pctx (if there is one).
                SubstContext ctx = new SubstContext(GetTypeManager().SubstType(meth.swtSlot.GetType(), pctx).AsAggregateType());
                ErrAppendSym(meth.swtSlot.Sym, ctx, fArgs);

                // args already added
                return;
            }

            if (meth.isPropertyAccessor())
            {
                PropertySymbol prop = meth.getProperty();

                // this includes the parent class
                ErrAppendSym(prop, pctx);

                // add accessor name
                if (prop.methGet == meth)
                {
                    ErrAppendString(".get");
                }
                else
                {
                    Debug.Assert(meth == prop.methSet);
                    ErrAppendString(".set");
                }

                // args already added
                return;
            }

            if (meth.isEventAccessor())
            {
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

            TypeArray replacementTypeArray = null;
            ErrAppendMethodParentSym(meth, pctx, out replacementTypeArray);
            if (meth.IsConstructor())
            {
                // Use the name of the parent class instead of the name "<ctor>".
                ErrAppendName(meth.getClass().name);
            }
            else if (meth.IsDestructor())
            {
                // Use the name of the parent class instead of the name "Finalize".
                ErrAppendChar('~');
                ErrAppendName(meth.getClass().name);
            }
            else if (meth.isConversionOperator())
            {
                // implicit/explicit
                ErrAppendString(meth.isImplicit() ? "implicit" : "explicit");
                ErrAppendString(" operator ");

                // destination type name
                ErrAppendType(meth.RetType, pctx);
            }
            else if (meth.isOperator)
            {
                // handle user defined operators
                // map from CLS predefined names to "operator <X>"
                ErrAppendString("operator ");

                //
                // This is kinda slow, but the alternative is to add bits to methsym.
                //
                string operatorName;
                OperatorKind op = Operators.OperatorOfMethodName(GetNameManager(), meth.name);
                if (Operators.HasDisplayName(op))
                {
                    operatorName = Operators.GetDisplayName(op);
                }
                else
                {
                    //
                    // either equals or compare
                    //
                    if (meth.name == GetNameManager().GetPredefName(PredefinedName.PN_OPEQUALS))
                    {
                        operatorName = "equals";
                    }
                    else
                    {
                        Debug.Assert(meth.name == GetNameManager().GetPredefName(PredefinedName.PN_OPCOMPARE));
                        operatorName = "compare";
                    }
                }
                ErrAppendString(operatorName);
            }
            else if (meth.IsExpImpl())
            {
                if (meth.errExpImpl != null)
                    ErrAppendType(meth.errExpImpl, pctx, fArgs);
            }
            else
            {
                // regular method
                ErrAppendName(meth.name);
            }

            if (null == replacementTypeArray)
            {
                ErrAppendTypeParameters(meth.typeVars, pctx, false);
            }

            if (fArgs)
            {
                // append argument types
                ErrAppendChar('(');

                if (!meth.computeCurrentBogusState())
                {
                    ErrAppendParamList(GetTypeManager().SubstTypeArray(meth.Params, pctx), meth.isVarargs, meth.isParamArray);
                }

                ErrAppendChar(')');
            }
        }
        protected void ErrAppendIndexer(IndexerSymbol indexer, SubstContext pctx)
        {
            ErrAppendString("this[");
            ErrAppendParamList(GetTypeManager().SubstTypeArray(indexer.Params, pctx), false, indexer.isParamArray);
            ErrAppendChar(']');
        }
        protected void ErrAppendProperty(PropertySymbol prop, SubstContext pctx)
        {
            ErrAppendParentSym(prop, pctx);
            if (prop.IsExpImpl() && prop.swtSlot.Sym != null)
            {
                SubstContext ctx = new SubstContext(GetTypeManager().SubstType(prop.swtSlot.GetType(), pctx).AsAggregateType());
                ErrAppendSym(prop.swtSlot.Sym, ctx);
            }
            else if (prop.IsExpImpl())
            {
                if (prop.errExpImpl != null)
                    ErrAppendType(prop.errExpImpl, pctx, false);
                if (prop.isIndexer())
                {
                    ErrAppendChar('.');
                    ErrAppendIndexer(prop.AsIndexerSymbol(), pctx);
                }
            }
            else if (prop.isIndexer())
            {
                ErrAppendIndexer(prop.AsIndexerSymbol(), pctx);
            }
            else
            {
                ErrAppendName(prop.name);
            }
        }
        protected void ErrAppendEvent(EventSymbol @event, SubstContext pctx)
        {
        }
        public void ErrAppendId(MessageID id)
        {
            string str;
            ErrId(out str, id);
            ErrAppendString(str);
        }

        /*
         * Create a fill-in string describing a symbol.
         */
        public void ErrAppendSym(Symbol sym, SubstContext pctx)
        {
            ErrAppendSym(sym, pctx, true);
        }
        public void ErrAppendSym(Symbol sym, SubstContext pctx, bool fArgs)
        {
            switch (sym.getKind())
            {
                case SYMKIND.SK_NamespaceDeclaration:
                    // for namespace declarations just convert the namespace
                    ErrAppendSym(sym.AsNamespaceDeclaration().NameSpace(), null);
                    break;

                case SYMKIND.SK_GlobalAttributeDeclaration:
                    ErrAppendName(sym.name);
                    break;

                case SYMKIND.SK_AggregateDeclaration:
                    ErrAppendSym(sym.AsAggregateDeclaration().Agg(), pctx);
                    break;

                case SYMKIND.SK_AggregateSymbol:
                    {
                        // Check for a predefined class with a special "nice" name for
                        // error reported.
                        string text = PredefinedTypes.GetNiceName(sym.AsAggregateSymbol());
                        if (text != null)
                        {
                            // Found a nice name.
                            ErrAppendString(text);
                        }
                        else if (sym.AsAggregateSymbol().IsAnonymousType())
                        {
                            ErrAppendId(MessageID.AnonymousType);
                            break;
                        }
                        else
                        {
                            ErrAppendParentSym(sym, pctx);
                            ErrAppendName(sym.name);
                            ErrAppendTypeParameters(sym.AsAggregateSymbol().GetTypeVars(), pctx, true);
                        }
                        break;
                    }

                case SYMKIND.SK_MethodSymbol:
                    ErrAppendMethod(sym.AsMethodSymbol(), pctx, fArgs);
                    break;

                case SYMKIND.SK_PropertySymbol:
                    ErrAppendProperty(sym.AsPropertySymbol(), pctx);
                    break;

                case SYMKIND.SK_EventSymbol:
                    ErrAppendEvent(sym.AsEventSymbol(), pctx);
                    break;

                case SYMKIND.SK_AssemblyQualifiedNamespaceSymbol:
                case SYMKIND.SK_NamespaceSymbol:
                    if (sym == getBSymmgr().GetRootNS())
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
                        // It's a standard type variable.
                        if (sym.AsTypeParameterSymbol().IsMethodTypeParameter())
                            ErrAppendChar('!');
                        ErrAppendChar('!');
                        ErrAppendPrintf("{0}", sym.AsTypeParameterSymbol().GetIndexInTotalParameters());
                    }
                    else
                        ErrAppendName(sym.name);
                    break;

                case SYMKIND.SK_LocalVariableSymbol:
                case SYMKIND.SK_LabelSymbol:
                case SYMKIND.SK_TransparentIdentifierMemberSymbol:
                    // Generate symbol name.
                    ErrAppendName(sym.name);
                    break;

                case SYMKIND.SK_Scope:
                case SYMKIND.SK_LambdaScope:
                default:
                    // Shouldn't happen.
                    Debug.Assert(false, "Bad symbol kind");
                    break;
            }
        }

        public void ErrAppendType(CType pType, SubstContext pCtx)
        {
            ErrAppendType(pType, pCtx, true);
        }

        public void ErrAppendType(CType pType, SubstContext pctx, bool fArgs)
        {
            if (pctx != null)
            {
                if (!pctx.FNop())
                {
                    pType = GetTypeManager().SubstType(pType, pctx);
                }
                // We shouldn't use the SubstContext again so set it to NULL.
                pctx = null;
            }

            switch (pType.GetTypeKind())
            {
                case TypeKind.TK_AggregateType:
                    {
                        AggregateType pAggType = pType.AsAggregateType();

                        // Check for a predefined class with a special "nice" name for
                        // error reported.
                        string text = PredefinedTypes.GetNiceName(pAggType.getAggregate());
                        if (text != null)
                        {
                            // Found a nice name.
                            ErrAppendString(text);
                        }
                        else if (pAggType.getAggregate().IsAnonymousType())
                        {
                            ErrAppendPrintf("AnonymousType#{0}", GetTypeID(pAggType));
                            break;
                        }
                        else
                        {
                            if (pAggType.outerType != null)
                            {
                                ErrAppendType(pAggType.outerType, pctx);
                                ErrAppendChar('.');
                            }
                            else
                            {
                                // In a namespace.
                                ErrAppendParentSym(pAggType.getAggregate(), pctx);
                            }
                            ErrAppendName(pAggType.getAggregate().name);
                        }
                        ErrAppendTypeParameters(pAggType.GetTypeArgsThis(), pctx, true);
                        break;
                    }

                case TypeKind.TK_TypeParameterType:
                    if (null == pType.GetName())
                    {
                        // It's a standard type variable.
                        if (pType.AsTypeParameterType().IsMethodTypeParameter())
                        {
                            ErrAppendChar('!');
                        }
                        ErrAppendChar('!');
                        ErrAppendPrintf("{0}", pType.AsTypeParameterType().GetIndexInTotalParameters());
                    }
                    else
                    {
                        ErrAppendName(pType.GetName());
                    }
                    break;

                case TypeKind.TK_ErrorType:
                    if (pType.AsErrorType().HasParent())
                    {
                        Debug.Assert(pType.AsErrorType().nameText != null && pType.AsErrorType().typeArgs != null);
                        ErrAppendParentType(pType, pctx);
                        ErrAppendName(pType.AsErrorType().nameText);
                        ErrAppendTypeParameters(pType.AsErrorType().typeArgs, pctx, true);
                    }
                    else
                    {
                        // Load the string "<error>".
                        Debug.Assert(null == pType.AsErrorType().typeArgs);
                        ErrAppendId(MessageID.ERRORSYM);
                    }
                    break;

                case TypeKind.TK_NullType:
                    // Load the string "<null>".
                    ErrAppendId(MessageID.NULL);
                    break;

                case TypeKind.TK_OpenTypePlaceholderType:
                    // Leave blank.
                    break;

                case TypeKind.TK_BoundLambdaType:
                    ErrAppendId(MessageID.AnonMethod);
                    break;

                case TypeKind.TK_UnboundLambdaType:
                    ErrAppendId(MessageID.Lambda);
                    break;

                case TypeKind.TK_MethodGroupType:
                    ErrAppendId(MessageID.MethodGroup);
                    break;

                case TypeKind.TK_ArgumentListType:
                    ErrAppendString(TokenFacts.GetText(TokenKind.ArgList));
                    break;

                case TypeKind.TK_ArrayType:
                    {
                        CType elementType = pType.AsArrayType().GetBaseElementType();

                        if (null == elementType)
                        {
                            Debug.Assert(false, "No element type");
                            break;
                        }

                        ErrAppendType(elementType, pctx);

                        for (elementType = pType;
                                elementType != null && elementType.IsArrayType();
                                elementType = elementType.AsArrayType().GetElementType())
                        {
                            int rank = elementType.AsArrayType().rank;

                            // Add [] with (rank-1) commas inside
                            ErrAppendChar('[');

#if ! CSEE
                            // known rank.
                            if (rank > 1)
                            {
                                ErrAppendChar('*');
                            }
#endif

                            for (int i = rank; i > 1; --i)
                            {
                                ErrAppendChar(',');
#if ! CSEE

                                ErrAppendChar('*');
#endif
                            }

                            ErrAppendChar(']');
                        }
                        break;
                    }

                case TypeKind.TK_VoidType:
                    ErrAppendName(GetNameManager().Lookup(TokenFacts.GetText(TokenKind.Void)));
                    break;

                case TypeKind.TK_ParameterModifierType:
                    // add ref or out
                    ErrAppendString(pType.AsParameterModifierType().isOut ? "out " : "ref ");

                    // add base type name
                    ErrAppendType(pType.AsParameterModifierType().GetParameterType(), pctx);
                    break;

                case TypeKind.TK_PointerType:
                    // Generate the base type.
                    ErrAppendType(pType.AsPointerType().GetReferentType(), pctx);
                    {
                        // add the trailing *
                        ErrAppendChar('*');
                    }
                    break;

                case TypeKind.TK_NullableType:
                    ErrAppendType(pType.AsNullableType().GetUnderlyingType(), pctx);
                    ErrAppendChar('?');
                    break;

                case TypeKind.TK_NaturalIntegerType:
                default:
                    // Shouldn't happen.
                    Debug.Assert(false, "Bad type kind");
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
                case ErrArgKind.Ids:
                    ErrId(out psz, parg.ids);
                    break;
                case ErrArgKind.SymKind:
                    ErrSK(out psz, parg.sk);
                    break;
                case ErrArgKind.Type:
                    BeginString();
                    ErrAppendType(parg.pType, null);
                    EndString(out psz);
                    fUserStrings = true;
                    break;
                case ErrArgKind.Sym:
                    BeginString();
                    ErrAppendSym(parg.sym, null);
                    EndString(out psz);
                    fUserStrings = true;
                    break;
                case ErrArgKind.Name:
                    if (parg.name == GetNameManager().GetPredefinedName(PredefinedName.PN_INDEXERINTERNAL))
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
                case ErrArgKind.PredefName:
                    BeginString();
                    ErrAppendName(GetNameManager().GetPredefName(parg.pdn));
                    EndString(out psz);
                    break;
                case ErrArgKind.SymWithType:
                    {
                        SubstContext ctx = new SubstContext(parg.swtMemo.ats, null);
                        BeginString();
                        ErrAppendSym(parg.swtMemo.sym, ctx, true);
                        EndString(out psz);
                        fUserStrings = true;
                        break;
                    }

                case ErrArgKind.MethWithInst:
                    {
                        SubstContext ctx = new SubstContext(parg.mpwiMemo.ats, parg.mpwiMemo.typeArgs);
                        BeginString();
                        ErrAppendSym(parg.mpwiMemo.sym, ctx, true);
                        EndString(out psz);
                        fUserStrings = true;
                        break;
                    }
                default:
                    result = false;
                    break;
            }

            return result;
        }
        protected bool IsDisplayableName(Name name)
        {
            return name != GetNameManager().GetPredefName(PredefinedName.PN_MISSING);
        }
        protected void CheckDisplayableName(Name name)
        {
            if (!IsDisplayableName(name))
            {
                fHadUndisplayableStringInError = true;
            }
        }

        protected NameManager GetNameManager()
        {
            return m_globalSymbols.GetNameManager();
        }
        protected TypeManager GetTypeManager()
        {
            return m_globalSymbols.GetTypes();
        }
        protected BSYMMGR getBSymmgr()
        {
            return m_globalSymbols.GetGlobalSymbols();
        }
        protected int GetTypeID(CType type)
        {
            return 0;
        }
        public void ErrId(out string s, MessageID id)
        {
            s = ErrorFacts.GetMessage(id);
        }
    }
}
