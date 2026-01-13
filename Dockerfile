# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["BulkyWebV01.sln", "."]
COPY ["BulkyWebV01/BulkyWebV01.csproj", "BulkyWebV01/"]
COPY ["Bulky.DataAccess/Bulky.DataAccess.csproj", "Bulky.DataAccess/"]
COPY ["Bulky.Models/Bulky.Models.csproj", "Bulky.Models/"]
COPY ["Bulky.Utility/Bulky.Utility.csproj", "Bulky.Utility/"]

# Restore dependencies
RUN dotnet restore "BulkyWebV01/BulkyWebV01.csproj"

# Copy everything else
COPY . .

# Build the application
WORKDIR "/src/BulkyWebV01"
RUN dotnet build "BulkyWebV01.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "BulkyWebV01.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create directory for SQLite database
RUN mkdir -p /app/data

EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "BulkyWebV01.dll"]
