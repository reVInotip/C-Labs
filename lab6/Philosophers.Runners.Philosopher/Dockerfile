# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. Копируем csproj файлы
COPY ["Philosophers.Runners.Philosopher/Core/Core.csproj", "Philosophers.Runners.Philosopher/Core/"]
COPY ["DataContracts/DataContracts.csproj", "DataContracts/"]
COPY ["InterfaceContracts/InterfaceContracts.csproj", "InterfaceContracts/"]

# 2. Восстанавливаем зависимости
WORKDIR /src/Philosophers.Runners.Philosopher/Core
RUN dotnet restore "Core.csproj"

# 3. Копируем весь исходный код
WORKDIR /src
COPY . .

# 4. Сборка
WORKDIR /src/Philosophers.Runners.Philosopher/Core
RUN dotnet build "Core.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Core.csproj" -c Release -o /app/publish

# Этап рантайма
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Core.dll"]