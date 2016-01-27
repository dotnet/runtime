// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




#include "common.h"
#include "gcinfodecoder.h"

#ifdef VERIFY_GCINFO
#ifdef USE_GC_INFO_DECODER

#include "dbggcinfodecoder.h"

#ifndef GCINFODECODER_CONTRACT
#define GCINFODECODER_CONTRACT(contract) contract
#endif // !GCINFODECODER_CONTRACT

#ifndef GET_CALLER_SP
#define GET_CALLER_SP(pREGDISPLAY) EECodeManager::GetCallerSp(pREGDISPLAY)
#endif // !GET_CALLER_SP

#ifndef VALIDATE_OBJECTREF
#ifdef DACCESS_COMPILE
#define VALIDATE_OBJECTREF(objref, fDeep) 
#else // DACCESS_COMPILE
#define VALIDATE_OBJECTREF(objref, fDeep) OBJECTREFToObject(objref)->Validate(fDeep)
#endif // DACCESS_COMPILE
#endif // !VALIDATE_OBJECTREF

#ifndef VALIDATE_ROOT
#define VALIDATE_ROOT(isInterior, hCallBack, pObjRef)                                           \
    do {                                                                                        \
        /* Only call Object::Validate() with bDeep == TRUE if we are in the promote phase.  */  \
        /* We should call Validate() with bDeep == FALSE if we are in the relocation phase. */  \
                                                                                                \
        GCCONTEXT* pGCCtx = (GCCONTEXT*)(hCallBack);                                            \
                                                                                                \
        if (!(isInterior) && !(m_Flags & DECODE_NO_VALIDATION))                                 \
            VALIDATE_OBJECTREF(*(pObjRef), pGCCtx->sc->promotion == TRUE);                      \
    } while (0)
#endif // !VALIDATE_ROOT



namespace DbgGcInfo {


//static
bool GcInfoDecoder::SetIsInterruptibleCB (UINT32 startOffset, UINT32 stopOffset, LPVOID hCallback)
{
    GcInfoDecoder *pThis = (GcInfoDecoder*)hCallback;

    bool fStop = pThis->m_InstructionOffset >= startOffset && pThis->m_InstructionOffset < stopOffset;

    if (fStop)
        pThis->m_IsInterruptible = true;

    return fStop;
}


GcInfoDecoder::GcInfoDecoder(
            const BYTE* gcInfoAddr,
            GcInfoDecoderFlags flags,
            UINT32 breakOffset
            )
            : m_Reader( gcInfoAddr )
            , m_InstructionOffset( breakOffset )
            , m_IsInterruptible( false )
            , m_pLiveRegisters( NULL )
            , m_pLiveStackSlots( NULL )
            , m_NumLiveRegisters(0)
            , m_NumLiveStackSlots(0)
#ifdef _DEBUG
            , m_Flags( flags )
#endif
{
#ifdef _TARGET_ARM_
    _ASSERTE(!"JIT32 is not generating GCInfo in the correct format yet!");
#endif

    _ASSERTE( (flags & (DECODE_INTERRUPTIBILITY | DECODE_GC_LIFETIMES)) || (0 == breakOffset) );

    // The current implementation doesn't support the two flags together
    _ASSERTE(
        ((flags & (DECODE_INTERRUPTIBILITY | DECODE_GC_LIFETIMES)) != (DECODE_INTERRUPTIBILITY | DECODE_GC_LIFETIMES))
            );


    //--------------------------------------------
    // Pre-decode information
    //--------------------------------------------

    m_IsVarArg = (m_Reader.Read(1)) ? true : false;

    size_t hasSecurityObject = m_Reader.Read(1);
    if(hasSecurityObject)
        m_SecurityObjectStackSlot = (INT32) DENORMALIZE_STACK_SLOT(m_Reader.DecodeVarLengthSigned(SECURITY_OBJECT_STACK_SLOT_ENCBASE));
    else
        m_SecurityObjectStackSlot = NO_SECURITY_OBJECT;

    size_t hasPSPSym = m_Reader.Read(1);
    if(hasPSPSym)
    {
        m_PSPSymStackSlot              = (INT32) DENORMALIZE_STACK_SLOT(m_Reader.DecodeVarLengthSigned(PSP_SYM_STACK_SLOT_ENCBASE));
    }
    else
    {
        m_PSPSymStackSlot              = NO_PSP_SYM;
    }

    size_t hasGenericsInstContext = m_Reader.Read(1);
    if(hasGenericsInstContext)
    {
        m_GenericsInstContextStackSlot = (INT32) DENORMALIZE_STACK_SLOT(m_Reader.DecodeVarLengthSigned(GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE));
    }
    else
    {
        m_GenericsInstContextStackSlot = NO_GENERICS_INST_CONTEXT;
    }

    m_CodeLength = (UINT32) DENORMALIZE_CODE_LENGTH(m_Reader.DecodeVarLengthUnsigned(CODE_LENGTH_ENCBASE));

    size_t hasStackBaseRegister = m_Reader.Read(1);
    if(hasStackBaseRegister)
        m_StackBaseRegister = (UINT32) DENORMALIZE_STACK_BASE_REGISTER(m_Reader.DecodeVarLengthUnsigned(STACK_BASE_REGISTER_ENCBASE));
    else
        m_StackBaseRegister = NO_STACK_BASE_REGISTER;

    size_t hasSizeOfEditAndContinuePreservedArea = m_Reader.Read(1);
    if(hasSizeOfEditAndContinuePreservedArea)
        m_SizeOfEditAndContinuePreservedArea = (UINT32) m_Reader.DecodeVarLengthUnsigned(SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE);
    else
        m_SizeOfEditAndContinuePreservedArea = NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA;
    
#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
    m_SizeOfStackOutgoingAndScratchArea = (UINT32)DENORMALIZE_SIZE_OF_STACK_AREA(m_Reader.DecodeVarLengthUnsigned(SIZE_OF_STACK_AREA_ENCBASE));
#endif // FIXED_STACK_PARAMETER_SCRATCH_AREA

    m_NumInterruptibleRanges = (UINT32) DENORMALIZE_NUM_INTERRUPTIBLE_RANGES(m_Reader.DecodeVarLengthUnsigned(NUM_INTERRUPTIBLE_RANGES_ENCBASE));

    if( flags & DECODE_INTERRUPTIBILITY )
    {
        EnumerateInterruptibleRanges(&SetIsInterruptibleCB, this);
    }
}


bool GcInfoDecoder::IsInterruptible()
{
    _ASSERTE( m_Flags & DECODE_INTERRUPTIBILITY );
    return m_IsInterruptible;
}


void GcInfoDecoder::EnumerateInterruptibleRanges (
            EnumerateInterruptibleRangesCallback *pCallback,
            LPVOID                                hCallback)
{
#if 0    
#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED

    //------------------------------------------------------------------------------
    // Try partially interruptible first
    //------------------------------------------------------------------------------

    UINT32 numCallSites = (UINT32)m_Reader.Read( sizeof( numCallSites ) * 8 );
    UINT32 callSiteIdx = 0;

    if( numCallSites > 0 )
    {
        UINT32 numSlotMappings = (UINT32)m_Reader.Read( sizeof( numSlotMappings ) * 8 );

        // Align the reader to the next byte to continue decoding
        m_Reader.Skip( ( 8 - ( m_Reader.GetCurrentPos() % 8 ) ) % 8 );

        for( callSiteIdx=0; callSiteIdx<numCallSites; callSiteIdx++ )
        {
            UINT32 instructionOffset = (UINT32)m_Reader.Read( 32 );

            bool fStop = pCallback(instructionOffset, instructionOffset+1, hCallback);
            if (fStop)
                return;

            m_Reader.Skip( numSlotMappings );
        }

        // Call site not found. Skip the slot mapping table in preparation for reading the fully-interruptible information
        m_Reader.Skip( numSlotMappings * sizeof( GcSlotDesc ) * 8 );
    }

#endif // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
#endif


    // If no info is found for the call site, we default to fully-interruptbile
    LOG((LF_GCROOTS, LL_INFO1000000, "No GC info found for call site at offset %x. Defaulting to fully-interruptible information.\n", (int) m_InstructionOffset));

    // Align the reader to the next byte to continue decoding
    m_Reader.Skip( ( 8 - ( m_Reader.GetCurrentPos() % 8 ) ) % 8 );

    UINT32 lastInterruptibleRangeStopOffsetNormalized = 0;

    for(UINT32 i=0; i<m_NumInterruptibleRanges; i++)
    {
        UINT32 normStartDelta = (UINT32) m_Reader.DecodeVarLengthUnsigned( INTERRUPTIBLE_RANGE_DELTA_ENCBASE );
        UINT32 normStopDelta = (UINT32) m_Reader.DecodeVarLengthUnsigned( INTERRUPTIBLE_RANGE_DELTA_ENCBASE ) + 1;

        UINT32 rangeStartOffsetNormalized = lastInterruptibleRangeStopOffsetNormalized + normStartDelta;
        UINT32 rangeStopOffsetNormalized = rangeStartOffsetNormalized + normStopDelta;

        UINT32 rangeStartOffset = DENORMALIZE_CODE_OFFSET(rangeStartOffsetNormalized);
        UINT32 rangeStopOffset = DENORMALIZE_CODE_OFFSET(rangeStopOffsetNormalized);

        bool fStop = pCallback(rangeStartOffset, rangeStopOffset, hCallback);
        if (fStop)
            return;

        lastInterruptibleRangeStopOffsetNormalized = rangeStopOffsetNormalized;
    }
}


INT32 GcInfoDecoder::GetSecurityObjectStackSlot()
{
    _ASSERTE( m_Flags & DECODE_SECURITY_OBJECT );
    return m_SecurityObjectStackSlot;
}

INT32 GcInfoDecoder::GetGenericsInstContextStackSlot()
{
    _ASSERTE( m_Flags & DECODE_GENERICS_INST_CONTEXT );
    return m_GenericsInstContextStackSlot;
}

INT32 GcInfoDecoder::GetPSPSymStackSlot()
{
    _ASSERTE( m_Flags & DECODE_PSP_SYM);
    return m_PSPSymStackSlot;
}

bool GcInfoDecoder::GetIsVarArg()
{
    _ASSERTE( m_Flags & DECODE_VARARG );
    return m_IsVarArg;
}

UINT32 GcInfoDecoder::GetCodeLength()
{
    _ASSERTE( m_Flags & DECODE_CODE_LENGTH );
    return m_CodeLength;
}

UINT32  GcInfoDecoder::GetStackBaseRegister()
{
    return m_StackBaseRegister;
}

UINT32  GcInfoDecoder::GetSizeOfEditAndContinuePreservedArea()
{
    _ASSERTE( m_Flags & DECODE_EDIT_AND_CONTINUE );
    return m_SizeOfEditAndContinuePreservedArea;
}


#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA

UINT32  GcInfoDecoder::GetSizeOfStackParameterArea()
{
    return m_SizeOfStackOutgoingAndScratchArea;
}

#endif // FIXED_STACK_PARAMETER_SCRATCH_AREA


bool GcInfoDecoder::EnumerateLiveSlots(
                PREGDISPLAY         pRD,
                bool                reportScratchSlots,
                unsigned            flags,
                GCEnumCallback      pCallBack,
                LPVOID              hCallBack
                )
{
    _ASSERTE( m_Flags & DECODE_GC_LIFETIMES );

#if 0
#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED

    //------------------------------------------------------------------------------
    // Try partially interruptible first
    //------------------------------------------------------------------------------

    UINT32 numCallSites = (UINT32)m_Reader.Read( sizeof( numCallSites ) * 8 );
    UINT32 callSiteIdx = 0;

    if( numCallSites > 0 )
    {
        UINT32 numSlotMappings = (UINT32)m_Reader.Read( sizeof( numSlotMappings ) * 8 );

        // Align the reader to the next byte to continue decoding
        m_Reader.Skip( ( 8 - ( m_Reader.GetCurrentPos() % 8 ) ) % 8 );

        for( callSiteIdx=0; callSiteIdx<numCallSites; callSiteIdx++ )
        {
            UINT32 instructionOffset = (UINT32)m_Reader.Read( 32 );
            if( instructionOffset == m_InstructionOffset )
            {
                m_IsInterruptible = true;

                BYTE* callSiteLiveSet = (BYTE*) _alloca( ( numSlotMappings + 7 ) / 8 );

                UINT32 i;
                for( i=0; i<numSlotMappings/8; i++ )
                    callSiteLiveSet[ i ] = (BYTE)m_Reader.Read( 8 );

                callSiteLiveSet[ i ] = (BYTE)m_Reader.Read( numSlotMappings % 8 );

                m_Reader.Skip( ( numCallSites - callSiteIdx - 1 ) * ( 32 + numSlotMappings ) );

                //---------------------------------------------------------------------------
                // Read slot mappings
                //---------------------------------------------------------------------------

                GcSlotDesc* slotMappings = (GcSlotDesc*) _alloca( numSlotMappings * sizeof( GcSlotDesc ) );
                // Assert that we can read a GcSlotDesc with a single call to m_Reader.Read()
                _ASSERTE( sizeof( GcSlotDesc ) <= sizeof ( size_t ) );
                for( UINT32 i=0; i<numSlotMappings; i++ )
                {
                    size_t data = m_Reader.Read( sizeof( GcSlotDesc ) * 8 );
                    slotMappings[ i ] = *( (GcSlotDesc*) &data );
                }

                //---------------------------------------------------------------------------
                // Report live slots
                //---------------------------------------------------------------------------

                for( UINT32 i=0; i<numSlotMappings; i++ )
                {
                    BYTE isLive = callSiteLiveSet[ i / 8 ] & ( 1 << ( i % 8 ) );
                    if( isLive )
                    {
                        GcSlotDesc slotDesc = slotMappings[ i ];
                        if( slotDesc.IsRegister )
                        {
                            if( reportScratchSlots || !IsScratchRegister( slotDesc.Slot.RegisterNumber, pRD ) )
                            {
                                ReportRegisterToGC(
                                                slotDesc.Slot.RegisterNumber,
                                                slotDesc.IsInterior,
                                                slotDesc.IsPinned,
                                                pRD,
                                                flags,
                                                pCallBack,
                                                hCallBack
                                                );
                            }
                            else
                            {
                                LOG((LF_GCROOTS, LL_INFO1000, "\"Live\" scratch register " FMT_REG " not reported\n", slotDesc.Slot.RegisterNumber));
                            }
                        }
                        else
                        {
                            GcStackSlotBase spBase = (GcStackSlotBase) (slotDesc.Slot.SpOffset & 0x3);
                            INT32 realSpOffset = slotDesc.Slot.SpOffset ^ (int) spBase;

                            if( reportScratchSlots || !IsScratchStackSlot(realSpOffset, spBase, pRD) )
                            {
                                ReportStackSlotToGC(
                                                realSpOffset,
                                                spBase,
                                                slotDesc.IsInterior,
                                                slotDesc.IsPinned,
                                                pRD,
                                                flags,
                                                pCallBack,
                                                hCallBack
                                                );
                            }
                            else
                            {
                                LOG((LF_GCROOTS, LL_INFO1000, "\"Live\" scratch stack slot " FMT_STK  " not reported\n", DBG_STK(realSpOffset)));
                            }
                        }
                    }
                }

                return true;
            }

            m_Reader.Skip( numSlotMappings );
        }

        // Call site not found. Skip the slot mapping table in preparation for reading the fully-interruptible information
        m_Reader.Skip( numSlotMappings * sizeof( GcSlotDesc ) * 8 );
    }

#endif // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
#endif


    // If no info is found for the call site, we default to fully-interruptbile
    LOG((LF_GCROOTS, LL_INFO1000000, "No GC info found for call site at offset %x. Defaulting to fully-interruptible information.\n", (int) m_InstructionOffset));

    // Align the reader to the next byte to continue decoding
    m_Reader.Skip( ( 8 - ( m_Reader.GetCurrentPos() % 8 ) ) % 8 );

    // Skip interruptibility information
    for(UINT32 i=0; i<m_NumInterruptibleRanges; i++)
    {
        m_Reader.DecodeVarLengthUnsigned( INTERRUPTIBLE_RANGE_DELTA_ENCBASE );
        m_Reader.DecodeVarLengthUnsigned( INTERRUPTIBLE_RANGE_DELTA_ENCBASE );
    }

    //
    // If this is a non-leaf frame and we are executing a call, the unwinder has given us the PC
    //  of the call instruction. We should adjust it to the PC of the instruction after the call in order to
    //  obtain transition information for scratch slots. However, we always assume scratch slots to be
    //  dead for non-leaf frames (except for ResumableFrames), so we don't need to adjust the PC.
    // If this is a non-leaf frame and we are not executing a call (i.e.: a fault occurred in the function),
    //  then it would be incorrect to ajust the PC
    //

    int lifetimeTransitionsCount = 0;

    //--------------------------------------------------------------------
    // Decode registers
    //--------------------------------------------------------------------

    size_t numRegisters = m_Reader.DecodeVarLengthUnsigned(NUM_REGISTERS_ENCBASE);

    {
#ifdef ENABLE_CONTRACTS_IMPL
        CONTRACT_VIOLATION(FaultViolation | FaultNotFatal);
#endif
        m_pLiveRegisters = (GcSlotDesc*) qbSlots1.AllocNoThrow(sizeof(GcSlotDesc)*numRegisters);
    }
    if (m_pLiveRegisters == NULL)
    {
        return false;
    }
    
    
    _ASSERTE(m_pLiveRegisters);
    
    int lastNormRegNum = 0;

    for(int i=0; i<numRegisters; i++)
    {
        if( i==0 )
        {
            lastNormRegNum = (int) m_Reader.DecodeVarLengthUnsigned(REGISTER_ENCBASE);
        }
        else
        {
            int normRegDelta = (int) m_Reader.DecodeVarLengthUnsigned(REGISTER_DELTA_ENCBASE) + 1;
            lastNormRegNum += normRegDelta;
        }    
        int regNum = DENORMALIZE_REGISTER(lastNormRegNum);
        
        BOOL isInterior  = FALSE;
        BOOL isPinned    = FALSE;
        BOOL isLive      = FALSE;

        size_t normCodeOffset = (size_t)(SSIZE_T)(-1);
        BOOL becomesLive = TRUE;
        for(;;)
        {
            size_t normCodeOffsetDelta = m_Reader.DecodeVarLengthUnsigned(NORM_CODE_OFFSET_DELTA_ENCBASE);
            if(normCodeOffsetDelta == 0) // terminator
                break;

            if(normCodeOffset != (size_t)(SSIZE_T)(-1))
                becomesLive = (BOOL) m_Reader.Read(1);
            
            normCodeOffset += normCodeOffsetDelta;

            UINT32 instructionOffset = DENORMALIZE_CODE_OFFSET((UINT32)normCodeOffset);

            BOOL   becomesInterior = FALSE;
            BOOL   becomesPinned = FALSE;

            if(becomesLive)
            {
                if(m_Reader.Read(1))
                {
                    size_t flagEnc = m_Reader.Read( 2 );
                    becomesInterior = (BOOL)(flagEnc & 0x1);
                    becomesPinned = (BOOL)(flagEnc & 0x2);
                }
            }

            lifetimeTransitionsCount++;

            LOG((LF_GCROOTS, LL_INFO1000000,
                 "Transition " FMT_PIPTR "in " FMT_REG "going %s at offset %04x.\n",
                 DBG_PIN_NAME(becomesPinned), DBG_IPTR_NAME(becomesInterior), regNum,
                 becomesLive ? "live" : "dead",
                 (int) instructionOffset ));

            if( instructionOffset > m_InstructionOffset )
                continue;

            isLive     = becomesLive;
            isInterior = becomesInterior;
            isPinned   = becomesPinned;
        }
            
        if( isLive )
        {
            if( reportScratchSlots || !IsScratchRegister( regNum, pRD ) )
            {
                m_pLiveRegisters[m_NumLiveRegisters].Slot.RegisterNumber = regNum;
                GcSlotFlags flags = GC_SLOT_BASE;
                if(isInterior)
                    flags = (GcSlotFlags) (flags | GC_SLOT_INTERIOR);
                if(isPinned)
                    flags = (GcSlotFlags) (flags | GC_SLOT_PINNED);
                    
                m_pLiveRegisters[m_NumLiveRegisters].Flags = flags;
                m_NumLiveRegisters++;
            }
            else
            {
                LOG((LF_GCROOTS, LL_INFO1000, "\"Live\" scratch register " FMT_REG " not reported\n", regNum));
            }
        }
    }

    //--------------------------------------------------------------------
    // Decode stack slots
    //--------------------------------------------------------------------

    size_t numStackSlots = m_Reader.DecodeVarLengthUnsigned(NUM_STACK_SLOTS_ENCBASE);
    {
#ifdef ENABLE_CONTRACTS_IMPL
        CONTRACT_VIOLATION(FaultViolation | FaultNotFatal);
#endif
        m_pLiveStackSlots = (GcSlotDesc*) qbSlots2.AllocNoThrow(sizeof(GcSlotDesc)*numStackSlots);
    }
    if (m_pLiveStackSlots == NULL)
    {
        return false;
    }
    _ASSERTE(m_pLiveStackSlots);

    INT32 lastNormStackSlot = 0;

    for(int i=0; i<numStackSlots; i++)
    {
        if( i==0 )
        {
            lastNormStackSlot = (INT32) m_Reader.DecodeVarLengthSigned(STACK_SLOT_ENCBASE);
        }
        else
        {
            INT32 normStackSlotDelta = (INT32) m_Reader.DecodeVarLengthUnsigned(STACK_SLOT_DELTA_ENCBASE);
            lastNormStackSlot += normStackSlotDelta;
        }    
        INT32 spOffset = DENORMALIZE_STACK_SLOT(lastNormStackSlot);
        GcStackSlotBase spBase = (GcStackSlotBase) m_Reader.Read(2);
            
        BOOL isInterior  = FALSE;
        BOOL isPinned    = FALSE;
        BOOL isLive      = FALSE;

        size_t normCodeOffset = (size_t)(SSIZE_T)(-1);
        BOOL becomesLive = TRUE;
        for(;;)
        {
            size_t normCodeOffsetDelta = m_Reader.DecodeVarLengthUnsigned(NORM_CODE_OFFSET_DELTA_ENCBASE);
            if(normCodeOffsetDelta == 0) // terminator
                break;
            
            if(normCodeOffset != (size_t)(SSIZE_T)(-1))
                becomesLive = (BOOL) m_Reader.Read(1);

            normCodeOffset += normCodeOffsetDelta;

            UINT32 instructionOffset = DENORMALIZE_CODE_OFFSET((UINT32)normCodeOffset);

            BOOL   becomesInterior = FALSE;
            BOOL   becomesPinned = FALSE;

            if(becomesLive)
            {
                if(m_Reader.Read(1))
                {
                    size_t flagEnc = m_Reader.Read( 2 );
                    becomesInterior = (BOOL)(flagEnc & 0x1);
                    becomesPinned = (BOOL)(flagEnc & 0x2);
                }
            }

            lifetimeTransitionsCount++;

            LOG((LF_GCROOTS, LL_INFO1000000,
                 "Transition " FMT_PIPTR "in " FMT_STK "going %s at offset %04x.\n",
                 DBG_PIN_NAME(becomesPinned), DBG_IPTR_NAME(becomesInterior), DBG_STK(spOffset),
                 becomesLive ? "live" : "dead",
                 (int) instructionOffset ));

            if( instructionOffset > m_InstructionOffset )
                continue;

            isLive     = becomesLive;
            isInterior = becomesInterior;
            isPinned   = becomesPinned;
        }
            
        if( isLive )
        {
            if( reportScratchSlots || !IsScratchStackSlot(spOffset, spBase, pRD) )
            {
                m_pLiveStackSlots[m_NumLiveStackSlots].Slot.Stack.SpOffset = spOffset;
                m_pLiveStackSlots[m_NumLiveStackSlots].Slot.Stack.Base = spBase;
                GcSlotFlags flags = GC_SLOT_BASE;
                if(isInterior)
                    flags = (GcSlotFlags) (flags | GC_SLOT_INTERIOR);
                if(isPinned)
                    flags = (GcSlotFlags) (flags | GC_SLOT_PINNED);
                    
                m_pLiveStackSlots[m_NumLiveStackSlots].Flags = flags;
                m_NumLiveStackSlots++;
            }
            else
            {
                LOG((LF_GCROOTS, LL_INFO1000, "\"Live\" scratch stack slot " FMT_STK  " not reported\n", DBG_STK(spOffset)));
            }
        }
    }


    LOG((LF_GCROOTS, LL_INFO1000000, "Decoded %d lifetime transitions.\n", (int) lifetimeTransitionsCount ));

    return true;
}

void GcInfoDecoder::VerifyLiveRegister(
                            UINT32 regNum,
                            GcSlotFlags flags
                            )
{
    _ASSERTE(m_pLiveRegisters);

    // If this assert fails, the slot being passed was not found to be live in this decoder
    _ASSERTE(m_NumLiveRegisters > 0);

    int pos;
    for(pos = 0; pos < m_NumLiveRegisters; pos++)
    {
        if(regNum == m_pLiveRegisters[pos].Slot.RegisterNumber &&
            flags == m_pLiveRegisters[pos].Flags)
        {
            break;
        }
    }

    // If this assert fails, the slot being passed was not found to be live in this decoder
    _ASSERTE(pos < m_NumLiveRegisters);

    m_pLiveRegisters[pos] = m_pLiveRegisters[--m_NumLiveRegisters];
}
                            
void GcInfoDecoder::VerifyLiveStackSlot(
                            INT32 spOffset,
                            GcStackSlotBase spBase,
                            GcSlotFlags flags
                            )
{
    _ASSERTE(m_pLiveStackSlots);

    // If this assert fails, the slot being passed was not found to be live in this decoder
    _ASSERTE(m_NumLiveStackSlots > 0);

    int pos;
    for(pos = 0; pos < m_NumLiveStackSlots; pos++)
    {
        if(spOffset == m_pLiveStackSlots[pos].Slot.Stack.SpOffset &&
            spBase == m_pLiveStackSlots[pos].Slot.Stack.Base &&
            flags == m_pLiveStackSlots[pos].Flags)
        {
            break;
        }
    }

    // If this assert fails, the slot being passed was not found to be live in this decoder
    _ASSERTE(pos < m_NumLiveStackSlots);

    m_pLiveStackSlots[pos] = m_pLiveStackSlots[--m_NumLiveStackSlots];
}

void GcInfoDecoder::DoFinalVerification()
{
    // If this assert fails, the m_NumLiveRegisters slots remaining in m_pLiveRegisters
    //      were not reported by the calling decoder
    _ASSERTE(m_NumLiveRegisters == 0);

    // If this assert fails, the m_NumLiveStackSlots slots remaining in m_pLiveStackSlots
    //      were not reported by the calling decoder
    _ASSERTE(m_NumLiveStackSlots == 0);

}

//-----------------------------------------------------------------------------
// Platform-specific methods
//-----------------------------------------------------------------------------

#if defined(_TARGET_AMD64_)


OBJECTREF* GcInfoDecoder::GetRegisterSlot(
                        int             regNum,
                        PREGDISPLAY     pRD
                        )
{
    _ASSERTE(regNum >= 0 && regNum <= 16);
    _ASSERTE(regNum != 4);  // rsp

    // The fields of KNONVOLATILE_CONTEXT_POINTERS are in the same order as
    // the processor encoding numbers.

    ULONGLONG **ppRax;
#ifdef _NTAMD64_
    ppRax = &pRD->pCurrentContextPointers->Rax;
#else
    ppRax = &pRD->pCurrentContextPointers->Integer.Register.Rax;
#endif

    return (OBJECTREF*)*(ppRax + regNum);
}


bool GcInfoDecoder::IsScratchRegister(int regNum,  PREGDISPLAY pRD)
{
    _ASSERTE(regNum >= 0 && regNum <= 16);
    _ASSERTE(regNum != 4);  // rsp

    UINT16 PreservedRegMask =
          (1 << 3)  // rbx
        | (1 << 5)  // rbp
        | (1 << 6)  // rsi
        | (1 << 7)  // rdi
        | (1 << 12)  // r12
        | (1 << 13)  // r13
        | (1 << 14)  // r14
        | (1 << 15); // r15

    return !(PreservedRegMask & (1 << regNum));
}


bool GcInfoDecoder::IsScratchStackSlot(INT32 spOffset, GcStackSlotBase spBase, PREGDISPLAY     pRD)
{
#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
    _ASSERTE( m_Flags & DECODE_GC_LIFETIMES );

    ULONGLONG pSlot = (ULONGLONG) GetStackSlot(spOffset, spBase, pRD);
    _ASSERTE(pSlot >= pRD->SP);

    return (pSlot < pRD->SP + m_SizeOfStackOutgoingAndScratchArea);
#else
    return FALSE;
#endif
}


void GcInfoDecoder::ReportRegisterToGC(  // AMD64
                                int             regNum,
                                BOOL            isInterior,
                                BOOL            isPinned,
                                PREGDISPLAY     pRD,
                                unsigned        flags,
                                GCEnumCallback  pCallBack,
                                LPVOID          hCallBack)
{
    GCINFODECODER_CONTRACT(CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END);

    _ASSERTE(regNum >= 0 && regNum <= 16);
    _ASSERTE(regNum != 4);  // rsp

    LOG((LF_GCROOTS, LL_INFO1000, "Reporting " FMT_REG, regNum ));

    OBJECTREF* pObjRef = GetRegisterSlot( regNum, pRD );

#ifdef _DEBUG
    if(IsScratchRegister(regNum, pRD))
    {
        // Scratch registers cannot be reported for non-leaf frames
        _ASSERTE(flags & ActiveStackFrame);
    }

    LOG((LF_GCROOTS, LL_INFO1000, /* Part Two */
         "at" FMT_ADDR "as ", DBG_ADDR(pObjRef) ));

    VALIDATE_ROOT(isInterior, hCallBack, pObjRef);

    LOG((LF_GCROOTS, LL_INFO1000, /* Part Three */
         LOG_PIPTR_OBJECT_CLASS(OBJECTREF_TO_UNCHECKED_OBJECTREF(*pObjRef), isPinned, isInterior)));
#endif //_DEBUG

    DWORD gcFlags = CHECK_APP_DOMAIN;

    if (isInterior)
        gcFlags |= GC_CALL_INTERIOR;

    if (isPinned)
        gcFlags |= GC_CALL_PINNED;

    pCallBack(hCallBack, pObjRef, gcFlags);
}

#else // Unknown platform

OBJECTREF* GcInfoDecoder::GetRegisterSlot(
                        int             regNum,
                        PREGDISPLAY     pRD
                        )
{
    PORTABILITY_ASSERT("GcInfoDecoder::GetRegisterSlot");
    return NULL;
}

bool GcInfoDecoder::IsScratchRegister(int regNum,  PREGDISPLAY pRD)
{
    PORTABILITY_ASSERT("GcInfoDecoder::IsScratchRegister");
    return false;
}

bool GcInfoDecoder::IsScratchStackSlot(INT32 spOffset, GcStackSlotBase spBase, PREGDISPLAY     pRD)
{
    _ASSERTE( !"NYI" );
    return false;
}

void GcInfoDecoder::ReportRegisterToGC(
                                int             regNum,
                                BOOL            isInterior,
                                BOOL            isPinned,
                                PREGDISPLAY     pRD,
                                unsigned        flags,
                                GCEnumCallback  pCallBack,
                                LPVOID          hCallBack)
{
    _ASSERTE( !"NYI" );
}

#endif // Unknown platform


OBJECTREF* GcInfoDecoder::GetStackSlot(
                        INT32           spOffset,
                        GcStackSlotBase spBase,
                        PREGDISPLAY     pRD
                        )
{
    OBJECTREF* pObjRef;

    if( GC_SP_REL == spBase )
    {
        pObjRef = (OBJECTREF*) ((SIZE_T)GetRegdisplaySP(pRD) + spOffset);
    }
    else if( GC_CALLER_SP_REL == spBase )
    {
        pObjRef = (OBJECTREF*) (GET_CALLER_SP(pRD) + spOffset);
    }
    else
    {
        _ASSERTE( GC_FRAMEREG_REL == spBase );
        _ASSERTE( NO_STACK_BASE_REGISTER != m_StackBaseRegister );

        pObjRef = (OBJECTREF*)((*((INT64*)(GetRegisterSlot( m_StackBaseRegister, pRD )))) + spOffset);
    }

    return pObjRef;
}

void GcInfoDecoder::ReportStackSlotToGC(
                                INT32           spOffset,
                                GcStackSlotBase spBase,
                                BOOL            isInterior,
                                BOOL            isPinned,
                                PREGDISPLAY     pRD,
                                unsigned        flags,
                                GCEnumCallback  pCallBack,
                                LPVOID          hCallBack)
{
    GCINFODECODER_CONTRACT(CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END);

    OBJECTREF* pObjRef = GetStackSlot(spOffset, spBase, pRD);
    _ASSERTE( IS_ALIGNED( pObjRef, sizeof( Object* ) ) );

#ifdef _DEBUG
    LOG((LF_GCROOTS, LL_INFO1000, /* Part One */
             "Reporting %s" FMT_STK,
             ( (GC_SP_REL        == spBase) ? "" :
              ((GC_CALLER_SP_REL == spBase) ? "caller's " :
              ((GC_FRAMEREG_REL  == spBase) ? "frame " : "<unrecognized GcStackSlotBase> "))),
             DBG_STK(spOffset) ));

    LOG((LF_GCROOTS, LL_INFO1000, /* Part Two */
         "at" FMT_ADDR "as ", DBG_ADDR(pObjRef) ));

    VALIDATE_ROOT(isInterior, hCallBack, pObjRef);

    LOG((LF_GCROOTS, LL_INFO1000, /* Part Three */
         LOG_PIPTR_OBJECT_CLASS(OBJECTREF_TO_UNCHECKED_OBJECTREF(*pObjRef), isPinned, isInterior)));
#endif

    DWORD gcFlags = CHECK_APP_DOMAIN;

    if (isInterior)
        gcFlags |= GC_CALL_INTERIOR;

    if (isPinned)
        gcFlags |= GC_CALL_PINNED;

    pCallBack(hCallBack, pObjRef, gcFlags);
}

}

#endif // USE_GC_INFO_DECODER
#endif // VERIFY_GCINFO
