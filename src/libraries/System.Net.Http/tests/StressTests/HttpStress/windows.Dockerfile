# escape=`
ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/nightly/sdk:5.0-nanoserver-1809
FROM $SDK_BASE_IMAGE

# Use powershell as the default shell
SHELL ["pwsh", "-Command"]

WORKDIR /app
COPY . .

ARG CONFIGURATION=Release
RUN dotnet build -c $env:CONFIGURATION

EXPOSE 5001

ENV CONFIGURATION=$CONFIGURATION
ENV HTTPSTRESS_ARGS=""
CMD dotnet run --no-build -c $env:CONFIGURATION -- $env:HTTPSTRESS_ARGS.Split()
