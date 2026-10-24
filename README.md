# SunPet & 峰哥 Windows 桌宠

把「太对了」和「这是个好事」放到 Windows 桌面上。点击播放原声，拖动移动；两个一起运行时，还能轮流接话。

这是基于 [Ganzhe2028/SunPet](https://github.com/Ganzhe2028/SunPet) 和 [Ganzhe2028/FengGe-Desktop-Pet](https://github.com/Ganzhe2028/FengGe-Desktop-Pet) 制作的 Windows 社区移植版。

## 下载与运行

从 [Releases](https://github.com/Heziru/SunPet-FengGe-Windows/releases/latest) 下载：

- [SunPet-Windows.zip](https://github.com/Heziru/SunPet-FengGe-Windows/releases/latest/download/SunPet-Windows.zip)：孙宇晨，28 条原声。
- [FengGe-Windows.zip](https://github.com/Heziru/SunPet-FengGe-Windows/releases/latest/download/FengGe-Windows.zip)：峰哥，11 条原声。

解压后分别双击 `SunPet.exe` 和 `FengGe.exe`。需要保留整个解压目录，EXE 使用旁边的 `Assets` 文件夹。

要求 **Windows 10/11 x64、.NET Framework 4.8 和可用的音频输出设备**。已在 Windows 11 上验证。程序不联网，不需要管理员权限，不修改系统安全设置或开机启动项。

## 怎么玩

- 点击人物：随机播放一段原声，各自的音频池播完前不重复，连续点击不会叠加声音。
- 按住人物拖动：移动透明置顶窗口，重开后记住位置。
- 右键人物：播放、移回默认位置、退出。
- 人物获得焦点时，空格或 Enter 也能播放；Alt+F4 退出。

同时运行两个桌宠后，**点击峰哥一次，默认连续播放 4 轮，每轮 4 句，共 16 句**：

```text
峰哥随机一句 → SunPet 随机一句 → 峰哥随机一句 → SunPet 随机一句
```

四轮结束后自动停止。字幕只显示台词，不显示轮数。单独点击 SunPet 不会开始联动；接话对象正在播放时跳过并中断该链，不抢话、不排队。

## 可选：安装快捷方式

在解压目录中运行各自的 `install.ps1 -Launch`，会安装到当前用户的 `%LOCALAPPDATA%\Programs\SunPet-Windows` 和 `%LOCALAPPDATA%\Programs\FengGe-Windows`，并创建桌面、开始菜单快捷方式。

更新前先从右键菜单退出旧版本。也可以一直使用解压目录中的 EXE；脚本受本机策略限制时，直接运行 EXE 即可，无需修改策略。

卸载时退出程序，再删除应用目录、快捷方式，以及 `%LOCALAPPDATA%\SunPet.Windows`、`%LOCALAPPDATA%\FengGe.Windows` 中的位置记录。

## 从源码构建

在仓库根目录的 PowerShell 中运行：

```powershell
& .\build.ps1
```

构建直接调用 .NET Framework 自带的 C# 编译器，无需 Visual Studio、.NET SDK、npm 或 Python。输出为 `SunPet-Windows\SunPet.exe` 和 `FengGe-Windows\FengGe.exe`。

SunPet 的原 M4A 已离线解码为 PCM WAV，峰哥直接使用上游 WAV。成品使用 Windows 原生 `SoundPlayer`，无需额外 M4A 编解码器。

## 验证与打包

```powershell
& .\test.ps1
& .\package.ps1
```

`test.ps1` 检查 28 + 11 条 PCM 音频数据、时长、播放器加载和随机队列。打包产物在 `outputs`，包括两个 ZIP 和 SHA-256 清单。

需要复核实际联动时，用 `--diagnostics` 启动两个 EXE，点击峰哥一次，等待完整播放后运行：

```powershell
& .\tests\check-chain.ps1 `
  -SunLog "$env:LOCALAPPDATA\SunPet.Windows\diagnostics.jsonl" `
  -FengLog "$env:LOCALAPPDATA\FengGe.Windows\diagnostics.jsonl" `
  -Report "$env:TEMP\desktop-pets-chain-test.json"
```

该检查核对一次手动启动、16 句的顺序和完成事件、4 轮结束以及最后停止。诊断日志只保存在本机，默认启动不会记录。

首次发布前已完成一次点击、4 轮 16 句的实际联动验证，并在结束后继续观察 8 秒，确认没有自动重启。随后仅移除了字幕中的轮数提示，播放逻辑保持一致。

## 来源与许可

代码采用 [MIT 许可](LICENSE)，上游版权声明保留在各应用目录中。**头像和音频不属于 MIT 授权范围，版权归原权利人所有**；详情见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。

仓库保留素材的上游来源、音频时间码和剪辑记录。请在使用、修改或再分发时遵守相应权利要求。
