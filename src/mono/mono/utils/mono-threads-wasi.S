// TODO after https://github.com/llvm/llvm-project/commit/1532be98f99384990544bd5289ba339bca61e15b
// use __stack_low && __stack_high

.globl get_wasm_data_end
.globl get_wasm_heap_base

get_wasm_data_end:
  .functype get_wasm_data_end () -> (i32)
  global.get __data_end@GOT
  end_function

get_wasm_heap_base:
  .functype get_wasm_heap_base () -> (i32)
  global.get __heap_base@GOT
  end_function

.globl get_wasm_stack_low
.globl get_wasm_stack_high

get_wasm_stack_low:
  .functype get_wasm_stack_low () -> (i32)
  global.get __stack_low@GOT
  end_function

get_wasm_stack_high:
  .functype get_wasm_stack_high () -> (i32)
  global.get __stack_high@GOT
  end_function
