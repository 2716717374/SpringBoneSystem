# Spring Bone System

一个轻量级的Unity弹簧骨骼系统，支持Humanoid角色。

## ✨ 特性

- 🎯 自动识别Humanoid骨骼，只给附加物体添加弹簧效果
- 🎮 支持运行时动态调整参数
- 🛠️ 提供编辑器工具，一键自动绑定
- 📦 轻量级，无额外依赖
- 🔧 支持碰撞检测

## 📦 安装

### 通过 Package Manager (推荐)

1. 打开 Unity Package Manager
2. 点击 `+` → `Add package from git URL`
3. 输入：`https://github.com/yourusername/SpringBoneSystem.git`

### 手动安装

1. 下载最新 Release
2. 解压到 `Assets/Plugins/SpringBoneSystem/`

## 🚀 快速开始

### 1. 打开工具

1. Unity菜单 → Spring System → Auto Setup Bones


### 2. 选择角色
将您的角色拖入 "Character" 字段

### 3. 自动查找骨骼根节点
点击 "Auto Find Skeleton Root"

### 4. 自动设置
点击 "Auto Setup Spring Bones"

## ⚙️ 参数说明

| 参数 | 说明 | 推荐值 |
|------|------|--------|
| Stiffness | 弹性系数 | 0.01 - 0.1 |
| Drag | 阻尼系数 | 0.1 - 0.9 |
| Radius | 碰撞半径 | 0.03 - 0.1 |


## 🤝 贡献

欢迎提交 Issue 和 Pull Request！
