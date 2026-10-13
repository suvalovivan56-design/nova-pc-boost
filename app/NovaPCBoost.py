import os, sys, shutil, tempfile, subprocess, tkinter as tk
from tkinter import messagebox
try:
 import psutil
except ImportError:
 psutil=None
VERSION="1.3.1"
BG="#080b10"; PANEL="#111923"; GREEN="#b8ff57"; CYAN="#58d9ff"; TEXT="#f5f7fa"; MUTED="#a7b1bd"
class Nova:
 def __init__(self,root):
  self.root=root; root.title("NOVA PC BOOST v"+VERSION); root.geometry("900x650"); root.minsize(760,560); root.configure(bg=BG)
  tk.Label(root,text="✳ NOVA PC BOOST",font=("Segoe UI",25,"bold"),fg=GREEN,bg=BG).pack(anchor="w",padx=28,pady=(24,4))
  tk.Label(root,text="WINDOWS GAMING TOOLKIT  •  v"+VERSION,font=("Segoe UI",10,"bold"),fg=MUTED,bg=BG).pack(anchor="w",padx=30)
  tk.Label(root,text="ТВОЙ ПК. ТВОИ ПРАВИЛА.",font=("Segoe UI",18,"bold"),fg=TEXT,bg=PANEL,padx=20,pady=18).pack(fill="x",padx=28,pady=22)
  self.stats=tk.Label(root,text="Загрузка показателей…",font=("Segoe UI",14),fg=CYAN,bg=BG); self.stats.pack(anchor="w",padx=30)
  grid=tk.Frame(root,bg=BG); grid.pack(fill="both",expand=True,padx=28,pady=20)
  items=[("Очистить временные файлы","Удаляет доступные временные файлы пользователя.",self.clean),("Игровой режим Windows","Открыть настройки Game Mode.",lambda:self.settings("ms-settings:gaming-gamemode")),("Автозагрузка","Проверить программы запуска Windows.",lambda:self.settings("ms-settings:startupapps")),("Параметры питания","Открыть параметры питания.",lambda:self.settings("ms-settings:powersleep")),("Диспетчер задач","Посмотреть нагрузку на процессы.",lambda:self.run("taskmgr.exe")),("Сведения о системе","Открыть системную информацию.",lambda:self.run("msinfo32.exe"))]
  for i,(title,desc,fn) in enumerate(items):
   box=tk.Frame(grid,bg=PANEL,highlightbackground="#263543",highlightthickness=1); box.grid(row=i//2,column=i%2,sticky="nsew",padx=6,pady=6)
   tk.Label(box,text=title,font=("Segoe UI",12,"bold"),fg=TEXT,bg=PANEL).pack(anchor="w",padx=15,pady=(14,5))
   tk.Label(box,text=desc,font=("Segoe UI",9),fg=MUTED,bg=PANEL,wraplength=300,justify="left").pack(anchor="w",padx=15)
   tk.Button(box,text="ОТКРЫТЬ",command=fn,bg=GREEN,fg=BG,relief="flat",font=("Segoe UI",9,"bold"),padx=12,pady=7).pack(anchor="w",padx=15,pady=13)
   grid.grid_columnconfigure(i%2,weight=1); grid.grid_rowconfigure(i//2,weight=1)
  tk.Button(root,text="Профили CS2 / Rust / Dota 2",command=self.profiles,bg="#17222d",fg=CYAN,relief="flat",font=("Segoe UI",11,"bold"),pady=10).pack(fill="x",padx=28,pady=(0,10))
  tk.Label(root,text="Профили содержат рекомендации и не меняют настройки автоматически.",fg=MUTED,bg=BG,font=("Segoe UI",9)).pack(pady=(0,18))
  self.refresh()
 def refresh(self):
  if psutil:
   self.stats.config(text=f"CPU  {psutil.cpu_percent()}%     RAM  {psutil.virtual_memory().percent}%")
  else:self.stats.config(text="Мониторинг CPU/RAM недоступен")
  self.root.after(1800,self.refresh)
 def clean(self):
  paths=[tempfile.gettempdir(),os.path.join(os.environ.get("LOCALAPPDATA",""),"Temp")]
  count=0
  for p in paths:
   if not p or not os.path.isdir(p): continue
   for name in os.listdir(p):
    target=os.path.join(p,name)
    try:
     if os.path.isfile(target) or os.path.islink(target): os.remove(target)
     elif os.path.isdir(target): shutil.rmtree(target)
     count+=1
    except (OSError,PermissionError): pass
  messagebox.showinfo("NOVA PC BOOST",f"Очистка завершена. Удалено объектов: {count}. Некоторые файлы могли быть заняты.")
 def settings(self,uri):
  if os.name=="nt":
   try: os.startfile(uri)
   except Exception as e: messagebox.showerror("Ошибка",str(e))
  else: messagebox.showinfo("Windows","Эта функция работает в Windows 10/11.")
 def run(self,cmd):
  if os.name=="nt":
   try: subprocess.Popen([cmd])
   except Exception as e: messagebox.showerror("Ошибка",str(e))
  else: messagebox.showinfo("Windows","Эта функция работает в Windows.")
 def profiles(self):
  w=tk.Toplevel(self.root); w.title("Игровые профили"); w.geometry("680x520"); w.configure(bg=BG)
  tk.Label(w,text="ИГРОВЫЕ ПРОФИЛИ",font=("Segoe UI",20,"bold"),fg=GREEN,bg=BG).pack(anchor="w",padx=20,pady=18)
  data={"CS2":["Проверь полноэкранный режим и частоту монитора.","Отключи ненужные фоновые приложения.","Сравни задержку ввода до и после одного изменения.","Настрой графику под стабильный FPS."],"Rust":["Сравни FPS в одной и той же локации.","Проверь использование RAM и закрывай тяжёлые фоновые задачи.","Снизь тяжёлые графические параметры по одному.","Оставляй запас места на SSD."],"Dota 2":["Сравни производительность в одинаковом игровом сценарии.","Проверь режим отображения и разрешение.","Уменьши эффекты, если просадки возникают в командных боях.","Проверь, не ограничивает ли FPS вертикальная синхронизация."]}
  v=tk.StringVar(value="CS2")
  for name in data: tk.Radiobutton(w,text=name,variable=v,value=name,bg=BG,fg=TEXT,selectcolor=PANEL,activebackground=BG,activeforeground=GREEN,font=("Segoe UI",12)).pack(anchor="w",padx=24)
  text=tk.Text(w,bg=PANEL,fg=TEXT,insertbackground=TEXT,relief="flat",wrap="word",font=("Segoe UI",11)); text.pack(fill="both",expand=True,padx=20,pady=15)
  def show(*_):
   text.config(state="normal"); text.delete("1.0","end"); text.insert("end","Рекомендации для "+v.get()+"\n\n"+"\n".join("• "+x for x in data[v.get()])); text.config(state="disabled")
  v.trace_add("write",show); show()
if __name__=="__main__":
 root=tk.Tk(); Nova(root); root.mainloop()
