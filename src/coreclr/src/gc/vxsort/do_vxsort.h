// Enum for the GCToOSInterface::SupportsInstructionSet method
enum class InstructionSet
{
    AVX2 = 0,
    AVX512F = 1,
};

bool SupportsInstructionSet(InstructionSet instructionSet);

void do_vxsort_avx2(uint8_t** low, uint8_t** high);
void do_vxsort_avx2(uint32_t* low, uint32_t* high);
void do_vxsort_avx512(uint8_t** low, uint8_t** high);
void do_vxsort_avx512(uint32_t* low, uint32_t* high);
