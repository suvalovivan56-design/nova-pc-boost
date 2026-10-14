using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace NovaPCBoost;

public partial class MainWindow : Window
{
    private const string AccentHex = "#B8FF57";
    private const string CyanHex = "#58D9FF";
    private const string PanelHex = "#111923";
    private const string PanelAltHex = "#151F2A";
    private const string LineHex = "#263543";
    private const string MutedHex = "#A7B1BD";
    private const string TextHex = "#F5F7FA";
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly string _statePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NOVA_PC_BOOST", "restore_state.json");
    private readonly Dictionary<string, JsonElement> _backup = new();
    private readonly Dictionary<string, (string Title, string Description, string Color, string[] Steps)> _games = new()
    {
        ["CS2"] = ("COMPETITIVE / LOW LATENCY", "Упор на отзывчивость, стабильный FPS и чистый тест производительности.", CyanHex, new[] { "Проверь максимальную частоту монитора в расширенных параметрах дисплея.", "Сравни полноэкранный режим и одинаковый игровой сценарий.", "Отключи ненужную фоновую запись клипов, если ей не пользуешься.", "Меняй графику по одному параметру и сравнивай результат." }),
        ["RUST"] = ("SURVIVAL / FRAME STABILITY", "Упор на стабильность кадров, контроль RAM и снижение фоновой нагрузки.", "#FF9A67", new[] { "Проверь использование RAM и закрой тяжёлые фоновые приложения.", "Сравни FPS в одной локации и похожем количестве игроков.", "Снижай тяжёлые графические параметры по одному.", "Оставь запас свободного места на SSD." }),
        ["DOTA 2"] = ("MOBA / CONSISTENCY", "Упор на стабильность в командных боях и контроль тяжёлых эффектов.", "#FF6D7B", new[] { "Проверь лимит FPS и вертикальную синхронизацию.", "Сравни режим отображения и выбранное разрешение.", "Тестируй производительность в похожем командном бою.", "Снижай тяжёлые эффекты по одному параметру." })
    };
    private readonly Dictionary<string, (string Title, string Category, string Description, string Warning)> _tweaks = new()
    {
        ["game_mode"] = ("Игровой режим Windows", "WINDOWS", "Включает Game Mode для текущего пользователя. Прирост зависит от конкретной системы.", "Меняется только профиль текущего пользователя; исходные значения сохраняются для отката."),
        ["capture"] = ("Отключить фоновую запись Xbox", "BACKGROUND", "Отключает фоновые игровые захваты, если они не нужны.", "Применяй, только если не пользуешься фоновыми клипами Xbox."),
        ["visuals"] = ("Уменьшить анимации Windows", "INTERFACE", "Выбирает базовый режим визуальных эффектов оболочки Windows.", "Может измениться внешний вид и анимации интерфейса."),
        ["power"] = ("Высокая производительность", "POWER", "Переключает план питания Windows, если он доступен.", "На ноутбуке увеличится расход батареи и может вырасти нагрев.")
    };
    private readonly Dictionary<string, List<RegSnapshot>> _regBackups = new();
    private readonly Dictionary<string, string> _powerBackups = new();
    private readonly Dictionary<string, (string Title, string Description)> _tools = new()
    {
        ["taskmgr.exe"] = ("Диспетчер задач", "Процессы, нагрузка CPU/RAM и автозагрузка."),
        ["resmon.exe"] = ("Монитор ресурсов", "Подробная нагрузка на процессор, память, диск и сеть."),
        ["msinfo32.exe"] = ("Сведения о системе", "Информация об оборудовании и Windows.")
    };

    public MainWindow()
    {
        InitializeComponent();
        LoadBackups();
        ShowDashboard();
        _timer.Tick += (_, _) => UpdateStats();
        _timer.Start();
    }

    private static Brush Brush(string hex) => new BrushConverter().ConvertFromString(hex) as Brush ?? Brushes.White;
    private TextBlock Txt(string text, double size = 12, string color = TextHex, bool bold = false) =>
        new() { Text = text, FontSize = size, Foreground = Brush(color), FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, TextWrapping = TextWrapping.Wrap };
    private Border Card(UIElement child, string background = PanelHex, Thickness? padding = null) =>
        new() { Child = child, Background = Brush(background), BorderBrush = Brush(LineHex), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = padding ?? new Thickness(18), Margin = new Thickness(0, 0, 0, 12) };
    private StackPanel Stack(double spacing = 8) => new() { Orientation = Orientation.Vertical, Margin = new Thickness(0), };
    private Button Btn(string text, RoutedEventHandler click, bool primary = true)
    {
        var button = new Button { Content = text, Background = Brush(primary ? AccentHex : PanelAltHex), Foreground = Brush(primary ? "#080B10" : TextHex), Padding = new Thickness(15, 10, 15, 10), Margin = new Thickness(0, 4, 8, 0), HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 110 };
        button.Click += click;
        return button;
    }
    private void AddSection(string eyebrow, string title, string subtitle)
    {
        PageContent.Children.Add(Txt(eyebrow, 10, AccentHex, true));
        var titleBlock = Txt(title, 24, TextHex, true);
        titleBlock.Margin = new Thickness(0, 6, 0, 4);
        PageContent.Children.Add(titleBlock);
        var sub = Txt(subtitle, 12, MutedHex);
        sub.Margin = new Thickness(0, 0, 0, 17);
        PageContent.Children.Add(sub);
    }
    private void SetPage(string crumb, string title)
    {
        Breadcrumb.Text = crumb;
        PageTitle.Text = title;
        PageContent.Children.Clear();
        PageContent.Opacity = 0;
        var pageShift = new TranslateTransform(0, 12);
        PageContent.RenderTransform = pageShift;
        PageContent.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
        pageShift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(320))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
        NavDashboard.Background = Brush("#0B1017"); NavDashboard.Foreground = Brush(MutedHex);
        NavProfiles.Background = Brush("#0B1017"); NavProfiles.Foreground = Brush(MutedHex);
        NavTweaks.Background = Brush("#0B1017"); NavTweaks.Foreground = Brush(MutedHex);
        NavTools.Background = Brush("#0B1017"); NavTools.Foreground = Brush(MutedHex);
        NavRestore.Background = Brush("#0B1017"); NavRestore.Foreground = Brush(MutedHex);
    }
    private void ActiveNav(Button button) { button.Background = Brush("#19251B"); button.Foreground = Brush(AccentHex); }
    private void Dashboard_Click(object sender, RoutedEventArgs e) => ShowDashboard();
    private void Profiles_Click(object sender, RoutedEventArgs e) => ShowProfiles();
    private void Tweaks_Click(object sender, RoutedEventArgs e) => ShowTweaks();
    private void Tools_Click(object sender, RoutedEventArgs e) => ShowTools();
    private void Restore_Click(object sender, RoutedEventArgs e) => ShowRestore();

    private void ShowDashboard()
    {
        SetPage("NOVA  /  OVERVIEW", "Твой ПК. Твои правила.");
        ActiveNav(NavDashboard);
        var hero = new Grid { MinHeight = 190, Margin = new Thickness(0, 4, 0, 18) };
        hero.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        hero.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var left = new StackPanel { Margin = new Thickness(22, 22, 10, 22), VerticalAlignment = VerticalAlignment.Center };
        left.Children.Add(Txt("✳  YOUR WINDOWS, TUNED BY YOU", 10, AccentHex, true));
        var headline = Txt("ГОТОВ К ИГРЕ?", 30, TextHex, true); headline.Margin = new Thickness(0, 12, 0, 5); left.Children.Add(headline);
        left.Children.Add(Txt("Твики, игровые профили и системные инструменты — в одном аккуратном приложении для Windows.", 13, MutedHex));
        var actions = new WrapPanel { Margin = new Thickness(0, 14, 0, 0) };
        actions.Children.Add(Btn("Открыть твики  →", (_, _) => ShowTweaks()));
        actions.Children.Add(Btn("Игровые профили", (_, _) => ShowProfiles(), false));
        left.Children.Add(actions);
        var right = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        right.Children.Add(Txt("NOVA", 38, AccentHex, true));
        right.Children.Add(Txt("CONTROL CENTER", 10, CyanHex, true));
        right.Children.Add(Txt("● READY", 10, AccentHex, true));
        Grid.SetColumn(left, 0); Grid.SetColumn(right, 1); hero.Children.Add(left); hero.Children.Add(right);
        PageContent.Children.Add(Card(hero, "#111B22", new Thickness(4)));
        AddSection("01 / LIVE MONITOR", "Состояние системы", "Показатели обновляются автоматически и не отправляются в интернет.");
        var stats = new UniformGrid { Columns = 3, Margin = new Thickness(0, 0, 0, 16) };
        stats.Children.Add(StatCard("CPU", "—%", "Нагрузка процессора", CyanHex, "cpu"));
        stats.Children.Add(StatCard("RAM", "—%", "Использование памяти", AccentHex, "ram"));
        stats.Children.Add(StatCard("ДИСК", "—%", "Занято на системном диске", "#FFCB70", "disk"));
        PageContent.Children.Add(stats);
        AddSection("02 / GAME PROFILES", "Выбери свою игру", "Практичные рекомендации без обещаний магического прироста FPS.");
        var games = new UniformGrid { Columns = 3 };
        foreach (var game in _games.Keys)
        {
            var info = _games[game];
            var panel = new StackPanel();
            panel.Children.Add(Txt(game, 22, info.Color, true));
            panel.Children.Add(Txt(info.Title, 9, MutedHex, true));
            var desc = Txt(info.Description, 11, MutedHex); desc.Margin = new Thickness(0, 9, 0, 8); panel.Children.Add(desc);
            panel.Children.Add(Btn("Открыть профиль  →", (_, _) => ShowGameProfile(game)));
            games.Children.Add(Card(panel, PanelHex, new Thickness(15)));
        }
        PageContent.Children.Add(games);
        var note = new StackPanel();
        note.Children.Add(Txt("ВАЖНО", 10, "#FFCB70", true));
        note.Children.Add(Txt("Меняй по одному параметру и проверяй результат. Все поддерживаемые твики можно откатить.", 11, MutedHex));
        PageContent.Children.Add(Card(note, "#17170F"));
    }

    private Border StatCard(string name, string value, string detail, string color, string tag)
    {
        var panel = new StackPanel();
        panel.Children.Add(Txt(name, 10, MutedHex, true));
        var val = Txt(value, 30, TextHex, true); val.Tag = tag; panel.Children.Add(val);
        var sub = Txt(detail, 10, MutedHex); sub.Tag = tag + "_detail"; panel.Children.Add(sub);
        var track = new Border { Background = Brush("#25313D"), Height = 5, CornerRadius = new CornerRadius(3), Margin = new Thickness(0, 10, 0, 0) };
        var fill = new Border { Background = Brush(color), Width = 0, HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(3) };
        track.Child = fill; fill.Tag = tag + "_fill"; panel.Children.Add(track);
        var card = Card(panel, PanelHex, new Thickness(16)); card.Margin = new Thickness(0, 0, 10, 0); card.Tag = tag + "_card";
        return card;
    }

    private void UpdateStats()
    {
        try
        {
            var mem = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() }; GlobalMemoryStatusEx(ref mem);
            var ramPercent = mem.TotalPhys == 0 ? 0 : (double)(mem.TotalPhys - mem.AvailPhys) / mem.TotalPhys * 100;
            var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\");
            var diskPercent = drive.TotalSize == 0 ? 0 : (double)(drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize * 100;
            var cpu = ReadCpu();
            UpdateStat("cpu", cpu, $"{cpu:0}% загрузки");
            UpdateStat("ram", ramPercent, $"{(mem.TotalPhys - mem.AvailPhys) / 1073741824d:0.0} / {mem.TotalPhys / 1073741824d:0.0} ГБ");
            UpdateStat("disk", diskPercent, $"Свободно {drive.AvailableFreeSpace / 1073741824d:0.0} ГБ");
        }
        catch { }
    }

    private ulong _lastIdle, _lastKernel, _lastUser;
    private double ReadCpu()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user)) return 0;
        var i = ToUInt64(idle); var k = ToUInt64(kernel); var u = ToUInt64(user);
        var total = (k - _lastKernel) + (u - _lastUser);
        var idleDelta = i - _lastIdle;
        _lastIdle = i; _lastKernel = k; _lastUser = u;
        return total == 0 ? 0 : Math.Clamp(100d * (total - idleDelta) / total, 0, 100);
    }
    private static ulong ToUInt64(FileTime ft) => ((ulong)ft.High << 32) | ft.Low;
    private void UpdateStat(string tag, double value, string detail)
    {
        foreach (var tb in FindVisualChildren<TextBlock>(PageContent))
        {
            if (Equals(tb.Tag, tag)) tb.Text = $"{value:0}%";
            else if (Equals(tb.Tag, tag + "_detail")) tb.Text = detail;
        }
        foreach (var border in FindVisualChildren<Border>(PageContent))
        {
            if (border.Child is Border fill && Equals(fill.Tag, tag + "_fill"))
                fill.Width = Math.Max(0, border.ActualWidth * Math.Clamp(value, 0, 100) / 100);
        }
    }
    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed) yield return typed;
            foreach (var descendant in FindVisualChildren<T>(child)) yield return descendant;
        }
    }

    private void ShowProfiles()
    {
        SetPage("NOVA  /  GAME PROFILES", "Под каждый стиль — свой план.");
        ActiveNav(NavProfiles);
        AddSection("01 / CHOOSE YOUR GAME", "Игровые профили", "Выбери игру, чтобы открыть план настройки и рекомендации.");
        var grid = new UniformGrid { Columns = 3 };
        foreach (var game in _games.Keys)
        {
            var info = _games[game]; var p = new StackPanel();
            p.Children.Add(Txt(game, 26, info.Color, true));
            p.Children.Add(Txt(info.Title, 9, MutedHex, true));
            var desc = Txt(info.Description, 11, MutedHex); desc.Margin = new Thickness(0, 10, 0, 12); p.Children.Add(desc);
            foreach (var step in info.Steps.Take(3))
            {
                var line = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };
                line.Children.Add(Txt("✓", 11, info.Color, true));
                var t = Txt("  " + step, 10, TextHex); t.MaxWidth = 240; line.Children.Add(t); p.Children.Add(line);
            }
            p.Children.Add(Btn("Открыть профиль  →", (_, _) => ShowGameProfile(game)));
            grid.Children.Add(Card(p, PanelHex, new Thickness(16)));
        }
        PageContent.Children.Add(grid);
    }

    private void ShowGameProfile(string game)
    {
        SetPage("NOVA  /  GAME PROFILE", $"Профиль {game}");
        ActiveNav(NavProfiles);
        var info = _games[game];
        AddSection("01 / " + info.Title, game, info.Description);
        var list = new StackPanel();
        foreach (var (step, i) in info.Steps.Select((s, i) => (s, i + 1)))
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            var num = new Border { Background = Brush("#1C2A20"), CornerRadius = new CornerRadius(8), Width = 38, Height = 38, Child = Txt(i.ToString("00"), 13, info.Color, true), VerticalAlignment = VerticalAlignment.Top };
            row.Children.Add(num);
            var text = new StackPanel { Margin = new Thickness(13, 0, 0, 0) };
            text.Children.Add(Txt(step, 13, TextHex, true));
            text.Children.Add(Txt("Рекомендация профиля. Измени настройку вручную и сравни производительность.", 11, MutedHex));
            row.Children.Add(text);
            list.Children.Add(Card(row, PanelHex));
        }
        PageContent.Children.Add(list);
        PageContent.Children.Add(Btn("Применить базовые игровые твики", (_, _) =>
        {
            if (MessageBox.Show($"Для {game} включить Game Mode и отключить фоновую запись Xbox? Исходные параметры сохранятся для отката.", "NOVA PC BOOST", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ApplyTweak("game_mode"); ApplyTweak("capture");
            }
        }));
        PageContent.Children.Add(Btn("← Все профили", (_, _) => ShowProfiles(), false));
    }

    private void ShowTweaks()
    {
        SetPage("NOVA  /  WINDOWS TWEAKS", "Настройки под контролем.");
        ActiveNav(NavTweaks);
        AddSection("01 / CONTROL PANEL", "Твики Windows", "Каждый параметр применяется отдельно. NOVA сохраняет исходные значения и позволяет откатить изменения.");
        var warning = new StackPanel();
        warning.Children.Add(Txt("⚠  ПЕРЕД ПРИМЕНЕНИЕМ", 10, "#FFCB70", true));
        warning.Children.Add(Txt("Закрой игры и сохрани работу. Меняй по одному параметру. Реестр изменяется только в профиле текущего пользователя.", 11, MutedHex));
        PageContent.Children.Add(Card(warning, "#17170F"));
        foreach (var (id, spec) in _tweaks)
        {
            var p = new StackPanel();
            p.Children.Add(Txt(spec.Title, 15, TextHex, true));
            p.Children.Add(Txt(spec.Category + "  •  " + spec.Description, 11, MutedHex));
            p.Children.Add(Txt("ВНИМАНИЕ: " + spec.Warning, 10, "#FFCB70"));
            var actions = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
            actions.Children.Add(Btn("Применить", (_, _) => ApplyTweak(id)));
            actions.Children.Add(Btn("Откатить", (_, _) => RestoreTweak(id), false));
            p.Children.Add(actions);
            PageContent.Children.Add(Card(p));
        }
    }

    private void ShowTools()
    {
        SetPage("NOVA  /  SYSTEM TOOLS", "Полезные инструменты.");
        ActiveNav(NavTools);
        AddSection("01 / WINDOWS UTILITIES", "Быстрый доступ к системе", "Открываются штатные инструменты Windows — без сомнительных замен.");
        foreach (var (exe, info) in _tools)
        {
            var p = new StackPanel();
            p.Children.Add(Txt(info.Title, 15, TextHex, true));
            p.Children.Add(Txt(info.Description, 11, MutedHex));
            p.Children.Add(Btn("Открыть  ↗", (_, _) => RunTool(exe), false));
            PageContent.Children.Add(Card(p));
        }
        foreach (var (uri, title, desc) in new[] {
            ("ms-settings:gaming-gamemode", "Игровой режим", "Настройки Game Mode Windows"),
            ("ms-settings:startupapps", "Автозагрузка", "Управление запускаемыми приложениями"),
            ("ms-settings:display", "Параметры дисплея", "Разрешение и частота монитора"),
            ("ms-settings:powersleep", "Питание", "Параметры питания Windows")
        })
        {
            var p = new StackPanel();
            p.Children.Add(Txt(title, 15, TextHex, true)); p.Children.Add(Txt(desc, 11, MutedHex));
            p.Children.Add(Btn("Открыть настройки  ↗", (_, _) => OpenSettings(uri), false));
            PageContent.Children.Add(Card(p));
        }
        var clean = new StackPanel();
        clean.Children.Add(Txt("Очистка временных файлов", 15, TextHex, true));
        clean.Children.Add(Txt("Удаляет только файлы старше 48 часов из пользовательских Temp-папок. Занятые файлы пропускаются.", 11, MutedHex));
        clean.Children.Add(Btn("Запустить очистку", (_, _) => CleanTemp()));
        PageContent.Children.Add(Card(clean));
    }

    private void ShowRestore()
    {
        SetPage("NOVA  /  RECOVERY", "Откат изменений.");
        ActiveNav(NavRestore);
        AddSection("01 / SAFETY NET", "Резервные копии", "Исходные значения хранятся локально в профиле пользователя.");
        LoadBackups();
        if (_regBackups.Count == 0 && _powerBackups.Count == 0)
        {
            var p = new StackPanel(); p.Children.Add(Txt("✓  Пока нечего откатывать", 18, AccentHex, true));
            p.Children.Add(Txt("Когда применишь твик, NOVA сохранит исходные значения и покажет их здесь.", 12, MutedHex));
            PageContent.Children.Add(Card(p));
            return;
        }
        foreach (var id in _regBackups.Keys.Concat(_powerBackups.Keys).Distinct().ToList())
        {
            var p = new StackPanel();
            p.Children.Add(Txt(_tweaks.GetValueOrDefault(id).Title ?? id, 15, TextHex, true));
            p.Children.Add(Txt("Исходные параметры сохранены локально.", 11, MutedHex));
            p.Children.Add(Btn("Откатить этот твик", (_, _) => RestoreTweak(id), false));
            PageContent.Children.Add(Card(p));
        }
        PageContent.Children.Add(Btn("Откатить все изменения", (_, _) =>
        {
            if (MessageBox.Show("Откатить все сохранённые изменения?", "NOVA PC BOOST", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            foreach (var id in _regBackups.Keys.Concat(_powerBackups.Keys).Distinct().ToList()) RestoreTweak(id, false);
            MessageBox.Show("Операция отката завершена.", "NOVA PC BOOST", MessageBoxButton.OK, MessageBoxImage.Information);
            ShowRestore();
        }, false));
    }

    private void ApplyTweak(string id)
    {
        if (!OperatingSystem.IsWindows()) { MessageBox.Show("Твики доступны только в Windows."); return; }
        try
        {
            if (id == "power")
            {
                if (!_powerBackups.ContainsKey(id))
                {
                    var current = RunCapture("powercfg", "/getactivescheme");
                    var match = System.Text.RegularExpressions.Regex.Match(current, @"[0-9a-fA-F]{8}-(?:[0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}");
                    if (!match.Success) throw new InvalidOperationException("Не удалось определить текущий план питания.");
                    _powerBackups[id] = match.Value;
                }
                RunChecked("powercfg", "/setactive SCHEME_MIN");
            }
            else
            {
                var values = GetRegValues(id);
                if (!_regBackups.ContainsKey(id))
                {
                    _regBackups[id] = values.Select(v => RegSnapshot.Capture(v.Hive, v.Key, v.Name)).ToList();
                }
                foreach (var v in values) using (var key = Registry.CurrentUser.CreateSubKey(v.Key)) key?.SetValue(v.Name, v.Value, RegistryValueKind.DWord);
            }
            SaveBackups();
            StatusText.Text = "ПРИМЕНЁН ТВИК: " + _tweaks[id].Title.ToUpperInvariant();
            MessageBox.Show("Параметр применён. Исходные значения сохранены — при необходимости откати его во вкладке «Откат изменений».", "NOVA PC BOOST", MessageBoxButton.OK, MessageBoxImage.Information);
            ShowTweaks();
        }
        catch (Exception ex) { MessageBox.Show("Не удалось применить твик:\n" + ex.Message, "NOVA PC BOOST", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private List<(RegistryKey Hive, string Key, string Name, int Value)> GetRegValues(string id) => id switch
    {
        "game_mode" => new() { (Registry.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1), (Registry.CurrentUser, @"Software\Microsoft\GameBar", "AllowAutoGameMode", 1) },
        "capture" => new() { (Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 0), (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0) },
        "visuals" => new() { (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2) },
        _ => new()
    };

    private void RestoreTweak(string id, bool notify = true)
    {
        try
        {
            if (_powerBackups.TryGetValue(id, out var guid)) RunChecked("powercfg", "/setactive " + guid);
            if (_regBackups.TryGetValue(id, out var values))
            {
                foreach (var value in values.AsEnumerable().Reverse()) value.Restore();
                _regBackups.Remove(id);
            }
            _powerBackups.Remove(id);
            SaveBackups();
            StatusText.Text = "ИЗМЕНЕНИЯ ОТКАЧЕНЫ";
            if (notify) MessageBox.Show("Исходные значения восстановлены.", "NOVA PC BOOST", MessageBoxButton.OK, MessageBoxImage.Information);
            ShowRestore();
        }
        catch (Exception ex) { MessageBox.Show("Не удалось выполнить откат:\n" + ex.Message, "NOVA PC BOOST", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void LoadBackups()
    {
        _regBackups.Clear(); _powerBackups.Clear();
        try
        {
            if (!File.Exists(_statePath)) return;
            using var doc = JsonDocument.Parse(File.ReadAllText(_statePath));
            if (doc.RootElement.TryGetProperty("power", out var power))
                foreach (var p in power.EnumerateObject()) _powerBackups[p.Name] = p.Value.GetString() ?? "";
            if (doc.RootElement.TryGetProperty("registry", out var registry))
                foreach (var p in registry.EnumerateObject())
                    _regBackups[p.Name] = p.Value.Deserialize<List<RegSnapshot>>() ?? new();
        }
        catch { }
    }
    private void SaveBackups()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_statePath)!);
        var data = new { power = _powerBackups, registry = _regBackups };
        File.WriteAllText(_statePath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void RunTool(string exe)
    {
        try { Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true }); StatusText.Text = "ЗАПУЩЕНО: " + exe.ToUpperInvariant(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "NOVA PC BOOST", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
    private void OpenSettings(string uri)
    {
        try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); StatusText.Text = "ОТКРЫТЫ ПАРАМЕТРЫ WINDOWS"; }
        catch (Exception ex) { MessageBox.Show(ex.Message, "NOVA PC BOOST", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
    private void CleanTemp()
    {
        if (MessageBox.Show("Удалить только пользовательские временные файлы старше 48 часов? Занятые файлы будут пропущены.", "NOVA PC BOOST", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        var roots = new[] { Path.GetTempPath(), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp") }.Distinct();
        var cutoff = DateTime.UtcNow.AddHours(-48); int removed = 0, errors = 0; long freed = 0;
        foreach (var root in roots)
        {
            if (!Directory.Exists(root)) continue;
            try
            {
                foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        if (info.LastWriteTimeUtc > cutoff || (info.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                        long size = info.Length; info.Delete(); removed++; freed += size;
                    }
                    catch { errors++; }
                }
            }
            catch { errors++; }
        }
        MessageBox.Show($"Удалено файлов: {removed}\nОсвобождено: {freed / 1048576d:0.0} МБ\nПропущено файлов: {errors}", "NOVA PC BOOST", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string RunCapture(string exe, string args)
    {
        using var p = Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true }) ?? throw new InvalidOperationException("Не удалось запустить " + exe);
        var stdout = p.StandardOutput.ReadToEnd(); var stderr = p.StandardError.ReadToEnd(); p.WaitForExit(10000);
        if (p.ExitCode != 0) throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? stdout : stderr);
        return stdout;
    }
    private static void RunChecked(string exe, string args)
    {
        using var p = Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true }) ?? throw new InvalidOperationException("Не удалось запустить " + exe);
        var stdout = p.StandardOutput.ReadToEnd(); var stderr = p.StandardError.ReadToEnd(); p.WaitForExit(10000);
        if (p.ExitCode != 0) throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? stdout : stderr);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime { public uint Low; public uint High; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatus
    {
        public uint Length; public uint MemoryLoad; public ulong TotalPhys; public ulong AvailPhys; public ulong TotalPageFile; public ulong AvailPageFile; public ulong TotalVirtual; public ulong AvailVirtual; public ulong AvailExtendedVirtual;
    }
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetSystemTimes(out FileTime idle, out FileTime kernel, out FileTime user);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GlobalMemoryStatusEx(ref MemoryStatus buffer);

    private sealed class RegSnapshot
    {
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public bool Exists { get; set; }
        public object? Value { get; set; }
        public RegistryValueKind Kind { get; set; }
        public static RegSnapshot Capture(RegistryKey hive, string keyPath, string name)
        {
            using var key = hive.OpenSubKey(keyPath, false);
            if (key is null || !key.GetValueNames().Contains(name)) return new() { Key = keyPath, Name = name, Exists = false, Kind = RegistryValueKind.DWord };
            return new() { Key = keyPath, Name = name, Exists = true, Value = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames), Kind = key.GetValueKind(name) };
        }
        public void Restore()
        {
            using var key = Registry.CurrentUser.CreateSubKey(Key);
            if (key is null) return;
            if (Exists)
            {
                object restored = Value is JsonElement element ? element.ValueKind switch
                {
                    JsonValueKind.Number when element.TryGetInt32(out var number) => number,
                    JsonValueKind.Number when element.TryGetInt64(out var longNumber) => longNumber,
                    JsonValueKind.String => element.GetString() ?? "",
                    JsonValueKind.True => 1,
                    JsonValueKind.False => 0,
                    JsonValueKind.Array => element.EnumerateArray().Select(x => x.GetString() ?? "").ToArray(),
                    _ => throw new InvalidOperationException("Неподдерживаемый тип значения для отката.")
                } : Value!;
                key.SetValue(Name, restored, Kind);
            }
            else if (key.GetValueNames().Contains(Name)) key.DeleteValue(Name, false);
        }
    }
}
