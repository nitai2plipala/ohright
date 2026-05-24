# OhRight

Windows 文件关联与右键菜单管理工具

## 简介

OhRight 是一款强大的 Windows 文件关联和右键菜单管理工具，支持：
- 查看和修改文件扩展名关联
- 管理右键上下文菜单
- 管理"打开方式"列表

## 技术架构

```
┌─────────────────────────────────────────┐
│              Flutter GUI (可选)          │
│           gRPC Client / UI              │
├─────────────────────────────────────────┤
│          OhRight.Cli (命令行工具)        │
│           直接调用 Core 层               │
├─────────────────────────────────────────┤
│        OhRight.Grpc.Server              │
│       gRPC 服务 / HTTP 端点             │
├─────────────────────────────────────────┤
│           OhRight.Core                  │
│     注册表操作 / 哈希算法 / 业务逻辑     │
└─────────────────────────────────────────┘
```

## 功能特性

### 文件关联管理
- 获取文件扩展名关联信息
- 设置文件关联（支持 UserChoice 哈希保护绕过）
- 支持用户级和系统级关联

### 右键菜单管理
- 列出文件/文件夹/通用右键菜单项
- 添加右键菜单项
- 移除右键菜单项
- 支持 shellex 类型（COM 组件）

### 打开方式管理
- 列出文件的"打开方式"列表
- 添加打开方式条目
- 移除打开方式条目
- 支持 HKCU（用户级）和 HKLM（系统级）

## 快速开始

### 环境要求

- .NET 8.0 SDK
- Windows 10/11
- 管理员权限（某些操作需要）

### 编译项目

```bash
# 克隆项目
git clone https://github.com/your-username/ohright.git
cd ohright

# 编译解决方案
dotnet build ohright.sln
```

### 使用 CLI 工具

```bash
# 显示帮助
dotnet run --project src/OhRight.Cli -- --help

# 获取文件关联信息
dotnet run --project src/OhRight.Cli -- get .txt

# 以 JSON 格式输出
dotnet run --project src/OhRight.Cli -- get .txt --json

# 列出右键菜单项
dotnet run --project src/OhRight.Cli -- list file

# 添加右键菜单项
dotnet run --project src/OhRight.Cli -- add "用记事本打开" "notepad.exe" file --extension .txt
```

### 启动 gRPC 服务器

```bash
dotnet run --project src/OhRight.Grpc.Server
```

服务器默认监听：
- HTTP: http://localhost:5000
- gRPC: http://localhost:5001
- 健康检查: http://localhost:5000/health
- Swagger: http://localhost:5000/swagger

## CLI 命令参考

### get - 获取文件关联

```bash
ohright get <扩展名> [选项]

选项:
  --level, -l    权限级别: User 或 Administrator
  --json, -j     以 JSON 格式输出

示例:
  ohright get .txt
  ohright get .pdf --level Administrator
```

### set - 设置文件关联

```bash
ohright set <扩展名> <ProgId> [选项]

选项:
  --force, -f    强制设置（绕过 UserChoice 保护）
  --json, -j     以 JSON 格式输出

示例:
  ohright set .txt txtfile --force
```

### list - 列出右键菜单项

```bash
ohright list <类型> [选项]

类型: file, directory, all

选项:
  --extension, -e    指定扩展名
  --json, -j         以 JSON 格式输出

示例:
  ohright list file
  ohright list directory
  ohright list file --extension .txt
```

### add - 添加右键菜单项

```bash
ohright add <名称> <命令> <类型> [选项]

类型: file, directory, all

选项:
  --extension, -e    文件扩展名
  --icon, -i         图标路径
  --position, -p     菜单位置
  --json, -j         以 JSON 格式输出

示例:
  ohright add "打开记事本" "notepad.exe" file --extension .txt
  ohright add "用 VSCode 打开" "code.exe" directory --position 1
```

### remove - 移除右键菜单项

```bash
ohright remove <ID> <类型> [选项]

类型: file, directory, all

选项:
  --extension, -e    文件扩展名
  --json, -j         以 JSON 格式输出

示例:
  ohright remove {GUID} file
```

## 项目结构

```
ohright/
├── src/
│   ├── OhRight.Core/              # 核心业务逻辑层
│   │   ├── Configuration/         # 配置类
│   │   ├── Constants/             # 常量定义
│   │   ├── Enums/                 # 枚举类型
│   │   ├── Exceptions/            # 自定义异常
│   │   ├── Interfaces/            # 服务接口
│   │   ├── Logging/               # 日志服务
│   │   ├── Models/                # 数据模型
│   │   └── Services/              # 服务实现
│   ├── OhRight.Grpc.Server/       # gRPC 服务器
│   │   └── Services/              # gRPC 服务实现
│   └── OhRight.Cli/               # 命令行工具
│       ├── Formatters/            # 输出格式化
│       └── Services/              # CLI 服务层
├── tests/
│   ├── OhRight.Core.Tests/        # Core 层单元测试
│   ├── OhRight.Grpc.Server.Tests/ # gRPC 层测试
│   └── OhRight.Cli.Tests/         # CLI 工具测试
├── proto/                         # Protobuf 定义文件
│   └── registry.proto
├── ohright.sln                    # 解决方案文件
└── README.md
```

## 开发指南

### 运行测试

```bash
# 运行所有测试
dotnet test ohright.sln

# 运行特定项目测试
dotnet test tests/OhRight.Core.Tests
```

### 添加新功能

1. 在 `OhRight.Core` 中定义接口和模型
2. 实现业务逻辑
3. 在 `OhRight.Grpc.Server` 中添加 gRPC 端点（如需要）
4. 在 `OhRight.Cli` 中添加 CLI 命令（如需要）
5. 编写单元测试

### 代码规范

- 使用 C# 8.0+ 特性
- 遵循 SOLID 原则
- 所有公共 API 需要 XML 文档注释
- 异常处理在 Core 层统一处理

## 已知限制

- 某些注册表操作需要管理员权限
- Windows UserChoice 保护需要特殊的哈希算法绕过
- 不同 Windows 版本的注册表结构可能有差异

## 路线图

- [x] 基础框架搭建
- [x] Core 层核心实现
- [x] gRPC 服务层
- [x] CLI 工具
- [x] 测试和质量保证
- [ ] Flutter GUI（可选）
- [ ] 文档和部署

## 许可证

MIT License

## 贡献

欢迎提交 Issue 和 Pull Request！

## 致谢

- [SetUserFTA](https://kolbi.cz/blog/2017/10/25/setuserfta-userchoice-hash-defeated-set-file-type-associations-per-user/) - UserChoice 哈希算法参考
- [Microsoft Win32 Registry](https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.registry) - 注册表操作 API
- [gRPC .NET](https://grpc.io/docs/languages/csharp/) - gRPC 框架
