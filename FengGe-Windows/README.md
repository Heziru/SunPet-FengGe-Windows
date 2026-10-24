# 峰哥 · 好事桌宠 Windows（本地移植版）

基于 [Ganzhe2028/FengGe-Desktop-Pet](https://github.com/Ganzhe2028/FengGe-Desktop-Pet) 的 Windows 社区移植，非原作者官方 Windows 发行版。
原项目版本：`aad1a5253aa966e7e0be570605f1956a6b678c21`。
Windows 窗口和播放实现复用了本次 SunPet Windows 移植代码。

双击 `FengGe.exe` 即可运行。默认显示在屏幕右下角偏左，方便与 SunPet 并排使用。

- 点击人物：随机播放一段峰哥原声，11 条一轮内不重复。
- 正在播放时再次点击不会叠加声音，与本次 SunPet Windows 版行为一致。
- 按住人物拖动：移动置顶透明悬浮窗，拖动不会播放，位置会记住。
- 右键人物：播放、移回默认位置、退出。
- 人物获得焦点时也可按空格或 Enter 播放，Alt+F4 退出。

同时运行本次 Windows 版 SunPet 时，点击峰哥一次会默认连续播放 **4 轮，每轮 4 句，共 16 句**：
每轮都是：峰哥随机一句 → SunPet 随机一句 → 峰哥随机一句 → SunPet 随机一句。
第 4 轮最后一句播完后自动停止；字幕只显示当前台词。
峰哥从全部 11 段原声中随机选择，SunPet 从全部 28 段原声中随机选择，各自一轮内不重复。
只在每段音频播放结束后通知下一位；对方忙碌时跳过并中断该链，不打断、不排队。
单独点击 SunPet 不会启动链，单独运行任一桌宠仍可正常播放。
联动在本机 Windows 会话内完成，无需配置或联网；任一方退出后接话链停止。

## 安装

要求 Windows 10/11 和 .NET Framework 4.8。使用原生 SoundPlayer 播放随包提供的 WAV，无需额外编解码器或开发环境。

直接运行 EXE 即可。也可在 PowerShell 中运行 `& .\install.ps1 -Launch`，安装到
`%LOCALAPPDATA%\Programs\FengGe-Windows`，并创建桌面、开始菜单的 `FengGe Windows` 快捷方式。
不需要管理员权限，不修改系统安全设置、执行策略或开机启动项，不联网。

与 SunPet 的应用目录、进程互斥锁、位置设置完全独立。位置保存在
`%LOCALAPPDATA%\FengGe.Windows\position.json`。

卸载：右键退出，再删除上述应用目录、数据目录和两个快捷方式。

## 源码与自检

`FengGe.cs` 为完整 Windows 源码；运行 `& .\build.ps1` 用 Windows 自带的 .NET Framework C# 编译器构建。

```powershell
$result = Join-Path $PWD 'self-test.json'
$p = Start-Process .\FengGe.exe -ArgumentList ('--self-test "' + $result + '"') -Wait -PassThru
$p.ExitCode
Get-Content -LiteralPath $result -Encoding UTF8
```

退出码 0 表示全部 11 条 PCM 音频数据、时长、原生播放器加载、随机轮播检查通过。
`FengGe.exe --diagnostics` 可将本次播放完成、拖动坐标等事件写入
`%LOCALAPPDATA%\FengGe.Windows\diagnostics.jsonl`；正常双击不会写该诊断文件。

## 来源与许可

人物 PNG、11 条 WAV 和 clips.json 均原样来自指定仓库；素材未做变声或语音合成。
音频剪辑来源记录保留于 `音频切分记录.md`。
原项目源代码和文档的 MIT 许可保留于 `LICENSE`，原 SunPet 的 MIT 许可保留于 `LICENSE-SunPet`；本次 Windows 移植代码也采用 MIT 许可。
人物图片和声音的版权归其原权利人所有，不属于这些代码许可范围。
