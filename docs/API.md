# OhRight API 文档

## 概述

OhRight 提供两种 API 接口：
1. **gRPC API** - 用于程序化调用
2. **CLI 命令** - 用于命令行操作

---

## gRPC API

### 服务地址

```
gRPC: http://localhost:5001 (h2c 明文 HTTP/2)
HTTP: http://localhost:5000
```

> **注意**: 本机开发场景使用 HTTP/2 明文 (h2c)，无需 TLS 证书。

### Proto 定义

文件位置: `proto/registry.proto`

```protobuf
service RegistryService {
  rpc GetFileAssociation (FileAssociationRequest) returns (FileAssociationResponse);
  rpc SetFileAssociation (SetFileAssociationRequest) returns (SetFileAssociationResponse);
  rpc GetContextMenuItems (ContextMenuRequest) returns (ContextMenuResponse);
  rpc AddContextMenuItem (AddContextMenuRequest) returns (AddContextMenuResponse);
  rpc RemoveContextMenuItem (RemoveContextMenuRequest) returns (RemoveContextMenuResponse);
  rpc GetOpenWithList (OpenWithRequest) returns (OpenWithResponse);
  rpc AddOpenWithEntry (AddOpenWithRequest) returns (AddOpenWithResponse);
  rpc RemoveOpenWithEntry (RemoveOpenWithRequest) returns (RemoveOpenWithResponse);
  rpc HealthCheck (HealthCheckRequest) returns (HealthCheckResponse);
}
```

---

### 1. GetFileAssociation

获取文件扩展名的关联信息。

**请求:**
```protobuf
message FileAssociationRequest {
  string extension = 1;  // 文件扩展名，例如 ".txt"
}
```

**响应:**
```protobuf
message FileAssociationResponse {
  FileAssociation association = 1;
  ErrorCode error_code = 2;
  string error_message = 3;
}

message FileAssociation {
  string extension = 1;
  string prog_id = 2;
  string command = 3;
  string description = 4;
}
```

**示例:**
```bash
# 使用 grpcurl 测试
grpcurl -plaintext localhost:5001 registry.RegistryService/GetFileAssociation \
  -d '{"extension": ".txt"}'
```

---

### 2. SetFileAssociation

设置文件扩展名的关联。

**请求:**
```protobuf
message SetFileAssociationRequest {
  FileAssociation association = 1;
  bool force = 2;  // 是否强制设置（绕过 UserChoice 保护）
}
```

**响应:**
```protobuf
message SetFileAssociationResponse {
  bool success = 1;
  ErrorCode error_code = 2;
  string error_message = 3;
}
```

**示例:**
```bash
grpcurl -plaintext localhost:5001 registry.RegistryService/SetFileAssociation \
  -d '{
    "association": {
      "extension": ".txt",
      "progId": "txtfile"
    },
    "force": true
  }'
```

---

### 3. GetContextMenuItems

获取右键菜单项列表。

**请求:**
```protobuf
message ContextMenuRequest {
  string target_type = 1;  // "file", "directory", "all"
  string extension = 2;    // 可选，特定后缀
}
```

**响应:**
```protobuf
message ContextMenuResponse {
  repeated ContextMenuItem items = 1;
  ErrorCode error_code = 2;
  string error_message = 3;
}

message ContextMenuItem {
  string id = 1;
  string name = 2;
  string command = 3;
  string icon = 4;
  bool enabled = 5;
  int32 position = 6;
  ContextMenuType type = 7;
}
```

**示例:**
```bash
grpcurl -plaintext localhost:5001 registry.RegistryService/GetContextMenuItems \
  -d '{"target_type": "file", "extension": ".txt"}'
```

---

### 4. AddContextMenuItem

添加右键菜单项。

**请求:**
```protobuf
message AddContextMenuRequest {
  ContextMenuItem item = 1;
  string target_type = 2;
  string extension = 3;
}
```

**响应:**
```protobuf
message AddContextMenuResponse {
  bool success = 1;
  ErrorCode error_code = 2;
  string error_message = 3;
}
```

**示例:**
```bash
grpcurl -plaintext localhost:5001 registry.RegistryService/AddContextMenuItem \
  -d '{
    "item": {
      "name": "用记事本打开",
      "command": "notepad.exe \"%1\"",
      "enabled": true
    },
    "target_type": "file",
    "extension": ".txt"
  }'
```

---

### 5. RemoveContextMenuItem

移除右键菜单项。

**请求:**
```protobuf
message RemoveContextMenuRequest {
  string item_id = 1;
  string target_type = 2;
  string extension = 3;
}
```

**响应:**
```protobuf
message RemoveContextMenuResponse {
  bool success = 1;
  ErrorCode error_code = 2;
  string error_message = 3;
}
```

---

### 6. GetOpenWithList

获取文件的"打开方式"列表。

**请求:**
```protobuf
message OpenWithRequest {
  string extension = 1;
}
```

**响应:**
```protobuf
message OpenWithResponse {
  repeated OpenWithEntry entries = 1;
  ErrorCode error_code = 2;
  string error_message = 3;
}

message OpenWithEntry {
  string prog_id = 1;
  string name = 2;
  string command = 3;
  string icon = 4;
  bool is_user = 5;  // 用户级还是系统级
}
```

---

### 7. AddOpenWithEntry

添加打开方式条目。

**请求:**
```protobuf
message AddOpenWithRequest {
  OpenWithEntry entry = 1;
  string extension = 2;
}
```

---

### 8. RemoveOpenWithEntry

移除打开方式条目。

**请求:**
```protobuf
message RemoveOpenWithRequest {
  string prog_id = 1;
  string extension = 2;
}
```

---

### 9. HealthCheck

健康检查端点。

**请求:**
```protobuf
message HealthCheckRequest {}
```

**响应:**
```protobuf
message HealthCheckResponse {
  bool healthy = 1;
  string version = 2;
  string status = 3;
}
```

---

## 错误码

```protobuf
enum ErrorCode {
  SUCCESS = 0;
  UNKNOWN_ERROR = 1;
  INVALID_ARGUMENT = 2;
  NOT_FOUND = 3;
  ALREADY_EXISTS = 4;
  PERMISSION_DENIED = 5;
  INTERNAL_ERROR = 6;
  NOT_SUPPORTED = 7;
  HASH_VALIDATION_FAILED = 8;
}
```

| 错误码 | 说明 |
|--------|------|
| SUCCESS | 成功 |
| UNKNOWN_ERROR | 未知错误 |
| INVALID_ARGUMENT | 参数无效 |
| NOT_FOUND | 未找到 |
| ALREADY_EXISTS | 已存在 |
| PERMISSION_DENIED | 权限不足 |
| INTERNAL_ERROR | 内部错误 |
| NOT_SUPPORTED | 不支持的操作 |
| HASH_VALIDATION_FAILED | UserChoice 哈希验证失败 |

---

## HTTP 端点

| 端点 | 方法 | 说明 |
|------|------|------|
| `/` | GET | 重定向到 Swagger UI |
| `/health` | GET | 健康检查 |
| `/status` | GET | 服务器状态 |
| `/swagger` | GET | Swagger API 文档 |

### /status 响应示例

```json
{
  "service": "OhRight gRPC Server",
  "version": "1.0.0",
  "status": "Running",
  "ports": {
    "http": 5000,
    "grpc": 5001
  },
  "startTime": "2026-05-24T11:00:00",
  "uptime": "01:30:00",
  "endpoints": {
    "grpc": "http://localhost:5001",
    "health": "/health",
    "status": "/status",
    "swagger": "/swagger"
  }
}
```

---

## C# 客户端示例

### 连接 gRPC 服务器

```csharp
using Grpc.Net.Client;
using OhRight.Grpc;

// 使用 h2c 明文 HTTP/2 连接（本地开发场景）
var channel = GrpcChannel.ForAddress("http://localhost:5001");
var client = new RegistryService.RegistryServiceClient(channel);
```

### 获取文件关联

```csharp
var response = await client.GetFileAssociationAsync(new FileAssociationRequest
{
    Extension = ".txt"
});

if (response.ErrorCode == ErrorCode.Success)
{
    Console.WriteLine($"ProgId: {response.Association.ProgId}");
}
```

### 设置文件关联

```csharp
var response = await client.SetFileAssociationAsync(new SetFileAssociationRequest
{
    Association = new FileAssociation
    {
        Extension = ".txt",
        ProgId = "txtfile"
    },
    Force = true
});
```

### 获取右键菜单项

```csharp
var response = await client.GetContextMenuItemsAsync(new ContextMenuRequest
{
    TargetType = "file",
    Extension = ".txt"
});

foreach (var item in response.Items)
{
    Console.WriteLine($"{item.Name}: {item.Command}");
}
```

---

## Python 客户端示例

```python
import grpc
import registry_pb2
import registry_pb2_grpc

# 连接服务器
channel = grpc.insecure_channel('localhost:5001')
stub = registry_pb2_grpc.RegistryServiceStub(channel)

# 获取文件关联
response = stub.GetFileAssociation(
    registry_pb2.FileAssociationRequest(extension=".txt")
)
print(f"ProgId: {response.association.prog_id}")
```

---

## 配置

### appsettings.json

```json
{
  "Server": {
    "HttpPort": 5000,
    "GrpcPort": 5001,
    "TimeoutSeconds": 30
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Grpc": "Information"
    }
  }
}
```

### 环境变量

```bash
# Windows
set ASPNETCORE_ENVIRONMENT=Development
set Server__HttpPort=5000
set Server__GrpcPort=5001

# Linux/macOS
export ASPNETCORE_ENVIRONMENT=Development
export Server__HttpPort=5000
export Server__GrpcPort=5001
```
