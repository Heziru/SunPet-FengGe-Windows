using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

// Windows community port of Ganzhe2028/FengGe-Desktop-Pet. See LICENSE and README.md.
public sealed class Clip
{
    public int id { get; set; }
    public string label { get; set; }
    public string file { get; set; }
    public double duration { get; set; }
}

public static class Program
{
    public static readonly string Home = AppDomain.CurrentDomain.BaseDirectory;
    public static readonly string Data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FengGe.Windows");
    public static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

    [STAThread]
    public static int Main(string[] args)
    {
        bool test = args.Length == 2 && args[0] == "--self-test";
        try
        {
            List<Clip> clips = Json.Deserialize<List<Clip>>(File.ReadAllText(Path.Combine(Home, "Assets", "clips.json"), Encoding.UTF8));
            if (clips == null || clips.Count == 0 || clips.Select(c => c.id).Distinct().Count() != clips.Count)
                throw new InvalidDataException("音频清单为空或编号重复。");
            foreach (Clip c in clips)
            {
                if (String.IsNullOrWhiteSpace(c.file) || Path.GetFileName(c.file) != c.file || !File.Exists(ClipPath(c)))
                    throw new InvalidDataException("音频文件不存在或路径无效：" + c.file);
            }
            LoadFace();
            if (test) return SelfTest.Run(clips, Path.GetFullPath(args[1]));
            Application app = new Application();
            bool first;
            using (Mutex instance = new Mutex(true, @"Local\FengGe.Windows", out first))
            {
                if (!first) return 0;
                Directory.CreateDirectory(Data);
                return app.Run(new PetWindow(clips, args.Contains("--diagnostics")));
            }
        }
        catch (Exception ex)
        {
            if (test) File.WriteAllText(args[1], Json.Serialize(new { pass = false, error = ex.ToString() }), Encoding.UTF8);
            else MessageBox.Show(ex.Message, "FengGe 无法启动", MessageBoxButton.OK, MessageBoxImage.Error);
            return 1;
        }
    }

    public static string ClipPath(Clip clip) { return Path.Combine(Home, "Assets", "Clips", Path.ChangeExtension(clip.file, ".wav")); }
    public static BitmapImage LoadFace()
    {
        BitmapImage image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(Path.Combine(Home, "Assets", "face.png"));
        image.EndInit();
        image.Freeze();
        return image;
    }
}

// The same shuffle bag is exercised by --self-test and used by the actual UI.
public sealed class ClipBag
{
    readonly Random random = new Random();
    readonly int count;
    readonly List<int> bag = new List<int>();
    int last = -1;
    public ClipBag(int count) { this.count = count; }
    public int Next()
    {
        if (bag.Count == 0)
        {
            for (int i = 0; i < count; i++) bag.Add(i);
            for (int i = count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1), temp = bag[i];
                bag[i] = bag[j]; bag[j] = temp;
            }
            if (count > 1 && bag[count - 1] == last)
            {
                int temp = bag[0]; bag[0] = bag[count - 1]; bag[count - 1] = temp;
            }
        }
        last = bag[bag.Count - 1]; bag.RemoveAt(bag.Count - 1);
        return last;
    }
}

public sealed class PetWindow : Window
{
    readonly List<Clip> clips;
    readonly ClipBag bag;
    SoundPlayer player;
    readonly Border caption;
    readonly TextBlock captionText;
    readonly Button face;
    readonly DispatcherTimer captionTimer = new DispatcherTimer();
    readonly bool diagnostics;
    const string BridgePrefix = @"Local\FengGe.SunPet.Chain.v3.Turn";
    const int TotalTurns = 16;
    readonly List<EventWaitHandle> bridgeEvents = new List<EventWaitHandle>();
    readonly List<RegisteredWaitHandle> bridgeWaits = new List<RegisteredWaitHandle>();
    int currentTurn;
    bool closing;
    readonly string settingsPath = Path.Combine(Program.Data, "position.json");
    Point mouseStart, windowStart;
    bool dragging, playing;
    int current = -1;

    public PetWindow(List<Clip> clips, bool diagnostics)
    {
        this.clips = clips; this.diagnostics = diagnostics; bag = new ClipBag(clips.Count);
        if (diagnostics) File.WriteAllText(Path.Combine(Program.Data, "diagnostics.jsonl"), "");
        Title = "FengGe · 好事桌宠";
        Width = 200; Height = 240;
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true; Background = Brushes.Transparent;
        Topmost = true; ShowInTaskbar = true;
        Icon = Program.LoadFace();
        Grid grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) });
        grid.RowDefinitions.Add(new RowDefinition());
        captionText = new TextBlock { Foreground = Brushes.White, FontSize = 12, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
        caption = new Border { Background = new SolidColorBrush(Color.FromArgb(220, 25, 25, 25)), CornerRadius = new CornerRadius(10), Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(2), Child = captionText, Visibility = Visibility.Hidden, IsHitTestVisible = false };
        grid.Children.Add(caption);
        face = new Button { Content = new Image { Source = Program.LoadFace(), Stretch = Stretch.Uniform }, Margin = new Thickness(10, 0, 10, 6), Background = Brushes.Transparent, BorderThickness = new Thickness(0), Cursor = Cursors.Hand, ToolTip = "点击播放原声 · 拖动移动 · 右键退出" };
        // A content-only template keeps the source PNG's alpha, with no button chrome.
        ControlTemplate template = new ControlTemplate(typeof(Button));
        template.VisualTree = new FrameworkElementFactory(typeof(ContentPresenter));
        face.Template = template;
        AutomationProperties.SetName(face, "峰哥桌宠，点击播放原声，拖动移动，右键菜单");
        Grid.SetRow(face, 1); grid.Children.Add(face); Content = grid;
        face.Click += delegate { PlayNext(); }; // Space/Enter and accessibility invocation.
        face.PreviewMouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
        {
            mouseStart = PointToScreen(e.GetPosition(this));
            windowStart = new Point(Left, Top); dragging = false;
            face.Focus(); face.CaptureMouse(); e.Handled = true;
        };
        face.PreviewMouseMove += delegate(object sender, MouseEventArgs e)
        {
            if (!face.IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed) return;
            Point now = PointToScreen(e.GetPosition(this));
            Vector delta = now - mouseStart;
            if (delta.Length > 4) dragging = true;
            if (dragging)
            {
                Vector logical = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice.Transform(delta);
                Left = windowStart.X + logical.X; Top = windowStart.Y + logical.Y;
            }
            e.Handled = true;
        };
        face.PreviewMouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e)
        {
            if (!face.IsMouseCaptured) return;
            face.ReleaseMouseCapture();
            if (dragging) { SavePosition(); Log("drag"); } else PlayNext();
            e.Handled = true;
        };
        ContextMenu menu = new ContextMenu();
        AddItem(menu, "随机播放一句", PlayNext);
        AddItem(menu, "移回右下角", delegate { ResetPosition(); SavePosition(); });
        menu.Items.Add(new Separator());
        AddItem(menu, "退出 FengGe", Close);
        face.ContextMenu = menu;
        captionTimer.Tick += delegate { captionTimer.Stop(); caption.Visibility = Visibility.Hidden; };
        Loaded += delegate
        {
            ResetPosition(); RestorePosition();
            StartBridge();
            ShowCaption("点我播放 · 拖动移动", 6); Log("window-shown visible=" + IsVisible + " handle=" + new System.Windows.Interop.WindowInteropHelper(this).Handle);
        };
        Closed += delegate
        {
            closing = true;
            foreach (RegisteredWaitHandle wait in bridgeWaits) wait.Unregister(null);
            foreach (EventWaitHandle signal in bridgeEvents) signal.Dispose();
            SavePosition(); captionTimer.Stop();
            if (player != null) { try { player.Stop(); } catch (ObjectDisposedException) { } }
            Log("closed");
        };
    }

    static void AddItem(ContextMenu menu, string text, Action action)
    {
        MenuItem item = new MenuItem { Header = text };
        item.Click += delegate { action(); }; menu.Items.Add(item);
    }
    void PlayNext()
    {
        if (playing) { Log("click-ignored-busy"); return; }
        PlayClip(bag.Next(), 0);
    }
    void StartBridge()
    {
        for (int turn = 2; turn < TotalTurns; turn += 2) ListenForTurn(turn);
    }
    void ListenForTurn(int turn)
    {
        EventWaitHandle signal = new EventWaitHandle(false, EventResetMode.AutoReset, BridgePrefix + turn);
        bridgeEvents.Add(signal);
        bridgeWaits.Add(ThreadPool.RegisterWaitForSingleObject(signal, delegate(object state, bool timedOut)
        {
            if (!Dispatcher.HasShutdownStarted)
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (closing) return;
                    if (playing) { Log("bridge-skipped-busy", turn); return; }
                    Log("bridge-received", turn);
                    PlayClip(bag.Next(), turn);
                }));
        }, null, Timeout.Infinite, false));
        Log("bridge-ready", turn);
    }
    void PlayClip(int index, int turn)
    {
        current = index; currentTurn = turn;
        try
        {
            player = new SoundPlayer(Program.ClipPath(clips[current]));
            player.Load();
        }
        catch (Exception ex) { ShowCaption("播放失败：" + ex.Message, 10); Log("audio-failed: " + ex.Message); return; }
        playing = true;
        ShowCaption(clips[current].label, clips[current].duration + 2);
        Log(turn == 0 ? "play-requested" : "reply-requested");
        SoundPlayer sound = player;
        ThreadPool.QueueUserWorkItem(delegate
        {
            string error = null;
            try { sound.PlaySync(); }
            catch (Exception ex) { error = ex.Message; }
            finally { sound.Dispose(); }
            if (!Dispatcher.HasShutdownStarted)
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (closing) return;
                    playing = false; player = null;
                    Log(error == null ? "audio-ended" : "audio-failed: " + error);
                    // Each even turn hands over to the next SunPet turn.
                    if (error == null && turn < TotalTurns - 1) NotifyPeer(turn + 1);
                    if (error != null) ShowCaption("播放失败：" + error, 10);
                }));
        });
    }
    void NotifyPeer(int nextTurn)
    {
        try
        {
            using (EventWaitHandle peer = EventWaitHandle.OpenExisting(BridgePrefix + nextTurn)) peer.Set();
            Log("bridge-sent", nextTurn);
        }
        catch (WaitHandleCannotBeOpenedException) { Log("bridge-peer-absent", nextTurn); }
        catch (UnauthorizedAccessException ex) { Log("bridge-unavailable: " + ex.Message, nextTurn); }
        catch (IOException ex) { Log("bridge-unavailable: " + ex.Message, nextTurn); }
    }
    void ShowCaption(string text, double seconds)
    {
        captionText.Text = text; caption.Visibility = Visibility.Visible;
        captionTimer.Stop(); captionTimer.Interval = TimeSpan.FromSeconds(seconds); captionTimer.Start();
    }
    void ResetPosition()
    {
        Rect area = SystemParameters.WorkArea;
        Left = area.Right - Width - 248; Top = area.Bottom - Height - 28;
    }
    void RestorePosition()
    {
        try
        {
            if (!File.Exists(settingsPath)) return;
            double[] point = Program.Json.Deserialize<double[]>(File.ReadAllText(settingsPath));
            if (point == null || point.Length != 2 || point.Any(v => Double.IsNaN(v) || Double.IsInfinity(v))) return;
            // Restore only where an attached screen can still show the avatar.
            Rect saved = new Rect(point[0], point[1], Width, Height);
            foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens)
            {
                Matrix fromPixels = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
                Point origin = fromPixels.Transform(new Point(screen.WorkingArea.X, screen.WorkingArea.Y));
                Point end = fromPixels.Transform(new Point(screen.WorkingArea.Right, screen.WorkingArea.Bottom));
                Rect visible = Rect.Intersect(saved, new Rect(origin, end));
                if (visible.Width >= 80 && visible.Height >= 100) { Left = point[0]; Top = point[1]; return; }
            }
        }
        catch (Exception ex) { Log("settings-read: " + ex.Message); }
    }
    void SavePosition()
    {
        try { File.WriteAllText(settingsPath, Program.Json.Serialize(new double[] { Left, Top })); }
        catch (Exception ex) { Log("settings-write: " + ex.Message); }
    }
    void Log(string name, int signalTurn = 0)
    {
        if (!diagnostics) return;
        try
        {
            File.AppendAllText(Path.Combine(Program.Data, "diagnostics.jsonl"), Program.Json.Serialize(new { utc = DateTime.UtcNow.ToString("o"), pid = System.Diagnostics.Process.GetCurrentProcess().Id, action = name, turn = currentTurn, signalTurn = signalTurn, clip = current < 0 ? 0 : clips[current].id, left = Left, top = Top, playing = playing }) + Environment.NewLine, Encoding.UTF8);
        }
        catch (IOException) { /* Diagnostics must not interrupt the pet. */ }
        catch (UnauthorizedAccessException) { }
    }
}

public static class SelfTest
{
    public static int Run(List<Clip> clips, string reportPath)
    {
        List<object> results = new List<object>();
        try
        {
            if (clips.Count != 11) throw new Exception("Expected 11 original clips.");
            foreach (int size in new int[] { 1, 2, clips.Count })
            {
                ClipBag bag = new ClipBag(size); int previous = -1;
                for (int round = 0; round < 100; round++)
                {
                    HashSet<int> seen = new HashSet<int>();
                    for (int i = 0; i < size; i++)
                    {
                        int next = bag.Next();
                        if (!seen.Add(next) || (size > 1 && next == previous)) throw new Exception("Shuffle bag repeated a clip.");
                        previous = next;
                    }
                }
            }
            foreach (Clip clip in clips)
            {
                string path = Program.ClipPath(clip);
                using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
                {
                    if (new string(reader.ReadChars(4)) != "RIFF") throw new Exception("Not WAV: " + path);
                    reader.ReadUInt32();
                    if (new string(reader.ReadChars(4)) != "WAVE") throw new Exception("Invalid WAV header: " + path);
                    bool pcm = false, data = false;
                    uint byteRate = 0, dataBytes = 0;
                    while (reader.BaseStream.Position + 8 <= reader.BaseStream.Length)
                    {
                        string chunk = new string(reader.ReadChars(4)); uint size = reader.ReadUInt32();
                        long start = reader.BaseStream.Position;
                        if (start + size > reader.BaseStream.Length) throw new Exception("Truncated WAV: " + path);
                        if (chunk == "fmt ")
                        {
                            if (size < 16) throw new Exception("Invalid PCM header: " + path);
                            pcm = reader.ReadUInt16() == 1;
                            ushort channels = reader.ReadUInt16(); reader.ReadUInt32(); byteRate = reader.ReadUInt32();
                            reader.ReadUInt16(); ushort bits = reader.ReadUInt16();
                            if (channels < 1 || channels > 2 || bits != 16 || byteRate == 0) throw new Exception("Unexpected PCM format: " + path);
                        }
                        if (chunk == "data")
                        {
                            dataBytes = size; bool audible = false;
                            for (uint i = 0; i + 1 < size; i += 2) if (reader.ReadInt16() != 0) audible = true;
                            data = size > 0 && audible;
                        }
                        reader.BaseStream.Position = start + size + (size % 2);
                    }
                    if (!pcm || !data) throw new Exception("Missing PCM audio samples: " + path);
                    double seconds = (double)dataBytes / byteRate;
                    if (Math.Abs(seconds - clip.duration) > 0.25) throw new Exception("Unexpected audio duration: " + path);
                    using (SoundPlayer sound = new SoundPlayer(path))
                    {
                        sound.Load(); if (!sound.IsLoadCompleted) throw new Exception("SoundPlayer cannot load: " + path);
                    }
                    results.Add(new { id = clip.id, file = Path.GetFileName(path), pcmSeconds = seconds, soundPlayerLoaded = true });
                }
            }
            File.WriteAllText(reportPath, Program.Json.Serialize(new { pass = true, utc = DateTime.UtcNow.ToString("o"), originalClips = clips.Count, decodedClips = results.Count, shuffleRoundsPerSize = 100, clips = results }), Encoding.UTF8);
            return 0;
        }
        catch (Exception ex)
        {
            File.WriteAllText(reportPath, Program.Json.Serialize(new { pass = false, error = ex.ToString(), decodedClips = results.Count }), Encoding.UTF8);
            return 1;
        }
    }
}
