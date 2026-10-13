import os
import sys
import json
import re
import time
import shutil
import tempfile
import subprocess
import tkinter as tk
from tkinter import messagebox
from datetime import datetime, timedelta

try:
    import psutil
except ImportError:
    psutil = None

try:
    import winreg
except ImportError:
    winreg = None

VERSION = "1.4.0"
APP_NAME = "NOVA PC BOOST"
BG = "#080B10"
SIDEBAR = "#0B1017"
PANEL = "#111923"
PANEL2 = "#151F2A"
LINE = "#263543"
GREEN = "#B8FF57"
CYAN = "#58D9FF"
TEXT = "#F5F7FA"
MUTED = "#A7B1BD"
DIM = "#72808E"
RED = "#FF6D7B"
AMBER = "#FFCB70"
FONT = "Segoe UI"

GAME_PROFILES = {
    "CS2": {
        "subtitle": "COMPETITIVE  /  LOW LATENCY",
        "color": CYAN,
        "description": "Упор на отзывчивость, стабильный FPS и понятный тест производительности.",
        "steps": [
            ("Проверь частоту монитора", "Windows → Система → Дисплей → Расширенный дисплей. Убедись, что выбрана максимальная поддерживаемая частота."),
            ("Сравни режим экрана", "Проверь полноэкранный режим и сравни плавность на одной и той же карте."),
            ("Отключи лишнюю запись клипов", "Если не пользуешься фоновыми клипами Xbox, отключи захват в разделе «Твики»."),
            ("Уменьшай графику по одному пункту", "Сохраняй настройки, проверяй FPS и задержку в похожем игровом сценарии."),
        ],
        "checks": ["Частота монитора выставлена правильно", "Нет тяжёлых фоновых задач", "FPS проверен в одинаковом сценарии"],
    },
    "RUST": {
        "subtitle": "SURVIVAL  /  FRAME STABILITY",
        "color": "#FF9A67",
        "description": "Упор на стабильность кадров, контроль RAM и уменьшение фоновой нагрузки.",
        "steps": [
            ("Посмотри нагрузку RAM", "Rust может потреблять много памяти. Закрой ненужные вкладки и тяжёлые приложения перед запуском."),
            ("Тестируй одну локацию", "Сравнивай FPS в похожем месте и при похожем количестве игроков — иначе тест нечестный."),
            ("Настраивай графику постепенно", "Измени один параметр, перезапусти сцену и оцени результат; не копируй чужие настройки вслепую."),
            ("Оставь запас на SSD", "Проверь свободное место на диске, где установлена игра и находится файл подкачки."),
        ],
        "checks": ["Свободно место на SSD", "RAM не занята лишними приложениями", "Графика проверена по одному параметру"],
    },
    "DOTA 2": {
        "subtitle": "MOBA  /  CONSISTENCY",
        "color": "#FF6D7B",
        "description": "Упор на стабильность в командных боях, видимость эффектов и ровное время кадра.",
        "steps": [
            ("Сравни большой бой", "Проверяй производительность в похожей сцене с большим количеством героев и эффектов."),
            ("Проверь режим отображения", "Попробуй полноэкранный режим и выбранное разрешение, затем сравни ощущения."),
            ("Настрой тяжёлые эффекты", "Если просадки появляются в замесах, снижай тяжёлые эффекты по одному."),
            ("Проверь лимит FPS", "Убедись, что игровой лимит FPS и вертикальная синхронизация не ограничивают кадры неожиданно."),
        ],
        "checks": ["Проверен лимит FPS", "Нет ненужных фоновых приложений", "Настройки протестированы в большом бою"],
    },
}

TWEAKS = {
    "game_mode": {
        "title": "Игровой режим Windows",
        "category": "WINDOWS",
        "description": "Включает связанные с Game Mode параметры текущего пользователя. Может помогать Windows отдавать игре приоритет, но прирост не гарантирован.",
        "impact": "Низкий риск",
        "warning": "Применяются два параметра текущего пользователя. Перезапуск обычно не нужен.",
    },
    "capture": {
        "title": "Отключить фоновую запись Xbox",
        "category": "BACKGROUND",
        "description": "Отключает фоновые игровые захваты и запись клипов через параметры пользователя. Не отключает обычный скриншот в каждой игре.",
        "impact": "Низкий риск",
        "warning": "Используй этот твик, только если не нужны фоновые записи и клипы Xbox.",
    },
    "visuals": {
        "title": "Уменьшить анимации Windows",
        "category": "INTERFACE",
        "description": "Выбирает базовый режим визуальных эффектов Windows для текущего пользователя. Это меняет вид интерфейса, а не гарантирует прирост FPS в игре.",
        "impact": "На внешний вид",
        "warning": "Некоторые анимации и визуальные эффекты интерфейса могут измениться.",
    },
    "power": {
        "title": "План питания «Высокая производительность»",
        "category": "POWER",
        "description": "Переключает план питания Windows на High performance, если он доступен на этом ПК.",
        "impact": "Больше энергии",
        "warning": "На ноутбуке может быстрее разряжаться батарея и увеличиться нагрев. Исходный план можно вернуть кнопкой «Откатить».",
    },
}

def config_dir():
    base = os.environ.get("LOCALAPPDATA") or os.path.expanduser("~")
    path = os.path.join(base, "NOVA_PC_BOOST")
    os.makedirs(path, exist_ok=True)
    return path

STATE_PATH = os.path.join(config_dir(), "restore_state.json")

class ScrollArea(tk.Frame):
    def __init__(self, parent, bg=BG):
        super().__init__(parent, bg=bg)
        self.canvas = tk.Canvas(self, bg=bg, highlightthickness=0, bd=0)
        self.scrollbar = tk.Scrollbar(self, orient="vertical", command=self.canvas.yview,
                                      bg=PANEL, troughcolor=BG, activebackground=GREEN, bd=0,
                                      highlightthickness=0, width=8)
        self.inner = tk.Frame(self.canvas, bg=bg)
        self.window_id = self.canvas.create_window((0, 0), window=self.inner, anchor="nw")
        self.canvas.configure(yscrollcommand=self.scrollbar.set)
        self.canvas.pack(side="left", fill="both", expand=True)
        self.scrollbar.pack(side="right", fill="y")
        self.inner.bind("<Configure>", self._inner_configure)
        self.canvas.bind("<Configure>", self._canvas_configure)
        self.canvas.bind_all("<MouseWheel>", self._wheel)

    def _inner_configure(self, _event=None):
        self.canvas.configure(scrollregion=self.canvas.bbox("all"))

    def _canvas_configure(self, event):
        self.canvas.itemconfigure(self.window_id, width=event.width)

    def _wheel(self, event):
        if self.winfo_exists() and self.winfo_pointerxy()[0] >= self.winfo_rootx() and self.winfo_pointerxy()[0] <= self.winfo_rootx() + self.winfo_width():
            self.canvas.yview_scroll(int(-1 * (event.delta / 120)), "units")

class Nova:
    def __init__(self, root):
        self.root = root
        self.root.title(f"{APP_NAME}  •  v{VERSION}")
        self.root.geometry("1180x790")
        self.root.minsize(980, 660)
        self.root.configure(bg=BG)
        self.page = "dashboard"
        self.selected_game = "CS2"
        self.status_var = tk.StringVar(value="ГОТОВ К РАБОТЕ")
        self.last_cpu = 0
        self.state = self.load_state()
        self.nav_buttons = {}
        self.setup_style()
        self.build_shell()
        self.render_page()
        self.refresh_stats()

    def setup_style(self):
        self.root.option_add("*Font", (FONT, 10))
        self.root.option_add("*Dialog.msg.font", (FONT, 10))

    def load_state(self):
        try:
            with open(STATE_PATH, "r", encoding="utf-8") as f:
                data = json.load(f)
            if isinstance(data, dict):
                data.setdefault("tweaks", {})
                return data
        except (OSError, ValueError, TypeError):
            pass
        return {"tweaks": {}}

    def save_state(self):
        temp_path = STATE_PATH + ".tmp"
        with open(temp_path, "w", encoding="utf-8") as f:
            json.dump(self.state, f, ensure_ascii=False, indent=2)
        os.replace(temp_path, STATE_PATH)

    def build_shell(self):
        self.shell = tk.Frame(self.root, bg=BG)
        self.shell.pack(fill="both", expand=True)
        self.sidebar = tk.Frame(self.shell, bg=SIDEBAR, width=218)
        self.sidebar.pack(side="left", fill="y")
        self.sidebar.pack_propagate(False)
        self.right = tk.Frame(self.shell, bg=BG)
        self.right.pack(side="left", fill="both", expand=True)

        brand = tk.Frame(self.sidebar, bg=SIDEBAR)
        brand.pack(fill="x", padx=20, pady=(22, 28))
        tk.Label(brand, text="✳", font=(FONT, 25, "bold"), fg=GREEN, bg=SIDEBAR).pack(side="left")
        name = tk.Frame(brand, bg=SIDEBAR)
        name.pack(side="left", padx=(9, 0))
        tk.Label(name, text="NOVA", font=(FONT, 16, "bold"), fg=TEXT, bg=SIDEBAR).pack(anchor="w")
        tk.Label(name, text="PC BOOST", font=(FONT, 8, "bold"), fg=GREEN, bg=SIDEBAR).pack(anchor="w")

        tk.Label(self.sidebar, text="WORKSPACE", fg=DIM, bg=SIDEBAR, font=(FONT, 8, "bold")).pack(anchor="w", padx=22, pady=(0, 9))
        for key, icon, title in [
            ("dashboard", "◈", "Обзор"),
            ("profiles", "▦", "Игровые профили"),
            ("tweaks", "⌁", "Твики Windows"),
            ("tools", "⚙", "Инструменты"),
            ("restore", "↶", "Откат изменений"),
        ]:
            btn = tk.Button(self.sidebar, text=f"  {icon}    {title}", command=lambda k=key: self.navigate(k),
                            anchor="w", padx=12, pady=12, bg=SIDEBAR, fg=MUTED,
                            activebackground=PANEL2, activeforeground=TEXT, relief="flat",
                            bd=0, cursor="hand2", font=(FONT, 10, "bold"))
            btn.pack(fill="x", padx=12, pady=2)
            self.nav_buttons[key] = btn

        lower = tk.Frame(self.sidebar, bg=SIDEBAR)
        lower.pack(side="bottom", fill="x", padx=17, pady=18)
        tk.Frame(lower, bg=LINE, height=1).pack(fill="x", pady=(0, 13))
        tk.Label(lower, text="WINDOWS GAMING TOOLKIT", fg=DIM, bg=SIDEBAR, font=(FONT, 7, "bold")).pack(anchor="w")
        tk.Label(lower, text=f"Версия {VERSION}", fg=MUTED, bg=SIDEBAR, font=(FONT, 9)).pack(anchor="w", pady=(5, 0))
        tk.Label(lower, text="✳  MADE FOR PLAYERS", fg=GREEN, bg=SIDEBAR, font=(FONT, 8, "bold")).pack(anchor="w", pady=(12, 0))

        self.topbar = tk.Frame(self.right, bg=BG, height=66)
        self.topbar.pack(fill="x", padx=27, pady=(15, 6))
        self.topbar.pack_propagate(False)
        left = tk.Frame(self.topbar, bg=BG)
        left.pack(side="left", fill="y")
        self.breadcrumb = tk.Label(left, text="NOVA  /  OVERVIEW", fg=MUTED, bg=BG, font=(FONT, 9, "bold"))
        self.breadcrumb.pack(anchor="w", pady=(7, 2))
        self.page_title = tk.Label(left, text="Твой ПК. Твои правила.", fg=TEXT, bg=BG, font=(FONT, 17, "bold"))
        self.page_title.pack(anchor="w")
        right = tk.Frame(self.topbar, bg=BG)
        right.pack(side="right", fill="y")
        tk.Label(right, text="●", fg=GREEN, bg=BG, font=(FONT, 10, "bold")).pack(side="left", padx=(0, 7))
        tk.Label(right, text="LOCAL SESSION", fg=MUTED, bg=BG, font=(FONT, 8, "bold")).pack(side="left")
        tk.Label(right, text="  │  ", fg=LINE, bg=BG).pack(side="left")
        tk.Label(right, text=f"v{VERSION}", fg=GREEN, bg=BG, font=(FONT, 9, "bold")).pack(side="left")

        self.area = ScrollArea(self.right, BG)
        self.area.pack(fill="both", expand=True, padx=(27, 20), pady=(2, 0))
        self.status_bar = tk.Frame(self.right, bg=BG)
        self.status_bar.pack(fill="x", padx=27, pady=(5, 11))
        tk.Frame(self.status_bar, bg=LINE, height=1).pack(fill="x", pady=(0, 7))
        tk.Label(self.status_bar, textvariable=self.status_var, fg=DIM, bg=BG, font=(FONT, 8, "bold")).pack(side="left")
        tk.Label(self.status_bar, text="Результат зависит от железа и настроек игры", fg=DIM, bg=BG, font=(FONT, 8)).pack(side="right")

    def navigate(self, page):
        self.page = page
        self.render_page()

    def clear_content(self):
        for child in self.area.inner.winfo_children():
            child.destroy()

    def render_page(self):
        self.clear_content()
        labels = {
            "dashboard": ("NOVA  /  OVERVIEW", "Твой ПК. Твои правила."),
            "profiles": ("NOVA  /  GAME PROFILES", "Выбери свою игру."),
            "profile_detail": ("NOVA  /  PROFILE DETAIL", f"Профиль {self.selected_game}"),
            "tweaks": ("NOVA  /  WINDOWS TWEAKS", "Настройки под контролем."),
            "tools": ("NOVA  /  SYSTEM TOOLS", "Полезные инструменты."),
            "restore": ("NOVA  /  RECOVERY", "Ты всегда можешь откатить изменения."),
        }
        crumb, title = labels.get(self.page, labels["dashboard"])
        self.breadcrumb.config(text=crumb)
        self.page_title.config(text=title)
        for key, btn in self.nav_buttons.items():
            selected = key == self.page or (self.page == "profile_detail" and key == "profiles")
            btn.config(bg=PANEL2 if selected else SIDEBAR, fg=GREEN if selected else MUTED)
        if self.page == "dashboard":
            self.render_dashboard()
        elif self.page == "profiles":
            self.render_profiles()
        elif self.page == "profile_detail":
            self.render_profile_detail()
        elif self.page == "tweaks":
            self.render_tweaks()
        elif self.page == "tools":
            self.render_tools()
        elif self.page == "restore":
            self.render_restore()
        self.area.canvas.yview_moveto(0)

    def card(self, parent, bg=PANEL, padx=18, pady=16):
        frame = tk.Frame(parent, bg=bg, highlightbackground=LINE, highlightthickness=1, bd=0)
        frame.configure(padx=padx, pady=pady)
        return frame

    def label(self, parent, text, size=10, color=TEXT, bold=False, bg=None, **kwargs):
        return tk.Label(parent, text=text, font=(FONT, size, "bold" if bold else "normal"),
                        fg=color, bg=bg or parent.cget("bg"), **kwargs)

    def button(self, parent, text, command, primary=True, small=False, **kwargs):
        bg = GREEN if primary else PANEL2
        fg = BG if primary else TEXT
        return tk.Button(parent, text=text, command=command, bg=bg, fg=fg,
                         activebackground="#CAFF83" if primary else "#233342",
                         activeforeground=BG if primary else TEXT,
                         relief="flat", bd=0, cursor="hand2",
                         font=(FONT, 9 if small else 10, "bold"),
                         padx=13 if small else 17, pady=7 if small else 10, **kwargs)

    def section_heading(self, parent, eyebrow, title, subtitle=None):
        self.label(parent, eyebrow, 8, GREEN, True).pack(anchor="w", pady=(0, 7))
        self.label(parent, title, 20, TEXT, True).pack(anchor="w")
        if subtitle:
            self.label(parent, subtitle, 10, MUTED, False, wraplength=810, justify="left").pack(anchor="w", pady=(6, 0))

    def meter(self, parent, value, color=GREEN):
        row = tk.Frame(parent, bg=PANEL)
        row.pack(fill="x", pady=(8, 0))
        canvas = tk.Canvas(row, height=7, bg="#25313D", highlightthickness=0, bd=0)
        canvas.pack(fill="x")
        def draw(_event=None):
            canvas.delete("all")
            width = max(canvas.winfo_width(), 2)
            canvas.create_rectangle(0, 0, width, 7, fill="#25313D", outline="")
            canvas.create_rectangle(0, 0, width * max(0, min(value, 100)) / 100, 7, fill=color, outline="")
        canvas.bind("<Configure>", draw)
        draw()
        return canvas

    def stat_card(self, parent, title, value, detail, percent=None, color=GREEN):
        frame = self.card(parent, PANEL, 15, 13)
        self.label(frame, title.upper(), 8, MUTED, True).pack(anchor="w")
        self.label(frame, value, 23, TEXT, True).pack(anchor="w", pady=(7, 0))
        self.label(frame, detail, 8, DIM).pack(anchor="w", pady=(4, 0))
        if percent is not None:
            self.meter(frame, percent, color)
        return frame

    def render_dashboard(self):
        wrap = self.area.inner
        hero = tk.Frame(wrap, bg="#111B22", highlightbackground="#314339", highlightthickness=1)
        hero.pack(fill="x", pady=(5, 18))
        left = tk.Frame(hero, bg="#111B22")
        left.pack(side="left", fill="both", expand=True, padx=22, pady=22)
        self.label(left, "✳  YOUR WINDOWS, TUNED BY YOU", 8, GREEN, True).pack(anchor="w")
        self.label(left, "ГОТОВ К ИГРЕ?", 26, TEXT, True).pack(anchor="w", pady=(11, 5))
        self.label(left, "Твики, игровые профили и системные инструменты —\nв одном аккуратном интерфейсе.", 10, MUTED, wraplength=500, justify="left").pack(anchor="w")
        actions = tk.Frame(left, bg="#111B22")
        actions.pack(anchor="w", pady=(16, 0))
        self.button(actions, "Открыть твики  →", lambda: self.navigate("tweaks")).pack(side="left")
        self.button(actions, "Игровые профили", lambda: self.navigate("profiles"), False).pack(side="left", padx=(9, 0))
        right = tk.Frame(hero, bg="#111B22")
        right.pack(side="right", padx=25, pady=20)
        tk.Label(right, text="NOVA", font=(FONT, 31, "bold"), fg=GREEN, bg="#111B22").pack()
        tk.Label(right, text="CONTROL CENTER", font=(FONT, 8, "bold"), fg=CYAN, bg="#111B22").pack()
        tk.Label(right, text="● READY", font=(FONT, 8, "bold"), fg=GREEN, bg="#111B22").pack(pady=(12, 0))

        self.section_heading(wrap, "01 / LIVE MONITOR", "Состояние системы", "Показатели обновляются автоматически. Значения зависят от текущей нагрузки.")
        stats = tk.Frame(wrap, bg=BG)
        stats.pack(fill="x", pady=(12, 19))
        for col in range(3):
            stats.grid_columnconfigure(col, weight=1, uniform="stats")
        cpu = self.stat_card(stats, "CPU", "—%", "Нагрузка процессора", 0, CYAN)
        ram = self.stat_card(stats, "RAM", "—%", "Использование памяти", 0, GREEN)
        disk = self.stat_card(stats, "Диск", "—%", "Занято на системном диске", 0, AMBER)
        cpu.grid(row=0, column=0, sticky="nsew", padx=(0, 6))
        ram.grid(row=0, column=1, sticky="nsew", padx=6)
        disk.grid(row=0, column=2, sticky="nsew", padx=(6, 0))
        self.stat_widgets = {"cpu": cpu, "ram": ram, "disk": disk}

        lower = tk.Frame(wrap, bg=BG)
        lower.pack(fill="x", pady=(0, 18))
        lower.grid_columnconfigure(0, weight=3)
        lower.grid_columnconfigure(1, weight=2)
        profiles = self.card(lower, PANEL, 18, 16)
        profiles.grid(row=0, column=0, sticky="nsew", padx=(0, 7))
        self.label(profiles, "02 / QUICK PROFILES", 8, GREEN, True).pack(anchor="w")
        self.label(profiles, "С чего начнём?", 16, TEXT, True).pack(anchor="w", pady=(6, 10))
        for game, desc, col in [
            ("CS2", "Стабильный FPS и отзывчивость", CYAN),
            ("RUST", "Контроль RAM и фризов", "#FF9A67"),
            ("DOTA 2", "Стабильность в командных боях", RED),
        ]:
            row = tk.Frame(profiles, bg=PANEL)
            row.pack(fill="x", pady=4)
            tk.Label(row, text="●", fg=col, bg=PANEL, font=(FONT, 10, "bold")).pack(side="left", padx=(0, 8))
            self.label(row, game, 10, TEXT, True).pack(side="left")
            self.label(row, desc, 9, MUTED).pack(side="left", padx=(10, 0))
            self.button(row, "Открыть", lambda g=game: self.open_profile(g), False, True).pack(side="right")
        tools = self.card(lower, PANEL, 17, 16)
        tools.grid(row=0, column=1, sticky="nsew", padx=(7, 0))
        self.label(tools, "03 / QUICK ACTIONS", 8, GREEN, True).pack(anchor="w")
        self.label(tools, "Инструменты", 16, TEXT, True).pack(anchor="w", pady=(6, 11))
        for title, desc, fn in [
            ("Диспетчер задач", "Проверить фоновые процессы", lambda: self.run_windows("taskmgr.exe")),
            ("Автозагрузка", "Управление запуском приложений", lambda: self.open_settings("ms-settings:startupapps")),
            ("Игровой режим", "Настройки Windows", lambda: self.open_settings("ms-settings:gaming-gamemode")),
        ]:
            self.action_row(tools, title, desc, fn)
        footer = self.card(wrap, "#0E151C", 15, 13)
        footer.pack(fill="x", pady=(0, 14))
        self.label(footer, "ВАЖНО", 8, AMBER, True).pack(anchor="w")
        self.label(footer, "NOVA не обещает магический прирост FPS. Меняй по одному параметру, проверяй результат и используй откат для применённых твиков.", 9, MUTED, wraplength=850, justify="left").pack(anchor="w", pady=(5, 0))

    def action_row(self, parent, title, desc, command):
        row = tk.Frame(parent, bg=PANEL)
        row.pack(fill="x", pady=5)
        col = tk.Frame(row, bg=PANEL)
        col.pack(side="left", fill="x", expand=True)
        self.label(col, title, 9, TEXT, True).pack(anchor="w")
        self.label(col, desc, 8, DIM).pack(anchor="w", pady=(2, 0))
        self.button(row, "↗", command, False, True, width=3).pack(side="right", padx=(6, 0))

    def render_profiles(self):
        wrap = self.area.inner
        self.section_heading(wrap, "01 / CHOOSE YOUR GAME", "Под каждый стиль — свой план", "Это не волшебные пресеты: здесь понятные рекомендации и базовые Windows-настройки, которые можно применить с резервной копией.")
        grid = tk.Frame(wrap, bg=BG)
        grid.pack(fill="x", pady=(19, 18))
        for i, (game, info) in enumerate(GAME_PROFILES.items()):
            card = self.card(grid, PANEL, 17, 17)
            card.grid(row=0, column=i, sticky="nsew", padx=(0 if i == 0 else 6, 0 if i == 2 else 6))
            grid.grid_columnconfigure(i, weight=1, uniform="games")
            band = tk.Frame(card, bg=info["color"], height=4)
            band.pack(fill="x", pady=(0, 14))
            self.label(card, game, 22, info["color"], True).pack(anchor="w")
            self.label(card, info["subtitle"], 7, MUTED, True).pack(anchor="w", pady=(5, 11))
            self.label(card, info["description"], 9, MUTED, wraplength=245, justify="left").pack(anchor="w", pady=(0, 14))
            self.label(card, "ЧТО ВНУТРИ", 7, DIM, True).pack(anchor="w", pady=(0, 7))
            for step, _detail in info["steps"][:3]:
                row = tk.Frame(card, bg=PANEL)
                row.pack(anchor="w", fill="x", pady=3)
                tk.Label(row, text="✓", fg=info["color"], bg=PANEL, font=(FONT, 9, "bold")).pack(side="left", padx=(0, 6))
                self.label(row, step, 8, TEXT, wraplength=205, justify="left").pack(side="left")
            self.button(card, "Открыть профиль  →", lambda g=game: self.open_profile(g)).pack(fill="x", pady=(17, 0))
        info = self.card(wrap, "#0E151C", 17, 14)
        info.pack(fill="x", pady=(0, 16))
        self.label(info, "ОБЩАЯ БАЗА", 8, GREEN, True).pack(anchor="w")
        self.label(info, "Базовый игровой режим Windows и отключение фоновой записи Xbox находятся во вкладке «Твики Windows». Все применённые параметры можно откатить.", 9, MUTED, wraplength=850, justify="left").pack(anchor="w", pady=(5, 0))

    def open_profile(self, game):
        self.selected_game = game
        self.page = "profile_detail"
        self.render_page()

    def render_profile_detail(self):
        game = self.selected_game
        info = GAME_PROFILES[game]
        wrap = self.area.inner
        hero = tk.Frame(wrap, bg=PANEL, highlightbackground=LINE, highlightthickness=1)
        hero.pack(fill="x", pady=(5, 15))
        tk.Frame(hero, bg=info["color"], width=5).pack(side="left", fill="y")
        body = tk.Frame(hero, bg=PANEL)
        body.pack(side="left", fill="both", expand=True, padx=20, pady=17)
        self.label(body, "GAME PROFILE  /  " + info["subtitle"], 8, info["color"], True).pack(anchor="w")
        self.label(body, game, 27, TEXT, True).pack(anchor="w", pady=(5, 3))
        self.label(body, info["description"], 9, MUTED, wraplength=800, justify="left").pack(anchor="w")
        self.button(body, "Применить базовые игровые твики", self.apply_game_basics, True).pack(anchor="w", pady=(14, 0))
        self.section_heading(wrap, "01 / RECOMMENDED CHECKLIST", "Пошаговая настройка", "Отмечай выполненные пункты. Чек-лист хранится только в этом окне.")
        checklist = self.card(wrap, PANEL, 17, 15)
        checklist.pack(fill="x", pady=(12, 17))
        self.check_vars = []
        for title, detail in info["steps"]:
            item = tk.Frame(checklist, bg=PANEL)
            item.pack(fill="x", pady=9)
            var = tk.BooleanVar(value=False)
            self.check_vars.append(var)
            cb = tk.Checkbutton(item, variable=var, bg=PANEL, activebackground=PANEL,
                                selectcolor=PANEL2, fg=GREEN, activeforeground=GREEN,
                                relief="flat", bd=0, highlightthickness=0, cursor="hand2")
            cb.pack(side="left", anchor="n", padx=(0, 9))
            col = tk.Frame(item, bg=PANEL)
            col.pack(side="left", fill="x", expand=True)
            self.label(col, title, 10, TEXT, True).pack(anchor="w")
            self.label(col, detail, 9, MUTED, wraplength=800, justify="left").pack(anchor="w", pady=(4, 0))
            tk.Frame(checklist, bg=LINE, height=1).pack(fill="x", pady=(5, 0))
        row = tk.Frame(wrap, bg=BG)
        row.pack(fill="x", pady=(0, 18))
        self.button(row, "← Все профили", lambda: self.navigate("profiles"), False).pack(side="left")
        self.button(row, "Открыть настройки графики", lambda: self.open_settings("ms-settings:display")).pack(side="right")

    def apply_game_basics(self):
        if os.name != "nt" or winreg is None:
            messagebox.showinfo(APP_NAME, "Применение твиков доступно только в Windows 10/11.")
            return
        answer = messagebox.askyesno(APP_NAME, "Для профиля " + self.selected_game + " будут включены параметры Game Mode и отключена фоновая запись Xbox. Исходные значения сохранятся для отката. Продолжить?")
        if not answer:
            return
        try:
            self.apply_tweak("game_mode", notify=False)
            self.apply_tweak("capture", notify=False)
            self.status_var.set("БАЗОВЫЙ ПРОФИЛЬ ПРИМЕНЁН")
            messagebox.showinfo(APP_NAME, "Готово. Применены два базовых твика Windows. Результат зависит от системы; проверить или отменить изменения можно во вкладке «Твики Windows».")
            self.render_page()
        except Exception as exc:
            messagebox.showerror(APP_NAME, "Не удалось применить профиль:\n" + str(exc))

    def render_tweaks(self):
        wrap = self.area.inner
        self.section_heading(wrap, "01 / CONTROL PANEL", "Твики Windows", "Каждый пункт применяется отдельно. NOVA сохраняет исходные значения перед изменением и предлагает откат.")
        warning = self.card(wrap, "#17170F", 15, 12)
        warning.pack(fill="x", pady=(14, 13))
        self.label(warning, "⚠ ПЕРЕД ПРИМЕНЕНИЕМ", 8, AMBER, True).pack(anchor="w")
        self.label(warning, "Закрой игры и сохрани работу. Не применяй всё подряд: меняй по одному параметру и проверяй результат. Изменения реестра относятся только к текущему пользователю.", 9, MUTED, wraplength=850, justify="left").pack(anchor="w", pady=(5, 0))
        for tid, spec in TWEAKS.items():
            card = self.card(wrap, PANEL, 17, 14)
            card.pack(fill="x", pady=6)
            top = tk.Frame(card, bg=PANEL)
            top.pack(fill="x")
            col = tk.Frame(top, bg=PANEL)
            col.pack(side="left", fill="x", expand=True)
            self.label(col, spec["title"], 12, TEXT, True).pack(anchor="w")
            self.label(col, spec["category"] + "   •   " + spec["impact"], 7, GREEN if self.is_applied(tid) else MUTED, True).pack(anchor="w", pady=(4, 0))
            if self.is_applied(tid):
                tk.Label(top, text="● APPLIED", bg=PANEL, fg=GREEN, font=(FONT, 8, "bold")).pack(side="right", padx=(8, 0))
            self.label(card, spec["description"], 9, MUTED, wraplength=850, justify="left").pack(anchor="w", pady=(11, 7))
            self.label(card, spec["warning"], 8, AMBER, wraplength=850, justify="left").pack(anchor="w", pady=(0, 10))
            actions = tk.Frame(card, bg=PANEL)
            actions.pack(fill="x")
            self.button(actions, "Применить", lambda t=tid: self.apply_tweak(t), True, True).pack(side="left")
            self.button(actions, "Откатить", lambda t=tid: self.restore_tweak(t), False, True).pack(side="left", padx=(8, 0))
        foot = self.card(wrap, "#0E151C", 15, 13)
        foot.pack(fill="x", pady=(12, 16))
        self.label(foot, "НЕТ КНОПКИ «УСКОРИТЬ ВСЁ» — И ЭТО ХОРОШО", 8, CYAN, True).pack(anchor="w")
        self.label(foot, "Сомнительные твики вроде отключения служб, защитника или массового изменения сетевых параметров здесь не используются.", 9, MUTED, wraplength=850, justify="left").pack(anchor="w", pady=(5, 0))

    def is_applied(self, tweak_id):
        return tweak_id in self.state.get("tweaks", {})

    def registry_edits(self, tweak_id):
        if tweak_id == "game_mode":
            return [
                ("HKCU", r"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1),
                ("HKCU", r"Software\Microsoft\GameBar", "AllowAutoGameMode", 1),
            ]
        if tweak_id == "capture":
            return [
                ("HKCU", r"System\GameConfigStore", "GameDVR_Enabled", 0),
                ("HKCU", r"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0),
            ]
        if tweak_id == "visuals":
            return [
                ("HKCU", r"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2),
            ]
        return []

    def hive(self, name):
        if name == "HKCU":
            return winreg.HKEY_CURRENT_USER
        if name == "HKLM":
            return winreg.HKEY_LOCAL_MACHINE
        raise ValueError("Неподдерживаемый раздел реестра")

    def capture_registry(self, hive_name, key_path, value_name):
        hive = self.hive(hive_name)
        try:
            with winreg.OpenKey(hive, key_path, 0, winreg.KEY_READ) as key:
                value, kind = winreg.QueryValueEx(key, value_name)
                return {"hive": hive_name, "key": key_path, "name": value_name,
                        "exists": True, "value": value, "type": kind}
        except FileNotFoundError:
            return {"hive": hive_name, "key": key_path, "name": value_name,
                    "exists": False, "value": None, "type": winreg.REG_DWORD}

    def write_registry(self, item, value):
        with winreg.CreateKeyEx(self.hive(item["hive"]), item["key"], 0, winreg.KEY_SET_VALUE) as key:
            winreg.SetValueEx(key, item["name"], 0, item["type"], value)

    def remove_registry_value(self, item):
        try:
            with winreg.OpenKey(self.hive(item["hive"]), item["key"], 0, winreg.KEY_SET_VALUE) as key:
                winreg.DeleteValue(key, item["name"])
        except FileNotFoundError:
            pass

    def apply_tweak(self, tweak_id, notify=True):
        if os.name != "nt" or winreg is None:
            messagebox.showinfo(APP_NAME, "Применение системных твиков доступно только в Windows 10/11.")
            return
        spec = TWEAKS[tweak_id]
        if tweak_id == "power":
            self.apply_power_plan(notify)
            return
        edits = self.registry_edits(tweak_id)
        try:
            entry = self.state["tweaks"].get(tweak_id)
            if entry is None:
                entry = {"registry": []}
                self.state["tweaks"][tweak_id] = entry
            existing = {(x["hive"], x["key"], x["name"]) for x in entry.get("registry", [])}
            for hive_name, key_path, value_name, new_value in edits:
                identity = (hive_name, key_path, value_name)
                if identity not in existing:
                    entry.setdefault("registry", []).append(self.capture_registry(hive_name, key_path, value_name))
                    existing.add(identity)
                self.write_registry({"hive": hive_name, "key": key_path, "name": value_name, "type": winreg.REG_DWORD}, new_value)
            entry["label"] = spec["title"]
            entry["applied_at"] = datetime.now().isoformat(timespec="seconds")
            self.save_state()
            self.status_var.set("ПРИМЕНЁН ТВИК: " + spec["title"].upper())
            if notify:
                messagebox.showinfo(APP_NAME, "Применено: " + spec["title"] + "\nИсходные значения сохранены. При необходимости нажми «Откатить».")
            self.render_page()
        except Exception as exc:
            messagebox.showerror(APP_NAME, "Не удалось применить твик:\n" + str(exc))

    def apply_power_plan(self, notify=True):
        try:
            entry = self.state["tweaks"].get("power")
            if entry is None:
                current = subprocess.run(["powercfg", "/getactivescheme"], capture_output=True, text=True, check=True, timeout=10)
                match = re.search(r"([0-9a-fA-F]{8}-(?:[0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12})", current.stdout)
                if not match:
                    raise RuntimeError("Не получилось определить текущий план питания.")
                entry = {"power_guid": match.group(1), "label": TWEAKS["power"]["title"]}
                self.state["tweaks"]["power"] = entry
                self.save_state()
            subprocess.run(["powercfg", "/setactive", "SCHEME_MIN"], capture_output=True, text=True, check=True, timeout=10)
            entry["applied_at"] = datetime.now().isoformat(timespec="seconds")
            self.save_state()
            self.status_var.set("ВЫБРАН ПЛАН HIGH PERFORMANCE")
            if notify:
                messagebox.showinfo(APP_NAME, "Включён план «Высокая производительность». На ноутбуке это может увеличить расход батареи. Исходный план сохранён для отката.")
            self.render_page()
        except subprocess.CalledProcessError as exc:
            detail = (exc.stderr or exc.stdout or str(exc)).strip()
            messagebox.showerror(APP_NAME, "Не удалось изменить план питания. Возможно, такой план недоступен.\n" + detail)
        except Exception as exc:
            messagebox.showerror(APP_NAME, "Не удалось изменить план питания:\n" + str(exc))

    def restore_tweak(self, tweak_id, notify=True):
        entry = self.state.get("tweaks", {}).get(tweak_id)
        if not entry:
            if notify:
                messagebox.showinfo(APP_NAME, "Для этого твика нет сохранённой резервной копии изменений.")
            return
        if os.name != "nt" or winreg is None:
            messagebox.showinfo(APP_NAME, "Откат системных настроек доступен только в Windows.")
            return
        try:
            if tweak_id == "power":
                guid = entry.get("power_guid")
                if guid:
                    subprocess.run(["powercfg", "/setactive", guid], capture_output=True, text=True, check=True, timeout=10)
            else:
                for item in reversed(entry.get("registry", [])):
                    if item.get("exists"):
                        self.write_registry(item, item["value"])
                    else:
                        self.remove_registry_value(item)
            del self.state["tweaks"][tweak_id]
            self.save_state()
            self.status_var.set("ИЗМЕНЕНИЯ ОТКАЧЕНЫ")
            if notify:
                messagebox.showinfo(APP_NAME, "Исходные значения восстановлены для твика: " + TWEAKS[tweak_id]["title"])
            self.render_page()
        except Exception as exc:
            messagebox.showerror(APP_NAME, "Не удалось полностью откатить изменения:\n" + str(exc))

    def render_tools(self):
        wrap = self.area.inner
        self.section_heading(wrap, "01 / WINDOWS UTILITIES", "Быстрый доступ к системе", "NOVA открывает штатные инструменты Windows, не заменяя их своими непроверенными аналогами.")
        grid = tk.Frame(wrap, bg=BG)
        grid.pack(fill="x", pady=(16, 18))
        items = [
            ("Диспетчер задач", "Посмотри процессы, CPU, память и автозагрузку.", lambda: self.run_windows("taskmgr.exe"), "SYSTEM"),
            ("Автозагрузка", "Проверь приложения, которые запускаются вместе с Windows.", lambda: self.open_settings("ms-settings:startupapps"), "STARTUP"),
            ("Игровой режим", "Параметры игрового режима Windows.", lambda: self.open_settings("ms-settings:gaming-gamemode"), "GAMING"),
            ("Параметры дисплея", "Разрешение, масштаб и дополнительные параметры экрана.", lambda: self.open_settings("ms-settings:display"), "DISPLAY"),
            ("Параметры питания", "Питание и переход в спящий режим.", lambda: self.open_settings("ms-settings:powersleep"), "POWER"),
            ("Сведения о системе", "Основная информация о Windows и оборудовании.", lambda: self.run_windows("msinfo32.exe"), "INFO"),
            ("Монитор ресурсов", "Подробная нагрузка на процессор, память, диск и сеть.", lambda: self.run_windows("resmon.exe"), "MONITOR"),
            ("Очистка временных файлов", "Удаляет только файлы старше 48 часов из пользовательских Temp-папок.", self.clean_temp, "CLEANUP"),
        ]
        for i, (title, desc, fn, tag) in enumerate(items):
            card = self.card(grid, PANEL, 15, 14)
            card.grid(row=i // 2, column=i % 2, sticky="nsew", padx=(0 if i % 2 == 0 else 6, 6 if i % 2 == 0 else 0), pady=6)
            grid.grid_columnconfigure(i % 2, weight=1, uniform="tools")
            self.label(card, tag, 7, GREEN, True).pack(anchor="w")
            self.label(card, title, 12, TEXT, True).pack(anchor="w", pady=(7, 6))
            self.label(card, desc, 9, MUTED, wraplength=360, justify="left").pack(anchor="w", pady=(0, 12))
            self.button(card, "Открыть  ↗", fn, False, True).pack(anchor="w")
        note = self.card(wrap, "#0E151C", 15, 12)
        note.pack(fill="x", pady=(6, 16))
        self.label(note, "ПРИМЕЧАНИЕ", 8, AMBER, True).pack(anchor="w")
        self.label(note, "Некоторые страницы параметров доступны только в Windows 10/11. Запуск отдельных системных инструментов может зависеть от редакции Windows.", 9, MUTED, wraplength=850, justify="left").pack(anchor="w", pady=(5, 0))

    def render_restore(self):
        wrap = self.area.inner
        self.section_heading(wrap, "01 / SAFETY NET", "Откат изменений", "Для каждого применённого твика резервная копия хранится локально на этом компьютере.")
        rows = self.state.get("tweaks", {})
        if not rows:
            empty = self.card(wrap, PANEL, 20, 20)
            empty.pack(fill="x", pady=(18, 16))
            self.label(empty, "✓", 28, GREEN, True).pack(anchor="w")
            self.label(empty, "Пока нечего откатывать", 15, TEXT, True).pack(anchor="w", pady=(7, 4))
            self.label(empty, "Когда применишь твик, NOVA сохранит исходное значение и покажет его в этом разделе.", 9, MUTED, wraplength=780, justify="left").pack(anchor="w")
        else:
            for tweak_id, entry in rows.items():
                card = self.card(wrap, PANEL, 17, 14)
                card.pack(fill="x", pady=6)
                self.label(card, TWEAKS.get(tweak_id, {}).get("title", entry.get("label", tweak_id)), 11, TEXT, True).pack(anchor="w")
                self.label(card, "Сохранено: " + entry.get("applied_at", "дата неизвестна"), 8, DIM).pack(anchor="w", pady=(4, 10))
                self.button(card, "Откатить этот твик", lambda t=tweak_id: self.restore_tweak(t), False, True).pack(anchor="w")
            self.button(wrap, "Откатить все изменения", self.restore_all, False).pack(anchor="w", pady=(12, 17))
        detail = self.card(wrap, "#17170F", 15, 13)
        detail.pack(fill="x", pady=(7, 16))
        self.label(detail, "ВАЖНО О РЕЗЕРВНОЙ КОПИИ", 8, AMBER, True).pack(anchor="w")
        self.label(detail, "Копия хранится в папке пользователя NOVA_PC_BOOST. Не удаляй её, пока не откатил настройки. Восстановление не требует подключения к интернету.", 9, MUTED, wraplength=850, justify="left").pack(anchor="w", pady=(5, 0))

    def restore_all(self):
        ids = list(self.state.get("tweaks", {}).keys())
        if not ids:
            return
        if not messagebox.askyesno(APP_NAME, "Откатить все изменения, которые сохранила NOVA?"):
            return
        errors = []
        for tweak_id in ids:
            try:
                self.restore_tweak(tweak_id, notify=False)
            except Exception as exc:
                errors.append(tweak_id + ": " + str(exc))
        if errors:
            messagebox.showwarning(APP_NAME, "Часть изменений не удалось откатить:\n" + "\n".join(errors))
        else:
            messagebox.showinfo(APP_NAME, "Все сохранённые изменения откатились.")
        self.render_page()

    def open_settings(self, uri):
        if os.name != "nt":
            messagebox.showinfo(APP_NAME, "Эта функция доступна в Windows 10/11.")
            return
        try:
            os.startfile(uri)
            self.status_var.set("ОТКРЫТЫ ПАРАМЕТРЫ WINDOWS")
        except Exception as exc:
            messagebox.showerror(APP_NAME, "Не получилось открыть настройки:\n" + str(exc))

    def run_windows(self, command):
        if os.name != "nt":
            messagebox.showinfo(APP_NAME, "Этот системный инструмент доступен только в Windows.")
            return
        try:
            subprocess.Popen([command], shell=False)
            self.status_var.set("ЗАПУЩЕНО: " + command.upper())
        except Exception as exc:
            messagebox.showerror(APP_NAME, "Не получилось запустить инструмент:\n" + str(exc))

    def clean_temp(self):
        if not messagebox.askyesno(APP_NAME, "Удалить пользовательские временные файлы старше 48 часов? Файлы из системных папок и свежие файлы останутся."):
            return
        cutoff = time.time() - 48 * 60 * 60
        roots = {
            os.environ.get("TEMP", ""),
            os.path.join(os.environ.get("LOCALAPPDATA", ""), "Temp"),
        }
        removed = 0
        freed = 0
        errors = 0
        for root in roots:
            if not root or not os.path.isdir(root):
                continue
            for current, dirs, files in os.walk(root, topdown=True, followlinks=False):
                dirs[:] = [d for d in dirs if not os.path.islink(os.path.join(current, d))]
                for name in files:
                    path = os.path.join(current, name)
                    try:
                        if os.path.islink(path) or os.path.getmtime(path) >= cutoff:
                            continue
                        size = os.path.getsize(path)
                        os.remove(path)
                        removed += 1
                        freed += size
                    except OSError:
                        errors += 1
        mb = freed / (1024 * 1024)
        self.status_var.set(f"ОЧИЩЕНО {removed} ФАЙЛОВ")
        messagebox.showinfo(APP_NAME, f"Очистка завершена.\nУдалено файлов: {removed}\nОсвобождено примерно: {mb:.1f} МБ\nПропущено занятых/недоступных файлов: {errors}")
    
    def refresh_stats(self):
        if hasattr(self, "stat_widgets") and psutil:
            try:
                cpu = psutil.cpu_percent(interval=None)
                mem = psutil.virtual_memory()
                system_drive = os.environ.get("SystemDrive", "C:") + "\\"
                disk_info = psutil.disk_usage(system_drive)
                values = [
                    ("cpu", f"{cpu:.0f}%", f"Процессор занят на {cpu:.0f}%", cpu, CYAN),
                    ("ram", f"{mem.percent:.0f}%", f"{mem.used / (1024**3):.1f} / {mem.total / (1024**3):.1f} ГБ", mem.percent, GREEN),
                    ("disk", f"{disk_info.percent:.0f}%", f"Свободно {disk_info.free / (1024**3):.1f} ГБ", disk_info.percent, AMBER),
                ]
                for key, value, detail, pct, color in values:
                    card = self.stat_widgets[key]
                    children = card.winfo_children()
                    # Each stat card has three labels and then its canvas meter.
                    for child in children:
                        if isinstance(child, tk.Label) and child.cget("font").find("23") >= 0:
                            child.config(text=value)
                        elif isinstance(child, tk.Label) and child.cget("text") not in ("CPU", "RAM", "ДИСК"):
                            if child.cget("font").find("8") >= 0 and child.cget("fg") == DIM:
                                child.config(text=detail)
                    for child in children:
                        if isinstance(child, tk.Frame) and child.winfo_children():
                            for canvas in child.winfo_children():
                                if isinstance(canvas, tk.Canvas):
                                    canvas.delete("all")
                                    width = max(canvas.winfo_width(), 2)
                                    canvas.create_rectangle(0, 0, width, 7, fill="#25313D", outline="")
                                    canvas.create_rectangle(0, 0, width * max(0, min(pct, 100)) / 100, 7, fill=color, outline="")
            except Exception:
                pass
        elif hasattr(self, "stat_widgets") and not psutil:
            for card in self.stat_widgets.values():
                for child in card.winfo_children():
                    if isinstance(child, tk.Label) and child.cget("font").find("23") >= 0:
                        child.config(text="N/A")
                    if isinstance(child, tk.Label) and child.cget("fg") == DIM:
                        child.config(text="Установи psutil для мониторинга")
        self.root.after(1800, self.refresh_stats)

if __name__ == "__main__":
    root = tk.Tk()
    app = Nova(root)
    root.mainloop()
