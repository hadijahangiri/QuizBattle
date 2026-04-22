FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY QuizBattle.sln ./
COPY src/QuizBattle.AdminPanel/QuizBattle.AdminPanel.csproj src/QuizBattle.AdminPanel/
COPY src/QuizBattle.Infrastructure/QuizBattle.Infrastructure.csproj src/QuizBattle.Infrastructure/
COPY src/QuizBattle.Application/QuizBattle.Application.csproj src/QuizBattle.Application/
COPY src/QuizBattle.Domain/QuizBattle.Domain.csproj src/QuizBattle.Domain/
COPY src/QuizBattle.Shared/QuizBattle.Shared.csproj src/QuizBattle.Shared/
RUN dotnet restore src/QuizBattle.AdminPanel/QuizBattle.AdminPanel.csproj

COPY . .
WORKDIR /src/src/QuizBattle.AdminPanel
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "QuizBattle.AdminPanel.dll"]
