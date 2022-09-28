FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

# Copy everything
COPY . ./
WORKDIR /app/InteropTestsWebsite
RUN dotnet --info
RUN dotnet restore
RUN dotnet publish --framework net6.0 -c Release -o out -p:LatestFramework=true

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/InteropTestsWebsite/out .
ENTRYPOINT ["dotnet", "InteropTestsWebsite.dll", "--use_tls", "true"]
