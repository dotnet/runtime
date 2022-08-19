import { dotnet } from './dotnet.js'

dotnet
    .withDiagnosticTracing(false)
    .withApplicationArguments(...arguments)
    .run()