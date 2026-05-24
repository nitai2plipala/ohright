# OhRight 使用指南

## 目录

- [简介](#简介)
- [系统要求](#系统要求)
- [安装](#安装)
- [快速开始](#快速开始)
- [CLI 命令详解](#cli-命令详解)
- [gRPC 服务器](#grpc-服务器)
- [常见问题](#常见问题)
- [故障排除](#故障排除)

---

## 简介

OhRight 是一款 Windows 文件关联和右键菜单管理工具，可以帮助你：

- 查看和修改文件扩展名的默认打开程序
- 管理文件和文件夹的右键菜单
- 管理"打开方式"列表

---

## 系统要求

- **操作系统**: Windows 10 / Windows 11
- **运行时**: .NET 8.0 Runtime（如果使用 CLI 工具）
- **权限**: 普通用户可查看，管理员权限可修改

---

## 安装

### 方式一：从源码编译

```bash
# 1. 克隆仓库
git clone https://github.com/your-username/ohright.git
cd ohright

# 2. 编译
dotnet build ohright.sln -c Release

# 3. 可选：发布为单文件
dotnet publish src/OhRight.Cli -c Release -r win-x64 --self-contained
```

### 方式二：使用预编译版本

1. 下载最新 Release
2. 解压到任意目录
3. 将目录添加到 PATH 环境变量（可选）

---

## 快速开始

### 查看文件关联

```bash
# 查看 .txt 文件的关联程序
ohright get .txt

# 输出示例：
# 文件关联信息:
#   扩展名: .txt
#   ProgId: txtfile
#   命令:
#   描述: Text Document
```

### 以 JSON 格式输出

```bash
ohright get .txt --json

# 输出示例：
# {
#   "extension": ".txt",
#   "progId": "txtfile",
#   "command": "",
#   "description": "Text Document"
# }
```

### 列出右键菜单

```bash
# 列出文件右键菜单
ohright list file

# 列出文件夹右键菜单
ohright list directory

# 列出 .txt 文件的特定菜单
ohright list file --extension .txt
```

---

## CLI 命令详解

### get - 获取文件关联

**语法:**
```bash
ohright get <扩展名> [选项]
```

**选项:**
| 选项 | 缩写 | 说明 |
|------|------|------|
| `--level` | `-l` | 权限级别: User 或 Administrator |
| `--json` | `-j` | 以 JSON 格式输出 |

**示例:**
```bash
# 获取用户级关联
ohright get .txt

# 获取系统级关联
ohright get .txt --level Administrator

# JSON 输出
ohright get .pdf --json
```

---

### set - 设置文件关联

**语法:**
```bash
ohright set <扩展名> <ProgId> [选项]
```

**选项:**
| 选项 | 缩写 | 说明 |
|------|------|------|
| `--force` | `-f` | 强制设置（绕过 UserChoice 保护）|
| `--json` | `-j` | 以 JSON 格式输出 |

**示例:**
```bash
# 设置 .txt 关联到记事本
ohright set .txt txtfile --force

# 设置 .pdf 关联到 Adobe Reader
ohright set .pdf AcroExch.Document --force
```

**注意:**
- 需要管理员权限
- `--force` 选项会绕过 Windows 10/11 的 UserChoice 保护

---

### list - 列出右键菜单项

**语法:**
```bash
ohright list <类型> [选项]
```

**类型:**
| 类型 | 说明 |
|------|------|
| `file` | 文件右键菜单 |
| `directory` | 文件夹右键菜单 |
| `all` | 通用右键菜单 |

**选项:**
| 选项 | 缩写 | 说明 |
|------|------|------|
| `--extension` | `-e` | 文件扩展名 |
| `--json` | `-j` | 以 JSON 格式输出 |

**示例:**
```bash
# 列出文件右键菜单
ohright list file

# 列出 .txt 文件的菜单
ohright list file --extension .txt

# 列出文件夹菜单
ohright list directory
```

**输出示例:**
```
右键菜单项 (15 个):
------------------------------------------------------------
ID           名称                 启用   位置
------------------------------------------------------------
open         打开                 ✓      0
edit         编辑                 ✓      1
print        打印                 ✓      2
             命令: notepad.exe "%1"
```

---

### add - 添加右键菜单项

**语法:**
```bash
ohright add <名称> <命令> <类型> [选项]
```

**选项:**
| 选项 | 缩写 | 说明 |
|------|------|------|
| `--extension` | `-e` | 文件扩展名 |
| `--icon` | `-i` | 图标路径 |
| `--position` | `-p` | 菜单位置 |
| `--json` | `-j` | 以 JSON 格式输出 |

**示例:**
```bash
# 添加"用记事本打开"菜单
ohright add "用记事本打开" "notepad.exe \"%1\"" file --extension .txt

# 添加到文件夹菜单
ohright add "用 VSCode 打开" "code.exe \"%1\"" directory

# 指定位置
ohright add "复制路径" "clip.exe" file --position 10
```

**命令中的特殊字符:**
- `%1` - 被选中的文件路径
- `%L` - 长文件名
- `%S` - 短文件名
- `%*` - 所有选中的文件

---

### remove - 移除右键菜单项

**语法:**
```bash
ohright remove <ID> <类型> [选项]
```

**选项:**
| 选项 | 缩写 | 说明 |
|------|------|------|
| `--extension` | `-e` | 文件扩展名 |
| `--json` | `-j` | 以 JSON 格式输出 |

**示例:**
```bash
# 先查看菜单项 ID
ohright list file

# 移除指定菜单项
ohright remove "my-custom-menu" file

# 移除特定扩展名的菜单
ohright remove "open-with-notepad" file --extension .txt
```

---

## gRPC 服务器

### 启动服务器

```bash
dotnet run --project src/OhRight.Grpc.Server
```

### 服务器端点

| 端点 | 说明 |
|------|------|
| `http://localhost:5000` | HTTP (Swagger UI) |
| `http://localhost:5001` | gRPC |
| `http://localhost:5000/health` | 健康检查 |
| `http://localhost:5000/status` | 服务器状态 |

### 使用 grpcurl 测试

```bash
# 安装 grpcurl
# Windows: scoop install grpcurl
# macOS: brew install grpcurl

# 查看服务列表
grpcurl -plaintext localhost:5001 list

# 获取文件关联
grpcurl -plaintext localhost:5001 registry.RegistryService/GetFileAssociation \
  -d '{"extension": ".txt"}'
```

---

## 常见问题

### Q: 为什么设置文件关联失败？

**A:** 可能的原因：
1. 没有管理员权限 - 使用 `--force` 或以管理员身份运行
2. Windows UserChoice 保护 - 使用 `--force` 选项
3. ProgId 不存在 - 确认 ProgId 正确

### Q: 如何查看可用的 ProgId？

**A:** 
```bash
# 查看当前关联
ohright get .txt

# 或在注册表中查看
reg query "HKCR\.txt"
```

### Q: 右键菜单添加后不显示？

**A:** 
1. 检查菜单是否被禁用（名称前有 `-` 前缀）
2. 重启资源管理器：`taskkill /f /im explorer.exe && start explorer`
3. 检查是否有权限问题

### Q: gRPC 服务器无法启动？

**A:** 
1. 检查端口是否被占用
2. 检查防火墙设置
3. 查看日志输出

---

## 故障排除

### 权限问题

如果遇到权限错误，尝试以管理员身份运行：

```bash
# 右键点击命令提示符，选择"以管理员身份运行"
# 然后执行命令
ohright set .txt txtfile --force
```

### 端口被占用

修改 `appsettings.json` 中的端口号：

```json
{
  "Server": {
    "HttpPort": 5010,
    "GrpcPort": 5011
  }
}
```

### 查看详细日志

设置环境变量启用详细日志：

```bash
set ASPNETCORE_ENVIRONMENT=Development
dotnet run --project src/OhRight.Grpc.Server
```

---

## 高级用法

### 批量操作

```bash
# 批量设置多个扩展名
for %e in (.txt .log .csv) do ohright set %e txtfile --force
```

### 脚本集成

```powershell
# PowerShell 脚本示例
$extensions = @(".txt", ".log", ".csv")
foreach ($ext in $extensions) {
    ohright set $ext txtfile --force
}
```

### 导出配置

```bash
# 导出为 JSON
ohright get .txt --json > config.json
ohright list file --json > menu.json
```

---

## 支持与反馈

- **GitHub Issues**: https://github.com/your-username/ohright/issues
- **文档**: https://github.com/your-username/ohright/tree/main/docs

---

## 更新日志

### v1.0.0 (2026-05-24)
- 初始版本发布
- 支持文件关联管理
- 支持右键菜单管理
- 支持打开方式管理
- CLI 工具和 gRPC 服务器
