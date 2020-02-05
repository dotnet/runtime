DOTNET=../../../.dotnet/dotnet

# run sample using local .dotnet (will be patched with Mono Runtime)
run-sample:
	$(DOTNET) msbuild /t:RunSample ../mono.proj

# run sample using dotnet from PATH
run-sample-coreclr:
	dotnet run -c Release -p sample/HelloWorld -f netcoreapp3.1

runtime:
	$(DOTNET) msbuild /t:Build ../mono.proj

# temp: makes $(DOTNET) to use mono runtime (to run real-world apps using '$(DOTNET) run')
patch-local-dotnet:
	$(DOTNET) msbuild /t:PatchLocalDotnet ../mono.proj

restore-local-dotnet:
	$(DOTNET) msbuild /t:RestoreLocalDotnet ../mono.proj

# run specific coreclr test, e.g.:
# make run-tests-coreclr CoreClrTest="bash ../../artifacts/tests/coreclr/OSX.x64.Checked/JIT/opt/InstructionCombining/DivToMul/DivToMul.sh"
run-tests-coreclr:
	$(DOTNET) msbuild /t:RunCoreClrTest /p:CoreClrTest="$(CoreClrTest)" ../mono.proj

# run all coreclr tests
run-tests-coreclr-all:
	$(DOTNET) msbuild /t:RunCoreClrTests ../mono.proj

# show summary for coreclr tests
tests-coreclr-summary:
	$(DOTNET) msbuild /t:CoreClrTestsSummary ../mono.proj

# run 'dotnet/performance' benchmarks
# e.g. 'make run-benchmarks BenchmarksRepo=/prj/performance'
# you can append BDN parameters at the end, e.g. ` -- --filter Burgers --keepFiles`
run-benchmarks: patch-local-dotnet
	$(DOTNET) msbuild /t:RunBenchmarks /p:BenchmarksRepo=$(BenchmarksRepo)