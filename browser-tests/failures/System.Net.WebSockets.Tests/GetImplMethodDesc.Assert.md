# Test: GetImplMethodDesc Assert

## Test Suite
System.Net.WebSockets.Tests

## Failure Type
assertion

## Exception Type
CoreCLR Debug Assert

## Stack Trace
```
ASSERT FAILED
        Expression: slotNumber >= GetNumVirtuals() || pMDRet == m_pDeclMT->GetMethodDescForSlot_NoThrow(slotNumber)
        Location:   /workspaces/runtime/src/coreclr/vm/methodtable.cpp:6963
        Function:   GetImplMethodDesc
        Process:    42
Frame (InterpreterFrame): 0x4fff2c
   0) MessageSinkMessageExtensions::Dispatch, IR_0037
   1) Microsoft.DotNet.XHarness.TestRunners.Xunit.CompletionCallbackExecutionSink::OnMessageWithTypes, IR_0030
   2) Xunit.MessageSinkAdapter::OnMessageWithTypes, IR_0015
   3) Xunit.OptimizedRemoteMessageSink::OnMessage, IR_001c
   4) Xunit.Sdk.SynchronousMessageBus::QueueMessage, IR_0032
   5) <RunAsync>d__41[__Canon]::MoveNext, IR_036e
   ... (test runner async machinery)
```

Second occurrence:
```
ASSERT FAILED
        Expression: slotNumber >= GetNumVirtuals() || pMDRet == m_pDeclMT->GetMethodDescForSlot_NoThrow(slotNumber)
        Location:   /workspaces/runtime/src/coreclr/vm/methodtable.cpp:6963
        Function:   GetImplMethodDesc
        Process:    42
Frame (InterpreterFrame): 0x4fff2c
   0) <>c::<.cctor>b__289_0, IR_002a
   1) System.Threading.ExecutionContext::RunFromThreadPoolDispatchLoop, IR_0060
   2) System.Threading.Tasks.Task::ExecuteWithThreadLocal, IR_0119
   3) System.Threading.Tasks.Task::ExecuteEntryUnsafe, IR_0056
   4) System.Threading.Tasks.Task::ExecuteFromThreadPool, IR_000e
   5) System.Threading.ThreadPoolWorkQueue::DispatchWorkItem, IR_0028
   6) System.Threading.ThreadPoolWorkQueue::Dispatch, IR_0140
   7) System.Threading.ThreadPool::BackgroundJobHandler, IR_0012
```

## Notes
- Platform: Browser/WASM + CoreCLR
- Category: interpreter
- **Non-fatal:** Test run completed successfully (exit code 0), all 268 tests passed
- Assert occurred **after** tests finished, during cleanup/finalization phase
- Occurred twice during the same test run
- Likely related to CoreCLR interpreter method dispatch table handling
- May be related to known "finalizers don't work" limitation on WASM
