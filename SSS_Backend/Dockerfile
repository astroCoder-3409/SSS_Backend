# Use the official .NET SDK image as the base image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
COPY *.xml ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish SSS_Backend.csproj -c Release -o out

# Use the official .NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /app/out .

# Expose the port the app runs on
EXPOSE 80
EXPOSE 443

# Set the entry point
ENTRYPOINT ["dotnet", "SSS_Backend.dll"]
