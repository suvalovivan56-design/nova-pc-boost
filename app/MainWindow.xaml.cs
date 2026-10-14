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
    private const string AccentHex = "#BCFF5B";
    private const string CyanHex = "#8DFFCB";
    private const string PanelHex = "#111812";
    private const string PanelAltHex = "#182119";
    private const string LineHex = "#29382B";
    private const string MutedHex = "#A2AEA0";
    private const string TextHex = "#F4F7F1";
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
        ["tailored"] = ("Отключить персонализированные советы", "PRIVACY", "Отключает использование диагностических данных для персонализированных советов и рекомендаций текущего пользователя.", "Некоторые советы и рекомендации Windows могут стать менее персонализированными."),
        ["ads_id"] = ("Отключить рекламный идентификатор", "PRIVACY", "Отключает рекламный ID Windows для текущего пользователя.", "Не отключает рекламу во всех приложениях и сайтах."),
        ["feedback"] = ("Уменьшить запросы обратной связи", "PRIVACY", "Снижает частоту системных запросов отправить отзыв.", "Не отключает обязательные диагностические данные."),
        ["typing"] = ("Отключить персонализацию ввода", "PRIVACY", "Отключает сбор данных об улучшении рукописного ввода и печати для текущего пользователя.", "Может снизить персонализацию функций ввода."),
        ["activity"] = ("Отключить историю активности", "PRIVACY", "Отключает публикацию активности пользователя в истории Windows для текущего пользователя.", "Функции, зависящие от истории активности, могут быть ограничены."),
        ["power"] = ("Высокая производительность", "POWER", "Переключает план питания Windows, если он доступен.", "На ноутбуке увеличится расход батареи и может вырасти нагрев."),
        ["search_highlights"] = ("Отключить подсветку поиска", "PRIVACY", "Убирает динамические подсказки в поиске Windows.", "Часть рекомендаций поиска пропадёт."),
        ["system_suggestions"] = ("Отключить системные предложения", "PRIVACY", "Отключает часть предложений Windows для текущего пользователя.", "В интерфейсе будет меньше подсказок."),
        ["soft_landing"] = ("Отключить советы после установки", "PRIVACY", "Отключает часть всплывающих советов Windows.", "Подсказки новым функциям не будут появляться."),
        ["subscribed_338388"] = ("Отключить рекомендуемый контент 1", "PRIVACY", "Отключает один канал рекомендованного контента Windows.", "Меньше рекомендаций."),
        ["subscribed_338389"] = ("Отключить рекомендуемый контент 2", "PRIVACY", "Отключает дополнительный канал подсказок Windows.", "Меньше рекомендаций."),
        ["subscribed_310093"] = ("Отключить советы после обновления", "PRIVACY", "Убирает часть приветственных экранов после обновлений.", "Информационные экраны могут не показываться."),
        ["subscribed_353694"] = ("Отключить предложения настройки", "PRIVACY", "Отключает часть предложений завершить настройку Windows.", "Напоминания о настройке пропадут."),
        ["subscribed_353696"] = ("Отключить советы меню Пуск", "PRIVACY", "Отключает дополнительный рекомендованный контент.", "Меньше подсказок в меню Пуск."),
        ["silent_apps"] = ("Отключить рекомендованные установки", "PRIVACY", "Отключает один из каналов рекомендаций приложений.", "Не удаляет уже установленные программы."),
        ["preinstalled_apps"] = ("Отключить предложения приложений", "PRIVACY", "Отключает предложения предустановленных приложений.", "Не удаляет уже установленные программы."),
        ["preinstalled_ever"] = ("Отключить повторные предложения приложений", "PRIVACY", "Отключает ещё один параметр рекомендаций приложений.", "Может быть проигнорирован отдельными сборками."),
        ["start_track_progs"] = ("Не учитывать запуск приложений", "PRIVACY", "Отключает учёт запуска приложений для персонализации меню Пуск.", "Рекомендации Пуска станут менее персональными."),
        ["start_track_docs"] = ("Не учитывать открытые документы", "PRIVACY", "Отключает учёт недавних документов для меню Пуск.", "Недавние элементы могут отображаться иначе."),
        ["no_recent_docs"] = ("Скрыть историю недавних документов", "PRIVACY", "Отключает историю недавних документов для текущего пользователя.", "Быстрый доступ к недавно открытым файлам станет ограничен."),
        ["widgets"] = ("Скрыть кнопку виджетов", "INTERFACE", "Скрывает кнопку виджетов на панели задач в поддерживаемых версиях Windows.", "Виджеты можно будет вернуть откатом."),
        ["chat_button"] = ("Скрыть кнопку чата", "INTERFACE", "Скрывает кнопку чата в совместимых версиях Windows.", "Не удаляет приложение чата."),
        ["task_view"] = ("Скрыть кнопку представления задач", "INTERFACE", "Скрывает кнопку Task View на панели задач.", "Горячие клавиши рабочих столов остаются доступны."),
        ["search_taskbar"] = ("Скрыть поиск на панели задач", "INTERFACE", "Скрывает поле поиска на панели задач.", "Поиск остаётся доступен через меню Пуск."),
        ["file_extensions"] = ("Показывать расширения файлов", "EXPLORER", "Показывает расширения известных типов файлов.", "Изменится отображение имён файлов."),
        ["hidden_files"] = ("Показывать скрытые файлы", "EXPLORER", "Показывает скрытые файлы и папки в Проводнике.", "Не изменяй неизвестные системные файлы."),
        ["super_hidden"] = ("Показывать защищённые системные файлы", "EXPLORER", "Показывает защищённые системные файлы.", "Риск случайного изменения системных файлов выше."),
        ["separate_explorer"] = ("Открывать папки в отдельном процессе", "EXPLORER", "Включает отдельный процесс для окон папок.", "Может немного увеличить использование памяти."),
        ["full_path_title"] = ("Показывать полный путь в заголовке", "EXPLORER", "Показывает полный путь в заголовке окна Проводника.", "Изменится заголовок окна."),
        ["disable_autoplay"] = ("Отключить автозапуск носителей", "SECURITY", "Отключает AutoPlay для текущего пользователя.", "Носители не будут предлагать автоматические действия."),
        ["disable_autorun"] = ("Отключить AutoRun", "SECURITY", "Отключает AutoRun для текущего пользователя.", "Автоматический запуск с носителей ограничивается."),
        ["clipboard_history"] = ("Отключить историю буфера обмена", "PRIVACY", "Отключает историю буфера обмена Windows.", "История Win+V перестанет работать."),
        ["cloud_clipboard"] = ("Отключить синхронизацию буфера", "PRIVACY", "Отключает облачную синхронизацию буфера обмена.", "Общий буфер между устройствами перестанет работать."),
        ["activity_feed"] = ("Отключить ленту активности", "PRIVACY", "Отключает ленту активности в поддерживаемых версиях Windows.", "Функции истории активности могут быть ограничены."),
        ["publish_activity"] = ("Запретить публикацию активности", "PRIVACY", "Отключает публикацию действий пользователя политикой Windows.", "Связанные функции синхронизации могут перестать работать."),
        ["upload_activity"] = ("Запретить отправку активности", "PRIVACY", "Отключает отправку истории активности в поддерживаемых версиях Windows.", "Синхронизация активности между устройствами будет ограничена."),
        ["consumer_features"] = ("Отключить потребительские рекомендации", "PRIVACY", "Отключает часть потребительских функций Windows там, где политика поддерживается.", "Некоторые рекомендации исчезнут; редакция Windows влияет на поддержку."),
        ["soft_landing_policy"] = ("Отключить рекомендации политикой", "PRIVACY", "Отключает часть мягких рекомендаций через пользовательскую политику.", "Поддержка зависит от редакции Windows."),
        ["tailored_policy"] = ("Отключить персонализацию политикой", "PRIVACY", "Ограничивает персонализацию контента на основе диагностических данных.", "Не отключает обязательную системную диагностику."),
        ["disable_web_search"] = ("Отключить веб-поиск Windows", "PRIVACY", "Ограничивает веб-результаты в поиске Windows там, где политика поддерживается.", "Веб-результаты из меню Пуск могут пропасть."),
        ["disable_cloud_search"] = ("Отключить облачный поиск", "PRIVACY", "Ограничивает облачные результаты поиска Windows.", "Может сократить число результатов поиска."),
        ["cortana_policy"] = ("Ограничить Cortana", "PRIVACY", "Отключает Cortana в версиях Windows, где параметр ещё используется.", "Функции Cortana могут стать недоступны."),
        ["disable_search_history"] = ("Отключить историю поиска устройства", "PRIVACY", "Отключает историю поиска устройства в поддерживаемых сборках Windows.", "История поиска не будет предлагаться."),
        ["disable_dynamic_search"] = ("Отключить динамические предложения поиска", "PRIVACY", "Отключает динамические предложения в поиске Windows.", "Подсказок будет меньше."),
        ["background_apps"] = ("Ограничить фоновые приложения", "BACKGROUND", "Отключает глобальную фоновую активность приложений там, где параметр поддерживается.", "Синхронизация и уведомления приложений могут задерживаться."),
        ["disable_maps_auto"] = ("Отключить автообновление карт", "BACKGROUND", "Отключает автоматическое обновление офлайн-карт в поддерживаемых версиях.", "Карты могут требовать ручного обновления."),
        ["maps_download_policy"] = ("Отключить автозагрузку карт", "BACKGROUND", "Отключает автоматическую загрузку карт политикой Windows, если поддерживается.", "Карты не будут скачиваться автоматически."),
        ["notifications_global"] = ("Отключить уведомления Windows", "FOCUS", "Отключает toast-уведомления текущего пользователя в поддерживаемых сборках.", "Можно пропустить важные уведомления."),
        ["disable_toast_policy"] = ("Отключить всплывающие уведомления политикой", "FOCUS", "Отключает toast-уведомления через пользовательскую политику Windows.", "Некоторые приложения перестанут показывать всплывающие сообщения."),
        ["nearby_sharing"] = ("Ограничить обмен поблизости", "PRIVACY", "Отключает разрешение обмена с близлежащими устройствами там, где поддерживается.", "Nearby Sharing может перестать работать."),
        ["cross_device"] = ("Ограничить функции между устройствами", "PRIVACY", "Ограничивает часть функций Connected Devices Platform для текущего пользователя.", "Связанные функции обмена могут быть ограничены."),
        ["typing_insights"] = ("Отключить инсайты ввода", "PRIVACY", "Отключает текстовые инсайты в сборках Windows, где параметр поддерживается.", "Подсказки ввода могут измениться."),
        ["emoji_suggestions"] = ("Отключить предложения эмодзи", "INTERFACE", "Отключает предложения эмодзи в поддерживаемых сборках Windows.", "Предложения эмодзи могут не отображаться."),
        ["thumbnail_cache_policy"] = ("Отключить кэш эскизов политикой", "EXPLORER", "Запрещает кэширование эскизов в совместимых версиях Windows.", "Папки с фото и видео могут открываться медленнее."),
        ["thumbnail_db"] = ("Не создавать thumbs.db", "EXPLORER", "Ограничивает создание thumbs.db в поддерживаемых конфигурациях.", "Просмотр сетевых папок с изображениями может замедлиться."),
        ["lock_screen_overlay"] = ("Отключить советы экрана блокировки", "PRIVACY", "Отключает часть рекомендованного контента на экране блокировки.", "Информационные карточки могут исчезнуть."),
        ["lock_screen_wallpaper"] = ("Отключить контент Windows Spotlight", "PRIVACY", "Отключает Spotlight-контент на экране блокировки в поддерживаемых сборках.", "Автоматические рекомендации изображений могут пропасть."),
        ["disable_consumer_tips"] = ("Отключить советы потребительского контента", "PRIVACY", "Ограничивает часть советов и рекомендованного контента Windows.", "Поддержка зависит от редакции Windows."),
    };
    private readonly Dictionary<string, List<RegSnapshot>> _regBackups = new();
    private readonly Dictionary<string, string> _powerBackups = new();
    private readonly Dictionary<string, (string ServiceName, string Title, string Description, string Warning)> _services = new()
    {
        ["diagtrack"] = ("DiagTrack", "Connected User Experiences and Telemetry", "Диагностические события и связанные функции телеметрии.", "Отключение может влиять на диагностику и некоторые функции Windows."),
        ["werSvc"] = ("WerSvc", "Windows Error Reporting", "Отчёты об ошибках Windows.", "Может затруднить сбор и отправку отчётов об ошибках."),
        ["dosvc"] = ("DoSvc", "Delivery Optimization", "Оптимизация доставки обновлений.", "Может изменить способ загрузки обновлений Windows."),
        ["mapsbroker"] = ("MapsBroker", "Downloaded Maps Manager", "Служба офлайн-карт.", "Офлайн-карты могут потерять часть функций."),
        ["retaildemo"] = ("RetailDemo", "Retail Demo Service", "Демонстрационная служба розничного режима.", "Оставь включённой, если устройство используется в демонстрационном режиме."),
        ["xblAuth"] = ("XblAuthManager", "Xbox Live Auth Manager", "Авторизация Xbox Live.", "Может нарушить вход в Xbox и некоторые игры Microsoft."),
        ["xblGameSave"] = ("XblGameSave", "Xbox Live Game Save", "Сохранения Xbox Live.", "Облачные сохранения Xbox могут не синхронизироваться."),
        ["xboxNet"] = ("XboxNetApiSvc", "Xbox Live Networking Service", "Сетевые функции Xbox Live.", "Сетевые функции Xbox и отдельных игр могут перестать работать."),
        ["xboxGip"] = ("XboxGipSvc", "Xbox Accessory Management Service", "Поддержка аксессуаров Xbox.", "Может затронуть аксессуары Xbox."),
        ["wsearch"] = ("WSearch", "Windows Search", "Индексирование и быстрый поиск файлов.", "Поиск в меню Пуск и Проводнике может стать медленнее."),
    };
    private readonly Dictionary<string, ServiceSnapshot> _serviceBackups = new();

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
        Loaded += (_, _) => HandleElevatedServiceCommand();
    }

    private static Brush Brush(string hex) => new BrushConverter().ConvertFromString(hex) as Brush ?? Brushes.White;
    private TextBlock Txt(string text, double size = 12, string color = TextHex, bool bold = false) =>
        new() { Text = text, FontSize = size, Foreground = Brush(color), FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, TextWrapping = TextWrapping.Wrap };
    private Border Card(UIElement child, string background = PanelHex, Thickness? padding = null)
    {
        Brush surface = background == "#111B22"
            ? new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#18251A"), (Color)ColorConverter.ConvertFromString("#0D1410"), new Point(0, 0), new Point(1, 1))
            : Brush(background);
        return new Border
        {
            Child = child,
            Background = surface,
            BorderBrush = Brush(background == "#111B22" ? "#344B32" : LineHex),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(17),
            Padding = padding ?? new Thickness(20),
            Margin = new Thickness(0, 0, 0, 14),
            SnapsToDevicePixels = true,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 17,
                ShadowDepth = 2,
                Opacity = 0.13
            }
        };
    }
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
        var titleBlock = Txt(title, 26, TextHex, true);
        titleBlock.Margin = new Thickness(0, 6, 0, 5);
        PageContent.Children.Add(titleBlock);
        var sectionRule = new Border { Height = 2, Width = 44, Background = Brush(AccentHex), HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(2), Margin = new Thickness(0, 0, 0, 9) };
        PageContent.Children.Add(sectionRule);
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
        NavDashboard.Background = Brush("#0B100C"); NavDashboard.Foreground = Brush(MutedHex);
        NavProfiles.Background = Brush("#0B100C"); NavProfiles.Foreground = Brush(MutedHex);
        NavTweaks.Background = Brush("#0B100C"); NavTweaks.Foreground = Brush(MutedHex);
        NavTools.Background = Brush("#0B100C"); NavTools.Foreground = Brush(MutedHex);
        NavRestore.Background = Brush("#0B100C"); NavRestore.Foreground = Brush(MutedHex);
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
        var headline = Txt("ГОТОВ К ИГРЕ?", 34, TextHex, true); headline.Margin = new Thickness(0, 12, 0, 5); left.Children.Add(headline);
        left.Children.Add(Txt("Твики, игровые профили и системные инструменты — в одном аккуратном приложении для Windows.", 13, MutedHex));
        var actions = new WrapPanel { Margin = new Thickness(0, 14, 0, 0) };
        actions.Children.Add(Btn("Открыть твики  →", (_, _) => ShowTweaks()));
        actions.Children.Add(Btn("Игровые профили", (_, _) => ShowProfiles(), false));
        left.Children.Add(actions);
        var right = new Grid { Width = 230, Height = 184, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        var outerRing = new System.Windows.Shapes.Ellipse { Width = 158, Height = 158, Stroke = Brush("#47633A"), StrokeThickness = 1, Opacity = 0.72, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var innerRing = new System.Windows.Shapes.Ellipse { Width = 124, Height = 124, Stroke = Brush(AccentHex), StrokeThickness = 1, Opacity = 0.22, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var orbitRing = new System.Windows.Shapes.Ellipse { Width = 174, Height = 66, Stroke = Brush(CyanHex), StrokeThickness = 1, Opacity = 0.55, StrokeDashArray = new DoubleCollection { 2, 3 }, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, RenderTransformOrigin = new Point(0.5, 0.5) };
        var orbitRotation = new RotateTransform(-26);
        orbitRing.RenderTransform = orbitRotation;
        orbitRing.Loaded += (_, _) => orbitRotation.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(-26, 334, TimeSpan.FromSeconds(24)) { RepeatBehavior = RepeatBehavior.Forever });
        var coreGlow = new System.Windows.Shapes.Ellipse { Width = 91, Height = 91, Fill = Brush("#19301A"), Stroke = Brush("#527941"), StrokeThickness = 1, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var coreLetter = Txt("N", 64, AccentHex, true);
        coreLetter.HorizontalAlignment = HorizontalAlignment.Center;
        coreLetter.VerticalAlignment = VerticalAlignment.Center;
        coreLetter.Margin = new Thickness(0, -5, 6, 0);
        var orbitLabel = Txt("LIVE ENGINE  /  01", 8, MutedHex, true);
        orbitLabel.HorizontalAlignment = HorizontalAlignment.Right;
        orbitLabel.VerticalAlignment = VerticalAlignment.Top;
        orbitLabel.Margin = new Thickness(0, 9, 0, 0);
        var readyLabel = Txt("● READY", 9, AccentHex, true);
        readyLabel.HorizontalAlignment = HorizontalAlignment.Left;
        readyLabel.VerticalAlignment = VerticalAlignment.Bottom;
        readyLabel.Margin = new Thickness(3, 0, 0, 8);
        right.Children.Add(outerRing); right.Children.Add(innerRing); right.Children.Add(orbitRing); right.Children.Add(coreGlow); right.Children.Add(coreLetter); right.Children.Add(orbitLabel); right.Children.Add(readyLabel);
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
        AddSection("02 / WINDOWS SERVICES", "Службы Windows", "Эти действия требуют подтверждения UAC. NOVA сохраняет исходный тип запуска и состояние службы для отката. Не отключай службы, если пользуешься их функциями.");
        foreach (var (id, spec) in _services)
        {
            var p = new StackPanel();
            p.Children.Add(Txt(spec.Title, 15, TextHex, true));
            p.Children.Add(Txt(spec.ServiceName + "  •  " + spec.Description, 11, MutedHex));
            p.Children.Add(Txt("ВНИМАНИЕ: " + spec.Warning, 10, "#FFCB70"));
            p.Children.Add(Txt("Текущее состояние: " + GetServiceStatus(spec.ServiceName), 10, CyanHex));
            var actions = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
            actions.Children.Add(Btn("Отключить службу", (_, _) => ApplyService(id)));
            actions.Children.Add(Btn("Восстановить", (_, _) => RestoreService(id), false));
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
        if (_regBackups.Count == 0 && _powerBackups.Count == 0 && _serviceBackups.Count == 0)
        {
            var p = new StackPanel(); p.Children.Add(Txt("✓  Пока нечего откатывать", 18, AccentHex, true));
            p.Children.Add(Txt("Когда применишь твик, NOVA сохранит исходные значения и покажет их здесь.", 12, MutedHex));
            PageContent.Children.Add(Card(p));
            return;
        }
        foreach (var id in _serviceBackups.Keys.ToList())
        {
            var p = new StackPanel();
            p.Children.Add(Txt(_services.GetValueOrDefault(id).Title ?? id, 15, TextHex, true));
            p.Children.Add(Txt("Исходное состояние службы сохранено локально.", 11, MutedHex));
            p.Children.Add(Btn("Восстановить службу", (_, _) => RestoreService(id), false));
            PageContent.Children.Add(Card(p));
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
            foreach (var id in _serviceBackups.Keys.ToList()) RestoreService(id, false);
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
        "tailored" => new() { (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", 0) },
        "ads_id" => new() { (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0) },
        "feedback" => new() { (Registry.CurrentUser, @"Software\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod", 0) },
        "typing" => new() { (Registry.CurrentUser, @"Software\Microsoft\InputPersonalization", "RestrictImplicitInkCollection", 1), (Registry.CurrentUser, @"Software\Microsoft\InputPersonalization", "RestrictImplicitTextCollection", 1) },
        "activity" => new() { (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Policies\System", "PublishUserActivities", 0), (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Policies\System", "UploadUserActivities", 0) },
        "search_highlights" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings", "IsDynamicSearchBoxEnabled", 0) },
        "system_suggestions" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", 0) },
        "soft_landing" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SoftLandingEnabled", 0) },
        "subscribed_338388" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338388Enabled", 0) },
        "subscribed_338389" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338389Enabled", 0) },
        "subscribed_310093" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-310093Enabled", 0) },
        "subscribed_353694" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-353694Enabled", 0) },
        "subscribed_353696" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-353696Enabled", 0) },
        "silent_apps" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SilentInstalledAppsEnabled", 0) },
        "preinstalled_apps" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "PreInstalledAppsEnabled", 0) },
        "preinstalled_ever" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "PreInstalledAppsEverEnabled", 0) },
        "start_track_progs" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_TrackProgs", 0) },
        "start_track_docs" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_TrackDocs", 0) },
        "no_recent_docs" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoRecentDocsHistory", 1) },
        "widgets" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarDa", 0) },
        "chat_button" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarMn", 0) },
        "task_view" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowTaskViewButton", 0) },
        "search_taskbar" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Search", "SearchboxTaskbarMode", 0) },
        "file_extensions" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "HideFileExt", 0) },
        "hidden_files" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Hidden", 1) },
        "super_hidden" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowSuperHidden", 1) },
        "separate_explorer" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "SeparateProcess", 1) },
        "full_path_title" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\CabinetState", "FullPath", 1) },
        "disable_autoplay" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoDriveTypeAutoRun", 255) },
        "disable_autorun" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoAutorun", 1) },
        "clipboard_history" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Clipboard", "EnableClipboardHistory", 0) },
        "cloud_clipboard" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Clipboard", "EnableCloudClipboard", 0) },
        "activity_feed" => new() { (Registry.CurrentUser, "Software\\Policies\\Microsoft\\Windows\\System", "EnableActivityFeed", 0) },
        "publish_activity" => new() { (Registry.CurrentUser, "Software\\Policies\\Microsoft\\Windows\\System", "PublishUserActivities", 0) },
        "upload_activity" => new() { (Registry.CurrentUser, "Software\\Policies\\Microsoft\\Windows\\System", "UploadUserActivities", 0) },
        "consumer_features" => new() { (Registry.CurrentUser, "Software\\Policies\\Microsoft\\Windows\\CloudContent", "DisableWindowsConsumerFeatures", 1) },
        "soft_landing_policy" => new() { (Registry.CurrentUser, "Software\\Policies\\Microsoft\\Windows\\CloudContent", "DisableSoftLanding", 1) },
        "tailored_policy" => new() { (Registry.CurrentUser, "Software\\Policies\\Microsoft\\Windows\\CloudContent", "DisableTailoredExperiencesWithDiagnosticData", 1) },
        "disable_web_search" => new() { (Registry.CurrentUser, "Software\\Policies\\Microsoft\\Windows\\Windows Search", "DisableWebSearch", 1) },
        "disable_cloud_search" => new() { (Registry.CurrentUser, "Software\\Policies\\Microsoft\\Windows\\Windows Search", "DisableCloudSearch", 1) },
        "cortana_policy" => new() { (Registry.CurrentUser, "Software\\Policies\\Microsoft\\Windows\\Windows Search", "AllowCortana", 0) },
        "disable_search_history" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings", "IsDeviceSearchHistoryEnabled", 0) },
        "disable_dynamic_search" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings", "IsDynamicSearchBoxEnabled", 0) },
        "background_apps" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications", "GlobalUserDisabled", 1) },
        "disable_maps_auto" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Maps", "AutoUpdateEnabled", 0) },
        "maps_download_policy" => new() { (Registry.CurrentUser, "Software\\Policies\\Microsoft\\Windows\\Maps", "AutoDownloadAndUpdateMapData", 0) },
        "notifications_global" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\PushNotifications", "ToastEnabled", 0) },
        "disable_toast_policy" => new() { (Registry.CurrentUser, "Software\\Policies\\Microsoft\\Windows\\CurrentVersion\\PushNotifications", "NoToastApplicationNotification", 1) },
        "nearby_sharing" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\CDP", "NearShareChannelUserAuthzPolicy", 0) },
        "cross_device" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\CDP", "CdpSessionUserAuthzPolicy", 0) },
        "typing_insights" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Input\\Settings", "InsightsEnabled", 0) },
        "emoji_suggestions" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Input\\Settings", "EnableEmojiSuggestion", 0) },
        "thumbnail_cache_policy" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "DisableThumbnails", 1) },
        "thumbnail_db" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "DisableThumbnailCache", 1) },
        "lock_screen_overlay" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "RotatingLockScreenOverlayEnabled", 0) },
        "lock_screen_wallpaper" => new() { (Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "RotatingLockScreenEnabled", 0) },
        "disable_consumer_tips" => new() { (Registry.CurrentUser, "Software\\Policies\\Microsoft\\Windows\\CloudContent", "DisableConsumerAccountStateContent", 1) },
        _ => new()
    };

    private bool IsAdministrator()
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        return new System.Security.Principal.WindowsPrincipal(identity).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

    private void HandleElevatedServiceCommand()
    {
        var args = Environment.GetCommandLineArgs();
        if (args.Length < 4 || args[1] != "--nova-service") return;
        var action = args[2]; var id = args[3];
        if (!_services.ContainsKey(id)) return;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (action == "disable") ApplyService(id);
            else if (action == "restore") RestoreService(id);
        }));
    }

    private bool EnsureServiceElevation(string action, string id)
    {
        if (IsAdministrator()) return true;
        try
        {
            var exe = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exe)) throw new InvalidOperationException("Не удалось определить путь к приложению.");
            var start = new ProcessStartInfo(exe) { UseShellExecute = true, Verb = "runas", Arguments = $"--nova-service {action} {id}" };
            Process.Start(start);
            Close();
        }
        catch (System.ComponentModel.Win32Exception) { MessageBox.Show("Запуск с правами администратора отменён.", "NOVA PC BOOST", MessageBoxButton.OK, MessageBoxImage.Information); }
        catch (Exception ex) { MessageBox.Show("Не удалось запросить права администратора:\n" + ex.Message, "NOVA PC BOOST", MessageBoxButton.OK, MessageBoxImage.Error); }
        return false;
    }

    private string GetServiceStatus(string serviceName)
    {
        try
        {
            var output = RunCapture("sc.exe", "query " + serviceName);
            var state = System.Text.RegularExpressions.Regex.Match(output, @"STATE\s*:\s*\d+\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var config = RunCapture("sc.exe", "qc " + serviceName);
            var start = System.Text.RegularExpressions.Regex.Match(config, @"START_TYPE\s*:\s*\d+\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return (state.Success ? state.Groups[1].Value : "UNKNOWN") + " / " + (start.Success ? start.Groups[1].Value : "UNKNOWN");
        }
        catch { return "недоступна или отсутствует"; }
    }

    private ServiceSnapshot CaptureService(string serviceName)
    {
        var config = RunCapture("sc.exe", "qc " + serviceName);
        var match = System.Text.RegularExpressions.Regex.Match(config, @"START_TYPE\s*:\s*(\d+)\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success) throw new InvalidOperationException("Не удалось определить исходный тип запуска службы " + serviceName);
        var state = RunCapture("sc.exe", "query " + serviceName);
        return new ServiceSnapshot
        {
            ServiceName = serviceName,
            StartMode = match.Groups[2].Value.ToUpperInvariant(),
            WasRunning = System.Text.RegularExpressions.Regex.IsMatch(state, @"STATE\s*:\s*\d+\s+RUNNING", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
        };
    }

    private void ApplyService(string id)
    {
        if (!OperatingSystem.IsWindows() || !_services.ContainsKey(id)) return;
        if (!EnsureServiceElevation("disable", id)) return;
        var spec = _services[id];
        try
        {
            if (!_serviceBackups.ContainsKey(id)) _serviceBackups[id] = CaptureService(spec.ServiceName);
            SaveBackups();
            try { RunChecked("sc.exe", "stop " + spec.ServiceName); } catch { /* service may already be stopped or not stoppable */ }
            RunChecked("sc.exe", "config " + spec.ServiceName + " start= disabled");
            SaveBackups();
            StatusText.Text = "ОТКЛЮЧЕНА СЛУЖБА: " + spec.ServiceName.ToUpperInvariant();
            MessageBox.Show("Служба отключена. Исходный тип запуска и состояние сохранены для отката.", "NOVA PC BOOST", MessageBoxButton.OK, MessageBoxImage.Information);
            ShowTweaks();
        }
        catch (Exception ex) { MessageBox.Show("Не удалось изменить службу:\n" + ex.Message, "NOVA PC BOOST", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void RestoreService(string id, bool notify = true)
    {
        if (!_services.ContainsKey(id) || !_serviceBackups.TryGetValue(id, out var snapshot)) return;
        if (!EnsureServiceElevation("restore", id)) return;
        try
        {
            var mode = snapshot.StartMode switch
            {
                "BOOT_START" => "boot",
                "SYSTEM_START" => "system",
                "AUTO_START" => "auto",
                "DEMAND_START" => "demand",
                "DISABLED" => "disabled",
                _ => throw new InvalidOperationException("Неизвестный исходный тип запуска: " + snapshot.StartMode)
            };
            RunChecked("sc.exe", $"config {snapshot.ServiceName} start= {mode}");
            if (snapshot.WasRunning) { try { RunChecked("sc.exe", "start " + snapshot.ServiceName); } catch { } }
            else { try { RunChecked("sc.exe", "stop " + snapshot.ServiceName); } catch { } }
            _serviceBackups.Remove(id);
            SaveBackups();
            StatusText.Text = "ВОССТАНОВЛЕНА СЛУЖБА: " + snapshot.ServiceName.ToUpperInvariant();
            if (notify) MessageBox.Show("Исходный тип запуска восстановлен. Состояние службы восстановлено настолько, насколько это разрешает Windows.", "NOVA PC BOOST", MessageBoxButton.OK, MessageBoxImage.Information);
            ShowTweaks();
        }
        catch (Exception ex) { MessageBox.Show("Не удалось восстановить службу:\n" + ex.Message, "NOVA PC BOOST", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private sealed class ServiceSnapshot
    {
        public string ServiceName { get; set; } = "";
        public string StartMode { get; set; } = "";
        public bool WasRunning { get; set; }
    }

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
        _regBackups.Clear(); _powerBackups.Clear(); _serviceBackups.Clear();
        try
        {
            if (!File.Exists(_statePath)) return;
            using var doc = JsonDocument.Parse(File.ReadAllText(_statePath));
            if (doc.RootElement.TryGetProperty("power", out var power))
                foreach (var p in power.EnumerateObject()) _powerBackups[p.Name] = p.Value.GetString() ?? "";
            if (doc.RootElement.TryGetProperty("registry", out var registry))
                foreach (var p in registry.EnumerateObject())
                    _regBackups[p.Name] = p.Value.Deserialize<List<RegSnapshot>>() ?? new();
            if (doc.RootElement.TryGetProperty("services", out var services))
                foreach (var p in services.EnumerateObject())
                    _serviceBackups[p.Name] = p.Value.Deserialize<ServiceSnapshot>() ?? new();
        }
        catch { }
    }
    private void SaveBackups()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_statePath)!);
        var data = new { power = _powerBackups, registry = _regBackups, services = _serviceBackups };
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
