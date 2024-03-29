FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

RUN mkdir /tmp/dapr-components-sockets

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app && chown -R appuser /tmp/dapr-components-sockets
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/DaprAI.Components/DaprAI.Components.csproj", "src/DaprAI.Components/"]
RUN dotnet restore "src/DaprAI.Components/DaprAI.Components.csproj"
COPY . .
WORKDIR "/src/src/DaprAI.Components"
RUN dotnet build "DaprAI.Components.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DaprAI.Components.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DaprAI.Components.dll"]
