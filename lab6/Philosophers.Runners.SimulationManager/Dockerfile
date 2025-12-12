# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Philosophers.Runners.SimulationManager/Core/Core.csproj", "Philosophers.Runners.SimulationManager/Core/"]
COPY ["DataContracts/DataContracts.csproj", "DataContracts/"]
COPY ["InterfaceContracts/InterfaceContracts.csproj", "InterfaceContracts/"]

# 2. Восстанавливаем зависимости основного проекта
WORKDIR /src/Philosophers.Runners.SimulationManager/Core
RUN dotnet restore "Core.csproj"

# 3. Копируем весь исходный код
WORKDIR /src
COPY . .

# 4. Сборка
WORKDIR /src/Philosophers.Runners.SimulationManager/Core
RUN dotnet build "Core.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Core.csproj" -c Release -o /app/publish

# Этап рантайма
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Core.dll"]