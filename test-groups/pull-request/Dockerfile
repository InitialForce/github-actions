# Set the base image as the .NET 5.0 SDK (this includes the runtime)
FROM mcr.microsoft.com/dotnet/sdk:5.0 as build-env

# Copy everything and publish the release (publish implicitly restores and builds)
COPY . ./
RUN dotnet publish ./GitHubActions/GitHubActions.PullRequest.csproj -c Release -o out --no-self-contained

# Label the container
LABEL maintainer="Leonardo Ferreira <leonardo@initialforce.com>"
LABEL repository="https://github.com/initialforce/github-actions"
LABEL homepage="https://github.com/initialforce/github-actions"

# Label as GitHub action
LABEL com.github.actions.name="Test Categories per Pull Request"
LABEL com.github.actions.description="Gets the test categories present in the assembly and related issues (for use in matrix strategies)."
LABEL com.github.actions.icon="list"
LABEL com.github.actions.color="orange"

# Relayer the .NET SDK, anew with the build output
FROM mcr.microsoft.com/dotnet/sdk:5.0
COPY --from=build-env /out .
ENTRYPOINT [ "dotnet", "/GitHubActions.PullRequest.dll" ]