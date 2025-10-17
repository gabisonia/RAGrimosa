# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY RAGrimosa/RAGrimosa.csproj RAGrimosa/
RUN dotnet restore RAGrimosa/RAGrimosa.csproj

# Copy the remaining source code and publish
COPY . .
RUN dotnet publish RAGrimosa/RAGrimosa.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "RAGrimosa.dll"]
