# Development Environments

Alternative ways to set up your development environment for dotnet/runtime.

## Cloud Development

### [GitHub Codespaces](codespaces.md)

Develop in a pre-configured cloud environment with no local setup required.

- **Pros:** Zero setup, pre-built runtime, works from any browser
- **Best for:** Quick contributions, trying out the repo
- **Cost:** Free tier available, then pay-per-use

## Containerized Development

### [Docker](docker.md)

Build in containers for consistent, reproducible environments.

- **Pros:** Matches CI environment, easy cross-compilation
- **Best for:** Cross-platform builds, reproducing CI issues
- **Requirements:** Docker installed on your machine

## Local Development

### Visual Studio (Windows)

Full IDE experience for Windows development.

- **Pros:** Rich debugging, IntelliSense, integrated testing
- **Best for:** Windows-focused development, native code debugging
- **Setup:** See [Windows Requirements](../requirements/windows-requirements.md)

See [Editing and Debugging](../editing-and-debugging.md) for Visual Studio solutions.

### VS Code (Cross-Platform)

Lightweight editor with debugging support.

- **Pros:** Cross-platform, fast, extensible
- **Best for:** Libraries development, managed code debugging
- **Setup:** Install C# extension

See [Debugging with VS Code](../debugging/libraries/debugging-vscode.md) for setup.

### Command Line

Build and test entirely from the terminal.

- **Pros:** Scriptable, no IDE overhead
- **Best for:** CI/CD, automation, remote development
- **Setup:** See platform requirements for your OS

## Comparison

| Feature | Codespaces | Docker | VS | VS Code | CLI |
|---------|------------|--------|-----|---------|-----|
| No local setup | ✓ | | | | |
| Pre-built runtime | ✓ | | | | |
| Native debugging | | | ✓ | partial | partial |
| Cross-compilation | ✓ | ✓ | | | |
| Full IntelliSense | ✓ | | ✓ | ✓ | |
| Offline work | | ✓ | ✓ | ✓ | ✓ |
