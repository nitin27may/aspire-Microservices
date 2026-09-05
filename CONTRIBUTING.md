# Contributing

Thanks for your interest in improving this project. It's a reference implementation for .NET Aspire microservices patterns, so contributions that improve clarity, correctness, or demonstrate additional patterns are welcome.

## Getting Started

1. Fork the repository and clone your fork.
2. Install the prerequisites listed in the [README](readme.md#prerequisites).
3. Run the app via the AppHost to confirm your environment works before making changes:
   ```bash
   cd src/ECommerce.AppHost
   dotnet run
   ```

## Making Changes

- Keep changes focused — one concern per pull request.
- Follow the existing code style. Run `dotnet format` before committing:
  ```bash
  dotnet format ECommerce.sln
  ```
- Build the solution and confirm it's warning-free:
  ```bash
  dotnet build ECommerce.sln -warnaserror
  ```
- If you change a microservice's public API, update the corresponding section in the README.

## Submitting a Pull Request

1. Push your branch and open a PR against `main`.
2. Describe what changed and why — link any related issue.
3. Ensure the CI workflow passes (build + format check).
4. Be responsive to review feedback; small, iterative PRs merge faster than large ones.

## Reporting Issues

Use the issue templates for bug reports and feature requests. Include:
- .NET SDK version (`dotnet --version`)
- Steps to reproduce
- Expected vs. actual behavior
- Relevant logs from the Aspire Dashboard, if applicable

## Code of Conduct

Be respectful and constructive. This is a learning resource — questions and beginner-friendly PRs are welcome.
