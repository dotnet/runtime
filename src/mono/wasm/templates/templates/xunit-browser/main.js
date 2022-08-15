import { dotnet } from './dotnet.js'

await dotnet
    .withApplicationArgumentsFromQuery()
    .run();
