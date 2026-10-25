# Use the explicit Noble (Ubuntu 24.04) tags for .NET 10
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["LeagueVisionSimple.csproj", "./"]
RUN dotnet restore "LeagueVisionSimple.csproj"

# Copy the rest of the files and build
COPY . .
RUN dotnet publish "LeagueVisionSimple.csproj" -c Release -o /app/publish

# Use the explicit Noble runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render expects the app to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LeagueVisionSimple.dll"]