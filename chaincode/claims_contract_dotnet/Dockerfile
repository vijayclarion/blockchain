# Use the official .NET SDK image to build the chaincode
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ClaimsContract.csproj", "./"]
RUN dotnet restore "ClaimsContract.csproj"
COPY . .
RUN dotnet publish "ClaimsContract.csproj" -c Release -o /app/publish

# Use the official .NET runtime image for the final container
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app
COPY --from=build /app/publish .
ENV CORE_CHAINCODE_ID_NAME=claims_cc:1.0
ENV CORE_PEER_TLS_ENABLED=false
# The entrypoint to run the chaincode
ENTRYPOINT ["dotnet", "ClaimsContract.dll"]
