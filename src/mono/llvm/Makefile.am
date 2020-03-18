if ENABLE_LLVM

if HOST_WIN32
llvm_config=llvm-config.exe
else
llvm_config=llvm-config
endif

if HAVE_STATIC_ZLIB
llvm_extra_libs = $(STATIC_ZLIB_PATH)
else
if HAVE_SYS_ZLIB
llvm_extra_libs=-lz
else
llvm_extra_libs=
endif
endif

if INTERNAL_LLVM_ASSERTS
ENABLE_ASSERTS=On
else
ENABLE_ASSERTS=Off
endif

all-local: llvm_config.mk

clean-local: clean-llvm-config

$(mono_build_root)/llvm/llvm_config.mk: $(top_srcdir)/llvm/Makefile.am
	$(top_srcdir)/llvm/build_llvm_config.sh "$(EXTERNAL_LLVM_CONFIG)" "$(LLVM_CODEGEN_LIBS)" "$(llvm_extra_libs)" | tee $@

llvm_config.mk: $(mono_build_root)/llvm/llvm_config.mk

clean-llvm-config:
	- rm -rf llvm_config.mk

else
all-local: 

clean-local: 
endif

# Override this so we don't try to re-copy llvm when we install mono
install:

if TARGET_WASM
LLVM_TARGET=wasm32
else
LLVM_TARGET=
endif

