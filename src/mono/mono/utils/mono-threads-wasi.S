.globl get_wasm_stack_high
.globl get_wasm_stack_low

get_wasm_stack_high:
  .functype get_wasm_stack_high () -> (i32)
  global.get __data_end@GOT
  // TODO after https://github.com/llvm/llvm-project/commit/1532be98f99384990544bd5289ba339bca61e15b
  // global.get __stack_high@GOT
  end_function

get_wasm_stack_low:
  .functype get_wasm_stack_low () -> (i32)
  global.get __heap_base@GOT
  // TODO after https://github.com/llvm/llvm-project/commit/1532be98f99384990544bd5289ba339bca61e15b
  // global.get __stack_low@GOT
  end_function
