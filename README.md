# Play-SDK-CSharp

## 安装

从 Release 下载 SDK.zip，解压后将 Plugins 目录拖拽至 Unity 工程。如果项目中已有 Plugins 目录，则合并至项目中的 Plugins 目录。

## 文档

- [API 文档](https://leancloud.github.io/Play-SDK-CSharp/html/)

## 编译

下载源码后，使用 Visual Studio 打开根目录下的 Play-SDK-CSharp.sln，点击编译。

或使用命令行 `msbuild /p:Configuration=Release Play-SDK-CSharp.sln` 进行编译。

目录 `Play-SDK-CSharp/SDK-Net35/bin/Release/` 下即为生成的 dll。

## 项目的目录结构

```
├── SDK-Net35                       // SDK 工程
│   └── src                			// 源码目录
│   	├── Play.cs                // 最重要的接口类，提供操作接口和注册事件
│   	├── Room.cs                // 房间类
│   	├── Player.cs                // 玩家类
│   	├── Event.cs                // SDK 事件
│   	├── LobbyRoom.cs                // 大厅房间类
│   	├── Region.cs                // App 节点枚举
│   	├── RoomOptions.cs                // 创建房间选项
│   	├── SendEventOptions.cs                // 发送自定义事件选项
│   	└── ...
├── Test                      // 单元测试
│   ├── ChangeProperties.cs                 // 连接测试
│   ├── CreateRoomTest.cs                 // 创建房间测试
│   ├── CustomEventTest.cs                   // 自定义事件测试
│   ├── JoinRoomTest.cs                   // 加入房间测试
│   ├── LobbyTest.cs                   // 大厅测试
│   ├── LogTest.cs                   // 日志测试
│   └── MasterTest.cs                   // 主机切换测试
```
