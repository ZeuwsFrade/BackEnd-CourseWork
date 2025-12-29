@echo off
echo Запуск всех микросервисов и Gateway...

start cmd /k "cd AuthService && echo Запуск Auth Service... && dotnet run --urls=http://localhost:5001"
timeout /t 3 /nobreak > nul

start cmd /k "cd ProductService && echo Запуск Product Service... && dotnet run --urls=http://localhost:5149"
timeout /t 3 /nobreak > nul

start cmd /k "cd OrderService && echo Запуск Order Service... && dotnet run --urls=http://localhost:5003"
timeout /t 3 /nobreak > nul

start cmd /k "cd ChatService && echo Запуск Chat Service... && dotnet run --urls=http://localhost:5004"
timeout /t 5 /nobreak > nul

start cmd /k "cd ApiGateway && echo Запуск API Gateway... && dotnet run --urls=http://localhost:5000"
timeout /t 3 /nobreak > nul

echo Все сервисы запущены!
echo API Gateway: http://localhost:5000
echo Нажмите любую клавишу для выхода...
pause > nul