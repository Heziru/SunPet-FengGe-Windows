# SunPet Windows（本地移植版）

基于 [Ganzhe2028/SunPet](https://github.com/Ganzhe2028/SunPet) 的 Windows 社区移植，非原作者官方 Windows 发行版。
原项目版本：`a01d22cca22a5362d26bd3c7cecdb88949245f85`。

双击 `SunPet.exe`：头像会显示在屏幕右下角，透明背景、窗口置顶；任务栏图标可帮助找到桌宠。

- 点击头像：随机播放一段原声，28 段一轮内不重复，连续点击不会叠加播放。
- 按住头像拖动：移动悬浮窗，位置会记住。拖动不会触发播放。
- 右键头像：播放、移回右下角、退出。
- 头像获得焦点时也可按空格或 Enter 播放，Alt+F4 退出。

同时运行本次 Windows 版峰哥桌宠时，点击峰哥一次会默认连续播放 **4 轮，每轮 4 句，共 16 句**：
每轮都是：峰哥随机一句 → SunPet 随机一句 → 峰哥随机一句 → SunPet 随机一句。
第 4 轮最后一句播完后自动停止；字幕只显示当前台词。
双方都使用各自的随机队列，一轮内不重复，SunPet 的两次接话从全部 28 段原声中随机选择。
接话对象正在播放时会跳过本次接话并中断该链，不打断、不排队；单独点击 SunPet 不会开始接话链。
联动使用当前 Windows 会话内的命名事件，双方同时打开即可，无需配置或联网。任一方退出后接话链停止。

## 安装与运行

要求：Windows 10/11、.NET Framework 4.8、可用的音频输出设备。
不需要管理员权限，不修改执行策略、系统安全设置或开机启动项，不联网。
原 M4A 音频已离线解码为 16-bit PCM WAV，运行时使用 Windows 原生 SoundPlayer，不依赖 M4A 编解码器。

可直接运行本目录的 EXE，也可在 PowerShell 中运行 `& .\install.ps1 -Launch`。
安装目录为 `%LOCALAPPDATA%\Programs\SunPet-Windows`，桌面及开始菜单会生成 `SunPet Windows` 快捷方式。
如果本机策略禁止 PowerShell 脚本，直接使用 EXE；无需修改策略。

位置数据：`%LOCALAPPDATA%\SunPet.Windows\position.json`。
卸载：右键退出，删除安装目录、两个快捷方式及上述数据目录即可。

## 构建与验证

本地源码 `SunPet.cs` 使用 Windows 自带 .NET Framework 的 C# 编译器：`& .\build.ps1`。
无需 Visual Studio、.NET SDK、npm 或 Python。素材和 ICO 已随目录提供。

音频与轮播自检（检查所有 PCM 数据、时长、SoundPlayer 加载和轮播不重复，静音检查，退出码 0 表示通过）：

```powershell
$result = Join-Path $PWD 'self-test.json'
$p = Start-Process .\SunPet.exe -ArgumentList ('--self-test "' + $result + '"') -PassThru -Wait
$p.ExitCode
Get-Content -LiteralPath $result -Encoding UTF8
```

`SunPet.exe --diagnostics` 可将本次会话的播放事件、播放完成和拖动坐标写入
`%LOCALAPPDATA%\SunPet.Windows\diagnostics.jsonl`，只保存在本机。正常双击运行不记录此文件。

## 来源与许可

原项目源代码使用 MIT 许可，保留于 `LICENSE`。本次 Windows 移植代码同样使用 MIT 许可。
头像、28 条 M4A 原音频及 clips.json 均来自上述仓库，未经修改；播放使用的 WAV 由原 M4A 解码，不做变声或语音合成。
Windows 图标由原头像缩放转换而来。WAV 已随目录提供，重新生成时可使用 FFmpeg 运行 `& .\prepare-audio.ps1 -FFmpegPath 'ffmpeg.exe'`；FFmpeg 仅用于开发时转换，成品无需安装。
音频原始来源：[B 站访谈 BV1fcHD6cEw3](https://www.bilibili.com/video/BV1fcHD6cEw3/)，时间码保留在 clips.json。
素材版权归原权利人所有，不属于 MIT 授权范围；遵循原项目学习交流用途说明。
