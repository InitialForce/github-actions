# Set the base image as the .NET 6.0 SDK (this includes the runtime)
FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim  as build-env

# Copy everything and publish the release (publish implicitly restores and builds)
COPY . ./
RUN dotnet publish ./GitHubActions/GitHubActions.Report.csproj -c Release -o out --no-self-contained

# Label the container
LABEL maintainer="Leonardo Ferreira <leonardo@initialforce.com>"
LABEL repository="https://github.com/initialforce/github-actions"
LABEL homepage="https://github.com/initialforce/github-actions"

# Label as GitHub action
LABEL com.github.actions.name="ReSharper Inspection Report"
LABEL com.github.actions.description="Generates the GitHub Actions workflow report based on ReSharper inspection output."
LABEL com.github.actions.icon="alert-circle"
LABEL com.github.actions.color="orange"

# Relayer the .NET SDK, a new with the build output
FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim 
COPY --from=build-env /out .
ENTRYPOINT [ "dotnet", "/GitHubActions.Report.dll" ]