#include "internal.h"

bool md_is_field_sig(uint8_t const* sig, size_t sig_len)
{
    if (sig_len == 0)
    {
        return false;
    }

    return (*sig & IMAGE_CEE_CS_CALLCONV_MASK) == IMAGE_CEE_CS_CALLCONV_FIELD;
}

static bool skip_if_sentinel(uint8_t const** sig, size_t* sig_length)
{
    assert(sig != NULL && sig_length != NULL && *sig_length != 0);

    if ((*sig)[0] == ELEMENT_TYPE_SENTINEL)
    {
        advance_stream(sig, sig_length, 1);
        return true;
    }
    return false;
}

// Given a signature buffer, skips over a parameter or return type as defined by the following sections of the ECMA spec:
// II.23.2.10 Param 
// II.23.2.11 RetType
// II.23.2.12 Type
static bool skip_sig_element(uint8_t const** sig, size_t* sig_length)
{
    assert(sig != NULL && sig_length != NULL && *sig_length != 0);

    uint8_t elem_type;
    uint32_t ignored_compressed_u32_arg;
    if (!read_u8(sig, sig_length, &elem_type))
    {
        return false;
    }

    assert(elem_type != ELEMENT_TYPE_SENTINEL && "The SENTINEL element should be handled by the caller.");

    switch (elem_type)
    {
        case ELEMENT_TYPE_VOID:
        case ELEMENT_TYPE_BOOLEAN:
        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R4:
        case ELEMENT_TYPE_R8:
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_OBJECT:
        case ELEMENT_TYPE_TYPEDBYREF:
        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_U:
            return true;
        case ELEMENT_TYPE_FNPTR:
        {
            // We need to read a whole MethodDefSig (II.23.2.1) or MethodRefSig (II.23.2.2) here
            // See II.23.2.12 Type for more details
            uint8_t call_conv;
            if (!read_u8(sig, sig_length, &call_conv))
            {
                return false;
            }
            uint32_t generic_arg_count = 0;
            if ((call_conv & IMAGE_CEE_CS_CALLCONV_GENERIC) == IMAGE_CEE_CS_CALLCONV_GENERIC)
            {
                if (!decompress_u32(sig, sig_length, &generic_arg_count))
                {
                    return false;
                }
            }
            uint32_t param_count;
            if (!decompress_u32(sig, sig_length, &param_count))
            {
                return false;
            }

            // skip return type
            if (!skip_sig_element(sig, sig_length))
            {
                return false;
            }

            // skip parameters
            for (uint32_t i = 0; i < param_count; i++)
            {
                // If we see the SENTINEL element, we'll skip it.
                // As defined in II.23.2.2, the ParamCount field caounts the number of
                // Param instances, and SENTINEL is a separate entity in the signature than the Param instances.
                (void)skip_if_sentinel(sig, sig_length);
                if (!skip_sig_element(sig, sig_length))
                {
                    return false;
                }
            }
            return true;
        }
        case ELEMENT_TYPE_PTR:
        case ELEMENT_TYPE_BYREF:
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_PINNED:
            return skip_sig_element(sig, sig_length);

        case ELEMENT_TYPE_VAR:
        case ELEMENT_TYPE_MVAR:
        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_CLASS:
            return decompress_u32(sig, sig_length, &ignored_compressed_u32_arg);

        case ELEMENT_TYPE_CMOD_REQD:
        case ELEMENT_TYPE_CMOD_OPT:
            if (!decompress_u32(sig, sig_length, &ignored_compressed_u32_arg))
                return false;
            return skip_sig_element(sig, sig_length);

        case ELEMENT_TYPE_ARRAY:
        {
            // type
            if (!skip_sig_element(sig, sig_length))
                return false;
            // rank
            if (!decompress_u32(sig, sig_length, &ignored_compressed_u32_arg))
                return false;

            uint32_t bound_count;
            if (!decompress_u32(sig, sig_length, &bound_count))
                return false;
            
            for (uint32_t i = 0; i < bound_count; i++)
            {
                // bound
                if (!decompress_u32(sig, sig_length, &ignored_compressed_u32_arg))
                    return false;
            }

            uint32_t lbound_count;
            if (!decompress_u32(sig, sig_length, &lbound_count))
                return false;
            
            for (uint32_t i = 0; i < lbound_count; i++)
            {
                int32_t ignored_compressed_i32_arg;
                // lbound
                if (!decompress_i32(sig, sig_length, &ignored_compressed_i32_arg))
                    return false;
            }
            return true;
        }
        case ELEMENT_TYPE_GENERICINST:
        {
            // class or value type
            if (!advance_stream(sig, sig_length, 1))
                return false;
            // token
            if (!decompress_u32(sig, sig_length, &ignored_compressed_u32_arg))
                return false;
            uint32_t num_generic_args;
            if (!decompress_u32(sig, sig_length, &num_generic_args))
                return false;
            for (uint32_t i = 0; i < num_generic_args; ++i)
            {
                if (!skip_sig_element(sig, sig_length))
                    return false;
            }
            return true;
        }
    }
    assert(false && "Unknown element type");
    return false;
}

bool md_get_methoddefsig_from_methodrefsig(uint8_t const* sig, size_t ref_sig_len, uint8_t** def_sig, size_t* def_sig_len)
{
    if (sig == NULL || ref_sig_len == 0 || def_sig == NULL || def_sig_len == NULL)
    {
        return false;
    }

    uint8_t const* curr = sig;
    size_t curr_len = ref_sig_len;

    // Consume the calling convention
    uint8_t call_conv;
    if (!read_u8(&curr, &curr_len, &call_conv))
    {
        return false;
    }
    
    // The MethodDefSig is the same as the MethodRefSig if the calling convention is not vararg.
    // Only in the vararg case does the MethodRefSig have additional data to describe the exact vararg
    // parameter list.
    if ((call_conv & IMAGE_CEE_CS_CALLCONV_MASK) != IMAGE_CEE_CS_CALLCONV_VARARG)
    {
        *def_sig_len = ref_sig_len;
        *def_sig = malloc(*def_sig_len);
        memcpy(*def_sig, sig, *def_sig_len);
        return true;
    }

    // Consume the generic parameter count
    uint32_t generic_param_count = 0;
    if (call_conv & IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
        if (!decompress_u32(&curr, &curr_len, &generic_param_count))
        {
            return false;
        }
    }

    // Consume the parameter count
    uint32_t param_count;
    if (!decompress_u32(&curr, &curr_len, &param_count))
    {
        return false;
    }
    
    uint8_t const* return_and_parameter_start = curr;
    // Skip return type
    if (!skip_sig_element(&curr, &curr_len))
    {
        return false;
    }
    // Skip parameter types until we see the sentinel
    uint32_t i = 0;
    uint8_t const* def_sig_end = curr;
    for (; i < param_count; i++, def_sig_end = curr)
    {
        if (skip_if_sentinel(&curr, &curr_len))
        {
            break;
        }

        if (!skip_sig_element(&curr, &curr_len))
        {
            return false;
        }
    }

    // Now that we know the number of parameters, we can copy the MethodDefSig portion of the signature
    // and update the parameter count.
    // We need to account for the fact that the parameter count may be encoded with less bytes
    // as it is emitted using the compressed unsigned integer format.
    // A compressed integer can take up a maximum of 4 bytes, so we only need a four-byte buffer here.
    uint8_t encoded_original_param_count[4];
    size_t encoded_original_param_count_length = 4;
    compress_u32(param_count, encoded_original_param_count, &encoded_original_param_count_length);

    uint8_t encoded_def_param_count[4];
    size_t encoded_def_param_count_length = 4;
    compress_u32(i, encoded_def_param_count, &encoded_def_param_count_length);

    size_t def_sig_buffer_len = *def_sig_len = (uint32_t)(def_sig_end - sig) - encoded_original_param_count_length + encoded_def_param_count_length;
    uint8_t * def_sig_buffer = *def_sig = malloc(def_sig_buffer_len);
    if (!def_sig_buffer)
        return false;

    def_sig_buffer[0] = call_conv;
    advance_stream(&def_sig_buffer, &def_sig_buffer_len, 1);

    if (call_conv & IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
        size_t used_len;
        compress_u32(generic_param_count, def_sig_buffer, &used_len);
        advance_stream(&def_sig_buffer, &def_sig_buffer_len, used_len);
    }
    memcpy(def_sig_buffer, encoded_def_param_count, encoded_def_param_count_length);
    advance_stream(&def_sig_buffer, &def_sig_buffer_len, encoded_def_param_count_length);
    
    // Now that we've re-written the parameter count, we can copy the rest of the signature directly from the MethodRefSig
    memcpy(def_sig_buffer, return_and_parameter_start, def_sig_buffer_len);

    return true;
}