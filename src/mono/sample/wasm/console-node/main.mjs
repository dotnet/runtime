import { dotnet } from './dotnet.js'

dotnet
    .withDiagnosticTracing(false)
    .withApplicationArguments("dotnet", "is", "great!")
    .run()