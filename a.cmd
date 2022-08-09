rem call dotnet.exe build /p:TargetOS=Browser /p:TargetArchitecture=wasm /p:Configuration=Debug /t:Test src\libraries\System.Security.Cryptography\tests /p:Scenario=WasmTestOnBrowser /p:UseSubtleCryptoForTests=true /p:WasmTestAppArgs="-class System.Security.Cryptography.Encryption.Aes.Tests.AesContractTests" 
pushd C:\Dev\runtime\artifacts\bin\System.Security.Cryptography.Tests\Debug\net7.0-browser\browser-wasm\AppBundle\

:again
@echo %date% %time%
dotnet.exe xharness wasm test-browser --app=. --output-directory=C:\Dev\runtime\artifacts\bin\System.Security.Cryptography.Tests\Debug\net7.0-browser\browser-wasm\AppBundle\xharness-output%1  --web-server-use-cop -s dotnet.js.symbols --symbol-patterns wasm-symbol-patterns.txt --symbolicator WasmSymbolicator.dll,Microsoft.WebAssembly.Internal.SymbolicatorWrapperForXHarness       --   --setenv=TEST_EXPECT_SUBTLE_CRYPTO=true --setenv=XHARNESS_LOG_TEST_START=true  --run WasmTestRunner.dll System.Security.Cryptography.Tests.dll -class System.Security.Cryptography.Encryption.Aes.Tests.AesContractTests  -notrait category=OuterLoop -notrait category=failing
IF %ERRORLEVEL% EQU 0 goto again

popd