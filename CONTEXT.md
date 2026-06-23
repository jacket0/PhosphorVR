# CONTEXT — VR-тренажёр ЭТУ фосфора (перенос контекста в новый чат)

## 1. Суть проекта
ВКР: VR-тренажёр для обучения операторов управлению электротермической установкой РКЗ-48
получения фосфора. Unity 6 (6000.3.10f1) + XRI 3.4.1 + OpenXR, очки Oculus/Meta Quest (билд в APK).
Мат-модель — чистый C# (детерминирована, `Random(42)`), эталон — WinForms-проект.

### Три проекта
| Проект | Путь | Роль |
|---|---|---|
| Unity VR | `C:\UniryProjects\Phosphor VR` | фронт обучаемого (этот репозиторий) |
| WinForms-эталон | `C:\Users\420pr\Desktop\PHOSPHOR\PhosphorSimulator` | рабочая модель, сверка чисел |
| Инструктор/сервер | `C:\Users\420pr\Desktop\diplom\PhosphorManager` | WinForms .NET 8: TCP-сервер + CRUD-интерфейс + SQLite |

### Разделение труда
Ассистент пишет C#-код и даёт шаги по Editor; пользователь делает всё в Unity Editor
(сцены, привязка компонентов в Inspector), запускает Play/билды, тестирует в шлеме.
Код: строго ООП/SOLID, чистый, минимум комментариев, стиль `SimulationController.cs`
(XML `<summary>` на русском, `_camelCase` приватные).

## 2. Архитектура Unity (Assets/Scripts)
- `Core/` — мат-ядро (НЕ править врозь с WinForms): `PhosphorusModel.cs` (вся физика,
  namespace `PhosphorSimulator`), `Scenario/Protocol/...` (namespace `PhosphorModeling`).
- `SimulationController.cs` — MonoBehaviour-драйвер: свойства `FosforiteFlow/QuartzFlow/CokeFlow`
  (т/ч), `ElectrodeDir/ElectrodeRate`; события `OnStep/OnDrain/OnFeed/OnProtocolReady`;
  `StartSession(scenario, parameters, idUser, idScenario)` / `StopSession()`.
- `Networking/` — `InstructorClient` (TCP, JSON-строки `\n`, корреляция по `request_id`,
  таймауты), `ServerEndpoint`, `ScenarioDto`, `IInstructorClient`.
- `Session/SessionManager.cs` — DontDestroyOnLoad-синглтон: endpoint, UserId, сценарии;
  `LoginAsync`, `UploadProtocolAsync`. Соединение открывается на время диалога.
- `Flow/` — `LoginController` (вход; флаги `fetchScenariosFromServer`, `offlineLogin`),
  `MainSceneController` (экраны: loginRoot → scenarioPickRoot → pulpitRoot; старт сессии).
- `Scenarios/` — `ScenarioMapper` (DTO → `Scenario` + `ModelParameters`), `ScenarioCatalog` (fallback).
- `UI/` — `ChartMetric` (реестр 6 метрик: Concentration/Temperature/Productivity/Energy/Mpr/MeltMass),
  `ChartBinder` (XCharts LineChart: оси, шрифты, MarkLine-регламент), `IndicatorBinder` (мнемосхема),
  `EcoTableBinder` (CO/CO₂ текущие+пределы, подсветка превышения), `ValueStepper` (+/- регулятор),
  `ControlPanelBinder` (пульт→модель; электрод: мм на пульте → м в модели, ÷1000),
  `SimulationHud` (координатор: OnStep → графики/индикаторы/эко; авто-поиск биндеров),
  `ScenarioPickController`/`ScenarioCardView` (выбор сценария; карточка недавно расширена
  пользователем: time/conc/prod/energy/temperature лейблы), `KeyboardInputTarget` (OSK→TMP-поле).
- Одна сцена **MainScene**: вход → выбор сценария → пульт (show/hide руты, без смены сцен).
- XCharts: пакет `com.monitor1394.xcharts` (3.15), `XCharts.Runtime` авто-референсится.
  НЕ использовать `init`-аксессоры (Mono). Время: моделирование фиксировано ~120 с (6 sim-ч,
  `SimHoursPerRealSecond=0.05`); поле БД `time` = время ОБУЧЕНИЯ (~1 ч) → `TrainingDurationSeconds`.

## 3. Сервер инструктора (PhosphorManager)
- `Server.cs`: TcpListener **порт 5000**, JSON-строки `\n`, эхо `request_id`. Методы:
  - `authorization` → `{success, user_id}`; в VR пускается только роль «Обучаемый»;
  - `get_scenarios` → `{scenarios:[Scenario{id,name,time,id_equipment,id_material,id_technological,
    parameters:[{name,value,min,max}]}]}` — **полный набор параметров** (UNION трёх связок EAV);
  - `save_protocol` → пишет Protocols + Protocol_data (date/endtime = unix-секунды REAL).
- `InstructorForm`: 4 вкладки — Сценарии (CRUD), Оборудование/Сырьё (имя-CRUD), Протоколы (read-only);
  кнопки-иконки `Properties.Resources.icon_add/edit/delete`. Диалоги `EditScenarioForm`/`EditNameForm`.
  Designer-код «плоский» (хелперы в InitializeComponent ломают конструктор VS).
- Слой БД: `DbUsers` (PBKDF2-SHA256, 10000 итер., 32 байта, base64), `DbScenarios`, `DbReference`
  (защита: имя таблицы из констант; нельзя удалить запись, на которую ссылается сценарий), `DbProtocols`.
- Сценарии общие для всех (таблицы назначения юзер↔сценарий НЕТ — решение пользователя).

## 4. База данных (SQLite)
Файл: `PhosphorManager\bin\Debug\net8.0-windows\db.db` (код читает `DataSource=db.db`;
при Rebuild bin чистится — копировать заново или прописать Copy to Output).
- Схема EAV: `Parameters(id,name,id_units)` + `Units`; связки `Equipment_to_params`/
  `Material_to_params`/`Technological_params` — все с `value, min, max` (min/max NULLable).
- `Scenarios(id,name,time,id_equipment,id_material,id_technological)`;
  `id_technological` = id сценария (1:1, присваивается при создании). FK на
  Technological_params УДАЛЁН (был «foreign key mismatch» — составной PK).
- `Protocol_data`: time, 3 подачи, электрод, концентрация, производительность, энергия,
  температура, **mpr, mraspl, CO, CO2**, id_protocol.
- Засеяно: 40 параметров (27 оборудование РКЗ-48 — имена = коды полей модели `D,H,D_el,I,U,P,
  Cos_phi,R_nom,E_nom,K_Rl,K_Rt,K_Rc,K_Et,K_Ec,A_raspl,K_bottom,P_melt,H_max,H_min,G_el,Rho_el,
  KPD_0,KPD_max,EcoLeakFactor_CO,EcoLeakFactor_CO2,EcoVentAirRate,K_CO2_reduction`;
  5 сырьё «Фосфоритная шихта»: `K,Ks,Cprod_nom,Craspl,Rho_prod`; 8 регламентных режима
  (русские имена): Концентрация P₄ 98/96/-, Температура 1500/1400/1600, МПР 0.3/0.05/0.6,
  Производительность 8.25/8/-, Энергопотребление 42/-/47 (МВт), CO 0.6/-/1, CO₂ 0.4/-/1,
  Нач. высота расплава 0.11). 1 сценарий «Регламентный режим РКЗ-48».
- Маппинг БД→модель — **по точному name** (решение пользователя; code-столбца нет).
- Захардкожены сознательно (нет места в БД): M_P4/M_CO/M_CO2, DtStep, KPD_changeInterval,
  NPortionsPerCycle, Rho_lmpr_eff=7000, Tsmelt, T0, DropAtDrain. Состав сырья — пропущен.
- Пользователи: `operator/operator` (Обучаемый), `instructor/instructor`, `admin/admin`,
  `operator123` — хеши совместимы с сервером.

## 5. Проделанная работа (хронологически)
1. Переходы/вход: TCP-клиент, авторизация, экраны на одной сцене, OSK-клавиатура (TMP).
2. Привязка модели к UI: 4 графика XCharts (округление, splitNumber=5, шрифты настраиваемые,
   +5% запас оси X), пульт (ValueStepper), мнемосхема (6 индикаторов), эко-таблица.
3. Фиксы: перезапуск моделирования (float-погрешность в условии конца сессии),
   маска карточек (Rect Mask 2D + Viewport), namespace `PhosphorSimulator` для SimulationStep.
4. Сервер: методы get_scenarios (полный EAV-набор) и save_protocol; интерфейс инструктора
   (4 вкладки, CRUD, иконки); сборка проверена `dotnet build` — 0 ошибок.
5. БД: финальная EAV-схема, сид параметров модели, починены битые FK.
6. Протокол урезан: клиент шлёт только ПОСЛЕДНЮЮ точку (RecordProtocolPoint в StopSession).
7. Quest: Internet Access = Require (ForceInternetPermission=1), IL2CPP+ARM64 уже стояли.
   Эпизод с подключением: сервер показывает IP виртуального адаптера (Docker 172.18.x) —
   вводить реальный LAN-IP ПК; правка GetLocalIPAddress откачена по просьбе пользователя.
8. Эко-анализ: CO 0.12–0.65%, CO₂ 0.08–0.41% во всех режимах (≤1% регламент) — лимит
   нарушить НЕЛЬЗЯ; эко зависит только от Gprod=min(K·I·КПД, ΣG/Ks), электрод не влияет.

## 6. Предстоящая работа
1. **EAV-загрузчик в Unity** (главное): `ScenarioDto` обновить под новый контракт
   (`parameters:[{name,value,min,max}]`), `ScenarioMapper` — собирать `ModelParameters`
   и регламент `Scenario` из списка по точному `name` (рус. имена регламентов ↔ поля
   ConcMin/TempMin/TempMax/ProdMin/EnergyMax(×1000 МВт→кВт)/EcoCOMax/EcoCO2Max; коды
   оборудования/сырья ↔ одноимённые поля модели). Карточка выбора (ScenarioCardView,
   расширена лейблами) — наполнять из этих параметров.
2. VR-доводка: перемещение левым стиком + поворот (заменить риг на Starter Assets
   `XR Origin (XR Rig).prefab`), удалить лишнюю не-XR камеру из сцены, Event Camera
   на World-Space Canvas, проверка на очках.
3. Протоколы: вкладка инструктора — детализация (точки Protocol_data), возможно оценка
   обучения (score/нарушения регламента) — в БД полей пока нет.
4. SQLite на клиенте (`ProtocolRepository`, define `PHOSPHOR_SQLITE`) — не подключён, опционально.
5. Не реализовано из обсуждавшегося: назначение сценариев конкретному обучаемому
   (решили «общие для всех»), редактор EAV-параметров в интерфейсе инструктора,
   эко как управляемое ограничение (требует правки мат-модели — только с согласия).

## 7. Грабли (не наступать повторно)
- Editor владеет ProjectSettings — править настройки при закрытом Unity или через UI.
- WinForms Designer: только «плоский» InitializeComponent.
- Newtonsoft есть и в Unity (com.unity.nuget.newtonsoft-json), и на сервере.
- Сервер на неизвестный метод НЕ отвечает → у клиента таймауты обязательны (есть).
- `dotnet build` падает MSB3027, если exe сервера запущен — закрыть перед сборкой.
- Время обучения ≠ время моделирования (не трогать DurationSeconds=120).
