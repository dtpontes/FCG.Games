# Script PowerShell para resolver problemas do Service Bus - Versão Atualizada

Write-Host "=== Diagnóstico e Correção do Service Bus ===" -ForegroundColor Green

# Verificar se Azure CLI está instalado
try {
    $azVersion = az version --output tsv 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? Azure CLI não encontrado!" -ForegroundColor Red
        Write-Host "   Instale em: https://docs.microsoft.com/cli/azure/install-azure-cli" -ForegroundColor Yellow
        exit 1
    }
    Write-Host "? Azure CLI encontrado" -ForegroundColor Green
} catch {
    Write-Host "? Azure CLI não encontrado!" -ForegroundColor Red
    Write-Host "   Instale em: https://docs.microsoft.com/cli/azure/install-azure-cli" -ForegroundColor Yellow
    exit 1
}

# 1. Verificar se está logado no Azure
Write-Host "`n1. Verificando login no Azure..." -ForegroundColor Yellow
try {
    $account = az account show --query "user.name" -o tsv 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? Você precisa fazer login no Azure" -ForegroundColor Red
        Write-Host "   Executando login..." -ForegroundColor Yellow
        az login
        if ($LASTEXITCODE -ne 0) {
            Write-Host "? Erro no login" -ForegroundColor Red
            exit 1
        }
        $account = az account show --query "user.name" -o tsv
    }
    Write-Host "? Logado como: $account" -ForegroundColor Green
} catch {
    Write-Host "? Erro ao verificar login" -ForegroundColor Red
    exit 1
}

# 2. Verificar se o resource group existe
Write-Host "`n2. Verificando resource group..." -ForegroundColor Yellow
try {
    $rgExists = az group exists --name "fcg-games-rg" 2>$null
    if ($rgExists -eq "false") {
        Write-Host "??  Resource group não existe. Criando..." -ForegroundColor Yellow
        az group create --name "fcg-games-rg" --location "East US"
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Resource group criado" -ForegroundColor Green
        } else {
            Write-Host "? Erro ao criar resource group" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "? Resource group existe" -ForegroundColor Green
    }
} catch {
    Write-Host "? Erro ao verificar resource group" -ForegroundColor Red
    exit 1
}

# 3. Verificar se o namespace existe
Write-Host "`n3. Verificando Service Bus namespace..." -ForegroundColor Yellow
try {
    $namespaceExists = az servicebus namespace show --resource-group "fcg-games-rg" --name "fcg-games-servicebus" --query "name" -o tsv 2>$null
    if ([string]::IsNullOrEmpty($namespaceExists)) {
        Write-Host "??  Namespace não existe. Criando..." -ForegroundColor Yellow
        az servicebus namespace create --resource-group "fcg-games-rg" --name "fcg-games-servicebus" --location "East US" --sku Standard
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Namespace criado. Aguardando ativação (60 segundos)..." -ForegroundColor Green
            Start-Sleep -Seconds 60
        } else {
            Write-Host "? Erro ao criar namespace. Tentando nome alternativo..." -ForegroundColor Red
            
            # Tentar com nome único
            $uniqueName = "fcg-games-sb-" + (Get-Date).ToString("yyyyMMddHHmm")
            Write-Host "   Tentando com nome: $uniqueName" -ForegroundColor Yellow
            az servicebus namespace create --resource-group "fcg-games-rg" --name $uniqueName --location "East US" --sku Standard
            if ($LASTEXITCODE -eq 0) {
                Write-Host "? Namespace criado com nome único: $uniqueName" -ForegroundColor Green
                Write-Host "??  IMPORTANTE: Atualize a connection string para usar o namespace: $uniqueName" -ForegroundColor Yellow
                $global:namespaceName = $uniqueName
                Start-Sleep -Seconds 60
            } else {
                Write-Host "? Erro ao criar namespace com nome único" -ForegroundColor Red
                exit 1
            }
        }
    } else {
        Write-Host "? Namespace existe: $namespaceExists" -ForegroundColor Green
        $global:namespaceName = $namespaceExists
    }
} catch {
    Write-Host "? Erro ao verificar namespace" -ForegroundColor Red
    exit 1
}

# Usar o namespace correto
if (-not $global:namespaceName) {
    $global:namespaceName = "fcg-games-servicebus"
}

# 4. Verificar se a fila existe
Write-Host "`n4. Verificando fila..." -ForegroundColor Yellow
try {
    $queueExists = az servicebus queue show --resource-group "fcg-games-rg" --namespace-name $global:namespaceName --name "sale-processing-queue" --query "name" -o tsv 2>$null
    if ([string]::IsNullOrEmpty($queueExists)) {
        Write-Host "??  Fila não existe. Criando..." -ForegroundColor Yellow
        az servicebus queue create --resource-group "fcg-games-rg" --namespace-name $global:namespaceName --name "sale-processing-queue" --max-size 1024
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Fila criada" -ForegroundColor Green
        } else {
            Write-Host "? Erro ao criar fila" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "? Fila existe: $queueExists" -ForegroundColor Green
    }
} catch {
    Write-Host "? Erro ao verificar fila" -ForegroundColor Red
    exit 1
}

# 5. Obter connection string
Write-Host "`n5. Obtendo connection string..." -ForegroundColor Yellow
try {
    $connectionString = az servicebus namespace authorization-rule keys list --resource-group "fcg-games-rg" --namespace-name $global:namespaceName --name "RootManageSharedAccessKey" --query "primaryConnectionString" -o tsv 2>$null

    if ([string]::IsNullOrEmpty($connectionString)) {
        Write-Host "? Erro ao obter connection string" -ForegroundColor Red
        exit 1
    }

    # Mascarar a chave para exibição
    $maskedConnectionString = $connectionString -replace "SharedAccessKey=[^;]+", "SharedAccessKey=****"
    Write-Host "? Connection string obtida: $maskedConnectionString" -ForegroundColor Green
} catch {
    Write-Host "? Erro ao obter connection string" -ForegroundColor Red
    exit 1
}

# 6. Configurar arquivo .env
Write-Host "`n6. Configurando arquivo .env..." -ForegroundColor Yellow
try {
    if (Test-Path ".env") {
        # Ler conteúdo atual
        $envContent = Get-Content ".env" -Raw
        
        # Substituir a linha da connection string
        $newContent = $envContent -replace "SERVICEBUS_CONNECTION_STRING=.*", "SERVICEBUS_CONNECTION_STRING=$connectionString"
        
        # Salvar arquivo
        Set-Content ".env" -Value $newContent -NoNewline
        
        Write-Host "? Arquivo .env atualizado" -ForegroundColor Green
    } else {
        Write-Host "? Arquivo .env não encontrado!" -ForegroundColor Red
        Write-Host "   Criando arquivo .env..." -ForegroundColor Yellow
        
        $envTemplate = @"
# Service Bus Configuration
SERVICEBUS_CONNECTION_STRING=$connectionString

# Database Configuration
SA_PASSWORD=StrongPassword123!
DATABASE_NAME=fcg-games-microservice

# Application Configuration
ASPNETCORE_ENVIRONMENT=Development
JWT_SECRET_KEY=MyGamesMicroserviceSecretKey123456789
JWT_ISSUER=FCG.Games.Microservice
JWT_AUDIENCE=FCG.WebApp

# Ports Configuration
GAMES_SERVICE_HTTP_PORT=5001
GAMES_SERVICE_HTTPS_PORT=5002
SQL_SERVER_PORT=1433

# Network Configuration
NETWORK_NAME=fcg-microservices-network
"@
        Set-Content ".env" -Value $envTemplate
        Write-Host "? Arquivo .env criado" -ForegroundColor Green
    }
} catch {
    Write-Host "? Erro ao configurar arquivo .env" -ForegroundColor Red
    exit 1
}

# 7. Configurar User Secrets para desenvolvimento local
Write-Host "`n7. Configurando User Secrets..." -ForegroundColor Yellow
$projectPath = "src\FCG.Games.Presentation"

if (Test-Path $projectPath) {
    try {
        Push-Location $projectPath
        
        dotnet user-secrets set "ServiceBus:ConnectionString" "$connectionString" 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? User secret configurado" -ForegroundColor Green
        } else {
            Write-Host "??  Erro ao configurar user secret (não crítico)" -ForegroundColor Yellow
        }
        
        Pop-Location
    } catch {
        Write-Host "??  Erro ao configurar user secret (não crítico)" -ForegroundColor Yellow
        Pop-Location
    }
} else {
    Write-Host "??  Diretório do projeto não encontrado: $projectPath" -ForegroundColor Yellow
}

Write-Host "`n=== Configuração Concluída com Sucesso ===" -ForegroundColor Green
Write-Host "?? Namespace: $global:namespaceName" -ForegroundColor Cyan
Write-Host "?? Fila: sale-processing-queue" -ForegroundColor Cyan
Write-Host "?? Connection string configurada no .env" -ForegroundColor Cyan

Write-Host "`n?? Próximos passos:" -ForegroundColor Yellow
Write-Host "   1. Reinicie a aplicação: docker-compose restart" -ForegroundColor Cyan
Write-Host "   2. Teste a conexão: GET /api/servicebusmonitor/test-connection" -ForegroundColor Cyan
Write-Host "   3. Execute: .\test-servicebus.ps1" -ForegroundColor Cyan

if ($global:namespaceName -ne "fcg-games-servicebus") {
    Write-Host "`n??  ATENÇÃO: Namespace criado com nome diferente!" -ForegroundColor Yellow
    Write-Host "   Seu namespace é: $global:namespaceName" -ForegroundColor Red
    Write-Host "   A connection string já foi atualizada automaticamente" -ForegroundColor Cyan
}