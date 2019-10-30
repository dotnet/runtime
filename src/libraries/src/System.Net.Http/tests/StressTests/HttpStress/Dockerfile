ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/core/sdk:3.0.100-buster
FROM $SDK_BASE_IMAGE

WORKDIR /app
COPY . .

ARG CONFIGURATION=Release
RUN dotnet build -c $CONFIGURATION

EXPOSE 5001

ENV CONFIGURATION=$CONFIGURATION
ENV HTTPSTRESS_ARGS='-maxExecutionTime 30 -displayInterval 60'
CMD dotnet run --no-build -c $CONFIGURATION -- $HTTPSTRESS_ARGS
