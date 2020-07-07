ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/nightly/sdk:5.0-buster-slim
FROM $SDK_BASE_IMAGE

WORKDIR /app
COPY . .
WORKDIR /app/System.Net.Security/tests/StressTests/SslStress 

ARG CONFIGURATION=Release
RUN dotnet build -c $CONFIGURATION

EXPOSE 5001

ENV CONFIGURATION=$CONFIGURATION
ENV SSLSTRESS_ARGS=''
CMD dotnet run --no-build -c $CONFIGURATION -- $SSLSTRESS_ARGS
