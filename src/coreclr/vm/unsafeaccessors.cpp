// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "customattribute.h"
#include "typeparse.h"

namespace
{
    enum class UnsafeAccessorKind
    {
        Constructor, // call instance constructor (`newobj` in IL)
        Method, // call instance method (`callvirt` in IL)
        StaticMethod, // call static method (`call` in IL)
        Field, // address of instance field (`ldflda` in IL)
        StaticField // address of static field (`ldsflda` in IL)
    };

    bool TryParseUnsafeAccessorAttribute(
        MethodDesc* pMD,
        CustomAttributeParser& ca,
        UnsafeAccessorKind& kind,
        SString& name)
    {
        STANDARD_VM_CONTRACT;
        _ASSERTE(pMD != NULL);

        // Get the kind of accessor
        CaArg args[1];
        args[0].InitEnum(SERIALIZATION_TYPE_I4, 0);
        if (FAILED(::ParseKnownCaArgs(ca, args, ARRAY_SIZE(args))))
            return false;

        kind = (UnsafeAccessorKind)args[0].val.i4;

        // Check the name of the target to access. This is the name we
        // use to look up the intended token in metadata.
        CaNamedArg namedArgs[1];
        CaType namedArgTypes[1];
        namedArgTypes[0].Init(SERIALIZATION_TYPE_STRING);
        namedArgs[0].Init("Name", SERIALIZATION_TYPE_PROPERTY, namedArgTypes[0]);
        if (FAILED(::ParseKnownCaNamedArgs(ca, namedArgs, ARRAY_SIZE(namedArgs))))
            return false;

        // If the Name isn't defined, then use the name of the method.
        if (namedArgs[0].val.type.tag == SERIALIZATION_TYPE_UNDEFINED)
        {
            // The Constructor case has an implied value provided by
            // the runtime. We are going to enforce this during consumption
            // so we avoid the setting of the value. We validate the name
            // as empty at the use site.
            if (kind != UnsafeAccessorKind::Constructor)
                name.SetUTF8(pMD->GetName());
        }
        else
        {
            const CaValue& val = namedArgs[0].val;
            name.SetUTF8(val.str.pStr, val.str.cbStr);
        }

        return true;
    }

    struct GenerationContext final
    {
        GenerationContext(UnsafeAccessorKind kind, MethodDesc* pMD)
            : Kind{ kind }
            , Declaration{ pMD }
            , DeclarationSig{ pMD->GetSigPointer() }
            , DeclarationMetaSig{ pMD }
            , TargetTypeSig{}
            , TargetType{}
            , IsTargetStatic{ false }
            , TargetMethod{}
            , TargetField{}
        { }

        UnsafeAccessorKind Kind;
        MethodDesc* Declaration;
        SigPointer DeclarationSig;  // This is the official declaration signature. It may be modified
                                    // to include the UnsafeAccessorTypeAttribute types.
        MetaSig DeclarationMetaSig;
        SigPointer TargetTypeSig;
        TypeHandle TargetType;
        bool IsTargetStatic;
        MethodDesc* TargetMethod;
        FieldDesc* TargetField;
    };

    void UpdateDeclarationSigWithTypes(GenerationContext& cxt, uint32_t translationsCount, MethodTable** translations)
    {
        STANDARD_VM_CONTRACT;
        _ASSERTE(cxt.Declaration != NULL);
        _ASSERTE(translationsCount != 0);
        _ASSERTE(translations != NULL);

        //
        // Parsing and building the signature follows details defined in ECMA-335 - II.23.2.1
        //

        Module* pSigModule = cxt.Declaration->GetModule();

        // Read the current signature and copy it, updating the
        // types for the parameters that had UnsafeAccessorTypeAttribute.
        SigPointer origSig = cxt.Declaration->GetSigPointer();

        // We're going to be modifying the signature and we will be inserting
        // ELEMENT_TYPE_INTERNAL instances. This will add an additional pointer
        // size for each discovered attribute.
        SigBuilder newSig;

        uint32_t callConvDecl;
        IfFailThrow(origSig.GetCallingConvInfo(&callConvDecl));
        newSig.AppendByte((BYTE)callConvDecl);

        uint32_t declGenericCount = 0;
        if (callConvDecl & IMAGE_CEE_CS_CALLCONV_GENERIC)
        {
            IfFailThrow(origSig.GetData(&declGenericCount));
            newSig.AppendData(declGenericCount);
        }

        uint32_t declArgCount;
        IfFailThrow(origSig.GetData(&declArgCount));
        newSig.AppendData(declArgCount);

        _ASSERTE(declArgCount + 1 == translationsCount);

        // Now we can copy over the return type and arguments.
        // The format for the return type is the same as the arguments,
        // except return parameters can be ELEMENT_TYPE_VOID.
        for (uint32_t i = 0; i < translationsCount; ++i)
        {
            // Copy over any modopts or modreqs.
            origSig.CopyModOptsReqs(pSigModule, &newSig);

            MethodTable* newType = translations[i];
            if (newType == NULL)
            {
                // Copy the original parameter and continue.
                origSig.CopyExactlyOne(pSigModule, &newSig);
                continue;
            }

            // We have a new type to insert. We need to update this parameter.
            CorElementType currTypeElem;
            IfFailThrow(origSig.GetElemType(&currTypeElem));
            if (currTypeElem == ELEMENT_TYPE_BYREF)
            {
                newSig.AppendElementType(currTypeElem);
                IfFailThrow(origSig.GetElemType(&currTypeElem));
            }

            // Validate the parameter resolves correctly. There is only one acceptable mappings:
            //     ELEMENT_TYPE_OBJECT - if new type is a reference type.
            _ASSERTE(!newType->IsValueType());
            CorElementType expectedType = ELEMENT_TYPE_OBJECT;

            if (expectedType != currTypeElem)
                ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSORTYPE);

            // Append the new type to the signature.
            newSig.AppendElementType(ELEMENT_TYPE_INTERNAL);
            newSig.AppendPointer(newType);
        }

        // Create a copy of the new signature and store it on the context.
        DWORD newSigLen;
        void* newSigRaw = newSig.GetSignature(&newSigLen);

        // Allocate the signature memory on the loader allocator associated
        // with the declaration method.
        void* newSigAlloc = cxt.Declaration->GetLoaderAllocator()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(newSigLen));
        memcpy(newSigAlloc, newSigRaw, newSigLen);

        // Update the declaration signature with the new signature.
        cxt.DeclarationSig = SigPointer{ (PCCOR_SIGNATURE)newSigAlloc, newSigLen };
        SigTypeContext tmpContext( cxt.Declaration );
        cxt.DeclarationMetaSig = MetaSig{ (PCCOR_SIGNATURE)newSigAlloc, newSigLen, cxt.Declaration->GetModule(), &tmpContext };
    }

    void ProcessUnsafeAccessorTypeAttributes(GenerationContext& cxt)
    {
        STANDARD_VM_CONTRACT;

        // Acquire attribute name to search for.
        const char* typeAttrName = GetWellKnownAttributeName(WellKnownAttribute::UnsafeAccessorTypeAttribute);

        uint32_t attrCount = 0;

        // Allocate memory to store the updated types.
        NewArrayHolder<MethodTable*> heapAlloc;
        MethodTable* stackAlloc[12]; // Use the stack alloc the majority of the time.
        MethodTable** translations = stackAlloc;

        // Determine the max parameter count. +1 for the return value, which is always index zero.
        const uint32_t totalParamCount = cxt.DeclarationMetaSig.NumFixedArgs() + 1;

        // Inspect all parameters to the declaration method for UnsafeAccessorTypeAttribute.
        IMDInternalImport *pInternalImport = cxt.Declaration->GetModule()->GetMDImport();
        HENUMInternalHolder hEnumParams(pInternalImport);
        hEnumParams.EnumInit(mdtParamDef, cxt.Declaration->GetMemberDef());
        mdParamDef currParamDef = mdParamDefNil;
        while (hEnumParams.EnumNext(&currParamDef))
        {
            const void *pData;
            ULONG cbData;
            HRESULT hr = IfFailThrow(pInternalImport->GetCustomAttributeByName(currParamDef, typeAttrName, &pData, &cbData));
            if (hr != S_OK)
                continue;

            // The first time we find an attribute, we need to initialize
            // the translations array and might need to allocate a larger array
            // to store the translations.
            if (attrCount == 0)
            {
                if (totalParamCount > ARRAY_SIZE(stackAlloc))
                {
                    heapAlloc = new MethodTable*[totalParamCount];
                    translations = heapAlloc;
                }
                memset(translations, 0, sizeof(translations[0]) * totalParamCount);
            }

            // Parse the attribute data.
            CustomAttributeParser cap(pData, cbData);
            IfFailThrow(cap.ValidateProlog());
            LPCUTF8 typeString;
            ULONG typeStringLen;
            IfFailThrow(cap.GetNonNullString(&typeString, &typeStringLen));

            StackSString typeStringUtf8{ SString::Utf8, typeString, typeStringLen };

            // Pass the string in the attribute to similar logic as Type.GetType(String).
            // The below API creates a dependency between the returned type and the requesting
            // assembly for the purposes of lifetime tracking of collectible types.
            TypeHandle typeHandle = TypeName::GetTypeReferencedByCustomAttribute(
                typeStringUtf8.GetUnicode(),
                cxt.Declaration->GetAssembly());
            _ASSERTE(!typeHandle.IsNull());

            MethodTable* targetType = typeHandle.AsMethodTable();

            // Future versions of the runtime may support
            // UnsafeAccessorTypeAttribute on value types.
            if (targetType->IsValueType())
                ThrowHR(COR_E_NOTSUPPORTED, BFA_INVALID_UNSAFEACCESSORTYPE_VALUETYPE);

            USHORT seq;
            DWORD attr;
            LPCSTR paramName;
            IfFailThrow(pInternalImport->GetParamDefProps(currParamDef, &seq, &attr, &paramName));

            if (seq >= totalParamCount)
                ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSORTYPE);

            // Store the MethodTable for the loaded type at the sequence number for the parameter.
            translations[seq] = targetType;
            attrCount++;
        }

        // Update the declaration signatures if any instances of UnsafeAccessorTypeAttribute were found.
        if (attrCount != 0)
            UpdateDeclarationSigWithTypes(cxt, totalParamCount, translations);
    }

    TypeHandle ValidateTargetType(TypeHandle targetTypeMaybe, CorElementType targetFromSig)
    {
        STANDARD_VM_CONTRACT;
        TypeHandle targetType = targetTypeMaybe.IsByRef()
            ? targetTypeMaybe.GetTypeParam()
            : targetTypeMaybe;

        // Due to how some types degrade, we block on parameterized
        // types that are represented as TypeDesc. For example ref or pointer.
        if (targetType.IsTypeDesc())
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);

        // We do not support generic signature types as valid targets.
        if (targetFromSig == ELEMENT_TYPE_VAR || targetFromSig == ELEMENT_TYPE_MVAR)
        {
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);
        }

        return targetType;
    }

    bool DoesMethodMatchUnsafeAccessorDeclaration(
        GenerationContext& cxt,
        MethodDesc* method,
        MetaSig::CompareState& state)
    {
        STANDARD_VM_CONTRACT;
        _ASSERTE(method != NULL);

        PCCOR_SIGNATURE pSig1;
        uint32_t cSig1;
        cxt.DeclarationSig.GetSignature(&pSig1, &cSig1);
        PCCOR_SIGNATURE pEndSig1 = pSig1 + cSig1;
        ModuleBase* pModule1 = cxt.Declaration->GetModule();
        const Substitution* pSubst1 = NULL;

        PCCOR_SIGNATURE pSig2;
        DWORD cSig2;
        method->GetSig(&pSig2, &cSig2);
        PCCOR_SIGNATURE pEndSig2 = pSig2 + cSig2;
        ModuleBase* pModule2 = method->GetModule();
        const Substitution* pSubst2 = NULL;

        //
        // Parsing the signature follows details defined in ECMA-335 - II.23.2.1
        //

        uint32_t callConvDecl;
        uint32_t callConvMethod;
        IfFailThrow(CorSigUncompressCallingConv(pSig1, cSig1, &callConvDecl));
        IfFailThrow(CorSigUncompressCallingConv(pSig2, cSig2, &callConvMethod));
        pSig1++;
        pSig2++;

        // Validate calling convention
        if ((callConvDecl & IMAGE_CEE_CS_CALLCONV_MASK) != (callConvMethod & IMAGE_CEE_CS_CALLCONV_MASK))
        {
            return false;
        }

        // Handle generic param count
        DWORD declGenericCount = 0;
        DWORD methodGenericCount = 0;
        if (callConvDecl & IMAGE_CEE_CS_CALLCONV_GENERIC)
            IfFailThrow(CorSigUncompressData_EndPtr(pSig1, pEndSig1, &declGenericCount));
        if (callConvMethod & IMAGE_CEE_CS_CALLCONV_GENERIC)
            IfFailThrow(CorSigUncompressData_EndPtr(pSig2, pEndSig2, &methodGenericCount));

        DWORD declArgCount;
        DWORD methodArgCount;
        IfFailThrow(CorSigUncompressData_EndPtr(pSig1, pEndSig1, &declArgCount));
        IfFailThrow(CorSigUncompressData_EndPtr(pSig2, pEndSig2, &methodArgCount));

        // Validate argument count
        if (cxt.Kind == UnsafeAccessorKind::Constructor)
        {
            // Declarations for constructor scenarios have
            // matching argument counts with the target.
            if (declArgCount != methodArgCount)
                return false;
        }
        else
        {
            // Declarations of non-constructor scenarios have
            // an additional argument to indicate target type
            // and to pass an instance for non-static methods.
            if (declArgCount != (methodArgCount + 1))
                return false;
        }

        // Validate return and argument types
        for (DWORD i = 0; i <= methodArgCount; ++i)
        {
            if (i == 0 && cxt.Kind == UnsafeAccessorKind::Constructor)
            {
                // Skip return value (index 0) validation on constructor
                // accessor declarations.
                SigPointer ptr1(pSig1, (DWORD)(pEndSig1 - pSig1));
                IfFailThrow(ptr1.SkipExactlyOne());
                pSig1 = ptr1.GetPtr();

                CorElementType typ;
                SigPointer ptr2(pSig2, (DWORD)(pEndSig2 - pSig2));
                IfFailThrow(ptr2.GetElemType(&typ));
                pSig2 = ptr2.GetPtr();

                // Validate the return value for target constructor
                // candidate is void.
                if (typ != ELEMENT_TYPE_VOID)
                    return false;

                continue;
            }
            else if (i == 1 && cxt.Kind != UnsafeAccessorKind::Constructor)
            {
                // Skip over first argument (index 1) on non-constructor accessors.
                // See argument count validation above.
                SigPointer ptr1(pSig1, (DWORD)(pEndSig1 - pSig1));
                IfFailThrow(ptr1.SkipExactlyOne());
                pSig1 = ptr1.GetPtr();
            }

            // Compare the types
            if (FALSE == MetaSig::CompareElementType(
                pSig1,
                pSig2,
                pEndSig1,
                pEndSig2,
                pModule1,
                pModule2,
                pSubst1,
                pSubst2,
                &state))
            {
                return false;
            }
        }

        return true;
    }

    void VerifyDeclarationSatisfiesTargetConstraints(MethodDesc* declaration, MethodTable* targetType, MethodDesc* targetMethod)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(declaration != NULL);
            PRECONDITION(targetType != NULL);
            PRECONDITION(targetMethod != NULL);
        }
        CONTRACTL_END;

        // If the target method has no generic parameters there is nothing to verify
        if (!targetMethod->HasClassOrMethodInstantiation())
            return;

        // Construct a context for verifying target's constraints are
        // satisfied by the declaration.
        Instantiation declClassInst;
        Instantiation declMethodInst;
        Instantiation targetClassInst;
        Instantiation targetMethodInst;
        if (targetType->HasInstantiation())
        {
            declClassInst = declaration->GetMethodTable()->GetTypicalMethodTable()->GetInstantiation();
            targetClassInst = targetType->GetTypicalMethodTable()->GetInstantiation();
        }
        if (targetMethod->HasMethodInstantiation())
        {
            declMethodInst = declaration->LoadTypicalMethodDefinition()->GetMethodInstantiation();
            targetMethodInst = targetMethod->LoadTypicalMethodDefinition()->GetMethodInstantiation();
        }

        SigTypeContext typeContext;
        SigTypeContext::InitTypeContext(declClassInst, declMethodInst, &typeContext);

        InstantiationContext instContext{ &typeContext };

        //
        // Validate constraints on Type parameters
        //
        DWORD typeParamCount = targetClassInst.GetNumArgs();
        if (typeParamCount != declClassInst.GetNumArgs())
            COMPlusThrow(kInvalidProgramException, W("Argument_GenTypeConstraintsNotEqual"));

        for (DWORD i = 0; i < typeParamCount; ++i)
        {
            TypeHandle arg = declClassInst[i];
            TypeVarTypeDesc* param = targetClassInst[i].AsGenericVariable();
            if (!param->SatisfiesConstraints(&typeContext, arg, &instContext))
                COMPlusThrow(kInvalidProgramException, W("Argument_GenTypeConstraintsNotEqual"));
        }

        //
        // Validate constraints on Method parameters
        //
        DWORD methodParamCount = targetMethodInst.GetNumArgs();
        if (methodParamCount != declMethodInst.GetNumArgs())
            COMPlusThrow(kInvalidProgramException, W("Argument_GenMethodConstraintsNotEqual"));

        for (DWORD i = 0; i < methodParamCount; ++i)
        {
            TypeHandle arg = declMethodInst[i];
            TypeVarTypeDesc* param = targetMethodInst[i].AsGenericVariable();
            if (!param->SatisfiesConstraints(&typeContext, arg, &instContext))
                COMPlusThrow(kInvalidProgramException, W("Argument_GenMethodConstraintsNotEqual"));
        }
    }

    bool TrySetTargetMethod(
        GenerationContext& cxt,
        LPCUTF8 methodName,
        bool ignoreCustomModifiers = true)
    {
        STANDARD_VM_CONTRACT;
        _ASSERTE(methodName != NULL);
        _ASSERTE(cxt.Kind == UnsafeAccessorKind::Constructor
                || cxt.Kind == UnsafeAccessorKind::Method
                || cxt.Kind == UnsafeAccessorKind::StaticMethod);

        TypeHandle targetType = cxt.TargetType;
        _ASSERTE(!targetType.IsTypeDesc());

        MethodTable* pMT = targetType.AsMethodTable();

        MethodDesc* targetMaybe = NULL;

        // Following a similar iteration pattern found in MemberLoader::FindMethod().
        // However, we are only operating on the current type not walking the type hierarchy.
        MethodTable::IntroducedMethodIterator iter(pMT);
        for (; iter.IsValid(); iter.Next())
        {
            MethodDesc* curr = iter.GetMethodDesc();

            // Check the target and current method match static/instance state.
            if (cxt.IsTargetStatic != (!!curr->IsStatic()))
                continue;

            // Check for matching name
            if (strcmp(methodName, curr->GetNameThrowing()) != 0)
                continue;

            // Check signature
            TokenPairList list { nullptr };
            MetaSig::CompareState state{ &list };
            state.IgnoreCustomModifiers = ignoreCustomModifiers;
            if (!DoesMethodMatchUnsafeAccessorDeclaration(cxt, curr, state))
                continue;

            // Check if there is some ambiguity.
            if (targetMaybe != NULL)
            {
                if (ignoreCustomModifiers)
                {
                    // We have detected ambiguity when ignoring custom modifiers.
                    // Start over, but look for a match requiring custom modifiers
                    // to match precisely.
                    if (TrySetTargetMethod(cxt, methodName, false /* ignoreCustomModifiers */))
                        return true;
                }
                COMPlusThrow(kAmbiguousMatchException, W("Arg_AmbiguousMatchException_UnsafeAccessor"));
            }
            targetMaybe = curr;
        }

        if (targetMaybe != NULL)
            VerifyDeclarationSatisfiesTargetConstraints(cxt.Declaration, pMT, targetMaybe);

        cxt.TargetMethod = targetMaybe;
        return cxt.TargetMethod != NULL;
    }

    bool DoesFieldMatchUnsafeAccessorDeclaration(
        GenerationContext& cxt,
        FieldDesc* field,
        MetaSig::CompareState& state)
    {
        STANDARD_VM_CONTRACT;
        _ASSERTE(field != NULL);

        PCCOR_SIGNATURE pSig1;
        uint32_t cSig1;
        cxt.DeclarationSig.GetSignature(&pSig1, &cSig1);
        PCCOR_SIGNATURE pEndSig1 = pSig1 + cSig1;
        ModuleBase* pModule1 = cxt.Declaration->GetModule();
        const Substitution* pSubst1 = NULL;

        PCCOR_SIGNATURE pSig2;
        DWORD cSig2;
        field->GetSig(&pSig2, &cSig2);
        PCCOR_SIGNATURE pEndSig2 = pSig2 + cSig2;
        ModuleBase* pModule2 = field->GetModule();
        const Substitution* pSubst2 = NULL;

        //
        // Parsing the signature follows details defined in ECMA-335 - II.23.2.1 (MethodDefSig) and II.23.2.4 (FieldSig)
        // The intent here is to compare the return type in the MethodDefSig with the type in the FieldSig
        //

        // Consume calling convention
        uint32_t callConvDecl;
        uint32_t callConvField;
        IfFailThrow(CorSigUncompressCallingConv(pSig1, cSig1, &callConvDecl));
        IfFailThrow(CorSigUncompressCallingConv(pSig2, cSig2, &callConvField));
        _ASSERTE(callConvField == IMAGE_CEE_CS_CALLCONV_FIELD);
        pSig1++;
        pSig2++;

        // Consume parts of the method signature until we get to the return type.
        DWORD declGenericCount = 0;
        if (callConvDecl & IMAGE_CEE_CS_CALLCONV_GENERIC)
            IfFailThrow(CorSigUncompressData_EndPtr(pSig1, pEndSig1, &declGenericCount));

        DWORD declArgCount;
        IfFailThrow(CorSigUncompressData_EndPtr(pSig1, pEndSig1, &declArgCount));
        if (pSig1 >= pEndSig1)
            ThrowHR(META_E_BAD_SIGNATURE);

        // UnsafeAccessors for fields require return types be byref. However, we first need to
        // consume any custom modifiers which are prior to the expected ELEMENT_TYPE_BYREF in
        // the RetType signature (II.23.2.11).
        _ASSERTE(state.IgnoreCustomModifiers); // We should always ignore custom modifiers for field look-up.
        MetaSig::ConsumeCustomModifiers(pSig1, pEndSig1);
        if (pSig1 >= pEndSig1)
            ThrowHR(META_E_BAD_SIGNATURE);

        // The ELEMENT_TYPE_BYREF was explicitly checked in TryGenerateUnsafeAccessor().
        CorElementType byRefType = CorSigUncompressElementType(pSig1);
        _ASSERTE(byRefType == ELEMENT_TYPE_BYREF);

        // Compare the types
        if (FALSE == MetaSig::CompareElementType(
            pSig1,
            pSig2,
            pEndSig1,
            pEndSig2,
            pModule1,
            pModule2,
            pSubst1,
            pSubst2,
            &state))
        {
            return false;
        }

        return true;
    }

    bool TrySetTargetField(
        GenerationContext& cxt,
        LPCUTF8 fieldName)
    {
        STANDARD_VM_CONTRACT;
        _ASSERTE(fieldName != NULL);
        _ASSERTE(cxt.Kind == UnsafeAccessorKind::Field
                || cxt.Kind == UnsafeAccessorKind::StaticField);

        TypeHandle targetType = cxt.TargetType;
        _ASSERTE(!targetType.IsTypeDesc());

        MethodTable* pMT = targetType.AsMethodTable();

        ApproxFieldDescIterator fdIterator(
            pMT,
            (cxt.IsTargetStatic ? ApproxFieldDescIterator::STATIC_FIELDS : ApproxFieldDescIterator::INSTANCE_FIELDS));
        PTR_FieldDesc pField;
        while ((pField = fdIterator.Next()) != NULL)
        {
            // Validate the name and target type match.
            if (strcmp(fieldName, pField->GetName()) != 0)
                continue;

            TokenPairList list { nullptr };
            MetaSig::CompareState state{ &list };
            state.IgnoreCustomModifiers = true;
            if (!DoesFieldMatchUnsafeAccessorDeclaration(cxt, pField, state))
                continue;

            if (cxt.Kind == UnsafeAccessorKind::StaticField && pMT->HasGenericsStaticsInfo())
            {
                // Statics require the exact typed field as opposed to the canonically
                // typed field. In order to do that we lookup the current index of the
                // approx field and then use that index to get the precise field from
                // the approx field.
                MethodTable* pFieldMT = pField->GetApproxEnclosingMethodTable();
                DWORD index = pFieldMT->GetIndexForFieldDesc(pField);
                pField = pMT->GetFieldDescByIndex(index);
            }

            cxt.TargetField = pField;
            return true;
        }
        return false;
    }

    void GenerateAccessor(
        GenerationContext& cxt,
        DynamicResolver** resolver,
        COR_ILMETHOD_DECODER** methodILDecoder)
    {
        STANDARD_VM_CONTRACT;

        NewHolder<ILStubResolver> ilResolver = new ILStubResolver();

        // Initialize the resolver target details.
        ilResolver->SetStubMethodDesc(cxt.Declaration);
        ilResolver->SetStubTargetMethodDesc(cxt.TargetMethod);

        SigTypeContext genericContext;
        if (cxt.Declaration->GetClassification() == mcInstantiated)
            SigTypeContext::InitTypeContext(cxt.Declaration, &genericContext);

        ILStubLinker sl(
            cxt.Declaration->GetModule(),
            cxt.Declaration->GetSignature(), // Must be the MethodDesc's declaration signature, not the DeclarationSig field.
            &genericContext,
            cxt.TargetMethod,
            (ILStubLinkerFlags)ILSTUB_LINKER_FLAG_NONE);

        ILCodeStream* pCode = sl.NewCodeStream(ILStubLinker::kDispatch);

        // Load stub arguments.
        // When the target is static, the first argument is only
        // used to look up the target member to access and ignored
        // during dispatch.
        UINT beginIndex = cxt.IsTargetStatic ? 1 : 0;
        UINT stubArgCount = cxt.DeclarationMetaSig.NumFixedArgs();
        for (UINT i = beginIndex; i < stubArgCount; ++i)
            pCode->EmitLDARG(i);

        // Provide access to the target member
        UINT targetArgCount = stubArgCount - beginIndex;
        UINT targetRetCount = cxt.DeclarationMetaSig.IsReturnTypeVoid() ? 0 : 1;
        switch (cxt.Kind)
        {
        case UnsafeAccessorKind::Constructor:
        {
            _ASSERTE(cxt.TargetMethod != NULL);
            mdToken target;
            if (!cxt.TargetType.HasInstantiation())
            {
                target = pCode->GetToken(cxt.TargetMethod);
            }
            else
            {
                PCCOR_SIGNATURE sig;
                uint32_t sigLen;
                cxt.TargetTypeSig.GetSignature(&sig, &sigLen);
                mdToken targetTypeSigToken = pCode->GetSigToken(sig, sigLen);
                target = pCode->GetToken(cxt.TargetMethod, targetTypeSigToken);
            }
            pCode->EmitNEWOBJ(target, targetArgCount);
            break;
        }
        case UnsafeAccessorKind::Method:
        case UnsafeAccessorKind::StaticMethod:
        {
            _ASSERTE(cxt.TargetMethod != NULL);
            mdToken target;
            if (!cxt.TargetMethod->HasClassOrMethodInstantiation())
            {
                target = pCode->GetToken(cxt.TargetMethod);
            }
            else
            {
                DWORD targetGenericCount = cxt.TargetMethod->GetNumGenericMethodArgs();

                mdToken methodSpecSigToken = mdTokenNil;
                SigBuilder sigBuilder;
                uint32_t sigLen;
                PCCOR_SIGNATURE sig;
                if (targetGenericCount != 0)
                {
                    // Create signature for the MethodSpec. See ECMA-335 - II.23.2.15
                    sigBuilder.AppendByte(IMAGE_CEE_CS_CALLCONV_GENERICINST);
                    sigBuilder.AppendData(targetGenericCount);
                    for (DWORD i = 0; i < targetGenericCount; ++i)
                    {
                        sigBuilder.AppendElementType(ELEMENT_TYPE_MVAR);
                        sigBuilder.AppendData(i);
                    }

                    sig = (PCCOR_SIGNATURE)sigBuilder.GetSignature((DWORD*)&sigLen);
                    methodSpecSigToken = pCode->GetSigToken(sig, sigLen);
                }

                cxt.TargetTypeSig.GetSignature(&sig, &sigLen);
                mdToken targetTypeSigToken = pCode->GetSigToken(sig, sigLen);

                if (methodSpecSigToken == mdTokenNil)
                {
                    // Create a MemberRef
                    target = pCode->GetToken(cxt.TargetMethod, targetTypeSigToken);
                    _ASSERTE(TypeFromToken(target) == mdtMemberRef);
                }
                else
                {
                    // Use the method declaration Instantiation to find the instantiated MethodDesc target.
                    Instantiation methodInst = cxt.Declaration->GetMethodInstantiation();
                    MethodDesc* instantiatedTarget = MethodDesc::FindOrCreateAssociatedMethodDesc(cxt.TargetMethod, cxt.TargetType.GetMethodTable(), FALSE, methodInst, TRUE);

                    // Create a MethodSpec
                    target = pCode->GetToken(instantiatedTarget, targetTypeSigToken, methodSpecSigToken);
                    _ASSERTE(TypeFromToken(target) == mdtMethodSpec);
                }
            }

            if (cxt.Kind == UnsafeAccessorKind::StaticMethod)
            {
                pCode->EmitCALL(target, targetArgCount, targetRetCount);
            }
            else
            {
                pCode->EmitCALLVIRT(target, targetArgCount, targetRetCount);
            }
            break;
        }
        case UnsafeAccessorKind::Field:
        {
            _ASSERTE(cxt.TargetField != NULL);
            mdToken target;
            if (!cxt.TargetType.HasInstantiation())
            {
                target = pCode->GetToken(cxt.TargetField);
            }
            else
            {
                // See the static field case for why this can be mdTokenNil.
                mdToken targetTypeSigToken = mdTokenNil;
                target = pCode->GetToken(cxt.TargetField, targetTypeSigToken);
            }
            pCode->EmitLDFLDA(target);
            break;
        }
        case UnsafeAccessorKind::StaticField:
            _ASSERTE(cxt.TargetField != NULL);
            mdToken target;
            if (!cxt.TargetType.HasInstantiation())
            {
                target = pCode->GetToken(cxt.TargetField);
            }
            else
            {
                // For accessing a generic instance field, every instantiation will
                // be at the same offset, and be the same size, with the same GC layout,
                // as long as the generic is canonically equivalent. However, for static fields,
                // while the offset, size and GC layout remain the same, the address of the
                // field is different, and needs to be found by a lookup of some form. The
                // current form of lookup means the exact type isn't with a type signature.
                PCCOR_SIGNATURE sig;
                uint32_t sigLen;
                cxt.TargetTypeSig.GetSignature(&sig, &sigLen);
                mdToken targetTypeSigToken = pCode->GetSigToken(sig, sigLen);
                target = pCode->GetToken(cxt.TargetField, targetTypeSigToken);
            }
            pCode->EmitLDSFLDA(target);
            break;
        default:
            _ASSERTE(!"Unknown UnsafeAccessorKind");
        }

        // Return from the generated stub
        pCode->EmitRET();

        // Generate all IL associated data for JIT
        {
            UINT maxStack;
            size_t cbCode = sl.Link(&maxStack);
            DWORD cbSig = sl.GetLocalSigSize();

            COR_ILMETHOD_DECODER* pILHeader = ilResolver->AllocGeneratedIL(cbCode, cbSig, maxStack);
            BYTE* pbBuffer = (BYTE*)pILHeader->Code;
            BYTE* pbLocalSig = (BYTE*)pILHeader->LocalVarSig;
            _ASSERTE(cbSig == pILHeader->cbLocalVarSig);
            sl.GenerateCode(pbBuffer, cbCode);
            sl.GetLocalSig(pbLocalSig, cbSig);

            // Store the token lookup map
            ilResolver->SetTokenLookupMap(sl.GetTokenLookupMap());
            ilResolver->SetJitFlags(CORJIT_FLAGS(CORJIT_FLAGS::CORJIT_FLAG_IL_STUB));

            *resolver = (DynamicResolver*)ilResolver;
            *methodILDecoder = pILHeader;
        }

        ilResolver.SuppressRelease();
    }
}

bool MethodDesc::TryGenerateUnsafeAccessor(DynamicResolver** resolver, COR_ILMETHOD_DECODER** methodILDecoder)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(resolver != NULL);
    _ASSERTE(methodILDecoder != NULL);
    _ASSERTE(*resolver == NULL && *methodILDecoder == NULL);
    _ASSERTE(IsIL());
    _ASSERTE(GetRVA() == 0);

    // The UnsafeAccessorAttribute is applied to methods with an
    // RVA of 0 (for example, C#'s extern keyword).
    const void* data;
    ULONG dataLen;
    HRESULT hr = GetCustomAttribute(WellKnownAttribute::UnsafeAccessorAttribute, &data, &dataLen);
    if (hr != S_OK)
        return false;

    // UnsafeAccessor must be on a static method
    if (!IsStatic())
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);

    UnsafeAccessorKind kind;
    SString name;

    CustomAttributeParser ca(data, dataLen);
    if (!TryParseUnsafeAccessorAttribute(this, ca, kind, name))
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);

    GenerationContext context{ kind, this };

    // Parse the signature and check for instances of UnsafeAccessorTypeAttribute.
    ProcessUnsafeAccessorTypeAttributes(context);

    // Parse the signature to determine the type to use:
    //  * Constructor access - examine the return type
    //  * Instance member access - examine type of first parameter
    //  * Static member access - examine type of first parameter
    TypeHandle retType;
    CorElementType retCorType;
    TypeHandle firstArgType;
    CorElementType firstArgCorType = ELEMENT_TYPE_END;
    retCorType = context.DeclarationMetaSig.GetReturnType();
    retType = context.DeclarationMetaSig.GetRetTypeHandleThrowing();
    UINT argCount = context.DeclarationMetaSig.NumFixedArgs();
    if (argCount > 0)
    {
        context.DeclarationMetaSig.NextArg();

        // Get the target type signature and resolve to a type handle.
        context.TargetTypeSig = context.DeclarationMetaSig.GetArgProps();
        (void)context.TargetTypeSig.PeekElemType(&firstArgCorType);
        firstArgType = context.DeclarationMetaSig.GetLastTypeHandleThrowing();
    }

    // Using the kind type, perform the following:
    //  1) Validate the basic type information from the signature.
    //  2) Resolve the name to the appropriate member.
    switch (context.Kind)
    {
    case UnsafeAccessorKind::Constructor:
        // A return type is required for a constructor, otherwise
        // we don't know the type to construct.
        // Types should not be parameterized (that is, byref).
        // The name is defined by the runtime and should be empty.
        if (context.DeclarationMetaSig.IsReturnTypeVoid() || retType.IsByRef() || !name.IsEmpty())
        {
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);
        }

        // Get the target type signature from the return type.
        context.TargetTypeSig = context.DeclarationMetaSig.GetReturnProps();
        context.TargetType = ValidateTargetType(retType, retCorType);
        if (!TrySetTargetMethod(context, ".ctor"))
            MemberLoader::ThrowMissingMethodException(context.TargetType.AsMethodTable(), ".ctor");
        break;

    case UnsafeAccessorKind::Method:
    case UnsafeAccessorKind::StaticMethod:
        // Method access requires a target type.
        if (firstArgType.IsNull())
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);

        // If the non-static method access is for a
        // value type, the instance must be byref.
        if (kind == UnsafeAccessorKind::Method
            && firstArgType.IsValueType()
            && !firstArgType.IsByRef())
        {
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);
        }

        context.TargetType = ValidateTargetType(firstArgType, firstArgCorType);
        context.IsTargetStatic = kind == UnsafeAccessorKind::StaticMethod;
        if (!TrySetTargetMethod(context, name.GetUTF8()))
            MemberLoader::ThrowMissingMethodException(context.TargetType.AsMethodTable(), name.GetUTF8());
        break;

    case UnsafeAccessorKind::Field:
    case UnsafeAccessorKind::StaticField:
        // Field access requires a single argument for target type and a return type.
        if (argCount != 1 || firstArgType.IsNull() || context.DeclarationMetaSig.IsReturnTypeVoid())
        {
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);
        }

        // The return type must be byref.
        // If the non-static field access is for a
        // value type, the instance must be byref.
        if (!retType.IsByRef()
            || (kind == UnsafeAccessorKind::Field
                && firstArgType.IsValueType()
                && !firstArgType.IsByRef()))
        {
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);
        }

        context.TargetType = ValidateTargetType(firstArgType, firstArgCorType);
        context.IsTargetStatic = kind == UnsafeAccessorKind::StaticField;
        if (!TrySetTargetField(context, name.GetUTF8()))
            MemberLoader::ThrowMissingFieldException(context.TargetType.AsMethodTable(), name.GetUTF8());
        break;

    default:
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);
    }

    // Generate the IL for the accessor.
    GenerateAccessor(context, resolver, methodILDecoder);
    return true;
}
