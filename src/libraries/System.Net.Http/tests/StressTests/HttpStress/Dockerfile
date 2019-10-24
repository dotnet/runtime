ARG SDK_BASE_IMAGE=mcr.microsoft.com/dotnet/core/sdk:3.0.100-buster
FROM $SDK_BASE_IMAGE

WORKDIR /app
COPY . .

ENV HTTPSTRESS_ARGS='-maxExecutionTime 30 -displayInterval 60'
CMD dotnet run -c Release -- $HTTPSTRESS_ARGS
