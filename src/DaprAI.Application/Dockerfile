FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 5050

ENV ASPNETCORE_URLS=http://+:5050

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/DaprAI.Application/DaprAI.Application.csproj", "src/DaprAI.Application/"]
RUN dotnet restore "src/DaprAI.Application/DaprAI.Application.csproj"
COPY . .
WORKDIR "/src/src/DaprAI.Application"
RUN dotnet build "DaprAI.Application.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DaprAI.Application.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DaprAI.Application.dll"]
