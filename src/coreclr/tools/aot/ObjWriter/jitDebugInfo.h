#ifndef JIT_DEBUG_INFO_H
#define JIT_DEBUG_INFO_H

typedef unsigned int DWORD;

#define PORTABILITY_WARNING(msg)
#include "cordebuginfo.h"

// RegNum enumeration is architecture-specific and we need it for all
// architectures we support.

namespace X86 {
#define TARGET_X86 1
#include "cordebuginfo.h"
#undef TARGET_X86
}

namespace Amd64 {
#define TARGET_AMD64 1
#include "cordebuginfo.h"
#undef TARGET_AMD64
}

namespace Arm {
#define TARGET_ARM 1
#include "cordebuginfo.h"
#undef TARGET_ARM
}

namespace Arm64 {
#define TARGET_ARM64 1
#include "cordebuginfo.h"
#undef TARGET_ARM64
}

struct DebugLocInfo {
  int NativeOffset;
  int FileId;
  int LineNumber;
  int ColNumber;
};

struct DebugVarInfo {
  std::string Name;
  int TypeIndex;
  bool IsParam;
  std::vector<ICorDebugInfo::NativeVarInfo> Ranges;

  DebugVarInfo() {}
  DebugVarInfo(char *ArgName, int ArgTypeIndex, bool ArgIsParam)
      : Name(ArgName), TypeIndex(ArgTypeIndex), IsParam(ArgIsParam) {}
};

struct DebugEHClauseInfo {
  unsigned TryOffset;
  unsigned TryLength;
  unsigned HandlerOffset;
  unsigned HandlerLength;

  DebugEHClauseInfo(unsigned TryOffset, unsigned TryLength,
                    unsigned HandlerOffset, unsigned HandlerLength) :
                    TryOffset(TryOffset), TryLength(TryLength),
                    HandlerOffset(HandlerOffset), HandlerLength(HandlerLength) {}
};

#endif // JIT_DEBUG_INFO_H
